using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;
using System.Diagnostics;
using System.Collections.Generic;
using RestSharp.Deserializers;

namespace PwdLess.TestClient
{
    [TestClass]
    public class BasicProcessTest
    {
        [TestMethod]
        public void BasicProcessWorks()
        {
            // This uses "TempMail" for a DEA on the fly
            // MAKE SURE PwdLess IS RUNNING BEFORE RUNNING TESTS
            LetPwdLessSendNonce("pwdless@fulvie.com");
            var nonce = GetNonceFromEmail("dd9d920ead4fa66064e4d110f3878b13");
            GetJwtUsingNonce(nonce);
        }
        
        
        private static void LetPwdLessSendNonce(string identifier)
        {
            var client = new RestClient("http://localhost:5000");
            var request = new RestRequest("auth/sendNonce", Method.POST);
            request.AddParameter("identifier", identifier);

            IRestResponse response = client.Execute(request);
            var content = response.Content;
            var status = response.StatusCode;

            Assert.AreEqual(status, System.Net.HttpStatusCode.OK);

            Console.WriteLine($@"Request to send nonce sent.
                                 Identifier: {identifier} 
                                 Response: {content}
                                 Status: {status}");
        }

        private string GetNonceFromEmail(string emailMd5Hash)
        {
            var client = new RestClient("https://api.temp-mail.org/request/mail/id/");
            var request = new RestRequest($"{emailMd5Hash}/format/json/", Method.GET);

            var response = client.Execute<dynamic>(request);

            var body = response.Data[0]["mail_text"];
            var nonce = response.Data[0]["mail_text"]
                .Split(new[] { "code: " }, 2, StringSplitOptions.None)[1]
                .Replace(".", "")
                .Trim()
                .Replace(" ", "");

            var status = response.StatusCode;

            Assert.AreEqual(status, System.Net.HttpStatusCode.OK);
            
            Console.WriteLine($@"Getting nonce from email.
                                 Body: {body}
                                 Nonce: {nonce}
                                 Status: {status}");
            return nonce;
        }

        private void GetJwtUsingNonce(string nonce)
        {
            var client = new RestClient("http://localhost:5000");
            var request = new RestRequest("auth/nonceToToken", Method.POST);
            request.AddParameter("nonce", nonce);

            IRestResponse response = client.Execute(request);
            var content = response.Content;
            var status = response.StatusCode;

            Assert.AreEqual(status, System.Net.HttpStatusCode.OK);
            Assert.AreEqual(content.Split('.').Length, 3);

            Console.WriteLine($@"Request to get JWT from nonce.
                                 Nonce Sent: {nonce} 
                                 Response (JWT): {content}
                                 Status: {status}");
        }


    }
    class TempMail
    {
        public List<Message> Messages { get; set; }
    }

    class Message
    {
        public string mail_subject { get; set; }
        public string mail_text { get; set; }
    }
    
}
