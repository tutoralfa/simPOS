using simPOS.Shared.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace simPOS.Management.Forms.POSMgmt
{
    /// <summary>
    /// Popup pilihan barang saat input nama menghasilkan lebih dari 1 hasil.
    /// Double-click atau Enter untuk memilih.
    /// </summary>
    public class FormProductPicker : Form
    {
        public Product SelectedProduct { get; private set; }

        private readonly List<Product> _products;
        private DataGridView dgv;

        public FormProductPicker(List<Product> products)
        {
            _products = products;
            InitializeComponent();
            PopulateGrid();
        }

        private void InitializeComponent()
        {
            this.Text = "Pilih Barang";
            this.Size = new Size(560, 380);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            var lblInfo = new Label
            {
                Dock = DockStyle.Top,
                Height = 36,
                Text = "  Ditemukan beberapa barang. Pilih salah satu:",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(44, 62, 80),
                BackColor = Color.FromArgb(236, 240, 241),
                TextAlign = ContentAlignment.MiddleLeft
            };

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
                Font = new Font("Segoe UI", 10f),
                ColumnHeadersHeight = 32,
                RowTemplate = { Height = 36 }
            };

            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 255);

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "Kode", FillWeight = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Nama Barang", FillWeight = 220 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStock", HeaderText = "Stok", FillWeight = 55 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice", HeaderText = "Harga Jual", FillWeight = 100 });

            dgv.Columns["colCode"].DefaultCellStyle.Font = new Font("Consolas", 9.5f);
            dgv.Columns["colStock"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["colPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) SelectAndClose(e.RowIndex); };
            dgv.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && dgv.SelectedRows.Count > 0)
                    SelectAndClose(dgv.SelectedRows[0].Index);
            };

            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = Color.FromArgb(248, 248, 248)
            };
            footer.Paint += (s, e) => e.Graphics.DrawLine(
                new System.Drawing.Pen(Color.FromArgb(220, 220, 220)), 0, 0, footer.Width, 0);

            var btnPilih = new Button
            {
                Text = "✔ Pilih",
                Width = 100,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnPilih.FlatAppearance.BorderSize = 0;
            btnPilih.Location = new Point(footer.Width - 210, 8);
            btnPilih.Click += (s, e) =>
            {
                if (dgv.SelectedRows.Count > 0)
                    SelectAndClose(dgv.SelectedRows[0].Index);
            };

            var btnBatal = new Button
            {
                Text = "Batal",
                Width = 90,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnBatal.FlatAppearance.BorderSize = 0;
            btnBatal.Location = new Point(footer.Width - 105, 8);
            btnBatal.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.AcceptButton = btnPilih;
            this.CancelButton = btnBatal;
            footer.Controls.Add(btnPilih);
            footer.Controls.Add(btnBatal);

            // ⚠ Fill dulu
            this.Controls.Add(dgv);
            this.Controls.Add(footer);
            this.Controls.Add(lblInfo);
        }

        private void PopulateGrid()
        {
            foreach (var p in _products)
            {
                var rowIdx = dgv.Rows.Add(
                    p.Code,
                    p.Name,
                    p.Stock,
                    $"Rp {p.SellPrice:N0}"
                );
                dgv.Rows[rowIdx].Tag = p;

                // Stok habis — tampilkan redup
                if (p.Stock <= 0)
                    dgv.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(180, 180, 180);
            }

            if (dgv.Rows.Count > 0)
                dgv.Rows[0].Selected = true;
        }

        private void SelectAndClose(int rowIndex)
        {
            SelectedProduct = dgv.Rows[rowIndex].Tag as Product;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
