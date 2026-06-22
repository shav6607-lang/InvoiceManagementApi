using System;

namespace InvoiceManagementApi.Models
{
    public class InvoiceResult
    {
        public int Affected { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? InvoiceId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}