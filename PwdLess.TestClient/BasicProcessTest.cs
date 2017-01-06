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
        

}
