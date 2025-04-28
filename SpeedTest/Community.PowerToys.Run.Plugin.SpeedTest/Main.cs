using ManagedCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.SpeedTest
{
    public class Main : IPlugin, IPluginI18n, IContextMenu, IDisposable
    {
        public static string PluginID => "5A0F7ED1D3F24B0A900732839D0E43DB";
        public string Name => "SpeedTest";
        public string Description => "Run internet speed tests directly from PowerToys Run";

        private PluginInitContext Context { get; set; }
        private string IconPath { get; set; }
        private bool _isRunningTest = false;

        // Шлях до bundled speedtest.exe (той, що лежить поряд із DLL)
        private readonly string _cliPath;

        public Main()
        {
            var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                         ?? AppContext.BaseDirectory;
            _cliPath = Path.Combine(folder, "speedtest.exe");
        }

        public void Init(PluginInitContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(Context.API.GetCurrentTheme());
        }

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();
            if (query == null) return results;

            if (!_isRunningTest)
            {
                results.Add(new Result
                {
                    Title       = "Run Speed Test",
                    SubTitle    = "Test your internet connection speed",
                    IcoPath     = IconPath,
                    Score       = 100,
                    ContextData = query.Search?.Trim() ?? "",
                    Action      = _ =>
                    {
                        RunSpeedTest(query.Search?.Trim() ?? "");
                        return true;
                    }
                });
            }
            else
            {
                results.Add(new Result
                {
                    Title    = "Speed test is currently running…",
                    SubTitle = "Please wait for the current test to complete",
                    IcoPath  = IconPath,
                    Score    = 100,
                    Action   = _ => false
                });
            }

            return results;
        }

        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            var items = new List<ContextMenuResult>();
            if (selectedResult.ContextData is string ctx && ctx == "speedtest")
            {
                // Use Specific Server
                items.Add(new ContextMenuResult
                {
                    PluginName = Name,
                    Title      = "Use Specific Server",
                    Glyph      = "\uE7BE",
                    AcceleratorKey       = Key.S,
                    AcceleratorModifiers = ModifierKeys.Control,
                    Action = _ =>
                    {
                        var id = Microsoft.VisualBasic.Interaction.InputBox(
                            "Enter a Speedtest server ID:", "Select Server", "");
                        if (!string.IsNullOrEmpty(id))
                            RunSpeedTest($"--server-id={id}");
                        return true;
                    }
                });

                // Download Only
                items.Add(new ContextMenuResult
                {
                    PluginName = Name,
                    Title      = "Download Only Test",
                    Glyph      = "\uE896",
                    AcceleratorKey       = Key.D,
                    AcceleratorModifiers = ModifierKeys.Control,
                    Action = _ =>
                    {
                        RunSpeedTest("--no-upload");
                        return true;
                    }
                });

                // List Servers
                items.Add(new ContextMenuResult
                {
                    PluginName = Name,
                    Title      = "List Available Servers",
                    Glyph      = "\uE71D",
                    AcceleratorKey       = Key.L,
                    AcceleratorModifiers = ModifierKeys.Control,
                    Action = _ =>
                    {
                        ListServers();
                        return true;
                    }
                });

                // Copy to clipboard
                items.Add(new ContextMenuResult
                {
                    PluginName = Name,
                    Title      = "Copy to clipboard (Ctrl+C)",
                    Glyph      = "\uE8C8",
                    AcceleratorKey       = Key.C,
                    AcceleratorModifiers = ModifierKeys.Control,
                    Action = _ =>
                    {
                        Clipboard.SetDataObject("speedtest");
                        return true;
                    }
                });
            }

            return items;
        }

        private void UpdateIconPath(Theme theme) =>
            IconPath = (theme == Theme.Light || theme == Theme.HighContrastWhite)
                       ? "Images\\speedtest.light.png"
                       : "Images\\speedtest.dark.png";

        private void OnThemeChanged(Theme _, Theme newTheme) =>
            UpdateIconPath(newTheme);

        private async void RunSpeedTest(string additionalArgs = "")
        {
            if (_isRunningTest) return;
            _isRunningTest = true;
            Context.API.ChangeQuery("speedtest running...");

            // Створюємо і показуємо вікно завантаження
            var loadingWindow = new LoadingWindow();
            loadingWindow.Show();

            try
            {
                loadingWindow.UpdateStatus("Пошук найкращого сервера...");

                var psi = new ProcessStartInfo
                {
                    FileName        = File.Exists(_cliPath) ? _cliPath : "speedtest",
                    Arguments       = $"--format=json --accept-license --accept-gdpr {additionalArgs}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow  = true
                };

                using var proc = Process.Start(psi);

                // Затримка для імітації початку тесту
                await Task.Delay(1000);

                loadingWindow.UpdateStatus("Перевірка латентності...");
                await Task.Delay(1500);

                loadingWindow.UpdateStatus("Тестування швидкості завантаження...");
                // Тут можна додати якийсь спосіб дізнатися про обраний сервер, якщо доступно
                loadingWindow.UpdateServerInfo("UARNET, Львів");
                await Task.Delay(3000);

                loadingWindow.UpdateStatus("Тестування швидкості вивантаження...");
                await Task.Delay(3000);

                // Зчитуємо результати
                var output = await proc.StandardOutput.ReadToEndAsync();
                await proc.WaitForExitAsync();

                // Закриваємо вікно завантаження
                loadingWindow.Close();

                // Показуємо результати
                DisplayResults(output);
            }
            catch (Exception ex)
            {
                // Закриваємо вікно завантаження у випадку помилки
                loadingWindow.Close();

                MessageBox.Show(
                    $"Error running speed test: {ex.Message}\n\nMake sure Speedtest CLI is installed or bundled.",
                    "Speed Test Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isRunningTest = false;
                Context.API.ChangeQuery("speedtest ");
            }
        }

        private void CopyResults_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Отримуємо результат з вікна
                if (sender is FrameworkElement element && element.DataContext is SpeedTestResult result)
                {
                    Clipboard.SetText(result.ToString());
                    MessageBox.Show("Results copied to clipboard!", "Speed Test", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying results: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void DisplayResults(string jsonOutput)
        {
            try
            {
                var result = JsonSerializer.Deserialize<SpeedTestResult>(
                    jsonOutput,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result != null)
                {
                    // Формуємо текст і копіюємо його в буфер
                    var text = result.ToString();
                    Clipboard.SetText(text);

                    // Показуємо кастомне WPF-вікно
                    var window = new ResultsWindow(result);
                    window.ShowDialog();
                }
                else
                {
                    throw new InvalidOperationException("Could not parse JSON");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing results: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ListServers()
        {
            if (_isRunningTest) return;
            _isRunningTest = true;
            Context.API.ChangeQuery("speedtest servers…");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName        = File.Exists(_cliPath) ? _cliPath : "speedtest",
                    Arguments       = "--servers --format=json --accept-license --accept-gdpr",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow  = true
                };

                using var proc = Process.Start(psi);
                var output = await proc.StandardOutput.ReadToEndAsync();
                await proc.WaitForExitAsync();

                MessageBox.Show($"Available Servers:\n{output}", "Speed Test Servers", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error listing servers: {ex.Message}\n\nMake sure Speedtest CLI is installed or bundled.",
                    "Speed Test Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isRunningTest = false;
                Context.API.ChangeQuery("speedtest ");
            }
        }

        public string GetTranslatedPluginTitle()       => Name;
        public string GetTranslatedPluginDescription() => Description;

        public void Dispose()
        {
            Context.API.ThemeChanged -= OnThemeChanged;
            GC.SuppressFinalize(this);
        }
    }
}
