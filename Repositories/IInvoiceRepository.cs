using System.Threading.Tasks;
using InvoiceManagementApi.Models;

namespace InvoiceManagementApi.Repositories
{
    public interface IInvoiceRepository
    {
        Task<IEnumerable<dynamic>> GetInvoiceAsync(string? invoiceNum = null, string? fromDate = null, string? toDate = null, int? userid=0);
        Task<string> InsertOrUpdateInvoiceAsync(Invoice invoice, int? createdBy = null);
        Task<InvoiceResult> AddInvoiceAsync(Invoice invoice, int? createdBy = null);
    }
}