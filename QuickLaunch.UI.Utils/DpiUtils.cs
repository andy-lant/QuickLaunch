using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace QuickLaunch.UI.Utils;

public static class DpiUtils
{
    public const double STANDARD_DPI = 96.0;

    private static class Interop
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetDpiForWindow(nint hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        // Delegate for EnumChildWindows
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        public const uint GW_CHILD = 5;

        public static IntPtr? GetHwnd(Visual visual)
        {
            if (visual == null)
            {
                return null;
            }

            // Get the PresentationSource for the Popup
            PresentationSource source = PresentationSource.FromVisual(visual);
            if (source == null || source is not HwndSource)
            {
                return null;
            }

            // Get the HwndSource
            HwndSource hwndSource = (HwndSource)source;

            // The actual Popup window is often a child of an invisible host window.
            // We need to traverse the Win32 child windows to find it.
            IntPtr hostHwnd = hwndSource.Handle;
            IntPtr popupHwnd = IntPtr.Zero;

            // Enumerate child windows to find the actual Popup window
            EnumChildWindows(hostHwnd, (hWnd, lParam) =>
            {
                // You might need more sophisticated logic here to identify the correct Popup window.
                // Often, the Popup window is the first or only child.
                popupHwnd = hWnd;
                return false; // Stop enumeration after finding one (or adjust logic as needed)
            }, IntPtr.Zero);

            return popupHwnd;
        }
    }

    public static double PhysicalToDipX(this DpiScale dpiScale, int physical)
    {
        return physical / dpiScale.DpiScaleX;
    }

    public static double PhysicalToDipY(this DpiScale dpiScale, int physical)
    {
        return physical / dpiScale.DpiScaleY;
    }

    public static int DipToPhysicalX(this DpiScale dpiScale, double dip)
    {
        return (int)Math.Round(dip * dpiScale.DpiScaleX);
    }

    public static int DipToPhysicalY(this DpiScale dpiScale, double dip)
    {
        return (int)Math.Round(dip * dpiScale.DpiScaleY);
    }


    public static uint GetDpi(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        var hwnd = new WindowInteropHelper(window);

        return Interop.GetDpiForWindow(hwnd.Handle);
    }

    public static uint GetDpi(Visual visual)
    {
        ArgumentNullException.ThrowIfNull(visual, nameof(visual));

        nint? hwnd = Interop.GetHwnd(visual);

        if (hwnd is nint hwnd2)
        {
            return Interop.GetDpiForWindow(hwnd2);
        }
        else
        {
            throw new InvalidOperationException("Could not determine HWND handle for Popup.");
        }
    }

    public static double GetDpiScale(Window window)
    {
        return GetDpi(window) / STANDARD_DPI;
    }

    public static double GetDpiScale(Visual visual)
    {
        return GetDpi(visual) / STANDARD_DPI;
    }

}
