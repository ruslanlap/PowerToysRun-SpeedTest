using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Community.PowerToys.Run.Plugin.SpeedTest
{
    public partial class SpeedTestPanel : Window
    {
        private readonly Gauge _download;
        private readonly Gauge _upload;
        private readonly Action _run;
        private readonly Action _cancel;
        private SpeedTestResult _result;

        public SpeedTestPanel(Action run, Action cancel)
        {
            InitializeComponent();
            _run = run;
            _cancel = cancel;
            if (SystemParameters.HighContrast)
            {
                Resources["PanelBrush"] = SystemColors.WindowBrush;
                Resources["TextBrush"] = SystemColors.WindowTextBrush;
                Resources["DimBrush"] = SystemColors.WindowTextBrush;
                Resources["TrackBrush"] = SystemColors.GrayTextBrush;
                Resources["AccentBrush"] = SystemColors.HighlightBrush;
            }
            _download = new Gauge(this, DownloadDial, "Download");
            _upload = new Gauge(this, UploadDial, "Upload");
        }

        public void BeginRun()
        {
            _result = null;
            RunAgain.IsEnabled = false;
            Details.IsEnabled = false;
            Dismiss.Content = "Cancel";
            Status.Text = "Connecting…";
            Summary.Text = "";
            Summary.ToolTip = null;
            _download.Set(null);
            _upload.Set(null);
        }

        public void UpdateProgress(double? download, double? upload, string status, string server)
        {
            Status.Text = status;
            Summary.Text = server;
            _download.Set(download);
            _upload.Set(upload);
        }

        public void Finish(SpeedTestResult result, string error = null)
        {
            _result = result;
            if (result != null)
            {
                _download.Set(result.DisplayDownloadSpeed);
                _upload.Set(result.DisplayUploadSpeed);
                var server = result.Server?.Name;
                var ping = result.Ping == null ? null : $"{result.Ping.Latency:F1} ms latency";
                Summary.Text = string.IsNullOrWhiteSpace(server) ? ping : ping == null ? server : $"{server} · {ping}";
                Summary.ToolTip = Summary.Text;
            }
            Status.Text = error ?? "Test complete";
            Dismiss.Content = "Close";
            RunAgain.IsEnabled = true;
            Details.IsEnabled = result != null;
        }

        private void RunAgain_Click(object sender, RoutedEventArgs e) => _run();
        private void Details_Click(object sender, RoutedEventArgs e) => new ResultsWindow(_result) { Owner = this }.ShowDialog();
        private void Dismiss_Click(object sender, RoutedEventArgs e) => Close();
        private void OnClosed(object sender, EventArgs e) => _cancel();

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close();
            }
        }

        private sealed class Gauge
        {
            private readonly TextBlock _reading;
            private readonly RotateTransform _needle = new RotateTransform(0, 74, 74);
            private readonly Brush _track;
            private readonly Line _line;
            private readonly Brush _accent;
            private readonly string _label;

            public Gauge(Window window, StackPanel host, string label)
            {
                _label = label;
                _track = (Brush)window.FindResource("TrackBrush");
                _accent = (Brush)window.FindResource("AccentBrush");
                host.Children.Add(new TextBlock { Text = label.ToUpperInvariant(), TextAlignment = TextAlignment.Center });
                var dial = new Grid { Width = 148, Height = 132, Margin = new Thickness(0, 8, 0, 0) };
                var face = new Canvas { IsHitTestVisible = false };
                face.Children.Add(new Path { Data = Geometry.Parse("M 29,119 A 64,64 0 1 1 119,119"), Stroke = _track, StrokeThickness = 3 });
                _line = new Line { X1 = 74, Y1 = 74, X2 = 29, Y2 = 119, Stroke = _track, StrokeThickness = 3, RenderTransform = _needle };
                face.Children.Add(_line);
                dial.Children.Add(face);
                _reading = new TextBlock { FontSize = 27, FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center };
                var labels = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                labels.Children.Add(_reading);
                labels.Children.Add(new TextBlock { Text = "Mbps", TextAlignment = TextAlignment.Center, Foreground = (Brush)window.FindResource("DimBrush") });
                dial.Children.Add(new Border
                {
                    Width = 110, Height = 58, CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 16, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                    Background = window.Background, Child = labels
                });
                host.Children.Add(dial);
                Set(null);
            }

            public void Set(double? mbps)
            {
                _reading.Text = mbps?.ToString("F1") ?? "—";
                AutomationProperties.SetName(_reading, mbps.HasValue ? $"{_label}: {_reading.Text} Mbps" : $"{_label}: not measured");
                _line.Stroke = mbps.HasValue ? _accent : _track;
                // ponytail: fixed logarithmic dial; the numeric reading stays exact above 1 Gbps.
                _needle.Angle = 270 * Math.Clamp(Math.Log10(1 + (mbps ?? 0)) / Math.Log10(1001), 0, 1);
            }
        }
    }
}
