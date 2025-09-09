// File: LoadingWindow.xaml.cs
using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading;
using System.Windows.Input;

namespace Community.PowerToys.Run.Plugin.SpeedTest
{
    public partial class LoadingWindow : Window
    {
        private TestStage _currentStage = TestStage.Connecting;
        private DispatcherTimer _dotAnimationTimer;
        private bool _isCompleted = false;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public enum TestStage
        {
            Connecting = 0,
            Latency = 1,
            Download = 2,
            Upload = 3,
            Complete = 4,
            Error = 5
        }

        public LoadingWindow(CancellationTokenSource cancellationTokenSource = null)
        {
            InitializeComponent();
            _cancellationTokenSource = cancellationTokenSource;
            Loaded += LoadingWindow_Loaded;
            PreviewKeyDown += LoadingWindow_PreviewKeyDown;
        }

        private void LoadingWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _cancellationTokenSource?.Cancel();
                CloseWithAnimation();
                e.Handled = true;
            }
        }

        private void LoadingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize step indicators with proper transforms
                InitializeStepIndicators();

                _dotAnimationTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };
                _dotAnimationTimer.Tick += UpdateAnimations;
                _dotAnimationTimer.Start();

                UpdateStage(TestStage.Connecting);
                UpdateStatus("Connecting to server...");
                UpdateDetail("Initializing speed test...");
                UpdateCLIOutput("Starting Speedtest CLI...\n");
                UpdateCurrentSpeed("");
                UpdateServerInfo("");

                // Test stage indicator changes (for debugging)
                #if DEBUG
                TestStageIndicators();
                #endif

                // Animate window entrance
                AnimateWindowEntrance();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadingWindow Error: Initialization failed. {ex.Message}");
                // Show a simpler error message instead of the detailed one
                try
                {
                    this.Opacity = 1; // Ensure window is visible
                    MessageBox.Show("Error initializing speed test window. The test will continue without animations.", 
                                  "Animation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch
                {
                    // If even the message box fails, just continue
                }
            }
        }

        #if DEBUG
        private void TestStageIndicators()
        {
            // Test the stage indicators by cycling through them quickly
            var testTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            var stages = new[] { TestStage.Connecting, TestStage.Latency, TestStage.Download, TestStage.Upload, TestStage.Complete };
            var currentStageIndex = 0;

            testTimer.Tick += (s, e) =>
            {
                if (currentStageIndex < stages.Length)
                {
                    UpdateStage(stages[currentStageIndex]);
                    Debug.WriteLine($"Test: Set stage to {stages[currentStageIndex]}");
                    currentStageIndex++;
                }
                else
                {
                    testTimer.Stop();
                    // Reset to connecting for actual test
                    UpdateStage(TestStage.Connecting);
                    Debug.WriteLine("Stage indicator test completed");
                }
            };

            // Start test after a short delay
            var startTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            startTimer.Tick += (s, e) =>
            {
                startTimer.Stop();
                testTimer.Start();
            };
            startTimer.Start();
        }
        #endif

        private void InitializeStepIndicators()
        {
            try
            {
                var indicators = new[] { Step1Indicator, Step2Indicator, Step3Indicator, Step4Indicator };
                foreach (var indicator in indicators)
                {
                    if (indicator != null)
                    {
                        // Initialize with a proper ScaleTransform
                        indicator.RenderTransform = new ScaleTransform(1.0, 1.0);
                        indicator.RenderTransformOrigin = new Point(0.5, 0.5);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize step indicators: {ex.Message}");
            }
        }

        private void AnimateWindowEntrance()
        {
            try
            {
                this.Opacity = 0;

                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                var scaleTransform = new ScaleTransform(0.95, 0.95);
                this.RenderTransform = scaleTransform;
                this.RenderTransformOrigin = new Point(0.5, 0.5);

                var scaleX = new DoubleAnimation(0.95, 1, TimeSpan.FromMilliseconds(400))
                {
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                };

                var scaleY = new DoubleAnimation(0.95, 1, TimeSpan.FromMilliseconds(400))
                {
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                };

                this.BeginAnimation(OpacityProperty, fadeIn);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Animation error in AnimateWindowEntrance: {ex.Message}");
                // Fallback: just set opacity to 1
                this.Opacity = 1;
            }
        }

        private void UpdateAnimations(object sender, EventArgs e)
        {
            if (_currentStage == TestStage.Complete || _currentStage == TestStage.Error)
            {
                _dotAnimationTimer?.Stop();
                return;
            }

            // Add subtle pulsing effects to active indicators
            AnimateActiveIndicator();
        }

        private void AnimateActiveIndicator()
        {
            Border activeIndicator = null;

            switch (_currentStage)
            {
                case TestStage.Connecting:
                    activeIndicator = Step1Indicator;
                    break;
                case TestStage.Latency:
                    activeIndicator = Step2Indicator;
                    break;
                case TestStage.Download:
                    activeIndicator = Step3Indicator;
                    break;
                case TestStage.Upload:
                    activeIndicator = Step4Indicator;
                    break;
            }

            if (activeIndicator != null && activeIndicator.Style == FindResource("ActiveStepIndicator"))
            {
                var pulseAnimation = new DoubleAnimation(0.8, 1.0, TimeSpan.FromMilliseconds(800))
                {
                    AutoReverse = true,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                };

                activeIndicator.BeginAnimation(OpacityProperty, pulseAnimation);
            }
        }

        public TestStage GetCurrentStage() => _currentStage;

        public void UpdateStatus(string status)
        {
            Dispatcher.Invoke(() => 
            { 
                if (StatusText != null) 
                {
                    StatusText.Text = status;
                    AnimateTextChange(StatusText);
                }
            });
        }

        public void UpdateDetail(string detail)
        {
            Dispatcher.Invoke(() => 
            { 
                if (DetailText != null) 
                {
                    DetailText.Text = detail;
                    AnimateTextChange(DetailText);
                }
            });
        }

        public void UpdateServerInfo(string serverName) 
        {
            Dispatcher.Invoke(() => 
            { 
                if (ServerNameText != null) 
                {
                    ServerNameText.Text = serverName;
                    if (!string.IsNullOrEmpty(serverName))
                    {
                        AnimateTextChange(ServerNameText);
                    }
                }
            });
        }

        public void UpdateCurrentSpeed(string speed)
        {
            Dispatcher.Invoke(() => 
            { 
                if (CurrentSpeedText != null) 
                {
                    CurrentSpeedText.Text = speed;
                    if (!string.IsNullOrEmpty(speed))
                    {
                        AnimateSpeedUpdate();
                    }
                }
            });
        }

        private void AnimateSpeedUpdate()
        {
            try
            {
                // Create a new ScaleTransform to avoid frozen object issues
                var scaleTransform = new ScaleTransform(1.0, 1.0);
                CurrentSpeedText.RenderTransform = scaleTransform;
                CurrentSpeedText.RenderTransformOrigin = new Point(0.5, 0.5);

                var scaleUp = new DoubleAnimation(1, 1.1, TimeSpan.FromMilliseconds(150))
                {
                    AutoReverse = true,
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                };

                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUp);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUp);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Animation error in AnimateSpeedUpdate: {ex.Message}");
                // Fallback: continue without animation
            }
        }

        private void AnimateTextChange(TextBlock textBlock)
        {
            var fadeOut = new DoubleAnimation(1, 0.7, TimeSpan.FromMilliseconds(100));
            var fadeIn = new DoubleAnimation(0.7, 1, TimeSpan.FromMilliseconds(100));

            fadeOut.Completed += (s, e) => textBlock.BeginAnimation(OpacityProperty, fadeIn);
            textBlock.BeginAnimation(OpacityProperty, fadeOut);
        }

        public void UpdateCLIOutput(string output)
        {
            Dispatcher.Invoke(() =>
            {
                if (CLIOutputText != null)
                {
                    CLIOutputText.Text = output;
                    OutputScrollViewer?.ScrollToBottom();
                    CLIOutputText.UpdateLayout();
                    OutputScrollViewer?.UpdateLayout();
                }
            }, DispatcherPriority.Render);
        }

        public void UpdateStage(TestStage stage)
        {
            var previousStage = _currentStage;
            _currentStage = stage;

            Dispatcher.Invoke(() =>
            {
                try
                {
                    UpdateStageIndicators(stage);
                    UpdateProgressLine(stage);
                    UpdateCenterText(stage);

                    if (stage == TestStage.Complete && !_isCompleted)
                    {
                        _isCompleted = true;
                        ShowCompletionAnimation();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"LoadingWindow Error: Failed to update stage. {ex.Message}");
                }
            });
        }

        private void UpdateStageIndicators(TestStage stage)
        {
            try
            {
                // Reset all indicators first
                ResetIndicators();

                // Apply styles based on current stage
                switch (stage)
                {
                    case TestStage.Connecting:
                        if (Step1Indicator != null) 
                            Step1Indicator.Style = (Style)FindResource("ActiveStepIndicator");
                        break;

                    case TestStage.Latency:
                        if (Step1Indicator != null) 
                            Step1Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                        if (Step2Indicator != null) 
                            Step2Indicator.Style = (Style)FindResource("ActiveStepIndicator");
                        break;

                    case TestStage.Download:
                        if (Step1Indicator != null) 
                            Step1Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                        if (Step2Indicator != null) 
                            Step2Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                        if (Step3Indicator != null) 
                            Step3Indicator.Style = (Style)FindResource("ActiveStepIndicator");
                        break;

                    case TestStage.Upload:
                        if (Step1Indicator != null) 
                            Step1Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                        if (Step2Indicator != null) 
                            Step2Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                        if (Step3Indicator != null) 
                            Step3Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                        if (Step4Indicator != null) 
                            Step4Indicator.Style = (Style)FindResource("ActiveStepIndicator");
                        break;

                    case TestStage.Complete:
                        if (Step1Indicator != null) 
                            Step1Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                        if (Step2Indicator != null) 
                            Step2Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                        if (Step3Indicator != null) 
                            Step3Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                        if (Step4Indicator != null) 
                            Step4Indicator.Style = (Style)FindResource("CompletedStepIndicator");
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
                        break;
                }

                Debug.WriteLine($"Updated stage indicators to: {stage}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating stage indicators: {ex.Message}");
            }
        }

        private void AnimateStepCompletion(Border indicator)
        {
            try
            {
                // Create a new ScaleTransform to avoid frozen object issues
                var scaleTransform = new ScaleTransform(1.0, 1.0);
                indicator.RenderTransform = scaleTransform;
                indicator.RenderTransformOrigin = new Point(0.5, 0.5);

                var scaleAnimation = new DoubleAnimation(1, 1.3, TimeSpan.FromMilliseconds(200))
                {
                    AutoReverse = true,
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 }
                };

                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Animation error in AnimateStepCompletion: {ex.Message}");
                // Fallback: just apply the style without animation
            }
        }

        private void UpdateProgressLine(TestStage stage)
        {
            if (ProgressLine == null) return;

            double targetWidth = 0;
            var totalWidth = 380; // Approximate width between first and last indicator

            switch (stage)
            {
                case TestStage.Connecting:
                    targetWidth = 0;
                    break;
                case TestStage.Latency:
                    targetWidth = totalWidth * 0.33;
                    break;
                case TestStage.Download:
                    targetWidth = totalWidth * 0.66;
                    break;
                case TestStage.Upload:
                case TestStage.Complete:
                    targetWidth = totalWidth;
                    break;
                case TestStage.Error:
                    // Keep current width on error
                    return;
            }

            var widthAnimation = new DoubleAnimation(ProgressLine.Width, targetWidth, TimeSpan.FromMilliseconds(500))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            ProgressLine.BeginAnimation(WidthProperty, widthAnimation);
        }

        private void ShowCompletionAnimation()
        {
            if (CompletionSection != null)
            {
                try
                {
                    CompletionSection.Visibility = Visibility.Visible;

                    var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(600))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };

                    var slideUp = new DoubleAnimation(20, 0, TimeSpan.FromMilliseconds(600))
                    {
                        EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                    };

                    var transform = new TranslateTransform(0, 20);
                    CompletionSection.RenderTransform = transform;
                    CompletionSection.Opacity = 0;

                    CompletionSection.BeginAnimation(OpacityProperty, fadeIn);
                    transform.BeginAnimation(TranslateTransform.YProperty, slideUp);

                    // Stop the main spinner after a delay
                    var stopTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
                    stopTimer.Tick += (s, e) =>
                    {
                        stopTimer.Stop();
                    };
                    stopTimer.Start();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in ShowCompletionAnimation: {ex.Message}");
                    // Fallback: just show the section without animation
                    CompletionSection.Opacity = 1;
                    CompletionSection.RenderTransform = null;
                }
            }
        }

        private void ResetIndicators()
        {
            try
            {
                var defaultStyle = (Style)FindResource("StepIndicator");
                if (defaultStyle != null)
                {
                    if (Step1Indicator != null) Step1Indicator.Style = defaultStyle;
                    if (Step2Indicator != null) Step2Indicator.Style = defaultStyle;
                    if (Step3Indicator != null) Step3Indicator.Style = defaultStyle;
                    if (Step4Indicator != null) Step4Indicator.Style = defaultStyle;
                    Debug.WriteLine("Reset all indicators to default style");
                }
                else
                {
                    Debug.WriteLine("WARNING: StepIndicator default style not found");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadingWindow Error: Failed to reset indicators. {ex.Message}");
            }
        }

        public void ShowError(string errorMessage)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStage(TestStage.Error);
                UpdateStatus("❌ Error occurred");
                UpdateDetail(errorMessage);
                UpdateCurrentSpeed("");

                // Show error state in UI
                if (CompletionSection != null)
                {
                    CompletionTitleText.Text = "⚠️ Test Failed";
                    CompletionSubtitleText.Text = errorMessage;
                    CompletionSection.Visibility = Visibility.Visible;

                    // Change completion section to error styling
                    if (CompletionSection.Children.Count > 0 && CompletionSection.Children[0] is Border border)
                    {
                        border.Background = new SolidColorBrush(Color.FromArgb(26, 239, 68, 68)); // Semi-transparent red
                    }
                }
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            try
            {
                _dotAnimationTimer?.Stop();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadingWindow Error: Failed to stop animations on close. {ex.Message}");
            }
        }

        // Add smooth close animation
        public void CloseWithAnimation()
        {
            try
            {
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };

                var scaleTransform = new ScaleTransform(1.0, 1.0);
                this.RenderTransform = scaleTransform;
                this.RenderTransformOrigin = new Point(0.5, 0.5);

                var scaleDown = new DoubleAnimation(1, 0.95, TimeSpan.FromMilliseconds(300))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };

                fadeOut.Completed += (s, e) => Close();

                this.BeginAnimation(OpacityProperty, fadeOut);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleDown);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleDown);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Animation error in CloseWithAnimation: {ex.Message}");
                // Fallback: just close immediately
                Close();
            }
        }

        private void UpdateCenterText(TestStage stage)
        {
            if (CenterText == null) return;

            string text = stage switch
            {
                TestStage.Connecting => "Connecting to server...",
                TestStage.Latency => "Testing latency...",
                TestStage.Download => "Testing download...",
                TestStage.Upload => "Testing upload...",
                TestStage.Complete => "Complete!",
                TestStage.Error => "Error occurred",
                _ => "Initializing..."
            };

            CenterText.Text = text;
        }
    }
}