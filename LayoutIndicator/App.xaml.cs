using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using LayoutIndicator.Services;
using WinForms = System.Windows.Forms;

namespace LayoutIndicator;

public partial class App : Application
{
    private const string MutexName = "Global\\LayoutIndicator_SingleInstance_8F2E4A";

    private Mutex? _singleInstanceMutex;
    private WinForms.NotifyIcon? _trayIcon;
    private SettingsService _settingsService = null!;
    private KeyboardLayoutMonitor _layoutMonitor = null!;
    private MonitorService _monitorService = null!;

    // Context menu items we need to update dynamically
    private WinForms.ToolStripMenuItem? _alwaysOnItem;
    private WinForms.ToolStripMenuItem? _flashItem;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ── Single-instance guard ──────────────────────────────────────
        _singleInstanceMutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "LayoutIndicator is already running.",
                "LayoutIndicator",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // ── Load settings ──────────────────────────────────────────────
        _settingsService = new SettingsService();
        _settingsService.Load();

        // ── Initialize services ────────────────────────────────────────
        _monitorService = new MonitorService(_settingsService.Settings);
        _monitorService.Initialize();

        _layoutMonitor = new KeyboardLayoutMonitor();
        _layoutMonitor.LayoutChanged += OnLayoutChanged;
        _layoutMonitor.Start();

        // ── System tray icon ───────────────────────────────────────────
        SetupTrayIcon();
    }

    private void OnLayoutChanged(string cultureName)
    {
        _monitorService.OnLayoutChanged(cultureName);
        UpdateTrayTooltip(cultureName);
    }

    private void UpdateTrayTooltip(string cultureName)
    {
        if (_trayIcon != null)
        {
            // NotifyIcon.Text has a 127-character limit
            var text = $"Layout: {cultureName}";
            _trayIcon.Text = text.Length > 127 ? text[..127] : text;
        }
    }

    #region System Tray

    private void SetupTrayIcon()
    {
        // Create tray icon with a generated icon (colored square)
        _trayIcon = new WinForms.NotifyIcon
        {
            Icon = CreateTrayIcon(),
            Visible = true,
            Text = "LayoutIndicator"
        };

        var contextMenu = new WinForms.ContextMenuStrip();

        // ── Mode submenu ───────────────────────────────────────────
        var modeMenu = new WinForms.ToolStripMenuItem("Mode");

        _alwaysOnItem = new WinForms.ToolStripMenuItem("Always On");
        _alwaysOnItem.Click += (_, _) => SetMode("always");

        _flashItem = new WinForms.ToolStripMenuItem("Flash on Change");
        _flashItem.Click += (_, _) => SetMode("flash");

        UpdateModeChecks();

        modeMenu.DropDownItems.Add(_alwaysOnItem);
        modeMenu.DropDownItems.Add(_flashItem);

        // ── Width submenu ──────────────────────────────────────────
        var widthMenu = new WinForms.ToolStripMenuItem("Strip Width");
        foreach (var w in new[] { 5, 10, 15, 20, 30, 50 })
        {
            var item = new WinForms.ToolStripMenuItem($"{w} px");
            var width = w; // capture
            item.Click += (_, _) => SetStripWidth(width);
            widthMenu.DropDownItems.Add(item);
        }

        // ── Opacity submenu ────────────────────────────────────────
        var opacityMenu = new WinForms.ToolStripMenuItem("Opacity");
        foreach (var pct in new[] { 30, 50, 70, 85, 100 })
        {
            var item = new WinForms.ToolStripMenuItem($"{pct}%");
            var val = pct / 100.0;
            item.Click += (_, _) => SetOpacity(val);
            opacityMenu.DropDownItems.Add(item);
        }

        // ── Open settings file ─────────────────────────────────────
        var openSettingsItem = new WinForms.ToolStripMenuItem("Open settings.json");
        openSettingsItem.Click += (_, _) =>
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            if (File.Exists(path))
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
        };

        // ── Reload settings ────────────────────────────────────────
        var reloadItem = new WinForms.ToolStripMenuItem("Reload Settings");
        reloadItem.Click += (_, _) => ReloadSettings();

        // ── Exit ───────────────────────────────────────────────────
        var exitItem = new WinForms.ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitApplication();

        contextMenu.Items.Add(modeMenu);
        contextMenu.Items.Add(widthMenu);
        contextMenu.Items.Add(opacityMenu);
        contextMenu.Items.Add(new WinForms.ToolStripSeparator());
        contextMenu.Items.Add(openSettingsItem);
        contextMenu.Items.Add(reloadItem);
        contextMenu.Items.Add(new WinForms.ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        _trayIcon.ContextMenuStrip = contextMenu;
    }

    private void SetMode(string mode)
    {
        _settingsService.Settings.Mode = mode;
        _settingsService.Save();
        UpdateModeChecks();

        if (mode == "always")
        {
            _monitorService.ShowAll();
        }
        else
        {
            _monitorService.HideAll();
        }
    }

    private void SetStripWidth(int width)
    {
        _settingsService.Settings.StripWidth = width;
        _settingsService.Save();
        _monitorService.UpdateSettings(_settingsService.Settings);
    }

    private void SetOpacity(double opacity)
    {
        _settingsService.Settings.Opacity = opacity;
        _settingsService.Save();
        _monitorService.UpdateSettings(_settingsService.Settings);
    }

    private void ReloadSettings()
    {
        _settingsService.Load();
        _monitorService.UpdateSettings(_settingsService.Settings);
        UpdateModeChecks();

        // Re-trigger layout detection to apply new colors
        var currentLayout = _layoutMonitor.CurrentLayout;
        if (!string.IsNullOrEmpty(currentLayout))
        {
            _monitorService.OnLayoutChanged(currentLayout);
        }
    }

    private void UpdateModeChecks()
    {
        if (_alwaysOnItem != null)
            _alwaysOnItem.Checked = _settingsService.Settings.Mode == "always";
        if (_flashItem != null)
            _flashItem.Checked = _settingsService.Settings.Mode == "flash";
    }

    /// <summary>
    /// Generates a simple tray icon — a 16x16 colored square.
    /// </summary>
    private static Icon CreateTrayIcon()
    {
        using var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);

        // Draw a blue/red split icon to represent the app
        g.Clear(Color.FromArgb(0, 120, 212)); // Blue
        g.FillRectangle(new SolidBrush(Color.FromArgb(232, 17, 35)), 8, 0, 8, 16); // Red right half

        var handle = bmp.GetHicon();
        return Icon.FromHandle(handle);
    }

    #endregion

    private void ExitApplication()
    {
        _layoutMonitor.Stop();
        _layoutMonitor.Dispose();
        _monitorService.Dispose();

        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }

        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();

        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
