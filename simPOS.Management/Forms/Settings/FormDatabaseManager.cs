using simPOS.Shared.Database;
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
    public class FormDatabaseManager : Form
    {
        private readonly DatabaseBackupService _svc = new DatabaseBackupService();

        private DataGridView dgvBackups;
        private Label lblDbPath, lblDbSize;
        private Button btnBackup, btnRestore, btnOpenFolder;
        private Button btnResetTrx, btnResetAll;
        private Label lblStatus = new Label();

        public FormDatabaseManager()
        {
            InitializeComponent();
            LoadInfo();
            LoadBackupList();
        }

        private void InitializeComponent()
        {
            this.Text = "Manajemen Database";
            this.Size = new Size(720, 580);
            this.MinimumSize = new Size(640, 500);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            // ── Header ────────────────────────────────────────────
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(44, 62, 80)
            };
            header.Controls.Add(new Label
            {
                Text = "🗄  Manajemen Database",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            });

            // ── Info database ─────────────────────────────────────
            var pnlInfo = BuildInfoPanel();

            // ── Backup section ────────────────────────────────────
            var pnlBackup = BuildBackupPanel();

            // ── Reset section ─────────────────────────────────────
            var pnlReset = BuildResetPanel();

            // ── Status bar ────────────────────────────────────────
            lblStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            this.Controls.Add(pnlReset);
            this.Controls.Add(pnlBackup);
            this.Controls.Add(pnlInfo);
            this.Controls.Add(lblStatus);
            this.Controls.Add(header);
        }

        // ── Info panel ────────────────────────────────────────────

        private Panel BuildInfoPanel()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                Padding = new Padding(12, 8, 12, 4),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            pnl.Paint += (s, e) => e.Graphics.DrawLine(
                new Pen(Color.FromArgb(220, 220, 220)),
                0, pnl.Height - 1, pnl.Width, pnl.Height - 1);

            lblDbPath = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoEllipsis = true
            };
            lblDbSize = new Label
            {
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            pnl.Controls.Add(lblDbSize);
            pnl.Controls.Add(lblDbPath);
            return pnl;
        }

        // ── Backup panel ──────────────────────────────────────────

        private Panel BuildBackupPanel()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 8, 12, 4)
            };

            // Title
            var title = new Label
            {
                Text = "BACKUP & RESTORE",
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(120, 120, 120)
            };

            // Toolbar
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.Transparent };

            btnBackup = MakeBtn("💾 Buat Backup Sekarang", Color.FromArgb(39, 174, 96), 190);
            btnRestore = MakeBtn("📂 Restore dari File...", Color.FromArgb(52, 152, 219), 180);
            btnOpenFolder = MakeBtn("📁 Buka Folder Backup", Color.FromArgb(108, 122, 137), 170);

            btnBackup.Location = new Point(0, 3);
            btnRestore.Location = new Point(196, 3);
            btnOpenFolder.Location = new Point(382, 3);

            btnBackup.Click += BtnBackup_Click;
            btnRestore.Click += BtnRestore_Click;
            btnOpenFolder.Click += (s, e) =>
            {
                Directory.CreateDirectory(DatabaseBackupService.BackupFolder);
                Process.Start("explorer.exe", DatabaseBackupService.BackupFolder);
            };

            toolbar.Controls.AddRange(new Control[] { btnBackup, btnRestore, btnOpenFolder });

            // Grid daftar backup
            dgvBackups = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9f),
                ColumnHeadersHeight = 30,
                RowTemplate = { Height = 28 }
            };
            dgvBackups.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgvBackups.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvBackups.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgvBackups.EnableHeadersVisualStyles = false;
            dgvBackups.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
            dgvBackups.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvBackups.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

            foreach (var (name, hdr, w, align) in new[]
            {
                ("colNo",      "#",           36, DataGridViewContentAlignment.MiddleCenter),
                ("colFile",    "Nama File",   280, DataGridViewContentAlignment.MiddleLeft),
                ("colDate",    "Tanggal",     140, DataGridViewContentAlignment.MiddleCenter),
                ("colSize",    "Ukuran",       90, DataGridViewContentAlignment.MiddleRight),
                ("colAction",  "Aksi",         80, DataGridViewContentAlignment.MiddleCenter),
            })
            {
                var col = new DataGridViewTextBoxColumn { Name = name, HeaderText = hdr, Width = w };
                col.DefaultCellStyle.Alignment = align;
                dgvBackups.Columns.Add(col);
            }
            dgvBackups.Columns["colFile"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // Tombol restore di dalam grid (cell click)
            dgvBackups.CellClick += DgvBackups_CellClick;

            // Context menu: restore + delete
            var ctxMenu = new ContextMenuStrip();
            ctxMenu.Items.Add("📂 Restore backup ini", null, (s, e) => RestoreSelected());
            ctxMenu.Items.Add("🗑 Hapus backup ini", null, (s, e) => DeleteSelected());
            dgvBackups.ContextMenuStrip = ctxMenu;

            pnl.Controls.Add(dgvBackups);
            pnl.Controls.Add(toolbar);
            pnl.Controls.Add(title);
            return pnl;
        }

        // ── Reset panel ───────────────────────────────────────────

        private Panel BuildResetPanel()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                Padding = new Padding(12, 6, 12, 8),
                BackColor = Color.FromArgb(254, 245, 245)
            };
            pnl.Paint += (s, e) => e.Graphics.DrawLine(
                new Pen(Color.FromArgb(220, 180, 180)),
                0, 0, pnl.Width, 0);

            var title = new Label
            {
                Text = "⚠  RESET DATABASE  —  Tidak dapat dibatalkan! Backup otomatis dibuat sebelum reset.",
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 40, 40)
            };

            var pnlBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            btnResetTrx = MakeBtn("🗑 Reset Data Transaksi", Color.FromArgb(230, 126, 34), 200);
            btnResetAll = MakeBtn("☢ Reset SEMUA Data", Color.FromArgb(192, 57, 43), 170);

            btnResetTrx.Location = new Point(0, 10);
            btnResetAll.Location = new Point(208, 10);

            var lblHint = new Label
            {
                Text = "Reset Transaksi: hapus penjualan & riwayat stok, master data (produk, kategori) tetap ada.\n" +
                             "Reset Semua: kembalikan database ke kondisi fresh install.",
                Location = new Point(386, 8),
                Size = new Size(300, 40),
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(120, 60, 60)
            };

            btnResetTrx.Click += BtnResetTrx_Click;
            btnResetAll.Click += BtnResetAll_Click;

            pnlBtns.Controls.AddRange(new Control[] { btnResetTrx, btnResetAll, lblHint });
            pnl.Controls.Add(pnlBtns);
            pnl.Controls.Add(title);
            return pnl;
        }

        // ══════════════════════════════════════════════════════════════
        // LOAD DATA
        // ══════════════════════════════════════════════════════════════

        private void LoadInfo()
        {
            lblDbPath.Text = $"📂  {AppConfig.DatabasePath}";

            if (File.Exists(AppConfig.DatabasePath))
            {
                var fi = new FileInfo(AppConfig.DatabasePath);
                var size = fi.Length >= 1024 * 1024
                    ? $"{fi.Length / 1024.0 / 1024.0:N2} MB"
                    : $"{fi.Length / 1024.0:N1} KB";
                lblDbSize.Text = $"Ukuran: {size}   |   Terakhir diubah: {fi.LastWriteTime:dd/MM/yyyy HH:mm}";
            }
            else
            {
                lblDbSize.Text = "File database belum ada.";
            }
        }

        private void LoadBackupList()
        {
            dgvBackups.Rows.Clear();
            var list = _svc.GetBackupList();

            for (int i = 0; i < list.Length; i++)
            {
                var b = list[i];
                var rowIdx = dgvBackups.Rows.Add(
                    i + 1,
                    b.FileName,
                    b.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    b.SizeLabel,
                    "Restore");
                dgvBackups.Rows[rowIdx].Tag = b;
                dgvBackups.Rows[rowIdx].Cells["colAction"].Style.ForeColor = Color.FromArgb(52, 152, 219);
                dgvBackups.Rows[rowIdx].Cells["colAction"].Style.Font = new Font("Segoe UI", 9f, FontStyle.Underline);
            }

            if (list.Length == 0)
            {
                var rowIdx = dgvBackups.Rows.Add("", "Belum ada backup.", "", "", "");
                dgvBackups.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.Gray;
                dgvBackups.Rows[rowIdx].DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Italic);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // EVENTS
        // ══════════════════════════════════════════════════════════════

        private void BtnBackup_Click(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                btnBackup.Enabled = false;
                var path = _svc.CreateBackup();
                this.Cursor = Cursors.Default;
                btnBackup.Enabled = true;

                SetStatus($"✔ Backup berhasil: {Path.GetFileName(path)}", success: true);
                LoadBackupList();
                LoadInfo();

                if (MessageBox.Show(
                    $"Backup berhasil disimpan:\n{path}\n\nBuka folder backup?",
                    "Backup Berhasil",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information) == DialogResult.Yes)
                    Process.Start("explorer.exe", DatabaseBackupService.BackupFolder);
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                btnBackup.Enabled = true;
                SetStatus($"⚠ Backup gagal: {ex.Message}", success: false);
                MessageBox.Show($"Backup gagal:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRestore_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Pilih File Backup",
                Filter = "simPOS Backup (*.zip)|*.zip",
                InitialDirectory = Directory.Exists(DatabaseBackupService.BackupFolder)
                                   ? DatabaseBackupService.BackupFolder
                                   : AppConfig.DataFolder
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            RestoreFile(dlg.FileName);
        }

        private void DgvBackups_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvBackups.Columns[e.ColumnIndex].Name == "colAction")
                RestoreSelected();
        }

        private void RestoreSelected()
        {
            if (dgvBackups.SelectedRows.Count == 0) return;
            var info = dgvBackups.SelectedRows[0].Tag as BackupFileInfo;
            if (info == null) return;
            RestoreFile(info.FilePath);
        }

        private void RestoreFile(string zipPath)
        {
            var result = MessageBox.Show(
                $"Restore database dari:\n{Path.GetFileName(zipPath)}\n\n" +
                "⚠ Data saat ini akan digantikan.\n" +
                "Backup otomatis dari data saat ini akan dibuat terlebih dahulu.\n\n" +
                "Lanjutkan?",
                "Konfirmasi Restore",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                this.Cursor = Cursors.WaitCursor;
                _svc.RestoreFromBackup(zipPath);
                this.Cursor = Cursors.Default;

                SetStatus("✔ Restore berhasil. Restart aplikasi untuk memuat data terbaru.", success: true);
                LoadInfo();
                LoadBackupList();

                MessageBox.Show(
                    "Restore berhasil!\n\nSilakan restart aplikasi simPOS agar data terbaru dimuat.",
                    "Restore Berhasil",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                SetStatus($"⚠ Restore gagal: {ex.Message}", success: false);
                MessageBox.Show($"Restore gagal:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteSelected()
        {
            if (dgvBackups.SelectedRows.Count == 0) return;
            var info = dgvBackups.SelectedRows[0].Tag as BackupFileInfo;
            if (info == null) return;

            if (MessageBox.Show(
                $"Hapus file backup:\n{info.FileName}?",
                "Konfirmasi Hapus",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                File.Delete(info.FilePath);
                LoadBackupList();
                SetStatus($"✔ File backup dihapus: {info.FileName}", success: true);
            }
            catch (Exception ex)
            {
                SetStatus($"⚠ Gagal hapus: {ex.Message}", success: false);
            }
        }

        private void BtnResetTrx_Click(object sender, EventArgs e)
        {
            // Konfirmasi 2 langkah
            if (MessageBox.Show(
                "Reset semua DATA TRANSAKSI (penjualan, riwayat stok, stok produk)?\n\n" +
                "• Master data (produk, kategori, supplier) TETAP ADA\n" +
                "• Backup otomatis dibuat sebelum reset\n\n" +
                "Lanjutkan?",
                "Konfirmasi Reset Transaksi",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes) return;

            // Konfirmasi kedua — ketik RESET
            using var dlgConfirm = new ResetConfirmDialog("RESET");
            if (dlgConfirm.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                this.Cursor = Cursors.WaitCursor;
                _svc.ResetTransactionData();
                this.Cursor = Cursors.Default;

                SetStatus("✔ Data transaksi berhasil direset.", success: true);
                LoadInfo();
                LoadBackupList();
                MessageBox.Show("Reset berhasil!\nData transaksi telah dihapus.",
                    "Reset Selesai", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                SetStatus($"⚠ Reset gagal: {ex.Message}", success: false);
                MessageBox.Show($"Reset gagal:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnResetAll_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                "☢ RESET TOTAL — Seluruh data akan dihapus!\n\n" +
                "• Semua produk, kategori, supplier akan terhapus\n" +
                "• Semua transaksi dan riwayat stok akan terhapus\n" +
                "• Database dikembalikan ke kondisi fresh install\n" +
                "• Backup otomatis dibuat sebelum reset\n\n" +
                "Tindakan ini TIDAK DAPAT DIBATALKAN!\nLanjutkan?",
                "⚠ Konfirmasi Reset Total",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes) return;

            using var dlgConfirm = new ResetConfirmDialog("RESET TOTAL");
            if (dlgConfirm.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                this.Cursor = Cursors.WaitCursor;
                _svc.ResetAllData();
                this.Cursor = Cursors.Default;

                SetStatus("✔ Database berhasil direset total.", success: true);
                LoadInfo();
                LoadBackupList();
                MessageBox.Show(
                    "Reset total berhasil!\n\nSilakan restart aplikasi simPOS.",
                    "Reset Selesai", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                SetStatus($"⚠ Reset gagal: {ex.Message}", success: false);
                MessageBox.Show($"Reset gagal:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════

        private void SetStatus(string msg, bool success)
        {
            lblStatus.Text = "  " + msg;
            lblStatus.ForeColor = success
                ? Color.FromArgb(39, 120, 70)
                : Color.FromArgb(192, 57, 43);
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

    // ── Dialog konfirmasi ketik teks ──────────────────────────────

    internal class ResetConfirmDialog : Form
    {
        private readonly string _expected;
        private TextBox _txtInput;
        private Button _btnOk;

        public ResetConfirmDialog(string expectedText)
        {
            _expected = expectedText;

            this.Text = "Konfirmasi";
            this.Size = new Size(380, 170);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            var lbl = new Label
            {
                Text = $"Ketik  \"{expectedText}\"  untuk konfirmasi:",
                Dock = DockStyle.Top,
                Height = 50,
                Font = new Font("Segoe UI", 9.5f),
                TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(12, 0, 0, 4)
            };

            _txtInput = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 32,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Margin = new Padding(12),
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtInput.TextChanged += (s, e) =>
                _btnOk.Enabled = _txtInput.Text.Trim()
                    .Equals(_expected, StringComparison.OrdinalIgnoreCase);

            _btnOk = new Button
            {
                Text = "Konfirmasi",
                Dock = DockStyle.Bottom,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(192, 57, 43),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Enabled = false
            };
            _btnOk.FlatAppearance.BorderSize = 0;
            _btnOk.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            var btnCancel = new Button
            {
                Text = "Batal",
                Dock = DockStyle.Bottom,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.AcceptButton = _btnOk;
            this.CancelButton = btnCancel;
            this.Controls.Add(_btnOk);
            this.Controls.Add(btnCancel);
            this.Controls.Add(_txtInput);
            this.Controls.Add(lbl);

            this.Shown += (s, e) => _txtInput.Focus();
        }
    }
}
