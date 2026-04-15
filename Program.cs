using System;
using System.Windows.Forms;

// ============================================================
//  Program.cs - Uygulama Giriş Noktası
//  Bu dosyaya dokunma, sadece MainForm'u başlatıyor.
// ============================================================

namespace SteganografiApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Windows görsel stillerini aktif et
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // formu başlat
            Application.Run(new MainForm());
        }
    }
}