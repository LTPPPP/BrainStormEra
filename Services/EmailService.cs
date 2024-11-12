using System.Net.Mail;
using System.Net;
using OpenQA.Selenium.BiDi.Modules.Network;

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


        public async Task SendPaymentConfirmationEmailAsync(string toEmail, string userId, string userEmail, int paymentAmount, Stream imageStream, string imageName)
        {
            var subject = "[BRAINSTORMERA] Payment Confirmation";
            var body = $@"
        <html>
            <body style='font-family: Arial, sans-serif; color: #333;'>
                <h2 style='color: #2c3e50;'>Payment Confirmation from User</h2>
                <p>Hello Admin,</p>
                <p>The user with the following details has submitted a payment confirmation:</p>
                <table style='border-collapse: collapse; width: 100%; max-width: 500px;'>
                    <tr>
                        <td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>User ID</td>
                        <td style='padding: 8px; border: 1px solid #ddd;'>{userId}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Email</td>
                        <td style='padding: 8px; border: 1px solid #ddd;'>{userEmail}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Amount</td>
                        <td style='padding: 8px; border: 1px solid #ddd;'>{paymentAmount.ToString("N0")} VND</td>
                    </tr>
                </table>
                <p style='margin-top: 20px;'>Please check the attached image for payment verification.</p>
                <p>Best Regards,<br>BrainStormEra Team</p>
            </body>
        </html>";

            using var smtp = new SmtpClient(_smtpServer, _smtpPort)
            {
                Credentials = new NetworkCredential(_username, _password),
                EnableSsl = _enableSsl
            };
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_senderEmail, _senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            // Đính kèm ảnh từ stream
            if (imageStream != null && imageStream.Length > 0)
            {
                var attachment = new Attachment(imageStream, imageName);
                mailMessage.Attachments.Add(attachment);
            }

            await smtp.SendMailAsync(mailMessage);
        }

    }
}