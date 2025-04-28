using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace Community.PowerToys.Run.Plugin.SpeedTest
{
    public partial class LoadingWindow : Window
    {
        private readonly Storyboard _spinnerAnimation;

        // Етапи тесту
        public enum TestStage
        {
            Connecting = 0,
            Latency = 1,
            Download = 2,
            Upload = 3,
            Complete = 4
        }

        private TestStage _currentStage = TestStage.Connecting;

        public LoadingWindow()
        {
            InitializeComponent();

            // Отримуємо анімацію та запускаємо її
            _spinnerAnimation = (Storyboard)FindResource("SpinnerAnimation");
            _spinnerAnimation.Begin();

            // Початковий стан - етап підключення
            UpdateStage(TestStage.Connecting);
        }

        public void UpdateStatus(string status)
        {
            // Оновлення тексту статусу
            if (StatusText != null)
            {
                StatusText.Text = status;
            }
        }

        public void UpdateDetail(string detail)
        {
            // Оновлення детальної інформації
            if (DetailText != null)
            {
                DetailText.Text = detail;
            }
        }

        public void UpdateServerInfo(string serverName)
        {
            // Оновлення інформації про сервер
            if (ServerNameText != null)
            {
                ServerNameText.Text = serverName;
            }
        }

        public void UpdateCurrentSpeed(string speed)
        {
            // Оновлення інформації про поточну швидкість
            if (CurrentSpeedText != null)
            {
                CurrentSpeedText.Text = speed;
            }
        }

        public void UpdateStage(TestStage stage)
        {
            // Зберігаємо поточний етап
            _currentStage = stage;

            // Скидаємо всі індикатори до стандартного стилю
            ResetIndicators();

            // Оновлюємо індикатори етапів відповідно до поточного етапу
            switch (stage)
            {
                case TestStage.Connecting:
                    Step1Indicator.Style = (Style)FindResource("ActiveStepIndicator");
                    UpdateStatus("Connecting to server...");
                    break;

                case TestStage.Latency:
                    Step1Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                    Step2Indicator.Style = (Style)FindResource("ActiveStepIndicator");
                    UpdateStatus("Testing latency...");
                    break;

                case TestStage.Download:
                    Step1Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                    Step2Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                    Step3Indicator.Style = (Style)FindResource("ActiveStepIndicator");
                    UpdateStatus("Testing download speed...");
                    break;

                case TestStage.Upload:
                    Step1Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                    Step2Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                    Step3Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                    Step4Indicator.Style = (Style)FindResource("ActiveStepIndicator");
                    UpdateStatus("Testing upload speed...");
                    break;

                case TestStage.Complete:
                    Step1Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                    Step2Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                    Step3Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                    Step4Indicator.Style = (Style)FindResource("CompletedStepIndicator");
                    UpdateStatus("Testing complete!");
                    break;
            }
        }

        private void ResetIndicators()
        {
            // Встановлюємо всі індикатори в стандартний стиль
            Step1Indicator.Style = (Style)FindResource("StepIndicator");
            Step2Indicator.Style = (Style)FindResource("StepIndicator");
            Step3Indicator.Style = (Style)FindResource("StepIndicator");
            Step4Indicator.Style = (Style)FindResource("StepIndicator");
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Зупиняємо анімацію при закритті вікна
            _spinnerAnimation.Stop();
        }
    }
}