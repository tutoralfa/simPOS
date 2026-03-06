using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace simPOS.POS.Forms
{
    /// <summary>
    /// Main window aplikasi simPOS.POS.
    /// Langsung menampilkan FormPOS fullscreen tanpa shell navigasi.
    /// </summary>
    public class FormPOSMain : Form
    {
        public FormPOSMain()
        {
            this.Text = "simPOS — Kasir";
            this.Size = new Size(1100, 700);
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(236, 240, 241);
            this.WindowState = FormWindowState.Maximized;
            this.Icon = SystemIcons.Application;

            var pos = new FormPOS
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill,
                Visible = true
            };

            this.Controls.Add(pos);
        }
    }
}
