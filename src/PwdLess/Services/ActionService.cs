using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PwdLess.Auth.Services
{
    /// <summary>
    /// Handles running the HTTP Actions defined in configuration.
    /// </summary>
    public interface IActionService
    {
        Task<string> BeforeSendingNonce(string identifier);
        Task<string> BeforeSendingToken(string nonce);
    }

    public class ActionService
    {
    }
}
