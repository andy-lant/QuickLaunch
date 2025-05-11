using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace QuickLaunch.Core.KeyEvents.KeyboardHook;

public struct HotKey
{
    public bool win = false;
    public bool ctrl = false;
    public bool alt = false;
    public bool shift = false;
    public Key code = 0;

    public HotKey() { }

    public override string ToString()
    {
        string str = "";
        if (win) str += "Win+";
        if (ctrl) str += "Ctrl+";
        if (alt) str += "Alt+";
        if (shift) str += "Shift+";
        str += code.ToString();
        return str;
    }
}

public class HookEventArgs : EventArgs
{
    public HotKey hotKey; // The hotkey
    public IntPtr wParam; // WPARAM argument
    public IntPtr lParam; // LPARAM argument
    public bool swallow = false; // Swallow the event
}

// Hook Types
public enum HookType : int
{
    WH_KEYBOARD_LL = 13,
}

// Keyboard messages
enum Win32_KeyboardMessages : int
{
    WM_KEYDOWN = 0x0100,
    WM_KEYUP = 0x0101,
    WM_SYSKEYDOWN = 0x0104,
    WM_SYSKEYUP = 0x0105,
}

// Some virtual key codes
enum Win32_VKey : int
{
    VK_LWIN = 0x5B, // Left Windows key
    VK_RWIN = 0x5C, // Right Windows key
    VK_CONTROL = 0x11, // Control key
    VK_LCONTROL = 0xA2, // Left Control key
    VK_RCONTROL = 0xA3, // Right Control key
    VK_SHIFT = 0x10, // Shift key
    VK_LSHIFT = 0xA0, // Left Shift key
    VK_RSHIFT = 0xA1, // Right Shift key
    VK_MENU = 0x12, // Menu key (Alt key)
    VK_LMENU = 0xA4, // Left Alt key
    VK_RMENU = 0xA5, // Right Alt key
}


public class LocalWindowsKeyboardHook
{
    // ****************************************************************
    // Filter function delegate
    public delegate int HookProc(int code, IntPtr wParam, IntPtr lParam);
    // ****************************************************************

    // ****************************************************************
    // Internal properties
    protected IntPtr m_hhook;
    protected HookProc m_filterFunc;
    protected HookType m_hookType;
    // ****************************************************************

    // ****************************************************************
    // Event delegate
    public delegate void HookEventHandler(object sender, HookEventArgs e);
    // ****************************************************************

    // ****************************************************************
    // Event: HookInvoked
    public event HookEventHandler? HookInvoked;

    protected void OnHookInvoked(HookEventArgs e) { HookInvoked?.Invoke(this, e); }
    // ****************************************************************

    // ****************************************************************
    // Class constructor(s)
    public LocalWindowsKeyboardHook()
    {
        m_hookType = HookType.WH_KEYBOARD_LL;
        m_filterFunc = new HookProc(this.CoreHookProc);
    }
    public LocalWindowsKeyboardHook(HookProc func)
    {
        m_hookType = HookType.WH_KEYBOARD_LL;
        m_filterFunc = func;
    }
    // ****************************************************************

    // ****************************************************************
    // Default filter function
    public int CoreHookProc(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code < 0) return CallNextHookEx(m_hhook, code, wParam, lParam);
        Win32_KeyboardMessages msg = (Win32_KeyboardMessages)wParam.ToInt32(); // Get the message type

        if (msg == Win32_KeyboardMessages.WM_KEYDOWN || msg == Win32_KeyboardMessages.WM_SYSKEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam); // Get the virtual key, first DWORD element of structure pointed to by lParam
            Key key = KeyInterop.KeyFromVirtualKey(vkCode);

            var hotKey = new HotKey
            {
                win = (GetAsyncKeyState(((int)Win32_VKey.VK_LWIN)) & 0x8000) != 0 || (GetAsyncKeyState((int)Win32_VKey.VK_RWIN) & 0x8000) != 0,
                ctrl = (GetAsyncKeyState(((int)Win32_VKey.VK_CONTROL)) & 0x8000) != 0,
                alt = (GetAsyncKeyState(((int)Win32_VKey.VK_MENU)) & 0x8000) != 0,
                shift = (GetAsyncKeyState(((int)Win32_VKey.VK_SHIFT)) & 0x8000) != 0,
                code = key
            };

            // Let clients determine what to do
            HookEventArgs e = new HookEventArgs { hotKey = hotKey };
            OnHookInvoked(e);
            if (e.swallow)
            {
                return -1;
            }
        }
        // Yield to the next hook in the chain
        return CallNextHookEx(m_hhook, code, wParam, lParam);
    }

    // **************************************************************** 

    // ****************************************************************
    // Install the hook
    public void Install()
    {
        using (var curProcess = Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            if (curModule is not null)
            {
                m_hhook = SetWindowsHookEx(m_hookType, m_filterFunc, GetModuleHandle(curModule.ModuleName!), 0);
            }
        }
    }
    // **************************************************************** 

    // **************************************************************** 
    // Uninstall the hook
    public void Uninstall()
    {
        UnhookWindowsHookEx(m_hhook);
    }
    // ****************************************************************

    #region Win32 Imports

    // **********************************************************************
    // Win32: SetWindowsHookEx()
    [DllImport("user32.dll")] protected static extern IntPtr SetWindowsHookEx(HookType code, HookProc func, IntPtr hInstance, int threadID);
    // ********************************************************************** /

    // **********************************************************************
    // Win32: UnhookWindowsHookEx()
    [DllImport("user32.dll")] protected static extern int UnhookWindowsHookEx(IntPtr hhook);
    // **********************************************************************

    // **********************************************************************
    // Win32: CallNextHookEx()
    [DllImport("user32.dll")] protected static extern int CallNextHookEx(IntPtr hhook, int code, IntPtr wParam, IntPtr lParam);
    // **********************************************************************

    // **********************************************************************
    // Win32: GetModuleHandle()
    [DllImport("kernel32.dll")] protected static extern IntPtr GetModuleHandle(string lpModuleName);
    // **********************************************************************

    // **********************************************************************
    // Win32: GetAsyncKeyState()
    [DllImport("user32.dll")] protected static extern UInt16 GetAsyncKeyState(int vKey);
    // **********************************************************************


    #endregion
}