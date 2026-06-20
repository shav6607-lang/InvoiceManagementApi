using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Threading.Tasks;
using InvoiceManagementApi.Models;

namespace InvoiceManagementApi.Repositories
{
    public class MaterialRepository : IMaterialRepository
    {
        private readonly string _connectionString;
        private readonly Microsoft.Extensions.Logging.ILogger<MaterialRepository> _logger;

        public MaterialRepository(IConfiguration configuration, Microsoft.Extensions.Logging.ILogger<MaterialRepository> logger)
        {
            _logger = logger;
            // Try common connection string keys
            _connectionString = configuration.GetConnectionString("DefaultConnection");

            // Attempt to decrypt connection string if it appears encrypted
            try
            {
                var decrypted = InvoiceManagementApi.Utilities.Utility.ConnectionStringDecrypt(_connectionString);
                if (!string.IsNullOrWhiteSpace(decrypted))
                {
                    _connectionString = decrypted;
                }
            }
            catch
            {
                // ignore and use original
            }
          

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException("Database connection string is not configured. Please set 'ConnectionStrings:DefaultConnection' (or 'ConnectionStrings:TCA') in appsettings.json or environment variables.");
            }
        }

        public async Task<IEnumerable<MaterialMaster>> GetMaterialsAsync(int? companyId = null, int? materialId = null)
        {
            var connString = _connectionString;
            if (!connString.Contains("Connect Timeout", StringComparison.OrdinalIgnoreCase) && !connString.Contains("ConnectTimeout", StringComparison.OrdinalIgnoreCase))
            {
                connString = connString.TrimEnd(';') + ";Connect Timeout=60";
            }
            if (!connString.Contains("Encrypt", StringComparison.OrdinalIgnoreCase))
            {
                connString = connString.TrimEnd(';') + ";Encrypt=True;TrustServerCertificate=False";
            }

            using var conn = new SqlConnection(connString);
            var sql = "SELECT MaterialId, CompanyId, MaterialName, RatePerUnit, MaterialType FROM MaterialMaster WHERE IsActive = 1";
            if (companyId.HasValue)
            {
                sql += " AND CompanyId = @CompanyId";
            }
            if (materialId.HasValue)
            {
                sql += " AND MaterialId = @MaterialId";
            }

            await conn.OpenAsync();
            var rows = await conn.QueryAsync<MaterialMaster>(sql, new { CompanyId = companyId, MaterialId = materialId });
            return rows;
        }

        public async Task<IEnumerable<CompanyMaster>> GetCompaniesAsync(int? userid = null)
        {
            var connString = _connectionString;

            if (!connString.Contains("Connect Timeout", StringComparison.OrdinalIgnoreCase) &&
                !connString.Contains("ConnectTimeout", StringComparison.OrdinalIgnoreCase))
            {
                connString = connString.TrimEnd(';') + ";Connect Timeout=60";
            }

            if (!connString.Contains("Encrypt", StringComparison.OrdinalIgnoreCase))
            {
                connString = connString.TrimEnd(';') + ";Encrypt=True;TrustServerCertificate=False";
            }

            using var conn = new SqlConnection(connString);

            await conn.OpenAsync();

            var rows = await conn.QueryAsync<CompanyMaster>(
                "USP_GetCompanies",
                new
                {
                    UserId = userid
                },
                commandType: CommandType.StoredProcedure);

            return rows;
        }

        public async Task<MaterialResult> InsertOrUpdateMaterialAsync(MaterialRequest model)
        {
            // Ensure Azure-friendly settings
            var connString = _connectionString;
            if (!connString.Contains("Connect Timeout", StringComparison.OrdinalIgnoreCase) && !connString.Contains("ConnectTimeout", StringComparison.OrdinalIgnoreCase))
            {
                connString = connString.TrimEnd(';') + ";Connect Timeout=60";
            }
            if (!connString.Contains("Encrypt", StringComparison.OrdinalIgnoreCase))
            {
                connString = connString.TrimEnd(';') + ";Encrypt=True;TrustServerCertificate=False";
            }

            using var conn = new SqlConnection(connString);
            var parameters = new DynamicParameters();
            parameters.Add("@MaterialId", model.MaterialId);
            parameters.Add("@CompanyId", model.CompanyId);
            parameters.Add("@MaterialName", model.MaterialName);
            parameters.Add("@RatePerUnit", model.RatePerUnit);
            parameters.Add("@IsActive", model.IsActive);
            parameters.Add("@MaterialType", model.MaterialType);
            parameters.Add("@UserId", model.UserId);

            await conn.OpenAsync();
            const int commandTimeoutSeconds = 60;
            try
            {
                // Example: stored proc returns output parameter @Message varchar(100)
                parameters.Add("@Result", dbType: DbType.String, size: 100, direction: ParameterDirection.Output);

                var result = await conn.ExecuteAsync("sp_InsertUpdateMaterial", parameters, commandType: CommandType.StoredProcedure, commandTimeout: commandTimeoutSeconds);

                var message = parameters.Get<string>("@Result");
                return new MaterialResult { Affected = result, Message = message ?? string.Empty };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error executing stored procedure sp_InsertUpdateMaterial with parameters {@Parameters}", parameters);
                throw;
            }
        }
    }
}
