using System.Threading.Tasks;

namespace PwdLess.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
