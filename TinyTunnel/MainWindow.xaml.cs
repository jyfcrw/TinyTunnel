using System;
using System.Collections.Generic;
using System.Windows;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Windows.Forms;

namespace TinyTunnel
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isRunning = false;
        private Process tunnel;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private SharpConfig.Configuration config = new SharpConfig.Configuration();
        private const string ConfigFileName = "config.ini";

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                config = SharpConfig.Configuration.LoadFromFile(ConfigFileName);
                var section = config["General"];
                localHostTextBox.Text = section["LocalHost"].StringValue;
                localPortTextBox.Text = section["LocalPort"].IntValue.ToString();
                remoteHostTextBox.Text = section["RemoteHost"].StringValue;
                remotePortTextBox.Text = section["RemotePort"].IntValue.ToString();
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
                section["RemoteHost"].StringValue = remoteHostTextBox.Text.Trim();
                section["RemotePort"].IntValue = (int.TryParse(remotePortTextBox.Text, out tmpInt)) ? tmpInt : 22;
                section["PrivateFilePath"].StringValue = privateFilePathTextBox.Text.Trim();
                config.SaveToFile(ConfigFileName);

                start_tunnel();
                startButton.Content = "Close";
            }
            else
            {
                close_tunnel();
                startButton.Content = "Start";
            }
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

            string argsStr = String.Join(" ", args);
            Console.WriteLine(argsStr);
            tunnel.StartInfo.Arguments = argsStr;

            bool started = false;
            try
            {
                started = tunnel.Start();
            }
            catch
            {
            }

            if (started)
            {
                isRunning = true;
                notifyIcon.Icon = Properties.Resources.logo;
                this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/logo.ico", UriKind.Absolute));
            }
            else
            {
                notifyIcon.ShowBalloonTip(3000, "TinyTunnel Warning", "Failed to start a new tunnel!", ToolTipIcon.Warning);
                close_tunnel();
                startButton.Content = "Start";
            }
        }

        private void close_tunnel()
        {
            if (tunnel != null)
            {
                try
                {
                    if (!tunnel.HasExited)
                        tunnel.Kill();
                }
                catch
                {
                }

                tunnel.Dispose();
            }
            isRunning = false;
            notifyIcon.Icon = Properties.Resources.logo_gray;
            this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/logo_gray.ico", UriKind.Absolute));
        }

        private void tunnel_Exited(object sender, EventArgs e)
        {
            close_tunnel();
            notifyIcon.ShowBalloonTip(3000, "TinyTunnel Warning", "Tunnel close abnormally, start again.", ToolTipIcon.Warning);
            start_tunnel();
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
    }
}
