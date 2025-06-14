// File: ResultsWindow.xaml.cs
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Navigation;
using Microsoft.Win32; // For Registry
using System;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace Community.PowerToys.Run.Plugin.SpeedTest
{
    public partial class ResultsWindow : Window // Ensure this matches your XAML
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindow(IntPtr hWnd, bool bInvert);

        private readonly SpeedTestResult _result;

        public ResultsWindow(SpeedTestResult result)
        {
            InitializeComponent(); // This calls XAML parsing. If XAML uses StaticResource for brushes defined below, it can fail.
                                   // RECOMMENDATION: Use DynamicResource in XAML for BackgroundBrush, TextBrush, CardBrush.

            _result = result ?? throw new ArgumentNullException(nameof(result), "SpeedTestResult cannot be null.");
            DataContext = _result; // Set DataContext for XAML bindings

            ApplyTheme(); // Apply theme after InitializeComponent and DataContext are set.

            // Flash the window when it's loaded
            this.Loaded += (s, e) => FlashWindow(new WindowInteropHelper(this).Handle, true);

            #if DEBUG
            // Log the result data for debugging purposes.
            Debug.WriteLine("ResultsWindow: DataContext set. Result Object Debug Info:");
            Debug.WriteLine(_result.GetDebugInfo()); // Assumes GetDebugInfo() is a comprehensive string representation
            #endif
        }

        private void ApplyTheme()
        {
            bool isDarkTheme = false;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key?.GetValue("AppsUseLightTheme") is int appsUseLightThemeValue && appsUseLightThemeValue == 0)
                    {
                        isDarkTheme = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ResultsWindow Error: Failed to read theme from registry. {ex.Message}");
                // Default to light theme if registry read fails
            }

            // Define brushes directly in the window's resources.
            // XAML should use <Setter Property="Background" Value="{DynamicResource BackgroundBrush}" />
            // or ensure these keys exist with default values if using StaticResource.
            if (isDarkTheme)
            {
                Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(32, 32, 32));
                Resources["TextBrush"] = new SolidColorBrush(Colors.White);
                Resources["CardBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(60, 60, 60)); // Example
                Resources["HyperlinkBrush"] = new SolidColorBrush(Colors.Cyan); // Example
            }
            else // Explicitly set light theme colors
            {
                Resources["BackgroundBrush"] = new SolidColorBrush(Colors.WhiteSmoke);
                Resources["TextBrush"] = new SolidColorBrush(Colors.Black);
                Resources["CardBrush"] = new SolidColorBrush(Colors.White);
                Resources["BorderBrush"] = new SolidColorBrush(Colors.LightGray); // Example
                Resources["HyperlinkBrush"] = new SolidColorBrush(Colors.Blue); // Example
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CopyResults_Click(object sender, RoutedEventArgs e)
        {
            if (_result != null)
            {
                try
                {
                    // Use the ToString method from SpeedTestResult which formats the output correctly
                    string formattedResults = _result.ToString();
                    Clipboard.SetText(formattedResults);
                    MessageBox.Show("Results copied to clipboard!", "Speed Test", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to copy results to clipboard: {ex.Message}", "Clipboard Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Debug.WriteLine($"ResultsWindow Clipboard Error: {ex.Message}");
                }
            }
        }

        private void CopyImageUrl_Click(object sender, RoutedEventArgs e)
        {
            if (_result?.Result?.Url != null && Uri.TryCreate(_result.Result.Url, UriKind.Absolute, out _))
            {
                try
                {
                    string imageUrl = _result.Result.Url.TrimEnd('/') + ".png";
                    Clipboard.SetText(imageUrl);
                    MessageBox.Show("Result Image URL copied to clipboard!", "Speed Test", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to copy image URL: {ex.Message}", "Clipboard Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Debug.WriteLine($"ResultsWindow Copy Image URL Error: {ex.Message}");
                }
            }
            else
            {
                 MessageBox.Show("No valid result URL available to create an image link.", "URL Not Available", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (e.Uri != null && e.Uri.IsAbsoluteUri)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open link: {e.Uri.AbsoluteUri}\nError: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Debug.WriteLine($"ResultsWindow Hyperlink Navigation Error: {ex.Message}");
                }
            }
            e.Handled = true; // Mark event as handled
        }
    }
}