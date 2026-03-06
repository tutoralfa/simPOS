using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simPOS.Shared.Reports
{
    /// <summary>
    /// Satu baris ringkasan laba/rugi per periode (hari atau bulan).
    /// </summary>
    public class ProfitLossRow
    {
        public string Period { get; set; }  // "2025-01-15" atau "2025-01"
        public int TotalTrx { get; set; }
        public int TotalQty { get; set; }

        public decimal Pendapatan { get; set; }  // total sell_price × qty
        public decimal HPP { get; set; }  // total buy_price × qty
        public decimal LabaKotor => Pendapatan - HPP;
        public decimal MarginPct => Pendapatan > 0
                                        ? Math.Round(LabaKotor / Pendapatan * 100, 1)
                                        : 0;
    }

    /// <summary>
    /// Grand total seluruh periode untuk footer laporan.
    /// </summary>
    public class ProfitLossSummary
    {
        public decimal TotalPendapatan { get; set; }
        public decimal TotalHPP { get; set; }
        public decimal TotalLabaKotor => TotalPendapatan - TotalHPP;
        public decimal MarginPct => TotalPendapatan > 0
                                          ? Math.Round(TotalLabaKotor / TotalPendapatan * 100, 1)
                                          : 0;
        public int TotalTrx { get; set; }
        public int TotalQty { get; set; }
    }

    /// <summary>Filter periode untuk laporan laba/rugi.</summary>
    public class ProfitLossFilter
    {
        public DateTime DateFrom { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        public DateTime DateTo { get; set; } = DateTime.Today;
        public string GroupBy { get; set; } = "DAY"; // DAY | MONTH
    }
}
