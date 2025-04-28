using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Navigation;
using Microsoft.Win32;

namespace Community.PowerToys.Run.Plugin.SpeedTest
{
    public partial class ResultsWindow : Window
    {
        private readonly SpeedTestResult _result;

        public ResultsWindow(SpeedTestResult result)
        {
            InitializeComponent();

            // Додаткове логування перед встановленням DataContext
            Debug.WriteLine($"Setting DataContext with result:");
            Debug.WriteLine($"Using CLI values: {result.UsingCliValues}");
            Debug.WriteLine($"CLI Download: {result.CliDownloadMbps:F2} Mbps");
            Debug.WriteLine($"CLI Upload: {result.CliUploadMbps:F2} Mbps");
            Debug.WriteLine($"Calculated Download: {result.Download?.MbpsSpeed:F2} Mbps");
            Debug.WriteLine($"Calculated Upload: {result.Upload?.MbpsSpeed:F2} Mbps");
            Debug.WriteLine($"Display Download: {result.DisplayDownloadSpeed:F2} Mbps");
            Debug.WriteLine($"Display Upload: {result.DisplayUploadSpeed:F2} Mbps");

            // Встановлюємо DataContext
            DataContext = result;
            _result = result;

            // Налаштування теми в залежності від системних параметрів
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            // Альтернативний спосіб виявлення темної теми через реєстр Windows
            bool isDarkTheme = false;

            try 
            {
                // Перевірка налаштування теми в реєстрі Windows
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        object appsUseLightTheme = key.GetValue("AppsUseLightTheme");
                        if (appsUseLightTheme != null && appsUseLightTheme is int value && value == 0)
                        {
                            isDarkTheme = true;
                        }
                    }
                }
            }
            catch
            {
                // У випадку помилки просто продовжуємо зі світлою темою
            }

            if (isDarkTheme)
            {
                // Темна тема
                Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(32, 32, 32));
                Resources["TextBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                Resources["CardBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CopyResults_Click(object sender, RoutedEventArgs e)
        {
            // Копіюємо результати в буфер обміну
            Clipboard.SetText(_result.ToString());

            // Повідомлення про успішне копіювання
            MessageBox.Show("Results copied to clipboard!", "Speed Test", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // Відкриваємо URL в браузері
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}