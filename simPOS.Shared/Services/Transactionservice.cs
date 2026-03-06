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

        public string GenerateInvoiceNo()
        {
            var now = DateTime.Now;
            return $"INV-{now:yyyyMMdd}-{now:HHmmss}";
        }
    }
}
