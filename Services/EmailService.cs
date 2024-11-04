using System.Net.Mail;
using System.Net;

namespace BrainStormEra.Services
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly bool _enableSsl;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly string _username;
        private readonly string _password;

        public EmailService(IConfiguration configuration)
        {
            _smtpServer = configuration["SmtpSettings:Server"];
            _smtpPort = int.Parse(configuration["SmtpSettings:Port"]);
            _enableSsl = bool.Parse(configuration["SmtpSettings:EnableSsl"]);
            _senderEmail = configuration["SmtpSettings:SenderEmail"];
            _senderName = configuration["SmtpSettings:SenderName"];
            _username = configuration["SmtpSettings:Username"];
            _password = configuration["SmtpSettings:Password"];
        }

        public async Task SendOtpEmailAsync(string toEmail, string otp)
        {
            var subject = "Your OTP Code for Password Reset";
            var message = $"Your OTP code for password reset is: <b>{otp}</b>. This code will expire in 10 minutes.";

            using var client = new SmtpClient(_smtpServer, _smtpPort)
            {
                Credentials = new NetworkCredential(_username, _password),
                EnableSsl = _enableSsl
            };
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_senderEmail, _senderName),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }
    }
}
