namespace InvoiceManagementApi.Models
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
    }
}