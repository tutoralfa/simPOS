using simPOS.Management.Forms.Categories;
using simPOS.Management.Forms.GoodsReceipts;
using simPOS.Management.Forms.Products;
using simPOS.Management.Forms.Settings;
using simPOS.Management.Forms.StockOpnameMgmt;
using simPOS.Management.Forms.SupplierMgmt;
using simPOS.Shared.Reports;
using simPOS.Shared.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace simPOS.Management.Forms.Reports
{
    public class FormLaporan : Form
    {
        private readonly ReportService _service = new ReportService();

        private SplitContainer _splitMain;
        private SplitContainer _splitRight;

        // ── Tab Penjualan ──────────────────────────────────────────
        private DateTimePicker dtpFromSales, dtpToSales;
        private ComboBox cmbGroupBy;
        private DataGridView dgvSummary, dgvTransactions, dgvTrxItems;
        private Label lblGrandTotal;

        // ── Tab Kartu Stok ─────────────────────────────────────────
        private ComboBox cmbProduct;
        private DateTimePicker dtpFromStock, dtpToStock;
        private DataGridView dgvStockCard;
        private Label lblStockInfo;

        private List<Product> _products = new List<Product>();
        private StockCardFilter _stockFilter;
        private List<StockCardRow> _stockRows = new List<StockCardRow>();
        private List<SalesSummaryRow> _summaryRows = new List<SalesSummaryRow>();
        private List<SalesTrxRow> _trxRows = new List<SalesTrxRow>();

        public FormLaporan()
        {
            InitializeComponent();
            this.Load += FormLaporan_Load;
            LoadProducts();
        }

        private void InitializeComponent()
        {
            this.Text = "simPOS — Laporan";
            this.Size = new Size(1150, 720);
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.WhiteSmoke;

            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f),
                Padding = new Point(16, 6)
            };

            tabs.TabPages.Add(BuildSalesTab());
            tabs.TabPages.Add(BuildStockCardTab());

            this.Controls.Add(tabs);
        }

        // ══════════════════════════════════════════════════════════════
        // TAB 1: LAPORAN PENJUALAN
        // ══════════════════════════════════════════════════════════════

        private TabPage BuildSalesTab()
        {
            var tab = new TabPage("📊  Laporan Penjualan") { BackColor = Color.WhiteSmoke, Padding = new Padding(10) };

            // ── Toolbar filter ─────────────────────────────────────
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.White, Padding = new Padding(10, 10, 10, 0) };
            toolbar.Paint += PaintCardBorder;

            dtpFromSales = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
            dtpToSales = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            cmbGroupBy = new ComboBox { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbGroupBy.Items.AddRange(new object[] { "Per Hari", "Per Bulan" });
            cmbGroupBy.SelectedIndex = 0;

            var btnLoad = MakeButton("🔍 Tampilkan", Color.FromArgb(52, 152, 219), 120);
            var btnToday = MakeButton("Hari Ini", Color.FromArgb(100, 100, 100), 80);
            var btnMonth = MakeButton("Bulan Ini", Color.FromArgb(100, 100, 100), 80);
            var btnExcel = MakeButton("📥 Excel", Color.FromArgb(39, 174, 96), 90);
            var btnPdf = MakeButton("📄 PDF", Color.FromArgb(192, 57, 43), 80);
            var btnPrint = MakeButton("🖨 Print", Color.FromArgb(44, 62, 80), 80);

            btnLoad.Click += (s, e) => LoadSalesReport();
            btnToday.Click += (s, e) => { dtpFromSales.Value = DateTime.Today; dtpToSales.Value = DateTime.Today; LoadSalesReport(); };
            btnMonth.Click += (s, e) => { dtpFromSales.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); dtpToSales.Value = DateTime.Today; LoadSalesReport(); };
            btnExcel.Click += (s, e) => ExportSalesExcel();
            btnPdf.Click += (s, e) => ExportSalesPdf();
            btnPrint.Click += (s, e) => PrintSalesReport();

            int x = 10;
            foreach (var (lbl, ctrl) in new (string, Control)[] {
                ("Dari:", dtpFromSales), ("s/d:", dtpToSales), ("Group:", cmbGroupBy)
            })
            {
                var l = new Label { Text = lbl, AutoSize = true, Location = new Point(x, 17), Font = new Font("Segoe UI", 9f) };
                toolbar.Controls.Add(l);
                x += l.PreferredWidth + 4;
                ctrl.Location = new Point(x, 13);
                toolbar.Controls.Add(ctrl);
                x += ctrl.Width + 12;
            }
            x += 20;
            foreach (var btn in new[] { btnLoad, btnToday, btnMonth })
            {
                btn.Location = new Point(x, 11);
                toolbar.Controls.Add(btn);
                x += btn.Width + 6;
            }
            // Export buttons di kanan
            btnPrint.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPdf.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExcel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPrint.Location = new Point(toolbar.Width - 90, 11);
            btnPdf.Location = new Point(toolbar.Width - 180, 11);
            btnExcel.Location = new Point(toolbar.Width - 280, 11);
            toolbar.Controls.AddRange(new Control[] { btnExcel, btnPdf, btnPrint });

            // ── Grand total bar ────────────────────────────────────
            lblGrandTotal = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = Color.FromArgb(44, 62, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Text = "  Pilih periode dan klik Tampilkan"
            };

            // ── Split: ringkasan (kiri) | transaksi (kanan) ────────
            _splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 6
            };
            var split = _splitMain;

            // Panel kiri: ringkasan per hari/bulan
            var pnlSummary = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0) };
            pnlSummary.Paint += PaintCardBorder;
            var lblSumTitle = new Label
            {
                Text = "  Ringkasan",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                BackColor = Color.FromArgb(245, 248, 250)
            };

            dgvSummary = MakeGrid(new[]
            {
                ("colDate",  "Tanggal",    110, "L"),
                ("colTrx",   "Trx",         45, "C"),
                ("colQty",   "Item",         45, "C"),
                ("colOmzet", "Omzet",       110, "R"),
            });
            dgvSummary.SelectionChanged += DgvSummary_SelectionChanged;

            pnlSummary.Controls.Add(dgvSummary);
            pnlSummary.Controls.Add(lblSumTitle);
            split.Panel1.Controls.Add(pnlSummary);

            // Panel kanan: detail transaksi + items
            _splitRight = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 5
            };
            var splitRight = _splitRight;

            var pnlTrx = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlTrx.Paint += PaintCardBorder;
            var lblTrxTitle = new Label
            {
                Text = "  Detail Transaksi",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                BackColor = Color.FromArgb(245, 248, 250)
            };

            dgvTransactions = MakeGrid(new[]
            {
                ("colInv",    "No. Invoice",  160, "L"),
                ("colTime",   "Waktu",        130, "L"),
                ("colItems",  "Items",         50, "C"),
                ("colTotal",  "Total",        110, "R"),
                ("colPaid",   "Dibayar",      110, "R"),
                ("colChange", "Kembali",      100, "R"),
            });
            dgvTransactions.SelectionChanged += DgvTransactions_SelectionChanged;
            pnlTrx.Controls.Add(dgvTransactions);
            pnlTrx.Controls.Add(lblTrxTitle);
            splitRight.Panel1.Controls.Add(pnlTrx);

            // Detail item transaksi terpilih
            var pnlItems = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlItems.Paint += PaintCardBorder;
            var lblItemTitle = new Label
            {
                Text = "  Item Transaksi Terpilih",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                BackColor = Color.FromArgb(245, 248, 250)
            };

            dgvTrxItems = MakeGrid(new[]
            {
                ("colCode",  "Kode",        90, "L"),
                ("colName",  "Nama Barang", 200, "L"),
                ("colQty",   "Qty",          50, "C"),
                ("colPrice", "Harga",       100, "R"),
                ("colSub",   "Subtotal",    110, "R"),
            });
            pnlItems.Controls.Add(dgvTrxItems);
            pnlItems.Controls.Add(lblItemTitle);
            splitRight.Panel2.Controls.Add(pnlItems);

            split.Panel2.Controls.Add(splitRight);

            // Spacer antara toolbar dan split
            var sp = new Panel { Dock = DockStyle.Top, Height = 6, BackColor = Color.Transparent };

            // ⚠ Urutan: Fill → Top
            tab.Controls.Add(split);
            tab.Controls.Add(lblGrandTotal);
            tab.Controls.Add(sp);
            tab.Controls.Add(toolbar);

            return tab;
        }

        // ══════════════════════════════════════════════════════════════
        // TAB 2: KARTU STOK
        // ══════════════════════════════════════════════════════════════

        private TabPage BuildStockCardTab()
        {
            var tab = new TabPage("📦  Kartu Stok") { BackColor = Color.WhiteSmoke, Padding = new Padding(10) };

            // Toolbar
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.White, Padding = new Padding(10, 10, 10, 0) };
            toolbar.Paint += PaintCardBorder;

            cmbProduct = new ComboBox { Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
            dtpFromStock = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1) };
            dtpToStock = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            var btnLoad = MakeButton("🔍 Tampilkan", Color.FromArgb(52, 152, 219), 120);
            var btnExcel = MakeButton("📥 Excel", Color.FromArgb(39, 174, 96), 90);
            var btnPdf = MakeButton("📄 PDF", Color.FromArgb(192, 57, 43), 80);
            var btnPrint = MakeButton("🖨 Print", Color.FromArgb(44, 62, 80), 80);

            btnLoad.Click += (s, e) => LoadStockCard();
            btnExcel.Click += (s, e) => ExportStockExcel();
            btnPdf.Click += (s, e) => ExportStockPdf();
            btnPrint.Click += (s, e) => PrintStockCard();

            int x = 10;
            foreach (var (lbl, ctrl) in new (string, Control)[] {
                ("Barang:", cmbProduct), ("Dari:", dtpFromStock), ("s/d:", dtpToStock)
            })
            {
                var l = new Label { Text = lbl, AutoSize = true, Location = new Point(x, 17), Font = new Font("Segoe UI", 9f) };
                toolbar.Controls.Add(l);
                x += l.PreferredWidth + 4;
                ctrl.Location = new Point(x, 13);
                toolbar.Controls.Add(ctrl);
                x += ctrl.Width + 12;
            }
            btnLoad.Location = new Point(x, 11);
            toolbar.Controls.Add(btnLoad);

            btnPrint.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPdf.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExcel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPrint.Location = new Point(toolbar.Width - 90, 11);
            btnPdf.Location = new Point(toolbar.Width - 180, 11);
            btnExcel.Location = new Point(toolbar.Width - 280, 11);
            toolbar.Controls.AddRange(new Control[] { btnExcel, btnPdf, btnPrint });

            // Info bar
            lblStockInfo = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = Color.FromArgb(44, 62, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Text = "  Pilih barang dan periode, lalu klik Tampilkan"
            };

            // Grid kartu stok
            var pnlGrid = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlGrid.Paint += PaintCardBorder;

            dgvStockCard = MakeGrid(new[]
            {
                ("colDate",  "Tanggal",    140, "L"),
                ("colType",  "Tipe",        70, "C"),
                ("colIn",    "Masuk",        65, "C"),
                ("colOut",   "Keluar",       65, "C"),
                ("colAdj",   "Opname",       65, "C"),
                ("colStock", "Stok Akhir",   85, "C"),
                ("colRef",   "Referensi",   160, "L"),
                ("colNotes", "Keterangan",  200, "L"),
            });

            pnlGrid.Controls.Add(dgvStockCard);

            var sp = new Panel { Dock = DockStyle.Top, Height = 6, BackColor = Color.Transparent };

            tab.Controls.Add(pnlGrid);
            tab.Controls.Add(lblStockInfo);
            tab.Controls.Add(sp);
            tab.Controls.Add(toolbar);

            return tab;
        }

        // ══════════════════════════════════════════════════════════════
        // LOAD DATA
        // ══════════════════════════════════════════════════════════════

        private void FormLaporan_Load(object sender, EventArgs e)
        {
            // Set SplitterDistance setelah form punya ukuran nyata
            if (_splitMain != null && _splitMain.Width > 10)
                _splitMain.SplitterDistance = Math.Max(50, _splitMain.Width / 3);

            if (_splitRight != null && _splitRight.Height > 10)
                _splitRight.SplitterDistance = Math.Max(50, (int)(_splitRight.Height * 0.55));
        }

        private void LoadProducts()
        {
            _products = _service.GetAllProducts();
            cmbProduct.Items.Clear();
            cmbProduct.Items.Add("(Pilih barang...)");
            foreach (var p in _products)
                cmbProduct.Items.Add($"{p.Code}  —  {p.Name}");
            cmbProduct.SelectedIndex = 0;
        }

        private void LoadSalesReport()
        {
            var filter = new SalesReportFilter
            {
                DateFrom = dtpFromSales.Value.Date,
                DateTo = dtpToSales.Value.Date,
                GroupBy = cmbGroupBy.SelectedIndex == 1 ? "MONTH" : "DAY"
            };

            _summaryRows = _service.GetSalesSummary(filter);
            _trxRows = _service.GetTransactions(filter);

            // Muat items untuk semua transaksi
            foreach (var t in _trxRows)
                t.Items = _service.GetTransactionItems(t.Id);

            // Isi ringkasan
            dgvSummary.Rows.Clear();
            foreach (var r in _summaryRows)
            {
                var i = dgvSummary.Rows.Add(r.Date, r.TotalTrx, r.TotalQty, $"Rp {r.TotalOmzet:N0}");
                dgvSummary.Rows[i].Tag = r;
            }

            // Isi transaksi
            FillTransactionGrid(_trxRows);

            // Grand total
            var (totalTrx, totalQty, totalOmzet) = _service.GetGrandTotal(filter);
            lblGrandTotal.Text = $"   Periode {filter.DateFrom:dd/MM/yyyy} s/d {filter.DateTo:dd/MM/yyyy}  " +
                                 $"  |  Total Transaksi: {totalTrx}  " +
                                 $"  |  Total Item: {totalQty}  " +
                                 $"  |  Total Omzet: Rp {totalOmzet:N0}";

            dgvTrxItems.Rows.Clear();
        }

        private void FillTransactionGrid(List<SalesTrxRow> rows)
        {
            dgvTransactions.Rows.Clear();
            foreach (var t in rows)
            {
                var i = dgvTransactions.Rows.Add(
                    t.InvoiceNo, t.CreatedAt, t.ItemCount,
                    $"Rp {t.TotalAmount:N0}",
                    $"Rp {t.PaidAmount:N0}",
                    $"Rp {t.ChangeAmount:N0}");
                dgvTransactions.Rows[i].Tag = t;
            }
        }

        private void LoadStockCard()
        {
            if (cmbProduct.SelectedIndex <= 0)
            {
                MessageBox.Show("Pilih barang terlebih dahulu.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var product = _products[cmbProduct.SelectedIndex - 1];
            var (found, filter) = _service.PrepareStockCard(
                product.Id, dtpFromStock.Value.Date, dtpToStock.Value.Date);

            if (!found) return;

            _stockFilter = filter;
            _stockRows = _service.GetStockCard(filter);

            dgvStockCard.Rows.Clear();
            int running = filter.StockAwal;

            // Baris stok awal
            var iAwal = dgvStockCard.Rows.Add(
                filter.DateFrom.ToString("yyyy-MM-dd") + " (AWAL)", "—", "", "", "", filter.StockAwal, "", "Stok awal periode");
            dgvStockCard.Rows[iAwal].DefaultCellStyle.BackColor = Color.FromArgb(230, 243, 255);
            dgvStockCard.Rows[iAwal].DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

            foreach (var r in _stockRows)
            {
                var idx = dgvStockCard.Rows.Add(
                    r.Date, r.TypeLabel,
                    r.QtyIn > 0 ? r.QtyIn.ToString() : "",
                    r.QtyOut > 0 ? r.QtyOut.ToString() : "",
                    r.QtyAdj != 0 ? r.QtyAdj.ToString() : "",
                    r.StockAfter,
                    r.Reference,
                    r.Notes);

                // Warna per tipe
                var bg = r.Type == "IN" ? Color.FromArgb(213, 245, 227)
                       : r.Type == "OUT" ? Color.FromArgb(250, 219, 216)
                       : Color.FromArgb(253, 243, 213);
                dgvStockCard.Rows[idx].DefaultCellStyle.BackColor = bg;
                dgvStockCard.Rows[idx].Tag = r;
            }

            lblStockInfo.Text =
                $"   {filter.ProductCode} — {filter.ProductName}  " +
                $"  |  Stok Awal: {filter.StockAwal}  " +
                $"  |  Total Masuk: {_stockRows.FindAll(r => r.Type == "IN").ConvertAll(r => r.QtyIn).Aggregate(0, (a, b) => a + b)}  " +
                $"  |  Total Keluar: {_stockRows.FindAll(r => r.Type == "OUT").ConvertAll(r => r.QtyOut).Aggregate(0, (a, b) => a + b)}  " +
                $"  |  Stok Akhir: {(_stockRows.Count > 0 ? _stockRows[_stockRows.Count - 1].StockAfter : filter.StockAwal)}";
        }

        // ══════════════════════════════════════════════════════════════
        // EVENTS
        // ══════════════════════════════════════════════════════════════

        private void DgvSummary_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvSummary.SelectedRows.Count == 0) return;
            var row = dgvSummary.SelectedRows[0].Tag as SalesSummaryRow;
            if (row == null) return;

            // Filter transaksi hanya untuk hari/bulan terpilih
            var filtered = _trxRows.FindAll(t => t.CreatedAt.StartsWith(row.Date));
            FillTransactionGrid(filtered);
            dgvTrxItems.Rows.Clear();
        }

        private void DgvTransactions_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvTransactions.SelectedRows.Count == 0) return;
            var trx = dgvTransactions.SelectedRows[0].Tag as SalesTrxRow;
            if (trx == null) return;

            dgvTrxItems.Rows.Clear();
            foreach (var item in trx.Items)
            {
                var i = dgvTrxItems.Rows.Add(
                    item.ProductCode, item.ProductName, item.Quantity,
                    $"Rp {item.SellPrice:N0}", $"Rp {item.Subtotal:N0}");
                dgvTrxItems.Rows[i].Tag = item;
            }
        }

        // ══════════════════════════════════════════════════════════════
        // EXPORT / PRINT
        // ══════════════════════════════════════════════════════════════

        private SalesReportFilter GetSalesFilter() => new SalesReportFilter
        {
            DateFrom = dtpFromSales.Value.Date,
            DateTo = dtpToSales.Value.Date,
            GroupBy = cmbGroupBy.SelectedIndex == 1 ? "MONTH" : "DAY"
        };

        private void ExportSalesExcel()
        {
            if (_summaryRows.Count == 0) { MessageBox.Show("Tampilkan laporan terlebih dahulu."); return; }
            SaveFile("Excel|*.xlsx", "LaporanPenjualan", bytes =>
                ExcelExporter.ExportSalesReport(GetSalesFilter(), _summaryRows, _trxRows));
        }

        private void ExportSalesPdf()
        {
            if (_summaryRows.Count == 0) { MessageBox.Show("Tampilkan laporan terlebih dahulu."); return; }
            SaveFile("PDF|*.pdf", "LaporanPenjualan", bytes =>
                PdfExporter.ExportSalesReport(GetSalesFilter(), _summaryRows, _trxRows));
        }

        private void PrintSalesReport()
        {
            if (_summaryRows.Count == 0) { MessageBox.Show("Tampilkan laporan terlebih dahulu."); return; }
            ShowPrintPreview(
                new SalesPrinter(GetSalesFilter(), _summaryRows, _trxRows).CreateDocument(),
                "Laporan Penjualan");
        }

        private void ExportStockExcel()
        {
            if (_stockFilter == null) { MessageBox.Show("Tampilkan kartu stok terlebih dahulu."); return; }
            SaveFile("Excel|*.xlsx", $"KartuStok_{_stockFilter.ProductCode}", bytes =>
                ExcelExporter.ExportStockCard(_stockFilter, _stockRows));
        }

        private void ExportStockPdf()
        {
            if (_stockFilter == null) { MessageBox.Show("Tampilkan kartu stok terlebih dahulu."); return; }
            SaveFile("PDF|*.pdf", $"KartuStok_{_stockFilter.ProductCode}", bytes =>
                PdfExporter.ExportStockCard(_stockFilter, _stockRows));
        }

        private void PrintStockCard()
        {
            if (_stockFilter == null) { MessageBox.Show("Tampilkan kartu stok terlebih dahulu."); return; }
            ShowPrintPreview(
                new StockCardPrinter(_stockFilter, _stockRows).CreateDocument(),
                $"Kartu Stok — {_stockFilter.ProductCode}");
        }

        // ── Helper: simpan file & buka ─────────────────────────────
        private void SaveFile(string filter, string defaultName, Func<byte[], byte[]> generate)
        {
            using var dlg = new SaveFileDialog
            {
                Filter = filter,
                FileName = $"{defaultName}_{DateTime.Today:yyyyMMdd}"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                this.Cursor = Cursors.WaitCursor;
                var bytes = generate(null);
                File.WriteAllBytes(dlg.FileName, bytes);
                this.Cursor = Cursors.Default;

                if (MessageBox.Show("File berhasil disimpan. Buka sekarang?", "Sukses",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show($"Gagal export: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowPrintPreview(System.Drawing.Printing.PrintDocument doc, string title)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                var preview = new PrintPreviewDialog
                {
                    Document = doc,
                    Text = $"Print Preview — {title}",
                    WindowState = FormWindowState.Maximized,
                    UseAntiAlias = true
                };
                this.Cursor = Cursors.Default;
                preview.ShowDialog(this);
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show($"Gagal membuka print preview:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // UI HELPERS
        // ══════════════════════════════════════════════════════════════

        private static DataGridView MakeGrid((string name, string header, int width, string align)[] cols)
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9f),
                ColumnHeadersHeight = 32,
                RowTemplate = { Height = 30 }
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.EnableHeadersVisualStyles = false;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 255);

            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            foreach (var (name, header, width, align) in cols)
            {
                var col = new DataGridViewTextBoxColumn
                {
                    Name = name,
                    HeaderText = header,
                    Width = width
                };
                col.DefaultCellStyle.Alignment =
                    align == "R" ? DataGridViewContentAlignment.MiddleRight
                  : align == "C" ? DataGridViewContentAlignment.MiddleCenter
                  : DataGridViewContentAlignment.MiddleLeft;
                dgv.Columns.Add(col);
            }
            // Kolom terakhir fill sisa
            dgv.Columns[dgv.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            return dgv;
        }

        private static Button MakeButton(string text, Color color, int width)
        {
            var btn = new Button
            {
                Text = text,
                Width = width,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(color, 0.1f);
            return btn;
        }

        private static void PaintCardBorder(object sender, PaintEventArgs e)
        {
            var p = sender as Panel;
            if (p == null) return;
            e.Graphics.DrawRectangle(new Pen(Color.FromArgb(218, 220, 224)),
                0, 0, p.Width - 1, p.Height - 1);
        }
    }
}
