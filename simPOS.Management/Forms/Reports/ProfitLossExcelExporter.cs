using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using simPOS.Shared.Reports;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Font = iTextSharp.text.Font;

namespace simPOS.Management.Forms.Reports
{
    // ══════════════════════════════════════════════════════════════════
    // EXCEL
    // ══════════════════════════════════════════════════════════════════

    public static class ProfitLossExcelExporter
    {
        public static byte[] Export(ProfitLossFilter filter,
            List<ProfitLossRow> rows, ProfitLossSummary summary)
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Laba Rugi");

            // ── Judul ──────────────────────────────────────────────
            ws.Cell("A1").Value = "LAPORAN LABA / RUGI";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 14;
            ws.Cell("A2").Value = $"Periode : {filter.DateFrom:dd/MM/yyyy} s/d {filter.DateTo:dd/MM/yyyy}";
            ws.Cell("A3").Value = $"Dibuat  : {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Cell("A3").Style.Font.Italic = true;

            // ── Ringkasan 4 kotak (baris 5-9) ─────────────────────
            int sr = 5;
            WriteBoxed(ws, sr, 1, "PENDAPATAN", $"Rp {summary.TotalPendapatan:N0}", XLColor.FromArgb(52, 152, 219));
            WriteBoxed(ws, sr, 3, "HPP", $"Rp {summary.TotalHPP:N0}", XLColor.FromArgb(192, 57, 43));
            WriteBoxed(ws, sr, 5, "LABA KOTOR", $"Rp {summary.TotalLabaKotor:N0}", XLColor.FromArgb(39, 174, 96));
            WriteBoxed(ws, sr, 7, "MARGIN", $"{summary.MarginPct:N1}%", XLColor.FromArgb(142, 68, 173));

            // ── Tabel detail ───────────────────────────────────────
            int row = 11;
            var headers = new[] { "Periode", "Jml Trx", "Total Item", "Pendapatan", "HPP", "Laba Kotor", "Margin %" };
            for (int c = 0; c < headers.Length; c++)
            {
                var cell = ws.Cell(row, c + 1);
                cell.Value = headers[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(44, 62, 80);
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            row++;

            foreach (var r in rows)
            {
                ws.Cell(row, 1).Value = r.Period;
                ws.Cell(row, 2).Value = r.TotalTrx;
                ws.Cell(row, 3).Value = r.TotalQty;
                ws.Cell(row, 4).Value = (double)r.Pendapatan;
                ws.Cell(row, 5).Value = (double)r.HPP;
                ws.Cell(row, 6).Value = (double)r.LabaKotor;
                ws.Cell(row, 7).Value = (double)r.MarginPct / 100;

                foreach (var c in new[] { 4, 5, 6 })
                    ws.Cell(row, c).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 7).Style.NumberFormat.Format = "0.0%";
                ws.Cell(row, 2).Style.Alignment.Horizontal =
                ws.Cell(row, 3).Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;

                // Merah jika rugi
                if (r.LabaKotor < 0)
                    ws.Cell(row, 6).Style.Font.FontColor = XLColor.Red;
                else
                    ws.Cell(row, 6).Style.Font.Bold = true;

                if (row % 2 == 0)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromArgb(245, 248, 252);
                row++;
            }

            // Grand total
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 4).Value = (double)summary.TotalPendapatan;
            ws.Cell(row, 5).Value = (double)summary.TotalHPP;
            ws.Cell(row, 6).Value = (double)summary.TotalLabaKotor;
            ws.Cell(row, 7).Value = (double)summary.MarginPct / 100;
            foreach (var c in new[] { 4, 5, 6 })
                ws.Cell(row, c).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 7).Style.NumberFormat.Format = "0.0%";

            var totRange = ws.Range(row, 1, row, 7);
            totRange.Style.Font.Bold = true;
            totRange.Style.Fill.BackgroundColor = XLColor.FromArgb(211, 235, 211);

            ws.Columns().AdjustToContents();
            ws.Column(1).Width = 18;

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private static void WriteBoxed(IXLWorksheet ws, int row, int col,
            string title, string value, XLColor color)
        {
            ws.Cell(row, col).Value = title;
            ws.Cell(row, col).Style.Font.Bold = true;
            ws.Cell(row, col).Style.Font.FontColor = XLColor.White;
            ws.Cell(row, col).Style.Fill.BackgroundColor = color;
            ws.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row + 1, col).Value = value;
            ws.Cell(row + 1, col).Style.Font.Bold = true;
            ws.Cell(row + 1, col).Style.Font.FontSize = 13;
            ws.Cell(row + 1, col).Style.Fill.BackgroundColor = color;
            ws.Cell(row + 1, col).Style.Font.FontColor = XLColor.White;
            ws.Cell(row + 1, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Range(row, col, row + 1, col + 1).Merge();
            ws.Range(row, col, row + 1, col + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            ws.Column(col).Width = 22;
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // PDF
    // ══════════════════════════════════════════════════════════════════

    public static class ProfitLossPdfExporter
    {
        private static readonly BaseColor CHdr = new BaseColor(44, 62, 80);
        private static readonly BaseColor CAlt = new BaseColor(245, 248, 252);
        private static readonly BaseColor CGrand = new BaseColor(211, 235, 211);
        private static readonly BaseColor CRugi = new BaseColor(250, 219, 216);

        private static Font FTitle => new Font(Font.FontFamily.HELVETICA, 14, Font.BOLD);
        private static Font FSub => new Font(Font.FontFamily.HELVETICA, 8, Font.ITALIC, BaseColor.GRAY);
        private static Font FHead => new Font(Font.FontFamily.HELVETICA, 9, Font.BOLD, BaseColor.WHITE);
        private static Font FNormal => new Font(Font.FontFamily.HELVETICA, 9);
        private static Font FBold => new Font(Font.FontFamily.HELVETICA, 9, Font.BOLD);

        public static byte[] Export(ProfitLossFilter filter,
            List<ProfitLossRow> rows, ProfitLossSummary summary)
        {
            using var ms = new MemoryStream();
            var doc = new Document(PageSize.A4, 36, 36, 40, 36);
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            // Judul
            doc.Add(new Paragraph("LAPORAN LABA / RUGI", FTitle));
            doc.Add(new Paragraph(
                $"Periode: {filter.DateFrom:dd MMM yyyy} s/d {filter.DateTo:dd MMM yyyy}   |   " +
                $"Dibuat: {DateTime.Now:dd/MM/yyyy HH:mm}", FSub));
            doc.Add(new Chunk("\n"));

            // Ringkasan 4 kotak (tabel 1 baris)
            var tblCards = new PdfPTable(4) { WidthPercentage = 100, SpacingAfter = 14f };
            tblCards.SetWidths(new float[] { 25f, 25f, 25f, 25f });

            AddCard(tblCards, "PENDAPATAN", $"Rp {summary.TotalPendapatan:N0}", new BaseColor(52, 152, 219));
            AddCard(tblCards, "HPP", $"Rp {summary.TotalHPP:N0}", new BaseColor(192, 57, 43));
            AddCard(tblCards, "LABA KOTOR", $"Rp {summary.TotalLabaKotor:N0}", new BaseColor(39, 174, 96));
            AddCard(tblCards, "MARGIN", $"{summary.MarginPct:N1}%", new BaseColor(142, 68, 173));
            doc.Add(tblCards);

            // Tabel detail
            var tbl = new PdfPTable(7) { WidthPercentage = 100 };
            tbl.SetWidths(new float[] { 16f, 10f, 10f, 17f, 17f, 17f, 13f });

            foreach (var h in new[] { "Periode", "Jml Trx", "Total Item", "Pendapatan", "HPP", "Laba Kotor", "Margin %" })
                tbl.AddCell(new PdfPCell(new Phrase(h, FHead))
                { BackgroundColor = CHdr, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5f });

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var bg = r.LabaKotor < 0 ? CRugi : (i % 2 == 0 ? BaseColor.WHITE : CAlt);
                var labaFont = r.LabaKotor < 0
                    ? new Font(Font.FontFamily.HELVETICA, 9, Font.BOLD, new BaseColor(192, 57, 43))
                    : FBold;

                AddCell(tbl, r.Period, bg, FNormal, Element.ALIGN_LEFT);
                AddCell(tbl, r.TotalTrx.ToString(), bg, FNormal, Element.ALIGN_CENTER);
                AddCell(tbl, r.TotalQty.ToString(), bg, FNormal, Element.ALIGN_CENTER);
                AddCell(tbl, $"Rp {r.Pendapatan:N0}", bg, FNormal, Element.ALIGN_RIGHT);
                AddCell(tbl, $"Rp {r.HPP:N0}", bg, FNormal, Element.ALIGN_RIGHT);
                AddCell(tbl, $"Rp {r.LabaKotor:N0}", bg, labaFont, Element.ALIGN_RIGHT);
                AddCell(tbl, $"{r.MarginPct:N1}%", bg, FNormal, Element.ALIGN_CENTER);
            }

            // Grand total
            AddCell(tbl, "TOTAL", CGrand, FBold, Element.ALIGN_LEFT);
            AddCell(tbl, summary.TotalTrx.ToString(), CGrand, FBold, Element.ALIGN_CENTER);
            AddCell(tbl, summary.TotalQty.ToString(), CGrand, FBold, Element.ALIGN_CENTER);
            AddCell(tbl, $"Rp {summary.TotalPendapatan:N0}", CGrand, FBold, Element.ALIGN_RIGHT);
            AddCell(tbl, $"Rp {summary.TotalHPP:N0}", CGrand, FBold, Element.ALIGN_RIGHT);
            AddCell(tbl, $"Rp {summary.TotalLabaKotor:N0}", CGrand, FBold, Element.ALIGN_RIGHT);
            AddCell(tbl, $"{summary.MarginPct:N1}%", CGrand, FBold, Element.ALIGN_CENTER);

            doc.Add(tbl);
            doc.Close();
            return ms.ToArray();
        }

        private static void AddCard(PdfPTable tbl, string title, string value, BaseColor color)
        {
            var inner = new PdfPTable(1);
            inner.AddCell(new PdfPCell(new Phrase(title, FHead))
            {
                BackgroundColor = color,
                HorizontalAlignment = Element.ALIGN_CENTER,
                Border = 0,
                PaddingTop = 6f,
                PaddingBottom = 2f
            });
            inner.AddCell(new PdfPCell(new Phrase(value,
                new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.WHITE)))
            {
                BackgroundColor = color,
                HorizontalAlignment = Element.ALIGN_CENTER,
                Border = 0,
                PaddingBottom = 8f
            });

            tbl.AddCell(new PdfPCell(inner) { Padding = 3f, Border = PdfPCell.NO_BORDER });
        }

        private static void AddCell(PdfPTable tbl, string text, BaseColor bg, Font font, int align)
            => tbl.AddCell(new PdfPCell(new Phrase(text, font))
            { BackgroundColor = bg, HorizontalAlignment = align, Padding = 4f });
    }

    // ══════════════════════════════════════════════════════════════════
    // PRINT (GDI+ untuk PrintPreviewDialog)
    // ══════════════════════════════════════════════════════════════════

    internal class ProfitLossPrinter : BaseReportPrinter
    {
        private readonly ProfitLossFilter _filter;
        private readonly List<ProfitLossRow> _rows;
        private readonly ProfitLossSummary _summary;

        private int _rowIndex = 0;
        private bool _headerDrawn = false;

        public ProfitLossPrinter(ProfitLossFilter filter,
            List<ProfitLossRow> rows, ProfitLossSummary summary)
        {
            _filter = filter;
            _rows = rows;
            _summary = summary;
        }

        public override PrintDocument CreateDocument()
        {
            var pd = new PrintDocument();
            pd.DefaultPageSettings.Landscape = false; // Portrait A4
            pd.DefaultPageSettings.Margins = new Margins((int)ML, (int)MR, (int)MT, (int)MB);

            _rowIndex = 0;
            _headerDrawn = false;
            _currentPage = 1;

            pd.PrintPage += OnPrintPage;
            return pd;
        }

        private ColumnDef[] Cols(float w) => new[]
        {
            new ColumnDef("Periode",    w * 0.16f),
            new ColumnDef("Jml Trx",   w * 0.10f, HAlign.Center),
            new ColumnDef("Total Item",w * 0.10f, HAlign.Center),
            new ColumnDef("Pendapatan",w * 0.18f, HAlign.Right),
            new ColumnDef("HPP",       w * 0.18f, HAlign.Right),
            new ColumnDef("Laba Kotor",w * 0.18f, HAlign.Right),
            new ColumnDef("Margin %",  w * 0.10f, HAlign.Center),
        };

        private void OnPrintPage(object sender, PrintPageEventArgs e)
        {
            var g = e.Graphics;
            InitPage(e);
            var cw = _contentWidth;

            // ── Header (halaman pertama) ───────────────────────────
            if (_currentPage == 1)
            {
                DrawPageHeader(g, "LAPORAN LABA / RUGI",
                    $"Periode: {_filter.DateFrom:dd MMM yyyy} s/d {_filter.DateTo:dd MMM yyyy}" +
                    $"   |   Dibuat: {DateTime.Now:dd/MM/yyyy HH:mm}");

                // Summary bar — 4 kotak sejajar
                DrawSummaryBar(g, cw);
                _yPos += 6f;
            }

            // ── Header tabel ──────────────────────────────────────
            var cols = Cols(cw);
            if (!_headerDrawn)
            {
                DrawTableHeader(g, cols);
                _headerDrawn = true;
            }

            // ── Baris data ────────────────────────────────────────
            while (_rowIndex < _rows.Count)
            {
                var r = _rows[_rowIndex];
                var bg = r.LabaKotor < 0
                    ? Color.FromArgb(250, 219, 216)
                    : _rowIndex % 2 == 0 ? Color.White : Color.FromArgb(245, 248, 252);

                var ok = DrawRow(g, cols, new[]
                {
                    r.Period,
                    r.TotalTrx.ToString(),
                    r.TotalQty.ToString(),
                    $"Rp {r.Pendapatan:N0}",
                    $"Rp {r.HPP:N0}",
                    $"Rp {r.LabaKotor:N0}",
                    $"{r.MarginPct:N1}%"
                }, bg);

                if (!ok)
                {
                    DrawPageFooter(g, _currentPage++);
                    _headerDrawn = false;
                    e.HasMorePages = true;
                    return;
                }
                _rowIndex++;
            }

            // Grand total
            DrawRow(g, cols, new[]
            {
                "TOTAL",
                _summary.TotalTrx.ToString(),
                _summary.TotalQty.ToString(),
                $"Rp {_summary.TotalPendapatan:N0}",
                $"Rp {_summary.TotalHPP:N0}",
                $"Rp {_summary.TotalLabaKotor:N0}",
                $"{_summary.MarginPct:N1}%"
            }, Color.FromArgb(211, 235, 211), bold: true);

            DrawPageFooter(g, _currentPage);
            e.HasMorePages = false;
        }

        private void DrawSummaryBar(Graphics g, float cw)
        {
            float cardW = cw / 4f;
            float cardH = 52f;
            float x = ML;

            var cards = new[]
            {
                ("PENDAPATAN", $"Rp {_summary.TotalPendapatan:N0}", Color.FromArgb(52, 152, 219)),
                ("HPP",        $"Rp {_summary.TotalHPP:N0}",        Color.FromArgb(192, 57, 43)),
                ("LABA KOTOR", $"Rp {_summary.TotalLabaKotor:N0}",  Color.FromArgb(39, 174, 96)),
                ("MARGIN",     $"{_summary.MarginPct:N1}%",          Color.FromArgb(142, 68, 173)),
            };

            foreach (var (title, value, color) in cards)
            {
                g.FillRectangle(new SolidBrush(color), x, _yPos, cardW - 4f, cardH);

                // Title
                g.DrawString(title, FSmall, Brushes.White,
                    new RectangleF(x, _yPos + 4f, cardW - 4f, 18f),
                    new StringFormat { Alignment = StringAlignment.Center });

                // Value
                var valFont = new System.Drawing.Font("Segoe UI", 11f, FontStyle.Bold);
                g.DrawString(value, valFont, Brushes.White,
                    new RectangleF(x, _yPos + 20f, cardW - 4f, 26f),
                    new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    });
                x += cardW;
            }
            _yPos += cardH + 10f;
        }
    }
}
