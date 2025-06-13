using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions; 
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls; 
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.SpeedTest
{
    public class Main : IPlugin, IPluginI18n, ISettingProvider, IDisposable
    {
        public static string PluginID => "5A0F7ED1D3F24B0A900732839D0E43DB";
        public string Name => "SpeedTest";
        public string Description => "Run internet speed tests (Ookla Speedtest CLI) and view results.";

        private PluginInitContext _context;
        private bool _isRunningTest;
        private readonly string _cliPath;
        private string _iconPath;
        private CancellationTokenSource _cancellationTokenSource;
        
        // Settings
        private bool _copyToClipboard = false; // Default: do not copy to clipboard

        // Regexes to attempt to determine current test stage from stderr.
        private static readonly Regex ServerRegex = new Regex(@"(Server:|Hosted by)\s*(?<serverName>[^\(]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex LatencyProgressRegex = new Regex(@"(Ping|Latency):", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DownloadProgressRegex = new Regex(@"Download:", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex UploadProgressRegex = new Regex(@"Upload:", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ConnectingRegex = new Regex(@"(Connecting to|Selecting server|Testing from)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public Main()
        {
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var pluginDirectory = assemblyLocation ?? AppContext.BaseDirectory;
            _cliPath = Path.Combine(pluginDirectory, "speedtest.exe");
        }

        public void Init(PluginInitContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(_context.API.GetCurrentTheme());
            
            // Load settings
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                var settingsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "settings.json");
                if (File.Exists(settingsPath))
                {
                    var settingsJson = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(settingsJson);
                    if (settings.ContainsKey("CopyToClipboard"))
                    {
                        _copyToClipboard = Convert.ToBoolean(settings["CopyToClipboard"]);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load settings: {ex.Message}");
                // Use default values if loading fails
            }
        }
        
        private void SaveSettings()
        {
            try
            {
                var settings = new Dictionary<string, object>
                {
                    ["CopyToClipboard"] = _copyToClipboard
                };
                
                var settingsJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                var settingsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "settings.json");
                File.WriteAllText(settingsPath, settingsJson);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();
            string trimmedQuery = query?.Search?.Trim().ToLowerInvariant() ?? string.Empty;

            if (_isRunningTest)
            {
                results.Add(new Result
                {
                    Title = "Speed test is currently running...",
                    SubTitle = "Please wait. Type 'spt cancel' to attempt to stop.",
                    IcoPath = _iconPath,
                    Score = 100,
                    Action = _ => false
                });
                if (trimmedQuery == "cancel" || "spt cancel".Contains(trimmedQuery))
                {
                    results.Add(new Result
                    {
                        Title = "Cancel Speed Test",
                        SubTitle = "Stops the current speed test.",
                        IcoPath = _iconPath,
                        Score = 110,
                        Action = _ =>
                        {
                            CancelSpeedTest();
                            return true;
                        }
                    });
                }
            }
            else
            {
                results.Add(new Result
                {
                    Title = "Run Speed Test",
                    SubTitle = "Test your internet connection speed (using Ookla Speedtest CLI)",
                    IcoPath = _iconPath,
                    Score = 100,
                    Action = _ =>
                    {
                        Task.Run(async () => await RunSpeedTestAsync());
                        return true;
                    }
                });
            }
            return results;
        }

        private void CancelSpeedTest(bool showNotifications = true)
        {
            if (_isRunningTest && _cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    _cancellationTokenSource.Cancel();
                    if (showNotifications)
                    {
                        _context?.API?.ShowMsg("Speed Test", "Cancellation requested for the speed test.", _iconPath);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Ignore if already disposed
                }
            }
            else if (!_isRunningTest && showNotifications)
            {
                _context?.API?.ShowMsg("Speed Test", "No speed test is currently running.", _iconPath);
            }
        }

        private void UpdateIconPath(Theme theme)
        {
            _iconPath = (theme == Theme.Light || theme == Theme.HighContrastWhite)
                ? "Images\\speedtest.light.png"
                : "Images\\speedtest.dark.png";
        }

        private void OnThemeChanged(Theme _, Theme newTheme) => UpdateIconPath(newTheme);

        private async Task RunSpeedTestAsync()
        {
            if (!File.Exists(_cliPath) && !IsSpeedtestInPath())
            {
                _context.API.ShowMsg("Speed Test Error", $"Speedtest CLI not found at '{_cliPath}' or in PATH.", _iconPath);
                return;
            }

            if (_isRunningTest) return;

            _isRunningTest = true;
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            Application.Current.Dispatcher.Invoke(() => _context.API.ChangeQuery("spt running..."));

            LoadingWindow loadingWindow = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                loadingWindow = new LoadingWindow();
                loadingWindow.Show();
                loadingWindow.UpdateStage(LoadingWindow.TestStage.Connecting);
                loadingWindow.UpdateDetail("Initializing speed test...");
            });

            var cliJsonOutput = new StringBuilder();
            var cliStdErrAggregator = new StringBuilder();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = File.Exists(_cliPath) ? _cliPath : "speedtest",
                    Arguments = "--accept-license --accept-gdpr --format=json",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using var process = new Process { StartInfo = psi };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        cliJsonOutput.AppendLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        string line = e.Data;
                        cliStdErrAggregator.AppendLine(line);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            loadingWindow?.UpdateCLIOutput(cliStdErrAggregator.ToString());

                            if (line.Contains("Download", StringComparison.OrdinalIgnoreCase) && 
                                !line.Contains("data used", StringComparison.OrdinalIgnoreCase))
                            {
                                var match = Regex.Match(line, @"Download:?\s*(\d+[\.,]?\d*)\s*Mbps", RegexOptions.IgnoreCase);
                                if (match.Success)
                                {
                                    loadingWindow?.UpdateCurrentSpeed($"{match.Groups[1].Value} Mbps ↓");
                                    loadingWindow?.UpdateStage(LoadingWindow.TestStage.Download);
                                    loadingWindow?.UpdateDetail("Testing download speed...");
                                }
                            }
                            else if (line.Contains("Upload", StringComparison.OrdinalIgnoreCase) && 
                                     !line.Contains("data used", StringComparison.OrdinalIgnoreCase))
                            {
                                var match = Regex.Match(line, @"Upload:?\s*(\d+[\.,]?\d*)\s*Mbps", RegexOptions.IgnoreCase);
                                if (match.Success)
                                {
                                    loadingWindow?.UpdateCurrentSpeed($"{match.Groups[1].Value} Mbps ↑");
                                    loadingWindow?.UpdateStage(LoadingWindow.TestStage.Upload);
                                    loadingWindow?.UpdateDetail("Testing upload speed...");
                                }
                            }
                            else if (line.Contains("Ping", StringComparison.OrdinalIgnoreCase) || 
                                     line.Contains("Latency", StringComparison.OrdinalIgnoreCase))
                            {
                                loadingWindow?.UpdateStage(LoadingWindow.TestStage.Latency);
                                loadingWindow?.UpdateDetail("Testing latency...");
                            }
                            else if (ConnectingRegex.IsMatch(line) || 
                                     line.Contains("Selecting server", StringComparison.OrdinalIgnoreCase))
                            {
                                loadingWindow?.UpdateStage(LoadingWindow.TestStage.Connecting);
                                loadingWindow?.UpdateDetail("Connecting to server...");

                                var serverMatch = ServerRegex.Match(line);
                                if (serverMatch.Success)
                                {
                                    loadingWindow?.UpdateServerInfo(serverMatch.Groups["serverName"].Value.Trim());
                                }
                            }
                            else if (line.Contains("Results", StringComparison.OrdinalIgnoreCase) || 
                                     line.Contains("URL", StringComparison.OrdinalIgnoreCase))
                            {
                                loadingWindow?.UpdateStage(LoadingWindow.TestStage.Complete);
                                loadingWindow?.UpdateDetail("Test complete!");
                            }
                        });
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync(token);

                if (token.IsCancellationRequested)
                {
                    Application.Current.Dispatcher.Invoke(() => loadingWindow?.Close());
                    _context.API.ShowMsg("Speed Test Canceled", "The speed test was canceled by the user.", _iconPath);
                    return;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    loadingWindow?.UpdateStage(LoadingWindow.TestStage.Complete);
                    loadingWindow?.UpdateDetail("Test finished. Processing results...");
                });
            }
            catch (OperationCanceledException)
            {
                Application.Current.Dispatcher.Invoke(() => loadingWindow?.Close());
                _context.API.ShowMsg("Speed Test Canceled", "The speed test operation was canceled.", _iconPath);
                return;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => loadingWindow?.Close());
                _context.API.ShowMsg("Speed Test Error", $"Error during speed test execution: {ex.Message}\nStderr: {cliStdErrAggregator}", _iconPath);
                return;
            }
            finally
            {
                _isRunningTest = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                Application.Current.Dispatcher.Invoke(() => _context.API.ChangeQuery("spt "));
            }

            Application.Current.Dispatcher.Invoke(() => loadingWindow?.Close());

            SpeedTestResult resultData = null;
            string finalJsonOutput = cliJsonOutput.ToString();

            if (string.IsNullOrWhiteSpace(finalJsonOutput))
            {
                _context.API.ShowMsg("Speed Test Error", "Speedtest CLI returned no output (empty JSON)." +
                                     (cliStdErrAggregator.Length > 0 ? $"\nStandard Error Output:\n{cliStdErrAggregator}" : ""), _iconPath);
                return;
            }

            try
            {
                resultData = ParseJsonOutput(finalJsonOutput);
                if (resultData == null)
                {
                    throw new InvalidDataException("Failed to parse JSON output (ParseJsonOutput returned null)." +
                                                  (cliStdErrAggregator.Length > 0 ? $"\nStandard Error Output:\n{cliStdErrAggregator}" : ""));
                }
            }
            catch (Exception ex)
            {
                _context.API.ShowMsg("Result Parsing Error", $"Failed to process speed test results: {ex.Message}\nRaw JSON (first 500 chars): {finalJsonOutput.Substring(0, Math.Min(500, finalJsonOutput.Length))}", _iconPath);
                #if DEBUG
                string fullErrorMsg = $"Failed to parse results: {ex.Message}\n\nRaw JSON:\n{finalJsonOutput}\n\nStderr:\n{cliStdErrAggregator}";
                Application.Current.Dispatcher.Invoke(() =>
                    MessageBox.Show(fullErrorMsg, "Detailed JSON Parse Error (DEBUG)", MessageBoxButton.OK, MessageBoxImage.Error)
                );
                #endif
                return;
            }

            if (resultData != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        string message = "Speed test completed successfully.";
                        
                        if (_copyToClipboard)
                        {
                            Clipboard.SetText(resultData.ToString());
                            message = "Detailed results copied to clipboard.";
                        }
                        
                        _context.API.ShowMsg("Speed Test Results", message, _iconPath);

                        var resultsWindow = new ResultsWindow(resultData);
                        resultsWindow.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        _context.API.ShowMsg("UI Error", $"Error displaying results window: {ex.Message}", _iconPath);
                        Debug.WriteLine($"ResultsWindow display or Clipboard error: {ex}");
                    }
                });
            }
        }

        private bool IsSpeedtestInPath()
        {
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "where";
                    process.StartInfo.Arguments = "speedtest.exe";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return !string.IsNullOrWhiteSpace(output) && output.Contains("speedtest.exe", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        }

        private SpeedTestResult ParseJsonOutput(string jsonOutput)
        {
            if (string.IsNullOrWhiteSpace(jsonOutput))
            {
                Debug.WriteLine("ParseJsonOutput: Received null or whitespace JSON.");
                throw new ArgumentException("JSON output cannot be null or whitespace.", nameof(jsonOutput));
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };
                var result = JsonSerializer.Deserialize<SpeedTestResult>(jsonOutput, options);

                if (result == null)
                {
                    throw new JsonException("Deserialization returned null. JSON might be valid but not matching the expected structure.");
                }

                result.UsingCliValues = true;
                return result;
            }
            catch (JsonException jsonEx)
            {
                Debug.WriteLine($"JSON Deserialization Error: {jsonEx.Message}. Path: {jsonEx.Path}, Line: {jsonEx.LineNumber}, Pos: {jsonEx.BytePositionInLine}");
                Debug.WriteLine($"Problematic JSON (first 1000 chars): {jsonOutput.Substring(0, Math.Min(1000, jsonOutput.Length))}");
                throw;
            }
        }

        public string GetTranslatedPluginTitle() => Name;
        public string GetTranslatedPluginDescription() => Description;

        public void Dispose()
        {
            try
            {
                CancelSpeedTest(showNotifications: false);
                
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }

                if (_context?.API != null)
                {
                    _context.API.ThemeChanged -= OnThemeChanged;
                }

                SaveSettings();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during plugin disposal: {ex}");
            }
            finally
            {
                _context = null;
            }

            GC.SuppressFinalize(this);
        }

        // ISettingProvider implementation
        public System.Windows.Controls.Control CreateSettingPanel()
        {
            var userControl = new System.Windows.Controls.UserControl();
            var stackPanel = new StackPanel();
            
            var checkBox = new CheckBox
            {
                Content = "Copy results to clipboard automatically",
                IsChecked = _copyToClipboard,
                Margin = new Thickness(10)
            };
            
            checkBox.Checked += (sender, e) =>
            {
                _copyToClipboard = true;
                SaveSettings();
            };
            
            checkBox.Unchecked += (sender, e) =>
            {
                _copyToClipboard = false;
                SaveSettings();
            };
            
            stackPanel.Children.Add(checkBox);
            userControl.Content = stackPanel;
            
            return userControl;
        }

        public IEnumerable<PluginAdditionalOption> AdditionalOptions
        {
            get
            {
                return new List<PluginAdditionalOption>
                {
                    new PluginAdditionalOption
                    {
                        Key = "CopyToClipboard",
                        DisplayLabel = "Copy results to clipboard automatically",
                        Value = _copyToClipboard,
                        PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox
                    }
                };
            }
        }

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            if (settings?.AdditionalOptions != null)
            {
                foreach (var option in settings.AdditionalOptions)
                {
                    if (option.Key == "CopyToClipboard")
                    {
                        _copyToClipboard = option.Value;
                        SaveSettings();
                        break;
                    }
                }
            }
        }
    }
}