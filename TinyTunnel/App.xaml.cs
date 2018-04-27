using System.Windows;
using System.IO;

namespace TinyTunnel
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static string TempDir = "TinyTunnelTemp";

        void App_Startup(object sender, StartupEventArgs e)
        {
            LoadResourceDll.RegistDLL();

            if (!Directory.Exists(TempDir))
            {
                Directory.CreateDirectory(TempDir);
            }

            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
