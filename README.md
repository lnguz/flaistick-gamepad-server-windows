# FlaiStick — Windows Server

A background .NET 8 tray application that receives gamepad input streamed over UDP from the [flaistick-app](../flaistick-app/README.md) Android app and exposes each physical controller as a virtual Xbox 360 controller via [ViGEmBus](https://github.com/ViGEm/ViGEmBus), so any game or app that supports XInput sees a real gamepad.

## Requirements

**To run it (any PC):**
- Windows 10/11 (x64)
- [ViGEmBus driver](https://github.com/ViGEm/ViGEmBus/releases) installed (also installable via `winget install ViGEm.ViGEmBus`)
- Firewall rules allowing inbound UDP on the configured game port (`9000`) and discovery port (`47998`) — the installer sets these up automatically

**To build it from source (dev machine only):**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Inno Setup 6](https://jrsoftware.org/isinfo.php) (only if you want to rebuild the installer — `winget install JRSoftware.InnoSetup`)

## Project layout

```
flaistick-gamepad-server-windows/
  Program.cs               Entry point: process priority, ViGEmBus init, tray + server wiring
  TrayApp.cs                 System tray icon (NotifyIcon) + Exit menu
  UdpServer.cs               UDP receive loop, routes packets by opcode/length
  DiscoveryServer.cs           Answers LAN broadcast/unicast discovery requests, reports hostname + MAC
  PacketParser.cs             Applies a 12-byte state packet to a virtual controller
  ReorderPacket.cs             Parses the player-reorder command
  RemoteInput.cs               SendInput (Win32) wrapper: mouse move/click/scroll, keyboard, Unicode text
  RemoteInputPacket.cs          Parses mouse/keyboard/text packets and calls RemoteInput
  ControllerHub.cs            Owns the ViGEmClient + per-device virtual controllers
  StartupRegistration.cs       Scheduled Task (ONLOGON) registration helper
  installer.iss               Inno Setup script for the distributable installer
```

## Installing on another PC (recommended — no build tools needed)

1. Copy `dist\FlaiStickGamepadServerSetup.exe` to the target PC and run it (admin rights required).
2. The installer will:
   - Copy the app to `Program Files`
   - Add a Start Menu shortcut (and uninstaller)
   - Create the Windows Firewall inbound rules for UDP port 9000 (gamepad data) and 47998 (LAN discovery)
   - Install the ViGEmBus driver automatically (via `winget install ViGEm.ViGEmBus`) if it isn't already present, and warn you with manual install instructions if that fails
   - Launch the app once, which registers itself to auto-start on login (a Scheduled Task with an `ONLOGON` trigger) and shows a tray icon
3. If warned that ViGEmBus couldn't be installed automatically, install it manually (`winget install ViGEm.ViGEmBus`) and relaunch the app from the Start Menu shortcut or tray.

The app runs as a normal desktop (Win32) tray application — Windows' Game Mode / Xbox Game Bar does not suspend or close background Win32 apps the way it can with UWP/Store apps, so it keeps running while you game. Its process priority is also bumped to `AboveNormal` at startup so it isn't starved of CPU time.

To close it, right-click the tray icon and choose **Exit**.

## Building & running from source

```
dotnet build
dotnet run
```

When run from an existing terminal with `dotnet run`, diagnostic lines are printed showing:
- when the UDP listener starts,
- roughly every 200th packet received per device (proof that input is arriving),
- when a virtual controller connects or times out,
- when a player-reorder request is processed.

There is no other logging — this is intentional, since the shipped behavior is a silent background tray app. In particular, mouse/keyboard/text packets are not logged at all (they'd be far too frequent — every cursor-move delta would spam the console).

## Publishing a distributable build

Produces a single self-contained `.exe` (no .NET runtime required on the target PC):

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish
```

Output: `publish\GamepadServer.exe`.

## Building the installer

Requires [Inno Setup 6](https://jrsoftware.org/isinfo.php). After publishing (previous step):

```powershell
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" installer.iss
```

> Note: `winget install JRSoftware.InnoSetup` installs to `%LOCALAPPDATA%\Programs\Inno Setup 6`, not `Program Files` — adjust the path above if you installed it differently.

Output: `dist\FlaiStickGamepadServerSetup.exe` — copy this single file to any Windows 11 PC to install everything.

## First-time setup checklist (building/running from source)

1. Install the .NET 8 SDK and the ViGEmBus driver.
2. Confirm the PC's LAN IP (`ipconfig`) matches what's configured in the Android app.
3. Open the inbound UDP ports in Windows Firewall:
   ```powershell
   New-NetFirewallRule -DisplayName "GamepadServer UDP 9000" -Direction Inbound -Protocol UDP -LocalPort 9000 -Action Allow -Profile Any
   New-NetFirewallRule -DisplayName "GamepadServer UDP 47998 Discovery" -Direction Inbound -Protocol UDP -LocalPort 47998 -Action Allow -Profile Any
   ```
   This is required on networks with the "Public" profile, which blocks unsolicited inbound traffic by default. (The installer does this step for you automatically.)
4. Run `dotnet run` and confirm you see `Listening for gamepad packets on UDP port 9000...` and `Discovery listener active on UDP port 47998...`.
5. Connect from the Android app's PC list — a `discovery request from ...` line should appear as soon as the app scans, and once you tap the PC and start playing, a `virtual Xbox 360 controller connected for device <id>` line should appear; the controller should also show up under Windows' "Set up USB game controllers" (`joy.cpl`).

## How it works

- `Program.cs` raises its own process priority, initializes `ControllerHub` (which constructs the `ViGEmClient`) inside a try/catch — if that throws (ViGEmBus missing or not loaded), a clear error dialog is shown and the app exits instead of failing silently — then starts the UDP server and the discovery server on background tasks, and runs a Windows Forms message loop (`Application.Run`) on the main (STA) thread purely to host the tray icon — there is no visible window.
- `DiscoveryServer` listens on a dedicated UDP port (`47998`) for a 4-byte request (sent as either a broadcast scan or a direct unicast "are you awake" ping from the app), and replies directly to the sender with the PC's hostname, the gamepad-data port, and the MAC address of its active network adapter — the phone caches all of it, so it never needs a manually-typed IP and can send a Wake-on-LAN packet later if this PC stops responding.
- `UdpServer` binds a single UDP socket and routes every packet on the game port: an exact **12 bytes** is a gamepad state update; `2 + 4×N` bytes starting with opcode `0xAA` is a player-reorder command; anything else is handed to `RemoteInputPacket`, which checks the first byte against its own opcode set (`0xAB`–`0xB0`) for mouse/keyboard/text packets.
- `RemoteInputPacket` parses mouse move/button/scroll, single key events, key combos, and Unicode text packets, calling into `RemoteInput`, a thin `user32.dll` `SendInput` P/Invoke wrapper — no driver needed for this part (unlike the gamepad side), since Windows accepts synthetic mouse/keyboard input from any normal process. Text is injected with `KEYEVENTF_UNICODE`, so it works regardless of the PC's keyboard layout; key combos are pressed in order and released in reverse order.
- `ControllerHub` maintains one `IXbox360Controller` per Android device ID, created lazily on first packet and disposed automatically if no packet arrives for **3 seconds** (a background sweep runs every 2 seconds). The Android client sends a heartbeat every 200 ms specifically to avoid tripping this timeout during normal play.
- `PacketParser` maps the 12-byte packet directly onto `IXbox360Controller` button/axis/slider state using the standard XInput button bitmask.
- Player reordering is best-effort: ViGEmBus has no API to directly assign an XInput player slot, so `ControllerHub.ReorderAsync` disconnects the requested controllers and reconnects them in the desired sequence (with a short delay between each), relying on Windows assigning slots in connection order.
- `StartupRegistration` creates (or refreshes) a per-user Scheduled Task with an `ONLOGON` trigger on every launch, so the app starts silently on next login — this is handled by the Task Scheduler service directly and, unlike the classic `HKCU\...\Run` key (which is only processed by `explorer.exe`), still fires even when Windows boots straight into an alternate shell such as the Xbox full-screen experience used on handheld gaming PCs. It also cleans up any leftover `Run` key entry from older versions of this app.
- `installer.iss` bundles the published exe, creates the firewall rule, installs the ViGEmBus driver via winget if missing (warning with manual instructions if that fails), and cleans up the firewall rule + scheduled task on uninstall.

## Wire protocol

See the [flaistick-app README](../flaistick-app/README.md#wire-protocol) for the full packet layouts (gamepad state, player-order command, discovery, and Wake-on-LAN) — both sides must stay in sync if the protocol changes.

## Enabling Wake-on-LAN on this PC

The app can send a Wake-on-LAN magic packet to wake this PC if it doesn't respond to a connection attempt, but the PC's network adapter has to be configured to accept it first:

1. **Device Manager** → Network adapters → (your Ethernet/Wi-Fi adapter) → Properties → **Power Management** tab → check "Allow this device to wake the computer" (and ideally "Only allow a magic packet to wake the computer").
2. **Advanced** tab → set "Wake on Magic Packet" (or similarly named property) to **Enabled**.
3. In the PC's **BIOS/UEFI**, enable "Wake on LAN" / "Power On by PCI-E" (naming varies by motherboard vendor) if the option exists — required for waking from a fully powered-off (S5) state; not needed if you only sleep (S3) the PC.
4. Wi-Fi-based WOL is unreliable on most consumer hardware/drivers — a wired Ethernet connection is strongly recommended for this feature to work consistently.

## Troubleshooting

- **PC doesn't show up in the app's list**: confirm both firewall rules exist (9000 and 47998), that the phone and PC are on the same Wi-Fi network, and that the network doesn't have AP/Client Isolation enabled (common on guest networks — it blocks broadcast traffic between devices).
- **Controller shows in the app but nothing happens in Windows**: check the firewall rule for port 9000 and that the phone actually selected the right PC from the list.
- **Controller connects then disappears after a few seconds**: this means packets stopped arriving — check Wi-Fi stability; the 200 ms client heartbeat should otherwise prevent this during normal use.
- **Wake-on-LAN doesn't wake the PC**: verify the adapter/BIOS settings above, and prefer a wired connection — this is a hardware/driver limitation, not something the app can work around.
- **ViGEmBus warning on startup**: install it with `winget install ViGEm.ViGEmBus`, restart the PC, then relaunch the app.
- **Mouse/keyboard from the app does nothing on the PC**: this shares the same UDP port and firewall rule as the gamepad data (port 9000), so if the controller works but the mouse/keyboard doesn't, it's likely the target window is running elevated (as Administrator) while the server isn't — see the known limitation about UIPI in the [flaistick-app README](../flaistick-app/README.md#known-limitations).
- **File lock errors when rebuilding (`R.jar`/`app/build` in use)**: this is a Kotlin/Gradle language-server issue on the Android side, not this project — see the note in the Android client's build if you hit it there. On this project, stop any lingering `dotnet`/`GamepadServer` processes before rebuilding if `bin`/`obj` are locked.
