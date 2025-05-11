#nullable enable

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input; // Use specific using for KeyEventArgs
using System.Windows.Interop;
using Microsoft.Extensions.Logging;
using QuickLaunch.Core.Logging;
using QuickLaunch.Core.Utils;

namespace QuickLaunch;

/// <summary>
/// The application's main window.
/// </summary>
public partial class MainWindow : Window
{
    #region ----- Properties. -----

    /// <summary>
    /// The view model.
    /// </summary>
    internal MainWindowVM Model { get; }

    /// <summary>
    /// System tray notify icon.
    /// </summary>
    internal NotifyIcon? NotifyIcon { get; private set; }


    // Used for placing notification popup.
    public System.Windows.Point TopRight => new(Left + Width, Top);

    #endregion

    #region ----- Constructors. -----

    /// <summary>
    /// Constructor.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        Model = new(this);
        DataContext = Model;

        InitializeNotifyIcon();
    }

    private void InitializeNotifyIcon()
    {
        NotifyIcon = new();
        NotifyIcon.Visible = true; // start hidden
        try
        {
            var iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/QuickLaunch.ico"))?.Stream;
            if (iconStream != null)
            {
                NotifyIcon.Icon = new(iconStream);
                iconStream.Dispose();
            }
            else
            {
                NotifyIcon.Icon = SystemIcons.Application;
                Log.Logger?.LogError($"Application icon not found. Using default system icon.");
            }
        }
        catch (Exception ex)
        {
            Log.Logger?.LogError(ex, $"Error loading application icon. Using default system icon.");
            NotifyIcon.Icon = SystemIcons.Application;
        }
        NotifyIcon.DoubleClick += (s, args) => Model.RestoreWindowCommand.Execute(null);

        var contextMenu = new ContextMenuStrip();

        contextMenu.Items.Add("Restore", null, (s, args) => Model.RestoreWindowCommand.Execute(null));
        contextMenu.Items.Add("Exit", null, (s, args) => Model.ExitApplicationCommand.Execute(null));

        NotifyIcon.ContextMenuStrip = contextMenu;
    }

    #endregion

    #region ----- Event Handlers. -----

    #region --- Window Event Handlers. ---

    // -- Initialization. --

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Model.Initialize();
    }

    private void MainWindow_ContentRendered(object? sender, EventArgs e)
    {
        // Unsubscribe from ContentRendered as we only need it once if Loaded didn't work
        this.ContentRendered -= MainWindow_ContentRendered;
        // Ensure handle is available
        if (new WindowInteropHelper(this).Handle != IntPtr.Zero)
        {
            Model.EnsureWindowBounds(true, true, true); // Set initial constraints
        }
    }

    // -- Size and Location changes. --

    protected override void OnLocationChanged(EventArgs e)
    {
        Model.EnsureWindowBounds(true, true, true);
        base.OnLocationChanged(e);
    }


    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Model.EnsureWindowBounds(false);
    }

    // -- Hide/Unhide/Focus. --

    internal void _traceFocus()
    {
        // Helper to log current focus state from the window's perspective
        Log.Logger?.LogTrace($"Window Focus State: IsActive={this.IsActive}, IsFocused={this.IsFocused}, IsKeyboardFocused={this.IsKeyboardFocused}, IsKeyboardFocusWithin={this.IsKeyboardFocusWithin}, HasEffectiveKeyboardFocus={this.HasEffectiveKeyboardFocus}");
        IInputElement? focusedElement = Keyboard.FocusedElement;
        string focusedElementName = "null";
        if (focusedElement is FrameworkElement fe)
        {
            focusedElementName = fe.Name ?? fe.GetType().Name;
        }
        else if (focusedElement != null)
        {
            focusedElementName = focusedElement.GetType().Name;
        }
        Log.Logger?.LogTrace($"Global Keyboard.FocusedElement: {focusedElementName}");
    }

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        Log.Logger?.LogTrace("Window OnGotKeyboardFocus.");
        _traceFocus();
        base.OnGotKeyboardFocus(e);
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        Log.Logger?.LogTrace("Window OnLostKeyboardFocus.");
        _traceFocus();

        // Check if the new focus is outside this window.
        // This helps prevent hiding if focus moves to a child control or a popup owned by this window.
        bool newFocusIsOutside = true;
        if (e.NewFocus is DependencyObject newFocusElement)
        {
            if (newFocusElement == this || this.IsAncestorOf(newFocusElement))
            {
                newFocusIsOutside = false;
            }
        }

        if (newFocusIsOutside)
        {
            Log.Logger?.LogTrace("Lost keyboard focus to an element outside the window. Hiding.");
            Model.Hide();
        }
        else
        {
            Log.Logger?.LogTrace("Lost keyboard focus, but new focus is within the window or its descendants. Not hiding via OnLostKeyboardFocus.");
        }
        base.OnLostKeyboardFocus(e);
    }

    protected override void OnIsKeyboardFocusedChanged(DependencyPropertyChangedEventArgs e)
    {
        Log.Logger?.LogTrace($"Window OnIsKeyboardFocusedChanged: {e.NewValue}.");
        _traceFocus();
        base.OnIsKeyboardFocusedChanged(e);
    }

    protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
    {
        Log.Logger?.LogTrace($"Window OnIsKeyboardFocusWithinChanged: {e.NewValue}.");
        _traceFocus();
        base.OnIsKeyboardFocusWithinChanged(e);
    }


    private void Window_Activated(object sender, EventArgs e)
    {
        Log.Logger?.LogTrace("Window activated.");
        _traceFocus();
        Model.CancelSequence(); // Existing logic
        Model.WindowIsNowActive(); // Notify VM that window is confirmed active
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        Log.Logger?.LogTrace("Window deactivated. Hiding.");
        _traceFocus();
        Model.Hide();
    }


    private void MainWindow_DpiChanged(object sender, System.Windows.DpiChangedEventArgs e)
    {
        Model.DpiChanged(sender, e);

    }

    private void Window_KeyDown(object? sender, System.Windows.Input.KeyEventArgs e)
    {
        Model.OnKeyPressed(sender, e);
    }

    // -- Closing. --

    protected override void OnClosing(CancelEventArgs e)
    {
        NotifyIcon.Map((icon) =>
        {
            icon.Visible = false;
            icon.Dispose();
            NotifyIcon = null;
        });

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        NotifyIcon.Map((icon) =>
        {
            icon.Visible = false;
            icon.Dispose();
            NotifyIcon = null;
        });
        base.OnClosed(e);
    }

    #endregion

    #region --- Control Event Handlers. ---

    private void SettingsButton_Clicked(object sender, EventArgs e)
    {
        Model.OpenSettings();
    }

    #endregion

    #endregion

}
#nullable disable
