using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using InvoiceManagementApi.Models;
using InvoiceManagementApi.Utilities;
using System;

namespace InvoiceManagementApi.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly string _spName;

        public AuthRepository(IConfiguration configuration)
        {
            _configuration = configuration;

            _connectionString = _configuration.GetConnectionString("DefaultConnection");

            // If connection string is encrypted in configuration, attempt to decrypt it.
            try
            {
                var decrypted = Utility.ConnectionStringDecrypt(_connectionString);
                if (!string.IsNullOrWhiteSpace(decrypted))
                {
                    _connectionString = decrypted;
                }
            }
            catch
            {
                // If decryption fails, fall back to original connection string
            }

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException("Database connection string is not configured. Please set 'ConnectionStrings:DefaultConnection' (or 'ConnectionStrings:TCA') in appsettings.json or environment variables.");
            }

            _spName = _configuration["Auth:SpName"] ?? "USP_AuthenticateUser"; 
        }
        public async Task<AuthenticateResult> AuthenticateAsync(string username, string password)
        {
            using var conn = new SqlConnection(_connectionString);

            const string sql = @"
        -- User Details
        SELECT
            U.UserId,
            U.UserName,
            U.DisplayName,
            U.RoleId,
            R.Name AS RoleName
        FROM UserMaster U
        INNER JOIN RoleMaster R ON R.RoleId = U.RoleId
        WHERE U.UserName = @UserName
          AND U.PassWord = @Password
          AND U.IsActive = 1;

        -- Company Details
        SELECT
            UCM.UserId,
            UCM.CompanyId,
            CM.Name AS CompanyName,
            CM.GSTNO,
            CM.State,
            CM.StateCode,
            CM.EMail,
            CM.Phone
        FROM UserCompanyMapping UCM
        INNER JOIN CompanyMaster CM
            ON CM.CompanyId = UCM.CompanyId
        INNER JOIN UserMaster U
            ON U.UserId = UCM.UserId
        WHERE U.UserName = @UserName
          AND U.PassWord = @Password
          AND UCM.IsActive = 1
          AND CM.IsActive = 1;
    ";

            var result = new AuthenticateResult();

            using var multi = await conn.QueryMultipleAsync(
                sql,
                new
                {
                    UserName = username,
                    Password = password
                });

            var user = await multi.ReadFirstOrDefaultAsync<UserDto>();

            if (user == null)
            {
                return new AuthenticateResult
                {
                    IsSuccess = false,
                    Message = "Invalid username or password."
                };
            }

            var companies = (await multi.ReadAsync<CompanyDto>()).ToList();

            return new AuthenticateResult
            {
                IsSuccess = true,
                Message = "Login successful",
                User = user,
                Companies = companies
            };
        }
    }
}