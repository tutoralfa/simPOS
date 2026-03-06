using simPOS.Shared.Models;
using simPOS.Shared.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace simPOS.POS.Forms
{
    public class FormPOS : Form
    {
        private readonly ClerkService _clerk = new ClerkService();
        private readonly TransactionService _service = new TransactionService();
        private readonly List<TransactionItem> _cart = new List<TransactionItem>();

        // ── Mode keypad ──────────────────────────────────────────────────
        // null  = mode barcode (input ke txtBarcode)
        // item  = mode edit qty (input mengubah qty item terpilih)
        private TransactionItem _selectedItem = null;

        // Buffer digit keypad
        private string _keypadBuffer = "";

        // ── Controls ─────────────────────────────────────────────────────
        private TextBox txtBarcode;
        private Label lblKeypadMode;
        private Label lblKeypadBuffer;
        private DataGridView dgvCart;
        private Label lblCartEmpty;
        private Label lblTotal;
        private Button btnBayar;
        private Button btnVoid;
        private Button btnMenuClerk;

        // ══════════════════════════════════════════════════════════════════
        // INIT
        // ══════════════════════════════════════════════════════════════════

        public FormPOS()
        {
            InitializeComponent();
            SetKeypadMode(null);
        }

        private void InitializeComponent()
        {
            this.Text = "simPOS — Kasir";
            this.MinimizeBox = true;
            this.MaximizeBox = true;
            this.Size = new Size(1100, 700);
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(236, 240, 241);
            this.KeyPreview = true;
            this.KeyDown += FormPOS_KeyDown;
            EnsureAndCheckSession();

            BuildBody();
            BuildHeader();
            
        }

        // ══════════════════════════════════════════════════════════════════
        // LAYOUT
        // ══════════════════════════════════════════════════════════════════

        private void BuildHeader()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.FromArgb(44, 62, 80)
            };
            header.Controls.Add(new Label
            {
                Text = "POS v1.0",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            });
            this.Controls.Add(header);
        }

        private void BuildBody()
        {
            // ⚠ Fill dulu, Right belakangan
            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };

            var pnlRight = new Panel { Dock = DockStyle.Right, Width = 320, Padding = new Padding(8, 0, 0, 0) };
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0) };
            var pnlButtom = new Panel { Dock = DockStyle.Bottom, Height = 72, Padding = new Padding(10) };
            BuildRightPanel(pnlRight);
            BuildLeftPanel(pnlLeft);
            BuildBottomPanel(pnlButtom);

            body.Controls.Add(pnlLeft);
            body.Controls.Add(pnlRight);
            body.Controls.Add(pnlButtom);
            this.Controls.Add(body);
        }

        //---panel bawah--

        private void BuildBottomPanel(Panel parent)
        {
            var pnlMenuButton = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 76,
                BackColor = Color.White,
                Padding = new Padding(12, 8, 12, 8)
            };
            pnlMenuButton.Paint += PaintCardBorder;


            var lblMenuTitle = new Label
            {
                Text = "Menu",
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            btnMenuClerk = new Button
            {
                Text = "F9 Clerk",
                Width = 42,
                Height = 42,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                Location = new Point(0, 5)
            };
            btnMenuClerk.FlatAppearance.BorderSize = 0;
            btnMenuClerk.Click += BtnMenuClerk_Click;

            pnlMenuButton.Controls.Add(lblMenuTitle);
            pnlMenuButton.Controls.Add(btnMenuClerk);

            var spacer = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent };
        }

        // ── Panel Kiri ────────────────────────────────────────────────────

        private void BuildLeftPanel(Panel parent)
        {
            // Barcode input (Top)
            var pnlBarcode = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = Color.White,
                Padding = new Padding(12, 8, 12, 8)
            };
            pnlBarcode.Paint += PaintCardBorder;

            var lblBarcodeTitle = new Label
            {
                Text = "Barcode / Nama Barang",
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            txtBarcode = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 13f),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Scan barcode atau ketik nama barang..."
            };
            txtBarcode.KeyDown += TxtBarcode_KeyDown;
            txtBarcode.TextChanged += TxtBarcode_TextChanged;

            pnlBarcode.Controls.Add(txtBarcode);
            pnlBarcode.Controls.Add(lblBarcodeTitle);



            // Spacer
            var spacer = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent };

            



            // Cart (Fill)
            var pnlCart = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlCart.Paint += PaintCardBorder;

            var lblCartTitle = new Label
            {
                Text = "  List Belanjaan",
                Dock = DockStyle.Top,
                Height = 34,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                BackColor = Color.FromArgb(245, 248, 250),
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblCartEmpty = new Label
            {
                Text = "Keranjang kosong\nScan barcode atau ketik nama barang",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(190, 190, 190),
                Visible = true
            };

            dgvCart = new DataGridView
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
                RowTemplate = { Height = 38 },
                Visible = false
            };

            dgvCart.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 250);
            dgvCart.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(44, 62, 80);
            dgvCart.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgvCart.EnableHeadersVisualStyles = false;
            dgvCart.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
            dgvCart.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvCart.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);

            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "Kode", FillWeight = 70 });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Nama Barang", FillWeight = 200 });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty", HeaderText = "Qty", FillWeight = 45 });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice", HeaderText = "Harga", FillWeight = 95 });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSub", HeaderText = "Subtotal", FillWeight = 105 });

            var colDel = new DataGridViewButtonColumn
            {
                Name = "colDel",
                HeaderText = "",
                Text = "✕",
                UseColumnTextForButtonValue = true,
                FillWeight = 28
            };
            colDel.DefaultCellStyle.ForeColor = Color.FromArgb(192, 57, 43);
            colDel.DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            colDel.DefaultCellStyle.BackColor = Color.White;
            colDel.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvCart.Columns.Add(colDel);

            dgvCart.Columns["colQty"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvCart.Columns["colQty"].DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            dgvCart.Columns["colPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvCart.Columns["colSub"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvCart.Columns["colCode"].DefaultCellStyle.Font = new Font("Consolas", 9.5f);

            dgvCart.SelectionChanged += DgvCart_SelectionChanged;
            dgvCart.CellClick += DgvCart_CellClick;

            



            pnlCart.Controls.Add(dgvCart);
            pnlCart.Controls.Add(lblCartEmpty);
            pnlCart.Controls.Add(lblCartTitle);

            // ⚠ Fill dulu, Top belakangan
            parent.Controls.Add(pnlCart);
            parent.Controls.Add(spacer);
            parent.Controls.Add(pnlBarcode);
        }

        // ── Panel Kanan ───────────────────────────────────────────────────

        private void BuildRightPanel(Panel parent)
        {
            // Total (Top)
            var pnlTotal = BuildTotalPanel();

            // Keypad (Fill)
            var pnlKeypadWrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 8) };
            pnlKeypadWrap.Controls.Add(BuildKeypadPanel());

            // Action buttons (Bottom)
            var pnlActions = BuildActionButtons();

            // ⚠ Fill dulu
            parent.Controls.Add(pnlKeypadWrap);
            parent.Controls.Add(pnlActions);
            parent.Controls.Add(pnlTotal);
        }

        private Panel BuildTotalPanel()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = Color.FromArgb(44, 62, 80),
                Padding = new Padding(14, 10, 14, 10)
            };

            pnl.Controls.Add(new Label
            {
                Text = "TOTAL",
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(127, 140, 141)
            });

            lblTotal = new Label
            {
                Text = "Rp 0",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 26f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnl.Controls.Add(lblTotal);
            return pnl;
        }

        private Panel BuildKeypadPanel()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            pnl.Paint += PaintCardBorder;

            // Mode indicator (Top)
            var pnlMode = new Panel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(4, 4, 4, 6) };

            lblKeypadMode = new Label
            {
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblKeypadBuffer = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.FromArgb(245, 248, 250),
                BorderStyle = BorderStyle.FixedSingle
            };

            pnlMode.Controls.Add(lblKeypadBuffer);
            pnlMode.Controls.Add(lblKeypadMode);

            // Tombol keypad
            var keys = new[]
            {
                new[] { "7", "8", "9"   },
                new[] { "4", "5", "6"   },
                new[] { "1", "2", "3"   },
                new[] { "0", "00", "⌫"  },
                new[] { "C", "↵ ENTER", "" }   // Enter span 2 kolom
            };

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 5
            };
            for (int c = 0; c < 3; c++)
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            for (int r = 0; r < 5; r++)
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));

            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    var key = keys[r][c];
                    tbl.Controls.Add(MakeKeyButton(key), c, r);
                }
            }

            // Baris 5: C (col 0) | ENTER span 2 (col 1-2)
            tbl.Controls.Add(MakeKeyButton("C"), 0, 4);
            var btnEnter = MakeKeyButton("↵ ENTER");
            btnEnter.BackColor = Color.FromArgb(52, 152, 219);
            btnEnter.ForeColor = Color.White;
            btnEnter.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnEnter.FlatAppearance.MouseOverBackColor = Color.FromArgb(41, 128, 185);
            tbl.Controls.Add(btnEnter, 1, 4);
            tbl.SetColumnSpan(btnEnter, 2);

            pnl.Controls.Add(tbl);
            pnl.Controls.Add(pnlMode);
            return pnl;
        }

        private Button MakeKeyButton(string key)
        {
            var isClear = key == "C";
            var isBack = key == "⌫";

            var btn = new Button
            {
                Text = key,
                Dock = DockStyle.Fill,
                Margin = new Padding(2),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                BackColor = isClear ? Color.FromArgb(231, 76, 60)
                          : isBack ? Color.FromArgb(200, 200, 200)
                          : Color.FromArgb(245, 248, 250),
                ForeColor = (isClear || isBack) ? Color.White : Color.FromArgb(44, 62, 80),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(215, 215, 215);
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = isClear ? Color.FromArgb(192, 57, 43)
                                                 : isBack ? Color.FromArgb(160, 160, 160)
                                                 : Color.FromArgb(52, 152, 219);

            var capturedKey = key;
            btn.Click += (s, e) => HandleKeypadPress(capturedKey);
            return btn;
        }

        private Panel BuildActionButtons()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 110,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 0)
            };

            btnVoid = new Button
            {
                Text = "🗑  Void / Batalkan",
                Dock = DockStyle.Top,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            btnVoid.FlatAppearance.BorderSize = 0;
            btnVoid.Click += BtnVoid_Click;

            btnBayar = new Button
            {
                Text = "BAYAR",
                Dock = DockStyle.Bottom,
                Height = 60,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBayar.FlatAppearance.BorderSize = 0;
            btnBayar.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 150, 80);
            btnBayar.Click += BtnBayar_Click;

            pnl.Controls.Add(btnBayar);
            pnl.Controls.Add(btnVoid);
            return pnl;
        }

        // ══════════════════════════════════════════════════════════════════
        // KEYPAD LOGIC
        // ══════════════════════════════════════════════════════════════════

        private void SetKeypadMode(TransactionItem item)
        {
            _selectedItem = item;
            _keypadBuffer = "";

            if (item == null)
            {
                // Mode barcode
                lblKeypadMode.Text = "MODE: INPUT BARCODE";
                lblKeypadMode.ForeColor = Color.FromArgb(100, 100, 100);
                lblKeypadBuffer.Text = "";
                lblKeypadBuffer.BackColor = Color.FromArgb(245, 248, 250);
                txtBarcode.Focus();
            }
            else
            {
                // Mode edit qty
                lblKeypadMode.Text = $"EDIT QTY: {item.ProductName}";
                lblKeypadMode.ForeColor = Color.FromArgb(52, 152, 219);
                lblKeypadBuffer.Text = item.Quantity.ToString();
                lblKeypadBuffer.BackColor = Color.FromArgb(230, 243, 255);
                _keypadBuffer = item.Quantity.ToString();
            }
        }

        private void HandleKeypadPress(string key)
        {
            if (_selectedItem == null)
            {
                // Mode barcode — input ke txtBarcode
                if (key == "↵ ENTER")
                {
                    ProcessBarcodeInput(txtBarcode.Text.Trim());
                }
                else if (key == "C")
                {
                    txtBarcode.Clear();
                    _keypadBuffer = "";
                }
                else if (key == "⌫")
                {
                    if (txtBarcode.Text.Length > 0)
                        txtBarcode.Text = txtBarcode.Text.Substring(0, txtBarcode.Text.Length - 1);
                }
                else
                {
                    txtBarcode.AppendText(key);
                    _keypadBuffer = txtBarcode.Text;
                }
                lblKeypadBuffer.Text = txtBarcode.Text;
            }
            else
            {
                // Mode edit qty
                if (key == "↵ ENTER")
                {
                    ApplyQtyEdit();
                }
                else if (key == "C")
                {
                    _keypadBuffer = "";
                    lblKeypadBuffer.Text = "";
                }
                else if (key == "⌫")
                {
                    if (_keypadBuffer.Length > 0)
                        _keypadBuffer = _keypadBuffer.Substring(0, _keypadBuffer.Length - 1);
                    lblKeypadBuffer.Text = _keypadBuffer;
                }
                else if (key == "00")
                {
                    if (_keypadBuffer.Length < 6)
                        _keypadBuffer += "00";
                    lblKeypadBuffer.Text = _keypadBuffer;
                }
                else
                {
                    if (_keypadBuffer.Length < 6)
                        _keypadBuffer += key;
                    lblKeypadBuffer.Text = _keypadBuffer;
                }
            }
        }

        private void ApplyQtyEdit()
        {
            if (_selectedItem == null) return;

            if (!int.TryParse(_keypadBuffer, out int newQty) || newQty < 0)
            {
                FlashBuffer(Color.FromArgb(255, 200, 200));
                return;
            }

            if (newQty == 0)
            {
                // Qty 0 = hapus dari cart
                _cart.Remove(_selectedItem);
                SetKeypadMode(null);
                RefreshCart();
                return;
            }

            _selectedItem.Quantity = newQty;
            RefreshCart();

            // Tetap di mode edit item ini, update buffer
            _keypadBuffer = newQty.ToString();
            lblKeypadBuffer.Text = _keypadBuffer;
            lblKeypadBuffer.BackColor = Color.FromArgb(200, 240, 210); // flash hijau
            var t = new System.Windows.Forms.Timer { Interval = 300 };
            t.Tick += (s, e) =>
            {
                lblKeypadBuffer.BackColor = Color.FromArgb(230, 243, 255);
                t.Stop(); t.Dispose();
            };
            t.Start();
        }

        // ══════════════════════════════════════════════════════════════════
        // BARCODE / SEARCH LOGIC
        // ══════════════════════════════════════════════════════════════════

        private void EnsureAndCheckSession()
        {
            _clerk.EnsureSessionOpen();

            if (!_clerk.IsSessionOpen())
            {
                ShowSessionClosed();
            }
        }

        private void ShowSessionClosed()
        {
            // Blokir semua input
            txtBarcode.Enabled = false;
            btnBayar.Enabled = false;

            // Tampilkan overlay penutupan
            var overlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(200, 44, 62, 80),
                Visible = true
            };
            var lblMsg = new Label
            {
                Text = "🔒  Kasir Sudah Ditutup Tidak ada transaksi baru hari ini. Sampai jumpa besok!",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };
            overlay.Controls.Add(lblMsg);
            this.Controls.Add(overlay);
            overlay.BringToFront();
        }

        private void ShowClerkForm()
        {
            // Hitung transaksi hari ini untuk ditampilkan di FormClerk
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            int trxCount = _cart?.Count ?? 0;

            // Ambil dari service — total transaksi yang sudah selesai hari ini
            var (totalTrx, totalQty, totalOmzet, _) =
                new simPOS.Shared.Reports.DashboardService().GetTodaySummary();

            var session = _clerk.GetTodaySession();

            var form = new FormClerk
            {
                TodayTrxCount = totalTrx,
                TodayOmzet = totalOmzet,
                OpenedAt = session?.OpenedAt ?? ""
            };

            if (form.ShowDialog(this) == DialogResult.OK)
            {
                // Kasir berhasil tutup — tampilkan overlay dan tutup form
                ShowSessionClosed();
                // Tutup aplikasi POS setelah 1.5 detik
                var t = new System.Windows.Forms.Timer { Interval = 1500 };
                t.Tick += (s, e) => { t.Stop(); Application.Exit(); };
                t.Start();
            }
        }

        private void ProcessBarcodeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;

            // Coba exact match by code dulu (barcode scan)
            var product = _service.GetProductByCode(input);

            if (product != null)
            {
                AddProductToCart(product);
                txtBarcode.Clear();
                lblKeypadBuffer.Text = "";
                return;
            }

            // Tidak ketemu by code → cari by nama
            var results = _service.SearchProducts(input);

            if (results.Count == 0)
            {
                FlashBarcode(Color.FromArgb(255, 210, 210));
                ShowTitleMessage($"Barang \"{input}\" tidak ditemukan.");
                return;
            }

            if (results.Count == 1)
            {
                // Hanya 1 hasil — langsung tambah
                AddProductToCart(results[0]);
                txtBarcode.Clear();
                lblKeypadBuffer.Text = "";
                return;
            }

            // Lebih dari 1 hasil → tampilkan popup pilihan
            ShowProductPickerPopup(results);
        }

        private void ShowProductPickerPopup(List<Product> products)
        {
            var popup = new FormProductPicker(products);
            popup.StartPosition = FormStartPosition.CenterParent;

            if (popup.ShowDialog(this) == DialogResult.OK && popup.SelectedProduct != null)
            {
                AddProductToCart(popup.SelectedProduct);
                txtBarcode.Clear();
                lblKeypadBuffer.Text = "";
            }
            txtBarcode.Focus();
        }

        /// <summary>
        /// Tampilkan popup semua produk aktif (dipanggil via F2).
        /// User bisa scroll/search untuk pilih barang tanpa scan barcode.
        /// </summary>
        private void ShowAllProductPickerPopup()
        {
            var allProducts = _service.GetAllActiveProducts();
            if (allProducts.Count == 0)
            {
                ShowTitleMessage("Tidak ada produk aktif.");
                return;
            }

            var popup = new FormProductPicker(allProducts, showSearch: true);
            popup.StartPosition = FormStartPosition.CenterParent;

            if (popup.ShowDialog(this) == DialogResult.OK && popup.SelectedProduct != null)
            {
                AddProductToCart(popup.SelectedProduct);
                txtBarcode.Clear();
                lblKeypadBuffer.Text = "";
            }
            txtBarcode.Focus();
        }

        private void AddProductToCart(Product product)
        {
            if (!product.IsActive)
            {
                ShowTitleMessage($"\"{product.Name}\" tidak aktif.");
                return;
            }

            if (product.Stock <= 0)
            {
                FlashBarcode(Color.FromArgb(255, 230, 180));
                ShowTitleMessage($"Stok \"{product.Name}\" habis.");
                return;
            }

            var existing = _cart.Find(i => i.ProductId == product.Id);
            if (existing != null)
            {
                if (existing.Quantity >= product.Stock)
                {
                    ShowTitleMessage($"Stok \"{product.Name}\" hanya {product.Stock} {product.Unit}.");
                    return;
                }
                existing.Quantity++;
            }
            else
            {
                _cart.Add(new TransactionItem
                {
                    ProductId = product.Id,
                    ProductCode = product.Code,
                    ProductName = product.Name,
                    Unit = product.Unit,
                    Quantity = 1,
                    SellPrice = product.SellPrice
                });
            }

            RefreshCart();
            FlashBarcode(Color.FromArgb(200, 255, 210));
        }

        // ══════════════════════════════════════════════════════════════════
        // CART
        // ══════════════════════════════════════════════════════════════════

        private void RefreshCart()
        {
            dgvCart.Rows.Clear();
            decimal total = 0;

            foreach (var item in _cart)
            {
                var rowIdx = dgvCart.Rows.Add(
                    item.ProductCode,
                    item.ProductName,
                    item.Quantity,
                    $"Rp {item.SellPrice:N0}",
                    $"Rp {item.Subtotal:N0}"
                );
                dgvCart.Rows[rowIdx].Tag = item;
                total += item.Subtotal;

                // Highlight baris item yang sedang di-edit
                if (_selectedItem != null && item == _selectedItem)
                {
                    dgvCart.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(230, 243, 255);
                    dgvCart.Rows[rowIdx].DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                }
            }

            bool hasItems = _cart.Count > 0;
            lblCartEmpty.Visible = !hasItems;
            dgvCart.Visible = hasItems;

            lblTotal.Text = $"Rp {total:N0}";

            if (dgvCart.Rows.Count > 0)
                dgvCart.FirstDisplayedScrollingRowIndex = dgvCart.Rows.Count - 1;

            // Re-select baris yang sedang diedit
            if (_selectedItem != null)
            {
                foreach (DataGridViewRow row in dgvCart.Rows)
                {
                    if (row.Tag == _selectedItem)
                    {
                        row.Selected = true;
                        break;
                    }
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // EVENTS
        // ══════════════════════════════════════════════════════════════════

        private void BtnMenuClerk_Click(object sender, EventArgs e)
        {
            // F9 = Clerk (tutup kasir)
            if (_clerk.IsSessionOpen())
                ShowClerkForm();
            else
                MessageBox.Show("Kasir sudah ditutup.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void TxtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ProcessBarcodeInput(txtBarcode.Text.Trim());
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                txtBarcode.Clear();
                e.SuppressKeyPress = true;
            }
        }

        private void TxtBarcode_TextChanged(object sender, EventArgs e)
        {
            // Sync buffer display saat user ketik langsung di textbox
            if (_selectedItem == null)
                lblKeypadBuffer.Text = txtBarcode.Text;
        }

        private void DgvCart_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvCart.SelectedRows.Count == 0) return;
            var item = dgvCart.SelectedRows[0].Tag as TransactionItem;
            if (item != null && item != _selectedItem)
                SetKeypadMode(item);
        }

        private void DgvCart_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (dgvCart.Columns[e.ColumnIndex].Name == "colDel")
            {
                var item = dgvCart.Rows[e.RowIndex].Tag as TransactionItem;
                if (item != null)
                {
                    if (_selectedItem == item) SetKeypadMode(null);
                    _cart.Remove(item);
                    RefreshCart();
                }
                return;
            }

            // Klik baris → masuk mode edit qty
            var clicked = dgvCart.Rows[e.RowIndex].Tag as TransactionItem;
            if (clicked != null) SetKeypadMode(clicked);
        }

        

        private void FormPOS_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F1:
                    // F1 = kembali ke mode barcode
                    SetKeypadMode(null);
                    txtBarcode.SelectAll();
                    e.Handled = true;
                    break;

                case Keys.F2:
                    // F2 = popup semua produk
                    ShowAllProductPickerPopup();
                    e.Handled = true;
                    break;

                case Keys.F9:
                    // F9 = Clerk (tutup kasir)
                    if (_clerk.IsSessionOpen())
                        ShowClerkForm();
                    else
                        MessageBox.Show("Kasir sudah ditutup.", "Info",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    e.Handled = true;
                    break;

                case Keys.F12:
                    BtnBayar_Click(null, null);
                    e.Handled = true;
                    break;

                case Keys.Escape:
                    if (_selectedItem != null)
                    {
                        SetKeypadMode(null);
                        e.Handled = true;
                    }
                    break;

                case Keys.Delete:
                    if (_selectedItem != null)
                    {
                        _cart.Remove(_selectedItem);
                        SetKeypadMode(null);
                        RefreshCart();
                        e.Handled = true;
                    }
                    break;
            }
        }

        private decimal GetCartTotal()
        {
            decimal total = 0;
            foreach (var item in _cart) total += item.Subtotal;
            return total;
        }

        private void BtnBayar_Click(object sender, EventArgs e)
        {
            if (!_clerk.IsSessionOpen())
            {
                MessageBox.Show("Kasir sudah ditutup. Tidak bisa transaksi.", "Sesi Ditutup",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_cart.Count == 0)
            {
                ShowTitleMessage("Keranjang masih kosong.");
                return;
            }

            decimal total = GetCartTotal();
            var payment = new FormPayment(_cart, total);

            if (payment.ShowDialog(this) == DialogResult.OK && payment.Confirmed)
            {
                // Transaksi berhasil — reset POS untuk pelanggan berikutnya
                _cart.Clear();
                SetKeypadMode(null);
                RefreshCart();
            }
        }

        private void BtnVoid_Click(object sender, EventArgs e)
        {
            if (_cart.Count == 0) return;
            var confirm = MessageBox.Show("Batalkan semua item di keranjang?",
                "Void Transaksi", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                _cart.Clear();
                SetKeypadMode(null);
                RefreshCart();
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // UX HELPERS
        // ══════════════════════════════════════════════════════════════════

        private void FlashBarcode(Color color)
        {
            txtBarcode.BackColor = color;
            var t = new System.Windows.Forms.Timer { Interval = 350 };
            t.Tick += (s, e) => { txtBarcode.BackColor = SystemColors.Window; t.Stop(); t.Dispose(); };
            t.Start();
        }

        private void FlashBuffer(Color color)
        {
            lblKeypadBuffer.BackColor = color;
            var t = new System.Windows.Forms.Timer { Interval = 350 };
            t.Tick += (s, e) =>
            {
                lblKeypadBuffer.BackColor = _selectedItem != null
                    ? Color.FromArgb(230, 243, 255)
                    : Color.FromArgb(245, 248, 250);
                t.Stop(); t.Dispose();
            };
            t.Start();
        }

        private void ShowTitleMessage(string msg)
        {
            var original = this.Text;
            this.Text = $"⚠  {msg}";
            var t = new System.Windows.Forms.Timer { Interval = 2500 };
            t.Tick += (s, e) => { this.Text = original; t.Stop(); t.Dispose(); };
            t.Start();
        }

        private static void PaintCardBorder(object sender, PaintEventArgs e)
        {
            var p = sender as Panel;
            if (p == null) return;
            e.Graphics.DrawRectangle(new Pen(Color.FromArgb(218, 220, 224)),
                0, 0, p.Width - 1, p.Height - 1);
        }
    }
}
