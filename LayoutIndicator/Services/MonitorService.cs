using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using LayoutIndicator.Models;
using WinForms = System.Windows.Forms;

namespace LayoutIndicator.Services;

/// <summary>
/// Manages one <see cref="OverlayWindow"/> per connected monitor.
/// Handles repositioning when display settings change.
/// </summary>
public class MonitorService : IDisposable
{
    private readonly List<OverlayWindow> _overlays = new();
    private AppSettings _settings;
    private Color _currentColor;

    public MonitorService(AppSettings settings)
    {
        _settings = settings;
        _currentColor = ParseColor(settings.DefaultColor);

        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    /// <summary>
    /// Creates overlay windows for all screens and shows them.
    /// </summary>
    public void Initialize()
    {
        CreateOverlays();
    }

    /// <summary>
    /// Updates the settings reference and re-applies to all overlays.
    /// </summary>
    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings;
        RecreateOverlays();
    }

    /// <summary>
    /// Updates the color of all overlays based on the current keyboard layout.
    /// </summary>
    public void OnLayoutChanged(string cultureName)
    {
        // Look up the color for this layout; fall back to defaultColor
        string hexColor;
        if (_settings.Layouts.TryGetValue(cultureName, out var mapped))
        {
            hexColor = mapped;
        }
        else
        {
            // Also try matching by language only (e.g. "en" matches "en-US")
            var lang = cultureName.Split('-')[0];
            hexColor = _settings.DefaultColor;
            foreach (var kvp in _settings.Layouts)
            {
                if (kvp.Key.StartsWith(lang, StringComparison.OrdinalIgnoreCase))
                {
                    hexColor = kvp.Value;
                    break;
                }
            }
        }

        _currentColor = ParseColor(hexColor);

        foreach (var overlay in _overlays)
        {
            overlay.SetColor(_currentColor);

            if (_settings.Mode == "flash")
            {
                overlay.Flash(_settings.FlashDurationSeconds);
            }
            else
            {
                overlay.ShowAlways();
            }
        }
    }

    /// <summary>
    /// Shows all overlays in "always" mode (used when switching modes).
    /// </summary>
    public void ShowAll()
    {
        foreach (var overlay in _overlays)
        {
            overlay.SetColor(_currentColor);
            overlay.ShowAlways();
        }
    }

    /// <summary>
    /// Hides all overlays (used when switching to "flash" mode — overlays stay hidden until next change).
    /// </summary>
    public void HideAll()
    {
        foreach (var overlay in _overlays)
        {
            overlay.HideStrip();
        }
    }

    private void CreateOverlays()
    {
        // WPF uses device-independent pixels (96 DPI). Screen bounds from WinForms are in physical pixels.
        // We need to convert using the DPI scaling factor.
        var dpiScale = GetDpiScale();

        foreach (var screen in WinForms.Screen.AllScreens)
        {
            var overlay = new OverlayWindow();

            double left = screen.Bounds.Left / dpiScale;
            double top = screen.Bounds.Top / dpiScale;
            double width = screen.Bounds.Width / dpiScale;
            double height = screen.Bounds.Height / dpiScale;

            overlay.PositionOnScreen(left, top, width, height, _settings.StripWidth);
            overlay.SetTargetOpacity(_settings.Opacity);
            overlay.SetColor(_currentColor);

            if (_settings.Mode == "always")
            {
                overlay.ShowAlways();
            }
            else
            {
                // In flash mode, start hidden
                overlay.Show();
                overlay.HideStrip();
            }

            _overlays.Add(overlay);
        }
    }

    private void RecreateOverlays()
    {
        CloseAllOverlays();
        CreateOverlays();
    }

    private void CloseAllOverlays()
    {
        foreach (var overlay in _overlays)
        {
            overlay.Close();
        }
        _overlays.Clear();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        // Monitors connected/disconnected — rebuild overlays
        Application.Current?.Dispatcher.Invoke(RecreateOverlays);
    }

    private static double GetDpiScale()
    {
        // Get DPI from the main WPF visual if available
        try
        {
            var source = PresentationSource.FromVisual(Application.Current.MainWindow);
            if (source?.CompositionTarget != null)
            {
                return source.CompositionTarget.TransformToDevice.M11;
            }
        }
        catch
        {
            // Ignore — MainWindow may not exist yet
        }

        // Fallback: use per-monitor DPI from Win32
        // For simplicity, use the system DPI
        using var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
        return g.DpiX / 96.0;
    }

    private static Color ParseColor(string hex)
    {
        try
        {
            var converted = ColorConverter.ConvertFromString(hex);
            if (converted is Color c)
                return c;
        }
        catch
        {
            // Ignore parse errors
        }
        return Colors.Gray;
    }

    public void Dispose()
    {
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        CloseAllOverlays();
        GC.SuppressFinalize(this);
    }
}
