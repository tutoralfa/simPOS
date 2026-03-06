using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using Microsoft.Data.Sqlite;
using simPOS.Shared.Database;

namespace simPOS.Shared.Database
{
    /// <summary>
    /// Service untuk backup, restore, dan reset database simPOS.
    ///
    /// Backup  : copy file .sqlite ke folder backup, zip dengan timestamp.
    /// Restore : ekstrak zip backup, replace file database aktif.
    /// Reset   : hapus semua data transaksi/stok (TRUNCATE), atau reset total (drop + recreate).
    /// </summary>
    public class DatabaseBackupService
    {
        // Folder backup: %AppData%\Local\simPOS\Backup\
        public static string BackupFolder =>
            Path.Combine(AppConfig.DataFolder, "Backup");

        // ── BACKUP ───────────────────────────────────────────────────

        /// <summary>
        /// Buat backup database ke BackupFolder.
        /// Menggunakan SQLite Online Backup API (aman saat app berjalan).
        /// File hasil: simPOS_backup_yyyyMMdd_HHmmss.zip
        /// </summary>
        /// <returns>Path lengkap file .zip yang dibuat.</returns>
        public string CreateBackup()
        {
            Directory.CreateDirectory(BackupFolder);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var sqlitePath = Path.Combine(BackupFolder, $"simPOS_{timestamp}.sqlite");
            var zipPath = Path.Combine(BackupFolder, $"simPOS_backup_{timestamp}.zip");

            // Tutup semua koneksi pooled agar tidak ada file handle aktif
            SqliteConnection.ClearAllPools();

            // SQLite Online Backup API — buka koneksi baru khusus backup
            using (var src = new SqliteConnection(
                       $"Data Source={AppConfig.DatabasePath};Mode=ReadOnly"))
            using (var dest = new SqliteConnection(
                       $"Data Source={sqlitePath}"))
            {
                src.Open();
                dest.Open();
                src.BackupDatabase(dest);
                // Paksa flush WAL ke file utama sebelum tutup
                using (var cmd = dest.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE)";
                    cmd.ExecuteNonQuery();
                }
            }

            // Lepas semua handle setelah using block
            SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Zip file sqlite hasil backup
            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(sqlitePath,
                    Path.GetFileName(sqlitePath),
                    CompressionLevel.Optimal);
            }

            // Hapus file sqlite sementara
            File.Delete(sqlitePath);

            return zipPath;
        }

        /// <summary>
        /// Daftar semua file backup yang ada di BackupFolder,
        /// diurutkan terbaru di atas.
        /// </summary>
        public BackupFileInfo[] GetBackupList()
        {
            if (!Directory.Exists(BackupFolder))
                return Array.Empty<BackupFileInfo>();

            var files = Directory.GetFiles(BackupFolder, "simPOS_backup_*.zip");
            var list = new System.Collections.Generic.List<BackupFileInfo>();

            foreach (var f in files)
            {
                var fi = new FileInfo(f);
                list.Add(new BackupFileInfo
                {
                    FilePath = f,
                    FileName = fi.Name,
                    CreatedAt = fi.LastWriteTime,
                    SizeBytes = fi.Length
                });
            }

            list.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
            return list.ToArray();
        }

        // ── RESTORE ──────────────────────────────────────────────────

        /// <summary>
        /// Restore database dari file backup .zip.
        /// Backup database aktif dibuat otomatis sebelum restore.
        /// </summary>
        /// <param name="zipPath">Path file .zip backup.</param>
        public void RestoreFromBackup(string zipPath)
        {
            if (!File.Exists(zipPath))
                throw new FileNotFoundException("File backup tidak ditemukan.", zipPath);

            // 1. Auto-backup sebelum restore (safety net)
            CreateBackup();

            // 2. Ekstrak .sqlite dari zip ke folder temp
            var tempDir = Path.Combine(Path.GetTempPath(), "simPOS_restore");
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            ZipFile.ExtractToDirectory(zipPath, tempDir);

            var sqliteFiles = Directory.GetFiles(tempDir, "*.sqlite");
            if (sqliteFiles.Length == 0)
                throw new InvalidDataException("File backup tidak mengandung database (.sqlite).");

            var extractedDb = sqliteFiles[0];

            // 3. Validasi file sqlite yang diekstrak
            ValidateSqliteFile(extractedDb);

            // 4. Replace database aktif
            File.Copy(extractedDb, AppConfig.DatabasePath, overwrite: true);

            // 5. Cleanup temp
            Directory.Delete(tempDir, true);
        }

        // ── RESET ────────────────────────────────────────────────────

        /// <summary>
        /// Reset data transaksi saja (jual/beli/stok).
        /// Master data (produk, kategori, supplier) tetap ada.
        /// Backup otomatis dibuat sebelum reset.
        /// </summary>
        public void ResetTransactionData()
        {
            CreateBackup(); // safety net

            using var conn = new SqliteConnection($"Data Source={AppConfig.DatabasePath}");
            conn.Open();

            using var trx = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trx;

            // Urutan hapus: detail dulu, lalu header, lalu stok
            foreach (var sql in new[]
            {
                "DELETE FROM transaction_items",
                "DELETE FROM transactions",
                "DELETE FROM stock_movements",
                // Reset stock ke 0 di semua produk
                "UPDATE products SET stock = 0",
                // Reset autoincrement
                "DELETE FROM sqlite_sequence WHERE name IN " +
                "('transactions','transaction_items','stock_movements')"
            })
            {
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }

            trx.Commit();
        }

        /// <summary>
        /// Reset TOTAL — hapus semua data termasuk master.
        /// Database dikembalikan ke kondisi fresh install (hanya seed data).
        /// Backup otomatis dibuat sebelum reset.
        /// </summary>
        public void ResetAllData()
        {
            CreateBackup(); // safety net

            // Drop semua tabel, lalu jalankan ulang schema
            using var conn = new SqliteConnection($"Data Source={AppConfig.DatabasePath}");
            conn.Open();

            // Matikan FK sementara agar drop tidak error
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA foreign_keys = OFF";
                cmd.ExecuteNonQuery();
            }

            // Drop semua tabel
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT name FROM sqlite_master
                    WHERE type='table' AND name NOT LIKE 'sqlite_%'";
                var tables = new System.Collections.Generic.List<string>();
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) tables.Add(r.GetString(0));

                foreach (var t in tables)
                {
                    cmd.CommandText = $"DROP TABLE IF EXISTS [{t}]";
                    cmd.ExecuteNonQuery();
                }
            }

            conn.Close();

            // Jalankan ulang schema + seed
            DatabaseHelper.Initialize(AppConfig.DatabasePath);
        }

        // ── HELPERS ──────────────────────────────────────────────────

        private static void ValidateSqliteFile(string path)
        {
            // SQLite magic bytes: 53 51 4C 69 74 65 20 66 6F 72 6D 61 74 20 33 00
            var magic = new byte[] { 0x53,0x51,0x4C,0x69,0x74,0x65,0x20,
                                     0x66,0x6F,0x72,0x6D,0x61,0x74,0x20,0x33,0x00 };
            var header = new byte[16];
            using var fs = File.OpenRead(path);
            if (fs.Read(header, 0, 16) < 16)
                throw new InvalidDataException("File bukan database SQLite yang valid.");
            for (int i = 0; i < 16; i++)
                if (header[i] != magic[i])
                    throw new InvalidDataException("File bukan database SQLite yang valid.");
        }
    }

    public class BackupFileInfo
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedAt { get; set; }
        public long SizeBytes { get; set; }
        public string SizeLabel =>
            SizeBytes >= 1024 * 1024
            ? $"{SizeBytes / 1024.0 / 1024.0:N1} MB"
            : $"{SizeBytes / 1024.0:N1} KB";
    }
}
