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

namespace simPOS.Management.Forms.StockOpnameMgmt
{
    public class FormStockOpnameList : Form
    {
        private readonly StockOpnameService _service = new StockOpnameService();

        private TextBox txtSearch;
        private Button btnTambah;
        private Button btnDetail;
        private DataGridView dgv;
        private Label lblTotal;

        private List<StockOpname> _allOpnames;

        public FormStockOpnameList()
        {
            InitializeComponent();
            LoadOpnames();
        }

        private void InitializeComponent()
        {
            this.Text = "simPOS — Stock Opname";
            this.Size = new Size(900, 560);
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

            var lblSearch = new Label { Text = "Cari:", AutoSize = true, Location = new Point(10, 18), Font = new Font("Segoe UI", 9f) };
            txtSearch = new TextBox
            {
                Location = new Point(50, 14),
                Width = 260,
                Font = new Font("Segoe UI", 9f),
                PlaceholderText = "No. opname atau catatan..."
            };
            txtSearch.TextChanged += (s, e) => FilterGrid(txtSearch.Text);

            btnTambah = MakeButton("➕ Opname Baru", Color.FromArgb(39, 174, 96), bold: true, width: 130);
            btnDetail = MakeButton("🔍 Detail", Color.FromArgb(52, 152, 219), bold: false, width: 90);

            btnTambah.Location = new Point(panel.Width - 240, 12);
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
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNo", HeaderText = "No. Opname", FillWeight = 140 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "Status", FillWeight = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNotes", HeaderText = "Catatan", FillWeight = 250 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCreated", HeaderText = "Dibuat", FillWeight = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colConfirmed", HeaderText = "Dikonfirmasi", FillWeight = 120 });

            dgv.Columns["colId"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["colStatus"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["colNo"].DefaultCellStyle.Font = new Font("Consolas", 9f);

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
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            this.Controls.Add(lblTotal);
        }

        private void LoadOpnames()
        {
            _allOpnames = _service.GetAll();
            FilterGrid(txtSearch?.Text ?? "");
        }

        private void FilterGrid(string keyword)
        {
            dgv.Rows.Clear();

            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? _allOpnames
                : _allOpnames.FindAll(o =>
                    o.OpnameNo.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    o.Notes.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var o in filtered)
            {
                var rowIdx = dgv.Rows.Add(
                    o.Id,
                    o.OpnameNo,
                    o.Status,
                    o.Notes,
                    o.CreatedAt,
                    o.ConfirmedAt
                );
                dgv.Rows[rowIdx].Tag = o.Id;

                // Warna status
                var statusCell = dgv.Rows[rowIdx].Cells["colStatus"];
                if (o.IsConfirmed)
                {
                    statusCell.Style.BackColor = Color.FromArgb(213, 245, 227);
                    statusCell.Style.ForeColor = Color.FromArgb(30, 130, 76);
                    statusCell.Style.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                }
                else
                {
                    statusCell.Style.BackColor = Color.FromArgb(254, 249, 219);
                    statusCell.Style.ForeColor = Color.FromArgb(180, 120, 0);
                }
            }

            lblTotal.Text = $"  Total: {filtered.Count} sesi opname";
        }

        private void BtnTambah_Click(object sender, EventArgs e)
        {
            var form = new FormStockOpnameEntry();
            if (form.ShowDialog() == DialogResult.OK)
                LoadOpnames();
        }

        private void BtnDetail_Click(object sender, EventArgs e) => OpenDetail();

        private void OpenDetail()
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih sesi opname untuk melihat detail.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var id = (int)dgv.SelectedRows[0].Tag;
            var form = new FormStockOpnameDetail(id);
            if (form.ShowDialog() == DialogResult.OK)
                LoadOpnames(); // Reload jika ada konfirmasi dari detail
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
