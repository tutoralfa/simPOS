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

namespace simPOS.Management.Forms.SupplierMgmt
{
    public class FormSupplierList : Form
    {
        private readonly SupplierService _service = new SupplierService();

        private TextBox txtSearch;
        private Button btnTambah;
        private Button btnEdit;
        private Button btnHapus;
        private DataGridView dgv;
        private Label lblTotal;

        // Cache untuk live search
        private List<Supplier> _allSuppliers;

        public FormSupplierList()
        {
            InitializeComponent();
            LoadSuppliers();
        }

        private void InitializeComponent()
        {
            this.Text = "simPOS — Manajemen Supplier";
            this.Size = new Size(950, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.WhiteSmoke;

            BuildContentArea();   // Fill dulu
            BuildStatusBar();
            BuildToolbar();       // Top belakangan

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
                PlaceholderText = "Nama, kontak, atau no. telepon..."
            };
            txtSearch.TextChanged += (s, e) => FilterGrid(txtSearch.Text);

            btnTambah = MakeButton("➕ Tambah", Color.FromArgb(39, 174, 96), bold: true);
            btnEdit = MakeButton("✏️ Edit", Color.FromArgb(241, 196, 15), bold: false);
            btnHapus = MakeButton("🗑 Hapus", Color.FromArgb(231, 76, 60), bold: false);

            btnTambah.Location = new Point(panel.Width - 330, 12);
            btnEdit.Location = new Point(panel.Width - 220, 12);
            btnHapus.Location = new Point(panel.Width - 120, 12);

            btnTambah.Click += BtnTambah_Click;
            btnEdit.Click += BtnEdit_Click;
            btnHapus.Click += BtnHapus_Click;

            panel.Controls.AddRange(new Control[] { lblSearch, txtSearch, btnTambah, btnEdit, btnHapus });
            this.Controls.Add(panel);
        }

        private void BuildContentArea()
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

            dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) OpenEditForm(); };

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", FillWeight = 25 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Nama Supplier", FillWeight = 180 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colContact", HeaderText = "Kontak", FillWeight = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPhone", HeaderText = "Telepon", FillWeight = 100 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEmail", HeaderText = "Email", FillWeight = 140 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colProductCount", HeaderText = "Jml Barang", FillWeight = 60 });

            dgv.Columns["colId"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["colProductCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

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

        // ── Data ─────────────────────────────────────────────────────────

        private void LoadSuppliers()
        {
            _allSuppliers = _service.GetAllWithProductCount();
            FilterGrid(txtSearch?.Text ?? "");
        }

        private void FilterGrid(string keyword)
        {
            dgv.Rows.Clear();

            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? _allSuppliers
                : _allSuppliers.FindAll(s =>
                    s.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    s.ContactPerson.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    s.Phone.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var s in filtered)
            {
                var rowIdx = dgv.Rows.Add(
                    s.Id,
                    s.Name,
                    s.ContactPerson,
                    s.Phone,
                    s.Email,
                    s.ProductCount
                );
                dgv.Rows[rowIdx].Tag = s.Id;

                // Supplier tanpa barang — tampilkan lebih redup
                if (s.ProductCount == 0)
                    dgv.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(150, 150, 150);
            }

            var suffix = string.IsNullOrWhiteSpace(keyword) ? "" : $" (filter: \"{keyword}\")";
            lblTotal.Text = $"  Total: {filtered.Count} supplier{suffix}";
        }

        // ── Events ───────────────────────────────────────────────────────

        private void BtnTambah_Click(object sender, EventArgs e)
        {
            var form = new FormSupplierDetail(supplierId: 0);
            if (form.ShowDialog() == DialogResult.OK)
                LoadSuppliers();
        }

        private void BtnEdit_Click(object sender, EventArgs e) => OpenEditForm();

        private void BtnHapus_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih supplier yang akan dihapus.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var id = (int)dgv.SelectedRows[0].Tag;
            var name = dgv.SelectedRows[0].Cells["colName"].Value?.ToString();
            var productCount = (int)dgv.SelectedRows[0].Cells["colProductCount"].Value;

            if (productCount > 0)
            {
                MessageBox.Show(
                    $"Supplier \"{name}\" masih terhubung ke {productCount} barang aktif.\n\nPindahkan barang ke supplier lain terlebih dahulu.",
                    "Tidak Dapat Dinonaktifkan",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Nonaktifkan supplier:\n\n\"{name}\"?",
                "Konfirmasi",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            var (success, message) = _service.Delete(id);

            if (success)
            {
                MessageBox.Show(message, "Berhasil", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadSuppliers();
            }
            else
            {
                MessageBox.Show(message, "Gagal", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenEditForm()
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih supplier yang akan diedit.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var id = (int)dgv.SelectedRows[0].Tag;
            var form = new FormSupplierDetail(supplierId: id);
            if (form.ShowDialog() == DialogResult.OK)
                LoadSuppliers();
        }

        private static Button MakeButton(string text, Color color, bool bold)
        {
            var btn = new Button
            {
                Text = text,
                Width = 100,
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
