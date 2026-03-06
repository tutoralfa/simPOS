using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using simPOS.Shared.Database;

namespace simPOS.Shared.Reports
{
    public class ReportRepository
    {
        // ── Laporan Penjualan ────────────────────────────────────────

        /// <summary>Ringkasan penjualan digroup per hari atau per bulan.</summary>
        public List<SalesSummaryRow> GetSalesSummary(SalesReportFilter f)
        {
            var list = new List<SalesSummaryRow>();
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();

            var dateFmt = f.GroupBy == "MONTH"
                ? "strftime('%Y-%m', t.created_at)"
                : "strftime('%Y-%m-%d', t.created_at)";

            cmd.CommandText = $@"
                SELECT
                    {dateFmt}                       AS date,
                    COUNT(DISTINCT t.id)            AS total_trx,
                    COALESCE(SUM(ti.quantity), 0)   AS total_qty,
                    COALESCE(SUM(t.total_amount),0) AS total_omzet
                FROM transactions t
                LEFT JOIN transaction_items ti ON ti.transaction_id = t.id
                WHERE date(t.created_at) BETWEEN @from AND @to
                GROUP BY {dateFmt}
                ORDER BY date ASC";

            cmd.Parameters.AddWithValue("@from", f.DateFrom.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@to", f.DateTo.ToString("yyyy-MM-dd"));

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new SalesSummaryRow
                {
                    Date = r.GetString(0),
                    TotalTrx = r.GetInt32(1),
                    TotalQty = r.GetInt32(2),
                    TotalOmzet = r.GetDecimal(3)
                });

            return list;
        }

        /// <summary>List semua transaksi (header) dalam periode.</summary>
        public List<SalesTrxRow> GetTransactions(SalesReportFilter f)
        {
            var list = new List<SalesTrxRow>();
            using var conn = DatabaseHelper.GetConnection();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT t.id, t.invoice_no, t.created_at,
                           t.total_amount, t.paid_amount, t.change_amount,
                           t.payment_method,
                           COUNT(ti.id) AS item_count
                    FROM transactions t
                    LEFT JOIN transaction_items ti ON ti.transaction_id = t.id
                    WHERE date(t.created_at) BETWEEN @from AND @to
                    GROUP BY t.id
                    ORDER BY t.created_at DESC";
                cmd.Parameters.AddWithValue("@from", f.DateFrom.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@to", f.DateTo.ToString("yyyy-MM-dd"));

                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new SalesTrxRow
                    {
                        Id = r.GetInt32(0),
                        InvoiceNo = r.GetString(1),
                        CreatedAt = r.GetString(2),
                        TotalAmount = r.GetDecimal(3),
                        PaidAmount = r.GetDecimal(4),
                        ChangeAmount = r.GetDecimal(5),
                        PaymentMethod = r.GetString(6),
                        ItemCount = r.GetInt32(7)
                    });
            }

            return list;
        }

        /// <summary>Items untuk satu transaksi (dipakai saat expand detail).</summary>
        public List<SalesTrxItemRow> GetTransactionItems(int transactionId)
        {
            var list = new List<SalesTrxItemRow>();
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT product_code, product_name, unit, quantity, sell_price, subtotal
                FROM transaction_items
                WHERE transaction_id = @id
                ORDER BY id";
            cmd.Parameters.AddWithValue("@id", transactionId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new SalesTrxItemRow
                {
                    ProductCode = r.GetString(0),
                    ProductName = r.GetString(1),
                    Unit = r.GetString(2),
                    Quantity = r.GetInt32(3),
                    SellPrice = r.GetDecimal(4),
                    Subtotal = r.GetDecimal(5)
                });

            return list;
        }

        // ── Kartu Stok ───────────────────────────────────────────────

        /// <summary>
        /// Hitung stok awal produk (sebelum DateFrom) berdasarkan semua mutasi historis.
        /// </summary>
        public int GetStockBefore(int productId, DateTime before)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            // Stok awal = semua IN + ADJUSTMENT positif - semua OUT - ADJUSTMENT negatif
            // sebelum tanggal dari
            cmd.CommandText = @"
                SELECT COALESCE(SUM(
                    CASE
                        WHEN type IN ('IN') THEN quantity
                        WHEN type = 'ADJUSTMENT' THEN quantity
                        WHEN type = 'OUT' THEN -quantity
                        ELSE 0
                    END
                ), 0)
                FROM stock_movements
                WHERE product_id = @pid
                  AND date(created_at) < @before";
            cmd.Parameters.AddWithValue("@pid", productId);
            cmd.Parameters.AddWithValue("@before", before.ToString("yyyy-MM-dd"));

            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? 0 : Convert.ToInt32(result);
        }

        /// <summary>Semua mutasi stok produk dalam periode.</summary>
        public List<StockCardRow> GetStockMovements(StockCardFilter f)
        {
            var list = new List<StockCardRow>();
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT
                    sm.created_at,
                    sm.type,
                    sm.quantity,
                    sm.reference_no,
                    sm.notes
                FROM stock_movements sm
                WHERE sm.product_id = @pid
                  AND date(sm.created_at) BETWEEN @from AND @to
                ORDER BY sm.created_at ASC";

            cmd.Parameters.AddWithValue("@pid", f.ProductId);
            cmd.Parameters.AddWithValue("@from", f.DateFrom.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@to", f.DateTo.ToString("yyyy-MM-dd"));

            int running = f.StockAwal;

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var type = r.GetString(1);
                var qty = r.GetInt32(2);

                int qIn = 0, qOut = 0, qAdj = 0;
                if (type == "IN") { qIn = qty; running += qty; }
                else if (type == "OUT") { qOut = qty; running -= qty; }
                else if (type == "ADJUSTMENT") { qAdj = qty; running += qty; }

                list.Add(new StockCardRow
                {
                    Date = r.GetString(0),
                    Type = type,
                    QtyIn = qIn,
                    QtyOut = qOut,
                    QtyAdj = qAdj,
                    StockAfter = running,
                    Reference = r.IsDBNull(3) ? "" : r.GetString(3),
                    Notes = r.IsDBNull(4) ? "" : r.GetString(4)
                });
            }

            return list;
        }
    }
}
