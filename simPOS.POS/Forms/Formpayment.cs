using simPOS.Shared.Models;
using simPOS.Shared.Printing;
using simPOS.Shared.Services;
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

namespace simPOS.POS.Forms
{
    public class FormPayment : Form
    {
        private readonly List<TransactionItem> _cartItems;
        private readonly decimal _total;
        private readonly TransactionService _service = new TransactionService();

        // State
        private decimal _paidAmount = 0;
        private string _keypadBuffer = "";

        // Preset nominal uang
        private static readonly decimal[] Nominals =
        {
            10_000, 20_000, 50_000,
            100_000, 200_000, 500_000
        };

        // ── Controls ──────────────────────────────────────────────────
        // Panel kiri — struk
        private RichTextBox rtbStruk;

        // Panel tengah — total + nominal
        private Label lblTotalValue;

        // Panel kanan — uang dibayar, kembalian, keypad
        private Label lblPaidValue;
        private Label lblChangeValue;
        private Button btnBayar;
        private Button btnKembali; // batal / kembali ke POS

        // Public result
        public bool Confirmed { get; private set; } = false;
        public Transaction ResultTrx { get; private set; }

        // ══════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════════════

        public FormPayment(List<TransactionItem> cartItems, decimal total)
        {
            _cartItems = cartItems;
            _total = total;

            InitializeComponent();
            RenderStruk();
            UpdatePaymentDisplay();
        }

        // ══════════════════════════════════════════════════════════════
        // LAYOUT
        // ══════════════════════════════════════════════════════════════

        private void InitializeComponent()
        {
            this.Text = "Konfirmasi Pembayaran";
            this.Size = new Size(1050, 660);
            this.MinimumSize = new Size(900, 580);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(236, 240, 241);
            this.KeyPreview = true;
            this.KeyDown += FormPayment_KeyDown;

            // Tiga panel: Left (struk) | Center (total+nominal) | Right (bayar+keypad)
            // ⚠ Fill dulu, kemudian Right, kemudian Left
            var pnlCenter = BuildCenterPanel();
            var pnlRight = BuildRightPanel();
            var pnlLeft = BuildLeftPanel();

            this.Controls.Add(pnlCenter);
            this.Controls.Add(pnlRight);
            this.Controls.Add(pnlLeft);
        }

        // ── Panel Kiri: Struk ─────────────────────────────────────────

        private Panel BuildLeftPanel()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Color.White,
                Padding = new Padding(0)
            };
            pnl.Paint += PaintCardBorder;

            var lblTitle = new Label
            {
                Text = "  STRUK",
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(44, 62, 80),
                TextAlign = ContentAlignment.MiddleLeft
            };

            rtbStruk = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Courier New", 8.5f),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            pnl.Controls.Add(rtbStruk);
            pnl.Controls.Add(lblTitle);
            return pnl;
        }

        // ── Panel Tengah: Total + Nominal ─────────────────────────────

        private Panel BuildCenterPanel()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(12, 12, 6, 12)
            };

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20, 16, 20, 16)
            };
            card.Paint += PaintCardBorder;

            // "TOTAL BELANJA"
            var lblTotalTitle = new Label
            {
                Text = "TOTAL BELANJA",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Kotak total besar
            var pnlTotalBox = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Color.FromArgb(44, 62, 80),
                Margin = new Padding(0, 4, 0, 0)
            };
            lblTotalValue = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 28f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = $"Rp {_total:N0}"
            };
            pnlTotalBox.Controls.Add(lblTotalValue);

            // "PILIH NOMINAL UANG KONSUMEN"
            var spacer1 = new Panel { Dock = DockStyle.Top, Height = 18, BackColor = Color.White };
            var lblNominalTitle = new Label
            {
                Text = "PILIH NOMINAL UANG KONSUMEN",
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var spacer2 = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.White };

            // Grid 2x3 nominal
            var tblNominal = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 140,
                ColumnCount = 3,
                RowCount = 2,
                BackColor = Color.White
            };
            for (int c = 0; c < 3; c++)
                tblNominal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tblNominal.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tblNominal.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            for (int i = 0; i < Nominals.Length; i++)
            {
                var nominal = Nominals[i];
                var btn = new Button
                {
                    Text = $"Rp {nominal:N0}",
                    Dock = DockStyle.Fill,
                    Margin = new Padding(4),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    BackColor = Color.FromArgb(245, 248, 250),
                    ForeColor = Color.FromArgb(44, 62, 80),
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 152, 219);

                var capturedNominal = nominal;
                btn.Click += (s, e) =>
                {
                    _paidAmount = capturedNominal;
                    _keypadBuffer = ((long)capturedNominal).ToString();
                    UpdatePaymentDisplay();
                };
                btn.MouseEnter += (s, e) => btn.ForeColor = Color.White;
                btn.MouseLeave += (s, e) => btn.ForeColor = Color.FromArgb(44, 62, 80);

                tblNominal.Controls.Add(btn, i % 3, i / 3);
            }

            // Tombol UANG PAS di bawah nominal
            var spacer3 = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.White };
            var btnUangPas = new Button
            {
                Text = "💰  Uang Pas  (Rp " + $"{_total:N0})",
                Dock = DockStyle.Top,
                Height = 42,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnUangPas.FlatAppearance.BorderSize = 0;
            btnUangPas.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 62, 80);
            btnUangPas.Click += (s, e) =>
            {
                _paidAmount = _total;
                _keypadBuffer = ((long)_total).ToString();
                UpdatePaymentDisplay();
            };

            card.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Color.White }); // spacer bawah
            card.Controls.Add(btnUangPas);
            card.Controls.Add(spacer3);
            card.Controls.Add(tblNominal);
            card.Controls.Add(spacer2);
            card.Controls.Add(lblNominalTitle);
            card.Controls.Add(spacer1);
            card.Controls.Add(pnlTotalBox);
            card.Controls.Add(lblTotalTitle);

            pnl.Controls.Add(card);
            return pnl;
        }

        // ── Panel Kanan: Uang Dibayar + Kembalian + Keypad ────────────

        private Panel BuildRightPanel()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Right,
                Width = 260,
                BackColor = Color.White,
                Padding = new Padding(14, 14, 14, 14)
            };
            pnl.Paint += PaintCardBorder;

            // UANG DIBAYAR
            var lblPaidTitle = new Label
            {
                Text = "UANG DIBAYAR",
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var pnlPaidBox = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.FromArgb(245, 248, 250)
            };
            pnlPaidBox.Paint += PaintCardBorder;
            lblPaidValue = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 8, 0),
                Text = "Rp 0"
            };
            pnlPaidBox.Controls.Add(lblPaidValue);

            var sp1 = new Panel { Dock = DockStyle.Top, Height = 14, BackColor = Color.White };

            // ESTIMASI KEMBALIAN
            var lblChangeTitle = new Label
            {
                Text = "ESTIMASI KEMBALIAN",
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var pnlChangeBox = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.FromArgb(44, 62, 80)
            };
            pnlChangeBox.Paint += PaintCardBorder;
            lblChangeValue = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 204, 113),
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 8, 0),
                Text = "Rp 0"
            };
            pnlChangeBox.Controls.Add(lblChangeValue);

            var sp2 = new Panel { Dock = DockStyle.Top, Height = 14, BackColor = Color.White };

            // KEYPAD label
            var lblKeypadTitle = new Label
            {
                Text = "KEYPAD",
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Keypad 4x3
            var tblKeypad = BuildKeypad();

            // Tombol KEMBALI & BAYAR (Bottom)
            var pnlButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.White
            };

            btnKembali = new Button
            {
                Text = "◀ Batal",
                Width = 90,
                Height = 42,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                Location = new Point(0, 5)
            };
            btnKembali.FlatAppearance.BorderSize = 0;
            btnKembali.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            btnBayar = new Button
            {
                Text = "BAYAR ✔",
                Width = 120,
                Height = 42,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(96, 5)
            };
            btnBayar.FlatAppearance.BorderSize = 0;
            btnBayar.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 150, 76);
            btnBayar.Click += BtnBayar_Click;

            pnlButtons.Controls.Add(btnKembali);
            pnlButtons.Controls.Add(btnBayar);

            // ⚠ Fill (keypad) dulu, Bottom (tombol) lalu Top
            pnl.Controls.Add(tblKeypad);
            pnl.Controls.Add(pnlButtons);
            pnl.Controls.Add(lblKeypadTitle);
            pnl.Controls.Add(sp2);
            pnl.Controls.Add(pnlChangeBox);
            pnl.Controls.Add(lblChangeTitle);
            pnl.Controls.Add(sp1);
            pnl.Controls.Add(pnlPaidBox);
            pnl.Controls.Add(lblPaidTitle);

            return pnl;
        }

        private TableLayoutPanel BuildKeypad()
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 4,
                BackColor = Color.White
            };
            for (int c = 0; c < 3; c++)
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            for (int r = 0; r < 4; r++)
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));

            var keys = new[]
            {
                new[] { "7",  "8",  "9"  },
                new[] { "4",  "5",  "6"  },
                new[] { "1",  "2",  "3"  },
                new[] { "0",  "00", "C"  }
            };

            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    var key = keys[r][c];
                    var isClear = key == "C";

                    var btn = new Button
                    {
                        Text = key,
                        Dock = DockStyle.Fill,
                        Margin = new Padding(2),
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                        BackColor = isClear
                            ? Color.FromArgb(231, 76, 60)
                            : Color.FromArgb(245, 248, 250),
                        ForeColor = isClear ? Color.White : Color.FromArgb(44, 62, 80),
                        Cursor = Cursors.Hand
                    };
                    btn.FlatAppearance.BorderColor = Color.FromArgb(215, 215, 215);
                    btn.FlatAppearance.MouseOverBackColor = isClear
                        ? Color.FromArgb(192, 57, 43)
                        : Color.FromArgb(52, 152, 219);
                    //btn.FlatAppearance.MouseOverForeColor = Color.White;

                    var capturedKey = key;
                    btn.Click += (s, e) => HandleKeypad(capturedKey);
                    tbl.Controls.Add(btn, c, r);
                }
            }

            return tbl;
        }

        // ══════════════════════════════════════════════════════════════
        // STRUK
        // ══════════════════════════════════════════════════════════════

        private void RenderStruk()
        {
            const int W = 26;
            var sb = new StringBuilder();

            sb.AppendLine(Center("sim POS", W));
            sb.AppendLine(new string('=', W));
            sb.AppendLine();

            foreach (var item in _cartItems)
            {
                var name = item.ProductName.Length > W
                    ? item.ProductName.Substring(0, W - 2) + ".."
                    : item.ProductName;
                sb.AppendLine(name);
                sb.AppendLine($"  {item.Quantity}x @ {item.SellPrice:N0}");
                sb.AppendLine($"  {"",0}{item.Subtotal,W - 4:N0}");
            }

            sb.AppendLine(new string('-', W));
            sb.AppendLine(PadLR("TOTAL", $"{_total:N0}", W));

            rtbStruk.Text = sb.ToString();
        }

        // ══════════════════════════════════════════════════════════════
        // KEYPAD LOGIC
        // ══════════════════════════════════════════════════════════════

        private void HandleKeypad(string key)
        {
            if (key == "C")
            {
                _keypadBuffer = "";
                _paidAmount = 0;
            }
            else if (key == "00")
            {
                if (_keypadBuffer.Length < 12)
                    _keypadBuffer += "00";
            }
            else
            {
                if (_keypadBuffer.Length < 12)
                    _keypadBuffer += key;
            }

            if (long.TryParse(_keypadBuffer, out long raw))
                _paidAmount = raw;
            else
                _paidAmount = 0;

            UpdatePaymentDisplay();
        }

        private void UpdatePaymentDisplay()
        {
            lblPaidValue.Text = _paidAmount > 0 ? $"Rp {_paidAmount:N0}" : "Rp 0";

            var change = _paidAmount - _total;

            if (_paidAmount == 0)
            {
                lblChangeValue.Text = "Rp 0";
                lblChangeValue.ForeColor = Color.FromArgb(46, 204, 113);
                btnBayar.Enabled = false;
                btnBayar.BackColor = Color.FromArgb(100, 100, 100);
            }
            else if (change >= 0)
            {
                lblChangeValue.Text = $"Rp {change:N0}";
                lblChangeValue.ForeColor = Color.FromArgb(46, 204, 113);
                btnBayar.Enabled = true;
                btnBayar.BackColor = Color.FromArgb(39, 174, 96);
            }
            else
            {
                lblChangeValue.Text = $"- Rp {Math.Abs(change):N0}";
                lblChangeValue.ForeColor = Color.FromArgb(231, 76, 60);
                btnBayar.Enabled = false;
                btnBayar.BackColor = Color.FromArgb(100, 100, 100);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // BAYAR
        // ══════════════════════════════════════════════════════════════

        private void BtnBayar_Click(object sender, EventArgs e)
        {
            if (_paidAmount < _total) return;

            var trx = new Transaction
            {
                InvoiceNo = _service.GenerateInvoiceNo(),
                TotalAmount = _total,
                PaidAmount = _paidAmount,
                ChangeAmount = _paidAmount - _total,
                PaymentMethod = "CASH",
                Items = _cartItems,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            var (success, message) = _service.Save(trx);

            if (!success)
            {
                MessageBox.Show(message, "Gagal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Langsung print ke thermal, tidak tampilkan form struk
            PrintThermal(trx);

            ResultTrx = trx;
            Confirmed = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>
        /// Print langsung ke printer default tanpa dialog.
        /// Print struk ke thermal printer menggunakan ESC/POS.
        /// Konfigurasi diambil dari PrinterConfig (printer.json di AppData).
        /// Jika printer belum diset atau gagal, transaksi tetap tersimpan.
        /// </summary>
        private void PrintThermal(Transaction trx)
        {
            var cfg = PrinterConfig.Load();

            if (!cfg.PrintEnabled) return;

            if (string.IsNullOrWhiteSpace(cfg.PrinterName))
            {
                MessageBox.Show(
                    "Printer belum dikonfigurasi.\nBuka simPOS Management → Pengaturan Printer.",
                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var builder = new EscPosBuilder(cfg.CharPerLine);

                // ── Header toko ───────────────────────────────────────
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
                       .Divider();

                // ── Item belanjaan ────────────────────────────────────
                foreach (var item in trx.Items)
                {
                    var name = item.ProductName.Length > cfg.CharPerLine
                        ? item.ProductName.Substring(0, cfg.CharPerLine - 2) + ".."
                        : item.ProductName;
                    builder.TextLine(name);
                    builder.LeftRight($"  {item.Quantity}x {item.SellPrice:N0}",
                                      $"Rp {item.Subtotal:N0}");
                }

                // ── Total, Bayar, Kembali ─────────────────────────────
                builder.Divider('=')
                       .Bold().LeftRight("TOTAL", $"Rp {trx.TotalAmount:N0}")
                       .NoBold().LeftRight("BAYAR", $"Rp {trx.PaidAmount:N0}")
                       .Bold().LeftRight("KEMBALI", $"Rp {trx.ChangeAmount:N0}")
                       .Divider('=')
                       .Normal();

                // ── Footer ────────────────────────────────────────────
                builder.NewLine()
                       .Center()
                       .TextLine(cfg.FooterMessage)
                       .NewLine(2);

                if (cfg.AutoCut) builder.Cut();

                EscPosPrinter.PrintRaw(builder.Build(), cfg.PrinterName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Transaksi berhasil, namun gagal mencetak struk:\n{ex.Message}",
                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // EVENTS
        // ══════════════════════════════════════════════════════════════

        private void FormPayment_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    break;
                case Keys.Enter:
                    if (btnBayar.Enabled) BtnBayar_Click(null, null);
                    break;
                case Keys.D0: case Keys.NumPad0: HandleKeypad("0"); break;
                case Keys.D1: case Keys.NumPad1: HandleKeypad("1"); break;
                case Keys.D2: case Keys.NumPad2: HandleKeypad("2"); break;
                case Keys.D3: case Keys.NumPad3: HandleKeypad("3"); break;
                case Keys.D4: case Keys.NumPad4: HandleKeypad("4"); break;
                case Keys.D5: case Keys.NumPad5: HandleKeypad("5"); break;
                case Keys.D6: case Keys.NumPad6: HandleKeypad("6"); break;
                case Keys.D7: case Keys.NumPad7: HandleKeypad("7"); break;
                case Keys.D8: case Keys.NumPad8: HandleKeypad("8"); break;
                case Keys.D9: case Keys.NumPad9: HandleKeypad("9"); break;
                case Keys.Back: HandleKeypad("C"); break;
            }
        }

        // ══════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════

        private static void PaintCardBorder(object sender, PaintEventArgs e)
        {
            var p = sender as Panel;
            if (p == null) return;
            e.Graphics.DrawRectangle(new Pen(Color.FromArgb(218, 220, 224)),
                0, 0, p.Width - 1, p.Height - 1);
        }

        private static string Center(string text, int width)
        {
            if (text.Length >= width) return text;
            var pad = (width - text.Length) / 2;
            return text.PadLeft(pad + text.Length).PadRight(width);
        }

        private static string PadLR(string left, string right, int width)
        {
            var gap = width - left.Length - right.Length;
            return gap > 0 ? left + new string(' ', gap) + right : $"{left} {right}";
        }
    }
}
