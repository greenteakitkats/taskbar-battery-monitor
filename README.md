# Taskbar Battery Monitor

A lightweight **Windows system-tray app** that shows the battery level of your wireless devices —
**game controllers** (Xbox, PlayStation, Switch Pro, …) *and* **Bluetooth mice, keyboards, headsets,
and speakers** — and warns you before they die.

It lives quietly in the notification area on your taskbar: hover for every device's battery, and pick
which one's percentage is drawn on the icon.

## Why
Most "Bluetooth battery" tools can't read game controllers (controllers report battery through a
different Windows API than mice/headsets). This app reads **both** sources and merges them, so a
single tray icon covers everything wireless.

## Features
- 🎮 **Controllers** via `Windows.Gaming.Input.RawGameController` — Xbox, PlayStation, Switch Pro, 8BitDo, etc.
- 🖱️ **Other Bluetooth devices** via the Windows battery-level property (same value Settings shows).
- 🪧 **Hover tooltip** lists every connected device + level.
- 📌 **Pick what shows on the taskbar** — choose a device, or *Auto* (follows the lowest battery).
- 🔔 **Low-battery warning** (configurable threshold), fired once per episode.
- 🎨 Dynamic, color-coded tray icon (green / amber / red, blue when charging).
- ⚙️ Run-at-login toggle; settings persist to `%LocalAppData%`.

## Download (easiest)
Grab the ready-to-run build from the [**Releases**](../../releases/latest) page — download
`BluetoothBatteryMonitor.exe` and double-click it. No .NET install required (the runtime is bundled).
Windows SmartScreen may warn (unsigned) — click *More info → Run anyway*.

## Build from source
**Prerequisite:** the free [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows x64).

1. **Test your hardware first** — in `ControllerBatteryProbe/`, double-click
   **`1 - RUN PROBE FIRST.bat`**. It prints which of your devices Windows can read a battery from.
2. **Run the app** — in `BluetoothBatteryMonitor/`, double-click **`2 - START MONITOR.bat`**.
   A battery icon appears in your system tray. Right-click it for settings.

Prefer the command line / Visual Studio? Open either `.csproj` in **Visual Studio 2022**, or run
`dotnet run -c Release` from the project folder.

See [`START HERE.txt`](START%20HERE.txt) for the step-by-step, non-developer version.

## Project layout
| Folder | What it is |
|--------|------------|
| `BluetoothBatteryMonitor/` | The tray app. |
| `ControllerBatteryProbe/` | A console diagnostic that dumps both battery sources — run it to see what works on your hardware. |

## Status & known limitations
This is an early build, not yet validated against a wide range of hardware. Please open an issue with
your probe output if something doesn't read.

- **Controller battery is coarse** — values jump in steps; a device may show `?` if it only reports
  status, not a percentage. Hardware limitation, not a bug.
- **The generic-Bluetooth source varies by device** — if a mouse/headset doesn't appear, Windows
  itself isn't exposing its battery (check Settings → Bluetooth & devices) and no app can read it.
- **Steam Controller / Steam Input** — the Steam Controller usually isn't visible to this API, and
  running a controller through Steam Input / DS4Windows can hide its real battery behind a virtual pad.
- **Polling, not push** — refreshes every 30s by default.

> Note: the app's internal C# namespace is still `XboxControllerBattery` (the project began life
> Xbox-only). It's a cosmetic rename left for a later cleanup; the assembly builds as
> `BluetoothBatteryMonitor.exe`.

## License
MIT — see [LICENSE](LICENSE).
