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
using simPOS.Shared.Repositories;
using simPOS.Shared.Services;

namespace simPOS.Management.Forms.Products
{
    public partial class FormProductList : Form
    {
        // ── Services ────────────────────────────────────────────────────
        private readonly ProductService _productService = new ProductService();
        private readonly CategoryService _categoryService = new CategoryService();
        private readonly CategoryRepository _categoryRepo = new CategoryRepository();

        // ── Controls ────────────────────────────────────────────────────
        private TextBox txtSearch;
        private ComboBox cmbCategory;
        private Button btnSearch;
        private Button btnReset;
        private Button btnTambah;
        private Button btnEdit;
        private Button btnHapus;
        private DataGridView dgv;
        private Label lblTotal;

        public FormProductList()
        {
            InitializeComponent();
            LoadCategories();
            LoadProducts();
        }

        // ── UI Setup ────────────────────────────────────────────────────

        private void InitializeComponent()
        {
            this.Text = "simPOS — Manajemen Barang";
            this.Size = new Size(1100, 650);
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

            // Label pencarian
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
                Width = 200,
                Font = new Font("Segoe UI", 9f),
                PlaceholderText = "Nama atau kode barang..."
            };
            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) LoadProducts();
            };

            // Filter kategori
            var lblCat = new Label
            {
                Text = "Kategori:",
                AutoSize = true,
                Location = new Point(265, 18),
                Font = new Font("Segoe UI", 9f)
            };

            cmbCategory = new ComboBox
            {
                Location = new Point(330, 14),
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f)
            };

            btnSearch = new Button
            {
                Text = "🔍 Cari",
                Location = new Point(505, 12),
                Width = 80,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += (s, e) => LoadProducts();

            btnReset = new Button
            {
                Text = "Reset",
                Location = new Point(595, 12),
                Width = 65,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.Click += BtnReset_Click;

            // Tombol aksi — kanan panel
            btnTambah = new Button
            {
                Text = "➕ Tambah",
                Width = 100,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnTambah.FlatAppearance.BorderSize = 0;
            btnTambah.Location = new Point(panel.Width - 330, 12);
            btnTambah.Click += BtnTambah_Click;

            btnEdit = new Button
            {
                Text = "✏️ Edit",
                Width = 90,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 196, 15),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnEdit.FlatAppearance.BorderSize = 0;
            btnEdit.Location = new Point(panel.Width - 220, 12);
            btnEdit.Click += BtnEdit_Click;

            btnHapus = new Button
            {
                Text = "🗑 Hapus",
                Width = 90,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnHapus.FlatAppearance.BorderSize = 0;
            btnHapus.Location = new Point(panel.Width - 120, 12);
            btnHapus.Click += BtnHapus_Click;

            panel.Controls.AddRange(new Control[]
            {
                lblSearch, txtSearch,
                lblCat, cmbCategory,
                btnSearch, btnReset,
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

            // Style header
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.EnableHeadersVisualStyles = false;

            // Style rows
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 252);

            // Double click untuk edit
            dgv.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) OpenEditForm();
            };

            // Definisi kolom
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", Width = 50, FillWeight = 30 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "Kode", Width = 100, FillWeight = 60 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Nama Barang", Width = 250, FillWeight = 200 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCategory", HeaderText = "Kategori", Width = 120, FillWeight = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit", HeaderText = "Satuan", Width = 70, FillWeight = 50 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBuyPrice", HeaderText = "Harga Beli", Width = 110, FillWeight = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSellPrice", HeaderText = "Harga Jual", Width = 110, FillWeight = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStock", HeaderText = "Stok", Width = 70, FillWeight = 50 });

            // Alignment angka
            foreach (var col in new[] { "colId", "colBuyPrice", "colSellPrice", "colStock" })
                dgv.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            dgv.Columns["colCode"].DefaultCellStyle.Font = new Font("Consolas", 9f);

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

        // ── Data Loading ─────────────────────────────────────────────────

        private void LoadCategories()
        {
            var categories = _categoryService.GetAll();
            cmbCategory.Items.Clear();
            cmbCategory.Items.Add(new Category { Id = 0, Name = "-- Semua Kategori --" });
            cmbCategory.Items.AddRange(categories.ToArray());
            cmbCategory.SelectedIndex = 0;
        }

        private void LoadProducts()
        {
            var keyword = txtSearch.Text.Trim();
            var selectedCat = cmbCategory.SelectedItem as Category;
            var categoryId = selectedCat?.Id ?? 0;

            var products = _productService.Search(keyword, categoryId);
            PopulateGrid(products);
        }

        private void PopulateGrid(List<Product> products)
        {
            dgv.Rows.Clear();

            foreach (var p in products)
            {
                var rowIdx = dgv.Rows.Add(
                    p.Id,
                    p.Code,
                    p.Name,
                    p.CategoryName,
                    p.Unit,
                    p.BuyPrice.ToString("N0"),
                    p.SellPrice.ToString("N0"),
                    p.Stock
                );

                // Tandai merah jika stok rendah
                if (p.IsLowStock)
                {
                    dgv.Rows[rowIdx].Cells["colStock"].Style.ForeColor = Color.Red;
                    dgv.Rows[rowIdx].Cells["colStock"].Style.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                }

                // Simpan Id di tag untuk akses mudah
                dgv.Rows[rowIdx].Tag = p.Id;
            }

            lblTotal.Text = $"  Total: {products.Count} barang";
        }

        // ── Event Handlers ───────────────────────────────────────────────

        private void BtnReset_Click(object sender, EventArgs e)
        {
            txtSearch.Clear();
            cmbCategory.SelectedIndex = 0;
            LoadProducts();
        }

        private void BtnTambah_Click(object sender, EventArgs e)
        {
            var form = new FormProductDetail(productId: 0);
            if (form.ShowDialog() == DialogResult.OK)
                LoadProducts();
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            OpenEditForm();
        }

        private void BtnHapus_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih barang yang akan dihapus.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var id = (int)dgv.SelectedRows[0].Tag;
            var name = dgv.SelectedRows[0].Cells["colName"].Value?.ToString();

            var confirm = MessageBox.Show(
                $"Nonaktifkan barang:\n\n\"{name}\"?\n\nBarang tidak akan dihapus permanen.",
                "Konfirmasi Hapus",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            var (success, message) = _productService.Delete(id);

            if (success)
            {
                MessageBox.Show(message, "Berhasil", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadProducts();
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
                MessageBox.Show("Pilih barang yang akan diedit.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var id = (int)dgv.SelectedRows[0].Tag;
            var form = new FormProductDetail(productId: id);
            if (form.ShowDialog() == DialogResult.OK)
                LoadProducts();
        }
    }
}
