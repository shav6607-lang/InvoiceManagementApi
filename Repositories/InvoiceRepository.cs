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
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<InvoiceRepository> _logger;
        private readonly IConfiguration _configuration;

        public InvoiceRepository(IConfiguration configuration, ILogger<InvoiceRepository> logger)
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
        public async Task<IEnumerable<dynamic>> GetInvoiceAsync(string? invoiceNum = null, string? fromDate = null, string? toDate = null, int? userid=0)
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
                "sp_FetchInvoiceList",
                new
                {
                    InvoiceNo = invoiceNum ?? (object)DBNull.Value,
                    FromDate = fromDate ?? (object)DBNull.Value,
                    ToDate = toDate ?? (object)DBNull.Value,
                    UserId = userid
                },
                commandType: CommandType.StoredProcedure);

            return rows;
        }

        public async Task<string> InsertOrUpdateInvoiceAsync(Invoice invoice, int? createdBy = null)
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
            parameters.Add("@InvoiceId", invoice.Id);
            parameters.Add("@InvoiceNo", invoice.InvoiceNumber);
            parameters.Add("@InvoiceDate", invoice.InvoiceDate);
            parameters.Add("@BuyerName", invoice.BuyerName);
            parameters.Add("@Address" ,invoice.BuyerAddress);
            parameters.Add("@Phone", invoice.BuyerPhone);
            parameters.Add("@GSTNo", invoice.BuyerGstin);
            parameters.Add("@URN", invoice.Urn);
            parameters.Add("@State", invoice.BuyerState);
            parameters.Add("@StateCode",invoice.BuyerStateCode);
            parameters.Add("@DispatchThrough", invoice.DispatchedThrough);
            parameters.Add("@Destination", invoice.Destination);
            parameters.Add("@VehicleNo", invoice.VehicleNumber);
            parameters.Add("@CGSTPer", invoice.CgstPer);
            parameters.Add("@SGSTPer", invoice.SgstPer);
            parameters.Add("@IGSTPer", invoice.IgstPer);
            parameters.Add("@TaxPer", invoice.TaxPer);
            parameters.Add("@TaxAmount", invoice.SubTotal);
            parameters.Add("@TotalAmount", invoice.GrandTotal);
            parameters.Add("@UserId", createdBy);
            string jsonItems = JsonSerializer.Serialize(invoice.Items);
            parameters.Add("@InvoiceDetails", jsonItems);

            await conn.OpenAsync();
            const int commandTimeoutSeconds = 120;
            try
            {
                // Example: stored proc returns output parameter @Message varchar(100)
                parameters.Add("@Result", dbType: DbType.String, size: 100, direction: ParameterDirection.Output);

                var result = await conn.ExecuteAsync("sp_InsertInvoice", parameters, commandType: CommandType.StoredProcedure, commandTimeout: commandTimeoutSeconds);

                var message = parameters.Get<string>("@Result");
                return message;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error executing stored procedure sp_InsertUpdateMaterial with parameters {@Parameters}", parameters);
                throw;
            }
        }


        public async Task<InvoiceResult> AddInvoiceAsync(Invoice invoice, int? createdBy = null)
        {
            if (invoice == null)
                return new InvoiceResult { Affected = 0, Message = "Invoice is null." };

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

            try
            {
                using var conn = new SqlConnection(connString);
                await conn.OpenAsync();

                // Use ADO.NET for stored procedure invocation with explicit parameters (no JSON).
                using var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "";
                cmd.CommandTimeout = 1000;

                // Header parameters
                cmd.Parameters.Add(new SqlParameter("@InvoiceId", SqlDbType.NVarChar, 50) { Value = (object)invoice.Id ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@InvoiceNumber", SqlDbType.NVarChar, 100) { Value = (object)invoice.InvoiceNumber ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@InvoiceDate", SqlDbType.NVarChar, 50) { Value = (object)invoice.InvoiceDate ?? DBNull.Value });

                // Consignee
                cmd.Parameters.Add(new SqlParameter("@ConsigneeName", SqlDbType.NVarChar, 200) { Value = (object)invoice.ConsigneeName ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@ConsigneeAddress", SqlDbType.NVarChar, -1) { Value = (object)invoice.ConsigneeAddress ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@ConsigneeGstin", SqlDbType.NVarChar, 50) { Value = (object)invoice.ConsigneeGstin ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@ConsigneePhone", SqlDbType.NVarChar, 50) { Value = (object)invoice.ConsigneePhone ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@ConsigneeState", SqlDbType.NVarChar, 100) { Value = (object)invoice.ConsigneeState ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@ConsigneeStateCode", SqlDbType.NVarChar, 20) { Value = (object)invoice.ConsigneeStateCode ?? DBNull.Value });

                // Buyer
                cmd.Parameters.Add(new SqlParameter("@SameAsConsignee", SqlDbType.Bit) { Value = invoice.SameAsConsignee });
                cmd.Parameters.Add(new SqlParameter("@BuyerName", SqlDbType.NVarChar, 200) { Value = (object)invoice.BuyerName ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@BuyerAddress", SqlDbType.NVarChar, -1) { Value = (object)invoice.BuyerAddress ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@BuyerGstin", SqlDbType.NVarChar, 50) { Value = (object)invoice.BuyerGstin ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@BuyerPhone", SqlDbType.NVarChar, 50) { Value = (object)invoice.BuyerPhone ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@BuyerState", SqlDbType.NVarChar, 100) { Value = (object)invoice.BuyerState ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@BuyerStateCode", SqlDbType.NVarChar, 20) { Value = (object)invoice.BuyerStateCode ?? DBNull.Value });

                // Details
                cmd.Parameters.Add(new SqlParameter("@DeliveryNote", SqlDbType.NVarChar, 500) { Value = (object)invoice.DeliveryNote ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@PaymentTerms", SqlDbType.NVarChar, 200) { Value = (object)invoice.PaymentTerms ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@BuyerOrderNumber", SqlDbType.NVarChar, 200) { Value = (object)invoice.BuyerOrderNumber ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@BuyerOrderDate", SqlDbType.NVarChar, 50) { Value = (object)invoice.BuyerOrderDate ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@DispatchDocumentNumber", SqlDbType.NVarChar, 200) { Value = (object)invoice.DispatchDocumentNumber ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@DispatchNoteDate", SqlDbType.NVarChar, 50) { Value = (object)invoice.DispatchNoteDate ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@DispatchedThrough", SqlDbType.NVarChar, 200) { Value = (object)invoice.DispatchedThrough ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@Destination", SqlDbType.NVarChar, 200) { Value = (object)invoice.Destination ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@VehicleNumber", SqlDbType.NVarChar, 100) { Value = (object)invoice.VehicleNumber ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@LrRrNumber", SqlDbType.NVarChar, 100) { Value = (object)invoice.LrRrNumber ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@TermsOfDelivery", SqlDbType.NVarChar, 500) { Value = (object)invoice.TermsOfDelivery ?? DBNull.Value });

                // New fields / tax percentages
                cmd.Parameters.Add(new SqlParameter("@Urn", SqlDbType.Bit) { Value = (object)invoice.Urn ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@WeightmentNo", SqlDbType.NVarChar, 100) { Value = (object)invoice.WeightmentNo ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@CgstPer", SqlDbType.Decimal) { Precision = 5, Scale = 2, Value = (object)invoice.CgstPer ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@SgstPer", SqlDbType.Decimal) { Precision = 5, Scale = 2, Value = (object)invoice.SgstPer ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@IgstPer", SqlDbType.Decimal) { Precision = 5, Scale = 2, Value = (object)invoice.IgstPer ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@TaxPer", SqlDbType.Decimal) { Precision = 5, Scale = 2, Value = (object)invoice.TaxPer ?? DBNull.Value });

                // Totals
                cmd.Parameters.Add(new SqlParameter("@SubTotal", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = invoice.SubTotal });
                cmd.Parameters.Add(new SqlParameter("@TotalCgst", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = invoice.TotalCgst });
                cmd.Parameters.Add(new SqlParameter("@TotalSgst", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = invoice.TotalSgst });
                cmd.Parameters.Add(new SqlParameter("@TotalIgst", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = invoice.TotalIgst });
                cmd.Parameters.Add(new SqlParameter("@GrandTotal", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = invoice.GrandTotal });

                // CreatedBy
                cmd.Parameters.Add(new SqlParameter("@CreatedBy", SqlDbType.Int) { Value = (object)createdBy ?? DBNull.Value });

                // Prepare items as a Table-Valued Parameter. Assumes a user-defined table type dbo.InvoiceItemType exists.
                var itemsTable = CreateInvoiceItemsDataTable(invoice.Items);
                var itemsParam = new SqlParameter("@InvoiceItems", SqlDbType.Structured)
                {
                    TypeName = "dbo.InvoiceItemType",
                    Value = itemsTable
                };
                cmd.Parameters.Add(itemsParam);

                // Output parameter for result message (similar to AddUpdateUserMaster)
                var outParam = new SqlParameter("@Result", SqlDbType.NVarChar, 1000)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(outParam);

                // Execute stored procedure
                var rowsAffected = await cmd.ExecuteNonQueryAsync();

                var resultMessage = outParam.Value as string ?? string.Empty;
                var affected = rowsAffected > 0 ? rowsAffected : 1;

                return new InvoiceResult
                {
                    Affected = affected,
                    Message = string.IsNullOrWhiteSpace(resultMessage) ? "Invoice persisted." : resultMessage,
                    InvoiceId = "",
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (SqlException sqlEx)
            {
                // If stored procedure isn't present or DB schema differs, log and fallback to inserting into a simple table named InvoiceJson (if exists).
                _logger.LogWarning(sqlEx, "Stored procedure '{SpName}' failed. Attempting fallback JSON insert.", "");
                return new InvoiceResult
                {
                    Affected = 0,
                    Message = "",
                    InvoiceId = "",
                    CreatedAt = DateTime.UtcNow
                };
            }
        }

        // Helper to create a DataTable matching the expected TVP (dbo.InvoiceItemType).
        private static DataTable CreateInvoiceItemsDataTable(List<InvoiceItem> items)
        {
            var table = new DataTable();
            // Define columns — these should match your SQL user-defined table type.
            table.Columns.Add("Id", typeof(string));
            table.Columns.Add("ProductId", typeof(string));
            table.Columns.Add("ProductName", typeof(string));
            table.Columns.Add("HsnCode", typeof(string));
            table.Columns.Add("Quantity", typeof(decimal));
            table.Columns.Add("Rate", typeof(decimal));
            table.Columns.Add("Unit", typeof(string));
            table.Columns.Add("DiscountPercentage", typeof(decimal));
            table.Columns.Add("Amount", typeof(decimal));
            table.Columns.Add("TaxableValue", typeof(decimal));
            table.Columns.Add("Cgst", typeof(decimal));
            table.Columns.Add("Sgst", typeof(decimal));
            table.Columns.Add("Igst", typeof(decimal));
            table.Columns.Add("Total", typeof(decimal));

            if (items == null) return table;

            foreach (var it in items)
            {
                var row = table.NewRow();
                row["Id"] = it.Id ?? string.Empty;
                row["ProductId"] = it.ProductId ?? string.Empty;
                row["ProductName"] = it.ProductName ?? string.Empty;
                row["HsnCode"] = it.HsnCode ?? string.Empty;
                row["Quantity"] = it.Quantity;
                row["Rate"] = it.Rate;
                row["Unit"] = it.Unit ?? string.Empty;
                row["DiscountPercentage"] = it.DiscountPercentage;
                row["Amount"] = it.Amount;
                row["TaxableValue"] = it.TaxableValue;
                row["Cgst"] = it.Cgst;
                row["Sgst"] = it.Sgst;
                row["Igst"] = it.Igst;
                row["Total"] = it.Total;
                table.Rows.Add(row);
            }

            return table;
        }
    }
}