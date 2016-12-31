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
        Task SendAsync(string address, string body);
    }

    public class ConsoleEmailTestingService : ISenderService
    {
        private IConfigurationRoot _config;
        public ConsoleEmailTestingService(IConfigurationRoot config)
        {
            _config = config;
        }


        public Task SendAsync(string address, string body)
        {
            Console.WriteLine($"To: {address}, From: {new MailboxAddress(_config["PwdLess:EmailAuth:From"])}, Subject: {_config["PwdLess:EmailContents:Subject"]}, Body: {body}");
            return null;
        }
    }

    public class EmailService : ISenderService
    {
        private IConfigurationRoot _config;       
        public EmailService(IConfigurationRoot config)
        {
            _config = config;

        }


        public async Task SendAsync(string address, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config["PwdLess:EmailAuth:From"]));
            message.To.Add(new MailboxAddress(address));
            message.Subject = _config["PwdLess:EmailContents:Subject"];

            message.Body = new TextPart(_config["PwdLess:EmailContents:BodyType"])
            {
                Text = body
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
