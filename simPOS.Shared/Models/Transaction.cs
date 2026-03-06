using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simPOS.Shared.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public string InvoiceNo { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ChangeAmount { get; set; }
        public string PaymentMethod { get; set; } = "CASH";
        public string Notes { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;

        public List<TransactionItem> Items { get; set; } = new List<TransactionItem>();
    }

    public class TransactionItem
    {
        public int Id { get; set; }
        public int TransactionId { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal SellPrice { get; set; }
        public decimal Subtotal => Quantity * SellPrice;
    }
}
