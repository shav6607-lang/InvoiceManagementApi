namespace InvoiceManagementApi.Models
{
    public class CompanyMaster
    {
        public int CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string GSTNO { get; set; } = string.Empty;
        public string GSTUINO { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string StateCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string EMail { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string BankAddress { get; set; } = string.Empty;
        public string AccNo { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string ISFC { get; set; } = string.Empty;
        public string HSNCode { get; set; } = string.Empty;
        public string InvoiceNum { get; set; } = string.Empty;
        public string DCNum { get; set; } = string.Empty;
    }
}
