namespace PwdLess.Models
{
    public class Nonce
    {
        public string Content { get; set; }
        public string Contact { get; set; }
        public UserState UserState { get; set; }
        public long Expiry { get; set; }
    }
}