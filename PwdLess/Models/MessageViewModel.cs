using System;
using System.ComponentModel.DataAnnotations;

namespace PwdLess.Models
{
    public class MessageViewModel // Can be an error, a warning, or a success
    {
        public string MessageType { get; set; } // "Error" or "Warning" or "Success"

        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        [Display(Name = "Message")]
        public string Message { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }
    }
}