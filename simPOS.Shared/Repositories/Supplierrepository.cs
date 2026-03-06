using Microsoft.Data.Sqlite;
using simPOS.Shared.Database;
using simPOS.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace simPOS.Shared.Repositories
{
    public class SupplierRepository
    {
        private const string BASE_QUERY = @"
            SELECT id, name, contact_person, phone, email, address, is_active, created_at
            FROM suppliers";

        public List<Supplier> GetAll(bool includeInactive = false)
        {
            var list = new List<Supplier>();
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = BASE_QUERY
                + (includeInactive ? "" : " WHERE is_active = 1")
                + " ORDER BY name";
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(MapFromReader(reader));
            return list;
        }

        public List<Supplier> GetAllWithProductCount()
        {
            var list = new List<Supplier>();
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT s.id, s.name, s.contact_person, s.phone, s.email,
                       s.address, s.is_active, s.created_at, COUNT(p.id) AS product_count
                FROM suppliers s
                LEFT JOIN products p ON p.supplier_id = s.id AND p.is_active = 1
                WHERE s.is_active = 1
                GROUP BY s.id, s.name, s.contact_person, s.phone,
                         s.email, s.address, s.is_active, s.created_at
                ORDER BY s.name";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var s = MapFromReader(reader);
                s.ProductCount = reader.GetInt32(8);
                list.Add(s);
            }
            return list;
        }

        public List<Supplier> Search(string keyword)
        {
            var list = new List<Supplier>();
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT s.id, s.name, s.contact_person, s.phone, s.email,
                       s.address, s.is_active, s.created_at, COUNT(p.id) AS product_count
                FROM suppliers s
                LEFT JOIN products p ON p.supplier_id = s.id AND p.is_active = 1
                WHERE s.is_active = 1
                  AND (s.name LIKE @kw OR s.contact_person LIKE @kw OR s.phone LIKE @kw)
                GROUP BY s.id, s.name, s.contact_person, s.phone,
                         s.email, s.address, s.is_active, s.created_at
                ORDER BY s.name";
            cmd.Parameters.AddWithValue("@kw", $"%{keyword.Trim()}%");
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var s = MapFromReader(reader);
                s.ProductCount = reader.GetInt32(8);
                list.Add(s);
            }
            return list;
        }

        public Supplier GetById(int id)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = BASE_QUERY + " WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapFromReader(reader) : null;
        }

        public int Insert(Supplier s)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO suppliers (name, contact_person, phone, email, address, is_active)
                VALUES (@name, @cp, @phone, @email, @address, @active);
                SELECT last_insert_rowid();";
            AddParameters(cmd, s);
            return (int)(long)cmd.ExecuteScalar();
        }

        public void Update(Supplier s)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE suppliers SET
                    name = @name, contact_person = @cp, phone = @phone,
                    email = @email, address = @address, is_active = @active
                WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", s.Id);
            AddParameters(cmd, s);
            cmd.ExecuteNonQuery();
        }

        public void Deactivate(int id)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE suppliers SET is_active = 0 WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public bool HasActiveProducts(int supplierId)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM products WHERE supplier_id = @id AND is_active = 1";
            cmd.Parameters.AddWithValue("@id", supplierId);
            return (long)cmd.ExecuteScalar() > 0;
        }

        public bool IsNameExists(string name, int excludeId = 0)
        {
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM suppliers WHERE name = @name AND id != @excludeId AND is_active = 1";
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@excludeId", excludeId);
            return (long)cmd.ExecuteScalar() > 0;
        }

        private static void AddParameters(SqliteCommand cmd, Supplier s)
        {
            cmd.Parameters.AddWithValue("@name", s.Name);
            cmd.Parameters.AddWithValue("@cp", s.ContactPerson ?? "");
            cmd.Parameters.AddWithValue("@phone", s.Phone ?? "");
            cmd.Parameters.AddWithValue("@email", s.Email ?? "");
            cmd.Parameters.AddWithValue("@address", s.Address ?? "");
            cmd.Parameters.AddWithValue("@active", s.IsActive ? 1 : 0);
        }

        private static Supplier MapFromReader(SqliteDataReader r) => new Supplier
        {
            Id = r.GetInt32(0),
            Name = r.GetString(1),
            ContactPerson = r.IsDBNull(2) ? "" : r.GetString(2),
            Phone = r.IsDBNull(3) ? "" : r.GetString(3),
            Email = r.IsDBNull(4) ? "" : r.GetString(4),
            Address = r.IsDBNull(5) ? "" : r.GetString(5),
            IsActive = r.GetInt32(6) == 1,
            CreatedAt = r.IsDBNull(7) ? "" : r.GetString(7)
        };
    }
}
