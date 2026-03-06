using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using simPOS.Shared.Repositories;

namespace simPOS.Shared.Reports
{
    public class ReportService
    {
        private readonly ReportRepository _repo = new ReportRepository();
        private readonly ProductRepository _productRepo = new ProductRepository();

        // ── Penjualan ────────────────────────────────────────────────

        public List<SalesSummaryRow> GetSalesSummary(SalesReportFilter f)
            => _repo.GetSalesSummary(f);

        public List<SalesTrxRow> GetTransactions(SalesReportFilter f)
            => _repo.GetTransactions(f);

        public List<SalesTrxItemRow> GetTransactionItems(int trxId)
            => _repo.GetTransactionItems(trxId);

        /// <summary>Grand total omzet untuk periode yang dipilih.</summary>
        public (int TotalTrx, int TotalQty, decimal TotalOmzet) GetGrandTotal(SalesReportFilter f)
        {
            var rows = _repo.GetSalesSummary(f);
            return (
                rows.Sum(r => r.TotalTrx),
                rows.Sum(r => r.TotalQty),
                rows.Sum(r => r.TotalOmzet)
            );
        }

        // ── Kartu Stok ───────────────────────────────────────────────

        public (bool Found, StockCardFilter Filter) PrepareStockCard(int productId, DateTime from, DateTime to)
        {
            var product = _productRepo.GetById(productId);
            if (product == null) return (false, null);

            var filter = new StockCardFilter
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name,
                Unit = product.Unit,
                DateFrom = from,
                DateTo = to,
                StockAwal = _repo.GetStockBefore(product.Id, from)
            };
            return (true, filter);
        }

        public List<StockCardRow> GetStockCard(StockCardFilter f)
            => _repo.GetStockMovements(f);

        public List<simPOS.Shared.Models.Product> GetAllProducts()
            => _productRepo.GetAll();
    }
}
