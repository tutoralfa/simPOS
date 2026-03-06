using simPOS.Shared.Models;
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

namespace simPOS.Management.Forms.GoodsReceipts
{
    public class FormGoodsReceiptList : Form
    {
        private readonly GoodsReceiptService _service = new GoodsReceiptService();

        private TextBox txtSearch;
        private Button btnTambah;
        private Button btnDetail;
        private DataGridView dgv;
        private Label lblTotal;

        private List<GoodsReceipt> _allReceipts;

        public FormGoodsReceiptList()
        {
            InitializeComponent();
            LoadReceipts();
        }

        private void InitializeComponent()
        {
            this.Text = "simPOS — Penerimaan Barang";
            this.Size = new Size(1000, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.WhiteSmoke;

            BuildGrid();
            BuildStatusBar();
            BuildToolbar();
        }

        private void BuildToolbar()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.White,
                Padding = new Padding(10, 10, 10, 0)
            };

            var lblSearch = new Label
            {
                Text = "Cari:",
                AutoSize = true,
                Location = new Point(10, 18),
                Font = new Font("Segoe UI", 9f)
            };

            txtSearch = new TextBox
            {
                Location = new Point(50, 14),
                Width = 280,
                Font = new Font("Segoe UI", 9f),
                PlaceholderText = "No. penerimaan atau nama supplier..."
            };
            txtSearch.TextChanged += (s, e) => FilterGrid(txtSearch.Text);

            btnTambah = MakeButton("➕ Terima Barang", Color.FromArgb(39, 174, 96), bold: true, width: 140);
            btnDetail = MakeButton("🔍 Detail", Color.FromArgb(52, 152, 219), bold: false, width: 90);

            btnTambah.Location = new Point(panel.Width - 250, 12);
            btnDetail.Location = new Point(panel.Width - 100, 12);

            btnTambah.Click += BtnTambah_Click;
            btnDetail.Click += BtnDetail_Click;

            panel.Controls.AddRange(new Control[] { lblSearch, txtSearch, btnTambah, btnDetail });
            this.Controls.Add(panel);
        }

        private void BuildGrid()
        {
            dgv = new DataGridView
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9f),
                ColumnHeadersHeight = 35
            };

            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.EnableHeadersVisualStyles = false;

            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 252);

            dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) OpenDetail(); };

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", FillWeight = 30 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReceiptNo", HeaderText = "No. Penerimaan", FillWeight = 130 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplier", HeaderText = "Supplier", FillWeight = 160 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItems", HeaderText = "Jml Item", FillWeight = 60 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNotes", HeaderText = "Catatan", FillWeight = 200 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "Tanggal Terima", FillWeight = 120 });

            dgv.Columns["colId"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["colItems"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["colReceiptNo"].DefaultCellStyle.Font = new Font("Consolas", 9f);

            this.Controls.Add(dgv);
        }

        private void BuildStatusBar()
        {
            lblTotal = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                BackColor = Color.FromArgb(44, 62, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            this.Controls.Add(lblTotal);
        }

        private void LoadReceipts()
        {
            _allReceipts = _service.GetAll();
            FilterGrid(txtSearch?.Text ?? "");
        }

        private void FilterGrid(string keyword)
        {
            dgv.Rows.Clear();

            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? _allReceipts
                : _allReceipts.FindAll(r =>
                    r.ReceiptNo.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.SupplierName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var r in filtered)
            {
                var rowIdx = dgv.Rows.Add(
                    r.Id,
                    r.ReceiptNo,
                    r.SupplierName,
                    r.TotalItems,
                    r.Notes,
                    r.ReceivedAt
                );
                dgv.Rows[rowIdx].Tag = r.Id;
            }

            lblTotal.Text = $"  Total: {filtered.Count} dokumen penerimaan";
        }

        private void BtnTambah_Click(object sender, EventArgs e)
        {
            var form = new FormGoodsReceiptEntry();
            if (form.ShowDialog() == DialogResult.OK)
                LoadReceipts();
        }

        private void BtnDetail_Click(object sender, EventArgs e) => OpenDetail();

        private void OpenDetail()
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih dokumen penerimaan untuk melihat detail.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var id = (int)dgv.SelectedRows[0].Tag;
            var form = new FormGoodsReceiptDetail(id);
            form.ShowDialog();
        }

        private static Button MakeButton(string text, Color color, bool bold, int width = 100)
        {
            var btn = new Button
            {
                Text = text,
                Width = width,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, bold ? FontStyle.Bold : FontStyle.Regular),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }
    }
}
