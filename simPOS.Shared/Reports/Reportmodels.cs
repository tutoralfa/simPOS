using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simPOS.Shared.Reports
{
    // ── Laporan Penjualan ────────────────────────────────────────────

    /// <summary>Ringkasan penjualan per hari.</summary>
    public class SalesSummaryRow
    {
        public string Date { get; set; }
        public int TotalTrx { get; set; }
        public int TotalQty { get; set; }
        public decimal TotalOmzet { get; set; }
        public decimal AvgTrx => TotalTrx > 0 ? TotalOmzet / TotalTrx : 0;
    }

    /// <summary>Header satu transaksi untuk detail list.</summary>
    public class SalesTrxRow
    {
        public int Id { get; set; }
        public string InvoiceNo { get; set; }
        public string CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ChangeAmount { get; set; }
        public string PaymentMethod { get; set; }
        public int ItemCount { get; set; }
        public List<SalesTrxItemRow> Items { get; set; } = new List<SalesTrxItemRow>();
    }

    /// <summary>Satu baris item dalam transaksi.</summary>
    public class SalesTrxItemRow
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string Unit { get; set; }
        public int Quantity { get; set; }
        public decimal SellPrice { get; set; }
        public decimal Subtotal { get; set; }
    }

    /// <summary>Parameter filter laporan penjualan.</summary>
    public class SalesReportFilter
    {
        public DateTime DateFrom { get; set; } = DateTime.Today;
        public DateTime DateTo { get; set; } = DateTime.Today;
        public string GroupBy { get; set; } = "DAY"; // DAY | MONTH
    }

    // ── Kartu Stok ───────────────────────────────────────────────────

    /// <summary>Satu baris mutasi di kartu stok.</summary>
    public class StockCardRow
    {
        public string Date { get; set; }
        public string Type { get; set; }  // IN | OUT | ADJUSTMENT
        public string TypeLabel => Type == "IN" ? "Masuk"
                                       : Type == "OUT" ? "Keluar"
                                       : Type == "ADJUSTMENT" ? "Opname"
                                       : Type;
        public int QtyIn { get; set; }
        public int QtyOut { get; set; }
        public int QtyAdj { get; set; }
        public int StockAfter { get; set; }
        public string Reference { get; set; }
        public string Notes { get; set; }
    }

    /// <summary>Parameter filter kartu stok.</summary>
    public class StockCardFilter
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string Unit { get; set; }
        public int StockAwal { get; set; }  // stok sebelum periode
        public DateTime DateFrom { get; set; } = DateTime.Today.AddMonths(-1);
        public DateTime DateTo { get; set; } = DateTime.Today;
    }
}
