using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PwdLess.Auth.Services
{
    public interface ITemplateProcessor
    {
        string ProcessTemplate(string totp);
    }

    public class EmailTemplateProcessor : ITemplateProcessor
    {
        private IConfigurationRoot _config;
        public void TemplateProcessor(IConfigurationRoot config)
        {
            _config = config;
        }

        public string ProcessTemplate(string totp)
        {
            var url = _config["PwdLess:ClientJwtUrl"].Replace("{{totp}}", totp);

            var body = _config["PwdLess:EmailContents:Body"].Replace("{{url}}", url)
                                                             .Replace("{{totp}}", totp);
            return body;
        }
    }
}
