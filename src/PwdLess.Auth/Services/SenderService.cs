using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace PwdLess.Auth.Services
{
    public interface ISenderService
    {
        Task SendAsync(string address, string totp);
    }

    public class ConsoleEmailTestingService : ISenderService
    {
        public string EmailSubject { get; private set; }
        public string EmailBody { get; private set; }
        public MailboxAddress EmailFrom { get; private set; }

        private IConfigurationRoot _config;
        public ConsoleEmailTestingService(IConfigurationRoot config)
        {
            _config = config;

            EmailFrom = new MailboxAddress(_config["PwdLess:EmailAuth:From"]);
            EmailSubject = _config["PwdLess:EmailContents:Subject"];

        }


        public Task SendAsync(string email, string totp)
        {
            var url = _config["PwdLess:ClientJwtUrl"].Replace("{{totp}}", totp);

            EmailBody = _config["PwdLess:EmailContents:Body"].Replace("{{url}}", url)
                .Replace("{{totp}}", totp);
            Console.WriteLine($"To: {email}, From: {EmailFrom}, Subject: {EmailSubject}, Body: {EmailBody}");
            return null;
        }
    }

    public class EmailService : ISenderService
    {
        public string EmailSubject { get; private set; }
        public string EmailBody { get; private set; }
        public MailboxAddress EmailFrom { get; private set; }

        private IConfigurationRoot _config;       
        public EmailService(IConfigurationRoot config)
        {
            _config = config;

            EmailFrom = new MailboxAddress(_config["PwdLess:EmailAuth:From"]);
            EmailSubject = _config["PwdLess:EmailContents:Subject"];
            
        }


        public async Task SendAsync(string email, string totp)
        {
            ProcessTemplates(totp);

            var message = new MimeMessage();
            message.From.Add(EmailFrom);
            message.To.Add(new MailboxAddress(email));
            message.Subject = EmailSubject;

            message.Body = new TextPart("plain")
            {
                Text = EmailBody
            };

            using (var client = new SmtpClient())
            {
                var server = _config["PwdLess:EmailAuth:Server"];
                var port = Int32.Parse(_config["PwdLess:EmailAuth:Port"]);
                var ssl = Boolean.Parse(_config["PwdLess:EmailAuth:SSL"]);
                var username = _config["PwdLess:EmailAuth:Username"];
                var password = _config["PwdLess:EmailAuth:Password"];


                // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                client.Connect(server, port, ssl);

                // Note: since we don't have an OAuth2 token, disable
                // the XOAUTH2 authentication mechanism.
                //client.AuthenticationMechanisms.Remove("XOAUTH2");
                client.Authenticate(username, password);

                await client.SendAsync(message);
                client.Disconnect(true);
            }
        }

        private void ProcessTemplates(string totp)
        {
            var url = _config["PwdLess:ClientJwtUrl"].Replace("{{totp}}", totp);

            EmailBody = _config["PwdLess:EmailContents:Body"].Replace("{{url}}", url)
                .Replace("{{totp}}", totp);
        }
    }
}
