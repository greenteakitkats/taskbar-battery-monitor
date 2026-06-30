using Windows.Gaming.Input;
using Windows.System.Power;

namespace XboxControllerBattery;

/// <summary>
/// Provider #1: battery for connected game controllers via Windows.Gaming.Input.RawGameController.
/// Covers Xbox, PlayStation, Switch Pro, 8BitDo, etc. (Steam Controller is the known exception.)
/// </summary>
public static class BatteryReader
{
    public static IReadOnlyList<BatteryDevice> Read()
    {
        var result = new List<BatteryDevice>();

        foreach (var controller in RawGameController.RawGameControllers)
        {
            string id = SafeId(controller);
            string name = SafeName(controller);
            int? percent = null;
            var state = BatteryState.Unknown;

            try
            {
                var report = controller.TryGetBatteryReport();
                if (report != null)
                {
                    state = MapStatus(report.Status);

                    int? remaining = report.RemainingCapacityInMilliwattHours;
                    int? full = report.FullChargeCapacityInMilliwattHours;
                    if (remaining.HasValue && full.HasValue && full.Value > 0)
                    {
                        int pct = (int)Math.Round(remaining.Value * 100.0 / full.Value);
                        percent = Math.Clamp(pct, 0, 100);
                    }
                }
            }
            catch
            {
                // Some controllers throw instead of returning null; treat as "unknown".
            }

            result.Add(new BatteryDevice(id, name, percent, state, DeviceKind.Controller));
        }

        return result;
    }

    // NonRoamableId is stable per physical controller, so two identical pads stay distinct.
    private static string SafeId(RawGameController controller)
    {
        try
        {
            string? id = controller.NonRoamableId;
            if (!string.IsNullOrEmpty(id))
                return id!;
        }
        catch
        {
            // ignore
        }
        return "controller:" + SafeName(controller);
    }

    private static string SafeName(RawGameController controller)
    {
        try
        {
            string? name = controller.DisplayName;
            if (!string.IsNullOrWhiteSpace(name))
                return name!;
        }
        catch
        {
            // ignore
        }
        return "Controller";
    }

    private static BatteryState MapStatus(BatteryStatus status) => status switch
    {
        BatteryStatus.Charging => BatteryState.Charging,
        BatteryStatus.Discharging => BatteryState.Discharging,
        BatteryStatus.Idle => BatteryState.Idle,
        BatteryStatus.NotPresent => BatteryState.NotPresent,
        _ => BatteryState.Unknown
    };
}
