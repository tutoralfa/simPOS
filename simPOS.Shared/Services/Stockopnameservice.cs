using simPOS.Shared.Models;
using simPOS.Shared.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simPOS.Shared.Services
{
    public class StockOpnameService
    {
        private readonly StockOpnameRepository _opnameRepo;
        private readonly ProductRepository _productRepo;

        public StockOpnameService()
        {
            _opnameRepo = new StockOpnameRepository();
            _productRepo = new ProductRepository();
        }

        public List<StockOpname> GetAll() => _opnameRepo.GetAll();
        public StockOpname GetById(int id) => _opnameRepo.GetById(id);

        /// <summary>
        /// Buat sesi opname baru dengan snapshot stok saat ini.
        /// Mengembalikan objek opname lengkap beserta semua item produk aktif.
        /// </summary>
        public (bool Success, string Message, StockOpname Opname) CreateSession(string opnameNo, string notes)
        {
            if (string.IsNullOrWhiteSpace(opnameNo))
                return (false, "Nomor opname tidak boleh kosong.", null);

            if (_opnameRepo.IsOpnameNoExists(opnameNo))
                return (false, $"Nomor opname \"{opnameNo}\" sudah digunakan.", null);

            // Simpan header ke DB
            var opname = new StockOpname { OpnameNo = opnameNo, Notes = notes };
            int id = _opnameRepo.CreateDraft(opname);
            opname.Id = id;

            // Snapshot semua produk aktif ke Items — hanya di memory
            // PhysicalStock diisi sama dengan SystemStock sebagai default
            var products = _productRepo.GetAll();
            foreach (var p in products)
            {
                opname.Items.Add(new StockOpnameItem
                {
                    ProductId = p.Id,
                    ProductCode = p.Code,
                    ProductName = p.Name,
                    Unit = p.Unit,
                    SystemStock = p.Stock,
                    PhysicalStock = p.Stock  // default = sama, user edit yg berbeda
                });
            }

            return (true, $"Sesi opname \"{opnameNo}\" dibuat. {products.Count} produk dimuat.", opname);
        }

        /// <summary>
        /// Konfirmasi opname — simpan adjustment dan update stok.
        /// Hanya produk yang stok fisiknya berbeda dari sistem yang diproses.
        /// </summary>
        public (bool Success, string Message) Confirm(StockOpname opname)
        {
            if (opname.IsConfirmed)
                return (false, "Opname ini sudah dikonfirmasi sebelumnya.");

            var changed = opname.Items.FindAll(i => i.Difference != 0);

            try
            {
                _opnameRepo.Confirm(opname.Id, opname.OpnameNo, opname.Items);
                return (true, $"Opname \"{opname.OpnameNo}\" dikonfirmasi.\n{changed.Count} produk stoknya disesuaikan.");
            }
            catch (Exception ex)
            {
                return (false, $"Gagal konfirmasi: {ex.Message}");
            }
        }

        public string GenerateOpnameNo()
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var seq = new Random().Next(100, 999);
            return $"OPN-{datePart}-{seq}";
        }
    }
}
