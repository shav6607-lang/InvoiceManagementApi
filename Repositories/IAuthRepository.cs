using System.Threading.Tasks;
using InvoiceManagementApi.Models;

namespace InvoiceManagementApi.Repositories
{
    public interface IAuthRepository
    {
        Task<AuthenticateResult> AuthenticateAsync(string username, string password);
    }
}
