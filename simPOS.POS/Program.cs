using simPOS.Shared.Database;
using simPOS.POS.Forms;
using System.Text;

namespace simPOS.POS
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            try
            {
                DatabaseHelper.Initialize(AppConfig.DatabasePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Gagal menginisialisasi database:\n{ex.Message}",
                    "Error Startup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Application.Run(new FormPOSMain());
        }
    }
}