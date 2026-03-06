using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace simPOS.Shared.Reports
{
    /// <summary>
    /// Export laporan ke PDF menggunakan iTextSharp.
    /// NuGet: iTextSharp (versi 5.5.13+)
    /// </summary>
    public static class PdfExporter
    {
        // ── Warna tema ────────────────────────────────────────────────
        private static readonly BaseColor ColorHeader = new BaseColor(44, 62, 80);
        private static readonly BaseColor ColorAlt = new BaseColor(245, 248, 252);
        private static readonly BaseColor ColorGrandT = new BaseColor(211, 235, 211);
        private static readonly BaseColor ColorIN = new BaseColor(213, 245, 227);
        private static readonly BaseColor ColorOUT = new BaseColor(250, 219, 216);
        private static readonly BaseColor ColorADJ = new BaseColor(253, 243, 213);
        private static readonly BaseColor ColorWhite = BaseColor.WHITE;

        private static Font FontTitle => new Font(Font.FontFamily.HELVETICA, 14, Font.BOLD, BaseColor.BLACK);
        private static Font FontSub => new Font(Font.FontFamily.HELVETICA, 9, Font.ITALIC, BaseColor.GRAY);
        private static Font FontHead => new Font(Font.FontFamily.HELVETICA, 9, Font.BOLD, BaseColor.WHITE);
        private static Font FontNormal => new Font(Font.FontFamily.HELVETICA, 8, Font.NORMAL, BaseColor.BLACK);
        private static Font FontBold => new Font(Font.FontFamily.HELVETICA, 8, Font.BOLD, BaseColor.BLACK);
        private static Font FontSmall => new Font(Font.FontFamily.HELVETICA, 7, Font.NORMAL, BaseColor.GRAY);

        // ── Laporan Penjualan ────────────────────────────────────────

        public static byte[] ExportSalesReport(
            SalesReportFilter filter,
            List<SalesSummaryRow> summary,
            List<SalesTrxRow> transactions)
        {
            using var ms = new MemoryStream();
            var doc = new Document(PageSize.A4, 36, 36, 40, 36);
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            // ── Judul ──────────────────────────────────────────────
            doc.Add(new Paragraph("LAPORAN PENJUALAN", FontTitle));
            doc.Add(new Paragraph(
                $"Periode: {filter.DateFrom:dd MMM yyyy} s/d {filter.DateTo:dd MMM yyyy}   |   " +
                $"Dibuat: {DateTime.Now:dd MMM yyyy HH:mm}", FontSub));
            doc.Add(new Chunk("\n"));

            // ── Sheet 1: Ringkasan ─────────────────────────────────
            doc.Add(new Paragraph("Ringkasan Penjualan", FontBold) { SpacingBefore = 4 });
            doc.Add(new Chunk("\n"));

            var tbl = new PdfPTable(5) { WidthPercentage = 100 };
            tbl.SetWidths(new float[] { 24f, 15f, 15f, 23f, 23f });

            AddPdfHeader(tbl, "Tanggal", "Jml Trx", "Total Item", "Total Omzet", "Rata-rata/Trx");

            decimal grandOmzet = 0; int grandTrx = 0, grandQty = 0;

            for (int i = 0; i < summary.Count; i++)
            {
                var r = summary[i];
                var bg = i % 2 == 0 ? ColorWhite : ColorAlt;
                AddPdfRow(tbl, bg, FontNormal, Element.ALIGN_LEFT,
                    r.Date,
                    r.TotalTrx.ToString(),
                    r.TotalQty.ToString(),
                    $"Rp {r.TotalOmzet:N0}",
                    $"Rp {r.AvgTrx:N0}");
                grandOmzet += r.TotalOmzet;
                grandTrx += r.TotalTrx;
                grandQty += r.TotalQty;
            }

            // Grand total
            AddPdfRow(tbl, ColorGrandT, FontBold, Element.ALIGN_LEFT,
                "TOTAL",
                grandTrx.ToString(),
                grandQty.ToString(),
                $"Rp {grandOmzet:N0}",
                grandTrx > 0 ? $"Rp {grandOmzet / grandTrx:N0}" : "Rp 0");

            doc.Add(tbl);
            doc.Add(new Chunk("\n"));

            // ── Sheet 2: Detail Transaksi ──────────────────────────
            doc.Add(new Paragraph("Detail Transaksi", FontBold) { SpacingBefore = 8 });
            doc.Add(new Chunk("\n"));

            var tbl2 = new PdfPTable(6) { WidthPercentage = 100 };
            tbl2.SetWidths(new float[] { 26f, 22f, 10f, 16f, 13f, 13f });
            AddPdfHeader(tbl2, "No. Invoice", "Waktu", "Items", "Total", "Bayar", "Kembali");

            for (int i = 0; i < transactions.Count; i++)
            {
                var t = transactions[i];
                var bg = i % 2 == 0 ? ColorWhite : ColorAlt;
                AddPdfRow(tbl2, bg, FontNormal, Element.ALIGN_LEFT,
                    t.InvoiceNo,
                    t.CreatedAt,
                    t.ItemCount.ToString(),
                    $"Rp {t.TotalAmount:N0}",
                    $"Rp {t.PaidAmount:N0}",
                    $"Rp {t.ChangeAmount:N0}");

                // Detail item dengan indent
                if (t.Items != null && t.Items.Count > 0)
                {
                    foreach (var item in t.Items)
                    {
                        var cell1 = new PdfPCell(new Phrase($"   └ {item.ProductCode}", FontSmall))
                        { Border = 0, BackgroundColor = bg, Colspan = 2, PaddingLeft = 12 };
                        var cell2 = new PdfPCell(new Phrase(item.ProductName, FontSmall))
                        { Border = 0, BackgroundColor = bg };
                        var cell3 = new PdfPCell(new Phrase($"{item.Quantity} {item.Unit}", FontSmall))
                        { Border = 0, BackgroundColor = bg, HorizontalAlignment = Element.ALIGN_CENTER };
                        var cell4 = new PdfPCell(new Phrase($"Rp {item.SellPrice:N0}", FontSmall))
                        { Border = 0, BackgroundColor = bg, HorizontalAlignment = Element.ALIGN_RIGHT };
                        var cell5 = new PdfPCell(new Phrase($"Rp {item.Subtotal:N0}", FontSmall))
                        { Border = 0, BackgroundColor = bg, HorizontalAlignment = Element.ALIGN_RIGHT };
                        tbl2.AddCell(cell1);
                        tbl2.AddCell(cell2);
                        tbl2.AddCell(cell3);
                        tbl2.AddCell(cell4);
                        tbl2.AddCell(cell5);
                    }
                }
            }

            doc.Add(tbl2);
            doc.Close();
            return ms.ToArray();
        }

        // ── Kartu Stok ───────────────────────────────────────────────

        public static byte[] ExportStockCard(StockCardFilter filter, List<StockCardRow> rows)
        {
            using var ms = new MemoryStream();
            var doc = new Document(PageSize.A4.Rotate(), 36, 36, 40, 36); // landscape
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            doc.Add(new Paragraph("KARTU STOK", FontTitle));
            doc.Add(new Paragraph(
                $"Barang: {filter.ProductCode} — {filter.ProductName}   |   Satuan: {filter.Unit}", FontSub));
            doc.Add(new Paragraph(
                $"Periode: {filter.DateFrom:dd MMM yyyy} s/d {filter.DateTo:dd MMM yyyy}   |   " +
                $"Stok Awal: {filter.StockAwal} {filter.Unit}", FontSub));
            doc.Add(new Chunk("\n"));

            var tbl = new PdfPTable(8) { WidthPercentage = 100 };
            tbl.SetWidths(new float[] { 18f, 10f, 9f, 9f, 9f, 10f, 18f, 17f });
            AddPdfHeader(tbl, "Tanggal", "Tipe", "Masuk", "Keluar", "Opname", "Stok Akhir", "Referensi", "Keterangan");

            int totalIn = 0, totalOut = 0, totalAdj = 0;

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var bg = r.Type == "IN" ? ColorIN
                       : r.Type == "OUT" ? ColorOUT
                       : ColorADJ;

                var rowCells = new[]
                {
                    new PdfPCell(new Phrase(r.Date,        FontNormal)) { BackgroundColor = bg },
                    new PdfPCell(new Phrase(r.TypeLabel,   FontBold  )) { BackgroundColor = bg, HorizontalAlignment = Element.ALIGN_CENTER },
                    new PdfPCell(new Phrase(r.QtyIn  > 0  ? r.QtyIn.ToString()  : "", FontNormal)) { BackgroundColor = bg, HorizontalAlignment = Element.ALIGN_CENTER },
                    new PdfPCell(new Phrase(r.QtyOut > 0  ? r.QtyOut.ToString() : "", FontNormal)) { BackgroundColor = bg, HorizontalAlignment = Element.ALIGN_CENTER },
                    new PdfPCell(new Phrase(r.QtyAdj != 0 ? r.QtyAdj.ToString(): "", FontNormal)) { BackgroundColor = bg, HorizontalAlignment = Element.ALIGN_CENTER },
                    new PdfPCell(new Phrase(r.StockAfter.ToString(), FontBold))   { BackgroundColor = bg, HorizontalAlignment = Element.ALIGN_CENTER },
                    new PdfPCell(new Phrase(r.Reference,   FontSmall )) { BackgroundColor = bg },
                    new PdfPCell(new Phrase(r.Notes,       FontSmall )) { BackgroundColor = bg },
                };
                foreach (var c in rowCells) tbl.AddCell(c);

                totalIn += r.QtyIn;
                totalOut += r.QtyOut;
                totalAdj += r.QtyAdj;
            }

            // Total row
            AddPdfRow(tbl, ColorGrandT, FontBold, Element.ALIGN_CENTER,
                "TOTAL", "", totalIn.ToString(), totalOut.ToString(),
                totalAdj.ToString(), "", "", "");

            doc.Add(tbl);
            doc.Close();
            return ms.ToArray();
        }

        // ── Private Helpers ──────────────────────────────────────────

        private static void AddPdfHeader(PdfPTable tbl, params string[] cols)
        {
            foreach (var h in cols)
            {
                var cell = new PdfPCell(new Phrase(h, FontHead))
                {
                    BackgroundColor = ColorHeader,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 5f
                };
                tbl.AddCell(cell);
            }
        }

        private static void AddPdfRow(PdfPTable tbl, BaseColor bg, Font font,
            int align, params string[] values)
        {
            foreach (var v in values)
            {
                var cell = new PdfPCell(new Phrase(v, font))
                {
                    BackgroundColor = bg,
                    HorizontalAlignment = align,
                    Padding = 4f
                };
                tbl.AddCell(cell);
            }
        }
    }
}
