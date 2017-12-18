using System;

namespace PwdLess.Data
{
    public class AuthEvent
    {
        public string AuthEventId { get; set; }

        public string ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        public string ClientIPAddress { get; set; }

        public string ClientUserAgent { get; set; }

        public DateTimeOffset OccurrenceTime { get; set; }

        public AuthEventType Type { get; set; }

        public string Subject { get; set; }
    }

    // Note: CRU events are sent after successful completion
    //       D events are sent just before attempt
    public enum AuthEventType
    {
        Register,
        AddLogin,

        Login,

        EditUserInfo,

        RemoveLogin,
        Delete
    }
}
