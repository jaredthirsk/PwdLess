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
        string EmailSubject { get; }
        string EmailBody { get; }
        MailboxAddress EmailFrom { get; }

        Task SendEmailAsync(string email, string tokeUrl);
    }

    public class ConsoleTestingService : ISenderService
    {
        public string EmailSubject { get; private set; }
        public string EmailBody { get; private set; }
        public MailboxAddress EmailFrom { get; private set; }

        private IConfigurationRoot _config;
        public ConsoleTestingService(IConfigurationRoot config)
        {
            _config = config;

            EmailFrom = new MailboxAddress(_config["PwdLess:EmailAuth:From"]);
            EmailSubject = _config["PwdLess:EmailContents:Subject"];

        }


        public Task SendEmailAsync(string email, string tokenUrl)
        {
            EmailBody = _config["PwdLess:EmailContents:Body"].Replace("{{url}}", tokenUrl);
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


        public async Task SendEmailAsync(string email, string tokenUrl)
        {
            EmailBody = _config["PwdLess:EmailContents:Body"].Replace("{{url}}", tokenUrl);

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
    }
}
