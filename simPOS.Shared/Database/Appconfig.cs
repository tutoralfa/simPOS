using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simPOS.Shared.Database
{
    /// <summary>
    /// Konfigurasi path database yang dipakai bersama oleh
    /// simPOS.Management dan simPOS.POS.
    ///
    /// Database disimpan di folder AppData\Local\simPOS milik user
    /// sehingga tidak tergantung lokasi instalasi executable.
    /// Kedua app selalu menunjuk ke file yang sama.
    /// </summary>
    public static class AppConfig
    {
        /// <summary>
        /// Folder data aplikasi:
        /// C:\Users\[username]\AppData\Local\simPOS\
        /// </summary>
        public static string DataFolder =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "simPOS"
            );

        /// <summary>
        /// Path lengkap file database SQLite.
        /// C:\Users\[username]\AppData\Local\simPOS\dbSPOS.sqlite
        /// </summary>
        public static string DatabasePath =>
            Path.Combine(DataFolder, "dbSPOS.sqlite");
    }
}
