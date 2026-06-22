using System.Threading.Tasks;
using InvoiceManagementApi.Models;

namespace InvoiceManagementApi.Repositories
{
    public interface IMaterialRepository
    {
        Task<MaterialResult> InsertOrUpdateMaterialAsync(MaterialRequest model);
        Task<IEnumerable<MaterialMaster>> GetMaterialsAsync(int? companyId = null, int? materialId = null);
        Task<IEnumerable<CompanyMaster>> GetCompaniesAsync(int? companyId = null);
    }
}
