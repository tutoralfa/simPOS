using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simPOS.Shared.Reports
{
    public class DashboardService
    {
        private readonly DashboardRepository _repo = new DashboardRepository();

        public (int TotalTrx, int TotalQty, decimal TotalOmzet, decimal TotalLaba)
            GetTodaySummary() => _repo.GetTodaySummary();

        public List<(string Date, decimal Omzet, decimal Laba)>
            GetOmzetTrend(int days) => _repo.GetOmzetTrend(days);

        public List<(string Code, string Name, int Qty, decimal Omzet)>
            GetTopProducts(int topN, bool todayOnly) => _repo.GetTopProducts(topN, todayOnly);

        public List<(string Code, string Name, int Stock, int MinStock, string Unit)>
            GetLowStock() => _repo.GetLowStockProducts();

        public (int Products, int Categories, int Suppliers)
            GetMasterCount() => _repo.GetMasterCount();
    }
}
