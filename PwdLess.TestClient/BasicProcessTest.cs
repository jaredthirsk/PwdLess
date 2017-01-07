using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;
using System.Diagnostics;
using System.Collections.Generic;
using RestSharp.Deserializers;

namespace PwdLess.TestClient
{
    /// <summary>
    /// BE SURE PwdLess IS RUNNING FIRST.
    /// ONLY RUN THE ORDERED TEST.
    /// DELTE THE EMAIL INBOX BEFORE STARTING TEST: TempMail RETURNS EMAILS IN ARBITRARY ORDER.
    /// </summary>
    [TestClass]
    public class BasicProcessTest
    {
        public static string identifier { get; set; } = "pwdless@fulvie.com";
        public static string emailMd5Hash { get; set; } = "dd9d920ead4fa66064e4d110f3878b13";
        public static string nonce { get; set; }
        public static string token { get; set; }

        [TestMethod]
        public void FirstLetPwdLessSendNonce()
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

        [TestMethod]
        public void SecondGetNonceFromEmail()
        {
            var client = new RestClient("https://api.temp-mail.org/request/mail/id/");
            var request = new RestRequest($"{emailMd5Hash}/format/json/", Method.GET);

            var response = client.Execute<List<dynamic>>(request);

            var body = response.Data[0]["mail_text"];
            nonce = response.Data[0]["mail_text"]
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
        }

        [TestMethod]
        public void ThirdGetJwtUsingNonce()
        {
            var client = new RestClient("http://localhost:5000/");
            var request = new RestRequest("auth/nonceToToken", Method.POST);
            Console.WriteLine($"NONCE: {nonce}");
            request.AddParameter("nonce", nonce);

            IRestResponse response = client.Execute(request);
            token = response.Content;
            var status = response.StatusCode;

            Assert.AreEqual(status, System.Net.HttpStatusCode.OK);
            Assert.AreEqual(token.Split('.').Length, 3);

            Console.WriteLine($@"Request to get JWT from nonce.
                                 Nonce Sent: {nonce} 
                                 Response (JWT): {token}
                                 Status: {status}");
        }

        [TestMethod]
        public void FourthDecodeValidateJwt()
        {
            var client = new RestClient("http://localhost:5000/");
            var request = new RestRequest("auth/validateToken", Method.POST);
            request.AddHeader("Authorization", $"Bearer {token}");

            IRestResponse response = client.Execute(request);
            var content = response.Content;
            var status = response.StatusCode;

            Assert.AreEqual(status, System.Net.HttpStatusCode.OK);

            Console.WriteLine($@"Authorizing to PwdLess using JWT.
                                 JWT Sent: {token} 
                                 Response (JWT claims): {content}
                                 Status: {status}");
        }

    }
    
}
