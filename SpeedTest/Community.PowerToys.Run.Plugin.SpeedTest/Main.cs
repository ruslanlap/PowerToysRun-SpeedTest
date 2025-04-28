using ManagedCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.SpeedTest
{
    public class Main : IPlugin, IPluginI18n, IDisposable
    {
        public static string PluginID => "5A0F7ED1D3F24B0A900732839D0E43DB";
        public string Name        => "SpeedTest";
        public string Description => "Run internet speed tests directly from PowerToys Run";

        private PluginInitContext _context;
        private bool _isRunningTest;
        private readonly string _cliPath;
        private string _iconPath;

        public Main()
        {
            var folder  = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                          ?? AppContext.BaseDirectory;
            _cliPath = Path.Combine(folder, "speedtest.exe");
        }

        public void Init(PluginInitContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(_context.API.GetCurrentTheme());
        }

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();

            if (_isRunningTest)
            {
                results.Add(new Result
                {
                    Title    = "Speed test is running…",
                    SubTitle = "Please wait",
                    IcoPath  = _iconPath,
                    Score    = 100,
                    Action   = _ => false
                });
            }
            else
            {
                results.Add(new Result
                {
                    Title    = "Run Speed Test",
                    SubTitle = "Test your internet connection speed",
                    IcoPath  = _iconPath,
                    Score    = 100,
                    Action   = _ =>
                    {
                        RunSpeedTest();
                        return true;
                    }
                });
            }

            return results;
        }

        private void UpdateIconPath(Theme theme)
        {
            _iconPath = (theme == Theme.Light || theme == Theme.HighContrastWhite)
                ? "Images\\speedtest.light.png"
                : "Images\\speedtest.dark.png";
        }

        private void OnThemeChanged(Theme _, Theme newTheme) =>
            UpdateIconPath(newTheme);

        private async void RunSpeedTest()
        {
            if (_isRunningTest) return;
            _isRunningTest = true;
            _context.API.ChangeQuery("speedtest running...");

            var loadingWindow = new LoadingWindow();
            loadingWindow.Show();

            var cliOutput = new StringBuilder();

            // Регекси для обробки прогресу
            var serverRx   = new Regex(@"(?:Hosted by|Server:)\s+(.+?)(?=\s*\(id|\s*:)",
                                       RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var latencyRx  = new Regex(@"Latency:\s+([\d\.]+)\s+ms",
                                       RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var downloadRx = new Regex(@"Download:\s+([\d\.]+)\s+Mbps",
                                       RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var uploadRx   = new Regex(@"Upload:\s+([\d\.]+)\s+Mbps",
                                       RegexOptions.Compiled | RegexOptions.IgnoreCase);

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = File.Exists(_cliPath) ? _cliPath : "speedtest",
                    Arguments              = "--accept-license --accept-gdpr",
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    CreateNoWindow         = true
                };

                using var proc = Process.Start(psi);

                // STDOUT
                proc.OutputDataReceived += (s, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    cliOutput.AppendLine(e.Data);
                    Application.Current.Dispatcher.Invoke(() =>
                        loadingWindow.UpdateCLIOutput(cliOutput.ToString()));
                };
                proc.BeginOutputReadLine();

                // STDERR (там теж прогрес)
                proc.ErrorDataReceived += (s, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    cliOutput.AppendLine(e.Data);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        loadingWindow.UpdateCLIOutput(cliOutput.ToString());

                        if (e.Data.Contains("Hosted by", StringComparison.OrdinalIgnoreCase) ||
                            e.Data.StartsWith("Server:", StringComparison.OrdinalIgnoreCase))
                        {
                            var m = serverRx.Match(e.Data);
                            if (m.Success)
                                loadingWindow.UpdateServerInfo(m.Groups[1].Value.Trim());

                            loadingWindow.UpdateStage(LoadingWindow.TestStage.Latency);
                            loadingWindow.UpdateDetail("Measuring connection latency...");
                        }
                        else if (e.Data.Contains("Latency:", StringComparison.OrdinalIgnoreCase))
                        {
                            var m = latencyRx.Match(e.Data);
                            if (m.Success)
                                loadingWindow.UpdateDetail($"Latency: {m.Groups[1].Value} ms");
                        }
                        else if (e.Data.Contains("Download:", StringComparison.OrdinalIgnoreCase)
                                 && !e.Data.Contains("data used", StringComparison.OrdinalIgnoreCase))
                        {
                            loadingWindow.UpdateStage(LoadingWindow.TestStage.Download);
                            var m = downloadRx.Match(e.Data);
                            if (m.Success)
                                loadingWindow.UpdateCurrentSpeed($"{m.Groups[1].Value} Mbps");
                        }
                        else if (e.Data.Contains("Upload:", StringComparison.OrdinalIgnoreCase))
                        {
                            loadingWindow.UpdateStage(LoadingWindow.TestStage.Upload);
                            var m = uploadRx.Match(e.Data);
                            if (m.Success)
                                loadingWindow.UpdateCurrentSpeed($"{m.Groups[1].Value} Mbps");
                        }
                        else if (e.Data.StartsWith("Result", StringComparison.OrdinalIgnoreCase) ||
                                 e.Data.StartsWith("Results", StringComparison.OrdinalIgnoreCase) ||
                                 e.Data.StartsWith("Share", StringComparison.OrdinalIgnoreCase))
                        {
                            loadingWindow.UpdateStage(LoadingWindow.TestStage.Complete);
                            loadingWindow.UpdateDetail("Test completed successfully!");
                        }
                    });
                };
                proc.BeginErrorReadLine();

                await proc.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                loadingWindow.Close();
                MessageBox.Show(
                    $"Error running speed test: {ex.Message}\nMake sure Speedtest CLI is installed or bundled.",
                    "Speed Test Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                _isRunningTest = false;
                _context.API.ChangeQuery("speedtest ");
                return;
            }

            loadingWindow.Close();

            var result = ParseCliOutput(cliOutput.ToString());

            Clipboard.SetText(result.ToString());
            var resultsWindow = new ResultsWindow(result);
            resultsWindow.ShowDialog();

            _isRunningTest = false;
            _context.API.ChangeQuery("speedtest ");
        }

        private SpeedTestResult ParseCliOutput(string output)
        {
            var res = new SpeedTestResult
            {
                Download = new SpeedTestResult.DownloadInfo(),
                Upload   = new SpeedTestResult.UploadInfo(),
                Ping     = new SpeedTestResult.PingInfo(),
                Server   = new SpeedTestResult.ServerInfo(),
                Result   = new SpeedTestResult.ResultInfo()
            };

            var dRx   = new Regex(@"Download:\s+([\d\.]+)\s+Mbps",
                                  RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var uRx   = new Regex(@"Upload:\s+([\d\.]+)\s+Mbps",
                                  RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var pRx   = new Regex(@"Latency:\s+([\d\.]+)\s+ms",
                                  RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var sRx   = new Regex(@"(?:Hosted by|Server:)\s+(.+?)(?=\s*\(id|\s*:)",
                                  RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var urlRx = new Regex(
                @"(?:Result\s*URL|Results|Share\s*results?):\s*(https?://[^\s]+)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            // **Обов’язково з InvariantCulture для крапки**
            if (dRx.Match(output) is var dm
             && dm.Success
             && double.TryParse(dm.Groups[1].Value,
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out var ds))
            {
                res.CliDownloadMbps    = ds;
                res.Download.Bandwidth = (long)(ds * 1_000_000 / 8);
            }

            if (uRx.Match(output) is var um
             && um.Success
             && double.TryParse(um.Groups[1].Value,
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out var us))
            {
                res.CliUploadMbps    = us;
                res.Upload.Bandwidth = (long)(us * 1_000_000 / 8);
            }

            if (pRx.Match(output) is var pm
             && pm.Success
             && double.TryParse(pm.Groups[1].Value,
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out var ps))
            {
                res.Ping.Latency = ps;
            }

            if (sRx.Match(output) is var sm && sm.Success)
            {
                res.Server.Name = sm.Groups[1].Value.Trim();
            }

            var m = urlRx.Match(output);
            if (m.Success)
            {
                res.Result.Url = m.Groups[1].Value.Trim().TrimEnd('.', ',', ')', ']');
            }
            else
            {
                // fallback — перший https://… 
                var any = new Regex(@"https?://[^\s]+", RegexOptions.Compiled)
                          .Match(output);
                if (any.Success)
                    res.Result.Url = any.Value.Trim().TrimEnd('.', ',', ')', ']');
            }

            // Покажемо у ResultsWindow CLI-значення, а не нулі
            res.UsingCliValues = true;
            return res;
        }

        public string GetTranslatedPluginTitle()       => Name;
        public string GetTranslatedPluginDescription() => Description;

        public void Dispose()
        {
            if (_context != null)
                _context.API.ThemeChanged -= OnThemeChanged;
            GC.SuppressFinalize(this);
        }
    }
}
