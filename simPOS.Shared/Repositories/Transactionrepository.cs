using simPOS.Shared.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using simPOS.Shared.Models;

namespace simPOS.Shared.Repositories
{
    public class TransactionRepository
    {
        /// <summary>
        /// Simpan transaksi secara atomik:
        /// 1. INSERT transactions (header)
        /// 2. INSERT transaction_items per item
        /// 3. INSERT stock_movements type=OUT per item
        /// 4. UPDATE products.stock per item
        /// </summary>
        public int Insert(Transaction trx)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var dbTrx = conn.BeginTransaction();

            try
            {
                // 1. Insert header
                int trxId;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = dbTrx;
                    cmd.CommandText = @"
                        INSERT INTO transactions
                            (invoice_no, total_amount, paid_amount, change_amount, payment_method, notes)
                        VALUES
                            (@invoiceNo, @total, @paid, @change, @method, @notes);
                        SELECT last_insert_rowid();";
                    cmd.Parameters.AddWithValue("@invoiceNo", trx.InvoiceNo);
                    cmd.Parameters.AddWithValue("@total", trx.TotalAmount);
                    cmd.Parameters.AddWithValue("@paid", trx.PaidAmount);
                    cmd.Parameters.AddWithValue("@change", trx.ChangeAmount);
                    cmd.Parameters.AddWithValue("@method", trx.PaymentMethod);
                    cmd.Parameters.AddWithValue("@notes", trx.Notes ?? "");
                    trxId = (int)(long)cmd.ExecuteScalar();
                }

                foreach (var item in trx.Items)
                {
                    // 2. Insert transaction_items
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = dbTrx;
                        cmd.CommandText = @"
                            INSERT INTO transaction_items
                                (transaction_id, product_id, product_code, product_name, unit, quantity, sell_price, subtotal)
                            VALUES
                                (@trxId, @productId, @code, @name, @unit, @qty, @price, @subtotal)";
                        cmd.Parameters.AddWithValue("@trxId", trxId);
                        cmd.Parameters.AddWithValue("@productId", item.ProductId);
                        cmd.Parameters.AddWithValue("@code", item.ProductCode);
                        cmd.Parameters.AddWithValue("@name", item.ProductName);
                        cmd.Parameters.AddWithValue("@unit", item.Unit);
                        cmd.Parameters.AddWithValue("@qty", item.Quantity);
                        cmd.Parameters.AddWithValue("@price", item.SellPrice);
                        cmd.Parameters.AddWithValue("@subtotal", item.Subtotal);
                        cmd.ExecuteNonQuery();
                    }

                    // 3. Insert stock_movements OUT
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = dbTrx;
                        cmd.CommandText = @"
                            INSERT INTO stock_movements
                                (product_id, transaction_id, type, quantity, notes, reference_no)
                            VALUES
                                (@productId, @trxId, 'OUT', @qty, '', @invoiceNo)";
                        cmd.Parameters.AddWithValue("@productId", item.ProductId);
                        cmd.Parameters.AddWithValue("@trxId", trxId);
                        cmd.Parameters.AddWithValue("@qty", item.Quantity);
                        cmd.Parameters.AddWithValue("@invoiceNo", trx.InvoiceNo);
                        cmd.ExecuteNonQuery();
                    }

                    // 4. Kurangi stok
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = dbTrx;
                        cmd.CommandText = @"
                            UPDATE products
                            SET stock      = stock - @qty,
                                updated_at = datetime('now', 'localtime')
                            WHERE id = @productId";
                        cmd.Parameters.AddWithValue("@qty", item.Quantity);
                        cmd.Parameters.AddWithValue("@productId", item.ProductId);
                        cmd.ExecuteNonQuery();
                    }
                }

                dbTrx.Commit();
                return trxId;
            }
            catch
            {
                dbTrx.Rollback();
                throw;
            }
        }

        public bool IsInvoiceExists(string invoiceNo)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM transactions WHERE invoice_no = @no";
            cmd.Parameters.AddWithValue("@no", invoiceNo);
            return (long)cmd.ExecuteScalar() > 0;
        }
    }
}
