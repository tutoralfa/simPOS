using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simPOS.Shared.Models
{
    public class Product
    {
        public int Id { get; set; }
        public int? CategoryId { get; set; }
        public int? SupplierId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Unit { get; set; } = "pcs";
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public int Stock { get; set; }
        public int MinStock { get; set; }
        public bool IsActive { get; set; } = true;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;

        // Join properties — diisi saat query dengan JOIN
        public string CategoryName { get; set; } = "-";
        public string SupplierName { get; set; } = "-";

        // Helper untuk tampilan
        public bool IsLowStock => Stock <= MinStock;
    }
}
