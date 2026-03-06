using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using simPOS.Shared.Database;
using simPOS.Shared.Models;

namespace simPOS.Shared.Repositories
{
    public class GoodsReceiptRepository
    {
        /// <summary>
        /// Ambil semua penerimaan + nama supplier, diurutkan terbaru dulu.
        /// </summary>
        public List<GoodsReceipt> GetAll()
        {
            var list = new List<GoodsReceipt>();

            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    gr.id, gr.receipt_no, gr.supplier_id, gr.notes,
                    gr.received_at, gr.created_at,
                    COALESCE(s.name, '-') AS supplier_name,
                    COUNT(sm.id) AS item_count
                FROM goods_receipts gr
                LEFT JOIN suppliers      s  ON s.id  = gr.supplier_id
                LEFT JOIN stock_movements sm ON sm.receipt_id = gr.id
                GROUP BY gr.id
                ORDER BY gr.received_at DESC";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var gr = MapFromReader(reader);
                // Simpan item_count sementara di TotalItems via dummy items
                for (int i = 0; i < reader.GetInt32(7); i++)
                    gr.Items.Add(new GoodsReceiptItem()); // placeholder count saja
                list.Add(gr);
            }
            return list;
        }

        public GoodsReceipt GetById(int id)
        {
            using var conn = DatabaseHelper.GetConnection();

            // ✅ Reader ditutup (disposed) di akhir blok using sebelum GetItems dipanggil.
            // SQLite tidak boleh punya dua reader aktif di satu koneksi — jika reader
            // header masih terbuka, GetItems akan mengembalikan list kosong tanpa error.
            GoodsReceipt gr;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT gr.id, gr.receipt_no, gr.supplier_id, gr.notes,
                           gr.received_at, gr.created_at,
                           COALESCE(s.name, '-') AS supplier_name
                    FROM goods_receipts gr
                    LEFT JOIN suppliers s ON s.id = gr.supplier_id
                    WHERE gr.id = @id";
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read()) return null;
                gr = MapFromReader(reader);
            } // ← reader & cmd di-dispose di sini, koneksi kembali bebas

            // Baru ambil items setelah reader header tertutup
            gr.Items = GetItems(conn, id);
            return gr;
        }

        /// <summary>
        /// Simpan penerimaan barang dalam satu transaksi atomik:
        /// 1. INSERT goods_receipts (header)
        /// 2. INSERT stock_movements per item (detail)
        /// 3. UPDATE products.stock per item
        /// Kalau salah satu gagal, semuanya di-rollback.
        /// </summary>
        public int Insert(GoodsReceipt receipt)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var trx = conn.BeginTransaction();

            try
            {
                // 1. Insert header
                int receiptId;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = trx;
                    cmd.CommandText = @"
                        INSERT INTO goods_receipts (receipt_no, supplier_id, notes, received_at)
                        VALUES (@receiptNo, @supplierId, @notes, @receivedAt);
                        SELECT last_insert_rowid();";

                    cmd.Parameters.AddWithValue("@receiptNo", receipt.ReceiptNo);
                    cmd.Parameters.AddWithValue("@supplierId", (object)receipt.SupplierId ?? System.DBNull.Value);
                    cmd.Parameters.AddWithValue("@notes", receipt.Notes ?? "");
                    cmd.Parameters.AddWithValue("@receivedAt", receipt.ReceivedAt);

                    receiptId = (int)(long)cmd.ExecuteScalar();
                }

                // 2 & 3. Insert movement + update stok per item
                foreach (var item in receipt.Items)
                {
                    // Insert stock_movements
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = trx;
                        cmd.CommandText = @"
                            INSERT INTO stock_movements 
                                (product_id, receipt_id, type, quantity, buy_price, notes, reference_no)
                            VALUES 
                                (@productId, @receiptId, 'IN', @qty, @buyPrice, @notes, @refNo)";

                        cmd.Parameters.AddWithValue("@productId", item.ProductId);
                        cmd.Parameters.AddWithValue("@receiptId", receiptId);
                        cmd.Parameters.AddWithValue("@qty", item.Quantity);
                        cmd.Parameters.AddWithValue("@buyPrice", item.BuyPrice);
                        cmd.Parameters.AddWithValue("@notes", "");
                        cmd.Parameters.AddWithValue("@refNo", receipt.ReceiptNo);
                        cmd.ExecuteNonQuery();
                    }

                    // Update stok produk
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = trx;
                        cmd.CommandText = @"
                            UPDATE products 
                            SET stock      = stock + @qty,
                                buy_price  = @buyPrice,
                                updated_at = datetime('now', 'localtime')
                            WHERE id = @productId";

                        cmd.Parameters.AddWithValue("@qty", item.Quantity);
                        cmd.Parameters.AddWithValue("@buyPrice", item.BuyPrice);
                        cmd.Parameters.AddWithValue("@productId", item.ProductId);
                        cmd.ExecuteNonQuery();
                    }
                }

                trx.Commit();
                return receiptId;
            }
            catch
            {
                trx.Rollback();
                throw;
            }
        }

        public bool IsReceiptNoExists(string receiptNo)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM goods_receipts WHERE receipt_no = @no";
            cmd.Parameters.AddWithValue("@no", receiptNo);
            return (long)cmd.ExecuteScalar() > 0;
        }

        // ── Private Helpers ──────────────────────────────────────────────

        private List<GoodsReceiptItem> GetItems(SqliteConnection conn, int receiptId)
        {
            var items = new List<GoodsReceiptItem>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT sm.product_id, p.code, p.name, p.unit, sm.quantity, sm.buy_price
                FROM stock_movements sm
                JOIN products p ON p.id = sm.product_id
                WHERE sm.receipt_id = @receiptId
                ORDER BY p.name";
            cmd.Parameters.AddWithValue("@receiptId", receiptId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                items.Add(new GoodsReceiptItem
                {
                    ProductId = reader.GetInt32(0),
                    ProductCode = reader.GetString(1),
                    ProductName = reader.GetString(2),
                    Unit = reader.GetString(3),
                    Quantity = reader.GetInt32(4),
                    BuyPrice = reader.GetDecimal(5)
                });
            }
            return items;
        }

        private static GoodsReceipt MapFromReader(SqliteDataReader r) => new GoodsReceipt
        {
            Id = r.GetInt32(0),
            ReceiptNo = r.GetString(1),
            SupplierId = r.IsDBNull(2) ? null : r.GetInt32(2),
            Notes = r.IsDBNull(3) ? "" : r.GetString(3),
            ReceivedAt = r.IsDBNull(4) ? "" : r.GetString(4),
            CreatedAt = r.IsDBNull(5) ? "" : r.GetString(5),
            SupplierName = r.GetString(6)
        };
    }
}
