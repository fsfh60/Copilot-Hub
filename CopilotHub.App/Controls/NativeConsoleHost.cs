using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Serilog;

namespace CopilotHub.App.Controls;

/// <summary>
/// WPF HwndHost that embeds a native console window (CMD, PowerShell, Copilot CLI)
/// directly into the WPF visual tree. The hosted process runs in a real console
/// with full color, interactivity, and standard I/O — not a simulation.
/// </summary>
public class NativeConsoleHost : HwndHost
{
    private static readonly ILogger Logger = Log.ForContext<NativeConsoleHost>();
    private static readonly object ConsoleLock = new();

    private IntPtr _containerHwnd;
    private IntPtr _currentConsoleHwnd;

    #region Win32 P/Invoke

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(uint dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const int WS_CHILD = 0x40000000;
    private const int WS_VISIBLE = 0x10000000;
    private const int WS_CLIPCHILDREN = 0x02000000;
    private const int WS_CAPTION = 0x00C00000;
    private const int WS_THICKFRAME = 0x00040000;
    private const int WS_BORDER = 0x00800000;
    private const int WS_EX_APPWINDOW = 0x00040000;
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int WM_SIZE = 0x0005;

    #endregion

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        _containerHwnd = CreateWindowEx(0, "static", "",
            (uint)(WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN),
            0, 0, (int)RenderSize.Width, (int)RenderSize.Height,
            hwndParent.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        return new HandleRef(this, _containerHwnd);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        ReleaseConsole();
        if (_containerHwnd != IntPtr.Zero)
        {
            DestroyWindow(_containerHwnd);
            _containerHwnd = IntPtr.Zero;
        }
    }

    /// <summary>
    /// Embeds the given console window HWND into this host control.
    /// Removes decorations, makes it a child window, and resizes to fill.
    /// </summary>
    public void EmbedConsoleWindow(IntPtr consoleHwnd)
    {
        ReleaseConsole();
        if (consoleHwnd == IntPtr.Zero || _containerHwnd == IntPtr.Zero) return;

        _currentConsoleHwnd = consoleHwnd;

        // Remove window decorations, make child
        var style = GetWindowLong(consoleHwnd, GWL_STYLE);
        style &= ~(WS_CAPTION | WS_THICKFRAME | WS_BORDER);
        style |= WS_CHILD | WS_VISIBLE;
        SetWindowLong(consoleHwnd, GWL_STYLE, style);

        // Remove taskbar appearance
        var exStyle = GetWindowLong(consoleHwnd, GWL_EXSTYLE);
        exStyle &= ~WS_EX_APPWINDOW;
        SetWindowLong(consoleHwnd, GWL_EXSTYLE, exStyle);

        SetParent(consoleHwnd, _containerHwnd);
        FitToContainer();
        ShowWindow(consoleHwnd, SW_SHOW);

        Logger.Debug("Embedded console HWND {Hwnd} into container", consoleHwnd);
    }

    /// <summary>
    /// Detaches the current console window without killing the process.
    /// The window is hidden so it doesn't appear as a floating window.
    /// </summary>
    public void ReleaseConsole()
    {
        if (_currentConsoleHwnd == IntPtr.Zero) return;
        ShowWindow(_currentConsoleHwnd, SW_HIDE);
        SetParent(_currentConsoleHwnd, IntPtr.Zero);
        _currentConsoleHwnd = IntPtr.Zero;
    }

    public void FocusConsole()
    {
        if (_currentConsoleHwnd != IntPtr.Zero)
            SetFocus(_currentConsoleHwnd);
    }

    public bool HasConsole => _currentConsoleHwnd != IntPtr.Zero;

    private void FitToContainer()
    {
        if (_currentConsoleHwnd == IntPtr.Zero || _containerHwnd == IntPtr.Zero) return;
        GetClientRect(_containerHwnd, out var rect);
        if (rect.Right > 0 && rect.Bottom > 0)
            MoveWindow(_currentConsoleHwnd, 0, 0, rect.Right, rect.Bottom, true);
    }

    protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_SIZE && _currentConsoleHwnd != IntPtr.Zero)
            FitToContainer();
        return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
    }

    /// <summary>
    /// Finds the console window HWND for a process by temporarily attaching to its console.
    /// Thread-safe via lock (only one console attachment at a time per process).
    /// </summary>
    public static IntPtr GetConsoleHwndForProcess(Process process)
    {
        lock (ConsoleLock)
        {
            try
            {
                FreeConsole();
                if (!AttachConsole((uint)process.Id))
                {
                    Logger.Warning("AttachConsole failed for PID {Pid}, error {Error}",
                        process.Id, Marshal.GetLastWin32Error());
                    return IntPtr.Zero;
                }
                var hwnd = GetConsoleWindow();
                FreeConsole();
                return hwnd;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting console HWND for PID {Pid}", process.Id);
                return IntPtr.Zero;
            }
        }
    }

    /// <summary>
    /// Starts a console process and retrieves its console window HWND.
    /// Polls with timeout to wait for the console window to be created.
    /// </summary>
    public static async Task<(Process? Process, IntPtr ConsoleHwnd)> StartConsoleProcessAsync(
        string executable, string arguments, string workingDirectory)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = false,
            };

            var process = Process.Start(psi);
            if (process == null) return (null, IntPtr.Zero);

            // Poll for console window (up to 5 seconds)
            IntPtr hwnd = IntPtr.Zero;
            for (int i = 0; i < 50; i++)
            {
                await Task.Delay(100);
                if (process.HasExited) break;

                hwnd = GetConsoleHwndForProcess(process);
                if (hwnd != IntPtr.Zero) break;
            }

            if (hwnd == IntPtr.Zero)
                Logger.Warning("Could not find console window for {Exe} (PID {Pid})", executable, process.Id);
            else
                Logger.Information("Started {Exe} (PID {Pid}), console HWND {Hwnd}", executable, process.Id, hwnd);

            return (process, hwnd);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to start console process {Exe}", executable);
            return (null, IntPtr.Zero);
        }
    }
}
