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
    public class CategoryRepository
    {
        public List<Category> GetAll()
        {
            var list = new List<Category>();

            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, name, description, created_at FROM categories ORDER BY name";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapFromReader(reader));
            }

            return list;
        }

        /// <summary>
        /// GetAll dengan kolom tambahan: jumlah produk aktif per kategori.
        /// Digunakan untuk tampilan grid di FormCategoryList.
        /// </summary>
        public List<Category> GetAllWithProductCount()
        {
            var list = new List<Category>();

            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    c.id, c.name, c.description, c.created_at,
                    COUNT(p.id) AS product_count
                FROM categories c
                LEFT JOIN products p ON p.category_id = c.id AND p.is_active = 1
                GROUP BY c.id, c.name, c.description, c.created_at
                ORDER BY c.name";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var cat = MapFromReader(reader);
                cat.ProductCount = reader.GetInt32(4);
                list.Add(cat);
            }

            return list;
        }

        public Category GetById(int id)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, name, description, created_at FROM categories WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapFromReader(reader) : null;
        }

        public int Insert(Category category)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO categories (name, description)
                VALUES (@name, @description);
                SELECT last_insert_rowid();";

            cmd.Parameters.AddWithValue("@name", category.Name);
            cmd.Parameters.AddWithValue("@description", category.Description ?? "");

            return (int)(long)cmd.ExecuteScalar();
        }

        public void Update(Category category)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE categories
                SET name = @name, description = @description
                WHERE id = @id";

            cmd.Parameters.AddWithValue("@id", category.Id);
            cmd.Parameters.AddWithValue("@name", category.Name);
            cmd.Parameters.AddWithValue("@description", category.Description ?? "");

            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Cek apakah kategori masih dipakai oleh produk aktif.
        /// Digunakan sebelum menghapus kategori.
        /// </summary>
        public bool HasActiveProducts(int categoryId)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(*) FROM products 
                WHERE category_id = @id AND is_active = 1";
            cmd.Parameters.AddWithValue("@id", categoryId);
            return (long)cmd.ExecuteScalar() > 0;
        }

        public void Delete(int id)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM categories WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public bool IsNameExists(string name, int excludeId = 0)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM categories WHERE name = @name AND id != @excludeId";
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@excludeId", excludeId);
            return (long)cmd.ExecuteScalar() > 0;
        }

        private static Category MapFromReader(SqliteDataReader r) => new Category
        {
            Id = r.GetInt32(0),
            Name = r.GetString(1),
            Description = r.IsDBNull(2) ? "" : r.GetString(2),
            CreatedAt = r.IsDBNull(3) ? "" : r.GetString(3)
        };
    }
}
