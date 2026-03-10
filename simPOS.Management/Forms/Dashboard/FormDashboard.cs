using simPOS.Shared.Reports;
using System.Windows.Forms.DataVisualization.Charting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace simPOS.Management.Forms.Dashboard
{
    public class FormDashboard : Form
    {
        private readonly DashboardService _svc = new DashboardService();

        // ── Summary cards ──────────────────────────────────────────
        private Label lblOmzet, lblLaba, lblTrx, lblQty;
        private Label lblProd, lblCat, lblSup;

        // ── Chart ──────────────────────────────────────────────────
        private Chart _chart;
        private ComboBox cmbChartRange;

        // ── Top produk ─────────────────────────────────────────────
        private DataGridView dgvTop;
        private ComboBox cmbTopRange;

        // ── Stok menipis ───────────────────────────────────────────
        private DataGridView dgvLowStock;

        // ── Refresh ────────────────────────────────────────────────
        private System.Windows.Forms.Timer _autoRefresh;
        private TableLayoutPanel _body;
        private Label lblLastUpdate;

        public FormDashboard()
        {
            InitializeComponent();
            this.Load += (s, e) =>
            {
                // Build controls setelah form punya ukuran nyata
                _body.Controls.Add(BuildSummaryRow(), 0, 0);
                _body.Controls.Add(BuildMiddleRow(), 0, 1);
                _body.Controls.Add(BuildLowStockRow(), 0, 2);
                // RefreshAll dijalankan setelah semua control selesai di-render
                this.BeginInvoke(new Action(RefreshAll));
            };
        }

        // ══════════════════════════════════════════════════════════════
        // LAYOUT
        // ══════════════════════════════════════════════════════════════

        private void InitializeComponent()
        {
            this.Text = "Dashboard";
            this.BackColor = Color.FromArgb(245, 246, 250);
            this.Dock = DockStyle.Fill;

            // ── Header bar ────────────────────────────────────────
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(44, 62, 80)
            };

            var lblTitle = new Label
            {
                Text = "📊  Dashboard",
                Dock = DockStyle.Left,
                Width = 220,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            };

            lblLastUpdate = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 180, 200),
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 12, 0)
            };

            var btnRefresh = new Button
            {
                Text = "⟳  Refresh",
                Dock = DockStyle.Right,
                Width = 100,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => RefreshAll();

            header.Controls.Add(lblLastUpdate);
            header.Controls.Add(btnRefresh);
            header.Controls.Add(lblTitle);

            // ── Body: TableLayoutPanel 3 baris, full-width ──────
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.FromArgb(245, 246, 250),
                Padding = new Padding(14, 10, 14, 10),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 96f)); // summary cards
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 55f)); // chart + top produk
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 45f)); // stok menipis

            _body = body; // simpan referensi, isi saat Load

            // ── Auto refresh setiap 5 menit ───────────────────────
            //_autoRefresh = new System.Windows.Forms.Timer { Interval = 5 * 60 * 1000 };
            //_autoRefresh.Tick += (s, e) => RefreshAll();
            //_autoRefresh.Start();

            // ⚠ Fill dulu
            this.Controls.Add(body);
            this.Controls.Add(header);
        }

        // ── Row 1: Summary cards ──────────────────────────────────

        private Control BuildSummaryRow()
        {
            var row = new TableLayoutPanel
            {
                ColumnCount = 7,
                RowCount = 1,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            for (int i = 0; i < 7; i++)
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 7f));
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Hari ini
            row.Controls.Add(MakeLabelCard("OMZET HARI INI", out lblOmzet, Color.FromArgb(52, 152, 219)), 0, 0);
            row.Controls.Add(MakeLabelCard("LABA HARI INI", out lblLaba, Color.FromArgb(39, 174, 96)), 1, 0);
            row.Controls.Add(MakeLabelCard("TRANSAKSI", out lblTrx, Color.FromArgb(142, 68, 173)), 2, 0);
            row.Controls.Add(MakeLabelCard("ITEM TERJUAL", out lblQty, Color.FromArgb(230, 126, 34)), 3, 0);

            // Pemisah
            var sep = new Panel
            {
                BackColor = Color.FromArgb(220, 225, 230),
                Width = 2,
                Margin = new Padding(4, 8, 4, 8),
                Dock = DockStyle.Fill
            };
            row.Controls.Add(sep, 4, 0);

            // Master data
            row.Controls.Add(MakeLabelCard("PRODUK AKTIF", out lblProd, Color.FromArgb(44, 62, 80)), 5, 0);
            row.Controls.Add(MakeLabelCard("KATEGORI", out lblCat, Color.FromArgb(44, 62, 80)), 6, 0);

            // Wrap ke Panel agar resize bisa dihandle
            row.Dock = DockStyle.Fill;
            return row;
        }

        // ── Row 2: Grafik + Top produk ────────────────────────────

        private Control BuildMiddleRow()
        {
            var row = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            row.Controls.Add(BuildChartCard(), 0, 0);
            row.Controls.Add(BuildTopCard(), 1, 0);

            row.Dock = DockStyle.Fill;
            return row;
        }

        private Panel BuildChartCard()
        {
            var card = MakeCard("Grafik Omzet", out var body);

            // Toolbar chart
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = Color.White };

            cmbChartRange = new ComboBox
            {
                Width = 130,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(0, 5)
            };
            cmbChartRange.Items.AddRange(new object[] { "7 Hari Terakhir", "30 Hari Terakhir" });
            cmbChartRange.SelectedIndex = 0;
            cmbChartRange.SelectedIndexChanged += (s, e) => LoadChart();
            toolbar.Controls.Add(cmbChartRange);

            // Chart
            _chart = new Chart
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            var area = new ChartArea("main")
            {
                BackColor = Color.White,
                BorderColor = Color.FromArgb(220, 220, 220),
            };
            area.AxisX.MajorGrid.LineColor = Color.FromArgb(235, 235, 235);
            area.AxisX.LabelStyle.Font = new Font("Segoe UI", 7.5f);
            area.AxisX.LabelStyle.Angle = -40;
            area.AxisX.LineColor = Color.FromArgb(200, 200, 200);
            area.AxisX.MajorTickMark.LineColor = Color.Transparent;

            area.AxisY.MajorGrid.LineColor = Color.FromArgb(235, 235, 235);
            area.AxisY.LabelStyle.Font = new Font("Segoe UI", 7.5f);
            area.AxisY.LineColor = Color.FromArgb(200, 200, 200);
            area.AxisY.LabelStyle.Format = "#,0";

            area.InnerPlotPosition = new ElementPosition(8, 5, 88, 82);
            _chart.ChartAreas.Add(area);

            // Series Omzet
            var serOmzet = new Series("Omzet")
            {
                ChartType = SeriesChartType.Column,
                Color = Color.FromArgb(52, 152, 219),
                XValueType = ChartValueType.String,
                IsValueShownAsLabel = false,
                BorderWidth = 0,
                ["PointWidth"] = "0.6"
            };
            _chart.Series.Add(serOmzet);

            // Series Laba
            var serLaba = new Series("Laba")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.FromArgb(39, 174, 96),
                BorderWidth = 2,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 6,
                MarkerColor = Color.FromArgb(39, 174, 96),
                XValueType = ChartValueType.String
            };
            _chart.Series.Add(serLaba);

            var legend = new Legend
            {
                Docking = Docking.Top,
                Alignment = StringAlignment.Far,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8f),
                BorderColor = Color.Transparent
            };
            _chart.Legends.Add(legend);

            body.Controls.Add(_chart);
            body.Controls.Add(toolbar);
            return card;
        }

        private Panel BuildTopCard()
        {
            var card = MakeCard("Produk Terlaris", out var body);

            var toolbar = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = Color.White };
            cmbTopRange = new ComboBox
            {
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(0, 5)
            };
            cmbTopRange.Items.AddRange(new object[] { "Hari Ini", "30 Hari Ini" });
            cmbTopRange.SelectedIndex = 0;
            cmbTopRange.SelectedIndexChanged += (s, e) => LoadTopProducts();
            toolbar.Controls.Add(cmbTopRange);

            dgvTop = MakeGrid(new[]
            {
                ("#",         28, DataGridViewContentAlignment.MiddleCenter),
                ("Nama",     170, DataGridViewContentAlignment.MiddleLeft),
                ("Qty",       48, DataGridViewContentAlignment.MiddleCenter),
                ("Omzet",    100, DataGridViewContentAlignment.MiddleRight),
            });

            body.Controls.Add(dgvTop);
            body.Controls.Add(toolbar);
            return card;
        }

        // ── Row 3: Stok menipis ───────────────────────────────────

        private Control BuildLowStockRow()
        {
            var card = MakeCard("⚠  Stok Menipis", out var body);
            card.Dock = DockStyle.Fill;

            dgvLowStock = MakeGrid(new[]
            {
                ("Kode",       90, DataGridViewContentAlignment.MiddleLeft),
                ("Nama Barang",240, DataGridViewContentAlignment.MiddleLeft),
                ("Satuan",      60, DataGridViewContentAlignment.MiddleCenter),
                ("Stok",        60, DataGridViewContentAlignment.MiddleCenter),
                ("Min. Stok",   70, DataGridViewContentAlignment.MiddleCenter),
                ("Status",     110, DataGridViewContentAlignment.MiddleCenter),
            });

            body.Controls.Add(dgvLowStock);
            return card;
        }

        // ══════════════════════════════════════════════════════════════
        // LOAD DATA
        // ══════════════════════════════════════════════════════════════

        private void RefreshAll()
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                LoadSummaryCards();
                LoadChart();
                LoadTopProducts();
                LoadLowStock();
                lblLastUpdate.Text = $"Terakhir diperbarui: {DateTime.Now:HH:mm:ss}";
            }
            finally { this.Cursor = Cursors.Default; }
        }

        private void LoadSummaryCards()
        {
            if (lblOmzet == null || lblLaba == null) return;
            var (trx, qty, omzet, laba) = _svc.GetTodaySummary();
            var (prod, cat, sup) = _svc.GetMasterCount();

            lblOmzet.Text = FormatRupiah(omzet);
            lblLaba.Text = FormatRupiah(laba);
            lblTrx.Text = trx.ToString("N0");
            lblQty.Text = qty.ToString("N0");
            lblProd.Text = prod.ToString("N0");
            lblCat.Text = cat.ToString("N0");

            // Warna laba — merah jika negatif
            lblLaba.ForeColor = laba < 0 ? Color.FromArgb(255, 160, 160) : Color.White;
        }

        private void LoadChart()
        {
            if (_chart == null || cmbChartRange == null) return;
            if (!_chart.Series.IsUniqueName("Omzet") == false &&
                _chart.Series.FindByName("Omzet") == null) return;

            int days = cmbChartRange.SelectedIndex == 1 ? 30 : 7;
            var data = _svc.GetOmzetTrend(days) ??
                       new System.Collections.Generic.List<(string, decimal, decimal)>();

            _chart.Series["Omzet"].Points.Clear();
            _chart.Series["Laba"].Points.Clear();

            // Pastikan semua hari terwakili (isi 0 untuk hari tanpa transaksi)
            var dict = new Dictionary<string, (decimal Omzet, decimal Laba)>();
            foreach (var (date, omzet, laba) in data)
                dict[date] = (omzet, laba);

            for (int i = days - 1; i >= 0; i--)
            {
                var day = DateTime.Today.AddDays(-i);
                var key = day.ToString("yyyy-MM-dd");
                var label = day.ToString(days <= 7 ? "ddd dd/MM" : "dd/MM");

                var omzet = dict.ContainsKey(key) ? (double)dict[key].Omzet : 0;
                var laba = dict.ContainsKey(key) ? (double)dict[key].Laba : 0;

                _chart.Series["Omzet"].Points.AddXY(label, omzet);
                _chart.Series["Laba"].Points.AddXY(label, laba);
            }

            // Warnai bar hari ini
            var todayLabel = DateTime.Today.ToString(days <= 7 ? "ddd dd/MM" : "dd/MM");
            foreach (DataPoint pt in _chart.Series["Omzet"].Points)
            {
                if (pt.AxisLabel == todayLabel)
                    pt.Color = Color.FromArgb(230, 126, 34);
            }
        }

        private void LoadTopProducts()
        {
            if (dgvTop == null || cmbTopRange == null) return;
            bool todayOnly = cmbTopRange.SelectedIndex == 0;
            var data = _svc.GetTopProducts(8, todayOnly);

            dgvTop.Rows.Clear();
            for (int i = 0; i < data.Count; i++)
            {
                var (code, name, qty, omzet) = data[i];
                var rowIdx = dgvTop.Rows.Add(i + 1, name, qty, $"Rp {omzet:N0}");
                dgvTop.Rows[rowIdx].Tag = data[i];

                // Top 3 highlight
                if (i == 0) dgvTop.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 220);
                else if (i == 1) dgvTop.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
                else if (i == 2) dgvTop.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(250, 235, 215);
            }

            if (data.Count == 0)
                dgvTop.Rows.Add("", "Belum ada data", "", "");
        }

        private void LoadLowStock()
        {
            if (dgvLowStock == null) return;
            var data = _svc.GetLowStock() ??
                       new System.Collections.Generic.List<(string, string, int, int, string)>();
            dgvLowStock.Rows.Clear();

            foreach (var (code, name, stock, minStock, unit) in data)
            {
                string status;
                Color bgColor;

                if (stock == 0)
                {
                    status = "🔴 HABIS";
                    bgColor = Color.FromArgb(255, 220, 220);
                }
                else if (stock <= minStock / 2)
                {
                    status = "🟠 KRITIS";
                    bgColor = Color.FromArgb(255, 235, 205);
                }
                else
                {
                    status = "🟡 MENIPIS";
                    bgColor = Color.FromArgb(255, 250, 205);
                }

                var rowIdx = dgvLowStock.Rows.Add(code, name, unit, stock, minStock, status);
                dgvLowStock.Rows[rowIdx].DefaultCellStyle.BackColor = bgColor;

                // Stok 0 = bold merah
                if (stock == 0)
                {
                    dgvLowStock.Rows[rowIdx].Cells["colStok"].Style.ForeColor = Color.FromArgb(180, 0, 0);
                    dgvLowStock.Rows[rowIdx].Cells["colStok"].Style.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                }
            }

            if (data.Count == 0)
            {
                var rowIdx = dgvLowStock.Rows.Add("", "✅  Semua stok aman", "", "", "", "");
                dgvLowStock.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(39, 120, 70);
                dgvLowStock.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(220, 245, 230);
                dgvLowStock.Rows[rowIdx].DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // UI HELPERS
        // ══════════════════════════════════════════════════════════════

        /// <summary>Card putih dengan title bar abu-abu atas.</summary>
        private static Panel MakeCard(string title, out Panel body)
        {
            var card = new Panel
            {
                BackColor = Color.White,
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                Padding = new Padding(0)
            };
            card.Paint += (s, e) => e.Graphics.DrawRectangle(
                new Pen(Color.FromArgb(218, 220, 224)), 0, 0, card.Width - 1, card.Height - 1);

            var titleBar = new Label
            {
                Text = "  " + title,
                Dock = DockStyle.Top,
                Height = 32,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                BackColor = Color.FromArgb(245, 248, 250),
                TextAlign = ContentAlignment.MiddleLeft
            };

            body = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(4) };
            card.Controls.Add(body);
            card.Controls.Add(titleBar);

            return card;
        }

        /// <summary>Summary card berwarna dengan nilai besar di tengah.</summary>
        private static Panel MakeLabelCard(string title, out Label valueLabel, Color color)
        {
            var card = new Panel
            {
                BackColor = color,
                Dock = DockStyle.Fill,
                Margin = new Padding(3)
            };

            var lblTitle = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 230, 255),
                TextAlign = ContentAlignment.BottomCenter
            };

            valueLabel = new Label
            {
                Text = "—",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };

            card.Controls.Add(valueLabel);
            card.Controls.Add(lblTitle);
            return card;
        }

        private static DataGridView MakeGrid(
            (string name, int width, DataGridViewContentAlignment align)[] cols)
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
                ColumnHeadersHeight = 28,
                RowTemplate = { Height = 26 },
                ScrollBars = ScrollBars.Vertical
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;

            foreach (var (name, width, align) in cols)
            {
                var colName = "col" + name.Replace(" ", "").Replace(".", "").Replace("#", "No");
                var col = new DataGridViewTextBoxColumn
                { Name = colName, HeaderText = name, Width = width };
                col.DefaultCellStyle.Alignment = align;
                dgv.Columns.Add(col);
            }
            dgv.Columns[dgv.Columns.Count - 1].AutoSizeMode =
                DataGridViewAutoSizeColumnMode.Fill;

            return dgv;
        }

        private static string FormatRupiah(decimal val)
        {
            if (val >= 1_000_000_000) return $"Rp {val / 1_000_000_000:N1}M";
            if (val >= 1_000_000) return $"Rp {val / 1_000_000:N1}jt";
            return $"Rp {val:N0}";
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _autoRefresh?.Stop();
            _autoRefresh?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
