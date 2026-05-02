using BroChat.Application.DTOs;
using BroChat.Application.Interfaces;
using BroChat.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;

namespace BroChat.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IExternalAuthService _externalAuthService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthController(IUserRepository userRepository, ITokenService tokenService, IExternalAuthService externalAuthService, IEmailService emailService, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _externalAuthService = externalAuthService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Conflict(new { Message = "Email already registered." });
        }

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { Message = "Registration successful" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        
        if (user == null || user.PasswordHash == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { Message = "Invalid credentials." });
        }

        var token = _tokenService.GenerateJwtToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id);

        await _userRepository.AddRefreshTokenAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        SetRefreshTokenCookie(refreshToken.Token, refreshToken.ExpiresAt);

        return Ok(new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name,
            AccessToken = token
        });
    }

    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        var externalUser = await _externalAuthService.ValidateGoogleIdTokenAsync(request.IdToken);
        if (externalUser == null)
        {
            return Unauthorized(new { Message = "Invalid Google token." });
        }

        // Check if there's any user already linked to this Google subject ID
        var user = await _userRepository.GetByProviderAsync("Google", externalUser.ProviderSubjectId);
        
        if (user == null)
        {
            // If not linked by subject ID, check if we should link to an existing account with this email
            user = await _userRepository.GetByEmailAsync(externalUser.Email);

            if (user == null)
            {
                // Register a new user
                user = new User
                {
                    Email = externalUser.Email,
                    Name = externalUser.Name
                };
                
                user.AuthProviders.Add(new AuthProvider
                {
                    ProviderName = "Google",
                    ProviderSubjectId = externalUser.ProviderSubjectId
                });

                await _userRepository.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                // Check if Google is already linked (shouldn't be, as GetByProviderAsync failed)
                if (!user.AuthProviders.Any(ap => ap.ProviderName == "Google"))
                {
                    // Link Google to this existing email account
                    user.AuthProviders.Add(new AuthProvider
                    {
                        ProviderName = "Google",
                        ProviderSubjectId = externalUser.ProviderSubjectId
                    });
                    await _userRepository.UpdateAsync(user);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
        }

        var token = _tokenService.GenerateJwtToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id);

        await _userRepository.AddRefreshTokenAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        SetRefreshTokenCookie(refreshToken.Token, refreshToken.ExpiresAt);

        return Ok(new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name,
            AccessToken = token
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken)) return Unauthorized();

        var storedToken = await _userRepository.GetRefreshTokenAsync(refreshToken);
        if (storedToken == null || !storedToken.IsActive) return Unauthorized();

        var user = storedToken.User!;
        
        var newJwtToken = _tokenService.GenerateJwtToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id);

        storedToken.RevokedAt = DateTime.UtcNow;
        await _userRepository.UpdateRefreshTokenAsync(storedToken);
        await _userRepository.AddRefreshTokenAsync(newRefreshToken);
        await _unitOfWork.SaveChangesAsync();

        SetRefreshTokenCookie(newRefreshToken.Token, newRefreshToken.ExpiresAt);

        return Ok(new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name,
            AccessToken = newJwtToken
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var storedToken = await _userRepository.GetRefreshTokenAsync(refreshToken);
            if (storedToken != null)
            {
                storedToken.RevokedAt = DateTime.UtcNow;
                await _userRepository.UpdateRefreshTokenAsync(storedToken);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        Response.Cookies.Delete("refreshToken");
        return Ok();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null)
        {
            // For security, don't reveal if user exists or not
            return Ok(new { Message = "If an account with that email exists, a password reset link has been sent." });
        }

        var token = Guid.NewGuid().ToString();
        user.ResetPasswordToken = token;
        user.ResetPasswordExpiry = DateTime.UtcNow.AddHours(1);

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Send email
        var resetLink = $"{Request.Headers["Origin"]}/reset-password?token={token}";
        var body = $@"
<div style=""font-family: 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; max-width: 500px; margin: 0 auto; padding: 40px 20px; background-color: #f8fafc;"">
    <div style=""background-color: #ffffff; padding: 40px; border-radius: 24px; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1); text-align: center; border: 1px solid #e2e8f0;"">
        <div style=""margin-bottom: 24px;"">
            <h1 style=""color: #f97316; margin: 0; font-size: 28px; font-weight: 800; letter-spacing: -0.025em;"">BroChat</h1>
        </div>
        <h2 style=""color: #1e293b; margin: 0 0 16px 0; font-size: 20px; font-weight: 700;"">Reset Your Password</h2>
        <p style=""color: #475569; line-height: 24px; margin: 0 0 32px 0; font-size: 15px;"">
            We received a request to reset your password. Click the button below to choose a new one. This link will expire in <strong>1 hour</strong>.
        </p>
        <a href=""{resetLink}"" style=""background-color: #f97316; color: #ffffff; padding: 14px 32px; text-decoration: none; border-radius: 12px; font-weight: 600; font-size: 15px; display: inline-block; transition: background-color 0.2s;"">Reset Password</a>
        <div style=""margin-top: 32px; padding-top: 24px; border-top: 1px solid #f1f5f9;"">
            <p style=""color: #94a3b8; font-size: 13px; margin: 0;"">
                If you didn't request this, you can safely ignore this email.
            </p>
        </div>
    </div>
    <div style=""text-align: center; margin-top: 24px;"">
        <p style=""color: #64748b; font-size: 12px; margin: 0;"">
            &copy; {DateTime.UtcNow.Year} BroChat. High-Performance AI Chat.
        </p>
    </div>
</div>";
        
        await _emailService.SendEmailAsync(user.Email, "Reset Your Password - BroChat", body);

        return Ok(new { Message = "If an account with that email exists, a password reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var user = await _userRepository.GetByResetTokenAsync(request.Token);

        if (user == null || user.ResetPasswordExpiry < DateTime.UtcNow)
        {
            return BadRequest(new { Message = "Invalid or expired token." });
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.ResetPasswordToken = null;
        user.ResetPasswordExpiry = null;

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { Message = "Password reset successful." });
    }

    private void SetRefreshTokenCookie(string token, DateTime expiresAt)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // should be true in production
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt
        };
        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }
}
