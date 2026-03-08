using simPOS.Management.Forms.Categories;
using simPOS.Management.Forms.Dashboard;
using simPOS.Management.Forms.Eod;
using simPOS.Management.Forms.GoodsReceipts;
using simPOS.Management.Forms.POSMgmt;
using simPOS.Management.Forms.Products;
using simPOS.Management.Forms.Reports;
using simPOS.Management.Forms.Settings;
using simPOS.Management.Forms.StockOpnameMgmt;
using simPOS.Management.Forms.SupplierMgmt;
using simPOS.Shared.Services;

namespace simPOS.Management
{
    public class MainForm : Form
    {
        private Panel pnlMenu;
        private Panel pnlContent;
        private Label lblTitle;
        private Button _activeMenuButton;

        private Button btnBarang;
        private Button btnKategori;
        private Button btnSupplier;
        private Button btnPenerimaan;
        private Button btnStokOpname;
        private Button btnSettings;
        private Button btnLaporan;
        private Button btnLabaRugi;
        private Button btnDashboard;
        private Button btnCetakLabel;
        private Button btnEod;
        private Button btnKasir;

        public MainForm()
        {
            InitializeComponent();
            OpenPage(new FormDashboard(), btnDashboard);
        }

        private void InitializeComponent()
        {
            this.Text = "simPOS Management";
            this.Size = new Size(1200, 700);
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.WhiteSmoke;

            // ⚠ Urutan PENTING: Fill → Left → Top
            BuildContentArea();
            BuildSidebar();
            BuildHeader();
        }

        private void BuildSidebar()
        {
            pnlMenu = new Panel
            {
                Dock = DockStyle.Left,
                Width = 180,
                BackColor = Color.FromArgb(44, 62, 80)
            };

            var lblLogo = new Label
            {
                Text = "sim POS",
                Dock = DockStyle.Top,
                Height = 60,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(36, 50, 65)
            };

            // ── Tombol buka POS (di atas section MANAJEMEN) ──────────────
            btnKasir = new Button
            {
                Text = "🖥  Buka Kasir / POS",
                Dock = DockStyle.Top,
                Height = 48,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnKasir.FlatAppearance.BorderSize = 0;
            btnKasir.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 150, 76);
            btnKasir.Click += (s, e) => LaunchPOS();

            var sepKasir = new Label
            {
                Dock = DockStyle.Top,
                Height = 8,
                BackColor = Color.FromArgb(36, 50, 65)
            };

            var lblMenuHeader = new Label
            {
                Text = "MANAJEMEN",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(127, 140, 141),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            };

            btnDashboard = CreateMenuButton("🏠  Dashboard", () => OpenPage(new FormDashboard(), btnDashboard));
            btnLaporan = CreateMenuButton("📊  Laporan", () => OpenPage(new FormLaporan(), btnLaporan));
            btnLabaRugi = CreateMenuButton("💰  Laba / Rugi", () => OpenPage(new FormProfitLoss(), btnLabaRugi));
            btnEod = CreateMenuButton("📋  EOD", () => OpenEod());
            btnCetakLabel = CreateMenuButton("🏷  Cetak Label", () => OpenPage(new simPOS.Management.Forms.Products.FormPrintLabel(), btnCetakLabel));

            var sepLaporan = new Label
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(60, 80, 100)
            };

            btnBarang = CreateMenuButton("📦  Barang", () => OpenPage(new FormProductList(), btnBarang));
            btnKategori = CreateMenuButton("🏷  Kategori", () => OpenPage(new FormCategoryList(), btnKategori));
            btnSupplier = CreateMenuButton("🏢  Supplier", () => OpenPage(new FormSupplierList(), btnSupplier));

            // Pemisah sebelum fitur yang belum ada
            btnPenerimaan = CreateMenuButton("📥  Terima Barang", () => OpenPage(new FormGoodsReceiptList(), btnPenerimaan));
            btnStokOpname = CreateMenuButton("📋  Stok Opname", () => OpenPage(new FormStockOpnameList(), btnStokOpname));

            // Urutan terbalik karena DockStyle.Top menumpuk dari bawah ke atas
            pnlMenu.Controls.Add(btnStokOpname);
            pnlMenu.Controls.Add(btnPenerimaan);
            pnlMenu.Controls.Add(btnSupplier);
            pnlMenu.Controls.Add(btnKategori);
            pnlMenu.Controls.Add(btnBarang);
            pnlMenu.Controls.Add(lblMenuHeader);
            pnlMenu.Controls.Add(sepLaporan);
            pnlMenu.Controls.Add(btnEod);
            pnlMenu.Controls.Add(btnCetakLabel);
            pnlMenu.Controls.Add(btnLabaRugi);
            pnlMenu.Controls.Add(btnLaporan);
            pnlMenu.Controls.Add(btnDashboard);
            pnlMenu.Controls.Add(sepKasir);
            pnlMenu.Controls.Add(btnKasir);
            pnlMenu.Controls.Add(lblLogo);

            // Tombol pengaturan di paling bawah sidebar
            btnSettings = new Button
            {
                Text = "⚙  Pengaturan",
                Dock = DockStyle.Bottom,
                Height = 42,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(36, 50, 65),
                ForeColor = Color.FromArgb(149, 165, 166),
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            btnSettings.FlatAppearance.BorderSize = 0;
            btnSettings.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 73, 94);
            //btnSettings.FlatAppearance.MouseOverForeColor = Color.White;
            btnSettings.Click += (s, e) =>
            {
                var form = new FormSettings();
                form.ShowDialog(this);
            };
            pnlMenu.Controls.Add(btnSettings);

            this.Controls.Add(pnlMenu);
        }

        private void BuildHeader()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Color.White
            };
            header.Paint += (s, e) => e.Graphics.DrawLine(
                new System.Drawing.Pen(Color.FromArgb(220, 220, 220)),
                0, header.Height - 1, header.Width, header.Height - 1);

            lblTitle = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0),
                Text = "Manajemen Barang"
            };

            header.Controls.Add(lblTitle);
            this.Controls.Add(header);
        }

        private void BuildContentArea()
        {
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.WhiteSmoke,
                Padding = new Padding(0)
            };
            this.Controls.Add(pnlContent);
        }

        // ── Navigasi ─────────────────────────────────────────────────────

        private void OpenPage(Form childForm, Button menuButton)
        {
            foreach (Control c in pnlContent.Controls)
                if (c is Form f) { f.Hide(); f.Dispose(); }
            pnlContent.Controls.Clear();

            lblTitle.Text = childForm.Text.Replace("simPOS — ", "");

            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;
            childForm.Visible = true;
            childForm.StartPosition = FormStartPosition.Manual;

            pnlContent.Controls.Add(childForm);
            childForm.BringToFront();

            UpdateActiveMenu(menuButton);
        }

        private void UpdateActiveMenu(Button active)
        {
            if (_activeMenuButton != null)
            {
                _activeMenuButton.BackColor = Color.FromArgb(44, 62, 80);
                _activeMenuButton.ForeColor = Color.FromArgb(200, 200, 200);
            }
            _activeMenuButton = active;
            _activeMenuButton.BackColor = Color.FromArgb(52, 152, 219);
            _activeMenuButton.ForeColor = Color.White;
        }

        // ── Helper ───────────────────────────────────────────────────────

        private void LaunchPOS()
        {
            // Cari apakah simPOS.POS.exe sudah berjalan
            var procs = System.Diagnostics.Process.GetProcessesByName("simPOS.POS");
            if (procs.Length > 0)
            {
                // Sudah berjalan — bawa ke depan
                var hWnd = procs[0].MainWindowHandle;
                if (hWnd != System.IntPtr.Zero)
                {
                    NativeMethods.ShowWindow(hWnd, 9);   // SW_RESTORE
                    NativeMethods.SetForegroundWindow(hWnd);
                }
                return;
            }

            // Cari exe di folder yang sama dengan Management
            var exeDir = System.IO.Path.GetDirectoryName(
                              System.Reflection.Assembly.GetExecutingAssembly().Location);
            var posExe = System.IO.Path.Combine(exeDir, "simPOS.POS.exe");

            if (!System.IO.File.Exists(posExe))
            {
                MessageBox.Show(
                    $"File simPOS.POS.exe tidak ditemukan. Pastikan file berada di folder yang sama:{ exeDir}                ",
                    "POS Tidak Ditemukan",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            System.Diagnostics.Process.Start(posExe);
        }

        private static Button CreateMenuButton(string text, Action onClick, bool disabled = false)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 44,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(44, 62, 80),
                ForeColor = disabled ? Color.FromArgb(80, 95, 110) : Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 9.5f),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0),
                Cursor = disabled ? Cursors.Default : Cursors.Hand,
                Enabled = !disabled
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = disabled
                ? Color.FromArgb(44, 62, 80)
                : Color.FromArgb(52, 73, 94);

            if (onClick != null)
                btn.Click += (s, e) => onClick();

            return btn;
        }
        private void OpenEod()
        {
            var clerk = new ClerkService();
            if (clerk.IsSessionOpen())
            {
                MessageBox.Show(
                    "Kasir di aplikasi POS masih BUKA." +  "Minta kasir tutup sesi (tekan F9 di POS) sebelum EOD bisa dilakukan.",
                    "EOD Tidak Bisa Dilakukan",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            OpenPage(new FormEod(), btnEod);
        }
    }
}
