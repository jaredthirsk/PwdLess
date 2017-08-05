using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PwdLess.Models;

namespace PwdLess.Services
{
    public interface ISenderService
    {
        Task SendAsync(string contact, string nonce, UserState userState);
    }

    enum ContactType
    {
        Email,
        PhoneNumber
    }

    public class ConsoleAsEmailTestingSenderService : ISenderService
    {
        private IConfigurationRoot _config;
        private ITemplateProcessor _templateProcessor;
        private ILogger _logger;

        public ConsoleAsEmailTestingSenderService(IConfigurationRoot config,
            ITemplateProcessor templateProcessor,
            ILogger<ConsoleAsEmailTestingSenderService> logger)
        {
            _config = config;
            _templateProcessor = templateProcessor;
            _logger = logger;
        }

        public async Task SendAsync(string contact, string nonce, UserState userState)
        {
            var templateName = nameof(userState) + "Email";
            _logger.LogDebug($@"To: {contact}, 
                                 From: {new MailboxAddress(_config["PwdLess:EmailAuth:From"])}, 
                                 Subject: {_config[$"PwdLess:MessageTemplates:{templateName}:Subject"]}, 
                                 Body: {_templateProcessor.ProcessTemplate(nonce, _config[$"PwdLess:MessageTemplates:{templateName}:Body"], $"{{\"contact\":\"{contact}\"}}")}");

            //return Task;
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

        public async Task SendAsync(string contact, string nonce, UserState userState)
        {
            switch (AssertTypeOf(contact))
            {
                case ContactType.Email:
                    await SendEmailAsync(contact, nonce, nameof(userState) + "Email"); // Note how the template name is formed
                    break;
                case ContactType.PhoneNumber:
                    break;
                default:
                    break;
            }
        }

        private async Task SendEmailAsync(string contact, string nonce, string templateName)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config["PwdLess:EmailAuth:From"]));
            message.To.Add(new MailboxAddress(contact));
            message.Subject = _config[$"PwdLess:MessageTemplates:{templateName}:Subject"];

            message.Body = new TextPart(_config[$"PwdLess:MessageTemplates:{templateName}:BodyType"])
            {
                Text = _templateProcessor.ProcessTemplate(nonce, _config[$"PwdLess:MessageTemplates:{templateName}:Body"], $"{{\"contact\":{contact}}}")
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
