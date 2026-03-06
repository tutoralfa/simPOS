using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using simPOS.Shared.Models;
using simPOS.Shared.Repositories;

namespace simPOS.Shared.Services
{
    public class ProductService
    {
        private readonly ProductRepository _productRepo;
        private readonly CategoryRepository _categoryRepo;

        public ProductService()
        {
            _productRepo = new ProductRepository();
            _categoryRepo = new CategoryRepository();
        }

        public List<Product> GetAll() => _productRepo.GetAll();
        public List<Product> Search(string kw, int categoryId = 0)
                                               => _productRepo.Search(kw, categoryId);
        public Product GetById(int id) => _productRepo.GetById(id);

        /// <summary>
        /// Simpan produk baru dengan validasi
        /// </summary>
        public (bool Success, string Message) Save(Product product)
        {
            var validation = Validate(product);
            if (!validation.Success) return validation;

            if (_productRepo.IsCodeExists(product.Code))
                return (false, $"Kode '{product.Code}' sudah digunakan produk lain.");

            _productRepo.Insert(product);
            return (true, "Produk berhasil disimpan.");
        }

        /// <summary>
        /// Update produk dengan validasi
        /// </summary>
        public (bool Success, string Message) Update(Product product)
        {
            var validation = Validate(product);
            if (!validation.Success) return validation;

            if (_productRepo.IsCodeExists(product.Code, product.Id))
                return (false, $"Kode '{product.Code}' sudah digunakan produk lain.");

            _productRepo.Update(product);
            return (true, "Produk berhasil diperbarui.");
        }

        /// <summary>
        /// Soft delete — tidak menghapus data permanen
        /// </summary>
        public (bool Success, string Message) Delete(int id)
        {
            var product = _productRepo.GetById(id);
            if (product == null)
                return (false, "Produk tidak ditemukan.");

            _productRepo.Deactivate(id);
            return (true, $"Produk '{product.Name}' berhasil dinonaktifkan.");
        }

        /// <summary>
        /// Generate kode produk otomatis jika kosong
        /// Format: PRD-YYYYMMDD-XXXX
        /// </summary>
        public string GenerateCode()
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"PRD-{datePart}-{random}";
        }

        // ── Private ─────────────────────────────────────────────────────

        private (bool Success, string Message) Validate(Product p)
        {
            if (string.IsNullOrWhiteSpace(p.Code))
                return (false, "Kode produk tidak boleh kosong.");

            if (string.IsNullOrWhiteSpace(p.Name))
                return (false, "Nama produk tidak boleh kosong.");

            if (p.SellPrice < 0)
                return (false, "Harga jual tidak boleh negatif.");

            if (p.BuyPrice < 0)
                return (false, "Harga beli tidak boleh negatif.");

            if (p.Stock < 0)
                return (false, "Stok tidak boleh negatif.");

            return (true, "OK");
        }
    }
}
