using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace PwdLess.Services
{
    public class SmtpMessageSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly SmtpClient _client;

        public SmtpMessageSender(IConfiguration configuration)
        {
            _configuration = configuration;

            _client = new SmtpClient()
            {
                Host = _configuration["Smtp:Host"],
                EnableSsl = Boolean.Parse(_configuration["Smtp:Ssl"]),
                Port = Int32.Parse(_configuration["Smtp:Port"]),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_configuration["Smtp:Username"], _configuration["Smtp:Password"]),
                Timeout = 100000
            };
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            using (var mailMessage = new MailMessage
            {
                From = new MailAddress(_configuration["Smtp:From"]),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            })
            {
                mailMessage.To.Add(email);
                await _client.SendMailAsync(mailMessage);
            }
        }
    }
}
