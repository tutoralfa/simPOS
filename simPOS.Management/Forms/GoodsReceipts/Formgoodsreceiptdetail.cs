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
    public class FormGoodsReceiptDetail : Form
    {
        private readonly int _receiptId;
        private readonly GoodsReceiptService _service = new GoodsReceiptService();

        public FormGoodsReceiptDetail(int receiptId)
        {
            _receiptId = receiptId;
            InitializeComponent();
            LoadData();
        }

        private Panel pnlInfo;
        private DataGridView dgv;
        private Label lblTotal;

        private void InitializeComponent()
        {
            this.Text = "Detail Penerimaan Barang";
            this.Size = new Size(750, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            // ⚠ Urutan PENTING: Fill → Bottom → Top
            // dgv (Fill) harus ditambahkan PERTAMA
            // Grid item
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
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit", HeaderText = "Satuan", FillWeight = 60 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty", HeaderText = "Jumlah", FillWeight = 70 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice", HeaderText = "Harga Beli", FillWeight = 110 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSubtotal", HeaderText = "Subtotal", FillWeight = 120 });

            foreach (var col in new[] { "colQty", "colPrice", "colSubtotal" })
                dgv.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgv.Columns["colCode"].DefaultCellStyle.Font = new Font("Consolas", 9f);

            this.Controls.Add(dgv);

            // Status bar total
            lblTotal = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Color.FromArgb(44, 62, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 20, 0)
            };
            this.Controls.Add(lblTotal);

            // Info header ditambahkan TERAKHIR agar mendorong Fill ke bawah
            pnlInfo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Color.FromArgb(236, 240, 241),
                Padding = new Padding(16, 10, 16, 10)
            };
            this.Controls.Add(pnlInfo);
        }

        private void LoadData()
        {
            var receipt = _service.GetById(_receiptId);
            if (receipt == null) { this.Close(); return; }

            this.Text = $"Detail Penerimaan — {receipt.ReceiptNo}";

            // Render info header
            pnlInfo.Controls.Clear();
            var info = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f),
                Text = $"No. Penerimaan  :  {receipt.ReceiptNo}\n" +
                           $"Supplier              :  {receipt.SupplierName}\n" +
                           $"Tanggal Terima   :  {receipt.ReceivedAt}" +
                           (string.IsNullOrEmpty(receipt.Notes) ? "" : $"\nCatatan               :  {receipt.Notes}"),
                ForeColor = Color.FromArgb(44, 62, 80)
            };
            pnlInfo.Controls.Add(info);

            // Render items
            decimal total = 0;
            foreach (var item in receipt.Items)
            {
                dgv.Rows.Add(
                    item.ProductCode,
                    item.ProductName,
                    item.Unit,
                    item.Quantity,
                    item.BuyPrice.ToString("N0"),
                    item.Subtotal.ToString("N0")
                );
                total += item.Subtotal;
            }

            lblTotal.Text = $"Total Nilai Penerimaan:   Rp {total:N0}   ";
        }
    }
}
