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
using simPOS.Shared.Printing;
using simPOS.Shared.Services;

namespace simPOS.POS
{
    /// <summary>
    /// Form reprint struk — input nomor bon → cari → cetak ulang via ESC/POS.
    /// </summary>
    public class FormReprint : Form
    {
        private readonly TransactionService _service = new();

        private TextBox _txtInvoice;
        private Label _lblInfo;
        private Button _btnReprint;
        private Button _btnBatal;

        public FormReprint(string lastInvoiceNo = "")
        {
            InitUI();
            _txtInvoice.Text = lastInvoiceNo;
            _txtInvoice.SelectAll();
        }

        private void InitUI()
        {
            Text = "Reprint Struk";
            Size = new Size(360, 210);
            MinimumSize = new Size(340, 210);
            MaximumSize = new Size(500, 210);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            // Header
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.FromArgb(44, 62, 80)
            };
            header.Controls.Add(new Label
            {
                Text = "🖨  Reprint Struk",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0)
            });

            // Body
            var body = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 14, 20, 10)
            };

            var lblPrompt = new Label
            {
                Text = "Nomor Bon / Invoice:",
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(60, 60, 60)
            };

            _txtInvoice = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Consolas", 12f),
                CharacterCasing = CharacterCasing.Upper
            };
            // Enter = reprint
            _txtInvoice.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; DoReprint(); }
                if (e.KeyCode == Keys.Escape) Close();
            };

            _lblInfo = new Label
            {
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(130, 130, 130),
                Text = "Kosongkan & ketik nomor bon lain, atau tekan Reprint."
            };

            var spacer = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent };

            // Tombol
            var pnlBtn = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 42,
                BackColor = Color.Transparent
            };

            _btnReprint = new Button
            {
                Text = "🖨  Reprint",
                Width = 120,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(0, 4)
            };
            _btnReprint.FlatAppearance.BorderSize = 0;
            _btnReprint.Click += (s, e) => DoReprint();

            _btnBatal = new Button
            {
                Text = "Batal",
                Width = 90,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand,
                Location = new Point(128, 4)
            };
            _btnBatal.FlatAppearance.BorderSize = 0;
            _btnBatal.Click += (s, e) => Close();

            pnlBtn.Controls.Add(_btnReprint);
            pnlBtn.Controls.Add(_btnBatal);

            // Susun (DockStyle.Top terbalik)
            body.Controls.Add(pnlBtn);
            body.Controls.Add(spacer);
            body.Controls.Add(_lblInfo);
            body.Controls.Add(_txtInvoice);
            body.Controls.Add(lblPrompt);

            Controls.Add(body);
            Controls.Add(header);

            // Focus ke textbox setelah form load
            Shown += (s, e) => { _txtInvoice.Focus(); _txtInvoice.SelectAll(); };
        }

        private void DoReprint()
        {
            string invoiceNo = _txtInvoice.Text.Trim();
            if (string.IsNullOrEmpty(invoiceNo))
            {
                SetInfo("⚠ Nomor bon tidak boleh kosong.", error: true);
                return;
            }

            SetInfo("Mencari transaksi...", error: false);
            this.Cursor = Cursors.WaitCursor;

            Transaction trx;
            try
            {
                trx = _service.GetByInvoiceNo(invoiceNo);
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                SetInfo($"⚠ Error: {ex.Message}", error: true);
                return;
            }

            this.Cursor = Cursors.Default;

            if (trx == null)
            {
                SetInfo($"⚠ Nomor bon '{invoiceNo}' tidak ditemukan.", error: true);
                _txtInvoice.Focus();
                _txtInvoice.SelectAll();
                return;
            }

            // Konfirmasi sebelum print
            var confirm = MessageBox.Show(
                $"Cetak ulang struk:\nNo  : {trx.InvoiceNo}\nTgl : {trx.CreatedAt}\nTotal: Rp {trx.TotalAmount:N0}\n\nLanjutkan?",
                "Konfirmasi Reprint",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (confirm != DialogResult.Yes) return;

            SetInfo("Mencetak...", error: false);
            PrintThermal(trx);
        }

        private void PrintThermal(Transaction trx)
        {
            var cfg = PrinterConfig.Load();

            if (!cfg.PrintEnabled)
            {
                SetInfo("⚠ Printer dinonaktifkan di pengaturan.", error: true);
                return;
            }

            if (string.IsNullOrWhiteSpace(cfg.PrinterName))
            {
                MessageBox.Show(
                    "Printer belum dikonfigurasi.\nBuka simPOS Management → Pengaturan → Printer.",
                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var builder = new EscPosBuilder(cfg.CharPerLine);

                builder.Initialize()
                       .Center().Bold().Font2H()
                       .TextLine(cfg.StoreName)
                       .Normal().Center();

                if (!string.IsNullOrWhiteSpace(cfg.StoreAddress))
                    builder.TextLine(cfg.StoreAddress);
                if (!string.IsNullOrWhiteSpace(cfg.StorePhone))
                    builder.TextLine(cfg.StorePhone);

                builder.Divider('=')
                       .Left()
                       .TextLine($"No  : {trx.InvoiceNo}")
                       .TextLine($"Tgl : {trx.CreatedAt}")
                       .Center().TextLine("** REPRINT **").Left()
                       .Divider();

                foreach (var item in trx.Items)
                {
                    var name = item.ProductName.Length > cfg.CharPerLine
                        ? item.ProductName.Substring(0, cfg.CharPerLine - 2) + ".."
                        : item.ProductName;
                    builder.TextLine(name);
                    builder.LeftRight($"  {item.Quantity}x {item.SellPrice:N0}",
                                      $"Rp {item.Subtotal:N0}");
                }

                builder.Divider('=')
                       .Bold().LeftRight("TOTAL", $"Rp {trx.TotalAmount:N0}")
                       .NoBold().LeftRight("BAYAR", $"Rp {trx.PaidAmount:N0}")
                       .Bold().LeftRight("KEMBALI", $"Rp {trx.ChangeAmount:N0}")
                       .Divider('=')
                       .Normal()
                       .NewLine()
                       .Center()
                       .TextLine(cfg.FooterMessage)
                       .NewLine(2);

                if (cfg.AutoCut) builder.Cut();

                EscPosPrinter.PrintRaw(builder.Build(), cfg.PrinterName);

                SetInfo($"✅ Struk {trx.InvoiceNo} berhasil dicetak ulang.", error: false);
            }
            catch (Exception ex)
            {
                SetInfo($"⚠ Gagal cetak: {ex.Message}", error: true);
                MessageBox.Show(
                    $"Gagal mencetak struk:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetInfo(string msg, bool error)
        {
            _lblInfo.Text = msg;
            _lblInfo.ForeColor = error
                ? Color.FromArgb(192, 57, 43)
                : Color.FromArgb(39, 120, 70);
        }
    }
}
