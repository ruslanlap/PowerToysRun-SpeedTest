using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Controls;

namespace Community.PowerToys.Run.Plugin.SpeedTest.UnitTests
{
    [TestClass]
    public class SpeedTestPanelTests
    {
        [STATestMethod]
        public void PanelTransitionsBetweenLiveResultAndRetry()
        {
            int reruns = 0;
            int closes = 0;
            var panel = new SpeedTestPanel(() => reruns++, () => closes++);
            var status = (TextBlock)panel.FindName("Status");
            var retry = (Button)panel.FindName("RunAgain");
            var details = (Button)panel.FindName("Details");

            panel.BeginRun();
            Assert.IsFalse(retry.IsEnabled);
            panel.UpdateProgress(125.5, null, "Measuring download…", "Test server");
            Assert.AreEqual("Measuring download…", status.Text);

            panel.Finish(new SpeedTestResult());
            Assert.AreEqual("Test complete", status.Text);
            Assert.IsTrue(details.IsEnabled);
            retry.RaiseEvent(new System.Windows.RoutedEventArgs(Button.ClickEvent));
            Assert.AreEqual(1, reruns);

            panel.BeginRun();
            panel.Finish(null, "Test canceled");
            Assert.AreEqual("Test canceled", status.Text);
            Assert.IsFalse(details.IsEnabled);
            Assert.IsTrue(retry.IsEnabled);
            panel.Close();
            Assert.AreEqual(1, closes);
        }
    }
}
