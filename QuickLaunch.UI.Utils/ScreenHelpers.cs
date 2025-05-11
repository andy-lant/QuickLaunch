
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;

namespace QuickLaunch.UI.Utils;

/// <summary>
/// Data regarding a specific screen.
/// </summary>
/// <param name="DpiScale"></param>
public record ScreenData
(
    DpiScale DpiScale,
    Rectangle WorkingAreaPhysical,
    Rect WorkingAreaDIP,
    Rectangle BoundsPhysical,
    Rect BoundsDIP
);

/// <summary>
/// A Visual's position on the screen.
/// </summary>
/// <param name="PositionPhysical"></param>
/// <param name="PositionDIP"></param>
/// <param name="OverflowsX">Whether Visual overflows screen on X axis.</param>
/// <param name="OverflowsY">Whether Visual overflows screen on Y axis.</param>
public record ScreenLocation
(
    Rectangle PositionPhysical,
    Rect PositionDIP,
    bool OverflowsX,
    bool OverflowsY
);

/// <summary>
/// A Visual's position on the desktop.
/// </summary>
/// <param name="PositionPhysical"></param>
/// <param name="PositionDIP"></param>
public record DesktopLocation
(
    Rectangle PositionPhysical,
    Rect PositionDIP
);

/// <summary>
/// Utility class for working with screens.
/// </summary>
public static class ScreenHelpers
{

    #region ----- Public Methods. -----

    /// <summary>
    /// Retrieve screen information for a given screen, using a given DpiScale.
    /// </summary>
    /// <param name="screen"></param>
    /// <param name="dpiScale"></param>
    /// <returns></returns>
    public static ScreenData GetScreenData(Screen screen, DpiScale dpiScale)
    {
        ArgumentNullException.ThrowIfNull(screen, nameof(screen));

        var calculateDIP = (Rectangle physical) =>
            new Rect(
                physical.Left / dpiScale.DpiScaleX,
                physical.Top / dpiScale.DpiScaleY,
                physical.Width / dpiScale.DpiScaleX,
                physical.Height / dpiScale.DpiScaleY
            );

        Rectangle workingAreaPhysical = screen.WorkingArea;
        var workingAreaDIP = calculateDIP(workingAreaPhysical);

        Rectangle boundsPhysical = screen.Bounds;
        var boundsDIP = calculateDIP(boundsPhysical);

        return new ScreenData(
            DpiScale: dpiScale,
            WorkingAreaPhysical: workingAreaPhysical,
            WorkingAreaDIP: workingAreaDIP,
            BoundsDIP: boundsDIP,
            BoundsPhysical: boundsPhysical
        );
    }

    /// <summary>
    /// Retrieve screeen information for a given point (using DPI as seen by Visual).
    /// </summary>
    /// <param name="point"></param>
    /// <param name="visual"></param>
    /// <returns></returns>
    public static ScreenData GetScreenDataForPoint(System.Windows.Point point, Visual visual)
    {
        ArgumentNullException.ThrowIfNull(point, nameof(point));
        ArgumentNullException.ThrowIfNull(visual, nameof(visual));

        var dpiScale = VisualTreeHelper.GetDpi(visual);

        System.Drawing.Point physicalPoint = new(
            dpiScale.DipToPhysicalX(point.X),
            dpiScale.DipToPhysicalY(point.Y)
        );

        Screen screen = Screen.FromPoint(physicalPoint);

        return GetScreenData(screen, dpiScale);
    }

    /// <summary>
    /// Retrieve screeen information for a given Visual.
    /// </summary>
    /// <param name="visual"></param>
    /// <exception cref="InvalidOperationException">if information could not be retrieved</exception>
    public static ScreenData GetScreenDataForVisual(Visual visual)
    {
        ArgumentNullException.ThrowIfNull(visual, nameof(visual));

        var window = VisualTreeUtils.GetWindow(visual);
        if (window is null)
        {
            throw new InvalidOperationException("The visual must be a child of a Window.");
        }

        IntPtr windowHandle = new WindowInteropHelper(window).Handle;
        if (windowHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("The window handle is invalid.");
        }

        Screen currentScreen = Screen.FromHandle(windowHandle);
        if (currentScreen is null)
        {
            throw new InvalidOperationException("Unable to determine the current screen.");
        }

        var dpiScale = VisualTreeHelper.GetDpi(visual);
        if (dpiScale.DpiScaleX == 0 || dpiScale.DpiScaleY == 0)
        {
            throw new InvalidOperationException("DPI scale cannot be zero.");
        }
        else
        {
        }

        return GetScreenData(currentScreen, dpiScale);
    }

    /// <summary>
    /// Get a Visual's position on the desktop.
    /// </summary>
    /// <param name="window"></param>
    /// <param name="screenData">Optional: ScreenData for the screen, if it is already available (see GetScreenDataForVisual).</param>
    /// <exception cref="InvalidOperationException">if information could not be retrieved</exception>
    public static DesktopLocation GetWindowDesktopPosition(Window window, ScreenData? screenData = null)
    {
        ArgumentNullException.ThrowIfNull(window, nameof(window));

        if (screenData == null)
        {
            screenData = GetScreenDataForVisual(window);
        }

        Rect dipBounds = new(
            window.Left, window.Top, window.Width, window.Height
        );

        var ToPhysical = (double value, double dipScale) => (int)Math.Round(value * dipScale);

        Rectangle physicalBounds = new Rectangle(
            ToPhysical(window.Left, screenData.DpiScale.DpiScaleX),
            ToPhysical(window.Top, screenData.DpiScale.DpiScaleY),
            ToPhysical(window.Width, screenData.DpiScale.DpiScaleX),
            ToPhysical(window.Height, screenData.DpiScale.DpiScaleY)
        );

        return new DesktopLocation(
            PositionDIP: dipBounds,
            PositionPhysical: physicalBounds
        );
    }

    /// <summary>
    /// Get a Visual's position on the screen.
    /// </summary>
    /// <param name="window"></param>
    /// <param name="screenData">Optional: ScreenData for the screen, if it is already available (see GetScreenDataForVisual).</param>
    /// <exception cref="InvalidOperationException">if information could not be retrieved</exception>
    public static ScreenLocation GetWindowScreenPosition(Window window, ScreenData? screenData = null)
    {
        ArgumentNullException.ThrowIfNull(window, nameof(window));

        if (screenData == null)
        {
            screenData = GetScreenDataForVisual(window);
        }

        var desktopLocation = GetWindowDesktopPosition(window, screenData);

        var dipBounds = desktopLocation.PositionDIP;
        dipBounds.Offset(-screenData.WorkingAreaDIP.Left, -screenData.WorkingAreaDIP.Top);

        var physicalBounds = desktopLocation.PositionPhysical;
        physicalBounds.Offset(-screenData.WorkingAreaPhysical.Left, -screenData.WorkingAreaPhysical.Top);

        bool overflowsX = desktopLocation.PositionDIP.Left < screenData.WorkingAreaDIP.Left || desktopLocation.PositionDIP.Right >= screenData.WorkingAreaDIP.Right;
        bool overflowsY = desktopLocation.PositionDIP.Top < screenData.WorkingAreaDIP.Top || desktopLocation.PositionDIP.Bottom >= screenData.WorkingAreaDIP.Bottom;

        return new ScreenLocation(
            PositionDIP: dipBounds,
            PositionPhysical: physicalBounds,
            OverflowsX: overflowsX,
            OverflowsY: overflowsY
        );
    }
    #endregion

}
