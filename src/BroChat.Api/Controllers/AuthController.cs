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
    private readonly IUnitOfWork _unitOfWork;

    public AuthController(IUserRepository userRepository, ITokenService tokenService, IExternalAuthService externalAuthService, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _externalAuthService = externalAuthService;
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
