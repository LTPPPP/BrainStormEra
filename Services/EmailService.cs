using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void SendOTPEmail(string toEmail, string otpCode)
    {
        var emailSettings = _configuration.GetSection("EmailSettings");
        var smtpClient = new SmtpClient(emailSettings["SMTPServer"])
        {
            Port = int.Parse(emailSettings["SMTPPort"]),
            Credentials = new NetworkCredential(emailSettings["SMTPUsername"], emailSettings["SMTPPassword"]),
            EnableSsl = true,
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(emailSettings["FromEmail"]),
            Subject = "Your OTP Code",
            Body = $"Your OTP code is: {otpCode}. It will expire in 90 seconds.",
            IsBodyHtml = true,
        };

        mailMessage.To.Add(toEmail);

        smtpClient.Send(mailMessage);
    }
}
