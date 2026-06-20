namespace InvoiceManagementApi.Models
{
    public class MaterialRequest
    {
        public int? MaterialId { get; set; }
        public int CompanyId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public decimal RatePerUnit { get; set; }
        public bool IsActive { get; set; } = true;
        public int? MaterialType { get; set; }
        public int? UserId { get; set; }
    }
}
