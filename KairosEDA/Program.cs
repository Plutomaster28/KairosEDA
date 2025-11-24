using System;

namespace KairosEDA
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Show WinForms splash screen
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            
            using (var splash = new SplashScreen())
            {
                splash.ShowDialog();
            }
            
            // Start WPF application
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
