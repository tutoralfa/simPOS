using simPOS.Shared.Reports;
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
    public class FormProfitLoss : Form
    {
        private readonly ProfitLossService _service = new ProfitLossService();

        private DateTimePicker dtpFrom, dtpTo;
        private ComboBox cmbGroupBy;
        private DataGridView dgvRows;
        private Panel pnlSummary;
        private Label lblPendapatan, lblHPP, lblLaba, lblMargin;
        private List<ProfitLossRow> _rows = new List<ProfitLossRow>();
        private ProfitLossSummary _summary = new ProfitLossSummary();
        private ProfitLossFilter _lastFilter;

        public FormProfitLoss()
        {
            InitializeComponent();
        }

        // ══════════════════════════════════════════════════════════════
        // LAYOUT
        // ══════════════════════════════════════════════════════════════

        private void InitializeComponent()
        {
            this.Text = "Laporan Laba / Rugi";
            this.Size = new Size(980, 680);
            this.MinimumSize = new Size(800, 540);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.WhiteSmoke;

            // ── Toolbar ───────────────────────────────────────────
            var toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.White,
                Padding = new Padding(12, 10, 12, 0)
            };
            toolbar.Paint += PaintBorder;

            dtpFrom = new DateTimePicker
            {
                Width = 118,
                Format = DateTimePickerFormat.Short,
                Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
            };
            dtpTo = new DateTimePicker
            {
                Width = 118,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            cmbGroupBy = new ComboBox { Width = 108, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbGroupBy.Items.AddRange(new object[] { "Per Hari", "Per Bulan" });
            cmbGroupBy.SelectedIndex = 0;

            var btnLoad = MakeBtn("🔍 Tampilkan", Color.FromArgb(52, 152, 219), 120);
            var btnToday = MakeBtn("Hari Ini", Color.FromArgb(108, 122, 137), 80);
            var btnMonth = MakeBtn("Bulan Ini", Color.FromArgb(108, 122, 137), 80);
            var btnYear = MakeBtn("Tahun Ini", Color.FromArgb(108, 122, 137), 80);
            var btnExcel = MakeBtn("📥 Excel", Color.FromArgb(39, 174, 96), 88);
            var btnPdf = MakeBtn("📄 PDF", Color.FromArgb(192, 57, 43), 76);
            var btnPrint = MakeBtn("🖨 Print", Color.FromArgb(44, 62, 80), 76);

            btnLoad.Click += (s, e) => LoadReport();
            btnToday.Click += (s, e) => { dtpFrom.Value = DateTime.Today; dtpTo.Value = DateTime.Today; LoadReport(); };
            btnMonth.Click += (s, e) => { dtpFrom.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); dtpTo.Value = DateTime.Today; LoadReport(); };
            btnYear.Click += (s, e) => { dtpFrom.Value = new DateTime(DateTime.Today.Year, 1, 1); dtpTo.Value = DateTime.Today; LoadReport(); };
            btnExcel.Click += (s, e) => ExportExcel();
            btnPdf.Click += (s, e) => ExportPdf();
            btnPrint.Click += (s, e) => PrintPreview();

            // Layout toolbar — kiri manual, export di kanan (Anchor)
            int x = 0;
            foreach (var (lbl, ctrl) in new (string, Control)[]
                { ("Dari:", dtpFrom), ("s/d:", dtpTo), ("Group:", cmbGroupBy) })
            {
                var l = new Label
                {
                    Text = lbl,
                    AutoSize = true,
                    Location = new Point(x, 17),
                    Font = new Font("Segoe UI", 9f)
                };
                toolbar.Controls.Add(l); x += l.PreferredWidth + 4;
                ctrl.Location = new Point(x, 13);
                toolbar.Controls.Add(ctrl); x += ctrl.Width + 10;
            }
            x += 10;
            foreach (var btn in new[] { btnLoad, btnToday, btnMonth, btnYear })
            {
                btn.Location = new Point(x, 11);
                toolbar.Controls.Add(btn);
                x += btn.Width + 6;
            }
            btnExcel.Anchor = btnPdf.Anchor = btnPrint.Anchor =
                AnchorStyles.Top | AnchorStyles.Right;
            btnPrint.Location = new Point(toolbar.Width - 88, 11);
            btnPdf.Location = new Point(toolbar.Width - 172, 11);
            btnExcel.Location = new Point(toolbar.Width - 268, 11);
            toolbar.Controls.AddRange(new Control[] { btnExcel, btnPdf, btnPrint });

            // ── Summary cards ─────────────────────────────────────
            pnlSummary = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 0)
            };

            lblPendapatan = MakeSummaryCard("PENDAPATAN", Color.FromArgb(52, 152, 219));
            lblHPP = MakeSummaryCard("HPP", Color.FromArgb(192, 57, 43));
            lblLaba = MakeSummaryCard("LABA KOTOR", Color.FromArgb(39, 174, 96));
            lblMargin = MakeSummaryCard("MARGIN", Color.FromArgb(142, 68, 173));

            // 4 card equal width — pakai TableLayoutPanel
            var tblCards = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1
            };
            for (int i = 0; i < 4; i++)
                tblCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblCards.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tblCards.Controls.Add(WrapCard(lblPendapatan, "PENDAPATAN", Color.FromArgb(52, 152, 219)), 0, 0);
            tblCards.Controls.Add(WrapCard(lblHPP, "HPP", Color.FromArgb(192, 57, 43)), 1, 0);
            tblCards.Controls.Add(WrapCard(lblLaba, "LABA KOTOR", Color.FromArgb(39, 174, 96)), 2, 0);
            tblCards.Controls.Add(WrapCard(lblMargin, "MARGIN", Color.FromArgb(142, 68, 173)), 3, 0);
            pnlSummary.Controls.Add(tblCards);

            // ── Grid ──────────────────────────────────────────────
            var pnlGrid = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0) };
            pnlGrid.Paint += PaintBorder;

            var lblGridTitle = new Label
            {
                Text = "  Detail per Periode",
                Dock = DockStyle.Top,
                Height = 32,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                BackColor = Color.FromArgb(245, 248, 250),
                TextAlign = ContentAlignment.MiddleLeft
            };

            dgvRows = new DataGridView
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
                Font = new Font("Segoe UI", 9.5f),
                ColumnHeadersHeight = 34,
                RowTemplate = { Height = 32 }
            };
            dgvRows.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgvRows.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvRows.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgvRows.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvRows.EnableHeadersVisualStyles = false;
            dgvRows.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
            dgvRows.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvRows.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 255);

            var cols = new (string name, string hdr, int w, DataGridViewContentAlignment align)[]
            {
                ("colPeriod",     "Periode",       130, DataGridViewContentAlignment.MiddleLeft),
                ("colTrx",        "Jml Trx",        80, DataGridViewContentAlignment.MiddleCenter),
                ("colQty",        "Total Item",      80, DataGridViewContentAlignment.MiddleCenter),
                ("colPendapatan", "Pendapatan",     150, DataGridViewContentAlignment.MiddleRight),
                ("colHPP",        "HPP",            150, DataGridViewContentAlignment.MiddleRight),
                ("colLaba",       "Laba Kotor",     150, DataGridViewContentAlignment.MiddleRight),
                ("colMargin",     "Margin %",        90, DataGridViewContentAlignment.MiddleCenter),
            };
            foreach (var (name, hdr, w, align) in cols)
            {
                var col = new DataGridViewTextBoxColumn
                { Name = name, HeaderText = hdr, Width = w };
                col.DefaultCellStyle.Alignment = align;
                dgvRows.Columns.Add(col);
            }
            dgvRows.Columns["colLaba"].DefaultCellStyle.Font =
                new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgvRows.Columns[dgvRows.Columns.Count - 1].AutoSizeMode =
                DataGridViewAutoSizeColumnMode.Fill;

            pnlGrid.Controls.Add(dgvRows);
            pnlGrid.Controls.Add(lblGridTitle);

            var sp = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent };

            // ⚠ Fill dulu
            this.Controls.Add(pnlGrid);
            this.Controls.Add(sp);
            this.Controls.Add(pnlSummary);
            this.Controls.Add(toolbar);
        }

        // ══════════════════════════════════════════════════════════════
        // LOAD DATA
        // ══════════════════════════════════════════════════════════════

        private void LoadReport()
        {
            _lastFilter = new ProfitLossFilter
            {
                DateFrom = dtpFrom.Value.Date,
                DateTo = dtpTo.Value.Date,
                GroupBy = cmbGroupBy.SelectedIndex == 1 ? "MONTH" : "DAY"
            };

            this.Cursor = Cursors.WaitCursor;
            try
            {
                _rows = _service.GetRows(_lastFilter);
                _summary = _service.GetSummary(_lastFilter);
            }
            finally { this.Cursor = Cursors.Default; }

            // Isi grid
            dgvRows.Rows.Clear();
            foreach (var r in _rows)
            {
                var idx = dgvRows.Rows.Add(
                    r.Period,
                    r.TotalTrx,
                    r.TotalQty,
                    $"Rp {r.Pendapatan:N0}",
                    $"Rp {r.HPP:N0}",
                    $"Rp {r.LabaKotor:N0}",
                    $"{r.MarginPct:N1}%");

                dgvRows.Rows[idx].Tag = r;

                // Warnai laba — merah jika rugi
                var labaCell = dgvRows.Rows[idx].Cells["colLaba"];
                labaCell.Style.ForeColor = r.LabaKotor >= 0
                    ? Color.FromArgb(39, 120, 70)
                    : Color.FromArgb(192, 57, 43);

                // Warnai margin
                var marginCell = dgvRows.Rows[idx].Cells["colMargin"];
                marginCell.Style.ForeColor = r.MarginPct >= 20
                    ? Color.FromArgb(39, 120, 70)
                    : r.MarginPct >= 10
                    ? Color.FromArgb(180, 120, 0)
                    : Color.FromArgb(192, 57, 43);
            }

            // Update summary cards
            lblPendapatan.Text = $"Rp {_summary.TotalPendapatan:N0}";
            lblHPP.Text = $"Rp {_summary.TotalHPP:N0}";
            lblLaba.Text = $"Rp {_summary.TotalLabaKotor:N0}";
            lblMargin.Text = $"{_summary.MarginPct:N1}%";

            lblLaba.ForeColor = _summary.TotalLabaKotor >= 0
                ? Color.White
                : Color.FromArgb(255, 180, 180);
        }

        // ══════════════════════════════════════════════════════════════
        // EXPORT & PRINT
        // ══════════════════════════════════════════════════════════════

        private void ExportExcel()
        {
            if (_rows.Count == 0) { MessageBox.Show("Tampilkan laporan terlebih dahulu."); return; }
            SaveFile("Excel|*.xlsx", "LabaRugi", () =>
                ProfitLossExcelExporter.Export(_lastFilter, _rows, _summary));
        }

        private void ExportPdf()
        {
            if (_rows.Count == 0) { MessageBox.Show("Tampilkan laporan terlebih dahulu."); return; }
            SaveFile("PDF|*.pdf", "LabaRugi", () =>
                ProfitLossPdfExporter.Export(_lastFilter, _rows, _summary));
        }

        private void PrintPreview()
        {
            if (_rows.Count == 0) { MessageBox.Show("Tampilkan laporan terlebih dahulu."); return; }
            try
            {
                this.Cursor = Cursors.WaitCursor;
                var doc = new ProfitLossPrinter(_lastFilter, _rows, _summary).CreateDocument();
                this.Cursor = Cursors.Default;

                var dlg = new PrintPreviewDialog
                {
                    Document = doc,
                    Text = "Print Preview — Laporan Laba/Rugi",
                    WindowState = FormWindowState.Maximized,
                    UseAntiAlias = true
                };
                dlg.ShowDialog(this);
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show($"Gagal print preview:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveFile(string filter, string name, Func<byte[]> generate)
        {
            using var dlg = new SaveFileDialog
            {
                Filter = filter,
                FileName = $"{name}_{_lastFilter.DateFrom:yyyyMMdd}_{_lastFilter.DateTo:yyyyMMdd}"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try
            {
                this.Cursor = Cursors.WaitCursor;
                var bytes = generate();
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

        // ══════════════════════════════════════════════════════════════
        // UI HELPERS
        // ══════════════════════════════════════════════════════════════

        private Label MakeSummaryCard(string title, Color color)
            => new Label
            {
                Text = "Rp 0",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

        private Panel WrapCard(Label valueLabel, string title, Color color)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = color, Margin = new Padding(4) };

            var lblTitle = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 230, 255),
                TextAlign = ContentAlignment.BottomCenter
            };
            card.Controls.Add(valueLabel);
            card.Controls.Add(lblTitle);
            return card;
        }

        private static Button MakeBtn(string text, Color color, int width)
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

        private static void PaintBorder(object sender, PaintEventArgs e)
        {
            var p = sender as Panel;
            if (p == null) return;
            e.Graphics.DrawRectangle(new Pen(Color.FromArgb(218, 220, 224)),
                0, 0, p.Width - 1, p.Height - 1);
        }
    }
}
