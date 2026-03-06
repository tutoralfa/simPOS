using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using simPOS.Shared.Database;

namespace simPOS.Shared.Reports
{
    public class ProfitLossRepository
    {
        /// <summary>
        /// Query laba/rugi per hari atau per bulan.
        ///
        /// HPP dihitung dari products.buy_price (harga beli master produk saat ini).
        /// JOIN ke products untuk ambil buy_price per product_id.
        /// </summary>
        public List<ProfitLossRow> GetProfitLoss(ProfitLossFilter f)
        {
            var list = new List<ProfitLossRow>();

            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();

            var dateFmt = f.GroupBy == "MONTH"
                ? "strftime('%Y-%m', t.created_at)"
                : "strftime('%Y-%m-%d', t.created_at)";

            cmd.CommandText = $@"
                SELECT
                    {dateFmt}                                        AS period,
                    COUNT(DISTINCT t.id)                             AS total_trx,
                    COALESCE(SUM(ti.quantity), 0)                    AS total_qty,
                    COALESCE(SUM(ti.subtotal), 0)                    AS pendapatan,
                    COALESCE(SUM(ti.quantity * p.buy_price), 0)      AS hpp
                FROM transactions t
                JOIN transaction_items ti ON ti.transaction_id = t.id
                JOIN products p           ON p.id = ti.product_id
                WHERE date(t.created_at) BETWEEN @from AND @to
                GROUP BY {dateFmt}
                ORDER BY period ASC";

            cmd.Parameters.AddWithValue("@from", f.DateFrom.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@to", f.DateTo.ToString("yyyy-MM-dd"));

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new ProfitLossRow
                {
                    Period = r.GetString(0),
                    TotalTrx = r.GetInt32(1),
                    TotalQty = r.GetInt32(2),
                    Pendapatan = r.GetDecimal(3),
                    HPP = r.GetDecimal(4)
                });

            return list;
        }

        /// <summary>Grand total seluruh periode.</summary>
        public ProfitLossSummary GetSummary(ProfitLossFilter f)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT
                    COUNT(DISTINCT t.id)                        AS total_trx,
                    COALESCE(SUM(ti.quantity), 0)               AS total_qty,
                    COALESCE(SUM(ti.subtotal), 0)               AS pendapatan,
                    COALESCE(SUM(ti.quantity * p.buy_price), 0) AS hpp
                FROM transactions t
                JOIN transaction_items ti ON ti.transaction_id = t.id
                JOIN products p           ON p.id = ti.product_id
                WHERE date(t.created_at) BETWEEN @from AND @to";

            cmd.Parameters.AddWithValue("@from", f.DateFrom.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@to", f.DateTo.ToString("yyyy-MM-dd"));

            using var rd = cmd.ExecuteReader();
            if (rd.Read())
                return new ProfitLossSummary
                {
                    TotalTrx = rd.GetInt32(0),
                    TotalQty = rd.GetInt32(1),
                    TotalPendapatan = rd.GetDecimal(2),
                    TotalHPP = rd.GetDecimal(3)
                };

            return new ProfitLossSummary();
        }
    }
}
