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
    /// <summary>
    /// Form input penerimaan barang baru.
    /// Terdiri dari: header (no. penerimaan, supplier, catatan)
    /// dan tabel item yang bisa ditambah/hapus sebelum disimpan.
    /// </summary>
    public class FormGoodsReceiptEntry : Form
    {
        private readonly GoodsReceiptService _receiptService = new GoodsReceiptService();
        private readonly ProductService _productService = new ProductService();
        private readonly SupplierService _supplierService = new SupplierService();

        // Header controls
        private TextBox txtReceiptNo;
        private ComboBox cmbSupplier;
        private TextBox txtNotes;
        private TextBox txtReceivedAt;
        private Button btnGenerateNo;

        // Item controls
        private ComboBox cmbProduct;
        private TextBox txtQty;
        private TextBox txtBuyPrice;
        private Button btnAddItem;
        private Button btnRemoveItem;
        private DataGridView dgvItems;

        // Footer
        private Label lblItemCount;
        private Label lblTotalValue;
        private Button btnSave;
        private Button btnCancel;

        // State
        private readonly List<GoodsReceiptItem> _items = new List<GoodsReceiptItem>();
        private List<Product> _products;

        public FormGoodsReceiptEntry()
        {
            InitializeComponent();
            LoadDropdowns();
            txtReceiptNo.Text = _receiptService.GenerateReceiptNo();
            txtReceivedAt.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        }

        private void InitializeComponent()
        {
            this.Text = "Penerimaan Barang Baru";
            this.Size = new Size(820, 640);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            BuildItemGrid();
            BuildItemInput();
            BuildFooter();
            BuildHeader();
        }

        // ── Section: Header Dokumen ───────────────────────────────────────

        private void BuildHeader()
        {
            var grp = new GroupBox
            {
                Text = "Informasi Penerimaan",
                Dock = DockStyle.Top,
                Height = 110,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Padding = new Padding(10, 15, 10, 5),
                Margin = new Padding(10)
            };

            // No. Penerimaan
            var lblNo = MakeLabel("No. Penerimaan *", new Point(10, 26));
            txtReceiptNo = new TextBox { Location = new Point(130, 23), Width = 180, Font = new Font("Consolas", 9.5f) };
            btnGenerateNo = new Button
            {
                Text = "⟳",
                Location = new Point(315, 22),
                Width = 30,
                Height = 23,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10f)
            };
            btnGenerateNo.FlatAppearance.BorderSize = 0;
            btnGenerateNo.Click += (s, e) => txtReceiptNo.Text = _receiptService.GenerateReceiptNo();

            // Supplier
            var lblSup = MakeLabel("Supplier", new Point(360, 26));
            cmbSupplier = new ComboBox
            {
                Location = new Point(430, 23),
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f)
            };

            // Tanggal
            var lblDate = MakeLabel("Tanggal Terima *", new Point(10, 62));
            txtReceivedAt = new TextBox { Location = new Point(130, 59), Width = 180, Font = new Font("Segoe UI", 9f) };

            // Catatan
            var lblNotes = MakeLabel("Catatan", new Point(360, 62));
            txtNotes = new TextBox { Location = new Point(430, 59), Width = 330, Font = new Font("Segoe UI", 9f) };

            grp.Controls.AddRange(new Control[]
            {
                lblNo, txtReceiptNo, btnGenerateNo,
                lblSup, cmbSupplier,
                lblDate, txtReceivedAt,
                lblNotes, txtNotes
            });

            this.Controls.Add(grp);
        }

        // ── Section: Input Item ───────────────────────────────────────────

        private void BuildItemInput()
        {
            var grp = new GroupBox
            {
                Text = "Tambah Barang",
                Dock = DockStyle.Top,
                Height = 70,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Padding = new Padding(10, 15, 10, 5)
            };

            var lblProduct = MakeLabel("Barang *", new Point(10, 26));
            cmbProduct = new ComboBox
            {
                Location = new Point(70, 23),
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f)
            };

            var lblQty = MakeLabel("Jumlah *", new Point(385, 26));
            txtQty = new TextBox { Location = new Point(450, 23), Width = 70, Font = new Font("Segoe UI", 9f), Text = "1" };
            txtQty.TextAlign = HorizontalAlignment.Right;

            var lblPrice = MakeLabel("Harga Beli", new Point(535, 26));
            txtBuyPrice = new TextBox { Location = new Point(615, 23), Width = 90, Font = new Font("Segoe UI", 9f), Text = "0" };
            txtBuyPrice.TextAlign = HorizontalAlignment.Right;

            btnAddItem = new Button
            {
                Text = "➕ Tambah",
                Location = new Point(720, 21),
                Width = 80,
                Height = 27,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddItem.FlatAppearance.BorderSize = 0;
            btnAddItem.Click += BtnAddItem_Click;

            grp.Controls.AddRange(new Control[]
            {
                lblProduct, cmbProduct,
                lblQty, txtQty,
                lblPrice, txtBuyPrice,
                btnAddItem
            });

            this.Controls.Add(grp);
        }

        // ── Section: Tabel Item ───────────────────────────────────────────

        private void BuildItemGrid()
        {
            var grp = new GroupBox
            {
                Text = "Daftar Barang Diterima",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Padding = new Padding(10, 15, 10, 5)
            };

            dgvItems = new DataGridView
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
                ColumnHeadersHeight = 32
            };

            dgvItems.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgvItems.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvItems.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgvItems.EnableHeadersVisualStyles = false;
            dgvItems.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
            dgvItems.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvItems.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 252);

            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "Kode", FillWeight = 80 });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Nama Barang", FillWeight = 220 });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit", HeaderText = "Satuan", FillWeight = 60 });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty", HeaderText = "Jumlah", FillWeight = 65 });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice", HeaderText = "Harga Beli", FillWeight = 100 });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSubtotal", HeaderText = "Subtotal", FillWeight = 110 });

            foreach (var col in new[] { "colQty", "colPrice", "colSubtotal" })
                dgvItems.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvItems.Columns["colCode"].DefaultCellStyle.Font = new Font("Consolas", 9f);

            // Tombol hapus item di bawah grid
            btnRemoveItem = new Button
            {
                Text = "🗑 Hapus Item",
                Dock = DockStyle.Bottom,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            btnRemoveItem.FlatAppearance.BorderSize = 0;
            btnRemoveItem.Click += BtnRemoveItem_Click;

            grp.Controls.Add(dgvItems);
            grp.Controls.Add(btnRemoveItem);
            this.Controls.Add(grp);
        }

        // ── Section: Footer ───────────────────────────────────────────────

        private void BuildFooter()
        {
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.FromArgb(248, 248, 248)
            };
            footer.Paint += (s, e) => e.Graphics.DrawLine(
                new System.Drawing.Pen(Color.FromArgb(220, 220, 220)), 0, 0, footer.Width, 0);

            lblItemCount = new Label
            {
                Text = "0 item",
                AutoSize = true,
                Location = new Point(15, 17),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            lblTotalValue = new Label
            {
                Text = "Total: Rp 0",
                AutoSize = true,
                Location = new Point(90, 17),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80)
            };

            btnSave = new Button
            {
                Text = "💾 Simpan Penerimaan",
                Width = 170,
                Height = 33,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Location = new Point(footer.Width - 290, 9);
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text = "Batal",
                Width = 90,
                Height = 33,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Location = new Point(footer.Width - 110, 9);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.CancelButton = btnCancel;

            footer.Controls.AddRange(new Control[] { lblItemCount, lblTotalValue, btnSave, btnCancel });
            this.Controls.Add(footer);
        }

        // ── Data Loading ──────────────────────────────────────────────────

        private void LoadDropdowns()
        {
            // Supplier
            cmbSupplier.Items.Add(new Supplier { Id = 0, Name = "-- Tanpa Supplier --" });
            cmbSupplier.Items.AddRange(_supplierService.GetAll().ToArray());
            cmbSupplier.SelectedIndex = 0;

            // Produk — saat dipilih, isi harga beli otomatis
            _products = _productService.GetAll();
            cmbProduct.Items.Add("-- Pilih Barang --");
            foreach (var p in _products)
                cmbProduct.Items.Add($"[{p.Code}] {p.Name}");
            cmbProduct.SelectedIndex = 0;

            cmbProduct.SelectedIndexChanged += (s, e) =>
            {
                int idx = cmbProduct.SelectedIndex - 1; // -1 karena ada placeholder
                if (idx >= 0 && idx < _products.Count)
                    txtBuyPrice.Text = _products[idx].BuyPrice.ToString("N0");
            };
        }

        private void RefreshGrid()
        {
            dgvItems.Rows.Clear();
            decimal total = 0;

            foreach (var item in _items)
            {
                var rowIdx = dgvItems.Rows.Add(
                    item.ProductCode,
                    item.ProductName,
                    item.Unit,
                    item.Quantity,
                    item.BuyPrice.ToString("N0"),
                    item.Subtotal.ToString("N0")
                );
                dgvItems.Rows[rowIdx].Tag = item;
                total += item.Subtotal;
            }

            lblItemCount.Text = $"{_items.Count} item";
            lblTotalValue.Text = $"Total: Rp {total:N0}";
        }

        // ── Events ───────────────────────────────────────────────────────

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            int productIdx = cmbProduct.SelectedIndex - 1;
            if (productIdx < 0)
            {
                MessageBox.Show("Pilih barang terlebih dahulu.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!int.TryParse(txtQty.Text, out int qty) || qty <= 0)
            {
                MessageBox.Show("Jumlah harus berupa angka lebih dari 0.", "Validasi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtQty.Focus(); txtQty.SelectAll();
                return;
            }

            if (!decimal.TryParse(txtBuyPrice.Text.Replace(",", "").Replace(".", ""), out decimal price) || price < 0)
            {
                MessageBox.Show("Harga beli tidak valid.", "Validasi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBuyPrice.Focus(); txtBuyPrice.SelectAll();
                return;
            }

            var product = _products[productIdx];

            // Jika barang sudah ada di list, tambah jumlahnya
            var existing = _items.Find(i => i.ProductId == product.Id);
            if (existing != null)
            {
                existing.Quantity += qty;
                existing.BuyPrice = price; // update harga dengan yang terakhir diinput
            }
            else
            {
                _items.Add(new GoodsReceiptItem
                {
                    ProductId = product.Id,
                    ProductCode = product.Code,
                    ProductName = product.Name,
                    Unit = product.Unit,
                    Quantity = qty,
                    BuyPrice = price
                });
            }

            RefreshGrid();

            // Reset input untuk barang berikutnya
            cmbProduct.SelectedIndex = 0;
            txtQty.Text = "1";
            txtBuyPrice.Text = "0";
            cmbProduct.Focus();
        }

        private void BtnRemoveItem_Click(object sender, EventArgs e)
        {
            if (dgvItems.SelectedRows.Count == 0) return;

            var item = dgvItems.SelectedRows[0].Tag as GoodsReceiptItem;
            if (item != null)
            {
                _items.Remove(item);
                RefreshGrid();
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var selSupplier = cmbSupplier.SelectedItem as Supplier;

            var receipt = new GoodsReceipt
            {
                ReceiptNo = txtReceiptNo.Text.Trim(),
                SupplierId = (selSupplier?.Id > 0) ? selSupplier.Id : (int?)null,
                Notes = txtNotes.Text.Trim(),
                ReceivedAt = txtReceivedAt.Text.Trim(),
                Items = _items
            };

            var (success, message) = _receiptService.Save(receipt);

            if (success)
            {
                MessageBox.Show(message, "Penerimaan Berhasil",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show(message, "Gagal Menyimpan",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ── Helper ───────────────────────────────────────────────────────

        private static Label MakeLabel(string text, Point location) => new Label
        {
            Text = text,
            AutoSize = true,
            Location = location,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(60, 60, 60)
        };
    }
}
