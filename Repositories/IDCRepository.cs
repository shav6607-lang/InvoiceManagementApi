using System.Threading.Tasks;
using InvoiceManagementApi.Models;

namespace InvoiceManagementApi.Repositories
{
    public interface IDCRepository
    {
        Task<IEnumerable<dynamic>> GetDCAsync(string? DCNum = null, string? fromDate = null, string? toDate = null, int? userid = 0);
        Task<string> InsertOrUpdateDCAsync(DCModel dc, int? createdBy = null);
    }
}