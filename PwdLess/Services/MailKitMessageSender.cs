using System;
using System.Threading.Tasks;

namespace PwdLess.Services
{
    public class MailKitMessageSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            //var message = new MimeMessage();
            //message.From.Add(new MailboxAddress(_config["PwdLess:EmailAuth:From"]));
            //message.To.Add(new MailboxAddress(email));
            //message.Subject = _config["PwdLess:EmailContents:Subject"];
            //
            //message.Body = new TextPart(_config["PwdLess:EmailContents:BodyType"])
            //{
            //    Text = body
            //};
            //
            //using (var client = new SmtpClient())
            //{
            //    var server = _config["PwdLess:EmailAuth:Server"];
            //    var port = Int32.Parse(_config["PwdLess:EmailAuth:Port"]);
            //    var ssl = Boolean.Parse(_config["PwdLess:EmailAuth:SSL"]);
            //    var username = _config["PwdLess:EmailAuth:Username"];
            //    var password = _config["PwdLess:EmailAuth:Password"];
            //
            //    client.Connect(server, port, ssl);
            //
            //    client.Authenticate(username, password);
            //
            //    await client.SendAsync(message);
            //    client.Disconnect(true);
            //}
            //
            return Task.FromResult(0);
        }
    }
}
