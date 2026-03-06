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
    /// View read-only untuk opname yang sudah CONFIRMED.
    /// Menampilkan hanya produk yang mengalami penyesuaian (Difference != 0).
    /// </summary>
    public class FormStockOpnameDetail : Form
    {
        private readonly int _opnameId;
        private readonly StockOpnameService _service = new StockOpnameService();

        private Panel pnlInfo;
        private DataGridView dgv;
        private Label lblSummary;

        public FormStockOpnameDetail(int opnameId)
        {
            _opnameId = opnameId;
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Detail Stock Opname";
            this.Size = new Size(740, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            // ⚠ Urutan: Fill → Bottom → Top
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9f),
                ColumnHeadersHeight = 32
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 252);

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "Kode", FillWeight = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Nama Barang", FillWeight = 240 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit", HeaderText = "Satuan", FillWeight = 55 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSystem", HeaderText = "Stok Sistem", FillWeight = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPhysical", HeaderText = "Stok Fisik", FillWeight = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDiff", HeaderText = "Selisih", FillWeight = 70 });

            foreach (var col in new[] { "colSystem", "colPhysical", "colDiff" })
                dgv.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgv.Columns["colCode"].DefaultCellStyle.Font = new Font("Consolas", 9f);

            this.Controls.Add(dgv);

            lblSummary = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                BackColor = Color.FromArgb(44, 62, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };
            this.Controls.Add(lblSummary);

            pnlInfo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(236, 240, 241),
                Padding = new Padding(16, 10, 16, 10)
            };
            this.Controls.Add(pnlInfo);
        }

        private void LoadData()
        {
            var opname = _service.GetById(_opnameId);
            if (opname == null) { this.Close(); return; }

            this.Text = $"Detail Opname — {opname.OpnameNo}";

            // Header info
            pnlInfo.Controls.Clear();
            pnlInfo.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(44, 62, 80),
                Text = $"No. Opname  :  {opname.OpnameNo}     |     Status: {opname.Status}\n" +
                            $"Dibuat          :  {opname.CreatedAt}     |     Dikonfirmasi: {opname.ConfirmedAt}\n" +
                            (string.IsNullOrEmpty(opname.Notes) ? "" : $"Catatan        :  {opname.Notes}")
            });

            // Isi grid — hanya tampilkan yang ada penyesuaian
            int adjCount = 0;
            foreach (var item in opname.Items)
            {
                var diff = item.Difference;
                var rowIdx = dgv.Rows.Add(
                    item.ProductCode,
                    item.ProductName,
                    item.Unit,
                    item.SystemStock,
                    item.PhysicalStock,
                    item.DifferenceDisplay
                );

                // Warna selisih
                var diffCell = dgv.Rows[rowIdx].Cells["colDiff"];
                if (diff > 0) { diffCell.Style.ForeColor = Color.FromArgb(30, 130, 76); diffCell.Style.Font = new Font("Segoe UI", 9f, FontStyle.Bold); }
                else if (diff < 0) { diffCell.Style.ForeColor = Color.FromArgb(192, 57, 43); diffCell.Style.Font = new Font("Segoe UI", 9f, FontStyle.Bold); }
                if (diff != 0) adjCount++;
            }

            lblSummary.Text = $"  {opname.Items.Count} produk tercatat  |  {adjCount} produk disesuaikan";
        }
    }
}
