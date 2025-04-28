using ManagedCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.SpeedTest
{
    public class Main : IPlugin, IPluginI18n, IDisposable
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
                    Action      = _ =>
                    {
                        RunSpeedTest();
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

        private void UpdateIconPath(Theme theme) =>
            IconPath = (theme == Theme.Light || theme == Theme.HighContrastWhite)
                       ? "Images\\speedtest.light.png"
                       : "Images\\speedtest.dark.png";

        private void OnThemeChanged(Theme _, Theme newTheme) =>
            UpdateIconPath(newTheme);

        private async void RunSpeedTest()
        {
            if (_isRunningTest) return;
            _isRunningTest = true;
            Context.API.ChangeQuery("speedtest running...");

            // Створюємо і показуємо вікно завантаження
            var loadingWindow = new LoadingWindow();
            loadingWindow.Show();

            try
            {
                // Перевіряємо наявність Speedtest CLI
                string cliExecutable = File.Exists(_cliPath) ? _cliPath : "speedtest";

                // Налаштовуємо процес для запуску Speedtest CLI
                var psi = new ProcessStartInfo
                {
                    FileName = cliExecutable,
                    Arguments = "--format=json --accept-license --accept-gdpr",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);

                // Обробка даних з stderr для відстеження прогресу тесту в реальному часі
                proc.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        loadingWindow.Dispatcher.Invoke(() =>
                        {
                            // Визначення етапу тесту на основі повідомлення
                            if (e.Data.Contains("Selecting best server") || e.Data.Contains("Finding optimal server"))
                            {
                                loadingWindow.UpdateStage(LoadingWindow.TestStage.Connecting);
                                loadingWindow.UpdateDetail(e.Data);
                            }
                            else if (e.Data.Contains("Hosted by"))
                            {
                                // Витягаємо інформацію про сервер за допомогою регулярного виразу
                                var serverMatch = Regex.Match(e.Data, @"Hosted by (.+?) \[.+?\].+?: (\d+\.\d+) ms");
                                if (serverMatch.Success)
                                {
                                    string serverName = serverMatch.Groups[1].Value;
                                    string ping = serverMatch.Groups[2].Value;
                                    loadingWindow.UpdateServerInfo($"{serverName} [{ping} ms]");
                                }
                                loadingWindow.UpdateStage(LoadingWindow.TestStage.Latency);
                                loadingWindow.UpdateDetail(e.Data);
                            }
                            else if (e.Data.Contains("Testing download speed"))
                            {
                                loadingWindow.UpdateStage(LoadingWindow.TestStage.Download);
                                loadingWindow.UpdateDetail("Measuring download speed...");
                            }
                            else if (e.Data.Contains("Testing upload speed"))
                            {
                                loadingWindow.UpdateStage(LoadingWindow.TestStage.Upload);
                                loadingWindow.UpdateDetail("Measuring upload speed...");
                            }
                            else if (e.Data.Contains("Mbps"))
                            {
                                // Витягуємо значення швидкості
                                var speedMatch = Regex.Match(e.Data, @"(\d+\.\d+) Mbps");
                                if (speedMatch.Success)
                                {
                                    loadingWindow.UpdateCurrentSpeed($"{speedMatch.Groups[1].Value} Mbps");
                                }
                                loadingWindow.UpdateDetail(e.Data);
                            }
                            else
                            {
                                loadingWindow.UpdateDetail(e.Data);
                            }
                        });
                    }
                };

                // Запускаємо асинхронне читання stderr
                proc.BeginErrorReadLine();

                // Читаємо результат (JSON) зі stdout
                string jsonOutput = await proc.StandardOutput.ReadToEndAsync();
                await proc.WaitForExitAsync();

                // Позначаємо тест як завершений
                loadingWindow.Dispatcher.Invoke(() =>
                {
                    loadingWindow.UpdateStage(LoadingWindow.TestStage.Complete);
                    loadingWindow.UpdateDetail("Analyzing results...");
                });

                // Невелика затримка, щоб користувач побачив повідомлення про завершення
                await Task.Delay(1000);

                // Закриваємо вікно завантаження
                loadingWindow.Close();

                // Відображаємо результати
                if (!string.IsNullOrEmpty(jsonOutput))
                {
                    DisplayResults(jsonOutput);
                }
                else
                {
                    MessageBox.Show("Unable to get test results. Please try again.", 
                        "Speed Test Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
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

        public string GetTranslatedPluginTitle()       => Name;
        public string GetTranslatedPluginDescription() => Description;

        public void Dispose()
        {
            Context.API.ThemeChanged -= OnThemeChanged;
            GC.SuppressFinalize(this);
        }
    }
}