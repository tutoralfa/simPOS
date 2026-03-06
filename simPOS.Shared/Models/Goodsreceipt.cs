using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simPOS.Shared.Models
{
    public class GoodsReceipt
    {
        public int Id { get; set; }
        public string ReceiptNo { get; set; } = string.Empty;
        public int? SupplierId { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string ReceivedAt { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;

        // Join
        public string SupplierName { get; set; } = "-";

        // Line items — diisi saat tampil detail
        public List<GoodsReceiptItem> Items { get; set; } = new List<GoodsReceiptItem>();

        // Computed
        public int TotalItems => Items.Count;
        public decimal TotalValue => Items.Count > 0
            ? Items.FindAll(i => true).ConvertAll(i => i.Subtotal)
                   .Aggregate(0m, (a, b) => a + b)
            : 0;
    }

    /// <summary>
    /// Satu baris barang dalam dokumen penerimaan.
    /// Ini yang menjadi satu record di stock_movements (type=IN).
    /// </summary>
    public class GoodsReceiptItem
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal Subtotal => Quantity * BuyPrice;
    }
}
