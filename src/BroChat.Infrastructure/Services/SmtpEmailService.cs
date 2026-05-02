using BroChat.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;

namespace BroChat.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public SmtpEmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var from = _configuration["Smtp:From"] ?? throw new InvalidOperationException("SMTP From address is not configured.");
        var host = _configuration["Smtp:Host"] ?? throw new InvalidOperationException("SMTP Host is not configured.");
        var user = _configuration["Smtp:User"] ?? throw new InvalidOperationException("SMTP User is not configured.");
        var pass = _configuration["Smtp:Pass"] ?? throw new InvalidOperationException("SMTP Password is not configured.");
        var port = int.Parse(_configuration["Smtp:Port"] ?? "587");

        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(from));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;
        email.Body = new TextPart(TextFormat.Html) { Text = body };

        using var smtp = new SmtpClient();
        
        await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(user, pass);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}
