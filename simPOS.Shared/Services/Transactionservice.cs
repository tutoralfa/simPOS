using simPOS.Shared.Database;
using simPOS.Shared.Models;
using simPOS.Shared.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simPOS.Shared.Services
{
    public class TransactionService
    {
        private readonly TransactionRepository _repo;
        private readonly ProductRepository _productRepo;

        public TransactionService()
        {
            _repo = new TransactionRepository();
            _productRepo = new ProductRepository();
        }

        public Product GetProductByCode(string code)
            => _productRepo.GetByCode(code?.Trim().ToUpper());

        public List<Product> SearchProducts(string keyword)
            => _productRepo.Search(keyword);

        public List<Product> GetAllActiveProducts()
            => _productRepo.GetAll(includeInactive: false);

        public (bool Success, string Message) Save(Transaction trx)
        {
            if (trx.Items == null || trx.Items.Count == 0)
                return (false, "Keranjang kosong.");

            if (trx.PaidAmount < trx.TotalAmount)
                return (false, $"Uang bayar kurang. Kurang: Rp {(trx.TotalAmount - trx.PaidAmount):N0}");

            // Cek stok semua item sebelum commit
            foreach (var item in trx.Items)
            {
                var product = _productRepo.GetById(item.ProductId);
                if (product == null)
                    return (false, $"Barang \"{item.ProductName}\" tidak ditemukan.");
                if (product.Stock < item.Quantity)
                    return (false, $"Stok \"{item.ProductName}\" tidak cukup.\nStok tersedia: {product.Stock} {item.Unit}");
            }

            trx.ChangeAmount = trx.PaidAmount - trx.TotalAmount;

            try
            {
                _repo.Insert(trx);
                return (true, "OK");
            }
            catch (Exception ex)
            {
                return (false, $"Gagal menyimpan transaksi: {ex.Message}");
            }
        }

        /// Ambil nomor invoice terakhir yang tersimpan di DB
        public string GetLastInvoiceNo()
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT invoice_no FROM transactions ORDER BY id DESC LIMIT 1";
            var result = cmd.ExecuteScalar();
            return result?.ToString() ?? "";
        }

        /// Ambil transaksi lengkap beserta items berdasarkan nomor invoice
        public Transaction GetByInvoiceNo(string invoiceNo)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, invoice_no, total_amount, paid_amount, change_amount,
                       payment_method, notes, created_at
                FROM transactions WHERE invoice_no = @inv";
            cmd.Parameters.AddWithValue("@inv", invoiceNo.Trim());

            Transaction trx = null;
            using (var r = cmd.ExecuteReader())
            {
                if (!r.Read()) return null;
                trx = new Transaction
                {
                    Id = r.GetInt32(0),
                    InvoiceNo = r.GetString(1),
                    TotalAmount = r.GetDecimal(2),
                    PaidAmount = r.GetDecimal(3),
                    ChangeAmount = r.GetDecimal(4),
                    PaymentMethod = r.IsDBNull(5) ? "CASH" : r.GetString(5),
                    Notes = r.IsDBNull(6) ? "" : r.GetString(6),
                    CreatedAt = r.IsDBNull(7) ? "" : r.GetString(7),
                    Items = new System.Collections.Generic.List<TransactionItem>()
                };
            }

            using var cmd2 = conn.CreateCommand();
            cmd2.CommandText = @"
                SELECT product_id, product_code, product_name, unit, quantity, sell_price
                FROM transaction_items WHERE transaction_id = @tid";
            cmd2.Parameters.AddWithValue("@tid", trx.Id);
            using var r2 = cmd2.ExecuteReader();
            while (r2.Read())
                trx.Items.Add(new TransactionItem
                {
                    ProductId = r2.GetInt32(0),
                    ProductCode = r2.IsDBNull(1) ? "" : r2.GetString(1),
                    ProductName = r2.GetString(2),
                    Unit = r2.IsDBNull(3) ? "" : r2.GetString(3),
                    Quantity = r2.GetInt32(4),
                    SellPrice = r2.GetDecimal(5)
                });

            return trx;
        }

        /// Ambil semua transaksi hari ini beserta items-nya
        public System.Collections.Generic.List<Transaction> GetTodaySales()
        {
            var result = new System.Collections.Generic.List<Transaction>();
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, invoice_no, total_amount, paid_amount, change_amount,
                       payment_method, notes, created_at
                FROM transactions
                WHERE DATE(created_at) = DATE('now','localtime')
                ORDER BY id DESC";

            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    result.Add(new Transaction
                    {
                        Id = r.GetInt32(0),
                        InvoiceNo = r.GetString(1),
                        TotalAmount = r.GetDecimal(2),
                        PaidAmount = r.GetDecimal(3),
                        ChangeAmount = r.GetDecimal(4),
                        PaymentMethod = r.IsDBNull(5) ? "CASH" : r.GetString(5),
                        Notes = r.IsDBNull(6) ? "" : r.GetString(6),
                        CreatedAt = r.IsDBNull(7) ? "" : r.GetString(7),
                        Items = new System.Collections.Generic.List<TransactionItem>()
                    });
            }

            // Load items untuk tiap transaksi
            foreach (var trx in result)
            {
                using var cmd2 = conn.CreateCommand();
                cmd2.CommandText = @"
                    SELECT product_id, product_code, product_name, unit, quantity, sell_price
                    FROM transaction_items WHERE transaction_id = @tid";
                cmd2.Parameters.AddWithValue("@tid", trx.Id);
                using var r2 = cmd2.ExecuteReader();
                while (r2.Read())
                    trx.Items.Add(new TransactionItem
                    {
                        ProductId = r2.GetInt32(0),
                        ProductCode = r2.IsDBNull(1) ? "" : r2.GetString(1),
                        ProductName = r2.GetString(2),
                        Unit = r2.IsDBNull(3) ? "" : r2.GetString(3),
                        Quantity = r2.GetInt32(4),
                        SellPrice = r2.GetDecimal(5)
                    });
            }
            return result;
        }

        public string GenerateInvoiceNo()
        {
            var now = DateTime.Now;
            return $"INV-{now:yyyyMMdd}-{now:HHmmss}";
        }
    }
}
