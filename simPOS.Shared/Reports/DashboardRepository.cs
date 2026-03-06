using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using simPOS.Shared.Database;

namespace simPOS.Shared.Reports
{
    public class DashboardRepository
    {
        // ── Ringkasan hari ini ───────────────────────────────────────

        public (int TotalTrx, int TotalQty, decimal TotalOmzet, decimal TotalLaba)
            GetTodaySummary()
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    COUNT(DISTINCT t.id),
                    COALESCE(SUM(ti.quantity), 0),
                    COALESCE(SUM(ti.subtotal), 0),
                    COALESCE(SUM(ti.quantity * (ti.sell_price - p.buy_price)), 0)
                FROM transactions t
                JOIN transaction_items ti ON ti.transaction_id = t.id
                JOIN products p           ON p.id = ti.product_id
                WHERE date(t.created_at) = date('now','localtime')";

            using var r = cmd.ExecuteReader();
            if (r.Read())
                return (r.GetInt32(0), r.GetInt32(1), r.GetDecimal(2), r.GetDecimal(3));
            return (0, 0, 0, 0);
        }

        // ── Grafik omzet N hari terakhir ────────────────────────────

        public List<(string Date, decimal Omzet, decimal Laba)>
            GetOmzetTrend(int days)
        {
            var list = new List<(string, decimal, decimal)>();
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    strftime('%Y-%m-%d', t.created_at)                              AS dt,
                    COALESCE(SUM(ti.subtotal), 0)                                   AS omzet,
                    COALESCE(SUM(ti.quantity*(ti.sell_price - p.buy_price)), 0)     AS laba
                FROM transactions t
                JOIN transaction_items ti ON ti.transaction_id = t.id
                JOIN products p           ON p.id = ti.product_id
                WHERE date(t.created_at) >= date('now','localtime',@offset)
                GROUP BY dt
                ORDER BY dt ASC";
            cmd.Parameters.AddWithValue("@offset", $"-{days - 1} days");

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add((r.GetString(0), r.GetDecimal(1), r.GetDecimal(2)));
            return list;
        }

        // ── Produk terlaris ─────────────────────────────────────────

        public List<(string Code, string Name, int Qty, decimal Omzet)>
            GetTopProducts(int topN, bool todayOnly)
        {
            var list = new List<(string, string, int, decimal)>();
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();

            var where = todayOnly
                ? "WHERE date(t.created_at) = date('now','localtime')"
                : $"WHERE date(t.created_at) >= date('now','localtime','-29 days')";

            cmd.CommandText = $@"
                SELECT
                    ti.product_code,
                    ti.product_name,
                    SUM(ti.quantity)  AS total_qty,
                    SUM(ti.subtotal)  AS total_omzet
                FROM transaction_items ti
                JOIN transactions t ON t.id = ti.transaction_id
                {where}
                GROUP BY ti.product_id
                ORDER BY total_qty DESC
                LIMIT @topN";
            cmd.Parameters.AddWithValue("@topN", topN);

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add((r.GetString(0), r.GetString(1),
                          r.GetInt32(2), r.GetDecimal(3)));
            return list;
        }

        // ── Stok menipis ────────────────────────────────────────────

        public List<(string Code, string Name, int Stock, int MinStock, string Unit)>
            GetLowStockProducts()
        {
            var list = new List<(string, string, int, int, string)>();
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT code, name, stock, min_stock, unit
                FROM products
                WHERE is_active = 1
                  AND min_stock > 0
                  AND stock <= min_stock
                ORDER BY (stock * 1.0 / NULLIF(min_stock,0)) ASC, name ASC";

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add((r.GetString(0), r.GetString(1),
                          r.GetInt32(2), r.GetInt32(3), r.GetString(4)));
            return list;
        }

        // ── Jumlah total produk & kategori aktif ────────────────────

        public (int Products, int Categories, int Suppliers) GetMasterCount()
        {
            using var conn = DatabaseHelper.GetConnection();
            int prod = 0, cat = 0, sup = 0;

            foreach (var (table, isActive) in new[]
            {
                ("products",   "WHERE is_active=1"),
                ("categories", ""),
                ("suppliers",  "")
            })
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT COUNT(*) FROM {table} {isActive}";
                var val = cmd.ExecuteScalar();
                var n = val == null || val == DBNull.Value ? 0 : Convert.ToInt32(val);
                if (table == "products") prod = n;
                if (table == "categories") cat = n;
                if (table == "suppliers") sup = n;
            }
            return (prod, cat, sup);
        }
    }
}
