using System.Runtime.InteropServices;

namespace QuickLaunch.Core.Utils;

public static class Console
{
    private class Interop
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeConsole();
    }

    public static void OpenConsole()
    {
        Interop.AllocConsole();
    }

    public static void CloseConsole()
    {
        Interop.FreeConsole();
    }

}
