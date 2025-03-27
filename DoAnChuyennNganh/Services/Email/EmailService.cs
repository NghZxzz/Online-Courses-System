
using System.Net;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using MailKit.Security;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace DoAnChuyennNganh.Services.Email
{
    public class EmailService : IEmailService
    {

        public async Task SendEmailAsync(string recipientEmail, string subject, string htmlBody)
        {
            // Tạo nội dung email
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody, // Nội dung HTML
                TextBody = "Đây là nội dung dạng text fallback nếu HTML không hiển thị." // Nội dung text
            };

            var message = new MimeMessage
            {
                Subject = subject,
                Body = bodyBuilder.ToMessageBody()
            };

            // Đặt người gửi và người nhận
            message.From.Add(new MailboxAddress("Web E-Learning", "huunghia14665.com"));
            message.To.Add(new MailboxAddress("Recipient", recipientEmail));
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            // Gửi email qua SMTP
            using (var client = new SmtpClient())
            {
                try
                {
                    // Kết nối tới SMTP Gmail
                    await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);

                    // Xác thực email và mật khẩu (sử dụng App Password)
                    await client.AuthenticateAsync("huunghia14665@gmail.com", "wmoq lcip xoel glme");

                    // Gửi email
                    await client.SendAsync(message);

                    // Ngắt kết nối
                    await client.DisconnectAsync(true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Gửi email thất bại: {ex.Message}");
                    throw;
                }
            }
        }
    }
}
