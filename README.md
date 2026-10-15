# Chromebook Keyboard Helper for Windows

A small Windows tray utility that maps the **Lenovo 14e Chromebook Gen 3
(Pujjo/Pujjoteen)** top-row keys to Windows functions. It runs in user mode;
it does not install a kernel driver or require Windows test mode.

This is an early preview. The executable has compiled and started successfully
on the target Windows laptop. Physical-key and Japanese IME validation are
still pending; broad Chromebook compatibility is not established.

## Key mapping

| Chromebook key | Windows behavior |
|---|---|
| Back | Browser back |
| Refresh | Browser refresh |
| Fullscreen | F11 |
| Overview | Task view (Win+Tab) |
| Brightness down / up | Change internal display brightness in 5% steps |
| Microphone mute | Toggle the default microphone's mute state, if available |
| Speaker mute / volume down / volume up | Standard Windows volume controls |
| Search / Launcher | Open Windows Start |
| Search + F1–F10 | Send the ordinary function key |
| Alt + Search | Toggle Caps Lock |

The main mapping uses the F1–F10 events emitted by the Chromebook's firmware.
Several extended Vivaldi scan codes are also supported. Search plus a top-row
key currently requires the firmware to emit F1–F10. This is the model's
10-key row with microphone mute; other Chromebook top rows differ.

Other modified function-key shortcuts pass through. Function keys on attached
external keyboards can also be affected. The helper operates in the signed-in
desktop and may not work in elevated apps, secure sign-in, or UAC prompts.
Its tray menu lets you pause the mappings or exit.

Brightness requires Windows WMI monitor-brightness support. Microphone mute
requires an existing capture endpoint; this utility does not supply missing
audio drivers or a microphone endpoint.

## Download and run

Download `ChromebookKeys.exe` from
[Releases](https://github.com/chevydan6/chromebook-keyboard-helper/releases)
and double-click it. Uses the built-in Windows .NET Framework. The executable
is unsigned; published SHA-256 checksums are included with the release.
Run only one copy of the helper at a time.

For automatic startup, extract the release ZIP and run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install.ps1
```

The installer copies the executable to
`%LOCALAPPDATA%\ChromebookKeyboardHelper` and adds a startup entry for your
account. No administrator rights are required. `Uninstall.ps1` removes that
installation and startup entry.

## Japanese keyboards

The helper does **not** configure Japanese text input. For a Japanese keyboard,
add Japanese/Microsoft IME in Windows Settings and select the Japanese
106/109 hardware layout. With Japanese IME active, the 半角/全角 key above Tab
switches between Latin and Japanese input. The hardware-layout change can
require a restart. English Windows display language can be retained.

Machine-specific setup scripts, registry backups, crash dumps, and personal
diagnostics are deliberately excluded from this repository and download.

## Build and privacy

Run `Build.ps1` on 64-bit Windows with .NET Framework 4.x. The source is
`src/ChromebookKeys.cs`; output is `dist/ChromebookKeys.exe`.

The low-level keyboard hook inspects events to remap keys. It does not record
typed characters or clipboard data and has no network functionality.
`helper-status.txt` contains startup status or control-availability errors.

The model's top-row reference is the
[ChromeOS Pujjo keyboard configuration](https://github.com/coreboot/chrome-ec/blob/main/zephyr/program/nissa/pujjo/keyboard.dtsi).

MIT licensed; see [LICENSE](LICENSE).
