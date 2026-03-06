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

namespace simPOS.POS
{
    /// <summary>
    /// Form tutup kasir (Clerk).
    /// Dipanggil dari FormPOS — setelah tutup, transaksi diblokir hari itu.
    /// </summary>
    public class FormClerk : Form
    {
        private readonly ClerkService _clerk = new ClerkService();

        private Label lblStatus, lblOpenedAt, lblTrxCount, lblOmzet;
        private TextBox txtNotes;
        private Button btnClose, btnCancel;

        // Diisi FormPOS sebelum ShowDialog
        public int TodayTrxCount { get; set; }
        public decimal TodayOmzet { get; set; }
        public string OpenedAt { get; set; }

        public FormClerk()
        {
            InitializeComponent();
            LoadInfo();
        }

        private void InitializeComponent()
        {
            this.Text = "Tutup Kasir (Clerk)";
            this.Size = new Size(440, 420);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            // ── Header ─────────────────────────────────────────
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(44, 62, 80)
            };
            header.Controls.Add(new Label
            {
                Text = "🔒  Tutup Kasir",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            });

            // ── Body ────────────────────────────────────────────
            var body = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 0)
            };

            // Info sesi
            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 130,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(12, 10, 12, 10)
            };
            pnlInfo.Paint += (s, e) => e.Graphics.DrawRectangle(
                new Pen(Color.FromArgb(200, 210, 220)), 0, 0, pnlInfo.Width - 1, pnlInfo.Height - 1);

            lblStatus = MakeInfoLabel("Status sesi  :", "");
            lblOpenedAt = MakeInfoLabel("Dibuka pukul :", "");
            lblTrxCount = MakeInfoLabel("Total transaksi :", "");
            lblOmzet = MakeInfoLabel("Total omzet     :", "");

            pnlInfo.Controls.Add(lblOmzet);
            pnlInfo.Controls.Add(lblTrxCount);
            pnlInfo.Controls.Add(lblOpenedAt);
            pnlInfo.Controls.Add(lblStatus);

            // Catatan
            var lblNotes = new Label
            {
                Text = "Catatan (opsional):",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(60, 60, 60),
                Padding = new Padding(0, 10, 0, 0)
            };

            txtNotes = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 64,
                Multiline = true,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Vertical
            };

            // Warning
            var lblWarn = new Label
            {
                Text = "⚠  Setelah ditutup, tidak ada transaksi baru yang bisa dilakukan hari ini.",
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(180, 80, 0),
                BackColor = Color.FromArgb(255, 243, 220),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };

            body.Controls.Add(lblWarn);
            body.Controls.Add(txtNotes);
            body.Controls.Add(lblNotes);
            body.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent });
            body.Controls.Add(pnlInfo);

            // ── Footer buttons ──────────────────────────────────
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(248, 248, 248),
                Padding = new Padding(24, 12, 24, 12)
            };
            footer.Paint += (s, e) => e.Graphics.DrawLine(
                new Pen(Color.FromArgb(220, 220, 220)), 0, 0, footer.Width, 0);

            btnClose = new Button
            {
                Text = "🔒  Tutup Kasir Sekarang",
                Dock = DockStyle.Right,
                Width = 200,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(192, 57, 43),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += BtnClose_Click;

            btnCancel = new Button
            {
                Text = "Batal",
                Dock = DockStyle.Left,
                Width = 90,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            footer.Controls.Add(btnClose);
            footer.Controls.Add(btnCancel);

            this.Controls.Add(body);
            this.Controls.Add(footer);
            this.Controls.Add(header);
        }

        private void LoadInfo()
        {
            var session = _clerk.GetTodaySession();
            if (session == null)
            {
                lblStatus.Text = "Belum ada sesi hari ini";
                return;
            }

            lblStatus.Text = session.IsOpen ? "🟢 BUKA" : "🔴 TUTUP";
            lblOpenedAt.Text = session.OpenedAt;
        }

        // LoadInfo dipanggil lagi setelah prop di-set dari FormPOS
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Update dengan data real-time dari FormPOS
            var session = _clerk.GetTodaySession();
            lblStatus.Text = session?.IsOpen == true ? "🟢 BUKA" : "🔴 TUTUP";
            lblOpenedAt.Text = OpenedAt.Length > 0 ? OpenedAt : session?.OpenedAt ?? "-";
            lblTrxCount.Text = $"{TodayTrxCount} transaksi";
            lblOmzet.Text = $"Rp {TodayOmzet:N0}";
            lblOmzet.ForeColor = Color.FromArgb(39, 120, 70);
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "Tutup kasir hari ini?\n\nSetelah ditutup, tidak bisa transaksi lagi hari ini.",
                "Konfirmasi Tutup Kasir",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                btnClose.Enabled = false;
                _clerk.CloseSession(txtNotes.Text.Trim());

                MessageBox.Show(
                    "Kasir berhasil ditutup.\n\nAplikasi POS akan ditutup.",
                    "Tutup Kasir Berhasil",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                btnClose.Enabled = true;
                MessageBox.Show($"Gagal tutup kasir:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static Label MakeInfoLabel(string caption, string value)
        {
            var lbl = new Label
            {
                Text = $"{caption.PadRight(20)} {value}",
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(4, 0, 0, 0)
            };
            return lbl;
        }
    }
}
