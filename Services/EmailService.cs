using System.Net;
using System.Net.Mail;

namespace QuanLySanPhamApp.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        try
        {
            var mailSettings = _configuration.GetSection("EmailSettings");
            var mail = new MailMessage
            {
                From = new MailAddress(mailSettings["Sender"], mailSettings["SenderName"]),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };

            mail.To.Add(new MailAddress(email));

            using var smtp = new SmtpClient(mailSettings["Host"], int.Parse(mailSettings["Port"]))
            {
                EnableSsl = bool.Parse(mailSettings["EnableSsl"]),
                Credentials = new NetworkCredential(mailSettings["Username"], mailSettings["Password"])
            };

            await smtp.SendMailAsync(mail);
            _logger.LogInformation($"Email sent to {email} successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to send email to {email}: {ex.Message}");
            throw;
        }
    }

    public async Task SendPasswordResetEmailAsync(string email, string callbackUrl)
    {
        var message = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .button {{ background-color: #4CAF50; color: white; padding: 12px 24px; text-decoration: none; display: inline-block; border-radius: 4px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2>Reset Your Password</h2>
                    <p>Please reset your password by clicking the button below:</p>
                    <p><a href='{callbackUrl}' class='button'>Reset Password</a></p>
                    <p>If you did not request to reset your password, please ignore this email.</p>
                    <p>This link will expire in 24 hours.</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, "Reset Your Password", message);
    }

    public async Task SendEmailConfirmationAsync(string email, string callbackUrl)
    {
        var message = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .button {{ background-color: #4CAF50; color: white; padding: 12px 24px; text-decoration: none; display: inline-block; border-radius: 4px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2>Confirm Your Email</h2>
                    <p>Please confirm your email address by clicking the button below:</p>
                    <p><a href='{callbackUrl}' class='button'>Confirm Email</a></p>
                    <p>If you did not create this account, please ignore this email.</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, "Confirm Your Email", message);
    }
}
