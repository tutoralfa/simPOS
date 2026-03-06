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

namespace simPOS.Management.Forms.Categories
{
    public partial class FormCategoryList : Form
    {
        private readonly CategoryService _categoryService = new CategoryService();

        // ── Controls ────────────────────────────────────────────────────
        private TextBox txtSearch;
        private Button btnTambah;
        private Button btnEdit;
        private Button btnHapus;
        private DataGridView dgv;
        private Label lblTotal;

        // Cache data untuk live search di client-side
        private System.Collections.Generic.List<Category> _allCategories;

        public FormCategoryList()
        {
            InitializeComponent();
            LoadCategories();
        }

        // ── UI Setup ────────────────────────────────────────────────────

        private void InitializeComponent()
        {
            this.Text = "simPOS — Manajemen Kategori";
            this.Size = new Size(720, 520);
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
                Width = 240,
                Font = new Font("Segoe UI", 9f),
                PlaceholderText = "Nama kategori..."
            };
            // Live search — filter langsung saat mengetik, tanpa tombol
            txtSearch.TextChanged += (s, e) => FilterGrid(txtSearch.Text);

            // Tombol aksi di kanan
            btnTambah = CreateButton("➕ Tambah", Color.FromArgb(39, 174, 96), bold: true);
            btnEdit = CreateButton("✏️ Edit", Color.FromArgb(241, 196, 15), bold: false);
            btnHapus = CreateButton("🗑 Hapus", Color.FromArgb(231, 76, 60), bold: false);

            btnTambah.Location = new Point(panel.Width - 330, 12);
            btnEdit.Location = new Point(panel.Width - 220, 12);
            btnHapus.Location = new Point(panel.Width - 120, 12);

            btnTambah.Click += BtnTambah_Click;
            btnEdit.Click += BtnEdit_Click;
            btnHapus.Click += BtnHapus_Click;

            panel.Controls.AddRange(new Control[]
            {
                lblSearch, txtSearch,
                btnTambah, btnEdit, btnHapus
            });

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

            // Style header — konsisten dengan FormProductList
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.EnableHeadersVisualStyles = false;

            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 252);

            dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) OpenEditForm(); };

            // Kolom-kolom
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colId",
                HeaderText = "ID",
                FillWeight = 30
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colName",
                HeaderText = "Nama Kategori",
                FillWeight = 200
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDescription",
                HeaderText = "Deskripsi",
                FillWeight = 250
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colProductCount",
                HeaderText = "Jml Barang",
                FillWeight = 70
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCreatedAt",
                HeaderText = "Dibuat",
                FillWeight = 100
            });

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

        private void LoadCategories()
        {
            _allCategories = _categoryService.GetAllWithProductCount();
            FilterGrid(txtSearch?.Text ?? "");
        }

        private void FilterGrid(string keyword)
        {
            dgv.Rows.Clear();

            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? _allCategories
                : _allCategories.FindAll(c =>
                    c.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    c.Description.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var cat in filtered)
            {
                var rowIdx = dgv.Rows.Add(
                    cat.Id,
                    cat.Name,
                    cat.Description,
                    cat.ProductCount,
                    cat.CreatedAt
                );
                dgv.Rows[rowIdx].Tag = cat.Id;

                // Tandai abu-abu jika kategori kosong (tidak ada barang)
                if (cat.ProductCount == 0)
                    dgv.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(150, 150, 150);
            }

            var suffix = string.IsNullOrWhiteSpace(keyword) ? "" : $" (filter: \"{keyword}\")";
            lblTotal.Text = $"  Total: {filtered.Count} kategori{suffix}";
        }

        // ── Event Handlers ───────────────────────────────────────────────

        private void BtnTambah_Click(object sender, EventArgs e)
        {
            var form = new FormCategoryDetail(categoryId: 0);
            if (form.ShowDialog() == DialogResult.OK)
                LoadCategories();
        }

        private void BtnEdit_Click(object sender, EventArgs e) => OpenEditForm();

        private void BtnHapus_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih kategori yang akan dihapus.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var id = (int)dgv.SelectedRows[0].Tag;
            var name = dgv.SelectedRows[0].Cells["colName"].Value?.ToString();
            var productCount = (int)dgv.SelectedRows[0].Cells["colProductCount"].Value;

            // Peringatan jika kategori masih ada barang
            if (productCount > 0)
            {
                MessageBox.Show(
                    $"Kategori \"{name}\" masih memiliki {productCount} barang aktif.\n\nPindahkan atau nonaktifkan barang terlebih dahulu sebelum menghapus kategori ini.",
                    "Tidak Dapat Dihapus",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Hapus kategori:\n\n\"{name}\"?",
                "Konfirmasi Hapus",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            var (success, message) = _categoryService.Delete(id);

            if (success)
            {
                MessageBox.Show(message, "Berhasil", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadCategories();
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
                MessageBox.Show("Pilih kategori yang akan diedit.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var id = (int)dgv.SelectedRows[0].Tag;
            var form = new FormCategoryDetail(categoryId: id);
            if (form.ShowDialog() == DialogResult.OK)
                LoadCategories();
        }

        // ── Helper ───────────────────────────────────────────────────────

        private static Button CreateButton(string text, Color backColor, bool bold)
        {
            var btn = new Button
            {
                Text = text,
                Width = 100,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
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
