using System;
using System.Windows;
using System.Windows.Media.Animation;
namespace Community.PowerToys.Run.Plugin.SpeedTest
{
    public partial class LoadingWindow : Window
    {
        private readonly Storyboard _spinnerAnimation;
        public LoadingWindow()
        {
            InitializeComponent();
            // Отримуємо анімацію та запускаємо її
            _spinnerAnimation = (Storyboard)FindResource("SpinnerAnimation");
            _spinnerAnimation.Begin();
        }

        public void UpdateStatus(string status)
        {
            // Ensure updates happen on UI thread
            if (Dispatcher.CheckAccess())
            {
                if (StatusText != null)
                {
                    StatusText.Text = status;
                }
            }
            else
            {
                Dispatcher.Invoke(() => UpdateStatus(status));
            }
        }

        public void UpdateServerInfo(string serverName)
        {
            // Ensure updates happen on UI thread
            if (Dispatcher.CheckAccess())
            {
                if (ServerNameText != null)
                {
                    ServerNameText.Text = serverName;
                }
            }
            else
            {
                Dispatcher.Invoke(() => UpdateServerInfo(serverName));
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // Зупиняємо анімацію при закритті вікна
            _spinnerAnimation.Stop();
        }
    }
}