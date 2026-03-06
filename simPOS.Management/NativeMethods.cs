using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace simPOS.Management
{
    /// <summary>
    /// P/Invoke untuk membawa window yang sudah terbuka ke foreground.
    /// Dipakai saat user klik "Buka Kasir" tapi POS sudah berjalan.
    /// </summary>
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
