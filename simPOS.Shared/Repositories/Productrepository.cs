using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using simPOS.Shared.Database;
using simPOS.Shared.Models;

namespace simPOS.Shared.Repositories
{
    public class ProductRepository
    {
        // Query dasar dengan JOIN — dipakai ulang di semua method GET
        private const string BASE_QUERY = @"
            SELECT 
                p.id, p.category_id, p.supplier_id,
                p.code, p.name, p.description, p.unit,
                p.buy_price, p.sell_price, p.stock, p.min_stock,
                p.is_active, p.created_at, p.updated_at,
                COALESCE(c.name, '-') AS category_name,
                COALESCE(s.name, '-') AS supplier_name
            FROM products p
            LEFT JOIN categories c ON p.category_id = c.id
            LEFT JOIN suppliers  s ON p.supplier_id  = s.id";

        public List<Product> GetAll(bool includeInactive = false)
        {
            var list = new List<Product>();

            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = BASE_QUERY
                + (includeInactive ? "" : " WHERE p.is_active = 1")
                + " ORDER BY p.name";

            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(MapFromReader(reader));

            return list;
        }

        /// <summary>
        /// Pencarian fleksibel berdasarkan nama, kode, atau kategori
        /// </summary>
        public List<Product> Search(string keyword, int categoryId = 0)
        {
            var list = new List<Product>();

            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();

            var where = " WHERE p.is_active = 1";

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                where += " AND (p.name LIKE @kw OR p.code LIKE @kw)";
                cmd.Parameters.AddWithValue("@kw", $"%{keyword.Trim()}%");
            }

            if (categoryId > 0)
            {
                where += " AND p.category_id = @catId";
                cmd.Parameters.AddWithValue("@catId", categoryId);
            }

            cmd.CommandText = BASE_QUERY + where + " ORDER BY p.name";

            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(MapFromReader(reader));

            return list;
        }

        public Product GetById(int id)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = BASE_QUERY + " WHERE p.id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapFromReader(reader) : null;
        }

        public Product GetByCode(string code)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = BASE_QUERY + " WHERE p.code = @code";
            cmd.Parameters.AddWithValue("@code", code);

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapFromReader(reader) : null;
        }

        public int Insert(Product p)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO products 
                    (category_id, supplier_id, code, name, description, unit,
                     buy_price, sell_price, stock, min_stock, is_active)
                VALUES 
                    (@categoryId, @supplierId, @code, @name, @description, @unit,
                     @buyPrice, @sellPrice, @stock, @minStock, @isActive);
                SELECT last_insert_rowid();";

            AddParameters(cmd, p);
            return (int)(long)cmd.ExecuteScalar();
        }

        public void Update(Product p)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE products SET
                    category_id = @categoryId,
                    supplier_id = @supplierId,
                    code        = @code,
                    name        = @name,
                    description = @description,
                    unit        = @unit,
                    buy_price   = @buyPrice,
                    sell_price  = @sellPrice,
                    stock       = @stock,
                    min_stock   = @minStock,
                    is_active   = @isActive,
                    updated_at  = datetime('now', 'localtime')
                WHERE id = @id";

            cmd.Parameters.AddWithValue("@id", p.Id);
            AddParameters(cmd, p);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Soft delete — barang tidak benar-benar dihapus dari database
        /// </summary>
        public void Deactivate(int id)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE products SET is_active = 0 WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public bool IsCodeExists(string code, int excludeId = 0)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM products WHERE code = @code AND id != @excludeId";
            cmd.Parameters.AddWithValue("@code", code);
            cmd.Parameters.AddWithValue("@excludeId", excludeId);
            return (long)cmd.ExecuteScalar() > 0;
        }

        // ── Private Helpers ─────────────────────────────────────────────

        private static void AddParameters(SqliteCommand cmd, Product p)
        {
            cmd.Parameters.AddWithValue("@categoryId", (object)p.CategoryId ?? System.DBNull.Value);
            cmd.Parameters.AddWithValue("@supplierId", (object)p.SupplierId ?? System.DBNull.Value);
            cmd.Parameters.AddWithValue("@code", p.Code);
            cmd.Parameters.AddWithValue("@name", p.Name);
            cmd.Parameters.AddWithValue("@description", p.Description ?? "");
            cmd.Parameters.AddWithValue("@unit", p.Unit);
            cmd.Parameters.AddWithValue("@buyPrice", p.BuyPrice);
            cmd.Parameters.AddWithValue("@sellPrice", p.SellPrice);
            cmd.Parameters.AddWithValue("@stock", p.Stock);
            cmd.Parameters.AddWithValue("@minStock", p.MinStock);
            cmd.Parameters.AddWithValue("@isActive", p.IsActive ? 1 : 0);
        }

        private static Product MapFromReader(SqliteDataReader r) => new Product
        {
            Id = r.GetInt32(0),
            CategoryId = r.IsDBNull(1) ? null : r.GetInt32(1),
            SupplierId = r.IsDBNull(2) ? null : r.GetInt32(2),
            Code = r.GetString(3),
            Name = r.GetString(4),
            Description = r.IsDBNull(5) ? "" : r.GetString(5),
            Unit = r.GetString(6),
            BuyPrice = r.GetDecimal(7),
            SellPrice = r.GetDecimal(8),
            Stock = r.GetInt32(9),
            MinStock = r.GetInt32(10),
            IsActive = r.GetInt32(11) == 1,
            CreatedAt = r.IsDBNull(12) ? "" : r.GetString(12),
            UpdatedAt = r.IsDBNull(13) ? "" : r.GetString(13),
            CategoryName = r.GetString(14),
            SupplierName = r.GetString(15)
        };
    }
}
