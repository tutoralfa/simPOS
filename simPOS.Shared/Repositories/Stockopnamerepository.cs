using Microsoft.Data.Sqlite;
using simPOS.Shared.Database;
using simPOS.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simPOS.Shared.Repositories
{
    public class StockOpnameRepository
    {
        public List<StockOpname> GetAll()
        {
            var list = new List<StockOpname>();
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, opname_no, notes, status, confirmed_at, created_at
                FROM stock_opnames
                ORDER BY created_at DESC";
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(MapFromReader(reader));
            return list;
        }

        public StockOpname GetById(int id)
        {
            StockOpname opname;
            using var conn = DatabaseHelper.GetConnection();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT id, opname_no, notes, status, confirmed_at, created_at
                    FROM stock_opnames WHERE id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                using var reader = cmd.ExecuteReader();
                if (!reader.Read()) return null;
                opname = MapFromReader(reader);
            }

            opname.Items = GetItems(conn, id);
            return opname;
        }

        /// <summary>
        /// Buat sesi opname baru (DRAFT) dan ambil snapshot stok semua produk aktif.
        /// </summary>
        public int CreateDraft(StockOpname opname)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var trx = conn.BeginTransaction();
            try
            {
                // 1. Insert header
                int opnameId;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = trx;
                    cmd.CommandText = @"
                        INSERT INTO stock_opnames (opname_no, notes, status)
                        VALUES (@no, @notes, 'DRAFT');
                        SELECT last_insert_rowid();";
                    cmd.Parameters.AddWithValue("@no", opname.OpnameNo);
                    cmd.Parameters.AddWithValue("@notes", opname.Notes ?? "");
                    opnameId = (int)(long)cmd.ExecuteScalar();
                }

                // 2. Snapshot stok semua produk aktif ke tabel sementara opname_items
                //    Kita simpan di stock_movements dengan quantity=0 sebagai "placeholder"
                //    Nanti saat Confirm baru quantity diisi selisihnya.
                //    ATAU: simpan langsung di memory saja — tidak perlu tabel baru.
                //    Pilihan ini lebih simple: items hanya ada saat form terbuka.

                trx.Commit();
                return opnameId;
            }
            catch
            {
                trx.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Konfirmasi opname: simpan ADJUSTMENT ke stock_movements dan update stok.
        /// Hanya item yang berbeda (Difference != 0) yang disimpan.
        /// Atomik — semua atau tidak sama sekali.
        /// </summary>
        public void Confirm(int opnameId, string opnameNo, List<StockOpnameItem> items)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var trx = conn.BeginTransaction();
            try
            {
                foreach (var item in items)
                {
                    if (item.Difference == 0) continue;

                    // Simpan movement ADJUSTMENT
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = trx;
                        cmd.CommandText = @"
                            INSERT INTO stock_movements
                                (product_id, opname_id, type, quantity, notes, reference_no)
                            VALUES
                                (@productId, @opnameId, 'ADJUSTMENT', @qty, @notes, @refNo)";
                        cmd.Parameters.AddWithValue("@productId", item.ProductId);
                        cmd.Parameters.AddWithValue("@opnameId", opnameId);
                        cmd.Parameters.AddWithValue("@qty", item.Difference);
                        cmd.Parameters.AddWithValue("@notes", $"Stok fisik: {item.PhysicalStock}, Stok sistem: {item.SystemStock}");
                        cmd.Parameters.AddWithValue("@refNo", opnameNo);
                        cmd.ExecuteNonQuery();
                    }

                    // Update stok produk langsung ke nilai fisik
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = trx;
                        cmd.CommandText = @"
                            UPDATE products
                            SET stock      = @physicalStock,
                                updated_at = datetime('now', 'localtime')
                            WHERE id = @productId";
                        cmd.Parameters.AddWithValue("@physicalStock", item.PhysicalStock);
                        cmd.Parameters.AddWithValue("@productId", item.ProductId);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Tandai opname sebagai CONFIRMED
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = trx;
                    cmd.CommandText = @"
                        UPDATE stock_opnames
                        SET status       = 'CONFIRMED',
                            confirmed_at = datetime('now', 'localtime')
                        WHERE id = @id";
                    cmd.Parameters.AddWithValue("@id", opnameId);
                    cmd.ExecuteNonQuery();
                }

                trx.Commit();
            }
            catch
            {
                trx.Rollback();
                throw;
            }
        }

        public bool IsOpnameNoExists(string opnameNo)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM stock_opnames WHERE opname_no = @no";
            cmd.Parameters.AddWithValue("@no", opnameNo);
            return (long)cmd.ExecuteScalar() > 0;
        }

        // ── Private Helpers ──────────────────────────────────────────────

        /// <summary>
        /// Ambil item dari stock_movements ADJUSTMENT yang terhubung ke opname ini.
        /// Dipakai untuk view detail opname yang sudah CONFIRMED.
        /// </summary>
        private List<StockOpnameItem> GetItems(SqliteConnection conn, int opnameId)
        {
            var items = new List<StockOpnameItem>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT sm.product_id, p.code, p.name, p.unit,
                       sm.quantity,
                       sm.notes
                FROM stock_movements sm
                JOIN products p ON p.id = sm.product_id
                WHERE sm.opname_id = @opnameId
                  AND sm.type = 'ADJUSTMENT'
                ORDER BY p.name";
            cmd.Parameters.AddWithValue("@opnameId", opnameId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                // Parse system & physical stock dari field notes yang kita simpan
                var notes = reader.IsDBNull(5) ? "" : reader.GetString(5);
                var diff = reader.GetInt32(4);
                var sysStock = ParseNoteValue(notes, "Stok sistem: ");
                var phyStock = ParseNoteValue(notes, "Stok fisik: ");

                items.Add(new StockOpnameItem
                {
                    ProductId = reader.GetInt32(0),
                    ProductCode = reader.GetString(1),
                    ProductName = reader.GetString(2),
                    Unit = reader.GetString(3),
                    SystemStock = sysStock,
                    PhysicalStock = phyStock
                });
            }
            return items;
        }

        private static int ParseNoteValue(string notes, string key)
        {
            var idx = notes.IndexOf(key, StringComparison.Ordinal);
            if (idx < 0) return 0;
            var start = idx + key.Length;
            var end = notes.IndexOf(',', start);
            var raw = end < 0 ? notes.Substring(start) : notes.Substring(start, end - start);
            return int.TryParse(raw.Trim(), out int val) ? val : 0;
        }

        private static StockOpname MapFromReader(SqliteDataReader r) => new StockOpname
        {
            Id = r.GetInt32(0),
            OpnameNo = r.GetString(1),
            Notes = r.IsDBNull(2) ? "" : r.GetString(2),
            Status = r.GetString(3),
            ConfirmedAt = r.IsDBNull(4) ? "" : r.GetString(4),
            CreatedAt = r.IsDBNull(5) ? "" : r.GetString(5)
        };
    }
}
