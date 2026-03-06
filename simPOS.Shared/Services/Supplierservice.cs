using simPOS.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using simPOS.Shared.Repositories;

namespace simPOS.Shared.Services
{
    public class SupplierService
    {
        private readonly SupplierRepository _repo;

        public SupplierService() => _repo = new SupplierRepository();

        public List<Supplier> GetAll() => _repo.GetAll();
        public List<Supplier> GetAllWithProductCount() => _repo.GetAllWithProductCount();
        public Supplier GetById(int id) => _repo.GetById(id);

        public List<Supplier> Search(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return _repo.GetAllWithProductCount();
            return _repo.Search(keyword);
        }

        public (bool Success, string Message) Save(Supplier supplier)
        {
            var v = Validate(supplier);
            if (!v.Success) return v;

            if (_repo.IsNameExists(supplier.Name))
                return (false, $"Supplier \"{supplier.Name}\" sudah terdaftar.");

            _repo.Insert(supplier);
            return (true, "Supplier berhasil disimpan.");
        }

        public (bool Success, string Message) Update(Supplier supplier)
        {
            var v = Validate(supplier);
            if (!v.Success) return v;

            if (_repo.IsNameExists(supplier.Name, supplier.Id))
                return (false, $"Supplier \"{supplier.Name}\" sudah terdaftar.");

            _repo.Update(supplier);
            return (true, "Supplier berhasil diperbarui.");
        }

        /// <summary>
        /// Soft delete — supplier dinonaktifkan, bukan dihapus permanen.
        /// Produk yang sudah terhubung ke supplier ini tetap aman di database.
        /// </summary>
        public (bool Success, string Message) Delete(int id)
        {
            var supplier = _repo.GetById(id);
            if (supplier == null)
                return (false, "Supplier tidak ditemukan.");

            if (_repo.HasActiveProducts(id))
                return (false, $"Supplier \"{supplier.Name}\" masih memiliki produk aktif.\nPindahkan produk ke supplier lain terlebih dahulu.");

            _repo.Deactivate(id);
            return (true, $"Supplier \"{supplier.Name}\" berhasil dinonaktifkan.");
        }

        private static (bool Success, string Message) Validate(Supplier s)
        {
            if (string.IsNullOrWhiteSpace(s.Name))
                return (false, "Nama supplier tidak boleh kosong.");

            if (!string.IsNullOrWhiteSpace(s.Email) && !s.Email.Contains("@"))
                return (false, "Format email tidak valid.");

            return (true, "OK");
        }
    }
}
