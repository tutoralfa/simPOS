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

namespace simPOS.Management.Forms.StockOpnameMgmt
{
    /// <summary>
    /// Form buat sesi opname baru.
    /// Cukup isi no. opname + catatan — produk dimuat otomatis dari DB.
    /// </summary>
    public class FormStockOpnameEntry : Form
    {
        private readonly StockOpnameService _service = new StockOpnameService();

        private TextBox txtOpnameNo;
        private TextBox txtNotes;
        private Button btnGenerate;
        private Button btnStart;
        private Button btnCancel;

        public FormStockOpnameEntry()
        {
            InitializeComponent();
            txtOpnameNo.Text = _service.GenerateOpnameNo();
        }

        private void InitializeComponent()
        {
            this.Text = "Buat Sesi Stock Opname";
            this.Size = new Size(420, 230);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(20, 20, 20, 10)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // No. Opname
            body.Controls.Add(MakeLabel("No. Opname *"), 0, 0);
            var noPanel = new Panel { Dock = DockStyle.Fill };
            txtOpnameNo = new TextBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 9.5f) };
            btnGenerate = new Button
            {
                Text = "⟳",
                Width = 30,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10f)
            };
            btnGenerate.FlatAppearance.BorderSize = 0;
            btnGenerate.Click += (s, e) => txtOpnameNo.Text = _service.GenerateOpnameNo();
            noPanel.Controls.Add(txtOpnameNo);
            noPanel.Controls.Add(btnGenerate);
            body.Controls.Add(noPanel, 1, 0);

            // Catatan
            body.Controls.Add(MakeLabel("Catatan"), 0, 1);
            txtNotes = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f) };
            body.Controls.Add(txtNotes, 1, 1);

            // Info
            var lblInfo = new Label
            {
                Dock = DockStyle.Fill,
                Text = "ℹ Semua produk aktif akan dimuat otomatis.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 100, 100),
                Padding = new Padding(110, 4, 0, 0)
            };
            body.SetColumnSpan(lblInfo, 2);
            body.Controls.Add(lblInfo, 0, 2);

            // Footer
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.FromArgb(248, 248, 248) };
            footer.Paint += (s, e) => e.Graphics.DrawLine(
                new System.Drawing.Pen(Color.FromArgb(220, 220, 220)), 0, 0, footer.Width, 0);

            btnStart = new Button
            {
                Text = "▶ Mulai Opname",
                Width = 130,
                Height = 33,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Location = new Point(footer.Width - 250, 9);
            btnStart.Click += BtnStart_Click;

            btnCancel = new Button
            {
                Text = "Batal",
                Width = 90,
                Height = 33,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Location = new Point(footer.Width - 110, 9);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.AcceptButton = btnStart;
            this.CancelButton = btnCancel;
            footer.Controls.Add(btnStart);
            footer.Controls.Add(btnCancel);

            this.Controls.Add(body);
            this.Controls.Add(footer);
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            var (success, message, opname) = _service.CreateSession(
                txtOpnameNo.Text.Trim(),
                txtNotes.Text.Trim());

            if (!success)
            {
                MessageBox.Show(message, "Gagal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Tutup form ini lalu buka form input langsung
            this.Hide();
            var inputForm = new FormStockOpnameInput(opname);
            var result = inputForm.ShowDialog();

            this.DialogResult = result;
            this.Close();
        }

        private static Label MakeLabel(string text) => new Label
        {
            Text = text,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = System.Drawing.ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 9f),
            Padding = new Padding(0, 0, 10, 0)
        };
    }
}
