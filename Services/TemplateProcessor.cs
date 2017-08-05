using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PwdLess.Services
{
    public interface ITemplateProcessor
    {
        string ProcessTemplate(string nonce, string template, string extraBodyData);
    }
    
    public class SimpleTemplateProcessor : ITemplateProcessor
    {
        private IConfigurationRoot _config;
        public SimpleTemplateProcessor(IConfigurationRoot config)
        {
            _config = config;
        }

        public string ProcessTemplate(string nonce, string template, string extraBodyData) // TODO: allow access to context to include personal User info in templates, ie. {{FavouriteColour}} 
        {

            var body = template.Replace("{{nonce}}", nonce);

            foreach (var kvPair in JsonConvert.DeserializeObject<Dictionary<string, string>>(extraBodyData))
                body = body.Replace($"{{{{{kvPair.Key}}}}}", kvPair.Value);

            return body;
        }
    }
}
