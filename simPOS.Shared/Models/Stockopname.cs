using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simPOS.Shared.Models
{
    /// <summary>
    /// Header sesi stock opname.
    /// Status DRAFT = sedang diisi, CONFIRMED = sudah dikonfirmasi & stok diupdate.
    /// </summary>
    public class StockOpname
    {
        public int Id { get; set; }
        public string OpnameNo { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Status { get; set; } = "DRAFT";
        public string ConfirmedAt { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;

        public List<StockOpnameItem> Items { get; set; } = new List<StockOpnameItem>();

        public bool IsConfirmed => Status == "CONFIRMED";
        public int TotalItems => Items.Count;
        public int TotalAdjusted => Items.FindAll(i => i.Difference != 0).Count;
    }

    /// <summary>
    /// Satu baris barang dalam sesi opname.
    /// SystemStock  = stok menurut database saat opname dibuat.
    /// PhysicalStock = stok hasil hitung fisik oleh user.
    /// Difference   = PhysicalStock - SystemStock (bisa negatif).
    /// </summary>
    public class StockOpnameItem
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int SystemStock { get; set; }   // stok di sistem saat snapshot
        public int PhysicalStock { get; set; }   // stok fisik yang dihitung user
        public int Difference => PhysicalStock - SystemStock;
        public string DifferenceDisplay => Difference == 0 ? "-"
            : Difference > 0 ? $"+{Difference}"
            : $"{Difference}";
    }
}
