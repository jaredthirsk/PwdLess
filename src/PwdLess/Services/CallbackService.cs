using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PwdLess.Services
{
    /// <summary>
    /// Handles running the HTTP Callbacks defined in configuration.
    /// </summary>
    public interface ICallbackService
    {
        Task<string> BeforeSendingNonce(string identifier, string type);
        Task<string> BeforeSendingToken(string token);
    }

    public class CallbackService : ICallbackService
    {
        private IConfigurationRoot _config;

        public CallbackService(IConfigurationRoot config)
        {
            _config = config;
        }

        public async Task<string> BeforeSendingNonce(string identifier, string type)
        {
            string uri = _config["PwdLess:Callbacks:BeforeSendingNonce"];

            // this feature is opt-in
            if (uri == null || uri.Length == 0)
                return "";

            HttpContent content = new StringContent(JsonConvert.SerializeObject(new
                                                    {
                                                        Identifier = identifier,
                                                        Type = type
                                                    }), 
                                                    Encoding.UTF8,
                                                    "application/json");
            HttpResponseMessage response;

            // send the POST request
            using (var client = new HttpClient())
                response = await client.PostAsync(uri, content);

            // throw an exception if not successful
            if (!response.IsSuccessStatusCode)
                throw new InvalidIdentifierException(identifier);
            else
                return await response.Content.ReadAsStringAsync();

        }

        public async Task<string> BeforeSendingToken(string token)
        {
            string uri = _config["PwdLess:Callbacks:BeforeSendingToken"];

            // this feature is opt-in
            if (uri == null || uri.Length == 0)
                return "";

            var authHeader = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response;

            // send the POST request with auth header
            using (var client = new HttpClient() { DefaultRequestHeaders = { Authorization = authHeader } })
                response = await client.PostAsync(uri, new StringContent(""));

            // throw an exception if not successful
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Unsuccessful status code recieved from callback: {uri}.");
            else
                return await response.Content.ReadAsStringAsync();
        }
    }

    public class InvalidIdentifierException : Exception
    {
        public InvalidIdentifierException(string identifier)
            : base($"Identifier invalid: {identifier}") { }
    }
}
