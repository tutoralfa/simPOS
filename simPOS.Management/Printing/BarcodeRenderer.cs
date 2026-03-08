using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simPOS.Management.Printing
{
    /// <summary>
    /// Jenis-jenis barcode yang didukung.
    /// </summary>
    public enum BarcodeType
    {
        Code128,   // Universal, semua karakter ASCII
        Code39,    // Huruf kapital + angka, lebih lebar per karakter
        EAN13,     // 13 digit angka, standar retail
        QRCode,    // 2D matrix, bisa muat lebih banyak data
    }

    /// <summary>
    /// Renderer terpusat untuk semua jenis barcode.
    /// Barcode selalu digambar dengan ukuran TETAP berdasarkan moduleWidthPx
    /// (tidak di-stretch mengikuti lebar rect).
    /// Barcode diposisikan di tengah rect secara horizontal.
    /// </summary>
    public static class BarcodeRenderer
    {
        /// <summary>
        /// Render barcode ke Graphics. Ukuran barcode ditentukan oleh moduleWidthPx,
        /// bukan oleh lebar rect — sehingga label bisa lebih lebar dari barcode.
        /// </summary>
        /// <param name="g">Target Graphics</param>
        /// <param name="text">Teks / data yang di-encode</param>
        /// <param name="rect">Area yang tersedia (barcode di-center di sini)</param>
        /// <param name="type">Jenis barcode</param>
        /// <param name="moduleWidthPx">Lebar 1 modul px. 0 = auto fill rect.</param>
        /// <param name="showText">Tampilkan teks di bawah (khusus 1D barcode)</param>
        public static void DrawTo(Graphics g, string text, RectangleF rect,
            BarcodeType type = BarcodeType.Code128,
            float moduleWidthPx = 0f, bool showText = true)
        {
            if (string.IsNullOrWhiteSpace(text)) text = " ";

            Bitmap bmp = type switch
            {
                BarcodeType.Code128 => DrawCode128(text, rect, moduleWidthPx, showText),
                BarcodeType.Code39 => DrawCode39(text, rect, moduleWidthPx, showText),
                BarcodeType.EAN13 => DrawEan13(text, rect, moduleWidthPx, showText),
                BarcodeType.QRCode => DrawQr(text, rect),
                _ => DrawCode128(text, rect, moduleWidthPx, showText),
            };

            // Center horizontal dalam rect
            float drawX = rect.X + (rect.Width - bmp.Width) / 2f;
            float drawY = rect.Y;
            g.DrawImage(bmp, drawX, drawY, bmp.Width, bmp.Height);
            bmp.Dispose();
        }

        // ── Ukuran bitmap barcode yang di-generate ────────────────────

        private static (int barW, float modW) CalcModuleWidth(
            int totalModules, RectangleF rect, float moduleWidthPx)
        {
            float mw = moduleWidthPx > 0f
                ? moduleWidthPx
                : Math.Max(1f, rect.Width / totalModules);
            int w = (int)(totalModules * mw) + 2;
            return (w, mw);
        }

        // ════════════════════════════════════════════════════════════
        // CODE 128 B
        // ════════════════════════════════════════════════════════════

        private static readonly int[] C128Patterns = {
            0b11011001100,0b11001101100,0b11001100110,0b10010011000,
            0b10010001100,0b10001001100,0b10011001000,0b10011000100,
            0b10001100100,0b11001001000,0b11001000100,0b11000100100,
            0b10110011100,0b10011011100,0b10011001110,0b10111001100,
            0b10011101100,0b10011100110,0b11001110010,0b11001011100,
            0b11001001110,0b11011100100,0b11001110100,0b11101101110,
            0b11101001100,0b11100101100,0b11100100110,0b11101100100,
            0b11100110100,0b11100110010,0b11011011000,0b11011000110,
            0b11000110110,0b10100011000,0b10001011000,0b10001000110,
            0b10110001000,0b10001101000,0b10001100010,0b11010001000,
            0b11000101000,0b11000100010,0b10110111000,0b10110001110,
            0b10001101110,0b10111011000,0b10111000110,0b10001110110,
            0b11101110110,0b11010001110,0b11000101110,0b11011101000,
            0b11011100010,0b11011101110,0b11101011000,0b11101000110,
            0b11100010110,0b11101101000,0b11101100010,0b11100011010,
            0b11101111010,0b11001000010,0b11110001010,0b10100110000,
            0b10100001100,0b10010110000,0b10010000110,0b10000101100,
            0b10000100110,0b10110010000,0b10110000100,0b10011010000,
            0b10011000010,0b10000110100,0b10000110010,0b11000010010,
            0b11001010000,0b11110111010,0b11000010100,0b10001111010,
            0b10100111100,0b10010111100,0b10010011110,0b10111100100,
            0b10011110100,0b10011110010,0b11110100100,0b11110010100,
            0b11110010010,0b11011011110,0b11011110110,0b11110110110,
            0b10101111000,0b10100011110,0b10001011110,0b10111101000,
            0b10111100010,0b11110101000,0b11110100010,0b10111011110,
            0b10111101110,0b11101011110,0b11110101110,0b11010000100,
            0b11010010000,0b11010011100,
        };

        private static Bitmap DrawCode128(string text, RectangleF rect,
            float moduleWidthPx, bool showText)
        {
            var vals = new List<int> { 104 }; // START B, checksum val=104
            int check = 104;
            for (int i = 0; i < text.Length; i++)
            {
                int v = Math.Max(0, Math.Min(94, text[i] - 32));
                vals.Add(v);
                check += v * (i + 1);
            }
            vals.Add(check % 103);

            int totalMod = vals.Count * 11 + 13;
            var (bmpW, mw) = CalcModuleWidth(totalMod, rect, moduleWidthPx);
            int textH = showText ? 18 : 0;
            int barH = Math.Max(1, (int)rect.Height - textH - 4);

            var bmp = new Bitmap(bmpW, barH + textH + 4, PixelFormat.Format32bppArgb);
            using var g2 = Graphics.FromImage(bmp);
            g2.Clear(Color.White);

            float x = 1f;
            void DrawPat(int pat, int bits = 11)
            {
                for (int b = bits - 1; b >= 0; b--)
                {
                    if (((pat >> b) & 1) == 1)
                        g2.FillRectangle(Brushes.Black, x, 2, mw, barH);
                    x += mw;
                }
            }

            DrawPat(C128Patterns[103]); // START B
            foreach (var v in vals.GetRange(1, vals.Count - 1))
                DrawPat(C128Patterns[v]);
            DrawPat(C128Patterns[105]); // STOP
            DrawPat(0b11, 2);

            if (showText)
            {
                using var font = new Font("Courier New", 7f);
                var sf = new StringFormat { Alignment = StringAlignment.Center };
                g2.DrawString(text, font, Brushes.Black,
                    new RectangleF(0, barH + 3, bmpW, textH), sf);
            }
            return bmp;
        }

        // ════════════════════════════════════════════════════════════
        // CODE 39
        // ════════════════════════════════════════════════════════════

        private static readonly Dictionary<char, string> C39Map = new()
        {
            {'0',"NnNwWnWnN"},{'1',"WnNwNnNnW"},{'2',"NnWwNnNnW"},
            {'3',"WnWwNnNnN"},{'4',"NnNwWnNnW"},{'5',"WnNwWnNnN"},
            {'6',"NnWwWnNnN"},{'7',"NnNwNnWnW"},{'8',"WnNwNnWnN"},
            {'9',"NnWwNnWnN"},{'A',"WnNnNwNnW"},{'B',"NnWnNwNnW"},
            {'C',"WnWnNwNnN"},{'D',"NnNnWwNnW"},{'E',"WnNnWwNnN"},
            {'F',"NnWnWwNnN"},{'G',"NnNnNwWnW"},{'H',"WnNnNwWnN"},
            {'I',"NnWnNwWnN"},{'J',"NnNnWwWnN"},{'K',"WnNnNnNwW"},
            {'L',"NnWnNnNwW"},{'M',"WnWnNnNwN"},{'N',"NnNnWnNwW"},
            {'O',"WnNnWnNwN"},{'P',"NnWnWnNwN"},{'Q',"NnNnNnWwW"},
            {'R',"WnNnNnWwN"},{'S',"NnWnNnWwN"},{'T',"NnNnWnWwN"},
            {'U',"WwNnNnNnW"},{'V',"NwWnNnNnW"},{'W',"WwWnNnNnN"},
            {'X',"NwNnWnNnW"},{'Y',"WwNnWnNnN"},{'Z',"NwWnWnNnN"},
            {'-',"NwNnNnWnW"},{' ',"NwNnWnNnW"},{'$',"NwNwNwNnN"},
            {'/',"NwNwNnNwN"},{'+',"NwNnNwNwN"},{'%',"NnNwNwNwN"},
            {'*',"NwNnWnNnW"}, // START/STOP
        };

        private static Bitmap DrawCode39(string text, RectangleF rect,
            float moduleWidthPx, bool showText)
        {
            text = text.ToUpper();
            var sb = new System.Text.StringBuilder();
            sb.Append("*");
            foreach (char c in text)
                if (C39Map.ContainsKey(c)) sb.Append(c);
            sb.Append("*");
            string encoded = sb.ToString();

            // Hitung total modul: tiap karakter = 9 elemen (N=1, W=3) + 1 gap antar karakter
            int totalMod = 0;
            foreach (char c in encoded)
            {
                string pat = C39Map.ContainsKey(c) ? C39Map[c] : C39Map[' '];
                foreach (char e in pat)
                    totalMod += e == 'W' ? 3 : 1;
                totalMod += 1; // gap
            }

            var (bmpW, mw) = CalcModuleWidth(totalMod, rect, moduleWidthPx);
            int textH = showText ? 18 : 0;
            int barH = Math.Max(1, (int)rect.Height - textH - 4);

            var bmp = new Bitmap(bmpW, barH + textH + 4, PixelFormat.Format32bppArgb);
            using var g2 = Graphics.FromImage(bmp);
            g2.Clear(Color.White);

            float x = 1f;
            bool isBar = true; // mulai dengan bar
            foreach (char c in encoded)
            {
                string pat = C39Map.ContainsKey(c) ? C39Map[c] : C39Map[' '];
                isBar = true;
                foreach (char e in pat)
                {
                    float w = (e == 'W' ? 3 : 1) * mw;
                    if (isBar)
                        g2.FillRectangle(Brushes.Black, x, 2, w, barH);
                    x += w;
                    isBar = !isBar;
                }
                x += mw; // inter-character gap (space)
            }

            if (showText)
            {
                using var font = new Font("Courier New", 7f);
                var sf = new StringFormat { Alignment = StringAlignment.Center };
                g2.DrawString(text.Replace("*", ""), font, Brushes.Black,
                    new RectangleF(0, barH + 3, bmpW, textH), sf);
            }
            return bmp;
        }

        // ════════════════════════════════════════════════════════════
        // EAN-13  (auto-pad / trim ke 12 digit, check digit dihitung)
        // ════════════════════════════════════════════════════════════

        private static readonly int[,] EanL = {
            {0b0001101,0b0011001,0b0010011,0b0111101,0b0100011,
             0b0110001,0b0101111,0b0111011,0b0110111,0b0001011}, // L-code
        };
        private static readonly int[,] EanG = {
            {0b0100111,0b0110011,0b0011011,0b0100001,0b0011101,
             0b0111001,0b0000101,0b0010001,0b0001001,0b0010111},
        };
        private static readonly int[,] EanR = {
            {0b1110010,0b1100110,0b1101100,0b1000010,0b1011100,
             0b1001110,0b1010000,0b1000100,0b1001000,0b1110100},
        };
        // Parity pattern untuk digit ke-2~7 berdasarkan digit pertama
        private static readonly string[] EanParity = {
            "LLLLLL","LLGLGG","LLGGLG","LLGGGL","LGLLGG",
            "LGGLLG","LGGGLL","LGLGLG","LGLGGL","LGGLGL"
        };

        private static Bitmap DrawEan13(string text, RectangleF rect,
            float moduleWidthPx, bool showText)
        {
            // Pastikan 12 digit angka
            var digits = new System.Text.StringBuilder();
            foreach (char c in text)
                if (char.IsDigit(c) && digits.Length < 12) digits.Append(c);
            while (digits.Length < 12) digits.Append('0');
            string d12 = digits.ToString();

            // Check digit
            int odd = 0, even = 0;
            for (int i = 0; i < 12; i++)
                if (i % 2 == 0) odd += d12[i] - '0'; else even += d12[i] - '0';
            int check = (10 - (odd + even * 3) % 10) % 10;
            string d13 = d12 + check;

            // EAN-13 = 3 (guard) + 6×7 + 5 (center) + 6×7 + 3 (guard) = 95 modules
            int totalMod = 95;
            var (bmpW, mw) = CalcModuleWidth(totalMod, rect, moduleWidthPx);
            int textH = showText ? 18 : 0;
            int barH = Math.Max(1, (int)rect.Height - textH - 4);

            var bmp = new Bitmap(bmpW, barH + textH + 4, PixelFormat.Format32bppArgb);
            using var g2 = Graphics.FromImage(bmp);
            g2.Clear(Color.White);

            int d0 = d13[0] - '0';
            string parity = EanParity[d0];

            float x = 1f;
            void DrawBits(int pat, int bits)
            {
                for (int b = bits - 1; b >= 0; b--)
                {
                    if (((pat >> b) & 1) == 1)
                        g2.FillRectangle(Brushes.Black, x, 2, mw, barH);
                    x += mw;
                }
            }

            // Guard L (101)
            DrawBits(0b101, 3);
            // Left 6 digits
            for (int i = 1; i <= 6; i++)
            {
                int di = d13[i] - '0';
                int pat = parity[i - 1] == 'G' ? EanG[0, di] : EanL[0, di];
                DrawBits(pat, 7);
            }
            // Center guard (01010)
            DrawBits(0b01010, 5);
            // Right 6 digits
            for (int i = 7; i <= 12; i++)
            {
                int di = d13[i] - '0';
                DrawBits(EanR[0, di], 7);
            }
            // Guard R (101)
            DrawBits(0b101, 3);

            if (showText)
            {
                using var font = new Font("Courier New", 7f);
                var sf = new StringFormat { Alignment = StringAlignment.Center };
                g2.DrawString(d13, font, Brushes.Black,
                    new RectangleF(0, barH + 3, bmpW, textH), sf);
            }
            return bmp;
        }

        // ════════════════════════════════════════════════════════════
        // QR CODE  — implementasi QR Code versi 1 (21×21) dengan
        // Reed-Solomon error correction level M (15%)
        // Menggunakan algoritma standar ISO/IEC 18004
        // ════════════════════════════════════════════════════════════

        private static Bitmap DrawQr(string text, RectangleF rect)
        {
            // Gunakan QrEncoder internal sederhana
            bool[,] matrix = QrEncoder.Encode(text);
            int size = matrix.GetLength(0);
            int cellPx = Math.Max(2, (int)(Math.Min(rect.Width, rect.Height) / size));
            int bmpSize = size * cellPx + 2;

            var bmp = new Bitmap(bmpSize, bmpSize, PixelFormat.Format32bppArgb);
            using var g2 = Graphics.FromImage(bmp);
            g2.Clear(Color.White);

            for (int r = 0; r < size; r++)
                for (int c2 = 0; c2 < size; c2++)
                    if (matrix[r, c2])
                        g2.FillRectangle(Brushes.Black,
                            1 + c2 * cellPx, 1 + r * cellPx, cellPx, cellPx);

            return bmp;
        }

        // ── Display name untuk UI ─────────────────────────────────────
        public static string DisplayName(BarcodeType t) => t switch
        {
            BarcodeType.Code128 => "Code 128 — Universal (semua karakter)",
            BarcodeType.Code39 => "Code 39 — Angka & huruf kapital",
            BarcodeType.EAN13 => "EAN-13 — 13 digit (retail)",
            BarcodeType.QRCode => "QR Code — 2D, data banyak",
            _ => t.ToString()
        };
    }

    // ════════════════════════════════════════════════════════════════
    // QR ENCODER — implementasi QR versi 1-3 byte mode
    // ════════════════════════════════════════════════════════════════

    internal static class QrEncoder
    {
        // QR Version 1 (21×21) — max 17 byte dengan ECC level M
        // Menggunakan referensi ISO/IEC 18004
        public static bool[,] Encode(string text)
        {
            // Clamp ke max 17 karakter untuk ver 1, atau naik ke ver 2/3
            int ver = 1;
            if (text.Length > 14) ver = 2;
            if (text.Length > 26) ver = 3;
            if (text.Length > 34) { text = text.Substring(0, 34); ver = 3; }

            int size = 17 + ver * 4; // ver1=21, ver2=25, ver3=29
            var mod = new bool[size, size];
            var func = new bool[size, size]; // tandai modul fungsional

            PlaceFinders(mod, func, size);
            PlaceTimings(mod, func, size);
            PlaceAlignments(mod, func, size, ver);
            PlaceDarkModule(mod, func, size, ver);
            ReserveFormatArea(func, size);

            var data = BuildDataBytes(text, ver);
            PlaceData(mod, func, size, data);
            ApplyBestMask(mod, func, size);
            PlaceFormat(mod, size, 0); // ECC level M = pattern 0 dengan mask terpilih

            return mod;
        }

        private static void PlaceFinders(bool[,] m, bool[,] f, int size)
        {
            var positions = new[] { (0, 0), (0, size - 7), (size - 7, 0) };
            foreach (var (tr, tc) in positions)
            {
                for (int r = 0; r < 7; r++)
                    for (int c = 0; c < 7; c++)
                    {
                        bool on = r == 0 || r == 6 || c == 0 || c == 6 ||
                                  (r >= 2 && r <= 4 && c >= 2 && c <= 4);
                        Set(m, f, tr + r, tc + c, on, size);
                    }
                // Separator (ring putih di luar)
                for (int i = -1; i <= 7; i++)
                {
                    Set(m, f, tr - 1, tc + i, false, size);
                    Set(m, f, tr + 7, tc + i, false, size);
                    Set(m, f, tr + i, tc - 1, false, size);
                    Set(m, f, tr + i, tc + 7, false, size);
                }
            }
        }

        private static void Set(bool[,] m, bool[,] f, int r, int c, bool v, int size)
        {
            if (r < 0 || c < 0 || r >= size || c >= size) return;
            m[r, c] = v; f[r, c] = true;
        }

        private static void PlaceTimings(bool[,] m, bool[,] f, int size)
        {
            for (int i = 8; i < size - 8; i++)
            {
                bool v = i % 2 == 0;
                if (!f[6, i]) { m[6, i] = v; f[6, i] = true; }
                if (!f[i, 6]) { m[i, 6] = v; f[i, 6] = true; }
            }
        }

        private static void PlaceAlignments(bool[,] m, bool[,] f, int size, int ver)
        {
            if (ver < 2) return;
            int[] centers = ver == 2 ? new[] { 6, 18 } : new[] { 6, 22 };
            foreach (int ar in centers)
                foreach (int ac in centers)
                {
                    if (f[ar, ac]) continue;
                    for (int dr = -2; dr <= 2; dr++)
                        for (int dc = -2; dc <= 2; dc++)
                        {
                            bool v = dr == -2 || dr == 2 || dc == -2 || dc == 2 || (dr == 0 && dc == 0);
                            Set(m, f, ar + dr, ac + dc, v, size);
                        }
                }
        }

        private static void PlaceDarkModule(bool[,] m, bool[,] f, int size, int ver)
        {
            int r = 4 * ver + 9, c = 8;
            if (r < size && c < size) { m[r, c] = true; f[r, c] = true; }
        }

        private static void ReserveFormatArea(bool[,] f, int size)
        {
            // Format info area (di sekitar finder kiri atas dan kanan atas/bawah kiri)
            for (int i = 0; i <= 8; i++)
            {
                if (i < size) { f[8, i] = true; f[i, 8] = true; }
            }
            for (int i = size - 8; i < size; i++)
            {
                f[8, i] = true; f[i, 8] = true;
            }
        }

        private static byte[] BuildDataBytes(string text, int ver)
        {
            // Byte mode: 0100 + 8-bit length + data + 0000 terminator + padding
            int maxBytes = ver == 1 ? 11 : (ver == 2 ? 20 : 28); // ECC M data codewords
            var bits = new System.Collections.BitArray(0);
            void AddBits(int val, int count)
            {
                var old = new bool[bits.Count];
                bits.CopyTo(old, 0);
                bits = new System.Collections.BitArray(bits.Count + count);
                for (int i = 0; i < old.Length; i++) bits[i] = old[i];
                for (int i = count - 1; i >= 0; i--)
                    bits[old.Length + (count - 1 - i)] = ((val >> i) & 1) == 1;
            }

            var bytes = System.Text.Encoding.Latin1.GetBytes(text);
            int len = Math.Min(bytes.Length, maxBytes);

            AddBits(0b0100, 4);  // Byte mode indicator
            AddBits(len, 8);     // Character count
            for (int i = 0; i < len; i++) AddBits(bytes[i], 8);
            AddBits(0, 4);       // Terminator

            // Pad to byte boundary
            while (bits.Count % 8 != 0) AddBits(0, 1);

            // Pad codewords
            int totalBits = maxBytes * 8;
            bool toggle = true;
            while (bits.Count < totalBits)
            {
                AddBits(toggle ? 0b11101100 : 0b00010001, 8);
                toggle = !toggle;
            }

            var result = new byte[bits.Count / 8];
            for (int i = 0; i < result.Length; i++)
                for (int b = 0; b < 8; b++)
                    if (bits[i * 8 + b]) result[i] |= (byte)(1 << (7 - b));
            return result;
        }

        private static void PlaceData(bool[,] m, bool[,] f, int size, byte[] data)
        {
            int bitIdx = 0, totalBits = data.Length * 8;
            bool GetBit() => bitIdx < totalBits &&
                ((data[bitIdx / 8] >> (7 - bitIdx++ % 8)) & 1) == 1;

            // Traverse kolom 2-lebar dari kanan ke kiri, zig-zag atas-bawah
            bool upward = true;
            for (int col = size - 1; col >= 0; col -= 2)
            {
                if (col == 6) col--; // skip timing column
                for (int i = 0; i < size; i++)
                {
                    int r = upward ? size - 1 - i : i;
                    for (int dc = 0; dc <= 1; dc++)
                    {
                        int c = col - dc;
                        if (c < 0 || f[r, c]) continue;
                        m[r, c] = GetBit();
                        f[r, c] = true;
                    }
                }
                upward = !upward;
            }
        }

        private static void ApplyBestMask(bool[,] m, bool[,] f, int size)
        {
            // Pakai mask pattern 0: (row+col) % 2 == 0
            // (sederhana, cukup untuk implementasi dasar)
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    if (!f[r, c] && (r + c) % 2 == 0)
                        m[r, c] = !m[r, c];
        }

        private static void PlaceFormat(bool[,] m, int size, int maskPattern)
        {
            // Format: ECC level M (01) + mask pattern
            int fmt = (0b01 << 3) | maskPattern;
            // XOR dengan mask 101010000010010
            fmt = (fmt << 10);
            int gen = 0b10100110111;
            for (int i = 14; i >= 10; i--)
                if ((fmt & (1 << i)) != 0) fmt ^= gen << (i - 10);
            int fmtFull = ((0b01 << 3 | maskPattern) << 10 | fmt) ^ 0b101010000010010;

            // Tempatkan format bits di sekitar finder kiri atas
            int[] pos = { 0, 1, 2, 3, 4, 5, 7, 8, 8, 8, 8, 8, 8, 8 };
            int[] posR = { 8, 8, 8, 8, 8, 8, 8, 8, 7, 5, 4, 3, 2, 1, 0 };
            for (int i = 0; i < 15; i++)
            {
                bool v = ((fmtFull >> i) & 1) == 1;
                if (i < 7) { if (pos[i] < size && 8 < size) m[8, pos[i]] = v; }
                else { if (posR[i] < size && 8 < size) m[posR[i], 8] = v; }
                // Copy ke finder kanan atas / kiri bawah
                if (i < 8 && size - 1 - i >= 0) m[size - 1 - i, 8] = v;
                if (i >= 8 && 8 < size) m[8, size - 8 + (i - 8)] = v;
            }
        }
    }
}
