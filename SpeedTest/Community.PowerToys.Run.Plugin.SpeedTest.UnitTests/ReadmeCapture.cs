using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Community.PowerToys.Run.Plugin.SpeedTest.UnitTests
{
    [TestClass]
    public class ReadmeCapture
    {
        [STATestMethod]
        public void RenderPanel()
        {
            var output = Environment.GetEnvironmentVariable("SPEEDTEST_CAPTURE_DIR");
            Directory.CreateDirectory(output);
            var panel = new SpeedTestPanel(() => { }, () => { });
            panel.BeginRun();
            panel.UpdateProgress(312.4, 68.7, "Measuring upload…", "Example server");
            Save(panel, Path.Combine(output, "omarchy-panel-live.png"));
            panel.Finish(new SpeedTestResult
            {
                Download = new SpeedTestResult.DownloadInfo { Bandwidth = 39050000 },
                Upload = new SpeedTestResult.UploadInfo { Bandwidth = 10687500 },
                Ping = new SpeedTestResult.PingInfo { Latency = 13.2 },
                Server = new SpeedTestResult.ServerInfo { Name = "Example server" }
            });
            Save(panel, Path.Combine(output, "omarchy-panel-result.png"));
            panel.Close();
        }

        private static void Save(SpeedTestPanel panel, string path)
        {
            var content = (FrameworkElement)panel.Content;
            double width = panel.Width - 2 * SystemParameters.FixedFrameVerticalBorderWidth;
            content.Measure(new Size(width, double.PositiveInfinity));
            var size = new Size(width, Math.Ceiling(content.DesiredSize.Height));
            content.Arrange(new Rect(size));
            content.UpdateLayout();
            Assert.IsTrue(size.Height > 200);
            var drawing = new DrawingVisual();
            using (var dc = drawing.RenderOpen())
            {
                dc.DrawRectangle(panel.Background, null, new Rect(size));
                dc.DrawRectangle(new VisualBrush(content), null, new Rect(size));
            }
            var image = new RenderTargetBitmap((int)Math.Ceiling(size.Width * 2), (int)size.Height * 2, 192, 192, PixelFormats.Pbgra32);
            image.Render(drawing);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using var stream = File.Create(path);
            encoder.Save(stream);
        }
    }
}
