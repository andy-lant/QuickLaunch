using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using QuickLaunch.Application;
using QuickLaunch.Core.Actions;
using QuickLaunch.Core.KeyEvents;
using QuickLaunch.Core.KeyEvents.KeyboardHook;
using QuickLaunch.Core.Logging;
using QuickLaunch.Core.Utils;
using QuickLaunch.Notification;
using QuickLaunch.UI.Utils;

namespace QuickLaunch;

internal partial class MainWindowVM : ObservableObject, IDisposable
{
    #region ----- Properties. -----

    private readonly AppService _appService;

    public AppService AppService => _appService;

    private readonly NotificationService _notificationService;

    public NotificationService NotificationService => _notificationService;

    #endregion

    #region ----- Observable Properties. -----

    [ObservableProperty]
    public string _keyStrokes = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CmdDispatcherInactive))]
    public bool _cmdDispatcherActive = true;

    public bool CmdDispatcherInactive => !CmdDispatcherActive;

    #endregion

    #region ----- Constructor. -----

    public MainWindowVM(MainWindow w)
    {
        _window = w;

        _notificationService = new(w.TopRight);

        _appService = AppService.Instance;
        _appService.MainWindowVM = this;

        var dispatcher = AppService.Dispatcher;
        KeyPressed += (obj, e) => dispatcher.KeyPressed(obj, e); // Subscribe to KeyPressed event

        AppService.Dispatcher.CommandDispatcherInvoked += DispatcherInvoked;

        AppService.ReloadConfiguration();

        dispatcher.EscapePressed += CmdDispatcher_Escape;
        dispatcher.CommandSequenceUpdated += CmdDispatcher_CommandSequenceUpdated;
        dispatcher.CommandSequenceCompleted += CmdDispatcher_CommandSequenceCompleted;

        _activationFallbackTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300) // Short delay (e.g., 300ms)
        };
        _activationFallbackTimer.Tick += ActivationFallbackTimer_Tick;
    }

    /// <summary>
    /// To be called after window is loaded.
    /// </summary>
    internal void Initialize()
    {
        _keyHook.HookInvoked += KeyHook;

        try { _keyHook.Install(); Log.Logger?.LogDebug("Keyboard hook installed successfully."); }
        catch (Exception ex)
        {
            Log.Logger?.LogDebug($"ERROR installing keyboard hook: {ex.Message}");
            MessageBox.Show(_window, $"Failed to install keyboard hook. Hotkeys may not work.\n{ex.Message}",
                "Hook Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        if (new WindowInteropHelper(_window).Handle != IntPtr.Zero)
        {
            _isAdjustingPosition = false;
            EnsureWindowBounds(true, true, true);
        }
        _window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
            new Action(() =>
            {
                // Ensure window is hidden after loading
                _window.Hide();
            }));
    }
    #endregion

    #region ----- Key Pressed Logic. -----

    private readonly LocalWindowsKeyboardHook _keyHook = new();

    public event EventHandler<KeyEventArgs> KeyPressed;
    private CommandDispatcher CmdDispatcher => AppService.Dispatcher;

    private bool _dispatchSuspended = false;

    internal void CancelSequence()
    {
        CmdDispatcher.CancelSequence();
    }

    private void CmdDispatcher_Escape(object? sender, EventArgs e)
    {
        _window.Hide();
    }

    private void CmdDispatcher_CommandSequenceCompleted(object? sender, EventArgs e)
    {
        this.Hide();
        // TODO: update UI with the completed sequence
    }

    private void CmdDispatcher_CommandSequenceUpdated(object? sender, SequenceEventArgs e)
    {
        if (e.IsCompleted)
        {
            KeyStrokes = "";

        }
        else if (e.IsAborted)
        {
            KeyStrokes = "";
            NotificationService.ShowNotification("Aborted sequence", e.SequenceDescription);
        }
        else
        {
            KeyStrokes = e.SequenceDescription;
        }
    }

    private void KeyHook(object? sender, HookEventArgs e)
    {
        if (e?.hotKey != null && e.hotKey.code == Key.Escape && e.hotKey.win)
        {
            Log.Logger?.LogDebug("Win+Q hotkey detected by hook.");
            ToggleWindowState();
            e.swallow = true;
        }
    }
    internal void OnKeyPressed(object? sender, KeyEventArgs e)
    {
        if (_dispatchSuspended || !_window.IsActive) return;

        KeyPressed?.Invoke(sender, e);
    }

    #endregion

    #region ----- Window Logic. -----

    private readonly MainWindow _window;

    internal MainWindow MainWindow => _window;

    private DispatcherTimer? _activationFallbackTimer; // Timer to check focus after showing

    private bool _isAdjustingPosition = true;

    public bool IsHidden() => _window.Visibility == Visibility.Hidden;

    // Backing field for CurrentWindowState
    [ObservableProperty]
    private WindowState _currentWindowState = WindowState.Normal;

    [ObservableProperty]
    private bool _isExiting = false;

    [RelayCommand]
    private void RestoreWindow()
    {
        // Request the window to restore to normal state.
        CurrentWindowState = WindowState.Normal;
    }

    [RelayCommand]
    private void MinimizeToTray()
    {
        CurrentWindowState = WindowState.Minimized;
    }

    [RelayCommand]
    public void ExitApplication()
    {
        IsExiting = true;
        MainWindow.Close();
    }

    // This method is called when the View's WindowState changes directly (e.g., user clicks minimize)
    internal void ViewWindowStateChanged(WindowState newWindowState)
    {
        if (CurrentWindowState != newWindowState)
        {
            CurrentWindowState = newWindowState;
        }
    }

    internal void Hide()
    {
        if (_window.IsVisible) // Only act if visible
        {
            Log.Logger?.LogTrace("VM.Hide() called. Hiding window.");
            _activationFallbackTimer?.Stop(); // Stop timer if it's running
            _window.Hide();
            CmdDispatcher.CancelSequence();
            KeyStrokes = ""; // Clear displayed keystrokes
            CmdDispatcherActive = true; // Reset to default UI state (showing KeyStrokes TextBlock)
        }
        else
        {
            Log.Logger?.LogTrace("VM.Hide() called, but window already hidden.");
        }
    }

    private void ToggleWindowState(bool forceShow = false)
    {
        Log.Logger?.LogTrace($"ToggleWindowState called. IsHidden: {IsHidden()}, forceShow: {forceShow}");
        if (IsHidden() || forceShow)
        {
            Log.Logger?.LogTrace("Window is hidden or forceShow=true. Attempting to show and activate.");

            // Determine which UI mode to be in.
            // If you want to always go to command input mode when window appears:
            CmdDispatcherActive = false; // This makes 'Command' TextBox visible and 'KeyStrokes' hidden.

            _window.Show();
            if (_window.WindowState == WindowState.Minimized)
            {
                _window.WindowState = WindowState.Normal;
            }

            bool activated = _window.Activate(); // Attempt to bring window to foreground and activate it
            Log.Logger?.LogTrace($"Window.Activate() called. Result: {activated}. Window IsActive: {_window.IsActive}, IsKeyboardFocusWithin: {_window.IsKeyboardFocusWithin}");

            // Try to focus the TextBox after a tiny delay to ensure window is ready and visible
            _window.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                var commandTextBox = _window.Command; // Assuming 'Command' is x:Name of your TextBox
                if (commandTextBox != null && commandTextBox.IsVisible && commandTextBox.IsEnabled)
                {
                    bool focusedProgrammatically = commandTextBox.Focus();
                    Keyboard.Focus(commandTextBox); // A more forceful way to set focus
                    Log.Logger?.LogTrace($"Attempted to focus Command TextBox. Programmatic focus result: {focusedProgrammatically}. TextBox IsKeyboardFocusWithin: {commandTextBox.IsKeyboardFocusWithin}. Keyboard.FocusedElement is '{Keyboard.FocusedElement?.GetType().Name}'");
                }
                else
                {
                    Log.Logger?.LogTrace("Command TextBox not available for focus (null, not visible, or not enabled).");
                    // As a fallback, try focusing the window itself if the textbox isn't focusable.
                    // This might not be desired if no control is meant to have focus.
                    // bool windowFocused = _window.Focus();
                    // Log.Logger?.LogTrace($"Command TextBox not focusable, tried focusing window. Result: {windowFocused}");
                }

                // Start the fallback timer AFTER attempting to show/activate/focus.
                // If the window fails to *stay* active/focused (e.g., Start Menu reclaims focus),
                // the timer will catch this and hide the window.
                _activationFallbackTimer?.Stop(); // Ensure it's stopped before starting
                _activationFallbackTimer?.Start();
                Log.Logger?.LogTrace("Activation fallback timer started.");
            }));
        }
        else
        {
            Log.Logger?.LogTrace("Window is visible. Hiding.");
            Hide(); // Use the central Hide method
        }
    }

    private void ActivationFallbackTimer_Tick(object? sender, EventArgs e)
    {
        _activationFallbackTimer?.Stop();
        Log.Logger?.LogTrace("ActivationFallbackTimer_Tick fired.");
        _window._traceFocus();

        if (_window == null || !_window.IsVisible)
        {
            Log.Logger?.LogTrace("Timer tick: Window is null or not visible. Doing nothing.");
            return;
        }

        if (!(_window.IsKeyboardFocused || _window.IsKeyboardFocusWithin))
        {
            Hide();
        }

        // Check if the focus is genuinely within our window.
        IInputElement? currentFocusedElement = Keyboard.FocusedElement;
        bool focusIsTrulyWithinWindow = false;
        if (currentFocusedElement is DependencyObject depObj)
        {
            // Corrected: Use IsAncestorOf on the window object, or check if depObj is the window itself.
            // VisualTreeHelper.IsDescendantOf is not the correct usage here.
            // Also, ensure depObj is not null before calling IsAncestorOf.
            if (depObj != null) // Add null check for depObj
            {
                focusIsTrulyWithinWindow = depObj == _window || _window.IsAncestorOf(depObj);
            }
        }

        Log.Logger?.LogTrace($"Timer Tick Focus Check: Window.IsActive={_window.IsActive}, focusIsTrulyWithinWindow={focusIsTrulyWithinWindow} (Keyboard.FocusedElement='{currentFocusedElement?.GetType().Name}')");

        // If the window is visible, but it's NOT the application's active window (e.g. another app is active)
        // AND the keyboard focus is NOT within this window or its children, then hide.
        if (!_window.IsActive && !focusIsTrulyWithinWindow)
        {
            Log.Logger?.LogWarning("Activation fallback: Window is visible but not active AND focus is not within it. Hiding window.");
            Hide();
        }
        else
        {
            Log.Logger?.LogTrace("Activation fallback: Window is active or focus is within it. Not hiding.");
        }
    }


    /// <summary>
    /// Called by MainWindow when it's confirmed to be active.
    /// This can stop the fallback timer.
    /// </summary>
    internal void WindowIsNowActive()
    {
        Log.Logger?.LogTrace("VM.WindowIsNowActive() called. Stopping activation fallback timer.");
        _activationFallbackTimer?.Stop();
    }

    #region --- Window Resize and Relocate. ---

    /// <summary>
    /// Space left to the right until screen's workarea.
    /// </summary>
    private const int WINDOW_SPACE_RIGHT = 2;

    /// <summary>
    /// Space left to the bottom until screen's workarea.
    /// </summary>
    private const int WINDOW_SPACE_BOTTOM = 2;

    /// <summary>
    /// Ensure correct size and placement.
    /// </summary>
    /// <param name="setMaximums">adjust maximum sizes</param>
    /// <param name="rightAlign">adjust right alignment</param>
    /// <param name="bottomAlign">adjust bottom alignment</param>
    internal void EnsureWindowBounds(bool setMaximums, bool rightAlign = false, bool bottomAlign = false)
    {
        if (_isAdjustingPosition) return;

        var screenData = ScreenHelpers.GetScreenDataForVisual(_window);
        screenData.Map((screenData) =>
        {
            _isAdjustingPosition = true;
            try
            {
                Rect workAreaDIP = screenData.WorkingAreaDIP;
                double workAreaWidthDIP = workAreaDIP.Width;
                double workAreaLeftDIP = workAreaDIP.Left;
                double workAreaRightDIP = workAreaDIP.Left + workAreaDIP.Width;
                double workAreaHeightDIP = workAreaDIP.Height;
                double workAreaTopDIP = workAreaDIP.Top;
                double workAreaBottomDIP = workAreaDIP.Top + workAreaDIP.Height;

                if (setMaximums)
                {
                    _window.MaxWidth = workAreaWidthDIP;
                    _window.MaxHeight = workAreaHeightDIP;
                }
                if (rightAlign)
                {
                    _window.Left = workAreaRightDIP - _window.ActualWidth - WINDOW_SPACE_RIGHT;
                }
                if (bottomAlign)
                {
                    _window.Top = workAreaBottomDIP - _window.ActualHeight - WINDOW_SPACE_BOTTOM;
                }
            }
            finally
            {
                _isAdjustingPosition = false;
                WindowLocationChanged(_window, new EventArgs());
            }
        });
    }

    public void WindowLocationChanged(object? sender, EventArgs e)
    {
        if (!_isAdjustingPosition)
        {
            NotificationService.SetBottomRightPoint(_window.TopRight);
        }
    }

    internal void DpiChanged(object? sender, EventArgs e)
    {
        EnsureWindowBounds(true, true, true);
    }

    #endregion


    #endregion

    #region ----- Actions. -----

    // --- Button Click Handler for Edit Config ---
    internal void OpenSettings()
    {
        AppService.OpenSettings();
    }

    private void DispatcherInvoked(object? sender, DispatcherInvokedEventArgs e)
    {
        if (e.ActionWasExecuted)
        {
            NotificationService.ShowNotification("Action executed", $"{e.Index}");
        }
        else
        {
            NotificationService.ShowNotification("Action aborted", $"No action for index {e.Index}");
        }
    }

    #endregion

    #region ----- Cleanup. -----

    internal void Shutdown()
    {
        Log.Logger?.LogDebug("Window closing. Unsubscribing and uninstalling keyboard hook.");

        _isAdjustingPosition = true; // Blocks any changes;

        AppService.Dispatcher.CommandDispatcherInvoked -= DispatcherInvoked;


        // Unsubscribe from events
        CmdDispatcher.EscapePressed -= CmdDispatcher_Escape;
        CmdDispatcher.CommandSequenceUpdated -= CmdDispatcher_CommandSequenceUpdated;
        CmdDispatcher.CommandSequenceCompleted -= CmdDispatcher_CommandSequenceCompleted;

        //// Unsubscribe from hook
        _keyHook.HookInvoked -= KeyHook;

        try
        {
            _keyHook.Uninstall();
            Log.Logger?.LogDebug("Keyboard hook uninstalled successfully.");
        }
        catch (Exception ex)
        {
            Log.Logger?.LogDebug($"ERROR uninstalling keyboard hook: {ex.Message}");
        }
    }

    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Shutdown();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
