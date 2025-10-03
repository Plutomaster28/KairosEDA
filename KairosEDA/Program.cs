using System;
using System.Windows.Forms;

namespace KairosEDA
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Enable Windows 7 visual styles
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Force Windows 7 Aero theme
            Win32Native.SetWindowTheme();
            
            // Show splash screen (modal dialog)
            using (SplashScreen splash = new SplashScreen())
            {
                splash.ShowDialog();
            }
            
            Application.Run(new MainForm());
        }
    }
}
