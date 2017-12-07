using System.Threading.Tasks;

namespace PwdLess.Services
{
    public interface ISmsSender
    {
        Task SendSmsAsync(string number, string message);
    }
}
