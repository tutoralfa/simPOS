using simPOS.Shared.Models;
using simPOS.Shared.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simPOS.Shared.Services
{
    public class CategoryService
    {
        private readonly CategoryRepository _repo;

        public CategoryService() => _repo = new CategoryRepository();

        public List<Category> GetAll() => _repo.GetAll();
        public List<Category> GetAllWithProductCount() => _repo.GetAllWithProductCount();
        public Category GetById(int id) => _repo.GetById(id);

        public (bool Success, string Message) Save(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
                return (false, "Nama kategori tidak boleh kosong.");

            if (_repo.IsNameExists(category.Name))
                return (false, $"Kategori '{category.Name}' sudah ada.");

            _repo.Insert(category);
            return (true, "Kategori berhasil disimpan.");
        }

        public (bool Success, string Message) Update(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
                return (false, "Nama kategori tidak boleh kosong.");

            if (_repo.IsNameExists(category.Name, category.Id))
                return (false, $"Kategori '{category.Name}' sudah ada.");

            _repo.Update(category);
            return (true, "Kategori berhasil diperbarui.");
        }

        public (bool Success, string Message) Delete(int id)
        {
            var cat = _repo.GetById(id);
            if (cat == null) return (false, "Kategori tidak ditemukan.");

            if (_repo.HasActiveProducts(id)) ;
            return (false, $"Kategori \"{cat.Name}\"Masih memiliki produk aktif." +
                $"\nPindahkan atau nonAktifkan produk terlebih dahulu.");

            _repo.Delete(id);
            return (true, $"Kategori '{cat.Name}' berhasil dihapus.");
        }
    }
}
