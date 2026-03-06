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

namespace simPOS.Management.Forms.Categories
{
    public partial class FormCategoryDetail : Form
    {
        private readonly int _categoryId;
        private readonly CategoryService _service = new CategoryService();

        // ── Controls ────────────────────────────────────────────────────
        private TextBox txtName;
        private TextBox txtDescription;
        private Button btnSave;
        private Button btnCancel;
        private Label lblInfo;     // Info jumlah barang saat mode Edit

        public FormCategoryDetail(int categoryId)
        {
            _categoryId = categoryId;
            InitializeComponent();

            if (_categoryId > 0)
                LoadCategory();
        }

        // ── UI Setup ────────────────────────────────────────────────────

        private void InitializeComponent()
        {
            this.Text = _categoryId > 0 ? "Edit Kategori" : "Tambah Kategori Baru";
            this.Size = new Size(420, 280);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            // ── Form body ──────────────────────────────────────────────
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(20, 20, 20, 10),
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));  // Nama
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 62f));  // Deskripsi
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Info

            // Nama
            body.Controls.Add(MakeLabel("Nama *"), 0, 0);
            txtName = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f),
                MaxLength = 100
            };
            txtName.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtDescription.Focus(); }
            };
            body.Controls.Add(txtName, 1, 0);

            // Deskripsi
            body.Controls.Add(MakeLabel("Deskripsi"), 0, 1);
            txtDescription = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f),
                Multiline = true,
                MaxLength = 255,
                ScrollBars = ScrollBars.Vertical
            };
            body.Controls.Add(txtDescription, 1, 1);

            // Label info (hanya muncul saat Edit)
            lblInfo = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(120, 120, 120),
                TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(100, 0, 0, 4),
                Text = ""
            };
            body.SetColumnSpan(lblInfo, 2);
            body.Controls.Add(lblInfo, 0, 2);

            // ── Footer dengan tombol ───────────────────────────────────
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.FromArgb(248, 248, 248)
            };

            // Garis pemisah tipis di atas footer
            footer.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(
                    new System.Drawing.Pen(Color.FromArgb(220, 220, 220)),
                    0, 0, footer.Width, 0);
            };

            btnSave = new Button
            {
                Text = "💾 Simpan",
                Width = 110,
                Height = 33,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Location = new Point(footer.Width - 240, 9);
            btnSave.Anchor = AnchorStyles.Right | AnchorStyles.Top;
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
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Location = new Point(footer.Width - 120, 9);
            btnCancel.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            // Enter di form langsung Save
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;

            footer.Controls.Add(btnSave);
            footer.Controls.Add(btnCancel);

            this.Controls.Add(body);
            this.Controls.Add(footer);
        }

        // ── Data ─────────────────────────────────────────────────────────

        private void LoadCategory()
        {
            var cat = _service.GetById(_categoryId);
            if (cat == null)
            {
                MessageBox.Show("Kategori tidak ditemukan.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

            txtName.Text = cat.Name;
            txtDescription.Text = cat.Description;

            // Tampilkan info jumlah barang agar user berhati-hati saat rename
            if (cat.ProductCount > 0)
                lblInfo.Text = $"⚠ Kategori ini digunakan oleh {cat.ProductCount} barang aktif.";
        }

        // ── Events ───────────────────────────────────────────────────────

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var category = new Category
            {
                Id = _categoryId,
                Name = txtName.Text.Trim(),
                Description = txtDescription.Text.Trim()
            };

            var (success, message) = _categoryId > 0
                ? _service.Update(category)
                : _service.Save(category);

            if (success)
            {
                MessageBox.Show(message, "Berhasil", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show(message, "Gagal Menyimpan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                txtName.SelectAll();
            }
        }

        // ── Helper ───────────────────────────────────────────────────────

        private static Label MakeLabel(string text) => new Label
        {
            Text = text,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 9f),
            Padding = new Padding(0, 0, 10, 0)
        };
    }
}
