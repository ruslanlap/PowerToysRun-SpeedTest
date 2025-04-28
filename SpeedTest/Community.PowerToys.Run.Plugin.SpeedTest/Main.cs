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
    public class Main : IPlugin, IPluginI18n, IDisposable, IContextMenu
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

        private async void RunSpeedTest(string additionalArgs = "")
        {
            if (_isRunningTest) return;
            _isRunningTest = true;
            Context.API.ChangeQuery("speedtest running...");

            // Create and show loading window
            var loadingWindow = new LoadingWindow();
            loadingWindow.Show();

            try
            {
                loadingWindow.UpdateStage(LoadingWindow.TestStage.Connecting);
                loadingWindow.UpdateDetail("Finding optimal test server...");

                var cliOutputBuilder = new System.Text.StringBuilder();

                var psi = new ProcessStartInfo
                {
                    FileName = File.Exists(_cliPath) ? _cliPath : "speedtest",
                    Arguments = $"--format=json --accept-license --accept-gdpr {additionalArgs}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);

                // Create a buffer for collecting the complete JSON result
                var outputBuilder = new System.Text.StringBuilder();

                // For reading text output in real-time, create a separate process for CLI display
                var displayPsi = new ProcessStartInfo
                {
                    FileName = File.Exists(_cliPath) ? _cliPath : "speedtest",
                    Arguments = $"--accept-license --accept-gdpr {additionalArgs}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var displayProc = Process.Start(displayPsi);

                // Regular expressions for parsing output
                var latencyRegex = new Regex(@"Latency:\s+([\d\.]+)\s+ms", RegexOptions.Compiled);
                var downloadRegex = new Regex(@"Download:\s+([\d\.]+)\s+Mbps", RegexOptions.Compiled);
                var uploadRegex = new Regex(@"Upload:\s+([\d\.]+)\s+Mbps", RegexOptions.Compiled);
                var serverRegex = new Regex(@"Hosted by\s+(.+?)\s*:", RegexOptions.Compiled);

                // Set up event handlers to process output in real-time
                displayProc.OutputDataReceived += (sender, e) => 
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        // Add line to CLI output
                        cliOutputBuilder.AppendLine(e.Data);

                        Application.Current.Dispatcher.Invoke(() => 
                        {
                            // Update CLI output text
                            loadingWindow.UpdateCLIOutput(cliOutputBuilder.ToString());

                            // Update status with actual CLI output
                            if (e.Data.Contains("Hosted by"))
                            {
                                var match = serverRegex.Match(e.Data);
                                if (match.Success)
                                {
                                    var serverName = match.Groups[1].Value.Trim();
                                    loadingWindow.UpdateServerInfo(serverName);
                                }
                                loadingWindow.UpdateStage(LoadingWindow.TestStage.Latency);
                                loadingWindow.UpdateDetail("Measuring connection latency...");
                            }
                            else if (e.Data.Contains("Latency:"))
                            {
                                var match = latencyRegex.Match(e.Data);
                                if (match.Success)
                                {
                                    loadingWindow.UpdateDetail($"Latency: {match.Groups[1].Value} ms");
                                }
                            }
                            else if (e.Data.Contains("Download:"))
                            {
                                loadingWindow.UpdateStage(LoadingWindow.TestStage.Download);
                                var match = downloadRegex.Match(e.Data);
                                if (match.Success)
                                {
                                    loadingWindow.UpdateDetail("Measuring download speed...");
                                    loadingWindow.UpdateCurrentSpeed($"{match.Groups[1].Value} Mbps");
                                }
                            }
                            else if (e.Data.Contains("Upload:"))
                            {
                                loadingWindow.UpdateStage(LoadingWindow.TestStage.Upload);
                                var match = uploadRegex.Match(e.Data);
                                if (match.Success)
                                {
                                    loadingWindow.UpdateDetail("Measuring upload speed...");
                                    loadingWindow.UpdateCurrentSpeed($"{match.Groups[1].Value} Mbps");
                                }
                            }
                            else if (e.Data.Contains("Result"))
                            {
                                loadingWindow.UpdateStage(LoadingWindow.TestStage.Complete);
                                loadingWindow.UpdateDetail("Test completed successfully!");
                            }
                        });
                    }
                };

                displayProc.ErrorDataReceived += (sender, e) => 
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        cliOutputBuilder.AppendLine($"ERROR: {e.Data}");

                        Application.Current.Dispatcher.Invoke(() => 
                        {
                            loadingWindow.UpdateCLIOutput(cliOutputBuilder.ToString());
                            loadingWindow.UpdateDetail($"Error: {e.Data}");
                        });
                    }
                };

                // Start reading the output asynchronously
                displayProc.BeginOutputReadLine();
                displayProc.BeginErrorReadLine();

                // Collect the JSON output from the main process
                string jsonOutput = await proc.StandardOutput.ReadToEndAsync();
                outputBuilder.Append(jsonOutput);

                // Wait for both processes to complete
                await Task.WhenAll(proc.WaitForExitAsync(), displayProc.WaitForExitAsync());

                // Update final stage
                Application.Current.Dispatcher.Invoke(() => 
                {
                    loadingWindow.UpdateStage(LoadingWindow.TestStage.Complete);
                });

                // Wait a moment to show the completion status
                await Task.Delay(1000);

                // Close the loading window
                loadingWindow.Close();

                // Display final results
                DisplayResults(outputBuilder.ToString());
            }
            catch (Exception ex)
            {
                // Close the loading window in case of an error
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
                // Check if the output is empty or not JSON
                if (string.IsNullOrWhiteSpace(jsonOutput) || !jsonOutput.TrimStart().StartsWith("{"))
                {
                    MessageBox.Show(
                        "No valid results received from the speed test. The server may be busy or there might be network issues.",
                        "Speed Test Warning",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Try to parse the JSON data
                var options = new JsonSerializerOptions { 
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                var result = JsonSerializer.Deserialize<SpeedTestResult>(jsonOutput, options);

                if (result != null)
                {
                    // Format results text and copy to clipboard
                    var text = result.ToString();
                    Clipboard.SetText(text);

                    // Display results window
                    var window = new ResultsWindow(result);
                    window.ShowDialog();
                }
                else
                {
                    throw new InvalidOperationException("Could not parse JSON data");
                }
            }
            catch (JsonException ex)
            {
                MessageBox.Show(
                    $"Error parsing JSON results: {ex.Message}\n\nRaw output was:\n{(jsonOutput?.Length > 200 ? jsonOutput.Substring(0, 200) + "..." : jsonOutput)}",
                    "JSON Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error processing results: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public string GetTranslatedPluginTitle() => Name;
        public string GetTranslatedPluginDescription() => Description;

        public System.Collections.Generic.List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            if (selectedResult.ContextData is string resultUrl && !string.IsNullOrEmpty(resultUrl))
            {
                return new List<ContextMenuResult>
                {
                    new ContextMenuResult
                    {
                        PluginName = Name,
                        Title = "Copy result URL",
                        Action = _ =>
                        {
                            Clipboard.SetDataObject(resultUrl);
                            return true;
                        }
                    }
                };
            }
            return new List<ContextMenuResult>();
        }

        public void Dispose()
        {
            Context.API.ThemeChanged -= OnThemeChanged;
            GC.SuppressFinalize(this);
        }
    }
}