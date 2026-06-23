using Dapper;
using InvoiceManagementApi.Models;
using InvoiceManagementApi.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;

namespace InvoiceManagementApi.Repositories
{
    public class DCRepository : IDCRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<DCRepository> _logger;
        private readonly IConfiguration _configuration;

        public DCRepository(IConfiguration configuration, ILogger<DCRepository> logger)
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
        public async Task<IEnumerable<dynamic>> GetDCAsync(string? DCNum = null, string? fromDate = null, string? toDate = null, int? userid = 0)
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
            await conn.OpenAsync();

            // 1. Pass ALL parameters expected by the stored procedure.
            // 2. Drop <CompanyMaster> and query as dynamic to match your method's return type.
            var rows = await conn.QueryAsync(
                "sp_FetchDCList",
                new
                {
                    DCNum = DCNum ?? (object)DBNull.Value,
                    FromDate = fromDate ?? (object)DBNull.Value,
                    ToDate = toDate ?? (object)DBNull.Value,
                    UserId = userid
                },
                commandType: CommandType.StoredProcedure);

            return rows;
        }

        public async Task<string> InsertOrUpdateDCAsync(DCModel dc, int? createdBy = null)
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
            parameters.Add("@DCId", dc.Id);
            parameters.Add("@DCNumber", dc.DCNumber);
            parameters.Add("@DCDate", dc.DCDate);
            parameters.Add("@VehicleNo", dc.VehicleNumber);
            parameters.Add("@CGSTPer", dc.CgstPer);
            parameters.Add("@SGSTPer", dc.SgstPer);
            parameters.Add("@IGSTPer", dc.IgstPer);
            parameters.Add("@TaxPer", dc.TaxPer);
            parameters.Add("@TaxAmount", dc.SubTotal);
            parameters.Add("@TotalAmount", dc.GrandTotal);
            parameters.Add("@UserId", createdBy);
            string jsonItems = JsonSerializer.Serialize(dc.Items);
            parameters.Add("@DCDetails", jsonItems);

            await conn.OpenAsync();
            const int commandTimeoutSeconds = 120;
            try
            {
                // Example: stored proc returns output parameter @Message varchar(100)
                parameters.Add("@Result", dbType: DbType.String, size: 100, direction: ParameterDirection.Output);

                var result = await conn.ExecuteAsync("sp_InsertDC", parameters, commandType: CommandType.StoredProcedure, commandTimeout: commandTimeoutSeconds);

                var message = parameters.Get<string>("@Result");
                return message;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error executing stored procedure sp_InsertUpdateMaterial with parameters {@Parameters}", parameters);
                throw;
            }
        }
    }
}