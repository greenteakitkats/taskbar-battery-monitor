using Windows.Devices.Enumeration;
using Windows.Gaming.Input;
using Windows.System.Power;

// Standalone diagnostic for BOTH battery sources, so you can confirm what works on YOUR hardware
// before trusting the tray app:
//   1. Controllers via Windows.Gaming.Input (raw mWh + computed %)
//   2. Generic Bluetooth devices via the Windows battery-level property (mice/headsets/etc.)
//
//   dotnet run -c Release        (in this folder)
//
// Press Ctrl+C to quit.

// DEVPKEY_Device_BatteryLevel — the % Windows shows in Settings > Bluetooth & devices.
const string BatteryLevelKey = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";

Console.WriteLine("Battery Probe — controllers + generic Bluetooth");
Console.WriteLine("Polling every 3s. Power on your devices… Ctrl+C to quit.");
Console.WriteLine(new string('-', 70));

RawGameController.RawGameControllerAdded += (_, c) => Console.WriteLine($"[+] controller: {SafeName(c)}");
RawGameController.RawGameControllerRemoved += (_, c) => Console.WriteLine($"[-] controller: {SafeName(c)}");

while (true)
{
    Console.WriteLine($"{DateTime.Now:HH:mm:ss}");

    // --- 1. Controllers ---------------------------------------------------
    var controllers = RawGameController.RawGameControllers;
    Console.WriteLine($"  Controllers: {controllers.Count}");
    foreach (var controller in controllers)
    {
        string name = SafeName(controller);
        try
        {
            var report = controller.TryGetBatteryReport();
            if (report == null)
            {
                Console.WriteLine($"    {name}: no battery report (null)");
                continue;
            }

            int? remaining = report.RemainingCapacityInMilliwattHours;
            int? full = report.FullChargeCapacityInMilliwattHours;
            string pct = (remaining.HasValue && full.HasValue && full.Value > 0)
                ? $"{Math.Clamp((int)Math.Round(remaining.Value * 100.0 / full.Value), 0, 100)}%"
                : "n/a";

            Console.WriteLine(
                $"    {name}: {pct}  [status={report.Status}, remaining={Fmt(remaining)}, full={Fmt(full)} mWh]");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    {name}: read threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    // --- 2. Generic Bluetooth devices reporting a battery level -----------
    Console.WriteLine("  Bluetooth devices reporting battery:");
    try
    {
        var devices = await DeviceInformation.FindAllAsync(
            aqsFilter: "",
            additionalProperties: new[] { BatteryLevelKey },
            kind: DeviceInformationKind.Device);

        int shown = 0;
        foreach (var d in devices)
        {
            if (d.Properties.TryGetValue(BatteryLevelKey, out object? value) && value is byte level)
            {
                Console.WriteLine($"    {d.Name}: {level}%   [id={d.Id}]");
                shown++;
            }
        }
        if (shown == 0)
            Console.WriteLine("    (none reported a battery level)");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"    enumeration failed: {ex.GetType().Name}: {ex.Message}");
    }

    Console.WriteLine();
    await Task.Delay(3000);
}

static string Fmt(int? v) => v.HasValue ? v.Value.ToString() : "null";

static string SafeName(RawGameController c)
{
    try
    {
        string? n = c.DisplayName;
        return string.IsNullOrWhiteSpace(n) ? "Controller" : n!;
    }
    catch
    {
        return "Controller";
    }
}
