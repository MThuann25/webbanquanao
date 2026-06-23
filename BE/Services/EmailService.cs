using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClothingShop.Application.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var portStr = _configuration["EmailSettings:Port"];
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var senderPassword = _configuration["EmailSettings:SenderPassword"];
            var senderName = _configuration["EmailSettings:SenderName"] ?? "Clothing Shop Support";

            // Luôn in ra console/log để test offline phòng trường hợp chưa cấu hình SMTP
            _logger.LogInformation("==================================================");
            _logger.LogInformation($"GỬI EMAIL TỚI: {toEmail}");
            _logger.LogInformation($"TIÊU ĐỀ: {subject}");
            _logger.LogInformation($"NỘI DUNG: \n{body}");
            _logger.LogInformation("==================================================");

            if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
            {
                _logger.LogWarning("EmailSettings chưa được cấu hình đầy đủ trong appsettings.json. Email được in ra Console thay vì gửi thực tế.");
                return true; // Trả về true để giả lập gửi thành công ở môi trường phát triển
            }

            try
            {
                int port = int.TryParse(portStr, out var p) ? p : 587;

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(senderEmail, senderName);
                    message.To.Add(new MailAddress(toEmail));
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = true;

                    using (var client = new SmtpClient(smtpServer, port))
                    {
                        client.UseDefaultCredentials = false;
                        client.Credentials = new NetworkCredential(senderEmail, senderPassword);
                        client.EnableSsl = true;

                        await client.SendMailAsync(message);
                    }
                }

                _logger.LogInformation($"Đã gửi email thực tế thành công tới {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi gửi email tới {toEmail} qua SMTP");
                return false;
            }
        }
    }
}
