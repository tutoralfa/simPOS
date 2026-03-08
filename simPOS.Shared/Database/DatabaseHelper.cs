using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simPOS.Shared.Database
{
    public static class DatabaseHelper
    {
        private static string _connectionString;

        public static void Initialize(string dbPath)
        {
            _connectionString = $"Data Source={dbPath};";
            EnsureDatabaseCreated(dbPath);
            MigrateIfNeeded(dbPath);
        }

        public static SqliteConnection GetConnection()
        {
            if (string.IsNullOrEmpty(_connectionString))
                throw new InvalidOperationException("Database belum diinisialisasi. Panggil DatabaseHelper.Initialize() dulu.");

            var conn = new SqliteConnection(_connectionString);
            conn.Open();

            // Foreign keys aktif untuk semua koneksi normal
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();

            return conn;
        }

        private static void EnsureDatabaseCreated(string dbPath)
        {
            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "schema.sql");
            if (!File.Exists(schemaPath))
                throw new FileNotFoundException($"Schema SQL tidak ditemukan: {schemaPath}");

            var sql = File.ReadAllText(schemaPath);

            using var conn = new SqliteConnection($"Data Source={dbPath};");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Migrasi kolom baru untuk database yang sudah ada sebelumnya.
        /// PENTING: Gunakan koneksi TANPA foreign_keys = ON.
        /// SQLite melarang ALTER TABLE saat foreign key enforcement aktif.
        /// </summary>
        private static void MigrateIfNeeded(string dbPath)
        {
            // Koneksi migrasi — TIDAK pakai GetConnection() agar FK tidak aktif
            using var conn = new SqliteConnection($"Data Source={dbPath};");
            conn.Open();

            // Tabel EOD & Cash Sessions (tambah jika belum ada)
            CreateTableIfNotExists(conn, @"
                CREATE TABLE IF NOT EXISTS cash_sessions (
                    id           INTEGER PRIMARY KEY AUTOINCREMENT,
                    session_date TEXT    NOT NULL,
                    opened_at    TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
                    closed_at    TEXT,
                    status       TEXT    NOT NULL DEFAULT 'OPEN'
                                         CHECK(status IN ('OPEN','CLOSED')),
                    notes        TEXT,
                    UNIQUE(session_date)
                )");

            CreateTableIfNotExists(conn, @"
                CREATE TABLE IF NOT EXISTS eod_records (
                    id               INTEGER PRIMARY KEY AUTOINCREMENT,
                    session_date     TEXT    NOT NULL UNIQUE,
                    total_trx        INTEGER NOT NULL DEFAULT 0,
                    total_qty        INTEGER NOT NULL DEFAULT 0,
                    total_omzet      REAL    NOT NULL DEFAULT 0,
                    total_hpp        REAL    NOT NULL DEFAULT 0,
                    total_laba       REAL    NOT NULL DEFAULT 0,
                    system_cash      REAL    NOT NULL DEFAULT 0,
                    physical_cash    REAL    NOT NULL DEFAULT 0,
                    cash_difference  REAL    NOT NULL DEFAULT 0,
                    cashier_notes    TEXT,
                    created_at       TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
                )");

            CreateTableIfNotExists(conn, @"
                CREATE TABLE IF NOT EXISTS eod_items (
                    id           INTEGER PRIMARY KEY AUTOINCREMENT,
                    eod_id       INTEGER NOT NULL REFERENCES eod_records(id) ON DELETE CASCADE,
                    product_code TEXT    NOT NULL,
                    product_name TEXT    NOT NULL,
                    unit         TEXT    NOT NULL,
                    total_qty    INTEGER NOT NULL DEFAULT 0,
                    total_omzet  REAL    NOT NULL DEFAULT 0,
                    total_hpp    REAL    NOT NULL DEFAULT 0,
                    total_laba   REAL    NOT NULL DEFAULT 0
                )");

            AddColumnIfNotExists(conn, "products", "barcode", "TEXT NOT NULL DEFAULT ''");

            AddColumnIfNotExists(conn, "stock_movements", "receipt_id",
                "INTEGER REFERENCES goods_receipts(id) ON DELETE SET NULL");

            AddColumnIfNotExists(conn, "stock_movements", "buy_price",
                "REAL NOT NULL DEFAULT 0");

            AddColumnIfNotExists(conn, "stock_movements", "opname_id",
                "INTEGER REFERENCES stock_opnames(id) ON DELETE SET NULL");

            AddColumnIfNotExists(conn, "stock_movements", "transaction_id",
                "INTEGER REFERENCES transactions(id) ON DELETE SET NULL");
        }

        /// <summary>
        /// Cek kolom via PRAGMA, baru ALTER jika belum ada.
        /// Aman dipanggil berulang kali (idempotent).
        /// </summary>
        private static void CreateTableIfNotExists(SqliteConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private static void AddColumnIfNotExists(SqliteConnection conn, string table, string column, string definition)
        {
            bool exists = false;

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA table_info({table})";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.GetString(1).Equals(column, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }
            }

            if (!exists)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition}";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
