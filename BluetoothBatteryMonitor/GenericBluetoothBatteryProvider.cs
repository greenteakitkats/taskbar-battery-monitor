using Windows.Devices.Enumeration;

namespace XboxControllerBattery;

/// <summary>
/// Provider #2: battery for paired non-controller Bluetooth devices (mice, keyboards, headsets,
/// speakers) via the Windows device "battery level" property — the same value shown in
/// Settings > Bluetooth &amp; devices. This is the same source BTBatteryWatch reads.
///
/// IMPORTANT: this WinRT enumeration is the part most likely to need tuning on real hardware —
/// which device objects actually carry the property, and which transports expose it, varies.
/// Validate with ControllerBatteryProbe first; coverage depends on each device's firmware.
/// </summary>
public static class GenericBluetoothBatteryProvider
{
    // DEVPKEY_Device_BatteryLevel — the battery percentage Windows surfaces for the device.
    private const string BatteryLevelKey = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";

    public static async Task<IReadOnlyList<BatteryDevice>> ReadAsync()
    {
        var results = new List<BatteryDevice>();

        try
        {
            var requested = new[] { BatteryLevelKey };

            // Enumerate device objects, asking for the battery property; keep the ones that report it.
            var devices = await DeviceInformation.FindAllAsync(
                aqsFilter: "",
                additionalProperties: requested,
                kind: DeviceInformationKind.Device);

            foreach (var d in devices)
            {
                if (d.Properties.TryGetValue(BatteryLevelKey, out object? value)
                    && value is byte level
                    && level <= 100)
                {
                    results.Add(new BatteryDevice(
                        Id: d.Id,
                        Name: string.IsNullOrWhiteSpace(d.Name) ? "Bluetooth device" : d.Name,
                        Percent: level,
                        State: BatteryState.Unknown, // the property doesn't expose charging state
                        Kind: DeviceKind.Bluetooth));
                }
            }
        }
        catch
        {
            // Enumeration unsupported/failed -> report no generic devices rather than crash.
        }

        return results;
    }
}
