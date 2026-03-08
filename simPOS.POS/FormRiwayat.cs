using simPOS.Shared.Printing;
using simPOS.Shared.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using simPOS.Shared.Models;
using simPOS.Shared.Printing;
using simPOS.Shared.Services;

namespace simPOS.POS
{
    public class FormRiwayat : Form
    {
        private readonly TransactionService _service = new();
        private List<Transaction> _sales = new();

        private DataGridView _dgv;
        private DataGridView _dgvDetail;
        private Label _lblSummary;
        private TextBox _txtSearch;

        public FormRiwayat()
        {
            InitUI();
            this.Load += (s, e) => LoadSales();
        }

        private void InitUI()
        {
            Text = "Riwayat Penjualan Hari Ini";
            Size = new Size(820, 560);
            MinimumSize = new Size(700, 460);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(245, 246, 250);

            // ── Header ───────────────────────────────────────────────
            var header = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.FromArgb(44, 62, 80) };
            var lblTitle = new Label
            {
                Text = $"📋  Riwayat Penjualan — {DateTime.Today:dd MMMM yyyy}",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0)
            };
            var btnRefresh = new Button
            {
                Text = "🔄",
                Width = 40,
                Height = 34,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => LoadSales();
            header.Controls.Add(btnRefresh);
            header.Controls.Add(lblTitle);

            // ── Toolbar: search + summary ─────────────────────────────
            var toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.White,
                Padding = new Padding(10, 6, 10, 4)
            };
            toolbar.Paint += (s, e) => e.Graphics.DrawLine(
                new Pen(Color.FromArgb(218, 220, 224)), 0, toolbar.Height - 1, toolbar.Width, toolbar.Height - 1);

            _txtSearch = new TextBox
            {
                Width = 220,
                Height = 26,
                Font = new Font("Segoe UI", 9f),
                PlaceholderText = "🔍 Cari no. bon / item...",
                Location = new Point(0, 4)
            };
            _txtSearch.TextChanged += (s, e) => FilterGrid();

            _lblSummary = new Label
            {
                Text = "",
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location = new Point(230, 8)
            };

            toolbar.Controls.Add(_txtSearch);
            toolbar.Controls.Add(_lblSummary);

            // ── Split: atas (list transaksi) | bawah (detail item) ───
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 5,
                Panel1MinSize = 10,
                Panel2MinSize = 10,
                BackColor = Color.FromArgb(218, 220, 224)
            };
            split.ClientSizeChanged += (s, e) =>
            {
                if (split.Height <= split.Panel1MinSize + split.Panel2MinSize + split.SplitterWidth) return;
                int t = (int)(split.Height * 0.55f);
                int max = split.Height - split.Panel2MinSize - split.SplitterWidth;
                split.SplitterDistance = Math.Max(split.Panel1MinSize, Math.Min(max, t));
            };

            // ── Grid atas: daftar transaksi ───────────────────────────
            var pnlTop = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 8, 10, 4) };
            var lblTop = new Label
            {
                Text = "Daftar Transaksi",
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80)
            };

            _dgv = MakeGrid();
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNo", HeaderText = "No Bon", Width = 130 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTime", HeaderText = "Waktu", Width = 75 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItems", HeaderText = "Item", Width = 45 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal", HeaderText = "Total", Width = 110 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPay", HeaderText = "Bayar", Width = 110 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colChg", HeaderText = "Kembali", Width = 90 });
            _dgv.Columns["colItems"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgv.Columns["colTotal"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgv.Columns["colPay"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgv.Columns["colChg"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgv.Columns["colNo"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // Tombol reprint per baris
            var colReprint = new DataGridViewButtonColumn
            {
                Name = "colReprint",
                HeaderText = "Struk",
                Text = "🖨",
                UseColumnTextForButtonValue = true,
                Width = 40
            };
            colReprint.DefaultCellStyle.Font = new Font("Segoe UI", 10f);
            colReprint.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colReprint.DefaultCellStyle.BackColor = Color.FromArgb(240, 248, 255);
            _dgv.Columns.Add(colReprint);

            _dgv.SelectionChanged += DgvMain_SelectionChanged;
            _dgv.CellClick += DgvMain_CellClick;

            pnlTop.Controls.Add(_dgv);
            pnlTop.Controls.Add(lblTop);
            split.Panel1.Controls.Add(pnlTop);

            // ── Grid bawah: detail item transaksi terpilih ────────────
            var pnlBot = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 6, 10, 8) };
            var lblBot = new Label
            {
                Text = "Detail Item",
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80)
            };

            _dgvDetail = MakeGrid();
            _dgvDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "dColCode", HeaderText = "Kode", Width = 90 });
            _dgvDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "dColName", HeaderText = "Nama Barang" });
            _dgvDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "dColQty", HeaderText = "Qty", Width = 50 });
            _dgvDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "dColPrice", HeaderText = "Harga", Width = 105 });
            _dgvDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "dColSub", HeaderText = "Subtotal", Width = 105 });
            _dgvDetail.Columns["dColName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _dgvDetail.Columns["dColQty"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvDetail.Columns["dColPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgvDetail.Columns["dColSub"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            pnlBot.Controls.Add(_dgvDetail);
            pnlBot.Controls.Add(lblBot);
            split.Panel2.Controls.Add(pnlBot);

            // ── Footer ────────────────────────────────────────────────
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Color.White,
                Padding = new Padding(10, 6, 10, 6)
            };
            footer.Paint += (s, e) => e.Graphics.DrawLine(
                new Pen(Color.FromArgb(218, 220, 224)), 0, 0, footer.Width, 0);

            var btnReprintSel = new Button
            {
                Text = "🖨  Cetak Ulang Struk",
                Width = 170,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(0, 0)
            };
            btnReprintSel.FlatAppearance.BorderSize = 0;
            btnReprintSel.FlatAppearance.MouseOverBackColor = Color.FromArgb(41, 128, 185);
            btnReprintSel.Click += BtnReprintSelected_Click;

            var btnClose = new Button
            {
                Text = "Tutup",
                Width = 80,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                Location = new Point(178, 0)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Close();

            footer.Controls.Add(btnReprintSel);
            footer.Controls.Add(btnClose);

            Controls.Add(split);
            Controls.Add(footer);
            Controls.Add(toolbar);
            Controls.Add(header);
        }

        // ══════════════════════════════════════════════════════════════
        // DATA
        // ══════════════════════════════════════════════════════════════

        private void LoadSales()
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                _sales = _service.GetTodaySales();
            }
            catch { _sales = new List<Transaction>(); }
            this.Cursor = Cursors.Default;
            FilterGrid();
        }

        private void FilterGrid()
        {
            string kw = _txtSearch.Text.Trim().ToLower();
            var filtered = string.IsNullOrEmpty(kw)
                ? _sales
                : _sales.Where(t =>
                    t.InvoiceNo.ToLower().Contains(kw) ||
                    t.Items.Any(i => i.ProductName.ToLower().Contains(kw) ||
                                     i.ProductCode.ToLower().Contains(kw))
                  ).ToList();

            FillGrid(filtered);
            UpdateSummary(filtered);
        }

        private void FillGrid(List<Transaction> list)
        {
            _dgv.Rows.Clear();
            foreach (var t in list)
            {
                string time = t.CreatedAt.Length >= 16
                    ? t.CreatedAt.Substring(11, 5) : t.CreatedAt;
                int idx = _dgv.Rows.Add(
                    t.InvoiceNo,
                    time,
                    t.Items.Count,
                    $"Rp {t.TotalAmount:N0}",
                    $"Rp {t.PaidAmount:N0}",
                    $"Rp {t.ChangeAmount:N0}",
                    "🖨"
                );
                _dgv.Rows[idx].Tag = t;
            }

            _dgvDetail.Rows.Clear();
        }

        private void UpdateSummary(List<Transaction> list)
        {
            int count = list.Count;
            decimal total = list.Sum(t => t.TotalAmount);
            _lblSummary.Text = $"  {count} transaksi  |  Total: Rp {total:N0}";
        }

        // ══════════════════════════════════════════════════════════════
        // EVENTS
        // ══════════════════════════════════════════════════════════════

        private void DgvMain_SelectionChanged(object sender, EventArgs e)
        {
            _dgvDetail.Rows.Clear();
            if (_dgv.SelectedRows.Count == 0) return;
            if (_dgv.SelectedRows[0].Tag is not Transaction trx) return;

            foreach (var item in trx.Items)
            {
                int idx = _dgvDetail.Rows.Add(
                    item.ProductCode,
                    item.ProductName,
                    item.Quantity,
                    $"Rp {item.SellPrice:N0}",
                    $"Rp {item.Subtotal:N0}"
                );
            }
        }

        private void DgvMain_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (_dgv.Columns[e.ColumnIndex].Name != "colReprint") return;
            if (_dgv.Rows[e.RowIndex].Tag is Transaction trx)
                ReprintTrx(trx);
        }

        private void BtnReprintSelected_Click(object sender, EventArgs e)
        {
            if (_dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih transaksi terlebih dahulu.",
                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_dgv.SelectedRows[0].Tag is Transaction trx)
                ReprintTrx(trx);
        }

        // ══════════════════════════════════════════════════════════════
        // REPRINT
        // ══════════════════════════════════════════════════════════════

        private void ReprintTrx(Transaction trx)
        {
            var confirm = MessageBox.Show(
                $"Cetak ulang struk:\nNo  : {trx.InvoiceNo}\nTotal: Rp {trx.TotalAmount:N0}",
                "Konfirmasi Reprint", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (confirm != DialogResult.Yes) return;

            var cfg = PrinterConfig.Load();
            if (!cfg.PrintEnabled) { ShowMsg("Printer dinonaktifkan di pengaturan."); return; }
            if (string.IsNullOrWhiteSpace(cfg.PrinterName))
            {
                MessageBox.Show("Printer belum dikonfigurasi.\nBuka simPOS Management → Pengaturan → Printer.",
                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var b = new EscPosBuilder(cfg.CharPerLine);
                b.Initialize()
                 .Center().Bold().Font2H().TextLine(cfg.StoreName)
                 .Normal().Center();
                if (!string.IsNullOrWhiteSpace(cfg.StoreAddress)) b.TextLine(cfg.StoreAddress);
                if (!string.IsNullOrWhiteSpace(cfg.StorePhone)) b.TextLine(cfg.StorePhone);

                b.Divider('=').Left()
                 .TextLine($"No  : {trx.InvoiceNo}")
                 .TextLine($"Tgl : {trx.CreatedAt}")
                 .Center().TextLine("** REPRINT **").Left()
                 .Divider();

                foreach (var item in trx.Items)
                {
                    string name = item.ProductName.Length > cfg.CharPerLine
                        ? item.ProductName.Substring(0, cfg.CharPerLine - 2) + ".."
                        : item.ProductName;
                    b.TextLine(name);
                    b.LeftRight($"  {item.Quantity}x {item.SellPrice:N0}", $"Rp {item.Subtotal:N0}");
                }

                b.Divider('=')
                 .Bold().LeftRight("TOTAL", $"Rp {trx.TotalAmount:N0}")
                 .NoBold().LeftRight("BAYAR", $"Rp {trx.PaidAmount:N0}")
                 .Bold().LeftRight("KEMBALI", $"Rp {trx.ChangeAmount:N0}")
                 .Divider('=').Normal()
                 .NewLine().Center().TextLine(cfg.FooterMessage).NewLine(2);

                if (cfg.AutoCut) b.Cut();
                EscPosPrinter.PrintRaw(b.Build(), cfg.PrinterName);

                ShowMsg($"✅ Struk {trx.InvoiceNo} dicetak ulang.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal mencetak:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════

        private void ShowMsg(string msg)
        {
            MessageBox.Show(msg, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static DataGridView MakeGrid()
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 8.5f),
                ColumnHeadersHeight = 28,
                RowTemplate = { Height = 26 },
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            return dgv;
        }
    }
}
