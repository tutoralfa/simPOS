using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using simPOS.Shared.Models;
using simPOS.Shared.Repositories;

namespace simPOS.Shared.Services
{
    public class GoodsReceiptService
    {
        private readonly GoodsReceiptRepository _repo;

        public GoodsReceiptService() => _repo = new GoodsReceiptRepository();

        public List<GoodsReceipt> GetAll() => _repo.GetAll();
        public GoodsReceipt GetById(int id) => _repo.GetById(id);

        /// <summary>
        /// Simpan penerimaan barang. Validasi dilakukan sebelum menyentuh DB.
        /// </summary>
        public (bool Success, string Message) Save(GoodsReceipt receipt)
        {
            if (string.IsNullOrWhiteSpace(receipt.ReceiptNo))
                return (false, "Nomor penerimaan tidak boleh kosong.");

            if (_repo.IsReceiptNoExists(receipt.ReceiptNo))
                return (false, $"Nomor penerimaan \"{receipt.ReceiptNo}\" sudah digunakan.");

            if (receipt.Items == null || receipt.Items.Count == 0)
                return (false, "Tambahkan minimal 1 barang sebelum menyimpan.");

            foreach (var item in receipt.Items)
            {
                if (item.Quantity <= 0)
                    return (false, $"Jumlah untuk \"{item.ProductName}\" harus lebih dari 0.");
                if (item.BuyPrice < 0)
                    return (false, $"Harga beli untuk \"{item.ProductName}\" tidak boleh negatif.");
            }

            try
            {
                _repo.Insert(receipt);
                return (true, $"Penerimaan \"{receipt.ReceiptNo}\" berhasil disimpan. {receipt.Items.Count} barang diperbarui.");
            }
            catch (Exception ex)
            {
                return (false, $"Gagal menyimpan: {ex.Message}");
            }
        }

        /// <summary>
        /// Generate nomor penerimaan otomatis.
        /// Format: GR-YYYYMMDD-XXXX
        /// </summary>
        public string GenerateReceiptNo()
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var seq = new Random().Next(1000, 9999);
            return $"GR-{datePart}-{seq}";
        }
    }
}
