using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PwdLess.Services
{
    /// <summary>
    /// Converts a template in configuration to a complete string, given a token.
    /// </summary>
    public interface ITemplateProcessor
    {
        string ProcessTemplate(string nonce);
    }
    
    public class EmailTemplateProcessor : ITemplateProcessor
    {
        private IConfigurationRoot _config;
        public EmailTemplateProcessor(IConfigurationRoot config)
        {
            _config = config;
        }

        public string ProcessTemplate(string nonce)
        {
            var body = _config["PwdLess:EmailContents:Body"].Replace("{{nonce}}", nonce);
            return body;
        }
    }
}
