using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LayoutIndicator;

/// <summary>
/// A transparent, topmost, click-through overlay window that shows a colored strip.
/// </summary>
public partial class OverlayWindow : Window
{
    #region Win32 — Make window click-through and hide from Alt+Tab

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    #endregion

    private double _targetOpacity = 0.7;

    public OverlayWindow()
    {
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

        // WS_EX_TRANSPARENT: clicks pass through
        // WS_EX_TOOLWINDOW: hidden from Alt+Tab
        // WS_EX_NOACTIVATE: doesn't steal focus
        exStyle |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
    }

    /// <summary>
    /// Positions this overlay at the right edge of the specified screen bounds.
    /// </summary>
    public void PositionOnScreen(double screenLeft, double screenTop, double screenWidth, double screenHeight, int stripWidth)
    {
        Left = screenLeft + screenWidth - stripWidth;
        Top = screenTop;
        Width = stripWidth;
        Height = screenHeight;
    }

    /// <summary>
    /// Sets the strip color.
    /// </summary>
    public void SetColor(Color color)
    {
        StripBorder.Background = new SolidColorBrush(color);
    }

    /// <summary>
    /// Sets the target (resting) opacity for the strip.
    /// </summary>
    public void SetTargetOpacity(double opacity)
    {
        _targetOpacity = Math.Clamp(opacity, 0.05, 1.0);
        Opacity = _targetOpacity;
    }

    /// <summary>
    /// Shows the strip in "always on" mode at the configured opacity.
    /// </summary>
    public void ShowAlways()
    {
        Opacity = _targetOpacity;
        Show();
    }

    /// <summary>
    /// Flashes the strip: shows it at full target opacity, then fades out over the given duration.
    /// </summary>
    public void Flash(double durationSeconds)
    {
        Opacity = _targetOpacity;
        Show();

        var fadeOut = new DoubleAnimation
        {
            From = _targetOpacity,
            To = 0.0,
            BeginTime = TimeSpan.FromSeconds(durationSeconds * 0.6), // hold for 60% of duration
            Duration = new Duration(TimeSpan.FromSeconds(durationSeconds * 0.4)), // fade for 40%
            FillBehavior = FillBehavior.Stop
        };

        fadeOut.Completed += (_, _) =>
        {
            Opacity = 0.0;
        };

        BeginAnimation(OpacityProperty, fadeOut);
    }

    /// <summary>
    /// Hides the strip (sets opacity to 0).
    /// </summary>
    public void HideStrip()
    {
        BeginAnimation(OpacityProperty, null); // cancel any running animation
        Opacity = 0.0;
    }
}
