using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives; // For Popup
using Microsoft.Win32;
using QuickLaunch.UI.Utils; // For SystemEvents

namespace QuickLaunch.Notification;

public class NotificationService : IDisposable
{
    #region ----- Constants. -----

    private const int POPUP_WIDTH = 200;    // Fixed width for the popup/stackpanel area

    #endregion

    #region ----- Fields. -----

    private Popup _notificationHostPopup;

    private StackPanel _notificationStackPanel;

    private readonly List<NotificationControl> _activeNotificationControls = new();

    private Point _bottomRight;

    #endregion

    #region ----- Constructor. -----

    /// <summary>
    /// Constructor.
    /// </summary>
    public NotificationService(Point bottomRight)
    {
        _bottomRight = bottomRight;
        _notificationHostPopup = null!; // Initialized later.
        _notificationStackPanel = null!; // Initialized later.
        InitializePopup();
    }

    /// <summary>
    /// Initialization helper.
    /// </summary>
    private void InitializePopup()
    {
        _notificationStackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
        };

        _notificationHostPopup = new Popup
        {
            AllowsTransparency = true,
            Placement = PlacementMode.Custom,
            StaysOpen = true,
            Child = new Border
            {
                Width = POPUP_WIDTH,
                Child = _notificationStackPanel
            },
            PopupAnimation = PopupAnimation.Slide,
            Focusable = false,
            IsOpen = false
        };

        _notificationHostPopup.CustomPopupPlacementCallback = GetPopupPlacement;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    #endregion

    #region ----- Public Methods. -----

    /// <summary>
    /// Set the bottom right point for the popup.
    /// </summary>
    /// <param name="bottomRightPoint"></param>
    public void SetBottomRightPoint(Point bottomRightPoint)
    {
        var screenData = ScreenHelpers.GetScreenDataForPoint(bottomRightPoint, visual: _notificationHostPopup);

        _bottomRight = new Point(
            bottomRightPoint.X * screenData.DpiScale.DpiScaleX,
            bottomRightPoint.Y * screenData.DpiScale.DpiScaleY
        );

        _notificationHostPopup.HorizontalOffset += 0.001;
        _notificationHostPopup.HorizontalOffset -= 0.001;
    }

    /// <summary>
    /// Shows a notification with the specified title and message.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="message"></param>
    public void ShowNotification(string title, string message)
    {
        if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.Invoke(() => ShowNotificationInternal(title, message));
        }
        else
        {
            ShowNotificationInternal(title, message);
        }
    }

    #endregion

    #region ----- Event Handlers. -----

    /// <summary>
    /// Handles the DismissRequested event from NotificationControl.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnNotificationDismissRequested(object? sender, EventArgs e)
    {
        if (sender is NotificationControl controlToRemove)
        {
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => RemoveNotification(controlToRemove));
            }
            else
            {
                RemoveNotification(controlToRemove);
            }
        }
    }

    /// <summary>
    /// Handles display settings changes (like screen resolution changes).
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        if (_notificationHostPopup != null && _notificationHostPopup.IsOpen)
        {
            _notificationHostPopup.HorizontalOffset += 0.001;
            _notificationHostPopup.HorizontalOffset -= 0.001;
        }
    }

    #endregion


    #region ----- Internal Helpers. -----

    /// <summary>
    /// Custom placement callback for the Popup.
    /// </summary>
    /// <param name="popupSize"></param>
    /// <param name="targetSize"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    private CustomPopupPlacement[] GetPopupPlacement(Size popupSize, Size targetSize, Point offset)
    {
        Point topLeft = new Point(
            _bottomRight.X - popupSize.Width,
            _bottomRight.Y - popupSize.Height
        );
        return new[] { new CustomPopupPlacement(topLeft, PopupPrimaryAxis.Vertical) };
    }

    /// <summary>
    /// Shows a notification with the specified title and message.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="message"></param>
    private void ShowNotificationInternal(string title, string message)
    {
        if (_notificationHostPopup == null || _notificationStackPanel == null)
        {
            return;
        }

        var notificationControl = new NotificationControl(title, message);
        notificationControl.DismissRequested += OnNotificationDismissRequested;

        _notificationStackPanel.Children.Insert(0, notificationControl);
        _activeNotificationControls.Add(notificationControl);

        if (!_notificationHostPopup.IsOpen && _notificationStackPanel.Children.Count > 0)
        {
            _notificationHostPopup.IsOpen = true;
        }
        _notificationHostPopup.HorizontalOffset += 0.001;
        _notificationHostPopup.HorizontalOffset -= 0.001;
    }

    /// <summary>
    /// Removes a notification control from the stack panel and cleans up resources.
    /// </summary>
    /// <param name="controlToRemove"></param>
    private void RemoveNotification(NotificationControl controlToRemove)
    {
        if (_notificationStackPanel == null || _notificationHostPopup == null) return;

        controlToRemove.DismissRequested -= OnNotificationDismissRequested;

        _notificationStackPanel.Children.Remove(controlToRemove);
        _activeNotificationControls.Remove(controlToRemove);

        if (_notificationStackPanel.Children.Count == 0 && _notificationHostPopup.IsOpen)
        {
            _notificationHostPopup.IsOpen = false;
        }
        if (_notificationHostPopup.IsOpen)
        {
            _notificationHostPopup.HorizontalOffset += 0.001;
            _notificationHostPopup.HorizontalOffset -= 0.001;
        }
    }

    #endregion


    #region ----- IDisposable. -----

    /// <summary>
    /// Disposes the NotificationService and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the NotificationService and cleans up resources.
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(CleanupPopup);
            }
            else
            {
                CleanupPopup();
            }
        }
    }

    private void CleanupPopup()
    {
        if (_notificationHostPopup != null)
        {
            _notificationHostPopup.IsOpen = false;
            _notificationHostPopup.Child = null;
            _notificationHostPopup.CustomPopupPlacementCallback = null;
            _notificationHostPopup = null!;
        }

        if (_notificationStackPanel != null)
        {
            foreach (var control in _activeNotificationControls.ToList())
            {
                control.DismissRequested -= OnNotificationDismissRequested;
            }
            _notificationStackPanel.Children.Clear();
            _notificationStackPanel = null!;
        }
        _activeNotificationControls.Clear();
    }

    #endregion
}
