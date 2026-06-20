namespace InvoiceManagementApi.Models
{
    public class MaterialMaster
    {
        public int MaterialId { get; set; }
        public int CompanyId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public decimal RatePerUnit { get; set; }
        public int MaterialType { get; set; }
    }
}
