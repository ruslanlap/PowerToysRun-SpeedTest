// File: LoadingWindow.xaml.cs
using System;
using System.Diagnostics;
using System.Text; // Required for StringBuilder
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Community.PowerToys.Run.Plugin.SpeedTest
{
    public partial class LoadingWindow : Window
    {
        private Storyboard _spinnerAnimation;
        private TestStage _currentStage = TestStage.Connecting;
        private DispatcherTimer _dotAnimationTimer;
        private DispatcherTimer _statusUpdateTimer;
        private int _animationTick = 0;

        public enum TestStage
        {
            Connecting = 0, // Dot 1
            Latency = 1,    // Dot 2
            Download = 2,   // Dot 3
            Upload = 3,     // Dot 4
            Complete = 4,
            Error = 5
        }

        public LoadingWindow()
        {
            InitializeComponent();
            Loaded += LoadingWindow_Loaded;
        }

        private void LoadingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _spinnerAnimation = FindResource("SpinnerAnimation") as Storyboard;
                if (_spinnerAnimation != null)
                {
                    _spinnerAnimation.Begin(this, true);
                }
                else
                {
                    Debug.WriteLine("WARNING: SpinnerAnimation resource not found");
                }

                _dotAnimationTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                _dotAnimationTimer.Tick += AnimateStepIndicators;
                _dotAnimationTimer.Start();

                _statusUpdateTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                _statusUpdateTimer.Tick += SimulateStatusUpdate;
                _statusUpdateTimer.Start();

                UpdateStage(TestStage.Connecting);
                UpdateStatus("Connecting to server...");
                UpdateDetail("Initializing speed test...");
                UpdateCLIOutput("");
                UpdateCurrentSpeed("");
                UpdateServerInfo("");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadingWindow Error: Animation initialization failed. {ex.Message}");
                MessageBox.Show($"Error initializing animations: {ex.Message}", "Animation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SimulateStatusUpdate(object sender, EventArgs e)
        {
            _animationTick++;

            switch (_animationTick)
            {
                case 1:
                    UpdateStage(TestStage.Connecting);
                    UpdateStatus("Connecting to server...");
                    UpdateDetail("Latency: 3.06 ms");

                    StringBuilder cliBuilder = new StringBuilder();
                    cliBuilder.AppendLine("Speedtest by Ookla");
                    cliBuilder.AppendLine("Server: UARNet - Lviv (id: 14887)");
                    cliBuilder.AppendLine("ISP: State Enterprise Scientific and Telecommunicat");
                    cliBuilder.Append("Idle Latency: 3.06 ms (jitter: 0.36ms, low: 2.65ms, hi");
                    UpdateCLIOutput(cliBuilder.ToString());

                    UpdateCurrentSpeed("");
                    UpdateServerInfo("");
                    break;
                case 2:
                    UpdateStage(TestStage.Latency);
                    UpdateStatus("Latency Test...");
                    UpdateDetail("Latency: 2.85 ms"); 
                    break;
                case 3:
                    UpdateStage(TestStage.Download);
                    UpdateStatus("Download Test...");
                    UpdateDetail("Testing download speed...");
                    UpdateCurrentSpeed("95.47 Mbps ↓"); 
                    break;
                case 4:
                    UpdateStage(TestStage.Upload);
                    UpdateStatus("Upload Test...");
                    UpdateDetail("Testing upload speed...");
                    UpdateCurrentSpeed("87.32 Mbps ↑"); 
                    break;
                case 5:
                    UpdateStage(TestStage.Complete);
                    UpdateStatus("Test Complete!");
                    UpdateDetail("Results are ready.");
                    UpdateCurrentSpeed(""); 
                    _statusUpdateTimer.Stop();
                    break;
            }
        }

        private void AnimateStepIndicators(object sender, EventArgs e)
        {
            if (_currentStage == TestStage.Complete || _currentStage == TestStage.Error)
            {
                return;
            }
        }

        public TestStage GetCurrentStage() => _currentStage;

        public void UpdateStatus(string status)
        {
            Dispatcher.Invoke(() => { if (StatusText != null) StatusText.Text = status; });
        }

        public void UpdateDetail(string detail)
        {
            Dispatcher.Invoke(() => { if (DetailText != null) DetailText.Text = detail; });
        }

        public void UpdateServerInfo(string serverName) 
        {
            Dispatcher.Invoke(() => { if (ServerNameText != null) ServerNameText.Text = serverName; });
        }

        public void UpdateCurrentSpeed(string speed)
        {
            Dispatcher.Invoke(() => { if (CurrentSpeedText != null) CurrentSpeedText.Text = speed; });
        }

        public void UpdateCLIOutput(string output)
        {
            Dispatcher.Invoke(() =>
            {
                if (CLIOutputText != null)
                {
                    CLIOutputText.Text = output;
                    OutputScrollViewer?.ScrollToBottom();
                }
            });
        }

        public void UpdateStage(TestStage stage)
        {
            _currentStage = stage;
            Dispatcher.Invoke(() =>
            {
                try
                {
                    ResetIndicators();

                    switch (stage)
                    {
                        case TestStage.Connecting:
                            if (Step1Indicator != null) Step1Indicator.Style = (Style)FindResource("ActiveStepIndicator");
                            break;
                        case TestStage.Latency:
                            if (Step1Indicator != null) Step1Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                            if (Step2Indicator != null) Step2Indicator.Style = (Style)FindResource("ActiveStepIndicator");
                            break;
                        case TestStage.Download:
                            if (Step1Indicator != null) Step1Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                            if (Step2Indicator != null) Step2Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                            if (Step3Indicator != null) Step3Indicator.Style = (Style)FindResource("ActiveStepIndicator");
                            break;
                        case TestStage.Upload:
                            if (Step1Indicator != null) Step1Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                            if (Step2Indicator != null) Step2Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                            if (Step3Indicator != null) Step3Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                            if (Step4Indicator != null) Step4Indicator.Style = (Style)FindResource("ActiveStepIndicator");
                            break;
                        case TestStage.Complete:
                            if (Step1Indicator != null) Step1Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                            if (Step2Indicator != null) Step2Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                            if (Step3Indicator != null) Step3Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                            if (Step4Indicator != null) Step4Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                            _dotAnimationTimer?.Stop();
                            break;
                        case TestStage.Error:
                            var errorStyle = (Style)FindResource("ErrorStepIndicator");
                            if (errorStyle != null)
                            {
                                if (Step1Indicator != null) Step1Indicator.Style = errorStyle;
                                if (Step2Indicator != null) Step2Indicator.Style = errorStyle;
                                if (Step3Indicator != null) Step3Indicator.Style = errorStyle;
                                if (Step4Indicator != null) Step4Indicator.Style = errorStyle;
                            }
                            _dotAnimationTimer?.Stop();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"LoadingWindow Error: Failed to apply step indicator style. {ex.Message}");
                }
            });
        }

        private void ResetIndicators()
        {
            try
            {
                var defaultStyle = (Style)FindResource("StepIndicator");
                if (Step1Indicator != null) Step1Indicator.Style = defaultStyle;
                if (Step2Indicator != null) Step2Indicator.Style = defaultStyle;
                if (Step3Indicator != null) Step3Indicator.Style = defaultStyle;
                if (Step4Indicator != null) Step4Indicator.Style = defaultStyle;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadingWindow Error: Failed to reset indicators to default style. {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            try
            {
                _spinnerAnimation?.Stop(this);
                _dotAnimationTimer?.Stop();
                _statusUpdateTimer?.Stop();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadingWindow Error: Failed to stop animations on close. {ex.Message}");
            }
        }
    }
}