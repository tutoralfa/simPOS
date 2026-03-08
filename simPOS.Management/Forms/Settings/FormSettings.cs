using simPOS.Shared.Database;
using simPOS.Shared.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace simPOS.Management.Forms.Settings
{
    /// <summary>
    /// Form pengaturan terpadu:
    /// - Tab Printer   : FormPrinterSettings (embed)
    /// - Tab Database  : FormDatabaseManager (embed)
    /// - Tab Export/Import Barang : CSV
    /// </summary>
    public class FormSettings : Form
    {
        private TabControl _tabs;

        public FormSettings()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Pengaturan";
            this.Size = new Size(820, 640);
            this.MinimumSize = new Size(700, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            // Header
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(44, 62, 80)
            };
            header.Controls.Add(new Label
            {
                Text = "⚙  Pengaturan",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            });

            // TabControl
            _tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f),
                Padding = new Point(16, 6)
            };

            _tabs.TabPages.Add(BuildPrinterTab());
            _tabs.TabPages.Add(BuildDatabaseTab());
            _tabs.TabPages.Add(BuildCsvTab());

            this.Controls.Add(_tabs);
            this.Controls.Add(header);
        }

        // ══════════════════════════════════════════════════════════════
        // TAB 1 — PRINTER
        // ══════════════════════════════════════════════════════════════

        private TabPage BuildPrinterTab()
        {
            var tab = new TabPage("🖨  Printer Struk")
            {
                BackColor = Color.White,
                Padding = new Padding(0)
            };

            // Embed FormPrinterSettings sebagai panel (tanpa titlebar)
            var inner = new FormPrinterSettings();
            inner.TopLevel = false;
            inner.FormBorderStyle = FormBorderStyle.None;
            inner.Dock = DockStyle.Fill;
            inner.Visible = true;
            tab.Controls.Add(inner);

            return tab;
        }

        // ══════════════════════════════════════════════════════════════
        // TAB 2 — DATABASE
        // ══════════════════════════════════════════════════════════════

        private TabPage BuildDatabaseTab()
        {
            var tab = new TabPage("🗄  Database")
            {
                BackColor = Color.White,
                Padding = new Padding(0)
            };

            var inner = new FormDatabaseManager();
            inner.TopLevel = false;
            inner.FormBorderStyle = FormBorderStyle.None;
            inner.Dock = DockStyle.Fill;
            inner.Visible = true;
            tab.Controls.Add(inner);

            return tab;
        }

        // ══════════════════════════════════════════════════════════════
        // TAB 3 — EXPORT / IMPORT BARANG (CSV)
        // ══════════════════════════════════════════════════════════════

        private TabPage BuildCsvTab()
        {
            var tab = new TabPage("📦  Export / Import Barang")
            {
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(0)
            };
            tab.Controls.Add(BuildCsvPanel());
            return tab;
        }

        private Panel BuildCsvPanel()
        {
            var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 16, 20, 16) };

            // ── Export section ────────────────────────────────────
            var pnlExport = MakeSection("📤  Export Data Barang ke CSV");

            var lblExportInfo = new Label
            {
                Text = "Export seluruh data barang (kode, nama, harga, stok, kategori, supplier) ke file CSV.\n" +
                             "File CSV bisa dibuka di Microsoft Excel atau Google Sheets.",
                Dock = DockStyle.Top,
                Height = 44,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(80, 80, 80),
                Padding = new Padding(2, 4, 0, 0)
            };

            var pnlExportBtns = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.Transparent };

            var btnExport = MakeBtn("📤  Export Sekarang", Color.FromArgb(39, 174, 96), 180);
            var btnTemplate = MakeBtn("📋  Unduh Template", Color.FromArgb(52, 152, 219), 170);

            btnExport.Location = new Point(0, 4);
            btnTemplate.Location = new Point(188, 4);

            btnExport.Click += BtnExport_Click;
            btnTemplate.Click += BtnTemplate_Click;

            pnlExportBtns.Controls.AddRange(new Control[] { btnExport, btnTemplate });

            pnlExport.Controls.Add(pnlExportBtns);
            pnlExport.Controls.Add(lblExportInfo);

            // ── Import section ────────────────────────────────────
            var pnlImport = MakeSection("📥  Import Data Barang dari CSV");

            var lblImportInfo = new Label
            {
                Text = "Import barang dari file CSV. Gunakan template di atas sebagai panduan format kolom.\n" +
                             "Kategori & Supplier yang belum ada akan dibuat otomatis.",
                Dock = DockStyle.Top,
                Height = 44,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(80, 80, 80),
                Padding = new Padding(2, 4, 0, 0)
            };

            // Mode import
            var pnlMode = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.Transparent };

            var rbInsert = new RadioButton
            {
                Text = "Tambah Baru Saja — skip barang yang kodenya sudah ada",
                Location = new Point(2, 4),
                Size = new Size(460, 22),
                Font = new Font("Segoe UI", 8.5f),
                Checked = true
            };
            var rbUpsert = new RadioButton
            {
                Text = "Tambah & Update — barang yang kodenya sudah ada akan di-update",
                Location = new Point(2, 30),
                Size = new Size(460, 22),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(150, 80, 0)
            };
            pnlMode.Controls.AddRange(new Control[] { rbInsert, rbUpsert });

            var pnlImportBtns = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.Transparent };
            var btnImport = MakeBtn("📥  Pilih File & Import", Color.FromArgb(142, 68, 173), 200);
            btnImport.Location = new Point(0, 4);
            btnImport.Click += (s, e) => BtnImport_Click(rbUpsert.Checked);
            pnlImportBtns.Controls.Add(btnImport);

            pnlImport.Controls.Add(pnlImportBtns);
            pnlImport.Controls.Add(pnlMode);
            pnlImport.Controls.Add(lblImportInfo);

            // ── Format CSV info ───────────────────────────────────
            var pnlFormat = MakeSection("📋  Format Kolom CSV");

            var dgvCols = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 8.5f),
                ColumnHeadersHeight = 28,
                RowTemplate = { Height = 24 },
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            dgvCols.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgvCols.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvCols.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            dgvCols.EnableHeadersVisualStyles = false;

            dgvCols.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kolom", Width = 110 });
            dgvCols.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Keterangan", Width = 260 });
            dgvCols.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Contoh", Width = 130 });
            dgvCols.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Wajib", Width = 55 });
            dgvCols.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            var colInfo = new[]
            {
                ("Kode",        "Kode unik barang (tidak boleh duplikat)",      "BRG001",    "✅"),
                ("Nama",        "Nama barang",                                   "Beras 5kg", "✅"),
                ("Deskripsi",   "Keterangan tambahan (boleh kosong)",            "",          ""),
                ("Satuan",      "Satuan barang",                                 "kg / pcs",  ""),
                ("Kategori",    "Nama kategori (buat otomatis jika belum ada)",  "Sembako",   ""),
                ("Supplier",    "Nama supplier (buat otomatis jika belum ada)",  "CV Maju",   ""),
                ("HargaBeli",   "Harga beli / HPP (angka, tanpa titik/koma)",   "10000",     "✅"),
                ("HargaJual",   "Harga jual (angka, tanpa titik/koma)",          "15000",     "✅"),
                ("Stok",        "Jumlah stok awal",                              "100",       ""),
                ("StokMinimum", "Batas stok minimum untuk alert",                "10",        ""),
                ("Aktif",       "1 = aktif, 0 = nonaktif",                       "1",         ""),
            };

            foreach (var (col, ket, contoh, wajib) in colInfo)
            {
                var idx = dgvCols.Rows.Add(col, ket, contoh, wajib);
                if (wajib == "✅")
                    dgvCols.Rows[idx].Cells[0].Style.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            }
            dgvCols.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

            pnlFormat.Controls.Add(dgvCols);

            // Status bar bawah
            var lblStatus = new Label
            {
                Name = "lblCsvStatus",
                Dock = DockStyle.Bottom,
                Height = 26,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0),
                BackColor = Color.FromArgb(245, 245, 245)
            };
            // Simpan ref ke status bar agar bisa diakses dari event
            _lblCsvStatus = lblStatus;

            // Susun layout — urutan Controls.Add = bawah ke atas (karena DockStyle.Top)
            root.Controls.Add(pnlFormat);
            root.Controls.Add(pnlImport);
            root.Controls.Add(pnlExport);
            root.Controls.Add(lblStatus);
            return root;
        }

        // Status label ref (diakses oleh event handler)
        private Label _lblCsvStatus;

        // ══════════════════════════════════════════════════════════════
        // CSV EVENTS
        // ══════════════════════════════════════════════════════════════

        private void BtnExport_Click(object sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Export Data Barang",
                Filter = "CSV File (*.csv)|*.csv",
                FileName = $"barang_export_{DateTime.Today:yyyyMMdd}.csv",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                this.Cursor = Cursors.WaitCursor;
                var svc = new ProductCsvService();
                var path = svc.ExportToCsv(dlg.FileName);
                this.Cursor = Cursors.Default;

                SetCsvStatus($"✔ Export berhasil → {Path.GetFileName(path)}", success: true);

                if (MessageBox.Show(
                    $"Export berhasil!\n{path}\n\nBuka file sekarang?",
                    "Export Selesai", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information) == DialogResult.Yes)
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                SetCsvStatus($"⚠ Export gagal: {ex.Message}", success: false);
                MessageBox.Show($"Export gagal:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnTemplate_Click(object sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Simpan Template CSV",
                Filter = "CSV File (*.csv)|*.csv",
                FileName = "template_barang.csv",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                new ProductCsvService().ExportTemplate(dlg.FileName);
                SetCsvStatus($"✔ Template disimpan: {Path.GetFileName(dlg.FileName)}", success: true);

                if (MessageBox.Show("Template disimpan!\n\nBuka file sekarang?",
                    "Template", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                SetCsvStatus($"⚠ {ex.Message}", success: false);
            }
        }

        private void BtnImport_Click(bool upsert)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Pilih File CSV",
                Filter = "CSV File (*.csv)|*.csv",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            string modeLabel = upsert ? "Tambah & Update" : "Tambah Baru Saja";
            if (MessageBox.Show(
                $"Import barang dari:\n{Path.GetFileName(dlg.FileName)}\n\nMode: {modeLabel}\n\nLanjutkan?",
                "Konfirmasi Import", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                this.Cursor = Cursors.WaitCursor;
                var result = new ProductCsvService().ImportFromCsv(dlg.FileName, upsert);
                this.Cursor = Cursors.Default;

                string summary =
                    $"Import selesai!\n\n" +
                    $"✅ Ditambahkan  : {result.Inserted} barang\n" +
                    $"🔄 Di-update    : {result.Updated} barang\n" +
                    $"⏭ Dilewati     : {result.Skipped} baris";

                if (result.HasErrors)
                    summary += $"\n⚠ Error        : {result.Errors.Count} baris\n\n" +
                               string.Join("\n", result.Errors.GetRange(0, Math.Min(5, result.Errors.Count)));

                SetCsvStatus(
                    $"✔ Import: +{result.Inserted} baru, ~{result.Updated} update, " +
                    $"{result.Skipped} skip, {result.Errors.Count} error",
                    success: !result.HasErrors);

                MessageBox.Show(summary, "Hasil Import",
                    MessageBoxButtons.OK,
                    result.HasErrors ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                SetCsvStatus($"⚠ Import gagal: {ex.Message}", success: false);
                MessageBox.Show($"Import gagal:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // UI HELPERS
        // ══════════════════════════════════════════════════════════════

        private void SetCsvStatus(string msg, bool success)
        {
            if (_lblCsvStatus == null) return;
            _lblCsvStatus.Text = "  " + msg;
            _lblCsvStatus.ForeColor = success
                ? Color.FromArgb(39, 120, 70)
                : Color.FromArgb(192, 57, 43);
        }

        private static Panel MakeSection(string title)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top,
                BackColor = Color.White,
                Padding = new Padding(12, 8, 12, 10),
                Margin = new Padding(0, 0, 0, 8)
            };
            pnl.Height = 110; // akan di-override oleh konten

            var lblTitle = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                BackColor = Color.FromArgb(245, 248, 250),
                Padding = new Padding(4, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnl.Controls.Add(lblTitle);

            // Garis bawah
            pnl.Paint += (s, e) =>
            {
                var p2 = s as Panel;
                e.Graphics.DrawRectangle(new Pen(Color.FromArgb(218, 220, 224)),
                    0, 0, p2.Width - 1, p2.Height - 1);
            };
            return pnl;
        }

        private static Button MakeBtn(string text, Color color, int width)
        {
            var btn = new Button
            {
                Text = text,
                Width = width,
                Height = 30,
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
}
