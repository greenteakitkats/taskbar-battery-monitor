# Bluetooth Battery Monitor (tray app)

A lightweight Windows system-tray app that shows the battery level of your wireless devices and
warns you before they die. It reads **two sources** and merges them:

1. **Game controllers** — `Windows.Gaming.Input.RawGameController` (Xbox, PlayStation, Switch Pro,
   8BitDo, etc.).
2. **Other Bluetooth devices** — mice, keyboards, headsets, speakers — via the Windows battery-level
   property (the same value Settings → Bluetooth & devices shows).

> The project folder/namespace is still `XboxControllerBattery` (the assembly builds as
> `BluetoothBatteryMonitor.exe`). Renaming the namespace is pure churn, so it's left for later.

## Features
- **Hover tooltip** lists *every* connected device and its level.
- **Pick what shows on the taskbar**: the right-click *Show on taskbar* menu lets you choose which
  device's battery is drawn on the icon, or **Auto** (follows whichever device is lowest).
- Dynamic, color-coded tray icon (green / amber / red, blue when charging).
- Low-battery balloon notification, once per episode, re-arming on recovery/charge.
- Right-click menu: per-device readout, taskbar-device picker, warnings on/off, threshold (10–30%),
  "Start with Windows", Exit.
- Settings persist to `%LocalAppData%\XboxControllerBattery\settings.json`.

## Build & run
Requires the **.NET 8 SDK** with the Windows desktop workload (Visual Studio 2022 installs both).

```powershell
dotnet build -c Release
# or open the folder in Visual Studio 2022 and press F5
```

## ⚠️ Validate the generic-Bluetooth source first
The controller source is solid. The **generic Bluetooth source is the uncertain part** — exactly
which devices expose a battery level (and the precise enumeration query) varies by hardware and
firmware. Before trusting it, run the **ControllerBatteryProbe** project; its
"Bluetooth devices reporting battery" section prints what actually shows up on your machine. If a
device is missing there, Windows itself isn't exposing its battery and no app can read it.

## Known limitations
- **Coarse battery on Xbox controllers** — values jump in steps; if only status is available the icon
  shows `?` and the tooltip says "battery unknown".
- **Connected + on only** — battery reads only while a device is powered and paired.
- **Polling, not push** — default refresh every 30s (`PollIntervalSeconds` in settings.json).
- **Steam Controller / Steam Input** — the Steam Controller usually isn't visible to this API, and
  running a controller through Steam Input / DS4Windows can hide its real battery behind a virtual pad.

## Project layout
| File | Role |
|------|------|
| `Program.cs` | Entry point; starts the tray `ApplicationContext`. |
| `TrayApplicationContext.cs` | Tray icon, timer, menu, taskbar-device picker, low-battery logic. |
| `BatteryService.cs` | Merges the two providers into one device list. |
| `BatteryReader.cs` | Provider #1 — controllers via `RawGameController`. |
| `GenericBluetoothBatteryProvider.cs` | Provider #2 — generic Bluetooth devices via the battery property. |
| `Device.cs` | Shared `BatteryDevice` model + enums. |
| `IconRenderer.cs` | Draws the dynamic percentage tray icon. |
| `StartupManager.cs` | "Start with Windows" registry toggle. |
| `Settings.cs` | Load/save JSON settings (incl. chosen taskbar device). |
