using System;
using System.Windows.Forms;

namespace KairosEDA
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Enable visual styles for Windows XP+ look
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Show splash screen (modal dialog)
            using (SplashScreen splash = new SplashScreen())
            {
                splash.ShowDialog();
            }
            
            Application.Run(new MainForm());
        }
    }
}
