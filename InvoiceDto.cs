using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InvoiceManagementApi.Models
{
    public class InvoiceItem
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

        // Amount = Rate * Quantity (gross)
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        // TaxableValue = Amount - DiscountAmount
        [Range(0, double.MaxValue)]
        public decimal TaxableValue { get; set; }

        // Per-item tax amounts (monetary values)
        [Range(0, double.MaxValue)]
        public decimal Cgst { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Sgst { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Igst { get; set; }

        // Total for the line (TaxableValue + taxes)
        [Range(0, double.MaxValue)]
        public decimal Total { get; set; }
    }

    public class Invoice
    {
        [Required]
        public int?  Id { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;
        public string? InvoiceDate { get; set; }

        // Consignee
        public string ConsigneeName { get; set; } = string.Empty;
        public string ConsigneeAddress { get; set; } = string.Empty;
        public string ConsigneeGstin { get; set; } = string.Empty;
        public string ConsigneePhone { get; set; } = string.Empty;
        public string ConsigneeState { get; set; } = string.Empty;
        public string? ConsigneeStateCode { get; set; }

        // Buyer
        public bool SameAsConsignee { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public string BuyerAddress { get; set; } = string.Empty;
        public string BuyerGstin { get; set; } = string.Empty;
        public string BuyerPhone { get; set; } = string.Empty;
        public string BuyerState { get; set; } = string.Empty;
        public string? BuyerStateCode { get; set; }

        // Details
        public string DeliveryNote { get; set; } = string.Empty;
        public string PaymentTerms { get; set; } = string.Empty;
        public string BuyerOrderNumber { get; set; } = string.Empty;
        public string BuyerOrderDate { get; set; } = string.Empty;
        public string DispatchDocumentNumber { get; set; } = string.Empty;
        public string DispatchNoteDate { get; set; } = string.Empty;
        public string DispatchedThrough { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public string LrRrNumber { get; set; } = string.Empty;
        public string TermsOfDelivery { get; set; } = string.Empty;

        // New fields
        public bool? Urn { get; set; }
        public string? WeightmentNo { get; set; }
        public decimal? CgstPer { get; set; }
        public decimal? SgstPer { get; set; }
        public decimal? IgstPer { get; set; }
        public decimal? TaxPer { get; set; }

        // Goods
        public List<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();

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