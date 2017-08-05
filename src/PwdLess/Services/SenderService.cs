using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;


namespace PwdLess.Services
{
    public interface ISenderService
    {
        Task SendAsync(string contact, string nonce, string template);
    }

    enum ContactType
    {
        Email,
        PhoneNumber
    }

    public class ConsoleTestingSenderService : ISenderService
    {
        private IConfigurationRoot _config;
        private ITemplateProcessor _templateProcessor;
        private ILogger _logger;

        public ConsoleTestingSenderService(IConfigurationRoot config,
            ITemplateProcessor templateProcessor,
            ILogger<ConsoleTestingSenderService> logger)
        {
            _config = config;
            _templateProcessor = templateProcessor;
            _logger = logger;
        }

        public async Task SendAsync(string contact, string nonce, string template)
        {
            switch (AssertTypeOf(contact))
            {
                case ContactType.Email:
                    break;
                case ContactType.PhoneNumber:
                    break;
                default:
                    break;
            }
            
            _logger.LogDebug($@"To: {contact}, 
                                 From: {new MailboxAddress(_config["PwdLess:EmailAuth:From"])}, 
                                 Subject: {_config[$"PwdLess:EmailContents:{template}:Subject"]}, 
                                 Body: {_templateProcessor.ProcessTemplate(nonce, template, $"{{\"contact\":\"{contact}\"}}")}");

            //return Task;
        }

        private ContactType AssertTypeOf(string contact)
        {
            //TODO implement this!
            return ContactType.Email;
        }
    }

    public class SenderService : ISenderService
    {
        private IConfigurationRoot _config;
        private ITemplateProcessor _templateProcessor;

        public SenderService(IConfigurationRoot config,
            ITemplateProcessor templateProcessor)
        {
            _config = config;
            _templateProcessor = templateProcessor;
        }

        public async Task SendAsync(string contact, string nonce, string template)
        {
            switch (AssertTypeOf(contact))
            {
                case ContactType.Email:
                    await SendEmailAsync(contact, nonce, template);
                    break;
                case ContactType.PhoneNumber:
                    break;
                default:
                    break;
            }
        }

        private async Task SendEmailAsync(string contact, string nonce, string template)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config["PwdLess:EmailAuth:From"]));
            message.To.Add(new MailboxAddress(contact));
            message.Subject = _config[$"PwdLess:EmailContents:{template}:Subject"];

            message.Body = new TextPart(_config[$"PwdLess:EmailContents:{template}:BodyType"])
            {
                Text = _templateProcessor.ProcessTemplate(nonce, template, $"{{\"contact\":{contact}}}")
            };

            using (var client = new SmtpClient())
            {
                var server = _config["PwdLess:EmailAuth:Server"];
                var port = Int32.Parse(_config["PwdLess:EmailAuth:Port"]);
                var ssl = Boolean.Parse(_config["PwdLess:EmailAuth:SSL"]);
                var username = _config["PwdLess:EmailAuth:Username"];
                var password = _config["PwdLess:EmailAuth:Password"];

                client.Connect(server, port, ssl);
                
                client.Authenticate(username, password);

                await client.SendAsync(message);
                client.Disconnect(true);
            }
        }

        private ContactType AssertTypeOf(string contact)
        {
            //TODO implement this!
            return ContactType.Email;
        }
    }
}
