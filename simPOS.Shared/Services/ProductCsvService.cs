using Microsoft.Data.Sqlite;
using simPOS.Shared.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simPOS.Shared.Services
{
    public class CsvImportResult
    {
        public int Inserted { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public List<string> Errors { get; set; } = new();
        public bool HasErrors => Errors.Count > 0;
    }

    public class ProductCsvService
    {
        // Kolom CSV (urutan header)
        // Kode,Nama,Deskripsi,Satuan,Kategori,Supplier,HargaBeli,HargaJual,Stok,StokMinimum,Aktif
        private static readonly string[] Headers = {
            "Kode","Nama","Deskripsi","Satuan","Kategori","Supplier",
            "HargaBeli","HargaJual","Stok","StokMinimum","Aktif"
        };

        // ── EXPORT ───────────────────────────────────────────────────

        public string ExportToCsv(string filePath)
        {
            var rows = LoadAllProducts();

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", Headers));

            foreach (var r in rows)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    CsvEncode(r.Code),
                    CsvEncode(r.Name),
                    CsvEncode(r.Description),
                    CsvEncode(r.Unit),
                    CsvEncode(r.CategoryName),
                    CsvEncode(r.SupplierName),
                    r.BuyPrice.ToString("F0"),
                    r.SellPrice.ToString("F0"),
                    r.Stock.ToString(),
                    r.MinStock.ToString(),
                    r.IsActive ? "1" : "0"
                }));
            }

            File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true)); // BOM agar Excel baca UTF-8
            return filePath;
        }

        /// Buat file template CSV kosong dengan header + 1 baris contoh
        public string ExportTemplate(string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", Headers));
            sb.AppendLine("BRG001,Contoh Barang,Deskripsi opsional,pcs,Kategori A,Supplier X,10000,15000,100,10,1");
            File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true));
            return filePath;
        }

        // ── IMPORT ───────────────────────────────────────────────────

        /// <summary>
        /// Import dari CSV. Mode:
        /// - insertOnly   : hanya tambah baru, skip jika kode sudah ada
        /// - upsert       : tambah baru + update yang sudah ada
        /// </summary>
        public CsvImportResult ImportFromCsv(string filePath, bool upsert = false)
        {
            var result = new CsvImportResult();
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);

            if (lines.Length < 2)
            {
                result.Errors.Add("File CSV kosong atau hanya berisi header.");
                return result;
            }

            // Validasi header
            var header = lines[0].Split(',');
            if (!ValidateHeader(header, out string headerErr))
            {
                result.Errors.Add(headerErr);
                return result;
            }

            // Cache kategori & supplier name→id (buat jika belum ada)
            var catCache = LoadOrCreateCategories();
            var supCache = LoadOrCreateSuppliers();

            using var conn = DatabaseHelper.GetConnection();
            using var trx = conn.BeginTransaction();

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var cols = ParseCsvLine(line);
                if (cols.Length < 8)
                {
                    result.Errors.Add($"Baris {i + 1}: kolom tidak lengkap (minimum 8 kolom).");
                    result.Skipped++;
                    continue;
                }

                string code = cols[0].Trim();
                string name = cols[1].Trim();

                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
                {
                    result.Errors.Add($"Baris {i + 1}: Kode dan Nama tidak boleh kosong.");
                    result.Skipped++;
                    continue;
                }

                if (!decimal.TryParse(cols[6], out decimal buyPrice)) buyPrice = 0;
                if (!decimal.TryParse(cols[7], out decimal sellPrice)) sellPrice = 0;
                int stock = cols.Length > 8 && int.TryParse(cols[8], out int s) ? s : 0;
                int minStock = cols.Length > 9 && int.TryParse(cols[9], out int ms) ? ms : 0;
                int isActive = cols.Length > 10 && cols[10].Trim() == "0" ? 0 : 1;

                string catName = cols.Length > 4 ? cols[4].Trim() : "";
                string supName = cols.Length > 5 ? cols[5].Trim() : "";

                int? catId = string.IsNullOrEmpty(catName) ? null
                    : (int?)GetOrCreate(conn, trx, "categories", "name", catName, catCache);
                int? supId = string.IsNullOrEmpty(supName) ? null
                    : (int?)GetOrCreate(conn, trx, "suppliers", "name", supName, supCache);

                // Cek apakah kode sudah ada
                bool exists = ProductExists(conn, trx, code);

                if (exists && !upsert)
                {
                    result.Skipped++;
                    continue;
                }

                try
                {
                    if (exists)
                    {
                        UpdateProduct(conn, trx, code, name,
                            cols.Length > 2 ? cols[2].Trim() : "",
                            cols.Length > 3 ? cols[3].Trim() : "pcs",
                            catId, supId, buyPrice, sellPrice, stock, minStock, isActive);
                        result.Updated++;
                    }
                    else
                    {
                        InsertProduct(conn, trx, code, name,
                            cols.Length > 2 ? cols[2].Trim() : "",
                            cols.Length > 3 ? cols[3].Trim() : "pcs",
                            catId, supId, buyPrice, sellPrice, stock, minStock, isActive);
                        result.Inserted++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Baris {i + 1} ({code}): {ex.Message}");
                    result.Skipped++;
                }
            }

            trx.Commit();
            return result;
        }

        // ── DB HELPERS ───────────────────────────────────────────────

        private List<ProductCsvRow> LoadAllProducts()
        {
            var list = new List<ProductCsvRow>();
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT p.code, p.name, COALESCE(p.description,''), p.unit,
                       COALESCE(c.name,''), COALESCE(s.name,''),
                       p.buy_price, p.sell_price, p.stock, p.min_stock, p.is_active
                FROM products p
                LEFT JOIN categories c ON c.id = p.category_id
                LEFT JOIN suppliers  s ON s.id = p.supplier_id
                ORDER BY p.code";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new ProductCsvRow
                {
                    Code = r.GetString(0),
                    Name = r.GetString(1),
                    Description = r.GetString(2),
                    Unit = r.GetString(3),
                    CategoryName = r.GetString(4),
                    SupplierName = r.GetString(5),
                    BuyPrice = r.GetDecimal(6),
                    SellPrice = r.GetDecimal(7),
                    Stock = r.GetInt32(8),
                    MinStock = r.GetInt32(9),
                    IsActive = r.GetInt32(10) == 1
                });
            return list;
        }

        private Dictionary<string, int> LoadOrCreateCategories()
        {
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, name FROM categories";
            using var r = cmd.ExecuteReader();
            while (r.Read()) dict[r.GetString(1)] = r.GetInt32(0);
            return dict;
        }

        private Dictionary<string, int> LoadOrCreateSuppliers()
        {
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            using var conn = DatabaseHelper.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, name FROM suppliers";
            using var r = cmd.ExecuteReader();
            while (r.Read()) dict[r.GetString(1)] = r.GetInt32(0);
            return dict;
        }

        private int GetOrCreate(SqliteConnection conn, SqliteTransaction trx,
            string table, string col, string value, Dictionary<string, int> cache)
        {
            if (cache.TryGetValue(value, out int id)) return id;

            using var cmd = conn.CreateCommand();
            cmd.Transaction = trx;
            cmd.CommandText = $"INSERT OR IGNORE INTO {table} ({col}) VALUES (@v); " +
                              $"SELECT id FROM {table} WHERE {col} = @v";
            cmd.Parameters.AddWithValue("@v", value);
            id = Convert.ToInt32(cmd.ExecuteScalar());
            cache[value] = id;
            return id;
        }

        private bool ProductExists(SqliteConnection conn, SqliteTransaction trx, string code)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trx;
            cmd.CommandText = "SELECT COUNT(*) FROM products WHERE code = @code";
            cmd.Parameters.AddWithValue("@code", code);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private void InsertProduct(SqliteConnection conn, SqliteTransaction trx,
            string code, string name, string desc, string unit,
            int? catId, int? supId, decimal buy, decimal sell,
            int stock, int minStock, int isActive)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trx;
            cmd.CommandText = @"
                INSERT INTO products
                    (code, name, description, unit, category_id, supplier_id,
                     buy_price, sell_price, stock, min_stock, is_active)
                VALUES
                    (@code,@name,@desc,@unit,@cat,@sup,@buy,@sell,@stock,@min,@active)";
            cmd.Parameters.AddWithValue("@code", code);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@desc", desc);
            cmd.Parameters.AddWithValue("@unit", string.IsNullOrEmpty(unit) ? "pcs" : unit);
            cmd.Parameters.AddWithValue("@cat", (object?)catId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sup", (object?)supId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@buy", buy);
            cmd.Parameters.AddWithValue("@sell", sell);
            cmd.Parameters.AddWithValue("@stock", stock);
            cmd.Parameters.AddWithValue("@min", minStock);
            cmd.Parameters.AddWithValue("@active", isActive);
            cmd.ExecuteNonQuery();
        }

        private void UpdateProduct(SqliteConnection conn, SqliteTransaction trx,
            string code, string name, string desc, string unit,
            int? catId, int? supId, decimal buy, decimal sell,
            int stock, int minStock, int isActive)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trx;
            cmd.CommandText = @"
                UPDATE products SET
                    name        = @name,
                    description = @desc,
                    unit        = @unit,
                    category_id = @cat,
                    supplier_id = @sup,
                    buy_price   = @buy,
                    sell_price  = @sell,
                    stock       = @stock,
                    min_stock   = @min,
                    is_active   = @active,
                    updated_at  = datetime('now','localtime')
                WHERE code = @code";
            cmd.Parameters.AddWithValue("@code", code);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@desc", desc);
            cmd.Parameters.AddWithValue("@unit", string.IsNullOrEmpty(unit) ? "pcs" : unit);
            cmd.Parameters.AddWithValue("@cat", (object?)catId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sup", (object?)supId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@buy", buy);
            cmd.Parameters.AddWithValue("@sell", sell);
            cmd.Parameters.AddWithValue("@stock", stock);
            cmd.Parameters.AddWithValue("@min", minStock);
            cmd.Parameters.AddWithValue("@active", isActive);
            cmd.ExecuteNonQuery();
        }

        // ── CSV HELPERS ──────────────────────────────────────────────

        private static string CsvEncode(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }

        private static string[] ParseCsvLine(string line)
        {
            // Sederhana: handle quoted fields
            var fields = new List<string>();
            int i = 0;
            while (i <= line.Length)
            {
                if (i == line.Length) { fields.Add(""); break; }
                if (line[i] == '"')
                {
                    int j = ++i;
                    var sb = new StringBuilder();
                    while (i < line.Length)
                    {
                        if (line[i] == '"' && i + 1 < line.Length && line[i + 1] == '"')
                        { sb.Append('"'); i += 2; }
                        else if (line[i] == '"') { i++; break; }
                        else { sb.Append(line[i++]); }
                    }
                    fields.Add(sb.ToString());
                    if (i < line.Length && line[i] == ',') i++;
                }
                else
                {
                    int j = line.IndexOf(',', i);
                    if (j < 0) { fields.Add(line.Substring(i)); break; }
                    fields.Add(line.Substring(i, j - i));
                    i = j + 1;
                }
            }
            return fields.ToArray();
        }

        private static bool ValidateHeader(string[] header, out string error)
        {
            var required = new[] { "Kode", "Nama", "HargaBeli", "HargaJual" };
            foreach (var req in required)
            {
                bool found = false;
                foreach (var h in header)
                    if (h.Trim().Equals(req, StringComparison.OrdinalIgnoreCase))
                    { found = true; break; }
                if (!found)
                {
                    error = $"Header CSV tidak valid. Kolom '{req}' tidak ditemukan. " +
                             "Gunakan tombol 'Unduh Template' untuk format yang benar.";
                    return false;
                }
            }
            error = "";
            return true;
        }
    }

    internal class ProductCsvRow
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public string CategoryName { get; set; }
        public string SupplierName { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public int Stock { get; set; }
        public int MinStock { get; set; }
        public bool IsActive { get; set; }
    }
}
