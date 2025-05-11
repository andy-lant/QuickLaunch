using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation; // Required for Storyboard
using System.Windows.Threading;

namespace QuickLaunch.Notification // Replace with your actual project namespace
{
    public partial class NotificationControl : UserControl
    {
        #region ----- Fields. -----

        private DispatcherTimer? _autoDismissTimer = null;

        private bool _isDismissing = false; // Flag to prevent multiple dismiss initiations

        #endregion

        #region ----- Properties. -----

        public string NotificationTitle { get; private set; }

        public string NotificationMessage { get; private set; }

        #endregion

        #region ----- Events. -----

        /// <summary>
        /// Event to notify the service to remove this control.
        /// </summary>
        public event EventHandler? DismissRequested = null;

        #endregion

        #region ----- Constructor. -----
        public NotificationControl(string title, string message)
        {
            InitializeComponent();

            NotificationTitle = title;
            NotificationMessage = message;

            if (this.FindName("TitleText") is TextBlock titleTextBlock)
            {
                titleTextBlock.Text = title;
            }
            if (this.FindName("MessageText") is TextBlock messageTextBlock)
            {
                messageTextBlock.Text = message;
            }

            Loaded += NotificationControl_Loaded;
        }
        #endregion

        #region ----- Event Handlers. -----

        /// <summary>
        /// Complete initialization of the control on loading.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NotificationControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Reset opacity and transform in case this control instance is reused
            this.Opacity = 1.0;
            if (this.RenderTransform is TranslateTransform tt)
            {
                tt.Y = 0;
            }

            _autoDismissTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3) // Notification display duration
            };
            _autoDismissTimer.Tick += AutoDismissTimer_Tick;
            _autoDismissTimer.Start();

            this.MouseLeftButtonDown += OnManualDismiss;
        }

        /// <summary>
        /// Handles manual dismissal of the notification when clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnManualDismiss(object? sender, MouseButtonEventArgs e)
        {
            RequestDismiss();
        }

        /// <summary>
        /// Handles the timer tick event for auto-dismissal of the notification.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoDismissTimer_Tick(object? sender, EventArgs e)
        {
            RequestDismiss();
        }

        #endregion

        #region ----- Internal Helpers. -----

        /// <summary>
        /// Initiates the dismiss animation. The DismissRequested event is raised after animation completes.
        /// </summary>
        public void RequestDismiss()
        {
            if (_isDismissing) // Prevent re-entrancy or multiple dismiss calls
            {
                return;
            }
            _isDismissing = true;

            if (_autoDismissTimer != null)
            {
                _autoDismissTimer.Stop();
                _autoDismissTimer.Tick -= AutoDismissTimer_Tick;
                _autoDismissTimer = null;
            }

            Storyboard? dismissAnimation = this.Resources["DismissAnimation"] as Storyboard;
            if (dismissAnimation != null)
            {
                // Clone the storyboard to allow it to be used independently if multiple notifications dismiss simultaneously
                Storyboard clonedAnimation = dismissAnimation.Clone();

                clonedAnimation.Completed += (s, eArgs) =>
                {
                    // Ensure event is raised only once
                    DismissRequested?.Invoke(this, EventArgs.Empty);
                    DismissRequested = null; // Prevent multiple invocations
                };
                clonedAnimation.Begin(this); // Apply animation to this UserControl instance
            }
            else
            {
                DismissRequested?.Invoke(this, EventArgs.Empty);
                DismissRequested = null;
            }
        }

        #endregion
    }
}
