using System.Collections.Generic;

namespace InvoiceManagementApi.Models
{
    public class AuthenticateResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserDto? User { get; set; }
        public List<CompanyDto>? Companies { get; set; }
    }
}