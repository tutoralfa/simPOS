using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using simPOS.Shared.Database;
using System.Text.Json;

namespace simPOS.Shared.Printing
{
    /// <summary>
    /// Konfigurasi printer thermal yang disimpan di AppData.
    /// Dipakai bersama oleh simPOS.Management dan simPOS.POS.
    /// File: %AppData%\Local\simPOS\printer.json
    /// </summary>
    public class PrinterConfig
    {
        // ── Setting yang disimpan ────────────────────────────────────
        public string PrinterName { get; set; } = "";   // nama printer di Windows
        public int PaperWidth { get; set; } = 80;   // 58 atau 80 mm
        public int CharPerLine { get; set; } = 48;   // karakter per baris (80mm=48, 58mm=32)
        public string StoreName { get; set; } = "sim POS";
        public string StoreAddress { get; set; } = "";
        public string StorePhone { get; set; } = "";
        public string FooterMessage { get; set; } = "Terima kasih sudah berbelanja!";
        public bool AutoCut { get; set; } = true;
        public bool PrintEnabled { get; set; } = true;

        // ── Persistence ──────────────────────────────────────────────
        private static readonly string ConfigPath =
            Path.Combine(AppConfig.DataFolder, "printer.json");

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public static PrinterConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<PrinterConfig>(json) ?? new PrinterConfig();
                }
            }
            catch { /* kembalikan default jika file corrupt */ }

            return new PrinterConfig();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(AppConfig.DataFolder);
                var json = JsonSerializer.Serialize(this, _jsonOpts);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                throw new IOException($"Gagal menyimpan konfigurasi printer: {ex.Message}", ex);
            }
        }
    }
}
