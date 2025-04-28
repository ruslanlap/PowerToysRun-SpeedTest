using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace Community.PowerToys.Run.Plugin.SpeedTest
{
    public partial class ResultsWindow : Window
    {
        public ResultsWindow(SpeedTestResult result)
        {
            InitializeComponent();
            DataContext = result;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // Відкриваємо URL в браузері
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
