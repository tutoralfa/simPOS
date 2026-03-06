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

namespace simPOS.Management.Forms.SupplierMgmt
{
    public class FormSupplierDetail : Form
    {
        private readonly int _supplierId;
        private readonly SupplierService _service = new SupplierService();

        private TextBox txtName;
        private TextBox txtContactPerson;
        private TextBox txtPhone;
        private TextBox txtEmail;
        private TextBox txtAddress;
        private CheckBox chkIsActive;
        private Label lblInfo;
        private Button btnSave;
        private Button btnCancel;

        public FormSupplierDetail(int supplierId)
        {
            _supplierId = supplierId;
            InitializeComponent();

            if (_supplierId > 0)
                LoadSupplier();
        }

        private void InitializeComponent()
        {
            this.Text = _supplierId > 0 ? "Edit Supplier" : "Tambah Supplier Baru";
            this.Size = new Size(460, 400);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            // ── Body ──────────────────────────────────────────────────
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Padding = new Padding(20, 18, 20, 8)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // Tinggi setiap baris
            for (int i = 0; i < 6; i++)
                body.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Baris info

            int row = 0;

            // Nama Supplier
            body.Controls.Add(MakeLabel("Nama Supplier *"), 0, row);
            txtName = MakeTextBox(maxLength: 150);
            body.Controls.Add(txtName, 1, row++);

            // Kontak
            body.Controls.Add(MakeLabel("Nama Kontak"), 0, row);
            txtContactPerson = MakeTextBox(maxLength: 100);
            body.Controls.Add(txtContactPerson, 1, row++);

            // Telepon
            body.Controls.Add(MakeLabel("Telepon"), 0, row);
            txtPhone = MakeTextBox(maxLength: 30, placeholder: "contoh: 0812-3456-7890");
            body.Controls.Add(txtPhone, 1, row++);

            // Email
            body.Controls.Add(MakeLabel("Email"), 0, row);
            txtEmail = MakeTextBox(maxLength: 100, placeholder: "contoh: supplier@email.com");
            body.Controls.Add(txtEmail, 1, row++);

            // Alamat
            body.Controls.Add(MakeLabel("Alamat"), 0, row);
            txtAddress = MakeTextBox(maxLength: 255);
            body.Controls.Add(txtAddress, 1, row++);

            // Status (hanya muncul saat Edit)
            body.Controls.Add(MakeLabel("Status"), 0, row);
            chkIsActive = new CheckBox
            {
                Text = "Aktif",
                Checked = true,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f),
                Visible = _supplierId > 0    // tersembunyi saat Tambah
            };
            body.Controls.Add(chkIsActive, 1, row++);

            // Label info (jumlah barang saat mode Edit)
            lblInfo = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(120, 120, 120),
                TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(120, 0, 0, 4),
                Text = ""
            };
            body.SetColumnSpan(lblInfo, 2);
            body.Controls.Add(lblInfo, 0, row);

            // ── Footer ────────────────────────────────────────────────
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.FromArgb(248, 248, 248)
            };
            footer.Paint += (s, e) => e.Graphics.DrawLine(
                new System.Drawing.Pen(Color.FromArgb(220, 220, 220)), 0, 0, footer.Width, 0);

            btnSave = new Button
            {
                Text = "💾 Simpan",
                Width = 110,
                Height = 33,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Location = new Point(footer.Width - 240, 9);
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
            btnCancel.Location = new Point(footer.Width - 120, 9);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;

            footer.Controls.Add(btnSave);
            footer.Controls.Add(btnCancel);

            this.Controls.Add(body);
            this.Controls.Add(footer);
        }

        // ── Data ─────────────────────────────────────────────────────────

        private void LoadSupplier()
        {
            var s = _service.GetById(_supplierId);
            if (s == null)
            {
                MessageBox.Show("Supplier tidak ditemukan.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

            txtName.Text = s.Name;
            txtContactPerson.Text = s.ContactPerson;
            txtPhone.Text = s.Phone;
            txtEmail.Text = s.Email;
            txtAddress.Text = s.Address;
            chkIsActive.Checked = s.IsActive;

            if (s.ProductCount > 0)
                lblInfo.Text = $"⚠ Supplier ini terhubung ke {s.ProductCount} barang aktif.";
        }

        // ── Events ───────────────────────────────────────────────────────

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var supplier = new Supplier
            {
                Id = _supplierId,
                Name = txtName.Text.Trim(),
                ContactPerson = txtContactPerson.Text.Trim(),
                Phone = txtPhone.Text.Trim(),
                Email = txtEmail.Text.Trim(),
                Address = txtAddress.Text.Trim(),
                IsActive = _supplierId > 0 ? chkIsActive.Checked : true
            };

            var (success, message) = _supplierId > 0
                ? _service.Update(supplier)
                : _service.Save(supplier);

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

        // ── Helpers ──────────────────────────────────────────────────────

        private static Label MakeLabel(string text) => new Label
        {
            Text = text,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 9f),
            Padding = new Padding(0, 0, 10, 0)
        };

        private static TextBox MakeTextBox(int maxLength = 200, string placeholder = "")
        {
            var tb = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f),
                MaxLength = maxLength,
                PlaceholderText = placeholder
            };
            return tb;
        }
    }
}
