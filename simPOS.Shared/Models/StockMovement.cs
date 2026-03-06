using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simPOS.Shared.Models
{
    public class StockMovement
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Type { get; set; } = "IN"; // IN | OUT | ADJUSTMENT
        public int Quantity { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string ReferenceNo { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;

        // Join
        public string ProductName { get; set; } = string.Empty;
    }
}
