using simPOS.Management.Forms.Reports;
using simPOS.Shared.Services;
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

namespace simPOS.Management.Forms.Eod
{
    public class FormEod : Form
    {
        private readonly EodService _eodSvc = new EodService();
        private readonly ClerkService _clerk = new ClerkService();

        private EodSummary _summary;

        // ── Summary cards ──────────────────────────────────────────
        private Label lblTrx, lblOmzet, lblLaba, lblMargin;

        // ── Kas section ────────────────────────────────────────────
        private Label lblSystemCash, lblDifference;
        private TextBox txtPhysical;
        private Label lblDiffStatus;

        // ── Grid items ─────────────────────────────────────────────
        private DataGridView dgvItems;

        // ── Notes & action ─────────────────────────────────────────
        private TextBox txtNotes;
        private Button btnSave, btnPrint;
        private Label lblSessionStatus;

        // [BARU] Tanggal target EOD — null = hari ini, string = tanggal spesifik
        private readonly string _targetDate;

        public FormEod() : this(null) { }

        // [BARU] Constructor untuk EOD tanggal tertentu (misal kemarin)
        public FormEod(string targetDate)
        {
            _targetDate = targetDate ?? DateTime.Today.ToString("yyyy-MM-dd");
            InitializeComponent();
            this.Load += (s, e) => LoadData();
        }

        // ══════════════════════════════════════════════════════════════
        // LAYOUT
        // ══════════════════════════════════════════════════════════════

        private void InitializeComponent()
        {
            this.Text = "End of Day (EOD)";
            this.Size = new Size(900, 700);
            this.MinimumSize = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 246, 250);

            // ── Header ───────────────────────────────────────────
            var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.FromArgb(44, 62, 80) };
            header.Controls.Add(new Label
            {
                // [DIUBAH] Judul mengikuti _targetDate
                Text = $"📋  End of Day  —  {(DateTime.TryParse(_targetDate, out var _td) ? _td : DateTime.Today):dddd, dd MMMM yyyy}",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            });

            lblSessionStatus = new Label
            {
                Dock = DockStyle.Right,
                Width = 220,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 16, 0)
            };
            header.Controls.Add(lblSessionStatus);

            // ── Main layout: kiri (detail) | kanan (kas + aksi) ──
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 6,
                Panel1MinSize = 100,
                Panel2MinSize = 100
            };
            // [DIUBAH] Child form tidak fire Shown — pakai Load + BeginInvoke
            // BeginInvoke memastikan form sudah punya ukuran nyata sebelum set splitter
            void SetSplitter()
            {
                int minTotal = split.Panel1MinSize + split.Panel2MinSize + split.SplitterWidth;
                if (split.Width <= minTotal) return;
                int max = split.Width - split.Panel2MinSize - split.SplitterWidth;
                int min = split.Panel1MinSize;
                if (max <= min) return;
                int target = (int)(split.Width * 0.60);
                split.SplitterDistance = Math.Max(min, Math.Min(max, target));
            }
            this.Load += (s, e) => this.BeginInvoke(new Action(SetSplitter));
            this.Resize += (s, e) => SetSplitter();

            // Panel kiri: summary cards + grid items
            split.Panel1.Controls.Add(BuildLeftPanel());

            // Panel kanan: kas + catatan + tombol
            split.Panel2.Controls.Add(BuildRightPanel());

            this.Controls.Add(split);
            this.Controls.Add(header);
        }

        // ── Panel kiri ────────────────────────────────────────────

        private Panel BuildLeftPanel()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(12, 10, 6, 12) };

            // Summary cards (4 card)
            var tblCards = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 80,
                ColumnCount = 4,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8)
            };
            for (int i = 0; i < 4; i++)
                tblCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblCards.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tblCards.Controls.Add(MakeCard("TRANSAKSI", out lblTrx, Color.FromArgb(52, 152, 219)), 0, 0);
            tblCards.Controls.Add(MakeCard("OMZET", out lblOmzet, Color.FromArgb(39, 174, 96)), 1, 0);
            tblCards.Controls.Add(MakeCard("LABA KOTOR", out lblLaba, Color.FromArgb(142, 68, 173)), 2, 0);
            tblCards.Controls.Add(MakeCard("MARGIN", out lblMargin, Color.FromArgb(230, 126, 34)), 3, 0);

            // Grid item terjual
            var pnlGrid = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0) };
            pnlGrid.Paint += PaintBorder;

            var lblGridTitle = new Label
            {
                Text = "  Detail Barang Terjual Hari Ini",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                BackColor = Color.FromArgb(245, 248, 250)
            };

            dgvItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9f),
                ColumnHeadersHeight = 30,
                RowTemplate = { Height = 28 }
            };
            dgvItems.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgvItems.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvItems.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            dgvItems.EnableHeadersVisualStyles = false;
            dgvItems.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 255);
            dgvItems.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
            dgvItems.DefaultCellStyle.SelectionForeColor = Color.White;

            foreach (var (name, hdr, w, align) in new[]
            {
                ("colCode",  "Kode",         80, DataGridViewContentAlignment.MiddleLeft),
                ("colName",  "Nama Barang",  200, DataGridViewContentAlignment.MiddleLeft),
                ("colUnit",  "Sat",           45, DataGridViewContentAlignment.MiddleCenter),
                ("colQty",   "Qty",           55, DataGridViewContentAlignment.MiddleCenter),
                ("colOmzet", "Omzet",        110, DataGridViewContentAlignment.MiddleRight),
                ("colLaba",  "Laba",         110, DataGridViewContentAlignment.MiddleRight),
            })
            {
                var col = new DataGridViewTextBoxColumn { Name = name, HeaderText = hdr, Width = w };
                col.DefaultCellStyle.Alignment = align;
                dgvItems.Columns.Add(col);
            }
            dgvItems.Columns["colName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            pnlGrid.Controls.Add(dgvItems);
            pnlGrid.Controls.Add(lblGridTitle);

            var sp = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent };

            pnl.Controls.Add(pnlGrid);
            pnl.Controls.Add(sp);
            pnl.Controls.Add(tblCards);
            return pnl;
        }

        // ── Panel kanan ───────────────────────────────────────────

        private Panel BuildRightPanel()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(6, 10, 12, 12) };

            // Kas section
            var pnlKas = new Panel
            {
                Dock = DockStyle.Top,
                Height = 220,
                BackColor = Color.White,
                Padding = new Padding(14, 10, 14, 10)
            };
            pnlKas.Paint += PaintBorder;

            var lblKasTitle = new Label
            {
                Text = "  💰  Setoran Kas",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                BackColor = Color.FromArgb(245, 248, 250)
            };

            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };

            lblSystemCash = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(60, 60, 60)
            };

            var lblPhysLabel = new Label
            {
                Text = "Uang Fisik (hasil hitung):",
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Padding = new Padding(0, 8, 0, 0)
            };

            txtPhysical = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Right,
                Text = "0"
            };
            txtPhysical.TextChanged += (s, e) => UpdateDifference();
            txtPhysical.KeyPress += (s, e) =>
            {
                // Hanya angka
                if (!char.IsDigit(e.KeyChar) && e.KeyChar != '\b')
                    e.Handled = true;
            };

            // Separator
            var sep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(220, 220, 220), Margin = new Padding(0, 8, 0, 8) };

            lblDifference = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            };

            lblDiffStatus = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            body.Controls.Add(lblDiffStatus);
            body.Controls.Add(lblDifference);
            body.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 6, BackColor = Color.Transparent });
            body.Controls.Add(sep);
            body.Controls.Add(txtPhysical);
            body.Controls.Add(lblPhysLabel);
            body.Controls.Add(lblSystemCash);

            pnlKas.Controls.Add(body);
            pnlKas.Controls.Add(lblKasTitle);

            // Catatan
            var pnlNotes = new Panel
            {
                Dock = DockStyle.Top,
                Height = 130,
                BackColor = Color.White,
                Padding = new Padding(14, 10, 14, 10),
                Margin = new Padding(0, 8, 0, 0)
            };
            pnlNotes.Paint += PaintBorder;

            txtNotes = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.None,
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText = "Catatan tambahan..."
            };

            var lblNotesTitle = new Label
            {
                Text = "  📝  Catatan",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                BackColor = Color.FromArgb(245, 248, 250)
            };

            pnlNotes.Controls.Add(txtNotes);
            pnlNotes.Controls.Add(lblNotesTitle);

            // Action buttons
            var pnlBtns = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 90,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 0)
            };

            btnPrint = new Button
            {
                Text = "🖨  Print Laporan",
                Dock = DockStyle.Top,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 4)
            };
            btnPrint.FlatAppearance.BorderSize = 0;
            btnPrint.Click += BtnPrint_Click;

            btnSave = new Button
            {
                Text = "✅  Simpan & Selesaikan EOD",
                Dock = DockStyle.Bottom,
                Height = 42,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            pnlBtns.Controls.Add(btnSave);
            pnlBtns.Controls.Add(btnPrint);

            var sp2 = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent };

            pnl.Controls.Add(pnlBtns);
            pnl.Controls.Add(pnlNotes);
            pnl.Controls.Add(sp2);
            pnl.Controls.Add(pnlKas);
            return pnl;
        }

        // ══════════════════════════════════════════════════════════════
        // LOAD DATA
        // ══════════════════════════════════════════════════════════════

        private void LoadData()
        {
            // [DIUBAH] Gunakan _targetDate
            bool isToday = _targetDate == DateTime.Today.ToString("yyyy-MM-dd");
            var session = isToday ? _clerk.GetTodaySession() : null;
            bool isOpen = session?.IsOpen == true;
            bool eodDone = _clerk.IsEodDoneForDate(_targetDate);

            lblSessionStatus.Text = isOpen
                ? "🟢 Kasir: BUKA"
                : "🔴 Kasir: TUTUP";

            // [DIUBAH] Tidak ada gate di sini — form selalu bisa dibuka
            // Validasi isOpen / eodDone dipindah ke BtnSave_Click
            if (eodDone)
            {
                lblSessionStatus.Text = "✅ EOD sudah selesai";
                lblSessionStatus.ForeColor = Color.FromArgb(100, 230, 130);
            }

            // [DIUBAH] Load summary untuk _targetDate
            _summary = _eodSvc.GetSummaryByDate(_targetDate);

            // Summary cards
            lblTrx.Text = _summary.TotalTrx.ToString("N0");
            lblOmzet.Text = $"Rp {_summary.TotalOmzet:N0}";
            lblLaba.Text = $"Rp {_summary.TotalLaba:N0}";
            lblMargin.Text = $"{_summary.MarginPct:N1}%";

            // Kas
            lblSystemCash.Text = $"Uang Sistem (omzet):   Rp {_summary.TotalOmzet:N0}";
            txtPhysical.Text = ((long)_summary.TotalOmzet).ToString();
            UpdateDifference();

            // Grid items
            dgvItems.Rows.Clear();
            foreach (var item in _summary.Items)
            {
                var idx = dgvItems.Rows.Add(
                    item.ProductCode,
                    item.ProductName,
                    item.Unit,
                    item.TotalQty,
                    $"Rp {item.TotalOmzet:N0}",
                    $"Rp {item.TotalLaba:N0}");

                dgvItems.Rows[idx].Tag = item;
                dgvItems.Rows[idx].Cells["colLaba"].Style.ForeColor =
                    item.TotalLaba >= 0 ? Color.FromArgb(39, 120, 70) : Color.FromArgb(192, 57, 43);
            }

            if (_summary.TotalTrx == 0)
            {
                dgvItems.Rows.Add("", "Tidak ada transaksi hari ini", "", "", "", "");
                dgvItems.Rows[0].DefaultCellStyle.ForeColor = Color.Gray;
                dgvItems.Rows[0].DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Italic);
            }
        }

        private void UpdateDifference()
        {
            if (_summary == null) return;

            if (!decimal.TryParse(txtPhysical.Text.Trim(), out decimal physical))
                physical = 0;

            decimal diff = physical - _summary.TotalOmzet;

            if (diff == 0)
            {
                lblDifference.Text = "Selisih: Rp 0";
                lblDifference.ForeColor = Color.FromArgb(39, 120, 70);
                lblDiffStatus.Text = "✅  Kas Pas";
                lblDiffStatus.BackColor = Color.FromArgb(220, 245, 225);
                lblDiffStatus.ForeColor = Color.FromArgb(39, 120, 70);
            }
            else if (diff > 0)
            {
                lblDifference.Text = $"Selisih: +Rp {diff:N0}  (LEBIH)";
                lblDifference.ForeColor = Color.FromArgb(52, 152, 219);
                lblDiffStatus.Text = $"➕  Lebih Rp {diff:N0}";
                lblDiffStatus.BackColor = Color.FromArgb(219, 234, 254);
                lblDiffStatus.ForeColor = Color.FromArgb(30, 100, 180);
            }
            else
            {
                lblDifference.Text = $"Selisih: -Rp {Math.Abs(diff):N0}  (KURANG)";
                lblDifference.ForeColor = Color.FromArgb(192, 57, 43);
                lblDiffStatus.Text = $"➖  Kurang Rp {Math.Abs(diff):N0}";
                lblDiffStatus.BackColor = Color.FromArgb(254, 226, 226);
                lblDiffStatus.ForeColor = Color.FromArgb(180, 40, 40);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // EVENTS
        // ══════════════════════════════════════════════════════════════

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // [DIUBAH] Validasi pakai _targetDate
            bool eodDone = _clerk.IsEodDoneForDate(_targetDate);

            if (eodDone)
            {
                MessageBox.Show(
                    "EOD hari ini sudah pernah dilakukan dan tidak dapat diulang.",
                    "EOD Sudah Selesai", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // [BARU] Tidak bisa EOD jika belum ada transaksi hari ini
            if (_summary == null || _summary.TotalTrx == 0)
            {
                MessageBox.Show(
                    "Belum ada transaksi hari ini.\nEOD hanya bisa dilakukan jika sudah ada penjualan.",
                    "Tidak Ada Transaksi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // ─────────────────────────────────────────────────────────

            if (!decimal.TryParse(txtPhysical.Text.Trim(), out decimal physical))
            {
                MessageBox.Show("Masukkan jumlah uang fisik yang valid.", "Validasi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPhysical.Focus();
                return;
            }

            decimal diff = physical - _summary.TotalOmzet;
            string diffMsg = diff == 0 ? "✅ Kas pas."
                           : diff > 0 ? $"⚠ Kas LEBIH Rp {diff:N0}"
                           : $"⚠ Kas KURANG Rp {Math.Abs(diff):N0}";

            var confirm = MessageBox.Show(
                $"Simpan laporan EOD hari ini?\n\n" +
                $"Total Omzet : Rp {_summary.TotalOmzet:N0}\n" +
                $"Uang Fisik  : Rp {physical:N0}\n" +
                $"{diffMsg}\n\n" +
                "EOD tidak dapat diulang setelah disimpan.",
                "Konfirmasi EOD",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                btnSave.Enabled = false;
                _eodSvc.SaveEod(_summary, physical, txtNotes.Text.Trim());

                MessageBox.Show(
                    "EOD berhasil disimpan!\n\nLaporan hari ini telah dicatat.",
                    "EOD Selesai",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Reload untuk update status
                LoadData();
                // [DIUBAH] Setelah EOD sukses, disable tombol dengan tampilan selesai
                btnSave.Text = "✅ EOD Sudah Dilakukan";
                btnSave.BackColor = Color.FromArgb(100, 130, 100);
                btnSave.Enabled = false;
            }
            catch (Exception ex)
            {
                btnSave.Enabled = true;
                MessageBox.Show($"Gagal simpan EOD:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (_summary == null) return;

            if (!decimal.TryParse(txtPhysical.Text.Trim(), out decimal physical))
                physical = _summary.TotalOmzet;

            try
            {
                var doc = new EodPrinter(_summary, physical, txtNotes.Text).CreateDocument();
                var preview = new PrintPreviewDialog
                {
                    Document = doc,
                    Text = "Print Preview — Laporan EOD",
                    WindowState = FormWindowState.Maximized,
                    UseAntiAlias = true
                };
                preview.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal print:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // UI HELPERS
        // ══════════════════════════════════════════════════════════════

        private static Panel MakeCard(string title, out Label valueLabel, Color color)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = color, Margin = new Padding(3) };
            var lblT = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 230, 255),
                TextAlign = ContentAlignment.BottomCenter
            };
            valueLabel = new Label
            {
                Text = "—",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(valueLabel);
            card.Controls.Add(lblT);
            return card;
        }

        private static void PaintBorder(object sender, PaintEventArgs e)
        {
            var p = sender as Panel;
            if (p == null) return;
            e.Graphics.DrawRectangle(new Pen(Color.FromArgb(218, 220, 224)),
                0, 0, p.Width - 1, p.Height - 1);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // PRINTER
    // ══════════════════════════════════════════════════════════════════

    internal class EodPrinter : BaseReportPrinter
    {
        private readonly EodSummary _summary;
        private readonly decimal _physical;
        private readonly string _notes;
        private int _itemIndex = 0;
        private bool _headerDrawn = false;

        public EodPrinter(EodSummary summary, decimal physical, string notes)
        {
            _summary = summary;
            _physical = physical;
            _notes = notes;
        }

        public override PrintDocument CreateDocument()
        {
            var pd = new PrintDocument();
            pd.DefaultPageSettings.Landscape = false;
            pd.DefaultPageSettings.Margins = new Margins((int)ML, (int)MR, (int)MT, (int)MB);
            _itemIndex = 0;
            _headerDrawn = false;
            _currentPage = 1;
            pd.PrintPage += OnPrintPage;
            return pd;
        }

        private ColumnDef[] Cols(float w) => new[]
        {
            new ColumnDef("Kode",        w * 0.14f),
            new ColumnDef("Nama Barang", w * 0.34f),
            new ColumnDef("Sat",         w * 0.07f, HAlign.Center),
            new ColumnDef("Qty",         w * 0.07f, HAlign.Center),
            new ColumnDef("Omzet",       w * 0.19f, HAlign.Right),
            new ColumnDef("Laba",        w * 0.19f, HAlign.Right),
        };

        private void OnPrintPage(object sender, PrintPageEventArgs e)
        {
            var g = e.Graphics;
            InitPage(e);
            var cw = _contentWidth;

            if (_currentPage == 1)
            {
                // Header
                DrawPageHeader(g,
                    "LAPORAN END OF DAY (EOD)",
                    $"Tanggal: {DateTime.Today:dddd, dd MMMM yyyy}   |   Dicetak: {DateTime.Now:HH:mm:ss}");

                // Summary bar 4 kotak
                DrawSummaryBar(g, cw);

                // Kas section
                DrawKasSection(g, cw);
                _yPos += 6f;
            }

            // Header tabel
            var cols = Cols(cw);
            if (!_headerDrawn)
            {
                if (_currentPage > 1)
                {
                    g.DrawString("LAPORAN EOD (lanjutan)", FBold, Brushes.Gray, ML, _yPos);
                    _yPos += FBold.Height + 6f;
                }
                else
                {
                    g.DrawString("Detail Barang Terjual", FBold, Brushes.Black, ML, _yPos);
                    _yPos += FBold.Height + 6f;
                }
                DrawTableHeader(g, cols);
                _headerDrawn = true;
            }

            // Rows
            while (_itemIndex < _summary.Items.Count)
            {
                var item = _summary.Items[_itemIndex];
                var bg = _itemIndex % 2 == 0 ? Color.White : Color.FromArgb(245, 248, 252);
                var ok = DrawRow(g, cols, new[]
                {
                    item.ProductCode,
                    item.ProductName,
                    item.Unit,
                    item.TotalQty.ToString(),
                    $"Rp {item.TotalOmzet:N0}",
                    $"Rp {item.TotalLaba:N0}"
                }, bg);

                if (!ok)
                {
                    DrawPageFooter(g, _currentPage++);
                    _headerDrawn = false;
                    e.HasMorePages = true;
                    return;
                }
                _itemIndex++;
            }

            // Total baris
            DrawRow(g, cols, new[]
            {
                "TOTAL", "", "",
                _summary.TotalQty.ToString(),
                $"Rp {_summary.TotalOmzet:N0}",
                $"Rp {_summary.TotalLaba:N0}"
            }, Color.FromArgb(211, 235, 211), bold: true);

            // Catatan
            if (!string.IsNullOrWhiteSpace(_notes))
            {
                _yPos += 10f;
                g.DrawString($"Catatan: {_notes}", FSmall, Brushes.Gray, ML, _yPos);
                _yPos += FSmall.Height + 4f;
            }

            // TTD area
            DrawSignatureArea(g, cw);

            DrawPageFooter(g, _currentPage);
            e.HasMorePages = false;
        }

        private void DrawSummaryBar(Graphics g, float cw)
        {
            float cardW = cw / 4f;
            float cardH = 48f;
            float x = ML;

            var cards = new[]
            {
                ("TRANSAKSI",  _summary.TotalTrx.ToString(),           Color.FromArgb(52, 152, 219)),
                ("OMZET",      $"Rp {_summary.TotalOmzet:N0}",         Color.FromArgb(39, 174, 96)),
                ("LABA KOTOR", $"Rp {_summary.TotalLaba:N0}",          Color.FromArgb(142, 68, 173)),
                ("MARGIN",     $"{_summary.MarginPct:N1}%",             Color.FromArgb(230, 126, 34)),
            };

            foreach (var (title, value, color) in cards)
            {
                g.FillRectangle(new SolidBrush(color), x, _yPos, cardW - 3f, cardH);
                g.DrawString(title, FSmall, Brushes.White,
                    new RectangleF(x, _yPos + 3f, cardW - 3f, 16f),
                    new StringFormat { Alignment = StringAlignment.Center });
                g.DrawString(value, new System.Drawing.Font("Segoe UI", 10f, FontStyle.Bold),
                    Brushes.White,
                    new RectangleF(x, _yPos + 18f, cardW - 3f, 26f),
                    new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    });
                x += cardW;
            }
            _yPos += cardH + 10f;
        }

        private void DrawKasSection(Graphics g, float cw)
        {
            decimal diff = _physical - _summary.TotalOmzet;

            // Background kotak kas
            var kasRect = new RectangleF(ML, _yPos, cw, 52f);
            g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), kasRect);
            g.DrawRectangle(new Pen(Color.FromArgb(200, 210, 220)), kasRect.X, kasRect.Y, kasRect.Width, kasRect.Height);

            float lx = ML + 8f;
            g.DrawString($"Uang Sistem   : Rp {_summary.TotalOmzet:N0}",
                FNormal, Brushes.Black, lx, _yPos + 6f);
            g.DrawString($"Uang Fisik    : Rp {_physical:N0}",
                FBold, Brushes.Black, lx, _yPos + 22f);

            string diffText = diff == 0 ? "✔ Kas Pas"
                            : diff > 0 ? $"➕ Lebih Rp {diff:N0}"
                            : $"➖ Kurang Rp {Math.Abs(diff):N0}";
            var diffBrush = diff == 0 ? new SolidBrush(Color.FromArgb(39, 120, 70))
                          : diff > 0 ? new SolidBrush(Color.FromArgb(30, 100, 180))
                          : new SolidBrush(Color.FromArgb(180, 40, 40));

            g.DrawString($"Selisih       : {diffText}",
                FBold, diffBrush, lx, _yPos + 36f);

            _yPos += 62f;
        }

        private void DrawSignatureArea(Graphics g, float cw)
        {
            if (_yPos + 80f > _pageHeight - MB) return;

            _yPos += 20f;
            float col = cw / 3f;

            for (int i = 0; i < 3; i++)
            {
                float x = ML + i * col;
                var lbl = i == 0 ? "Kasir" : i == 1 ? "Supervisor" : "Manager";
                g.DrawString(lbl, FSmall, Brushes.Gray, x + col / 2 - 20f, _yPos);
                g.DrawLine(new Pen(Color.FromArgb(180, 180, 180)),
                    x + 8, _yPos + 40f, x + col - 12, _yPos + 40f);
                g.DrawString("(________________)", FSmall, Brushes.Gray,
                    x + 10f, _yPos + 42f);
            }
        }
    }
}
