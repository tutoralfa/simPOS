using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Printing;
using simPOS.Shared.Reports;

namespace simPOS.Management.Forms.Reports
{
    // ══════════════════════════════════════════════════════════════════
    // BASE
    // ══════════════════════════════════════════════════════════════════

    internal abstract class BaseReportPrinter
    {
        // Margin halaman (dalam 1/100 inch)
        protected const float ML = 60f, MR = 60f, MT = 60f, MB = 60f;

        // Font set
        protected static readonly Font FTitle = new Font("Segoe UI", 14f, FontStyle.Bold);
        protected static readonly Font FSub = new Font("Segoe UI", 8.5f, FontStyle.Italic);
        protected static readonly Font FHead = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        protected static readonly Font FNormal = new Font("Segoe UI", 8.5f);
        protected static readonly Font FBold = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        protected static readonly Font FSmall = new Font("Segoe UI", 7.5f);

        // Warna
        protected static readonly Color CHeader = Color.FromArgb(44, 62, 80);
        protected static readonly Color CAlt = Color.FromArgb(245, 248, 252);
        protected static readonly Color CGrand = Color.FromArgb(211, 235, 211);
        protected static readonly Color CIN = Color.FromArgb(213, 245, 227);
        protected static readonly Color COUT = Color.FromArgb(250, 219, 216);
        protected static readonly Color CADJ = Color.FromArgb(253, 243, 213);

        // State paginasi
        protected int _currentPage = 1;
        protected float _yPos;
        protected float _pageWidth, _pageHeight;
        protected float _contentWidth => _pageWidth - ML - MR;

        // Setiap subclass implement ini
        public abstract PrintDocument CreateDocument();

        // ── Drawing helpers ──────────────────────────────────────────

        protected void DrawPageHeader(Graphics g, string title, string subtitle)
        {
            // Background header bar
            g.FillRectangle(new SolidBrush(CHeader),
                ML, _yPos, _contentWidth, 38f);

            g.DrawString(title, FTitle, Brushes.White, ML + 8, _yPos + 4);
            _yPos += 42f;
            g.DrawString(subtitle, FSub, Brushes.Gray, ML, _yPos);
            _yPos += FSub.Height + 10f;
        }

        protected void DrawTableHeader(Graphics g, ColumnDef[] cols)
        {
            float x = ML;
            float rowH = FHead.Height + 8f;

            g.FillRectangle(new SolidBrush(CHeader), ML, _yPos, _contentWidth, rowH);

            foreach (var col in cols)
            {
                var rect = new RectangleF(x + 2, _yPos + 2, col.Width - 4, rowH - 4);
                g.DrawString(col.Header, FHead, Brushes.White, rect,
                    AlignFormat(col.Align));
                x += col.Width;
            }
            _yPos += rowH;
        }

        protected bool DrawRow(Graphics g, ColumnDef[] cols, string[] values,
            Color bgColor, Font font = null, bool bold = false,
            float extraHeight = 0)
        {
            font ??= FNormal;
            float rowH = font.Height + 6f + extraHeight;

            // Cek apakah masih muat di halaman
            if (_yPos + rowH > _pageHeight - MB)
                return false; // butuh halaman baru

            if (bgColor != Color.White && bgColor != Color.Transparent)
                g.FillRectangle(new SolidBrush(bgColor), ML, _yPos, _contentWidth, rowH);

            // Garis bawah baris
            g.DrawLine(new Pen(Color.FromArgb(220, 220, 220)),
                ML, _yPos + rowH - 1, ML + _contentWidth, _yPos + rowH - 1);

            float x = ML;
            for (int i = 0; i < Math.Min(cols.Length, values.Length); i++)
            {
                var rect = new RectangleF(x + 3, _yPos + 3, cols[i].Width - 6, rowH - 4);
                var f = bold ? FBold : font;
                g.DrawString(values[i], f, Brushes.Black, rect, AlignFormat(cols[i].Align));
                x += cols[i].Width;
            }

            _yPos += rowH;
            return true;
        }

        protected void DrawPageFooter(Graphics g, int pageNum)
        {
            var y = _pageHeight - MB + 8f;
            var text = $"Halaman {pageNum}   |   Dicetak: {DateTime.Now:dd/MM/yyyy HH:mm}";
            g.DrawLine(new Pen(Color.FromArgb(200, 200, 200)), ML, y, ML + _contentWidth, y);
            g.DrawString(text, FSmall, Brushes.Gray,
                new RectangleF(ML, y + 4, _contentWidth, 20f),
                new StringFormat { Alignment = StringAlignment.Far });
        }

        protected void InitPage(PrintPageEventArgs e)
        {
            _pageWidth = e.PageBounds.Width;
            _pageHeight = e.PageBounds.Height;
            _yPos = MT;
        }

        private static StringFormat AlignFormat(HAlign align) => new StringFormat
        {
            Alignment = align == HAlign.Right ? StringAlignment.Far
                          : align == HAlign.Center ? StringAlignment.Center
                          : StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter
        };

        protected struct ColumnDef
        {
            public string Header;
            public float Width;
            public HAlign Align;
            public ColumnDef(string h, float w, HAlign a = HAlign.Left)
            { Header = h; Width = w; Align = a; }
        }

        protected enum HAlign { Left, Center, Right }
    }

    // ══════════════════════════════════════════════════════════════════
    // LAPORAN PENJUALAN
    // ══════════════════════════════════════════════════════════════════

    internal class SalesPrinter : BaseReportPrinter
    {
        private readonly SalesReportFilter _filter;
        private readonly List<SalesSummaryRow> _summary;
        private readonly List<SalesTrxRow> _trxList;

        // Paginasi — halaman mana sedang dirender
        private enum Section { Summary, Transactions }
        private Section _section = Section.Summary;
        private int _summaryIndex = 0;
        private int _trxIndex = 0;
        private int _itemIndex = 0;
        private bool _trxHeaderDrawn = false;

        public SalesPrinter(SalesReportFilter filter,
            List<SalesSummaryRow> summary, List<SalesTrxRow> trxList)
        {
            _filter = filter;
            _summary = summary;
            _trxList = trxList;
        }

        public override PrintDocument CreateDocument()
        {
            var pd = new PrintDocument();
            pd.DefaultPageSettings.Landscape = false; // Portrait A4
            pd.DefaultPageSettings.Margins = new Margins(
                (int)ML, (int)MR, (int)MT, (int)MB);

            // Reset state
            _section = Section.Summary;
            _summaryIndex = 0;
            _trxIndex = 0;
            _itemIndex = 0;
            _trxHeaderDrawn = false;
            _currentPage = 1;

            pd.PrintPage += OnPrintPage;
            return pd;
        }

        // Kolom ringkasan
        private ColumnDef[] ColsSummary(float w) => new[]
        {
            new ColumnDef("Tanggal",       w * 0.25f),
            new ColumnDef("Jml Transaksi", w * 0.18f, HAlign.Center),
            new ColumnDef("Total Item",    w * 0.18f, HAlign.Center),
            new ColumnDef("Total Omzet",   w * 0.20f, HAlign.Right),
            new ColumnDef("Rata-rata/Trx", w * 0.19f, HAlign.Right),
        };

        // Kolom transaksi
        private ColumnDef[] ColsTrx(float w) => new[]
        {
            new ColumnDef("No. Invoice",  w * 0.22f),
            new ColumnDef("Waktu",        w * 0.20f),
            new ColumnDef("Items",        w * 0.08f, HAlign.Center),
            new ColumnDef("Total",        w * 0.17f, HAlign.Right),
            new ColumnDef("Dibayar",      w * 0.16f, HAlign.Right),
            new ColumnDef("Kembali",      w * 0.17f, HAlign.Right),
        };

        private void OnPrintPage(object sender, PrintPageEventArgs e)
        {
            var g = e.Graphics;
            InitPage(e);
            var cw = _contentWidth;

            // ── Header halaman pertama ────────────────────────────
            if (_currentPage == 1)
            {
                DrawPageHeader(g,
                    "LAPORAN PENJUALAN",
                    $"Periode: {_filter.DateFrom:dd MMM yyyy} s/d {_filter.DateTo:dd MMM yyyy}" +
                    $"   |   Dibuat: {DateTime.Now:dd/MM/yyyy HH:mm}");
            }

            // ── Section: Ringkasan ────────────────────────────────
            if (_section == Section.Summary)
            {
                if (_summaryIndex == 0)
                {
                    _yPos += 4f;
                    g.DrawString("Ringkasan Penjualan", FBold, Brushes.Black, ML, _yPos);
                    _yPos += FBold.Height + 6f;
                    DrawTableHeader(g, ColsSummary(cw));
                }

                var cols = ColsSummary(cw);
                while (_summaryIndex < _summary.Count)
                {
                    var r = _summary[_summaryIndex];
                    var bg = _summaryIndex % 2 == 0 ? Color.White : CAlt;
                    var ok = DrawRow(g, cols,
                        new[] { r.Date, r.TotalTrx.ToString(), r.TotalQty.ToString(),
                                $"Rp {r.TotalOmzet:N0}", $"Rp {r.AvgTrx:N0}" },
                        bg);
                    if (!ok) { DrawPageFooter(g, _currentPage++); e.HasMorePages = true; return; }
                    _summaryIndex++;
                }

                // Grand total ringkasan
                decimal gOmzet = 0; int gTrx = 0, gQty = 0;
                foreach (var r in _summary) { gOmzet += r.TotalOmzet; gTrx += r.TotalTrx; gQty += r.TotalQty; }
                DrawRow(g, cols,
                    new[] { "TOTAL", gTrx.ToString(), gQty.ToString(),
                            $"Rp {gOmzet:N0}", gTrx > 0 ? $"Rp {gOmzet/gTrx:N0}" : "Rp 0" },
                    CGrand, bold: true);

                _yPos += 20f;
                _section = Section.Transactions;
            }

            // ── Section: Detail Transaksi ─────────────────────────
            if (_section == Section.Transactions)
            {
                if (!_trxHeaderDrawn)
                {
                    if (_yPos + 60f > _pageHeight - MB)
                    {
                        DrawPageFooter(g, _currentPage++); e.HasMorePages = true; return;
                    }
                    g.DrawString("Detail Transaksi", FBold, Brushes.Black, ML, _yPos);
                    _yPos += FBold.Height + 6f;
                    DrawTableHeader(g, ColsTrx(cw));
                    _trxHeaderDrawn = true;
                }

                var cols = ColsTrx(cw);
                while (_trxIndex < _trxList.Count)
                {
                    var t = _trxList[_trxIndex];
                    var bg = _trxIndex % 2 == 0 ? Color.White : CAlt;

                    // Baris header transaksi
                    if (_itemIndex == 0)
                    {
                        var ok = DrawRow(g, cols,
                            new[] { t.InvoiceNo, t.CreatedAt, t.ItemCount.ToString(),
                                    $"Rp {t.TotalAmount:N0}", $"Rp {t.PaidAmount:N0}",
                                    $"Rp {t.ChangeAmount:N0}" }, bg);
                        if (!ok) { DrawPageFooter(g, _currentPage++); e.HasMorePages = true; return; }
                    }

                    // Baris item detail dengan indent
                    while (_itemIndex < t.Items.Count)
                    {
                        var item = t.Items[_itemIndex];
                        var ok2 = DrawRow(g, cols,
                            new[] {
                                $"   └ {item.ProductCode}",
                                $"   {item.ProductName}",
                                $"{item.Quantity} {item.Unit}",
                                $"Rp {item.SellPrice:N0}",
                                $"Rp {item.Subtotal:N0}", "" },
                            bg, FSmall);
                        if (!ok2) { DrawPageFooter(g, _currentPage++); e.HasMorePages = true; return; }
                        _itemIndex++;
                    }

                    _itemIndex = 0;
                    _trxIndex++;
                }
            }

            DrawPageFooter(g, _currentPage);
            e.HasMorePages = false;
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // KARTU STOK
    // ══════════════════════════════════════════════════════════════════

    internal class StockCardPrinter : BaseReportPrinter
    {
        private readonly StockCardFilter _filter;
        private readonly List<StockCardRow> _rows;

        private int _rowIndex = 0;
        private bool _headerDrawn = false;

        public StockCardPrinter(StockCardFilter filter, List<StockCardRow> rows)
        {
            _filter = filter;
            _rows = rows;
        }

        public override PrintDocument CreateDocument()
        {
            var pd = new PrintDocument();
            pd.DefaultPageSettings.Landscape = true; // Landscape A4
            pd.DefaultPageSettings.Margins = new Margins(
                (int)ML, (int)MR, (int)MT, (int)MB);

            _rowIndex = 0;
            _headerDrawn = false;
            _currentPage = 1;

            pd.PrintPage += OnPrintPage;
            return pd;
        }

        private ColumnDef[] Cols(float w) => new[]
        {
            new ColumnDef("Tanggal",    w * 0.16f),
            new ColumnDef("Tipe",       w * 0.08f, HAlign.Center),
            new ColumnDef("Masuk",      w * 0.07f, HAlign.Center),
            new ColumnDef("Keluar",     w * 0.07f, HAlign.Center),
            new ColumnDef("Opname",     w * 0.07f, HAlign.Center),
            new ColumnDef("Stok Akhir", w * 0.09f, HAlign.Center),
            new ColumnDef("Referensi",  w * 0.18f),
            new ColumnDef("Keterangan", w * 0.28f),
        };

        private void OnPrintPage(object sender, PrintPageEventArgs e)
        {
            var g = e.Graphics;
            InitPage(e);
            var cw = _contentWidth;

            // Header halaman pertama
            if (_currentPage == 1)
            {
                DrawPageHeader(g, "KARTU STOK",
                    $"{_filter.ProductCode} — {_filter.ProductName}   |   " +
                    $"Periode: {_filter.DateFrom:dd MMM yyyy} s/d {_filter.DateTo:dd MMM yyyy}   |   " +
                    $"Satuan: {_filter.Unit}");

                // Info stok awal
                var infoRect = new RectangleF(ML, _yPos, cw, 22f);
                g.FillRectangle(new SolidBrush(Color.FromArgb(230, 243, 255)), infoRect);
                g.DrawString($"Stok Awal Periode: {_filter.StockAwal} {_filter.Unit}",
                    FBold, Brushes.Black, ML + 6f, _yPos + 3f);
                _yPos += 26f;
            }

            var cols = Cols(cw);

            if (!_headerDrawn)
            {
                DrawTableHeader(g, cols);
                _headerDrawn = true;
            }

            // Baris stok awal (hanya halaman pertama setelah header)
            if (_rowIndex == 0 && _currentPage == 1)
            {
                DrawRow(g, cols,
                    new[] {
                        _filter.DateFrom.ToString("yyyy-MM-dd"),
                        "AWAL", "", "", "", _filter.StockAwal.ToString(),
                        "", "Stok awal periode" },
                    Color.FromArgb(230, 243, 255), bold: true);
            }

            // Baris mutasi
            while (_rowIndex < _rows.Count)
            {
                var r = _rows[_rowIndex];
                var bg = r.Type == "IN" ? CIN
                       : r.Type == "OUT" ? COUT
                       : CADJ;

                var ok = DrawRow(g, cols, new[]
                {
                    r.Date,
                    r.TypeLabel,
                    r.QtyIn  > 0  ? r.QtyIn.ToString()  : "",
                    r.QtyOut > 0  ? r.QtyOut.ToString()  : "",
                    r.QtyAdj != 0 ? r.QtyAdj.ToString() : "",
                    r.StockAfter.ToString(),
                    r.Reference,
                    r.Notes
                }, bg);

                if (!ok)
                {
                    // Halaman baru — redraw header tabel
                    DrawPageFooter(g, _currentPage++);
                    _headerDrawn = false;
                    e.HasMorePages = true;
                    return;
                }
                _rowIndex++;
            }

            // Total row
            int totIn = 0, totOut = 0, totAdj = 0;
            foreach (var r in _rows) { totIn += r.QtyIn; totOut += r.QtyOut; totAdj += r.QtyAdj; }

            DrawRow(g, cols,
                new[] { "TOTAL", "", totIn.ToString(), totOut.ToString(),
                        totAdj.ToString(), "", "", "" },
                CGrand, bold: true);

            DrawPageFooter(g, _currentPage);
            e.HasMorePages = false;
        }
    }
}
