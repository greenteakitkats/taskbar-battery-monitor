using Windows.Gaming.Input;

namespace XboxControllerBattery;

/// <summary>
/// The app: a windowless tray icon. Polls all providers on a timer, draws the chosen device's
/// battery on the icon, lists everything in the tooltip, and warns when any device goes low.
/// </summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Settings _settings;

    // Ids we've already warned about, so we warn once per low-battery episode (reset on recovery).
    private readonly HashSet<string> _warned = new();
    private Icon? _currentIcon;

    // Last snapshot, so the (synchronous) menu builder doesn't have to re-poll asynchronously.
    private IReadOnlyList<BatteryDevice> _lastDevices = Array.Empty<BatteryDevice>();

    public TrayApplicationContext()
    {
        _settings = Settings.Load();

        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            Text = "Bluetooth Battery Monitor",
            Icon = IconRenderer.Render(null, BatteryState.NotPresent),
            ContextMenuStrip = new ContextMenuStrip()
        };
        _notifyIcon.ContextMenuStrip.Opening += (_, _) => BuildMenu();

        // Subscribing activates Windows.Gaming.Input device tracking so controllers enumerate reliably.
        RawGameController.RawGameControllerAdded += (_, _) => { };
        RawGameController.RawGameControllerRemoved += (_, _) => { };

        _timer = new System.Windows.Forms.Timer
        {
            Interval = Math.Max(5, _settings.PollIntervalSeconds) * 1000
        };
        _timer.Tick += async (_, _) => await RefreshAsync();
        _timer.Start();

        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        IReadOnlyList<BatteryDevice> devices;
        try
        {
            devices = await BatteryService.ReadAllAsync();
        }
        catch
        {
            devices = Array.Empty<BatteryDevice>();
        }

        _lastDevices = devices;
        UpdateIcon(devices);
        CheckLowBattery(devices);
    }

    /// <summary>The device whose battery drives the tray icon: the user's pick, or lowest if "auto".</summary>
    private BatteryDevice? PickPrimary(IReadOnlyList<BatteryDevice> devices)
    {
        if (devices.Count == 0)
            return null;

        if (_settings.PrimaryDeviceId != null)
        {
            var chosen = devices.FirstOrDefault(d => d.Id == _settings.PrimaryDeviceId);
            if (chosen != null)
                return chosen; // fall through to auto if the chosen device isn't currently present
        }

        return devices.Where(d => d.Percent.HasValue).OrderBy(d => d.Percent!.Value).FirstOrDefault()
               ?? devices[0];
    }

    private void UpdateIcon(IReadOnlyList<BatteryDevice> devices)
    {
        var primary = PickPrimary(devices);

        Icon newIcon = primary == null
            ? IconRenderer.Render(null, BatteryState.NotPresent)
            : IconRenderer.Render(primary.Percent, primary.State);

        string tooltip = devices.Count == 0
            ? "No devices found"
            : string.Join(Environment.NewLine, devices.Select(Format));
        if (tooltip.Length > 127)
            tooltip = tooltip[..124] + "...";

        _notifyIcon.Text = tooltip;
        _notifyIcon.Icon = newIcon;
        _currentIcon?.Dispose();
        _currentIcon = newIcon;
    }

    private void CheckLowBattery(IReadOnlyList<BatteryDevice> devices)
    {
        if (!_settings.LowBatteryWarningEnabled)
            return;

        foreach (var d in devices)
        {
            if (!d.Percent.HasValue)
                continue;

            bool low = d.Percent.Value <= _settings.LowBatteryThreshold
                       && d.State != BatteryState.Charging;

            if (low && _warned.Add(d.Id))
            {
                _notifyIcon.ShowBalloonTip(
                    10000,
                    "Battery low",
                    $"{d.Name} is at {d.Percent.Value}%. Time to charge or swap batteries.",
                    ToolTipIcon.Warning);
            }
            else if (!low)
            {
                _warned.Remove(d.Id);
            }
        }
    }

    private void BuildMenu()
    {
        var menu = _notifyIcon.ContextMenuStrip!;
        menu.Items.Clear();

        var devices = _lastDevices;

        if (devices.Count == 0)
        {
            menu.Items.Add(new ToolStripMenuItem("No devices found") { Enabled = false });
        }
        else
        {
            foreach (var d in devices)
                menu.Items.Add(new ToolStripMenuItem(Format(d)) { Enabled = false });
        }

        menu.Items.Add(new ToolStripSeparator());

        // "Show on taskbar" — choose which device's battery the icon displays.
        var show = new ToolStripMenuItem("Show on taskbar");
        var auto = new ToolStripMenuItem("Auto (lowest battery)") { Checked = _settings.PrimaryDeviceId == null };
        auto.Click += (_, _) =>
        {
            _settings.PrimaryDeviceId = null;
            _settings.Save();
            _ = RefreshAsync();
        };
        show.DropDownItems.Add(auto);
        if (devices.Count > 0)
            show.DropDownItems.Add(new ToolStripSeparator());
        foreach (var d in devices)
        {
            var item = new ToolStripMenuItem(d.Name) { Checked = _settings.PrimaryDeviceId == d.Id };
            string capturedId = d.Id;
            item.Click += (_, _) =>
            {
                _settings.PrimaryDeviceId = capturedId;
                _settings.Save();
                _ = RefreshAsync();
            };
            show.DropDownItems.Add(item);
        }
        menu.Items.Add(show);

        var warn = new ToolStripMenuItem("Low-battery warnings")
        {
            Checked = _settings.LowBatteryWarningEnabled,
            CheckOnClick = true
        };
        warn.Click += (_, _) =>
        {
            _settings.LowBatteryWarningEnabled = warn.Checked;
            _settings.Save();
        };
        menu.Items.Add(warn);

        var threshold = new ToolStripMenuItem($"Warn below … ({_settings.LowBatteryThreshold}%)");
        foreach (int t in new[] { 10, 15, 20, 25, 30 })
        {
            var item = new ToolStripMenuItem($"{t}%") { Checked = _settings.LowBatteryThreshold == t };
            int captured = t;
            item.Click += (_, _) =>
            {
                _settings.LowBatteryThreshold = captured;
                _settings.Save();
                _warned.Clear();
                _ = RefreshAsync();
            };
            threshold.DropDownItems.Add(item);
        }
        menu.Items.Add(threshold);

        var startup = new ToolStripMenuItem("Start with Windows")
        {
            Checked = StartupManager.IsEnabled(),
            CheckOnClick = true
        };
        startup.Click += (_, _) => StartupManager.SetEnabled(startup.Checked);
        menu.Items.Add(startup);

        menu.Items.Add(new ToolStripSeparator());

        var exit = new ToolStripMenuItem("Exit");
        exit.Click += (_, _) =>
        {
            _notifyIcon.Visible = false;
            ExitThread();
        };
        menu.Items.Add(exit);
    }

    private static string Format(BatteryDevice d)
    {
        string pct = d.Percent.HasValue ? $"{d.Percent.Value}%" : "battery unknown";
        string state = d.State == BatteryState.Charging ? " (charging)" : "";
        return $"{d.Name}: {pct}{state}";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _currentIcon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
