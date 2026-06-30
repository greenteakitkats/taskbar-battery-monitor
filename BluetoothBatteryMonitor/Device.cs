namespace XboxControllerBattery;

public enum BatteryState { Unknown, Discharging, Idle, Charging, NotPresent }

public enum DeviceKind { Controller, Bluetooth }

/// <summary>
/// A battery snapshot for one device, from either provider.
/// Percent is null when only coarse status is known (or none).
/// Id is a stable per-device identifier used for the "warn once" tracking and the taskbar picker.
/// </summary>
public sealed record BatteryDevice(
    string Id,
    string Name,
    int? Percent,
    BatteryState State,
    DeviceKind Kind);
