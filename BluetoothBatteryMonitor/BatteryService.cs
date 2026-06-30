namespace XboxControllerBattery;

/// <summary>
/// Aggregates the two providers into one device list. Controllers and generic Bluetooth devices
/// come from different Windows sources and effectively never overlap, so no dedup is needed.
/// </summary>
public static class BatteryService
{
    public static async Task<IReadOnlyList<BatteryDevice>> ReadAllAsync()
    {
        var all = new List<BatteryDevice>();

        all.AddRange(BatteryReader.Read());                              // controllers (sync, fast)
        all.AddRange(await GenericBluetoothBatteryProvider.ReadAsync()); // mice/headsets/etc.

        return all;
    }
}
