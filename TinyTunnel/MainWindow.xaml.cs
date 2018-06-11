using System;
using System.Collections.Generic;
using System.Windows;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace TinyTunnel
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isRunning = false;
        private Process tunnel;
        private Process httpProxy;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private SharpConfig.Configuration config = new SharpConfig.Configuration();
        private const string ConfigFileName = "config.ini";

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                config = SharpConfig.Configuration.LoadFromFile(App.TempDir + "\\" + ConfigFileName);
                var section = config["General"];
                if (section.Contains("LocalHost") && !String.IsNullOrEmpty(section["LocalHost"].StringValue.Trim()))
                    localHostTextBox.Text = section["LocalHost"].StringValue;
                if (section.Contains("LocalPort") && section["LocalPort"].IntValue > 0)
                    localPortTextBox.Text = section["LocalPort"].IntValue.ToString();
                if (section.Contains("HttpHost") && !String.IsNullOrEmpty(section["HttpHost"].StringValue.Trim()))
                    httpHostTextBox.Text = section["HttpHost"].StringValue;
                if (section.Contains("HttpPort") && section["HttpPort"].IntValue > 0)
                    httpPortTextBox.Text = section["HttpPort"].IntValue.ToString();
                if (section.Contains("RemoteHost") && !String.IsNullOrEmpty(section["RemoteHost"].StringValue.Trim()))
                    remoteHostTextBox.Text = section["RemoteHost"].StringValue;
                if (section.Contains("RemotePort") && section["RemotePort"].IntValue > 0)
                    remotePortTextBox.Text = section["RemotePort"].IntValue.ToString();
                if (section.Contains("PrivateFilePath") && !String.IsNullOrEmpty(section["PrivateFilePath"].StringValue.Trim()))
                    privateFilePathTextBox.Text = section["PrivateFilePath"].StringValue;
            }
            catch
            {
            }

            this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/logo_gray.ico", UriKind.Absolute));

            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Text = this.Title;
            notifyIcon.Icon = Properties.Resources.logo_gray;
            notifyIcon.Visible = true;
            notifyIcon.MouseDoubleClick += notifyIcon_MouseDoubleClick;

            List<MenuItem> menuItems = new List<MenuItem>();
            
            MenuItem menuItemOpen = new MenuItem();
            menuItemOpen.Text = "Open";
            menuItemOpen.Click += menu_item_open_Click;
            menuItems.Add(menuItemOpen);

            MenuItem menuItemClose = new MenuItem();
            menuItemClose.Text = "Close";
            menuItemClose.Click += menu_item_close_Click;
            menuItems.Add(menuItemClose);

            notifyIcon.ContextMenu = new ContextMenu(menuItems.ToArray());
        }

        private void notifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.ShowActivated = true;
            this.WindowState = WindowState.Normal;
        }

        private void menu_item_open_Click(object sender, EventArgs e)
        {
            this.ShowActivated = true;
            this.WindowState = WindowState.Normal;
        }

        private void menu_item_close_Click(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void start_button_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning == false)
            {
                int tmpInt;
                var section = config["General"];
                section["LocalHost"].StringValue = localHostTextBox.Text.Trim();
                section["LocalPort"].IntValue = (int.TryParse(localPortTextBox.Text, out tmpInt)) ? tmpInt : 7070;
                section["HttpHost"].StringValue = httpHostTextBox.Text.Trim();
                section["HttpPort"].IntValue = (int.TryParse(httpPortTextBox.Text, out tmpInt)) ? tmpInt : 7080;
                section["RemoteHost"].StringValue = remoteHostTextBox.Text.Trim();
                section["RemotePort"].IntValue = (int.TryParse(remotePortTextBox.Text, out tmpInt)) ? tmpInt : 22;
                section["PrivateFilePath"].StringValue = privateFilePathTextBox.Text.Trim();
                config.SaveToFile(App.TempDir + "\\" + ConfigFileName);

                string plinkPath = App.TempDir + "\\" + "plink.exe";
                if (!File.Exists(plinkPath))
                {
                    var stream = File.Create(plinkPath);
                    var data = TinyTunnel.Properties.Resources.plink;
                    stream.Write(data, 0, data.Length);
                    stream.Close();
                }

                string privateFilePath = App.TempDir + "\\" + section["PrivateFilePath"].StringValue;
                if (!File.Exists(privateFilePath))
                {
                    notifyIcon.ShowBalloonTip(5000, section["PrivateFilePath"].StringValue + " not found",
                        "Please click \"Gen\" to generate a private key file and save it to (" + privateFilePath + ")", ToolTipIcon.Info);
                    return;
                }

                string cowPath = App.TempDir + "\\" + "cow.exe";
                if (!File.Exists(cowPath))
                {
                    var stream = File.Create(cowPath);
                    var data = TinyTunnel.Properties.Resources.cow;
                    stream.Write(data, 0, data.Length);
                    stream.Close();
                }

                string cowRcPath = App.TempDir + "\\" + "rc.txt";
                {
                    var writer = new StreamWriter(cowRcPath, false);
                    writer.WriteLine(String.Format("listen = http://{0}:{1}", section["HttpHost"].StringValue, section["HttpPort"].StringValue));
                    writer.WriteLine(String.Format("proxy = socks5://{0}:{1}", section["LocalHost"].StringValue, section["LocalPort"].StringValue));
                    writer.Flush();
                    writer.Close();
                }

                if (test_tunnel())
                {
                    start_tunnel();
                }
                else
                {
                    notifyIcon.ShowBalloonTip(5000, "Tunnel connect test failed!", 
                        "Can not connect to tunnel, please check your remote host and local configuration.", ToolTipIcon.Error);
                }
            }
            else
            {
                close_tunnel();
            }
        }

        private bool test_tunnel()
        {
            var section = config["General"];
            string remoteHost = section["RemoteHost"].StringValue;
            int remotePort = section["RemotePort"].IntValue;
            string privateFilePath = section["PrivateFilePath"].StringValue;

            Process process = new Process();
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.WorkingDirectory = App.TempDir;

            List<string> args = new List<string>()
            {
                "/C", "echo y" , "|", ".\\plink.exe", "-ssh", "-v",
                String.Format("-i {0}", privateFilePath),
                String.Format("-P {0}", remotePort),
                remoteHost,
                "\"exit\""
            };

            process.StartInfo.Arguments = String.Join(" ", args);
            process.Start();
            return process.WaitForExit(30 * 1000);
        }

        private void start_tunnel()
        {
            var section = config["General"];
            string localHost = section["LocalHost"].StringValue;
            int localPort = section["LocalPort"].IntValue;
            string remoteHost = section["RemoteHost"].StringValue;
            int remotePort = section["RemotePort"].IntValue;
            string privateFilePath = section["PrivateFilePath"].StringValue;

            
            tunnel = new Process();
            tunnel.StartInfo = new ProcessStartInfo("plink.exe");
            tunnel.StartInfo.CreateNoWindow = true;
            //tunnel.StartInfo.UseShellExecute = false;
            tunnel.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            tunnel.StartInfo.WorkingDirectory = App.TempDir;
            tunnel.EnableRaisingEvents = true;
            tunnel.Exited += new EventHandler(tunnel_Exited);

            List<string> args = new List<string>()
            {
                "-ssh", "-v", "-N",
                String.Format("-D {0}:{1}", localHost, localPort),
                String.Format("-i {0}", privateFilePath),
                String.Format("-P {0}", remotePort),
                remoteHost
            };

            tunnel.StartInfo.Arguments = String.Join(" ", args);

            httpProxy = new Process();
            httpProxy.StartInfo = new ProcessStartInfo("cow.exe");
            httpProxy.StartInfo.CreateNoWindow = true;
            //httpProxy.StartInfo.UseShellExecute = false;
            httpProxy.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            httpProxy.StartInfo.WorkingDirectory = App.TempDir;

            bool started = false;
            try
            {
                started = tunnel.Start() && httpProxy.Start();
            }
            catch
            {
            }

            if (started)
            {
                isRunning = true;
                notifyIcon.Icon = Properties.Resources.logo;
                this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/logo.ico", UriKind.Absolute));
                this.startButton.Content = "Close";
            }
            else
            {
                notifyIcon.ShowBalloonTip(3000, "Warning", "Failed to start a new tunnel process!", ToolTipIcon.Warning);
                close_tunnel();
            }
        }

        private void close_tunnel()
        {
            isRunning = false;

            if (tunnel != null)
            {
                try
                {
                    if (!tunnel.HasExited)
                        tunnel.Kill();
                    tunnel.Dispose();
                }
                catch
                {
                }
            }

            if (httpProxy !=null)
            {
                try
                {
                    if (!httpProxy.HasExited)
                        httpProxy.Kill();
                    httpProxy.Dispose();
                }
                catch
                {
                }
            }

            notifyIcon.Icon = Properties.Resources.logo_gray;
            this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/logo_gray.ico", UriKind.Absolute));
            this.startButton.Content = "Start";
        }

        private void tunnel_Exited(object sender, EventArgs e)
        {
            tunnel.Dispose();
            tunnel = null;
            this.Dispatcher.Invoke(new Action(delegate
            {
                if (isRunning)
                    notifyIcon.ShowBalloonTip(3000, "Warning", "Tunnel is disconnected, please check your network.", ToolTipIcon.Warning);

                close_tunnel();
            }));
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            close_tunnel();
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.ShowInTaskbar = false;
            }
            else
            {
                this.ShowInTaskbar = true;
            }
        }

        private void generatePrivateKeyButton_Click(object sender, RoutedEventArgs e)
        {
            string puttygenPath = App.TempDir + "\\" + "puttygen.exe";
            if (!File.Exists(puttygenPath))
            {
                var stream = File.Create(puttygenPath);
                var data = Properties.Resources.puttygen;
                stream.Write(data, 0, data.Length);
                stream.Close();
            }

            Process process = new Process();
            process.StartInfo.FileName = "puttygen.exe";
            process.StartInfo.WorkingDirectory = App.TempDir;
            process.Start();
        }
    }
}
