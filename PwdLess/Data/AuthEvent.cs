using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using PwdLess.Models;

namespace PwdLess.Data
{
    public class AuthEvent
    {
        public string ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        public string ClientIPAddress { get; set; }

        public string ClientUserAgent { get; set; }

        public DateTime OccurrenceTime { get; set; }

        public string Subject { get; set; }

        public AuthEventType Type { get; set; }
    }

    public enum AuthEventType
    {
        RegisterLocal,
        RegisterExternal,
        LoginExternal,
        LoginLocal,
        Update,
        Delete
    }
}
