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
using simPOS.Shared.Services;

namespace simPOS.Management.Forms.Products
{
    public partial class FormProductDetail : Form
    {
        private readonly int _productId;
        private readonly ProductService _productService = new ProductService();
        private readonly CategoryService _categoryService = new CategoryService();
        private readonly SupplierService _supplierService = new SupplierService();

        // ── Controls ────────────────────────────────────────────────────
        private TextBox txtCode;
        private TextBox txtName;
        private TextBox txtDescription;
        private ComboBox cmbCategory;
        private ComboBox cmbSupplier;
        private TextBox txtUnit;
        private TextBox txtBuyPrice;
        private TextBox txtSellPrice;
        private TextBox txtStock;
        private TextBox txtMinStock;
        private CheckBox chkIsActive;
        private Button btnSave;
        private Button btnCancel;
        private Button btnGenerateCode;

        public FormProductDetail(int productId)
        {
            _productId = productId;
            InitializeComponent();
            LoadDropdowns();

            if (_productId > 0)
                LoadProduct();
            else
                txtCode.Text = _productService.GenerateCode();
        }

        // ── UI Setup ────────────────────────────────────────────────────

        private void InitializeComponent()
        {
            this.Text = _productId > 0 ? "Edit Barang" : "Tambah Barang Baru";
            this.Size = new Size(500, 580);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 12,
                Padding = new Padding(20),
                AutoSize = true
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130f));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            int row = 0;

            // Kode Barang
            AddLabel(panel, "Kode Barang *", row);
            var codePanel = new Panel { Dock = DockStyle.Fill, Height = 30 };
            txtCode = new TextBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 10f) };
            btnGenerateCode = new Button
            {
                Text = "⟳",
                Width = 30,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10f)
            };
            btnGenerateCode.FlatAppearance.BorderSize = 0;
            btnGenerateCode.Click += (s, e) => txtCode.Text = _productService.GenerateCode();
            codePanel.Controls.Add(txtCode);
            codePanel.Controls.Add(btnGenerateCode);
            panel.Controls.Add(codePanel, 1, row++);

            // Nama Barang
            AddLabel(panel, "Nama Barang *", row);
            txtName = AddTextBox(panel, row++);

            // Deskripsi
            AddLabel(panel, "Deskripsi", row);
            txtDescription = AddTextBox(panel, row++);

            // Kategori
            AddLabel(panel, "Kategori", row);
            cmbCategory = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9f) };
            panel.Controls.Add(cmbCategory, 1, row++);

            // Supplier
            AddLabel(panel, "Supplier", row);
            cmbSupplier = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9f) };
            panel.Controls.Add(cmbSupplier, 1, row++);

            // Satuan
            AddLabel(panel, "Satuan *", row);
            txtUnit = AddTextBox(panel, row++, "pcs");

            // Harga Beli
            AddLabel(panel, "Harga Beli (Rp)", row);
            txtBuyPrice = AddTextBox(panel, row++, "0");
            txtBuyPrice.TextAlign = HorizontalAlignment.Right;

            // Harga Jual
            AddLabel(panel, "Harga Jual (Rp) *", row);
            txtSellPrice = AddTextBox(panel, row++, "0");
            txtSellPrice.TextAlign = HorizontalAlignment.Right;

            // Stok Awal
            AddLabel(panel, "Stok Awal", row);
            txtStock = AddTextBox(panel, row++, "0");
            txtStock.TextAlign = HorizontalAlignment.Right;
            if (_productId > 0) txtStock.ReadOnly = true; // Edit: stok dikelola via movement

            // Stok Minimum
            AddLabel(panel, "Stok Minimum", row);
            txtMinStock = AddTextBox(panel, row++, "0");
            txtMinStock.TextAlign = HorizontalAlignment.Right;

            // Status Aktif
            AddLabel(panel, "Status", row);
            chkIsActive = new CheckBox { Text = "Aktif", Checked = true, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f) };
            panel.Controls.Add(chkIsActive, 1, row++);

            // Tombol
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 50,
                Padding = new Padding(10),
                BackColor = Color.WhiteSmoke
            };

            btnSave = new Button
            {
                Text = "💾 Simpan",
                Width = 100,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text = "Batal",
                Width = 80,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            btnPanel.Controls.Add(btnSave);
            btnPanel.Controls.Add(btnCancel);

            this.Controls.Add(panel);
            this.Controls.Add(btnPanel);
        }

        // ── Helpers untuk build form ─────────────────────────────────────

        private void AddLabel(TableLayoutPanel panel, string text, int row)
        {
            panel.Controls.Add(new Label
            {
                Text = text,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9f),
                Padding = new Padding(0, 0, 8, 0)
            }, 0, row);
        }

        private TextBox AddTextBox(TableLayoutPanel panel, int row, string defaultText = "")
        {
            var tb = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f),
                Text = defaultText
            };
            panel.Controls.Add(tb, 1, row);
            return tb;
        }

        // ── Data ─────────────────────────────────────────────────────────

        private void LoadDropdowns()
        {
            // Kategori
            cmbCategory.Items.Add(new Category { Id = 0, Name = "-- Pilih Kategori --" });
            cmbCategory.Items.AddRange(_categoryService.GetAll().ToArray());
            cmbCategory.SelectedIndex = 0;

            // Supplier
            cmbSupplier.Items.Add(new Supplier { Id = 0, Name = "-- Pilih Supplier --" });
            cmbSupplier.Items.AddRange(_supplierService.GetAll().ToArray());
            cmbSupplier.SelectedIndex = 0;
        }

        private void LoadProduct()
        {
            var product = _productService.GetById(_productId);
            if (product == null)
            {
                MessageBox.Show("Produk tidak ditemukan.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

            txtCode.Text = product.Code;
            txtName.Text = product.Name;
            txtDescription.Text = product.Description;
            txtUnit.Text = product.Unit;
            txtBuyPrice.Text = product.BuyPrice.ToString("N0");
            txtSellPrice.Text = product.SellPrice.ToString("N0");
            txtStock.Text = product.Stock.ToString();
            txtMinStock.Text = product.MinStock.ToString();
            chkIsActive.Checked = product.IsActive;

            // Set ComboBox ke nilai yang sesuai
            SelectComboByValue<Category>(cmbCategory, product.CategoryId);
            SelectComboByValue<Supplier>(cmbSupplier, product.SupplierId);
        }

        private void SelectComboByValue<T>(ComboBox cmb, int? id) where T : class
        {
            // Helper: cari item di ComboBox berdasarkan Id property
            if (id == null) return;

            for (int i = 0; i < cmb.Items.Count; i++)
            {
                var item = cmb.Items[i];
                var prop = item.GetType().GetProperty("Id");
                if (prop != null && (int)prop.GetValue(item) == id)
                {
                    cmb.SelectedIndex = i;
                    return;
                }
            }
        }

        // ── Event Handlers ───────────────────────────────────────────────

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!ParseInputs(out var product)) return;

            var (success, message) = _productId > 0
                ? _productService.Update(product)
                : _productService.Save(product);

            if (success)
            {
                //MessageBox.Show(message, "Berhasil", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show(message, "Gagal Menyimpan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private bool ParseInputs(out Product product)
        {
            product = null;

            // Parsing harga dan angka
            if (!decimal.TryParse(txtBuyPrice.Text.Replace(",", "").Replace(".", ""), out decimal buyPrice))
            {
                MessageBox.Show("Harga beli tidak valid.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBuyPrice.Focus();
                return false;
            }

            if (!decimal.TryParse(txtSellPrice.Text.Replace(",", "").Replace(".", ""), out decimal sellPrice))
            {
                MessageBox.Show("Harga jual tidak valid.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSellPrice.Focus();
                return false;
            }

            if (!int.TryParse(txtStock.Text, out int stock))
            {
                MessageBox.Show("Stok tidak valid.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtStock.Focus();
                return false;
            }

            if (!int.TryParse(txtMinStock.Text, out int minStock))
            {
                MessageBox.Show("Stok minimum tidak valid.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMinStock.Focus();
                return false;
            }

            var selCategory = cmbCategory.SelectedItem as Category;
            var selSupplier = cmbSupplier.SelectedItem as Supplier;

            product = new Product
            {
                Id = _productId,
                Code = txtCode.Text.Trim(),
                Name = txtName.Text.Trim(),
                Description = txtDescription.Text.Trim(),
                Unit = txtUnit.Text.Trim(),
                CategoryId = (selCategory?.Id > 0) ? selCategory.Id : (int?)null,
                SupplierId = (selSupplier?.Id > 0) ? selSupplier.Id : (int?)null,
                BuyPrice = buyPrice,
                SellPrice = sellPrice,
                Stock = stock,
                MinStock = minStock,
                IsActive = chkIsActive.Checked
            };

            return true;
        }
    }
}
