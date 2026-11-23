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
using System.Linq;

namespace Community.PowerToys.Run.Plugin.SpeedTest
{
    public class Main : IPlugin, IPluginI18n, ISettingProvider, IDisposable
    {
        public static string PluginID => "5A0F7ED1D3F24B0A900732839D0E43DB";
        public string Name => "SpeedTest";
        public string Description => "Run internet speed tests with beautiful UI and real-time feedback.";

        private PluginInitContext _context;
        private bool _isRunningTest;
        private readonly string _cliPath;
        private string _iconPath;
        private CancellationTokenSource _cancellationTokenSource;
        private SpeedTestPanel _panel;

        // Settings
        private bool _copyToClipboard = false;
        private bool _showNotifications = true;
        private bool _saveHistory = true;
        private string _preferredServer = "";

        // Enhanced progress tracking
        private double _currentDownloadSpeed = 0;
        private double _currentUploadSpeed = 0;
        private double _currentLatency = 0;
        private string _currentServerName = "";
        private DateTime _testStartTime;

        // Regexes for parsing CLI output
        private static readonly Regex ServerRegex = new Regex(@"(Server:|Hosted by|Testing from)\s*(?<serverName>[^\(]+)(?:\((?<location>[^\)]+)\))?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex LatencyRegex = new Regex(@"(Ping|Latency):?\s*(?<latency>\d+[\.,]?\d*)\s*ms", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DownloadRegex = new Regex(@"Download:?\s*(?<speed>\d+[\.,]?\d*)\s*(Mbit/s|Mbps)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex UploadRegex = new Regex(@"Upload:?\s*(?<speed>\d+[\.,]?\d*)\s*(Mbit/s|Mbps)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex IdleLatencyRegex = new Regex(@"Idle Latency:?\s*(?<latency>\d+[\.,]?\d*)\s*ms", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
                        _copyToClipboard = Convert.ToBoolean(settings["CopyToClipboard"]);
                    if (settings.ContainsKey("ShowNotifications"))
                        _showNotifications = Convert.ToBoolean(settings["ShowNotifications"]);
                    if (settings.ContainsKey("SaveHistory"))
                        _saveHistory = Convert.ToBoolean(settings["SaveHistory"]);
                    if (settings.ContainsKey("PreferredServer"))
                        _preferredServer = settings["PreferredServer"].ToString();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load settings: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new Dictionary<string, object>
                {
                    ["CopyToClipboard"] = _copyToClipboard,
                    ["ShowNotifications"] = _showNotifications,
                    ["SaveHistory"] = _saveHistory,
                    ["PreferredServer"] = _preferredServer
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
                    Title = "🚀 Speed test in progress...",
                    SubTitle = $"Current: {GetCurrentTestInfo()} • Type 'cancel' to stop",
                    IcoPath = _iconPath,
                    Score = 100,
                    Action = _ => 
                    {
                        _panel?.Activate();
                        return false;
                    }
                });

                if (trimmedQuery.Contains("cancel") || trimmedQuery.Contains("stop"))
                {
                    results.Add(new Result
                    {
                        Title = "❌ Cancel Speed Test",
                        SubTitle = "Stop the currently running speed test",
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
                // Main speed test option
                results.Add(new Result
                {
                    Title = "🚀 Run Speed Test",
                    SubTitle = "Watch live download and upload speeds",
                    IcoPath = _iconPath,
                    Score = 100,
                    Action = _ =>
                    {
                        Application.Current.Dispatcher.InvokeAsync(OpenSpeedTestPanel);
                        return true;
                    }
                });

                // Quick test option
                if (string.IsNullOrEmpty(trimmedQuery) || "quick".Contains(trimmedQuery))
                {
                    results.Add(new Result
                    {
                        Title = "⚡ Quick Speed Test",
                        SubTitle = "Run a faster speed test with minimal UI",
                        IcoPath = _iconPath,
                        Score = 90,
                        Action = _ =>
                        {
                            Application.Current.Dispatcher.InvokeAsync(RunQuickSpeedTestAsync);
                            return true;
                        }
                    });
                }

                // History option (if enabled)
                if (_saveHistory && (string.IsNullOrEmpty(trimmedQuery) || "history".Contains(trimmedQuery)))
                {
                    results.Add(new Result
                    {
                        Title = "📊 View Test History",
                        SubTitle = "View your previous speed test results",
                        IcoPath = _iconPath,
                        Score = 80,
                        Action = _ =>
                        {
                            ShowTestHistory();
                            return true;
                        }
                    });
                }

                // Last result inline
                if (_saveHistory && (string.IsNullOrEmpty(trimmedQuery) || "last".Contains(trimmedQuery)))
                {
                    var lastResult = GetLastHistoryEntry();
                    if (lastResult.HasValue)
                    {
                        var (dl, ul, lat, ts, srv) = lastResult.Value;
                        results.Add(new Result
                        {
                            Title = $"🕒 Last: ⬇️ {dl:F1} Mbps  ⬆️ {ul:F1} Mbps  📡 {lat:F0} ms",
                            SubTitle = $"{ts:MMM dd, HH:mm}{(string.IsNullOrEmpty(srv) ? "" : $"  •  {srv}")} — Click to copy",
                            IcoPath = _iconPath,
                            Score = 75,
                            Action = _ =>
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                    Clipboard.SetText($"↓ {dl:F1} Mbps  ↑ {ul:F1} Mbps  {lat:F0} ms"));
                                return true;
                            }
                        });
                    }
                }

                // Stats inline
                if (_saveHistory && (string.IsNullOrEmpty(trimmedQuery) || "stats".Contains(trimmedQuery)))
                {
                    var stats = GetHistoryStats();
                    if (stats.HasValue)
                    {
                        var (avgDl, avgUl, bestDl, count) = stats.Value;
                        results.Add(new Result
                        {
                            Title = $"📈 Stats ({count} tests): avg ⬇️ {avgDl:F1}  ⬆️ {avgUl:F1} Mbps  best ⬇️ {bestDl:F1} Mbps",
                            SubTitle = "Aggregated from saved history",
                            IcoPath = _iconPath,
                            Score = 70,
                            Action = _ => false
                        });
                    }
                }
            }

            return results;
        }

        private string GetCurrentTestInfo()
        {
            var info = new List<string>();

            if (_currentDownloadSpeed > 0)
                info.Add($"↓ {_currentDownloadSpeed:F1} Mbps");
            if (_currentUploadSpeed > 0)
                info.Add($"↑ {_currentUploadSpeed:F1} Mbps");
            if (_currentLatency > 0)
                info.Add($"{_currentLatency:F0}ms");
            if (!string.IsNullOrEmpty(_currentServerName))
                info.Add(_currentServerName);

            return info.Count > 0 ? string.Join(" • ", info) : "Initializing...";
        }

        private void CancelSpeedTest(bool showNotifications = true)
        {
            if (_isRunningTest && _cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    _cancellationTokenSource.Cancel();
                    if (showNotifications && _showNotifications)
                    {
                        _context?.API?.ShowMsg("Speed Test", "🛑 Speed test canceled", _iconPath);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Ignore if already disposed
                }
            }
            else if (!_isRunningTest && showNotifications && _showNotifications)
            {
                _context?.API?.ShowMsg("Speed Test", "ℹ️ No speed test is currently running", _iconPath);
            }
        }

        private void UpdateIconPath(Theme theme)
        {
            _iconPath = (theme == Theme.Light || theme == Theme.HighContrastWhite)
                ? "Images\\speedtest.light.png"
                : "Images\\speedtest.dark.png";
        }

        private void OnThemeChanged(Theme _, Theme newTheme) => UpdateIconPath(newTheme);

        private async Task RunQuickSpeedTestAsync()
        {
            // Quick test with minimal UI - just show notification
            if (!File.Exists(_cliPath) && !IsSpeedtestInPath())
            {
                _context.API.ShowMsg("Speed Test Error", "❌ Speedtest CLI not found", _iconPath);
                return;
            }

            if (_isRunningTest) return;

            _isRunningTest = true;
            _testStartTime = DateTime.Now;

            try
            {
                if (_showNotifications)
                {
                    _context.API.ShowMsg("Speed Test", "🚀 Quick test started...", _iconPath);
                }

                var result = await ExecuteSpeedTestCLI();

                if (result != null)
                {
                    var duration = DateTime.Now - _testStartTime;
                    var message = $"⬇️ {result.Download?.Bandwidth / 125000:F1} Mbps ⬆️ {result.Upload?.Bandwidth / 125000:F1} Mbps 📍 {result.Ping?.Latency:F0}ms ({duration.TotalSeconds:F0}s)";

                    if (_showNotifications)
                    {
                        _context.API.ShowMsg("Speed Test Complete", message, _iconPath);
                    }

                    if (_copyToClipboard)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Clipboard.SetText(result.ToString());
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (_showNotifications)
                {
                    _context.API.ShowMsg("Speed Test Error", $"❌ Test failed: {ex.Message}", _iconPath);
                }
            }
            finally
            {
                _isRunningTest = false;
                ResetCurrentValues();
            }
        }

        private void OpenSpeedTestPanel()
        {
            if (_isRunningTest) return;
            if (_panel == null)
            {
                _panel = new SpeedTestPanel(
                    () => _ = RunSpeedTestAsync(_panel),
                    () =>
                    {
                        _panel = null;
                        CancelSpeedTest(showNotifications: false);
                    });
                _panel.Show();
            }
            else
            {
                _panel.Activate();
            }
            _ = RunSpeedTestAsync(_panel);
        }

        private async Task RunSpeedTestAsync(SpeedTestPanel panel)
        {
            if (_isRunningTest || panel == null) return;

            _isRunningTest = true;
            _testStartTime = DateTime.Now;
            using var cancellation = new CancellationTokenSource();
            _cancellationTokenSource = cancellation;
            panel.BeginRun();

            try
            {
                if (!File.Exists(_cliPath) && !IsSpeedtestInPath())
                {
                    throw new FileNotFoundException("Ookla Speedtest CLI not found.");
                }
                var result = await ExecuteSpeedTestCLI(panel, cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                if (_saveHistory)
                {
                    SaveTestResult(result);
                }
                panel.Finish(result);
                if (_copyToClipboard)
                {
                    Clipboard.SetText(result.ToString());
                }
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                if (ReferenceEquals(_panel, panel)) panel.Finish(null, "Test canceled");
            }
            catch (Exception ex)
            {
                if (ReferenceEquals(_panel, panel)) panel.Finish(null, ex.Message);
                if (_showNotifications)
                {
                    _context.API.ShowMsg("Speed Test Error", ex.Message, _iconPath);
                }
            }
            finally
            {
                _cancellationTokenSource = null;
                _isRunningTest = false;
                ResetCurrentValues();
            }
        }

        private async Task<SpeedTestResult> ExecuteSpeedTestCLI(SpeedTestPanel panel = null, CancellationToken token = default)
        {
            var cliStdErrAggregator = new StringBuilder();
            var cliRawOutput = new StringBuilder(); // For parsing final results

            string speedtestExe = File.Exists(_cliPath) ? _cliPath : "speedtest";
            
            // Use human-readable format for real-time progress, not JSON
            var arguments = "--accept-license --accept-gdpr --progress=yes";

            if (!string.IsNullOrEmpty(_preferredServer))
            {
                arguments += $" --server-id={_preferredServer}";
            }

            var psi = new ProcessStartInfo
            {
                FileName = speedtestExe,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi };
            token.ThrowIfCancellationRequested();
            process.Start();
            using var registration = token.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(true);
                    }
                }
                catch
                {
                }
            });

            void ReportProgress(string line)
            {
                if (panel == null)
                {
                    ParseProgressValues(line);
                    return;
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!token.IsCancellationRequested)
                    {
                        ParseProgressValues(line);
                        var status = line.Contains("upload", StringComparison.OrdinalIgnoreCase) || _currentUploadSpeed > 0
                            ? "Measuring upload…"
                            : line.Contains("download", StringComparison.OrdinalIgnoreCase) || _currentDownloadSpeed > 0
                                ? "Measuring download…"
                                : "Connecting…";
                        panel.UpdateProgress(_currentDownloadSpeed > 0 ? _currentDownloadSpeed : null,
                            _currentUploadSpeed > 0 ? _currentUploadSpeed : null, status, _currentServerName);
                    }
                });
            }

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data == null) return;
                cliRawOutput.AppendLine(e.Data);
                ReportProgress(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data == null) return;
                cliStdErrAggregator.AppendLine(e.Data);
                if (panel != null) ReportProgress(e.Data);
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync(token);
            token.ThrowIfCancellationRequested();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Ookla Speedtest CLI exited with code {process.ExitCode}.");
            }
            return ParseHumanReadableOutput(cliRawOutput.ToString(), cliStdErrAggregator.ToString());
        }

        private void ParseProgressValues(string line)
        {
            var downloadMatch = DownloadRegex.Match(line);
            if (downloadMatch.Success && double.TryParse(downloadMatch.Groups["speed"].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double downloadSpeed))
            {
                _currentDownloadSpeed = downloadSpeed;
            }

            var uploadMatch = UploadRegex.Match(line);
            if (uploadMatch.Success && double.TryParse(uploadMatch.Groups["speed"].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double uploadSpeed))
            {
                _currentUploadSpeed = uploadSpeed;
            }

            var latencyMatch = LatencyRegex.Match(line);
            if (latencyMatch.Success && double.TryParse(latencyMatch.Groups["latency"].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double latency))
            {
                _currentLatency = latency;
            }

            var serverMatch = ServerRegex.Match(line);
            if (serverMatch.Success)
            {
                _currentServerName = serverMatch.Groups["serverName"].Value.Trim();
            }
        }

        private void ResetCurrentValues()
        {
            _currentDownloadSpeed = 0;
            _currentUploadSpeed = 0;
            _currentLatency = 0;
            _currentServerName = "";
        }

        private bool IsSpeedtestInPath()
        {
            try
            {
                using var process = new Process();
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
            catch
            {
                return false;
            }
        }

        private SpeedTestResult ParseJsonOutput(string jsonOutput, string stderrOutput)
        {
            if (string.IsNullOrWhiteSpace(jsonOutput))
            {
                throw new ArgumentException("Speedtest CLI returned no output. " + 
                    (!string.IsNullOrEmpty(stderrOutput) ? $"Error: {stderrOutput}" : ""));
            }

            if (jsonOutput.Contains("Speedtest by Ookla is the official command line client") ||
                jsonOutput.Contains("Usage: speedtest"))
            {
                throw new InvalidDataException("Speedtest CLI returned help information instead of results. Please check your CLI installation.");
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<SpeedTestResult>(jsonOutput, options);

                if (result == null)
                {
                    throw new JsonException("Failed to parse speedtest results. The JSON structure may have changed.");
                }

                result.UsingCliValues = true;
                return result;
            }
            catch (JsonException jsonEx)
            {
                Debug.WriteLine($"JSON Parse Error: {jsonEx.Message}");
                Debug.WriteLine($"Raw JSON: {jsonOutput.Substring(0, Math.Min(1000, jsonOutput.Length))}");
                throw new JsonException($"Failed to parse speedtest results: {jsonEx.Message}");
            }
        }

        private SpeedTestResult ParseHumanReadableOutput(string rawOutput, string stderrOutput)
        {
            if (string.IsNullOrWhiteSpace(rawOutput))
            {
                throw new ArgumentException("Speedtest CLI returned no output. " + 
                    (!string.IsNullOrEmpty(stderrOutput) ? $"Error: {stderrOutput}" : ""));
            }

            var result = new SpeedTestResult();
            var lines = rawOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            double downloadSpeed = 0;
            double uploadSpeed = 0;
            double latency = 0;
            string serverName = "";
            string serverLocation = "";

            foreach (var line in lines)
            {
                // Parse server information
                var serverMatch = ServerRegex.Match(line);
                if (serverMatch.Success)
                {
                    serverName = serverMatch.Groups["serverName"].Value.Trim();
                    if (serverMatch.Groups["location"].Success)
                    {
                        serverLocation = serverMatch.Groups["location"].Value.Trim();
                    }
                }

                // Parse download speed
                var downloadMatch = DownloadRegex.Match(line);
                if (downloadMatch.Success && double.TryParse(downloadMatch.Groups["speed"].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedDownload))
                {
                    downloadSpeed = parsedDownload;
                }

                // Parse upload speed
                var uploadMatch = UploadRegex.Match(line);
                if (uploadMatch.Success && double.TryParse(uploadMatch.Groups["speed"].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedUpload))
                {
                    uploadSpeed = parsedUpload;
                }

                // Parse latency
                var latencyMatch = LatencyRegex.Match(line);
                if (latencyMatch.Success && double.TryParse(latencyMatch.Groups["latency"].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedLatency))
                {
                    latency = parsedLatency;
                }

                // Also try idle latency
                var idleLatencyMatch = IdleLatencyRegex.Match(line);
                if (idleLatencyMatch.Success && double.TryParse(idleLatencyMatch.Groups["latency"].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedIdleLatency))
                {
                    latency = parsedIdleLatency;
                }
            }

            // Create result object
            result.Download = new SpeedTestResult.DownloadInfo { Bandwidth = (long)(downloadSpeed * 125000) };
            result.Upload = new SpeedTestResult.UploadInfo { Bandwidth = (long)(uploadSpeed * 125000) };
            result.Ping = new SpeedTestResult.PingInfo { Latency = latency };
            result.Server = new SpeedTestResult.ServerInfo 
            { 
                Name = serverName, 
                Location = !string.IsNullOrEmpty(serverLocation) ? serverLocation : serverName 
            };
            result.UsingCliValues = true;

            return result;
        }

        private void SaveTestResult(SpeedTestResult result)
        {
            try
            {
                var historyPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "test_history.json");

                List<object> history = new List<object>();
                if (File.Exists(historyPath))
                {
                    var existingJson = File.ReadAllText(historyPath);
                    history = JsonSerializer.Deserialize<List<object>>(existingJson) ?? new List<object>();
                }

                var testRecord = new
                {
                    Timestamp = DateTime.Now,
                    Download = result.Download?.Bandwidth / 125000,
                    Upload = result.Upload?.Bandwidth / 125000,
                    Latency = result.Ping?.Latency,
                    Server = result.Server?.Name,
                    Location = result.Server?.Location
                };

                history.Add(testRecord);

                // Keep only last 50 tests
                if (history.Count > 50)
                {
                    history = history.Skip(history.Count - 50).ToList();
                }

                var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(historyPath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save test history: {ex.Message}");
            }
        }

        private (double dl, double ul, double lat, DateTime ts, string srv)? GetLastHistoryEntry()
        {
            try
            {
                var historyPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "test_history.json");
                if (!File.Exists(historyPath)) return null;
                var history = JsonSerializer.Deserialize<List<JsonElement>>(File.ReadAllText(historyPath));
                if (history == null || history.Count == 0) return null;
                var h = history[^1];
                return (h.GetProperty("Download").GetDouble(),
                        h.GetProperty("Upload").GetDouble(),
                        h.GetProperty("Latency").GetDouble(),
                        h.GetProperty("Timestamp").GetDateTime(),
                        h.TryGetProperty("Server", out var srv) ? srv.GetString() ?? "" : "");
            }
            catch { return null; }
        }

        private (double avgDl, double avgUl, double bestDl, int count)? GetHistoryStats()
        {
            try
            {
                var historyPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "test_history.json");
                if (!File.Exists(historyPath)) return null;
                var history = JsonSerializer.Deserialize<List<JsonElement>>(File.ReadAllText(historyPath));
                if (history == null || history.Count == 0) return null;
                var dls = history.Select(h => h.GetProperty("Download").GetDouble()).ToList();
                var uls = history.Select(h => h.GetProperty("Upload").GetDouble()).ToList();
                return (dls.Average(), uls.Average(), dls.Max(), history.Count);
            }
            catch { return null; }
        }

        private void ShowTestHistory()
        {
            try
            {
                var historyPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "test_history.json");
                if (File.Exists(historyPath))
                {
                    var json = File.ReadAllText(historyPath);
                    var history = JsonSerializer.Deserialize<List<JsonElement>>(json);

                    if (history?.Count > 0)
                    {
                        var recent = history.TakeLast(5);
                        var message = "📊 Recent Speed Tests:\n\n" + 
                            string.Join("\n", recent.Select(h => 
                            {
                                var timestamp = h.GetProperty("Timestamp").GetDateTime();
                                var download = h.GetProperty("Download").GetDouble();
                                var upload = h.GetProperty("Upload").GetDouble();
                                return $"🕒 {timestamp:MMM dd, HH:mm} - ⬇️ {download:F1} Mbps ⬆️ {upload:F1} Mbps";
                            }));

                        _context.API.ShowMsg("Speed Test History", message, _iconPath);
                    }
                    else
                    {
                        _context.API.ShowMsg("Speed Test History", "📭 No test history found. Run some tests first!", _iconPath);
                    }
                }
                else
                {
                    _context.API.ShowMsg("Speed Test History", "📭 No test history found. Run some tests first!", _iconPath);
                }
            }
            catch (Exception ex)
            {
                _context.API.ShowMsg("Error", $"❌ Failed to load history: {ex.Message}", _iconPath);
            }
        }

        public string GetTranslatedPluginTitle() => Name;
        public string GetTranslatedPluginDescription() => Description;

        public void Dispose()
        {
            try
            {
                CancelSpeedTest(showNotifications: false);

                if (_panel != null)
                {
                    _panel.Dispatcher.Invoke(() => _panel?.Close());
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

        // Enhanced settings panel
        public Control CreateSettingPanel()
        {
            var userControl = new UserControl();
            var stackPanel = new StackPanel();

            // Copy to clipboard setting
            var copyCheckBox = new CheckBox
            {
                Content = "📋 Copy results to clipboard automatically",
                IsChecked = _copyToClipboard,
                Margin = new Thickness(10)
            };
            copyCheckBox.Checked += (s, e) => { _copyToClipboard = true; SaveSettings(); };
            copyCheckBox.Unchecked += (s, e) => { _copyToClipboard = false; SaveSettings(); };

            // Show notifications setting
            var notificationsCheckBox = new CheckBox
            {
                Content = "🔔 Show notifications",
                IsChecked = _showNotifications,
                Margin = new Thickness(10)
            };
            notificationsCheckBox.Checked += (s, e) => { _showNotifications = true; SaveSettings(); };
            notificationsCheckBox.Unchecked += (s, e) => { _showNotifications = false; SaveSettings(); };

            // Save history setting
            var historyCheckBox = new CheckBox
            {
                Content = "📊 Save test history",
                IsChecked = _saveHistory,
                Margin = new Thickness(10)
            };
            historyCheckBox.Checked += (s, e) => { _saveHistory = true; SaveSettings(); };
            historyCheckBox.Unchecked += (s, e) => { _saveHistory = false; SaveSettings(); };

            stackPanel.Children.Add(copyCheckBox);
            stackPanel.Children.Add(notificationsCheckBox);
            stackPanel.Children.Add(historyCheckBox);
            userControl.Content = stackPanel;

            return userControl;
        }

        public IEnumerable<PluginAdditionalOption> AdditionalOptions =>
            new List<PluginAdditionalOption>
            {
                new()
                {
                    Key = "CopyToClipboard",
                    DisplayLabel = "Copy results to clipboard automatically",
                    Value = _copyToClipboard,
                    PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox
                },
                new()
                {
                    Key = "ShowNotifications", 
                    DisplayLabel = "Show notifications",
                    Value = _showNotifications,
                    PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox
                },
                new()
                {
                    Key = "SaveHistory",
                    DisplayLabel = "Save test history",
                    Value = _saveHistory,
                    PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox
                }
            };

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            if (settings?.AdditionalOptions != null)
            {
                foreach (var option in settings.AdditionalOptions)
                {
                    switch (option.Key)
                    {
                        case "CopyToClipboard":
                            _copyToClipboard = option.Value;
                            break;
                        case "ShowNotifications":
                            _showNotifications = option.Value;
                            break;
                        case "SaveHistory":
                            _saveHistory = option.Value;
                            break;
                    }
                }
                SaveSettings();
            }
        }
    }
}