using simPOS.Shared.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using simPOS.Shared.Database;

namespace simPOS.Shared.Services
{
    // ── Model ────────────────────────────────────────────────────────

    public class EodSummary
    {
        public string SessionDate { get; set; }
        public int TotalTrx { get; set; }
        public int TotalQty { get; set; }
        public decimal TotalOmzet { get; set; }
        public decimal TotalHpp { get; set; }
        public decimal TotalLaba => TotalOmzet - TotalHpp;
        public decimal MarginPct => TotalOmzet > 0
            ? Math.Round(TotalLaba / TotalOmzet * 100, 1) : 0;
        public List<EodItemRow> Items { get; set; } = new();
    }

    public class EodItemRow
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string Unit { get; set; }
        public int TotalQty { get; set; }
        public decimal TotalOmzet { get; set; }
        public decimal TotalHpp { get; set; }
        public decimal TotalLaba => TotalOmzet - TotalHpp;
    }

    // ── Service ──────────────────────────────────────────────────────

    public class EodService
    {
        private readonly ClerkService _clerk = new ClerkService();

        /// <summary>
        /// Ambil ringkasan penjualan hari ini untuk preview EOD.
        /// </summary>
        // [BARU] Versi dengan tanggal eksplisit — dipakai oleh FormEod(date)
        public EodSummary GetSummaryByDate(string date) => GetSummaryInternal(date);
        public EodSummary GetTodaySummary() => GetSummaryInternal(DateTime.Today.ToString("yyyy-MM-dd"));

        private EodSummary GetSummaryInternal(string date)
        {
            var today = date;
            var summary = new EodSummary { SessionDate = today };

            using var conn = DatabaseHelper.GetConnection();

            // Ringkasan header
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT
                        COUNT(DISTINCT t.id),
                        COALESCE(SUM(ti.quantity), 0),
                        COALESCE(SUM(ti.subtotal), 0),
                        COALESCE(SUM(ti.quantity * p.buy_price), 0)
                    FROM transactions t
                    JOIN transaction_items ti ON ti.transaction_id = t.id
                    JOIN products p           ON p.id = ti.product_id
                    WHERE date(t.created_at) = @today";
                cmd.Parameters.AddWithValue("@today", today);

                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    summary.TotalTrx = r.GetInt32(0);
                    summary.TotalQty = r.GetInt32(1);
                    summary.TotalOmzet = r.GetDecimal(2);
                    summary.TotalHpp = r.GetDecimal(3);
                }
            }

            // Detail per produk
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT
                        ti.product_code,
                        ti.product_name,
                        ti.unit,
                        SUM(ti.quantity)                    AS qty,
                        SUM(ti.subtotal)                    AS omzet,
                        SUM(ti.quantity * p.buy_price)      AS hpp
                    FROM transactions t
                    JOIN transaction_items ti ON ti.transaction_id = t.id
                    JOIN products p           ON p.id = ti.product_id
                    WHERE date(t.created_at) = @today
                    GROUP BY ti.product_id
                    ORDER BY qty DESC";
                cmd.Parameters.AddWithValue("@today", today);

                using var r = cmd.ExecuteReader();
                while (r.Read())
                    summary.Items.Add(new EodItemRow
                    {
                        ProductCode = r.GetString(0),
                        ProductName = r.GetString(1),
                        Unit = r.GetString(2),
                        TotalQty = r.GetInt32(3),
                        TotalOmzet = r.GetDecimal(4),
                        TotalHpp = r.GetDecimal(5)
                    });
            }

            return summary;
        }

        /// <summary>
        /// Simpan EOD ke database.
        /// Validasi: sesi harus CLOSED, EOD hari ini belum dilakukan.
        /// </summary>
        public void SaveEod(EodSummary summary, decimal physicalCash, string notes)
        {
            // Validasi sesi
            if (_clerk.IsSessionOpen())
                throw new InvalidOperationException(
                    "Kasir masih buka. Lakukan Clerk (tutup kasir) di aplikasi POS terlebih dahulu.");

            if (_clerk.IsEodDone())
                throw new InvalidOperationException(
                    "EOD hari ini sudah pernah dilakukan.");

            using var conn = DatabaseHelper.GetConnection();
            using var trx = conn.BeginTransaction();

            int eodId;

            // Insert eod_records
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = trx;
                cmd.CommandText = @"
                    INSERT INTO eod_records
                        (session_date, total_trx, total_qty, total_omzet,
                         total_hpp, total_laba, system_cash,
                         physical_cash, cash_difference, cashier_notes)
                    VALUES
                        (@date, @trx, @qty, @omzet,
                         @hpp, @laba, @sys,
                         @phys, @diff, @notes);
                    SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("@date", summary.SessionDate);
                cmd.Parameters.AddWithValue("@trx", summary.TotalTrx);
                cmd.Parameters.AddWithValue("@qty", summary.TotalQty);
                cmd.Parameters.AddWithValue("@omzet", summary.TotalOmzet);
                cmd.Parameters.AddWithValue("@hpp", summary.TotalHpp);
                cmd.Parameters.AddWithValue("@laba", summary.TotalLaba);
                cmd.Parameters.AddWithValue("@sys", summary.TotalOmzet);
                cmd.Parameters.AddWithValue("@phys", physicalCash);
                cmd.Parameters.AddWithValue("@diff", physicalCash - summary.TotalOmzet);
                cmd.Parameters.AddWithValue("@notes", notes ?? "");

                eodId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // Insert eod_items
            foreach (var item in summary.Items)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = trx;
                cmd.CommandText = @"
                    INSERT INTO eod_items
                        (eod_id, product_code, product_name, unit,
                         total_qty, total_omzet, total_hpp, total_laba)
                    VALUES
                        (@eid, @code, @name, @unit,
                         @qty, @omzet, @hpp, @laba)";
                cmd.Parameters.AddWithValue("@eid", eodId);
                cmd.Parameters.AddWithValue("@code", item.ProductCode);
                cmd.Parameters.AddWithValue("@name", item.ProductName);
                cmd.Parameters.AddWithValue("@unit", item.Unit);
                cmd.Parameters.AddWithValue("@qty", item.TotalQty);
                cmd.Parameters.AddWithValue("@omzet", item.TotalOmzet);
                cmd.Parameters.AddWithValue("@hpp", item.TotalHpp);
                cmd.Parameters.AddWithValue("@laba", item.TotalLaba);
                cmd.ExecuteNonQuery();
            }

            trx.Commit();
        }

        /// <summary>
        /// Ambil riwayat EOD yang sudah tersimpan (untuk tampil di Management).
        /// </summary>
        public List<EodRecord> GetEodHistory(int limit = 30)
        {
            var list = new List<EodRecord>();
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, session_date, total_trx, total_qty,
                       total_omzet, total_hpp, system_cash,
                       physical_cash, cash_difference, cashier_notes, created_at
                FROM eod_records
                ORDER BY session_date DESC
                LIMIT @limit";
            cmd.Parameters.AddWithValue("@limit", limit);

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new EodRecord
                {
                    Id = r.GetInt32(0),
                    SessionDate = r.GetString(1),
                    TotalTrx = r.GetInt32(2),
                    TotalQty = r.GetInt32(3),
                    TotalOmzet = r.GetDecimal(4),
                    TotalHpp = r.GetDecimal(5),
                    SystemCash = r.GetDecimal(6),
                    PhysicalCash = r.GetDecimal(7),
                    CashDifference = r.GetDecimal(8),
                    CashierNotes = r.IsDBNull(9) ? "" : r.GetString(9),
                    CreatedAt = r.GetString(10)
                });

            return list;
        }
    }

    public class EodRecord
    {
        public int Id { get; set; }
        public string SessionDate { get; set; }
        public int TotalTrx { get; set; }
        public int TotalQty { get; set; }
        public decimal TotalOmzet { get; set; }
        public decimal TotalHpp { get; set; }
        public decimal TotalLaba => TotalOmzet - TotalHpp;
        public decimal SystemCash { get; set; }
        public decimal PhysicalCash { get; set; }
        public decimal CashDifference { get; set; }
        public string CashierNotes { get; set; }
        public string CreatedAt { get; set; }
        public string StatusLabel =>
            CashDifference == 0 ? "✅ Pas" :
            CashDifference > 0 ? $"➕ Lebih {CashDifference:N0}" :
                                   $"➖ Kurang {Math.Abs(CashDifference):N0}";
    }
}
