using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace LayoutIndicator.Services;

/// <summary>
/// Monitors the active keyboard layout by polling the foreground window's input locale.
/// Fires <see cref="LayoutChanged"/> when the layout changes.
/// </summary>
public class KeyboardLayoutMonitor : IDisposable
{
    #region Win32 Interop

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    #endregion

    /// <summary>
    /// Raised when the keyboard layout changes. Supplies the culture name (e.g. "en-US").
    /// </summary>
    public event Action<string>? LayoutChanged;

    private readonly DispatcherTimer _timer;
    private string _currentLayout = string.Empty;

    public string CurrentLayout => _currentLayout;

    public KeyboardLayoutMonitor()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _timer.Tick += OnTimerTick;
    }

    public void Start()
    {
        // Get the initial layout immediately
        var layout = DetectCurrentLayout();
        if (!string.IsNullOrEmpty(layout))
        {
            _currentLayout = layout;
            LayoutChanged?.Invoke(_currentLayout);
        }

        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var layout = DetectCurrentLayout();
        if (!string.IsNullOrEmpty(layout) && layout != _currentLayout)
        {
            _currentLayout = layout;
            LayoutChanged?.Invoke(_currentLayout);
        }
    }

    /// <summary>
    /// Detects the keyboard layout of the foreground window.
    /// Returns a culture name like "en-US" or "ru-RU".
    /// </summary>
    private static string DetectCurrentLayout()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return string.Empty;

            var threadId = GetWindowThreadProcessId(hwnd, out _);
            var hkl = GetKeyboardLayout(threadId);

            // The low word of the HKL is the language identifier (LANGID).
            int langId = (int)hkl & 0xFFFF;

            if (langId == 0)
                return string.Empty;

            var culture = CultureInfo.GetCultureInfo(langId);
            return culture.Name; // e.g. "en-US", "ru-RU"
        }
        catch
        {
            return string.Empty;
        }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
