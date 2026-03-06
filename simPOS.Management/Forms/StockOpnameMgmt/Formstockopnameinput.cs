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
    /// <summary>
    /// Form input stok fisik per produk.
    /// User mengedit kolom "Stok Fisik" — kolom lain read-only.
    /// Baris yang berbeda dari sistem ditandai warna.
    /// Tombol Konfirmasi menyimpan semua adjustment sekaligus.
    /// </summary>
    public class FormStockOpnameInput : Form
    {
        private readonly StockOpname _opname;
        private readonly StockOpnameService _service = new StockOpnameService();

        private DataGridView dgv;
        private Label lblInfo;
        private Label lblDiff;
        private Button btnConfirm;
        private Button btnCancel;
        private TextBox txtFilter;

        public FormStockOpnameInput(StockOpname opname)
        {
            _opname = opname;
            InitializeComponent();
            PopulateGrid();
        }

        private void InitializeComponent()
        {
            this.Text = $"Input Stok Fisik — {_opname.OpnameNo}";
            this.Size = new Size(820, 620);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.WhiteSmoke;

            BuildGrid();
            BuildFooter();
            BuildToolbar();
        }

        private void BuildToolbar()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.White,
                Padding = new Padding(10, 10, 10, 0)
            };

            var lblSearch = new Label { Text = "Filter:", AutoSize = true, Location = new Point(10, 17), Font = new Font("Segoe UI", 9f) };
            txtFilter = new TextBox
            {
                Location = new Point(58, 13),
                Width = 220,
                Font = new Font("Segoe UI", 9f),
                PlaceholderText = "Nama atau kode barang..."
            };
            txtFilter.TextChanged += (s, e) => FilterRows(txtFilter.Text);

            lblInfo = new Label
            {
                Text = $"Total: {_opname.Items.Count} produk  |  Edit kolom \"Stok Fisik\" lalu klik Konfirmasi",
                AutoSize = true,
                Location = new Point(295, 17),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            panel.Controls.AddRange(new System.Windows.Forms.Control[] { lblSearch, txtFilter, lblInfo });
            this.Controls.Add(panel);
        }

        private void BuildGrid()
        {
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
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
                ColumnHeadersHeight = 35,
                EditMode = DataGridViewEditMode.EditOnKeystroke
            };

            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.EnableHeadersVisualStyles = false;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 252);

            // Kolom read-only
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "Kode", FillWeight = 80, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Nama Barang", FillWeight = 220, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit", HeaderText = "Satuan", FillWeight = 55, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSystem", HeaderText = "Stok Sistem", FillWeight = 80, ReadOnly = true });

            // Kolom editable — stok fisik hasil hitung user
            var colPhysical = new DataGridViewTextBoxColumn
            {
                Name = "colPhysical",
                HeaderText = "Stok Fisik ✏",
                FillWeight = 80,
                ReadOnly = false
            };
            colPhysical.DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 220); // kuning muda
            colPhysical.DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.Columns.Add(colPhysical);

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDiff", HeaderText = "Selisih", FillWeight = 65, ReadOnly = true });

            foreach (var col in new[] { "colSystem", "colPhysical", "colDiff" })
                dgv.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // Update selisih real-time saat user mengedit stok fisik
            dgv.CellEndEdit += DgvCellEndEdit;

            this.Controls.Add(dgv);
        }

        private void BuildFooter()
        {
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.FromArgb(248, 248, 248) };
            footer.Paint += (s, e) => e.Graphics.DrawLine(
                new System.Drawing.Pen(Color.FromArgb(220, 220, 220)), 0, 0, footer.Width, 0);

            lblDiff = new Label
            {
                Text = "0 produk berbeda",
                AutoSize = true,
                Location = new Point(15, 17),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80)
            };

            btnConfirm = new Button
            {
                Text = "✔ Konfirmasi Opname",
                Width = 170,
                Height = 33,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Location = new Point(footer.Width - 290, 9);
            btnConfirm.Click += BtnConfirm_Click;

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
            footer.Controls.AddRange(new System.Windows.Forms.Control[] { lblDiff, btnConfirm, btnCancel });
            this.Controls.Add(footer);
        }

        // ── Data ─────────────────────────────────────────────────────────

        private void PopulateGrid()
        {
            dgv.Rows.Clear();
            foreach (var item in _opname.Items)
            {
                var rowIdx = dgv.Rows.Add(
                    item.ProductCode,
                    item.ProductName,
                    item.Unit,
                    item.SystemStock,
                    item.PhysicalStock,
                    item.DifferenceDisplay
                );
                dgv.Rows[rowIdx].Tag = item;
            }
            UpdateDiffSummary();
        }

        private void FilterRows(string keyword)
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                var code = row.Cells["colCode"].Value?.ToString() ?? "";
                var name = row.Cells["colName"].Value?.ToString() ?? "";
                row.Visible = string.IsNullOrWhiteSpace(keyword)
                    || code.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        private void UpdateDiffSummary()
        {
            int diffCount = _opname.Items.FindAll(i => i.Difference != 0).Count;
            lblDiff.Text = diffCount == 0 ? "Tidak ada perbedaan stok" : $"{diffCount} produk berbeda dari sistem";
            lblDiff.ForeColor = diffCount == 0 ? Color.FromArgb(39, 174, 96) : Color.FromArgb(192, 57, 43);
        }

        private void ColorizeRow(DataGridViewRow row, int diff)
        {
            if (diff > 0)
            {
                row.Cells["colDiff"].Style.ForeColor = Color.FromArgb(30, 130, 76);
                row.Cells["colDiff"].Style.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            }
            else if (diff < 0)
            {
                row.Cells["colDiff"].Style.ForeColor = Color.FromArgb(192, 57, 43);
                row.Cells["colDiff"].Style.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            }
            else
            {
                row.Cells["colDiff"].Style.ForeColor = Color.FromArgb(150, 150, 150);
                row.Cells["colDiff"].Style.Font = new Font("Segoe UI", 9f);
            }
        }

        // ── Events ───────────────────────────────────────────────────────

        private void DgvCellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (dgv.Columns[e.ColumnIndex].Name != "colPhysical") return;

            var row = dgv.Rows[e.RowIndex];
            var item = row.Tag as StockOpnameItem;
            if (item == null) return;

            var raw = row.Cells["colPhysical"].Value?.ToString() ?? "";
            if (!int.TryParse(raw, out int physical) || physical < 0)
            {
                // Kembalikan ke nilai sebelumnya jika input tidak valid
                row.Cells["colPhysical"].Value = item.PhysicalStock;
                return;
            }

            item.PhysicalStock = physical;
            row.Cells["colDiff"].Value = item.DifferenceDisplay;
            ColorizeRow(row, item.Difference);
            UpdateDiffSummary();
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            var diffCount = _opname.Items.FindAll(i => i.Difference != 0).Count;

            var confirmMsg = diffCount == 0
                ? "Tidak ada perbedaan stok. Tetap konfirmasi opname ini?"
                : $"{diffCount} produk akan disesuaikan stoknya.\n\nLanjutkan konfirmasi?";

            var confirm = MessageBox.Show(confirmMsg, "Konfirmasi Opname",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            var (success, message) = _service.Confirm(_opname);

            if (success)
            {
                MessageBox.Show(message, "Opname Selesai",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show(message, "Gagal", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
