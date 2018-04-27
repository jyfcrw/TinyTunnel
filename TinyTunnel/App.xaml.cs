using System.Windows;
using System.IO;

namespace TinyTunnel
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        void App_Startup(object sender, StartupEventArgs e)
        {
            LoadResourceDll.RegistDLL();

            if (!File.Exists("plink.exe")) { 
                var stream = File.Create("plink.exe");
                var data = TinyTunnel.Properties.Resources.plink;
                stream.Write(data, 0, data.Length);
                stream.Close();
            }

            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
