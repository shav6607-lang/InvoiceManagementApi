using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Threading.Tasks;
using InvoiceManagementApi.Models;
using InvoiceManagementApi.Utilities;

namespace InvoiceManagementApi.Repositories
{
    public class MaterialRepository : IMaterialRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<MaterialRepository> _logger;
        private readonly IConfiguration _configuration;

        public MaterialRepository(IConfiguration configuration, ILogger<MaterialRepository> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var rawConnectionString = configuration.GetConnectionString("DefaultConnection");
            _connectionString = rawConnectionString;

            if (!string.IsNullOrWhiteSpace(rawConnectionString))
            {
                try
                {
                    var decrypted = Utility.ConnectionStringDecrypt(rawConnectionString);
                    if (!string.IsNullOrWhiteSpace(decrypted))
                    {
                        _connectionString = decrypted;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to decrypt connection string. Falling back to the raw configuration value.");
                }
            }

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException("Database connection string is not configured. Please set 'ConnectionStrings:DefaultConnection' in appsettings.json or environment variables.");
            }
        }

        public async Task<IEnumerable<MaterialMaster>> GetMaterialsAsync(int? companyId = null, int? materialId = null, int? materialType = null, int ? userid = null)
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

            var rows = await conn.QueryAsync<MaterialMaster>(
               "sp_GetMaterialList",
               new
               {
                   CompanyId = companyId,
                   MaterialId = materialId,
                   MaterialType = materialType,
                   UserId = userid
               },
               commandType: CommandType.StoredProcedure);

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
