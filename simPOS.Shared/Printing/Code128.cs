using DocumentFormat.OpenXml.Drawing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;

namespace simPOS.Shared.Printing
{
    /// <summary>
    /// Generator Code 128 murni GDI+ — tanpa library eksternal.
    /// Menggunakan subset Code 128B (ASCII 32–127).
    /// </summary>
    public static class Code128
    {
        // Pola bar Code 128 (11 bit per karakter, 0=space 1=bar)
        private static readonly int[] Patterns = {
            0b11011001100, // 0  = ' ' (space)  val 0
            0b11001101100, // 1
            0b11001100110, // 2
            0b10010011000, // 3
            0b10010001100, // 4
            0b10001001100, // 5
            0b10011001000, // 6
            0b10011000100, // 7
            0b10001100100, // 8
            0b11001001000, // 9
            0b11001000100, // 10
            0b11000100100, // 11
            0b10110011100, // 12
            0b10011011100, // 13
            0b10011001110, // 14
            0b10111001100, // 15
            0b10011101100, // 16
            0b10011100110, // 17
            0b11001110010, // 18
            0b11001011100, // 19
            0b11001001110, // 20
            0b11011100100, // 21
            0b11001110100, // 22
            0b11101101110, // 23
            0b11101001100, // 24
            0b11100101100, // 25
            0b11100100110, // 26
            0b11101100100, // 27
            0b11100110100, // 28
            0b11100110010, // 29
            0b11011011000, // 30
            0b11011000110, // 31
            0b11000110110, // 32
            0b10100011000, // 33
            0b10001011000, // 34
            0b10001000110, // 35
            0b10110001000, // 36
            0b10001101000, // 37
            0b10001100010, // 38
            0b11010001000, // 39
            0b11000101000, // 40
            0b11000100010, // 41
            0b10110111000, // 42
            0b10110001110, // 43
            0b10001101110, // 44
            0b10111011000, // 45
            0b10111000110, // 46
            0b10001110110, // 47
            0b11101110110, // 48
            0b11010001110, // 49
            0b11000101110, // 50
            0b11011101000, // 51
            0b11011100010, // 52
            0b11011101110, // 53
            0b11101011000, // 54
            0b11101000110, // 55
            0b11100010110, // 56
            0b11101101000, // 57
            0b11101100010, // 58
            0b11100011010, // 59
            0b11101111010, // 60
            0b11001000010, // 61
            0b11110001010, // 62
            0b10100110000, // 63
            0b10100001100, // 64
            0b10010110000, // 65
            0b10010000110, // 66
            0b10000101100, // 67
            0b10000100110, // 68
            0b10110010000, // 69
            0b10110000100, // 70
            0b10011010000, // 71
            0b10011000010, // 72
            0b10000110100, // 73
            0b10000110010, // 74
            0b11000010010, // 75
            0b11001010000, // 76
            0b11110111010, // 77
            0b11000010100, // 78
            0b10001111010, // 79
            0b10100111100, // 80
            0b10010111100, // 81
            0b10010011110, // 82
            0b10111100100, // 83
            0b10011110100, // 84
            0b10011110010, // 85
            0b11110100100, // 86
            0b11110010100, // 87
            0b11110010010, // 88
            0b11011011110, // 89
            0b11011110110, // 90
            0b11110110110, // 91
            0b10101111000, // 92
            0b10100011110, // 93
            0b10001011110, // 94
            0b10111101000, // 95
            0b10111100010, // 96
            0b11110101000, // 97
            0b11110100010, // 98
            0b10111011110, // 99
            0b10111101110, // 100
            0b11101011110, // 101
            0b11110101110, // 102
            0b11010000100, // 103 START B
            0b11010010000, // 104 START C (unused)
            0b11010011100, // 105 STOP (pattern sebelum terminator)
        };

        private const int START_B = 104;
        private const int STOP = 106; // pola STOP = 11000111010 (terminator)
        private static readonly int StopPattern = 0b11000111010;

        /// <summary>
        /// Buat Bitmap barcode Code 128B dari teks.
        /// </summary>
        /// <param name="text">Teks yang di-encode.</param>
        /// <param name="width">Lebar bitmap (px). Barcode di-scale agar pas.</param>
        /// <param name="height">Tinggi area bar (px, tidak termasuk teks).</param>
        /// <param name="showText">Tampilkan teks di bawah barcode.</param>
        /// <param name="moduleWidthPx">Lebar 1 modul (px). 0 = auto (fill width). Nilai kecil = barcode lebih sempit.</param>
        public static Bitmap Generate(string text, int width, int height, bool showText = true, float moduleWidthPx = 0f)
        {
            if (string.IsNullOrEmpty(text)) text = " ";

            // Encode ke nilai Code128B
            var values = new List<int>();
            values.Add(START_B); // start code B

            int checksum = START_B - 100; // nilai start B = 104, checksum value = 104

            for (int i = 0; i < text.Length; i++)
            {
                int ascii = text[i];
                if (ascii < 32 || ascii > 126) ascii = 32; // fallback ke space
                int val = ascii - 32; // Code128B: space=0, !...~
                values.Add(val);
                checksum += val * (i + 1);
            }

            checksum %= 103;
            values.Add(checksum);

            // Hitung total bar (setiap karakter = 11 modul, stop = 13)
            int totalModules = values.Count * 11 + 13; // 13 = STOP (11) + terminator (2)
            float moduleW = moduleWidthPx > 0f
                ? moduleWidthPx                              // lebar manual
                : Math.Max(1f, (float)width / totalModules); // auto fill
            int bmpW = (int)(totalModules * moduleW) + 2;

            int textH = showText ? 18 : 0;
            var bmp = new Bitmap(bmpW, height + textH + 4, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.White);

            float x = 1f;

            void DrawPattern(int pattern, int bits = 11)
            {
                for (int b = bits - 1; b >= 0; b--)
                {
                    bool isBar = ((pattern >> b) & 1) == 1;
                    if (isBar)
                        g.FillRectangle(Brushes.Black, x, 2, moduleW, height);
                    x += moduleW;
                }
            }

            // Draw start + data + checksum
            DrawPattern(Patterns[103]); // START B pattern index 103
            foreach (var v in values.GetRange(1, values.Count - 1))
                DrawPattern(Patterns[v]);

            // STOP pattern (11 bit) + terminator 2 bit
            DrawPattern(Patterns[105]); // stop pattern
            DrawPattern(0b11, 2);       // terminator: 2 bar

            // Teks di bawah
            if (showText)
            {
                using var font = new Font("Courier New", 7.5f);
                var sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(text, font, Brushes.Black,
                    new RectangleF(0, height + 4, bmpW, textH), sf);
            }

            return bmp;
        }

        /// <summary>
        /// Render barcode langsung ke Graphics (untuk PrintDocument).
        /// </summary>
        /// <param name="moduleWidthPx">Lebar 1 modul (px). 0 = auto. Nilai kecil (mis. 1.0–1.5) = barcode sempit.</param>
        public static void DrawTo(Graphics g, string text, RectangleF rect, bool showText = true, float moduleWidthPx = 0f)
        {
            int barH = (int)(rect.Height - (showText ? 18 : 0));
            using var bmp = Generate(text, (int)rect.Width, Math.Max(1, barH), showText, moduleWidthPx);
            // Gambar di tengah rect secara horizontal jika barcode lebih sempit dari rect
            float drawX = rect.X + (rect.Width - bmp.Width) / 2f;
            g.DrawImage(bmp, drawX, rect.Y);
        }
    }
}
