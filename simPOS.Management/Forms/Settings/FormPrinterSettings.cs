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
using simPOS.Shared.Printing;

namespace simPOS.Management.Forms.Settings
{
    public class FormPrinterSettings : Form
    {
        private PrinterConfig _config;

        // Controls
        private ComboBox cmbPrinter;
        private ComboBox cmbPaperWidth;
        private TextBox txtStoreName;
        private TextBox txtStoreAddress;
        private TextBox txtStorePhone;
        private TextBox txtFooter;
        private CheckBox chkAutoCut;
        private CheckBox chkPrintEnabled;
        private Label lblStatus;
        private Button btnTest;
        private Button btnSave;
        private Button btnCancel;

        public FormPrinterSettings()
        {
            _config = PrinterConfig.Load();
            InitializeComponent();
            LoadPrinters();
            PopulateFields();
        }

        private void InitializeComponent()
        {
            this.Text = "Pengaturan Printer Thermal";
            this.Size = new Size(520, 540);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            BuildForm();
        }

        private void BuildForm()
        {
            // Header
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(44, 62, 80),
                Padding = new Padding(16, 0, 0, 0)
            };
            header.Controls.Add(new Label
            {
                Text = "🖨  Pengaturan Printer Thermal",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft
            });

            // Body
            var body = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 16, 20, 10)
            };

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoSize = true
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // Printer
            AddSectionLabel(tbl, "PRINTER");
            AddRow(tbl, "Printer *", cmbPrinter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9f) });
            AddRow(tbl, "Lebar Kertas", cmbPaperWidth = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9f) });
            cmbPaperWidth.Items.AddRange(new object[] { "80 mm (48 karakter)", "58 mm (32 karakter)" });

            // Tombol refresh & test printer dalam satu baris
            var pnlPrinterActions = new Panel { Dock = DockStyle.Fill, Height = 32 };
            var btnRefresh = new Button
            {
                Text = "⟳ Refresh",
                Width = 90,
                Height = 28,
                Location = new Point(0, 2),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => LoadPrinters();

            btnTest = new Button
            {
                Text = "🖨 Test Print",
                Width = 100,
                Height = 28,
                Location = new Point(96, 2),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnTest.FlatAppearance.BorderSize = 0;
            btnTest.Click += BtnTest_Click;

            pnlPrinterActions.Controls.Add(btnRefresh);
            pnlPrinterActions.Controls.Add(btnTest);
            tbl.Controls.Add(new Label()); // empty label kolom 1
            tbl.Controls.Add(pnlPrinterActions);

            // Toko
            AddSectionLabel(tbl, "INFORMASI TOKO");
            AddRow(tbl, "Nama Toko", txtStoreName = new TextBox { Font = new Font("Segoe UI", 9f) });
            AddRow(tbl, "Alamat", txtStoreAddress = new TextBox { Font = new Font("Segoe UI", 9f) });
            AddRow(tbl, "Telepon", txtStorePhone = new TextBox { Font = new Font("Segoe UI", 9f) });
            AddRow(tbl, "Pesan Footer", txtFooter = new TextBox { Font = new Font("Segoe UI", 9f) });

            // Opsi
            AddSectionLabel(tbl, "OPSI");

            chkAutoCut = new CheckBox
            {
                Text = "Auto cut setelah print",
                Font = new Font("Segoe UI", 9f),
                Dock = DockStyle.Fill,
                Checked = true
            };
            tbl.Controls.Add(new Label());
            tbl.Controls.Add(chkAutoCut);

            chkPrintEnabled = new CheckBox
            {
                Text = "Aktifkan print struk otomatis",
                Font = new Font("Segoe UI", 9f),
                Dock = DockStyle.Fill,
                Checked = true
            };
            tbl.Controls.Add(new Label());
            tbl.Controls.Add(chkPrintEnabled);

            body.Controls.Add(tbl);

            // Status bar
            lblStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0)
            };

            // Footer buttons
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.FromArgb(248, 248, 248)
            };
            footer.Paint += (s, e) => e.Graphics.DrawLine(
                new System.Drawing.Pen(Color.FromArgb(220, 220, 220)), 0, 0, footer.Width, 0);

            btnSave = new Button
            {
                Text = "💾 Simpan",
                Width = 100,
                Height = 33,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Location = new Point(footer.Width - 215, 9);
            btnSave.Click += BtnSave_Click;

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
            btnCancel.Location = new Point(footer.Width - 105, 9);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
            footer.Controls.Add(btnSave);
            footer.Controls.Add(btnCancel);

            this.Controls.Add(body);
            this.Controls.Add(lblStatus);
            this.Controls.Add(footer);
            this.Controls.Add(header);
        }

        // ── Data ─────────────────────────────────────────────────────

        private void LoadPrinters()
        {
            var current = cmbPrinter.SelectedItem?.ToString() ?? _config.PrinterName;
            cmbPrinter.Items.Clear();
            cmbPrinter.Items.Add("(Pilih printer...)");

            foreach (string name in PrinterSettings.InstalledPrinters)
                cmbPrinter.Items.Add(name);

            // Re-select
            if (!string.IsNullOrEmpty(current))
            {
                var idx = cmbPrinter.Items.IndexOf(current);
                cmbPrinter.SelectedIndex = idx >= 0 ? idx : 0;
            }
            else
            {
                cmbPrinter.SelectedIndex = 0;
            }

            SetStatus($"{cmbPrinter.Items.Count - 1} printer ditemukan.");
        }

        private void PopulateFields()
        {
            // Printer
            var printerIdx = cmbPrinter.Items.IndexOf(_config.PrinterName);
            cmbPrinter.SelectedIndex = printerIdx >= 0 ? printerIdx : 0;

            cmbPaperWidth.SelectedIndex = _config.PaperWidth == 58 ? 1 : 0;

            // Toko
            txtStoreName.Text = _config.StoreName;
            txtStoreAddress.Text = _config.StoreAddress;
            txtStorePhone.Text = _config.StorePhone;
            txtFooter.Text = _config.FooterMessage;

            // Opsi
            chkAutoCut.Checked = _config.AutoCut;
            chkPrintEnabled.Checked = _config.PrintEnabled;
        }

        private bool CollectFields()
        {
            if (cmbPrinter.SelectedIndex <= 0)
            {
                SetStatus("⚠ Pilih printer terlebih dahulu.", error: true);
                return false;
            }

            _config.PrinterName = cmbPrinter.SelectedItem.ToString();
            _config.PaperWidth = cmbPaperWidth.SelectedIndex == 1 ? 58 : 80;
            _config.CharPerLine = _config.PaperWidth == 58 ? 32 : 48;
            _config.StoreName = txtStoreName.Text.Trim();
            _config.StoreAddress = txtStoreAddress.Text.Trim();
            _config.StorePhone = txtStorePhone.Text.Trim();
            _config.FooterMessage = txtFooter.Text.Trim();
            _config.AutoCut = chkAutoCut.Checked;
            _config.PrintEnabled = chkPrintEnabled.Checked;
            return true;
        }

        // ── Events ───────────────────────────────────────────────────

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!CollectFields()) return;

            try
            {
                _config.Save();
                SetStatus("✔ Pengaturan berhasil disimpan.");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                SetStatus($"⚠ Gagal menyimpan: {ex.Message}", error: true);
            }
        }

        private void BtnTest_Click(object sender, EventArgs e)
        {
            if (!CollectFields()) return;

            try
            {
                var builder = new EscPosBuilder(_config.CharPerLine);
                builder.Initialize()
                       .Center().Bold().Font2H()
                       .TextLine(_config.StoreName)
                       .Normal()
                       .Center()
                       .TextLine(string.IsNullOrEmpty(_config.StoreAddress) ? "" : _config.StoreAddress)
                       .TextLine(string.IsNullOrEmpty(_config.StorePhone) ? "" : _config.StorePhone)
                       .Divider('=')
                       .Center().Bold()
                       .TextLine("-- TEST PRINT --")
                       .Normal()
                       .Center()
                       .TextLine($"Printer : {_config.PrinterName}")
                       .TextLine($"Lebar   : {_config.PaperWidth}mm ({_config.CharPerLine} char)")
                       .TextLine($"Waktu   : {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                       .Divider('=')
                       .Center()
                       .TextLine(_config.FooterMessage)
                       .NewLine(2);

                if (_config.AutoCut) builder.Cut();

                EscPosPrinter.PrintRaw(builder.Build(), _config.PrinterName);
                SetStatus("✔ Test print berhasil dikirim ke printer.");
            }
            catch (Exception ex)
            {
                SetStatus($"⚠ Test print gagal: {ex.Message}", error: true);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────

        private void SetStatus(string msg, bool error = false)
        {
            lblStatus.Text = "  " + msg;
            lblStatus.ForeColor = error
                ? Color.FromArgb(192, 57, 43)
                : Color.FromArgb(39, 130, 76);
        }

        private static void AddSectionLabel(TableLayoutPanel tbl, string title)
        {
            var lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(127, 140, 141),
                Dock = DockStyle.Fill,
                Height = 28,
                TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(0, 0, 0, 2)
            };
            tbl.SetColumnSpan(lbl, 2);
            tbl.Controls.Add(lbl);
        }

        private static void AddRow(TableLayoutPanel tbl, string labelText, Control control)
        {
            var lbl = new Label
            {
                Text = labelText,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(60, 60, 60),
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 10, 6)
            };
            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(0, 0, 0, 8);
            tbl.Controls.Add(lbl);
            tbl.Controls.Add(control);
        }
    }
}
