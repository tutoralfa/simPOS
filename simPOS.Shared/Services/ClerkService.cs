using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using simPOS.Shared.Database;

namespace simPOS.Shared.Services
{
    /// <summary>
    /// Mengelola sesi kasir harian (cash_sessions).
    /// Dipakai bersama oleh simPOS.POS dan simPOS.Management.
    /// </summary>
    public class ClerkService
    {
        // ── Buka sesi hari ini (dipanggil saat POS start) ────────────

        /// <summary>
        /// Pastikan ada sesi OPEN untuk hari ini.
        /// Jika belum ada, buat baru secara otomatis.
        /// </summary>
        public void EnsureSessionOpen()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                INSERT OR IGNORE INTO cash_sessions (session_date, status)
                VALUES (@date, 'OPEN')";
            cmd.Parameters.AddWithValue("@date", today);
            cmd.ExecuteNonQuery();
        }

        // ── Status sesi ──────────────────────────────────────────────

        /// <summary>
        /// Cek apakah kasir masih OPEN hari ini.
        /// Jika tidak ada record → dianggap belum dibuka.
        /// </summary>
        public bool IsSessionOpen()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(*) FROM cash_sessions
                WHERE session_date = @date AND status = 'OPEN'";
            cmd.Parameters.AddWithValue("@date", today);
            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result) > 0;
        }

        /// <summary>
        /// Cek apakah EOD sudah dilakukan hari ini.
        /// EOD bisa dilakukan hanya setelah sesi CLOSED.
        /// </summary>
        public bool IsEodDone()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(*) FROM eod_records
                WHERE session_date = @date";
            cmd.Parameters.AddWithValue("@date", today);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        /// <summary>
        /// Ambil info sesi hari ini.
        /// Returns null jika belum ada sesi.
        /// </summary>
        public CashSession GetTodaySession()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, session_date, opened_at, closed_at, status, notes
                FROM cash_sessions
                WHERE session_date = @date";
            cmd.Parameters.AddWithValue("@date", today);

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;

            return new CashSession
            {
                Id = r.GetInt32(0),
                SessionDate = r.GetString(1),
                OpenedAt = r.GetString(2),
                ClosedAt = r.IsDBNull(3) ? null : r.GetString(3),
                Status = r.GetString(4),
                Notes = r.IsDBNull(5) ? "" : r.GetString(5)
            };
        }

        // ── Tutup sesi (Clerk) ───────────────────────────────────────

        /// <summary>
        /// Tutup sesi kasir hari ini.
        /// Setelah ini transaksi baru tidak bisa dibuat di POS.
        /// </summary>
        public void CloseSession(string notes = "")
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE cash_sessions
                SET status    = 'CLOSED',
                    closed_at = datetime('now','localtime'),
                    notes     = @notes
                WHERE session_date = @date AND status = 'OPEN'";
            cmd.Parameters.AddWithValue("@date", today);
            cmd.Parameters.AddWithValue("@notes", notes ?? "");

            int affected = cmd.ExecuteNonQuery();
            if (affected == 0)
                throw new InvalidOperationException(
                    "Tidak ada sesi yang sedang buka hari ini, atau sesi sudah ditutup.");
        }

        // ── Paksa buka ulang (hanya dari Management / emergency) ─────

        public void ForceReopenSession()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE cash_sessions
                SET status    = 'OPEN',
                    closed_at = NULL
                WHERE session_date = @date";
            cmd.Parameters.AddWithValue("@date", today);
            cmd.ExecuteNonQuery();
        }
    }

    public class CashSession
    {
        public int Id { get; set; }
        public string SessionDate { get; set; }
        public string OpenedAt { get; set; }
        public string ClosedAt { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public bool IsOpen => Status == "OPEN";
    }
}
