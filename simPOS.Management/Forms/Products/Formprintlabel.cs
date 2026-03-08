using simPOS.Management.Printing;
using simPOS.Shared.Models;
using simPOS.Shared.Printing;
using simPOS.Shared.Repositories;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms;

namespace simPOS.Management.Forms.Products
{
    public class FormPrintLabel : Form
    {
        // ── Data ──────────────────────────────────────────────────────
        private readonly ProductRepository _repo = new();
        private readonly List<Product> _allProducts = new();
        private readonly List<LabelItem> _queue = new();   // antrian cetak
        private LabelSettings _settings = LabelSettings.Default;
        private List<List<LabelItem>> _pages = new();
        private int _previewPage = 0;

        // ── Controls ──────────────────────────────────────────────────
        private TextBox txtSearch;
        private DataGridView dgvAll;        // semua produk
        private DataGridView dgvQueue;      // antrian cetak
        private Panel pnlPreview;
        private Label lblPageInfo;

        // Settings controls
        private NumericUpDown numLabelW, numLabelH, numCols,
                              numFontName, numFontPrice, numBarH,
                              numMarginH, numMarginV, numFontPriceLarge;
        private NumericUpDown numModuleW;  // lebar modul barcode
        private ComboBox cmbBarcodeType;
        private TextBox txtStoreName;
        private CheckBox chkStoreName;

        // ── Konstruktor mandiri (tanpa parameter wajib) ───────────────
        public FormPrintLabel() : this(null) { }

        public FormPrintLabel(IEnumerable<Product> preselected)
        {
            InitUI();
            this.Load += (s, e) =>
            {
                LoadAllProducts();
                if (preselected != null)
                    foreach (var p in preselected)
                        AddToQueue(p, 1);
                RebuildAndRefresh();
            };
        }

        // ══════════════════════════════════════════════════════════════
        // LAYOUT  —  3 kolom: pilih | antrian | preview
        // ══════════════════════════════════════════════════════════════

        private void InitUI()
        {
            Text = "🏷  Cetak Label Harga & Barcode";
            Size = new Size(1100, 680);
            MinimumSize = new Size(900, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(245, 246, 250);

            var header = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.FromArgb(44, 62, 80) };
            header.Controls.Add(new Label
            {
                Text = "🏷  Cetak Label Harga & Barcode",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            });

            // TableLayoutPanel 3 kolom — tidak ada SplitterDistance issue
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.FromArgb(245, 246, 250)
            };
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

            tbl.Controls.Add(BuildPickPanel(), 0, 0);
            tbl.Controls.Add(BuildQueuePanel(), 1, 0);
            tbl.Controls.Add(BuildPreviewPanel(), 2, 0);

            Controls.Add(tbl);
            Controls.Add(header);
        }

        // ── Kolom 1: Pilih Barang ─────────────────────────────────────

        private Panel BuildPickPanel()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 8, 4, 10) };

            var title = new Label
            {
                Text = "📦  Pilih Barang",
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80)
            };

            // Search box
            var pnlSearch = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = Color.Transparent };
            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f),
                PlaceholderText = "Cari nama / kode barang..."
            };
            txtSearch.TextChanged += (s, e) => FilterProducts();
            pnlSearch.Controls.Add(txtSearch);

            // Grid semua produk
            dgvAll = MakeGrid(false);
            dgvAll.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "Kode", Width = 90, ReadOnly = true });
            dgvAll.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Nama Barang", ReadOnly = true });
            dgvAll.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice", HeaderText = "Harga", Width = 80, ReadOnly = true });
            dgvAll.Columns["colName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvAll.Columns["colPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvAll.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) AddSelectedToQueue(); };

            // Tombol tambah
            var pnlBtns = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = Color.Transparent };
            var btnAdd = MakeBtn("➕  Tambah ke Antrian", Color.FromArgb(39, 174, 96), 190);
            var btnAddAll = MakeBtn("➕  Semua", Color.FromArgb(52, 152, 219), 90);
            btnAdd.Location = new Point(0, 4);
            btnAddAll.Location = new Point(196, 4);
            btnAdd.Click += (s, e) => AddSelectedToQueue();
            btnAddAll.Click += (s, e) =>
            {
                foreach (DataGridViewRow r in dgvAll.Rows)
                    if (r.Tag is Product p) AddToQueue(p, 1);
                RebuildAndRefresh();
            };
            pnlBtns.Controls.AddRange(new Control[] { btnAdd, btnAddAll });

            var sp = new Panel { Dock = DockStyle.Top, Height = 6, BackColor = Color.Transparent };
            pnl.Controls.Add(pnlBtns);
            pnl.Controls.Add(dgvAll);
            pnl.Controls.Add(sp);
            pnl.Controls.Add(pnlSearch);
            pnl.Controls.Add(title);
            return pnl;
        }

        // ── Kolom 2: Antrian + Settings ───────────────────────────────

        private Panel BuildQueuePanel()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4, 8, 4, 10) };

            // Antrian cetak
            var titleQ = new Label
            {
                Text = "🖨  Antrian Cetak",
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80)
            };

            dgvQueue = MakeGrid(false);
            dgvQueue.ReadOnly = false;
            dgvQueue.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Nama Barang", ReadOnly = true });
            dgvQueue.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty", HeaderText = "Qty", Width = 50 });
            dgvQueue.Columns["colName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvQueue.Columns["colQty"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Kolom hapus
            var colDel = new DataGridViewButtonColumn
            {
                Name = "colDel",
                HeaderText = "",
                Text = "✕",
                UseColumnTextForButtonValue = true,
                Width = 28
            };
            colDel.DefaultCellStyle.ForeColor = Color.FromArgb(192, 57, 43);
            colDel.DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            colDel.DefaultCellStyle.BackColor = Color.White;
            colDel.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvQueue.Columns.Add(colDel);

            dgvQueue.CellValueChanged += DgvQueue_CellValueChanged;
            dgvQueue.CellClick += DgvQueue_CellClick;
            dgvQueue.Height = 160;

            var pnlQBtns = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = Color.Transparent };
            var btnClear = MakeBtn("🗑  Kosongkan", Color.FromArgb(192, 57, 43), 130);
            btnClear.Location = new Point(0, 2);
            btnClear.Click += (s, e) => { _queue.Clear(); RefreshQueueGrid(); RebuildAndRefresh(); };
            pnlQBtns.Controls.Add(btnClear);

            // Settings label
            var titleS = new Label
            {
                Text = "⚙  Pengaturan Label",
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Padding = new Padding(0, 8, 0, 0)
            };

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));

            numLabelW = MakeNum(20, 200, _settings.LabelWidthMm);
            numLabelH = MakeNum(10, 100, _settings.LabelHeightMm);
            numCols = MakeNum(1, 10, _settings.Columns);
            numFontName = MakeNum(2, 20, _settings.FontNameSize);
            numFontPrice = MakeNum(2, 24, _settings.FontPriceSize);
            numBarH = MakeNum(2, 60, _settings.BarcodeHeightMm);
            numMarginH = MakeNum(0, 20, _settings.MarginHorizontalMm);
            numMarginV = MakeNum(0, 20, _settings.MarginVerticalMm);
            numFontPriceLarge = MakeNum(2, 36, _settings.FontPriceLargeSize);
            numModuleW = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 5,
                DecimalPlaces = 1,
                Increment = 0.5m,
                Value = (decimal)_settings.BarcodeModuleWidth,
                Font = new Font("Segoe UI", 8f)
            };
            cmbBarcodeType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8f)
            };
            foreach (BarcodeType bt in Enum.GetValues(typeof(BarcodeType)))
                cmbBarcodeType.Items.Add(BarcodeRenderer.DisplayName(bt));
            cmbBarcodeType.SelectedIndex = (int)_settings.BarcodeType;
            txtStoreName = new TextBox { Text = _settings.StoreName, Font = new Font("Segoe UI", 8f) };
            chkStoreName = new CheckBox { Text = "Tampilkan nama toko", Checked = _settings.ShowStoreName, Font = new Font("Segoe UI", 8f), AutoSize = true };

            void AddRow(string lbl, Control ctrl)
            {
                ctrl.Dock = DockStyle.Fill;
                var l = new Label { Text = lbl, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8f), TextAlign = ContentAlignment.MiddleLeft };
                int r = tbl.RowCount++;
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 25f));
                tbl.Controls.Add(l, 0, r);
                tbl.Controls.Add(ctrl, 1, r);
            }

            AddRow("Lebar label (mm):", numLabelW);
            AddRow("Tinggi label (mm):", numLabelH);
            AddRow("Kolom per baris:", numCols);
            AddRow("Font nama (pt):", numFontName);
            AddRow("Font harga (pt):", numFontPrice);
            AddRow("Font harga besar (pt):", numFontPriceLarge);
            AddRow("Tinggi barcode (mm):", numBarH);
            AddRow("Margin kiri/kanan (mm):", numMarginH);
            AddRow("Margin atas/bawah (mm):", numMarginV);
            AddRow("Jenis barcode:", cmbBarcodeType);
            AddRow("Lebar modul barcode (px):", numModuleW);
            AddRow("Nama toko:", txtStoreName);

            numLabelW.ValueChanged += (s, e) => { _settings.LabelWidthMm = (int)numLabelW.Value; RebuildAndRefresh(); };
            numLabelH.ValueChanged += (s, e) => { _settings.LabelHeightMm = (int)numLabelH.Value; RebuildAndRefresh(); };
            numCols.ValueChanged += (s, e) => { _settings.Columns = (int)numCols.Value; RebuildAndRefresh(); };
            numFontName.ValueChanged += (s, e) => { _settings.FontNameSize = (int)numFontName.Value; Refresh_(); };
            numFontPrice.ValueChanged += (s, e) => { _settings.FontPriceSize = (int)numFontPrice.Value; Refresh_(); };
            numFontPriceLarge.ValueChanged += (s, e) => { _settings.FontPriceLargeSize = (int)numFontPriceLarge.Value; Refresh_(); };
            numBarH.ValueChanged += (s, e) => { _settings.BarcodeHeightMm = (int)numBarH.Value; Refresh_(); };
            numMarginH.ValueChanged += (s, e) => { _settings.MarginHorizontalMm = (int)numMarginH.Value; Refresh_(); };
            numMarginV.ValueChanged += (s, e) => { _settings.MarginVerticalMm = (int)numMarginV.Value; Refresh_(); };
            numModuleW.ValueChanged += (s, e) => { _settings.BarcodeModuleWidth = (float)numModuleW.Value; Refresh_(); };
            cmbBarcodeType.SelectedIndexChanged += (s, e) =>
            {
                if (cmbBarcodeType.SelectedIndex >= 0)
                    _settings.BarcodeType = (BarcodeType)cmbBarcodeType.SelectedIndex;
                Refresh_();
            };
            txtStoreName.TextChanged += (s, e) => { _settings.StoreName = txtStoreName.Text; Refresh_(); };
            chkStoreName.CheckedChanged += (s, e) => { _settings.ShowStoreName = chkStoreName.Checked; Refresh_(); };

            // Tombol cetak
            var pnlPrint = new Panel { Dock = DockStyle.Bottom, Height = 42, BackColor = Color.Transparent };
            var btnPrint = MakeBtn("🖨  Cetak Label", Color.FromArgb(39, 174, 96), 160);
            btnPrint.Dock = DockStyle.Right;
            btnPrint.Width = 160;
            btnPrint.Height = 34;
            btnPrint.Click += BtnPrint_Click;
            pnlPrint.Controls.Add(btnPrint);

            var sp1 = new Panel { Dock = DockStyle.Top, Height = 4, BackColor = Color.Transparent };
            var sp2 = new Panel { Dock = DockStyle.Top, Height = 4, BackColor = Color.Transparent };

            pnl.Controls.Add(pnlPrint);
            pnl.Controls.Add(chkStoreName);
            pnl.Controls.Add(tbl);
            pnl.Controls.Add(titleS);
            pnl.Controls.Add(sp2);
            pnl.Controls.Add(pnlQBtns);
            pnl.Controls.Add(dgvQueue);
            pnl.Controls.Add(sp1);
            pnl.Controls.Add(titleQ);
            return pnl;
        }

        // ── Kolom 3: Preview ──────────────────────────────────────────

        private Panel BuildPreviewPanel()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4, 8, 10, 10) };

            var titleP = new Label
            {
                Text = "👁  Preview",
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80)
            };

            var pnlNav = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = Color.Transparent };
            var btnPrev = new Button { Text = "◀", Width = 28, Height = 24, Location = new Point(0, 2), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f) };
            var btnNext = new Button { Text = "▶", Width = 28, Height = 24, Location = new Point(30, 2), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f) };
            lblPageInfo = new Label { Text = "Hal 1/1", Location = new Point(64, 6), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            btnPrev.FlatAppearance.BorderSize = 0;
            btnNext.FlatAppearance.BorderSize = 0;
            btnPrev.Click += (s, e) => { if (_previewPage > 0) { _previewPage--; Refresh_(); } };
            btnNext.Click += (s, e) => { if (_previewPage < _pages.Count - 1) { _previewPage++; Refresh_(); } };
            pnlNav.Controls.AddRange(new Control[] { btnPrev, btnNext, lblPageInfo });

            pnlPreview = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(160, 160, 160) };
            pnlPreview.Paint += Preview_Paint;
            pnlPreview.Resize += (s, e) => pnlPreview.Invalidate();

            pnl.Controls.Add(pnlPreview);
            pnl.Controls.Add(pnlNav);
            pnl.Controls.Add(titleP);
            return pnl;
        }

        // ══════════════════════════════════════════════════════════════
        // DATA
        // ══════════════════════════════════════════════════════════════

        private void LoadAllProducts()
        {
            _allProducts.Clear();
            _allProducts.AddRange(_repo.GetAll(includeInactive: false));
            FillProductGrid(_allProducts);
        }

        private void FillProductGrid(IEnumerable<Product> products)
        {
            dgvAll.Rows.Clear();
            foreach (var p in products)
            {
                int idx = dgvAll.Rows.Add(p.Code, p.Name, $"Rp {p.SellPrice:N0}");
                dgvAll.Rows[idx].Tag = p;
            }
        }

        private void FilterProducts()
        {
            string kw = txtSearch.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(kw)) { FillProductGrid(_allProducts); return; }
            FillProductGrid(_allProducts.Where(p =>
                p.Name.ToLower().Contains(kw) || p.Code.ToLower().Contains(kw)));
        }

        private void AddSelectedToQueue()
        {
            foreach (DataGridViewRow r in dgvAll.SelectedRows)
                if (r.Tag is Product p) AddToQueue(p, 1);
            RebuildAndRefresh();
        }

        private void AddToQueue(Product p, int qty)
        {
            var existing = _queue.FirstOrDefault(x => x.Product.Id == p.Id);
            if (existing != null) existing.Copies += qty;
            else _queue.Add(new LabelItem { Product = p, Copies = qty });
            RefreshQueueGrid();
        }

        private void RefreshQueueGrid()
        {
            dgvQueue.Rows.Clear();
            foreach (var item in _queue)
            {
                int idx = dgvQueue.Rows.Add(item.Product.Name, item.Copies, "✕");
                dgvQueue.Rows[idx].Tag = item;
            }
        }

        private void DgvQueue_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvQueue.Columns[e.ColumnIndex].Name != "colQty") return;
            if (dgvQueue.Rows[e.RowIndex].Tag is LabelItem item &&
                int.TryParse(dgvQueue.Rows[e.RowIndex].Cells["colQty"].Value?.ToString(), out int v))
                item.Copies = Math.Max(1, v);
            RebuildAndRefresh();
        }

        private void DgvQueue_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvQueue.Columns[e.ColumnIndex].Name != "colDel") return;
            if (dgvQueue.Rows[e.RowIndex].Tag is LabelItem item)
            {
                _queue.Remove(item);
                RefreshQueueGrid();
                RebuildAndRefresh();
            }
        }

        // ══════════════════════════════════════════════════════════════
        // PREVIEW
        // ══════════════════════════════════════════════════════════════

        private void RebuildAndRefresh()
        {
            _pages.Clear();
            var flat = _queue.SelectMany(item =>
                Enumerable.Repeat(item, Math.Max(1, item.Copies))).ToList();

            float pageHmm = 297f;
            int labelsPerRow = Math.Max(1, _settings.Columns);
            int labelsPerCol = Math.Max(1, (int)(pageHmm / _settings.LabelHeightMm));
            int perPage = labelsPerRow * labelsPerCol;

            for (int i = 0; i < flat.Count; i += perPage)
                _pages.Add(flat.GetRange(i, Math.Min(perPage, flat.Count - i)));

            if (_pages.Count == 0) _pages.Add(new List<LabelItem>());
            _previewPage = Math.Min(_previewPage, _pages.Count - 1);
            Refresh_();
        }

        private void Refresh_()
        {
            if (pnlPreview == null) return;
            if (lblPageInfo != null)
                lblPageInfo.Text = $"Hal {_previewPage + 1}/{Math.Max(1, _pages.Count)}";
            pnlPreview.Invalidate();
        }

        private void Preview_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var area = pnlPreview.ClientRectangle;
            g.Clear(Color.FromArgb(160, 160, 160));

            if (_pages.Count == 0 || _pages[_previewPage].Count == 0)
            {
                using var fEmpty = new Font("Segoe UI", 10f);
                g.DrawString("Tambah barang ke antrian cetak",
                    fEmpty, Brushes.White,
                    new RectangleF(0, 0, area.Width, area.Height),
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                return;
            }

            float pageWmm = 210f, pageHmm = 297f;
            float margin = 12f;
            float scaleX = (area.Width - margin * 2) / pageWmm;
            float scaleY = (area.Height - margin * 2) / pageHmm;
            float scale = Math.Min(scaleX, scaleY);

            float pageWpx = pageWmm * scale;
            float pageHpx = pageHmm * scale;
            float ox = (area.Width - pageWpx) / 2f;
            float oy = (area.Height - pageHpx) / 2f;

            g.FillRectangle(Brushes.White, ox, oy, pageWpx, pageHpx);
            g.DrawRectangle(Pens.LightGray, ox, oy, pageWpx - 1, pageHpx - 1);

            float lw = _settings.LabelWidthMm * scale;
            float lh = _settings.LabelHeightMm * scale;
            int col = 0, row = 0;
            int cols = _settings.Columns;

            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            foreach (var item in _pages[_previewPage])
            {
                float lx = ox + col * lw;
                float ly = oy + row * lh;
                DrawLabel(g, item, new RectangleF(lx, ly, lw, lh), scale);

                using var pen = new Pen(Color.FromArgb(190, 210, 220)) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
                g.DrawRectangle(pen, lx, ly, lw - 1, lh - 1);

                col++;
                if (col >= cols) { col = 0; row++; }
            }
        }

        private void DrawLabel(Graphics g, LabelItem item, RectangleF rect, float scale)
        {
            // Margin dari settings (mm → px sesuai scale)
            float mh = _settings.MarginHorizontalMm * scale;
            float mv = _settings.MarginVerticalMm * scale;
            float inner = rect.Width - mh * 2;
            float x = rect.X + mh;
            float y = rect.Y + mv;
            var sfC = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };

            // Nama toko (opsional)
            if (_settings.ShowStoreName && !string.IsNullOrWhiteSpace(_settings.StoreName))
            {
                using var f = new Font("Segoe UI", Math.Max(4f, (_settings.FontNameSize - 2) * scale), FontStyle.Italic);
                float h = f.GetHeight(g) + 1f;
                g.DrawString(_settings.StoreName, f, Brushes.Gray, new RectangleF(x, y, inner, h), sfC);
                y += h;
            }

            // Nama barang
            using (var f = new Font("Segoe UI", Math.Max(4f, _settings.FontNameSize * scale), FontStyle.Bold))
            {
                float maxH = Math.Max(f.GetHeight(g) * 1.3f, 1f);
                g.DrawString(item.Product.Name, f, Brushes.Black, new RectangleF(x, y, inner, maxH), sfC);
                y += maxH + 1f;
            }

            // Barcode
            float barHpx = _settings.BarcodeHeightMm * scale;
            if (inner > 8 && barHpx > 6)
            {
                BarcodeRenderer.DrawTo(g, item.BarcodeText,
                    new RectangleF(x, y, inner, barHpx),
                    _settings.BarcodeType,
                    _settings.BarcodeModuleWidth,
                    showText: true);
                y += barHpx + 2f;
            }

            // Harga — dua baris: kecil (font normal) + besar (font harga besar)
            using (var fSmall = new Font("Segoe UI", Math.Max(4f, _settings.FontPriceSize * scale)))
            {
                float hs = fSmall.GetHeight(g);
                g.DrawString("Harga Jual", fSmall, Brushes.Gray, new RectangleF(x, y, inner, hs), sfC);
                y += hs;
            }

            using (var fBig = new Font("Segoe UI", Math.Max(5f, _settings.FontPriceLargeSize * scale), FontStyle.Bold))
            {
                float hb = fBig.GetHeight(g) + 2f;
                g.DrawString($"Rp {item.Product.SellPrice:N0}", fBig,
                    new SolidBrush(Color.FromArgb(39, 120, 70)),
                    new RectangleF(x, y, inner, hb), sfC);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // PRINT
        // ══════════════════════════════════════════════════════════════

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (_queue.Count == 0)
            {
                MessageBox.Show("Antrian cetak kosong.\nTambahkan barang terlebih dahulu.",
                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            RebuildAndRefresh();
            int pageIndex = 0;

            var doc = new PrintDocument();
            doc.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);
            doc.PrintPage += (s2, pe) =>
            {
                float scale = 96f / 25.4f;
                int col = 0, row = 0;
                int cols = _settings.Columns;

                foreach (var item in _pages[pageIndex])
                {
                    float lx = pe.MarginBounds.Left + col * _settings.LabelWidthMm * scale;
                    float ly = pe.MarginBounds.Top + row * _settings.LabelHeightMm * scale;
                    // Saat print 96dpi → scale moduleW agar konsisten dengan preview
                    DrawLabel(pe.Graphics, item,
                        new RectangleF(lx, ly, _settings.LabelWidthMm * scale, _settings.LabelHeightMm * scale),
                        scale);
                    col++;
                    if (col >= cols) { col = 0; row++; }
                }

                pageIndex++;
                pe.HasMorePages = pageIndex < _pages.Count;
            };

            using var preview = new PrintPreviewDialog
            {
                Document = doc,
                Text = "Preview Cetak Label",
                WindowState = FormWindowState.Maximized,
                UseAntiAlias = true
            };
            preview.ShowDialog(this);
        }

        // ══════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════

        private static DataGridView MakeGrid(bool readOnly)
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = readOnly,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 8.5f),
                ColumnHeadersHeight = 26,
                RowTemplate = { Height = 24 },
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            return dgv;
        }

        private static NumericUpDown MakeNum(int min, int max, int val) =>
            new NumericUpDown { Minimum = min, Maximum = max, Value = val, Font = new Font("Segoe UI", 8f) };

        private static Button MakeBtn(string text, Color color, int w)
        {
            var btn = new Button
            {
                Text = text,
                Width = w,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(color, 0.1f);
            return btn;
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // MODELS
    // ══════════════════════════════════════════════════════════════════

    public class LabelItem
    {
        public Product Product { get; set; }
        public int Copies { get; set; } = 1;
        public string BarcodeText => string.IsNullOrWhiteSpace(Product.Barcode)
            ? Product.Code : Product.Barcode;
    }

    public class LabelSettings
    {
        public int LabelWidthMm { get; set; } = 40;
        public int LabelHeightMm { get; set; } = 25;
        public int Columns { get; set; } = 4;
        public int FontNameSize { get; set; } = 3;
        public int FontPriceSize { get; set; } = 3;
        public int BarcodeHeightMm { get; set; } = 13;
        public BarcodeType BarcodeType { get; set; } = BarcodeType.Code128;
        public float BarcodeModuleWidth { get; set; } = 0.5f;  // 0 = auto, >0 = px per modul
        public int MarginHorizontalMm { get; set; } = 2;   // margin kiri & kanan dalam label
        public int MarginVerticalMm { get; set; } = 2;   // margin atas & bawah dalam label
        public int FontPriceLargeSize { get; set; } = 5;  // ukuran font harga besar (bold)
        public bool ShowStoreName { get; set; } = false;
        public string StoreName { get; set; } = "simPOS";
        public static LabelSettings Default => new LabelSettings();
    }
}
