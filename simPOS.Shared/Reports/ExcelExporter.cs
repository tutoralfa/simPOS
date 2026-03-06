using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace simPOS.Shared.Reports
{
    /// <summary>
    /// Export laporan ke .xlsx menggunakan ClosedXML.
    /// NuGet: ClosedXML (versi 0.102+)
    /// </summary>
    public static class ExcelExporter
    {
        // ── Laporan Penjualan ────────────────────────────────────────

        public static byte[] ExportSalesReport(
            SalesReportFilter filter,
            List<SalesSummaryRow> summary,
            List<SalesTrxRow> transactions)
        {
            using var wb = new XLWorkbook();

            // Sheet 1: Ringkasan
            var wsSummary = wb.AddWorksheet("Ringkasan");
            WriteSalesSummarySheet(wsSummary, filter, summary);

            // Sheet 2: Detail Transaksi
            var wsDetail = wb.AddWorksheet("Detail Transaksi");
            WriteSalesDetailSheet(wsDetail, transactions);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private static void WriteSalesSummarySheet(IXLWorksheet ws,
            SalesReportFilter filter, List<SalesSummaryRow> rows)
        {
            // Judul
            ws.Cell("A1").Value = "LAPORAN PENJUALAN";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 14;

            ws.Cell("A2").Value = $"Periode : {filter.DateFrom:dd/MM/yyyy} s/d {filter.DateTo:dd/MM/yyyy}";
            ws.Cell("A3").Value = $"Dibuat  : {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Cell("A3").Style.Font.Italic = true;

            // Header tabel
            int row = 5;
            var headers = new[] { "Tanggal", "Jml Transaksi", "Total Item", "Total Omzet", "Rata-rata/Trx" };
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
            decimal grandTotal = 0;
            int grandTrx = 0, grandQty = 0;

            foreach (var r in rows)
            {
                ws.Cell(row, 1).Value = r.Date;
                ws.Cell(row, 2).Value = r.TotalTrx;
                ws.Cell(row, 3).Value = r.TotalQty;
                ws.Cell(row, 4).Value = (double)r.TotalOmzet;
                ws.Cell(row, 5).Value = (double)r.AvgTrx;

                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Zebra stripe
                if (row % 2 == 0)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromArgb(245, 248, 252);

                grandTotal += r.TotalOmzet;
                grandTrx += r.TotalTrx;
                grandQty += r.TotalQty;
                row++;
            }

            // Grand total row
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 2).Value = grandTrx;
            ws.Cell(row, 3).Value = grandQty;
            ws.Cell(row, 4).Value = (double)grandTotal;
            ws.Cell(row, 5).Value = grandTrx > 0 ? (double)(grandTotal / grandTrx) : 0;

            var totalRange = ws.Range(row, 1, row, 5);
            totalRange.Style.Font.Bold = true;
            totalRange.Style.Fill.BackgroundColor = XLColor.FromArgb(211, 235, 211);
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";

            // Auto-fit kolom
            ws.Columns().AdjustToContents();
            ws.Column(4).Width = 18;
            ws.Column(5).Width = 18;
        }

        private static void WriteSalesDetailSheet(IXLWorksheet ws, List<SalesTrxRow> trxList)
        {
            ws.Cell("A1").Value = "DETAIL TRANSAKSI";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 13;

            int row = 3;
            var headers = new[] { "No. Invoice", "Waktu", "Jml Item", "Total", "Dibayar", "Kembali", "Metode" };
            for (int c = 0; c < headers.Length; c++)
            {
                var cell = ws.Cell(row, c + 1);
                cell.Value = headers[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(44, 62, 80);
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            row++;

            int itemStartRow = row;

            foreach (var t in trxList)
            {
                ws.Cell(row, 1).Value = t.InvoiceNo;
                ws.Cell(row, 2).Value = t.CreatedAt;
                ws.Cell(row, 3).Value = t.ItemCount;
                ws.Cell(row, 4).Value = (double)t.TotalAmount;
                ws.Cell(row, 5).Value = (double)t.PaidAmount;
                ws.Cell(row, 6).Value = (double)t.ChangeAmount;
                ws.Cell(row, 7).Value = t.PaymentMethod;

                foreach (var col in new[] { 4, 5, 6 })
                    ws.Cell(row, col).Style.NumberFormat.Format = "#,##0";

                ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (row % 2 == 0)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromArgb(245, 248, 252);

                // Sub-rows: item detail dengan indentasi
                if (t.Items != null && t.Items.Count > 0)
                {
                    row++;
                    // Sub-header item
                    ws.Cell(row, 2).Value = "  Kode";
                    ws.Cell(row, 3).Value = "  Nama Barang";
                    ws.Cell(row, 4).Value = "Qty";
                    ws.Cell(row, 5).Value = "Harga";
                    ws.Cell(row, 6).Value = "Subtotal";
                    ws.Row(row).Style.Font.Italic = true;
                    ws.Row(row).Style.Font.FontColor = XLColor.Gray;

                    foreach (var item in t.Items)
                    {
                        row++;
                        ws.Cell(row, 2).Value = $"    {item.ProductCode}";
                        ws.Cell(row, 3).Value = $"    {item.ProductName}";
                        ws.Cell(row, 4).Value = item.Quantity;
                        ws.Cell(row, 5).Value = (double)item.SellPrice;
                        ws.Cell(row, 6).Value = (double)item.Subtotal;
                        ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                        ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                        ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                        ws.Row(row).Style.Font.FontColor = XLColor.FromArgb(80, 80, 80);
                        ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromArgb(252, 252, 252);
                    }
                }
                row++;
            }

            ws.Columns().AdjustToContents();
            ws.Column(1).Width = 22;
            ws.Column(3).Width = 28;
        }

        // ── Kartu Stok ───────────────────────────────────────────────

        public static byte[] ExportStockCard(StockCardFilter filter, List<StockCardRow> rows)
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Kartu Stok");

            // Header info
            ws.Cell("A1").Value = "KARTU STOK";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 14;

            ws.Cell("A2").Value = $"Barang  : {filter.ProductCode} — {filter.ProductName}";
            ws.Cell("A3").Value = $"Periode : {filter.DateFrom:dd/MM/yyyy} s/d {filter.DateTo:dd/MM/yyyy}";
            ws.Cell("A4").Value = $"Stok Awal Periode : {filter.StockAwal} {filter.Unit}";
            ws.Cell("A4").Style.Font.Bold = true;

            // Header tabel
            int row = 6;
            var headers = new[] { "Tanggal", "Tipe", "Masuk", "Keluar", "Opname", "Stok Akhir", "Referensi", "Keterangan" };
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

            int totalIn = 0, totalOut = 0, totalAdj = 0;

            foreach (var r in rows)
            {
                ws.Cell(row, 1).Value = r.Date;
                ws.Cell(row, 2).Value = r.TypeLabel;
                ws.Cell(row, 3).Value = (XLCellValue)(r.QtyIn > 0 ? r.QtyIn : (object)"");
                ws.Cell(row, 4).Value = (XLCellValue)(r.QtyOut > 0 ? r.QtyOut : (object)"");
                ws.Cell(row, 5).Value = (XLCellValue)(r.QtyAdj != 0 ? r.QtyAdj : (object)"");
                ws.Cell(row, 6).Value = r.StockAfter;
                ws.Cell(row, 7).Value = r.Reference;
                ws.Cell(row, 8).Value = r.Notes;

                // Warna per tipe
                var typeColor = r.Type == "IN"
                    ? XLColor.FromArgb(213, 245, 227)
                    : r.Type == "OUT"
                    ? XLColor.FromArgb(250, 219, 216)
                    : XLColor.FromArgb(253, 243, 213);

                ws.Cell(row, 2).Style.Fill.BackgroundColor = typeColor;
                ws.Cell(row, 6).Style.Font.Bold = true;

                totalIn += r.QtyIn;
                totalOut += r.QtyOut;
                totalAdj += r.QtyAdj;
                row++;
            }

            // Total row
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 3).Value = totalIn;
            ws.Cell(row, 4).Value = totalOut;
            ws.Cell(row, 5).Value = totalAdj;

            var totRange = ws.Range(row, 1, row, 8);
            totRange.Style.Font.Bold = true;
            totRange.Style.Fill.BackgroundColor = XLColor.FromArgb(211, 235, 211);

            ws.Columns().AdjustToContents();
            ws.Column(1).Width = 20;
            ws.Column(8).Width = 30;

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
