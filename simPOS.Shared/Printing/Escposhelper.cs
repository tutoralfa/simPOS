using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Drawing.Printing;

namespace simPOS.Shared.Printing
{
    /// <summary>
    /// ESC/POS Helper untuk thermal printer.
    ///
    /// Cara kerja:
    ///   var builder = new EscPosBuilder(config.CharPerLine);
    ///   builder.Initialize()
    ///          .Center().Bold().Text("TOKO ABC").NewLine()
    ///          .Normal().Divider()
    ///          .LeftRight("TOTAL", "Rp 50.000")
    ///          .Cut();
    ///   EscPosPrinter.Print(builder.Build(), config.PrinterName);
    ///
    /// Mendukung print via:
    ///   - Windows printer (GDI raw — paling umum untuk USB thermal)
    ///   - Serial port (COM1, COM2, dll)
    /// </summary>
    public class EscPosBuilder
    {
        // ── ESC/POS Command Constants ────────────────────────────────
        private static readonly byte[] ESC = { 0x1B };
        private static readonly byte[] GS = { 0x1D };

        // Inisialisasi printer
        public static readonly byte[] CMD_INIT = { 0x1B, 0x40 };

        // Alignment
        public static readonly byte[] CMD_LEFT = { 0x1B, 0x61, 0x00 };
        public static readonly byte[] CMD_CENTER = { 0x1B, 0x61, 0x01 };
        public static readonly byte[] CMD_RIGHT = { 0x1B, 0x61, 0x02 };

        // Text style
        public static readonly byte[] CMD_BOLD_ON = { 0x1B, 0x45, 0x01 };
        public static readonly byte[] CMD_BOLD_OFF = { 0x1B, 0x45, 0x00 };
        public static readonly byte[] CMD_UNDERLINE_ON = { 0x1B, 0x2D, 0x01 };
        public static readonly byte[] CMD_UNDERLINE_OFF = { 0x1B, 0x2D, 0x00 };

        // Font size — double width/height
        public static readonly byte[] CMD_FONT_NORMAL = { 0x1D, 0x21, 0x00 };
        public static readonly byte[] CMD_FONT_2X = { 0x1D, 0x21, 0x11 }; // 2x lebar & tinggi
        public static readonly byte[] CMD_FONT_2H = { 0x1D, 0x21, 0x01 }; // 2x tinggi saja
        public static readonly byte[] CMD_FONT_2W = { 0x1D, 0x21, 0x10 }; // 2x lebar saja

        // Feed & Cut
        public static readonly byte[] CMD_NEWLINE = { 0x0A };
        public static readonly byte[] CMD_FEED_3 = { 0x1B, 0x64, 0x03 }; // feed 3 baris
        public static readonly byte[] CMD_CUT_FULL = { 0x1D, 0x56, 0x00 }; // full cut
        public static readonly byte[] CMD_CUT_PARTIAL = { 0x1D, 0x56, 0x01 }; // partial cut

        // Encoding
        public static readonly byte[] CMD_CODEPAGE_PC437 = { 0x1B, 0x74, 0x00 };
        public static readonly byte[] CMD_CODEPAGE_PC850 = { 0x1B, 0x74, 0x02 };

        // ── State ────────────────────────────────────────────────────
        private readonly List<byte[]> _parts = new List<byte[]>();
        private readonly int _charPerLine;
        private readonly Encoding _encoding;

        public EscPosBuilder(int charPerLine = 48, Encoding encoding = null)
        {
            _charPerLine = charPerLine;
            _encoding = encoding ?? Encoding.GetEncoding(437); // CP437 — kompatibel luas
        }

        // ── Fluent API ───────────────────────────────────────────────

        public EscPosBuilder Initialize()
        {
            _parts.Add(CMD_INIT);
            _parts.Add(CMD_CODEPAGE_PC437);
            return this;
        }

        // Alignment
        public EscPosBuilder Left() { _parts.Add(CMD_LEFT); return this; }
        public EscPosBuilder Center() { _parts.Add(CMD_CENTER); return this; }
        public EscPosBuilder Right() { _parts.Add(CMD_RIGHT); return this; }

        // Style
        public EscPosBuilder Bold() { _parts.Add(CMD_BOLD_ON); return this; }
        public EscPosBuilder NoBold() { _parts.Add(CMD_BOLD_OFF); return this; }
        public EscPosBuilder Underline() { _parts.Add(CMD_UNDERLINE_ON); return this; }
        public EscPosBuilder NoUnderline() { _parts.Add(CMD_UNDERLINE_OFF); return this; }
        public EscPosBuilder Normal() { _parts.Add(CMD_FONT_NORMAL); _parts.Add(CMD_BOLD_OFF); _parts.Add(CMD_LEFT); return this; }
        public EscPosBuilder Font2X() { _parts.Add(CMD_FONT_2X); return this; }
        public EscPosBuilder Font2H() { _parts.Add(CMD_FONT_2H); return this; }
        public EscPosBuilder Font2W() { _parts.Add(CMD_FONT_2W); return this; }

        // Text
        public EscPosBuilder Text(string text)
        {
            if (!string.IsNullOrEmpty(text))
                _parts.Add(_encoding.GetBytes(text));
            return this;
        }

        public EscPosBuilder NewLine()
        {
            _parts.Add(CMD_NEWLINE);
            return this;
        }

        public EscPosBuilder NewLine(int count)
        {
            for (int i = 0; i < count; i++) _parts.Add(CMD_NEWLINE);
            return this;
        }

        /// <summary>Cetak teks lalu newline.</summary>
        public EscPosBuilder TextLine(string text)
            => Text(text).NewLine();

        /// <summary>Garis pemisah sepanjang _charPerLine.</summary>
        public EscPosBuilder Divider(char c = '-')
            => TextLine(new string(c, _charPerLine));

        /// <summary>
        /// Teks kiri dan kanan dalam satu baris.
        /// Contoh: LeftRight("TOTAL", "Rp 50.000")
        /// →  TOTAL              Rp 50.000
        /// </summary>
        public EscPosBuilder LeftRight(string left, string right)
        {
            var gap = _charPerLine - left.Length - right.Length;
            var line = gap > 0
                ? left + new string(' ', gap) + right
                : $"{left} {right}";
            return TextLine(line);
        }

        /// <summary>
        /// Tiga kolom dalam satu baris.
        /// Contoh: Col3("Indomie", "2x", "Rp 5.000")
        /// </summary>
        public EscPosBuilder Col3(string col1, string col2, string col3,
            int w1 = 16, int w2 = 6)
        {
            var w3 = _charPerLine - w1 - w2;
            var c1 = col1.Length > w1 ? col1.Substring(0, w1 - 1) + "." : col1.PadRight(w1);
            var c2 = col2.PadLeft(w2);
            var c3 = col3.PadLeft(Math.Max(0, w3));
            return TextLine(c1 + c2 + c3);
        }

        /// <summary>Feed lalu cut.</summary>
        public EscPosBuilder Cut(bool partial = true)
        {
            _parts.Add(CMD_FEED_3);
            _parts.Add(partial ? CMD_CUT_PARTIAL : CMD_CUT_FULL);
            return this;
        }

        /// <summary>Kumpulkan semua bytes menjadi satu array.</summary>
        public byte[] Build()
        {
            using var ms = new MemoryStream();
            foreach (var part in _parts)
                ms.Write(part, 0, part.Length);
            return ms.ToArray();
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // PRINTER — kirim raw bytes ke printer
    // ══════════════════════════════════════════════════════════════════

    public static class EscPosPrinter
    {
        /// <summary>
        /// Kirim raw ESC/POS bytes ke printer Windows (USB/Network).
        /// Menggunakan RawPrinterHelper via Win32 API.
        /// </summary>
        public static void PrintRaw(byte[] data, string printerName)
        {
            if (string.IsNullOrWhiteSpace(printerName))
                throw new ArgumentException("Nama printer tidak boleh kosong.");

            RawPrinterHelper.SendBytesToPrinter(printerName, data);
        }

        /// <summary>
        /// Kirim raw bytes ke serial port (COM port).
        /// Cocok untuk printer yang terhubung via RS-232.
        /// </summary>
        public static void PrintSerial(byte[] data, string portName,
            int baudRate = 9600, int dataBits = 8,
            Parity parity = Parity.None, StopBits stopBits = StopBits.One)
        {
            using var port = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            port.Open();
            port.Write(data, 0, data.Length);
            port.Close();
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // WIN32 RAW PRINTER — P/Invoke untuk kirim raw bytes
    // ══════════════════════════════════════════════════════════════════

    internal static class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)] public string pDocName;
            [MarshalAs(UnmanagedType.LPStr)] public string pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)] public string pDataType;
        }

        [DllImport("winspool.drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

        [DllImport("winspool.drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "WritePrinter", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        public static void SendBytesToPrinter(string printerName, byte[] bytes)
        {
            IntPtr hPrinter = IntPtr.Zero;
            IntPtr pBytes = IntPtr.Zero;

            try
            {
                if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                    throw new InvalidOperationException($"Tidak bisa membuka printer \"{printerName}\". Pastikan printer terpasang dan nama benar.");

                var di = new DOCINFOA
                {
                    pDocName = "simPOS Receipt",
                    pDataType = "RAW"
                };

                if (!StartDocPrinter(hPrinter, 1, di))
                    throw new InvalidOperationException("StartDocPrinter gagal.");

                StartPagePrinter(hPrinter);

                pBytes = Marshal.AllocCoTaskMem(bytes.Length);
                Marshal.Copy(bytes, 0, pBytes, bytes.Length);

                WritePrinter(hPrinter, pBytes, bytes.Length, out _);

                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);
            }
            finally
            {
                if (pBytes != IntPtr.Zero) Marshal.FreeCoTaskMem(pBytes);
                if (hPrinter != IntPtr.Zero) ClosePrinter(hPrinter);
            }
        }
    }
}
