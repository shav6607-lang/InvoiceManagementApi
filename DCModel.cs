using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InvoiceManagementApi.Models
{
    public class DCItem
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required]
        public string ProductId { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;
        public string HsnCode { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Rate { get; set; }

        public string Unit { get; set; } = string.Empty;

        [Range(0, 100)]
        public decimal DiscountPercentage { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TaxableValue { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Cgst { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Sgst { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Igst { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Total { get; set; }
    }

    public class DCModel
    {
        [Required]
        public int? Id { get; set; }

        public string DCNumber { get; set; } = string.Empty;
        public string? DCDate { get; set; }
        public string VehicleNumber { get; set; } = string.Empty;
        public string? WeightmentNo { get; set; }
        public decimal? CgstPer { get; set; }
        public decimal? SgstPer { get; set; }
        public decimal? IgstPer { get; set; }
        public decimal? TaxPer { get; set; }

        // Goods
        public List<DCItem> Items { get; set; } = new List<DCItem>();

        // Totals
        [Range(0, double.MaxValue)]
        public decimal SubTotal { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TotalCgst { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TotalSgst { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TotalIgst { get; set; }

        [Range(0, double.MaxValue)]
        public decimal GrandTotal { get; set; }
    }
}