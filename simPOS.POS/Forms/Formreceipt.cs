using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using simPOS.Shared.Models;

namespace simPOS.POS.Forms
{
    /// <summary>
    /// Struk transaksi — tampil setelah bayar berhasil.
    /// Menampilkan ringkasan, ada tombol Cetak dan Transaksi Baru.
    /// </summary>
    public class FormReceipt : Form
    {
        private readonly Transaction _trx;

        public FormReceipt(Transaction trx)
        {
            _trx = trx;
            InitializeComponent();
            PopulateReceipt();
        }

        private RichTextBox rtbReceipt;
        private Button btnPrint;
        private Button btnClose;

        private void InitializeComponent()
        {
            this.Text = $"Struk — {_trx.InvoiceNo}";
            this.Size = new Size(360, 560);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            rtbReceipt = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 9.5f),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.FromArgb(248, 248, 248)
            };
            footer.Paint += (s, e) => e.Graphics.DrawLine(
                new Pen(Color.FromArgb(220, 220, 220)), 0, 0, footer.Width, 0);

            btnPrint = new Button
            {
                Text = "🖨 Cetak",
                Width = 100,
                Height = 33,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnPrint.FlatAppearance.BorderSize = 0;
            btnPrint.Location = new Point(footer.Width - 220, 9);
            btnPrint.Click += BtnPrint_Click;

            btnClose = new Button
            {
                Text = "✔ Transaksi Baru",
                Width = 130,
                Height = 33,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Location = new Point(footer.Width - 110, 9);
            btnClose.Click += (s, e) => this.Close();

            footer.Controls.Add(btnPrint);
            footer.Controls.Add(btnClose);

            this.Controls.Add(rtbReceipt);
            this.Controls.Add(footer);
        }

        private void PopulateReceipt()
        {
            var sb = new StringBuilder();
            var w = 38; // lebar struk dalam karakter

            sb.AppendLine(Center("================================", w));
            sb.AppendLine(Center("sim POS", w));
            sb.AppendLine(Center("================================", w));
            sb.AppendLine();
            sb.AppendLine($"No. Invoice : {_trx.InvoiceNo}");
            sb.AppendLine($"Tanggal     : {_trx.CreatedAt}");
            sb.AppendLine(new string('-', w));
            sb.AppendLine($"{"Barang",-20} {"Qty",3} {"Harga",10}");
            sb.AppendLine(new string('-', w));

            foreach (var item in _trx.Items)
            {
                var name = item.ProductName.Length > 20
                    ? item.ProductName.Substring(0, 18) + ".."
                    : item.ProductName;
                sb.AppendLine($"{name,-20} {item.Quantity,3} {item.Subtotal,10:N0}");
            }

            sb.AppendLine(new string('=', w));
            sb.AppendLine(PadRight("TOTAL", $"Rp {_trx.TotalAmount:N0}", w));
            sb.AppendLine(PadRight("BAYAR", $"Rp {_trx.PaidAmount:N0}", w));
            sb.AppendLine(PadRight("KEMBALI", $"Rp {_trx.ChangeAmount:N0}", w));
            sb.AppendLine(new string('=', w));
            sb.AppendLine();
            sb.AppendLine(Center("Terima kasih!", w));
            sb.AppendLine(Center("Selamat berbelanja kembali", w));
            sb.AppendLine();

            rtbReceipt.Text = sb.ToString();
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            var pd = new PrintDocument();
            pd.PrintPage += (s, ev) =>
            {
                ev.Graphics.DrawString(
                    rtbReceipt.Text,
                    new Font("Courier New", 8f),
                    Brushes.Black,
                    ev.MarginBounds);
            };

            var preview = new PrintPreviewDialog
            {
                Document = pd,
                Width = 500,
                Height = 700,
                StartPosition = FormStartPosition.CenterParent
            };
            preview.ShowDialog(this);
        }

        private static string Center(string text, int width)
        {
            if (text.Length >= width) return text;
            var pad = (width - text.Length) / 2;
            return text.PadLeft(pad + text.Length).PadRight(width);
        }

        private static string PadRight(string left, string right, int width)
        {
            var gap = width - left.Length - right.Length;
            return gap > 0 ? left + new string(' ', gap) + right : $"{left} {right}";
        }
    }
}
