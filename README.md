# BCom

[简体中文](README.zh-CN.md)

**BCom** is a free and open-source cross-platform serial port debugging assistant, built with **.NET 10 LTS and Avalonia UI 12**.

Supported desktop platforms: **Windows, macOS, and Linux**.

## Features

- Scan and select serial ports, baud rates, data bits, stop bits, and parity.
- Send and receive text or hexadecimal data.
- Append CRLF, repeatedly send at a configurable interval, and toggle RTS/DTR.
- Open text files for sending and export received data.
- Built-in English / Simplified Chinese interface; defaults to the operating system language and supports manual switching.
- Launch platform-appropriate utilities (calculator, editor, terminal, screenshot tool) when installed.
- **Headless CLI & AI Agent Integration**: Automate serial interactions (list ports, query/response, binary HEX, streaming) with structured JSON output for scripts and AI agents.

## Command-Line Interface (CLI) & AI Agent Skill

BCom supports dual execution modes: launch without arguments for the graphical macOS-style interface, or pass subcommands for headless automation:

```bash
# List detected serial ports in JSON format
bcom ports --json

# Send ASCII / AT command with CRLF and capture response
bcom send /dev/tty.usbserial-10 "AT" --crlf --wait 1000 --json

# Send raw hexadecimal payload
bcom send COM3 "01 03 00 00 00 02 C4 0B" --hex --wait 500

# Monitor serial output for 5000ms
bcom monitor /dev/ttyACM0 --baud 115200 --duration 5000 --json

# Stream timestamped NDJSON events, including long-running captures
bcom monitor /dev/ttyACM0 --baud 115200 --duration 0 --jsonl > capture.ndjson
```

In the GUI, the received-data display keeps the latest **128 KiB** of bytes to avoid unbounded memory growth; the visible-text export reflects this limited window, not a full historical capture. For persistent monitoring, use CLI `monitor --jsonl` with output redirection. The GUI also supports custom baud rates, a follow-output toggle, and persistence of the last successful serial connection settings.

A ready-to-use AI agent skill is included in [`.claude/skills/bcom-serial/SKILL.md`](.claude/skills/bcom-serial/SKILL.md) for Claude Code and automated agents to seamlessly discover and debug hardware.

## Downloads

**For current builds**, use the [GitHub Actions builds](https://github.com/bestyize/BCom/actions/workflows/build.yml) or [GitHub Releases](https://github.com/bestyize/BCom/releases). Releases published before v3.0.0 are the legacy Windows Forms edition; the historical 2.0 EXE is not a .NET 10 package.

| Platform | Package | Notes |
| --- | --- | --- |
| Windows x64 | `BCom-*-win-x64.zip` | Extract and run `BCom.exe` |
| macOS Apple Silicon | `BCom-*-osx-arm64.dmg` | Open DMG and drag `BCom.app` to Applications |
| macOS Intel | `BCom-*-osx-x64.dmg` | Open DMG and drag `BCom.app` to Applications |
| Linux x64 | `BCom-*-linux-x64.tar.gz` | Extract and run `./BCom` |

All packages are self-contained and do not need a separate .NET runtime. **Official macOS DMGs are only included in future releases when Developer ID signing and Apple notarization succeed.** Otherwise, ad-hoc signed macOS test builds remain available as GitHub Actions artifacts and are excluded from the Release download list.

### macOS installation and Gatekeeper

macOS applies Gatekeeper checks to applications downloaded from the internet. If the app is not signed with a paid Apple Developer ID certificate, macOS may show a dialog saying the app is **"damaged and cannot be opened"** or from an unidentified developer:
1. Drag `BCom.app` into your **Applications** folder.
2. Open **Terminal**.
3. Run the following command to clear the download quarantine attribute:
   ```bash
   xattr -cr /Applications/BCom.app
   ```
4. Double-click `BCom.app` to open it normally. Alternatively, click "Open Anyway" in **System Settings -> Privacy & Security**.

When Apple Developer credentials are configured in repository secrets, the workflow automatically notarizes and staples the DMG packages for seamless opening.

### Launching on Linux

```bash
tar -xzf BCom-*-linux-x64.tar.gz
cd BCom
chmod +x BCom
./BCom
```

The Linux backend requires a graphical desktop and native libraries; see [Avalonia supported platforms](https://docs.avaloniaui.net/docs/supported-platforms).

## Build from source

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet restore BCom.csproj
dotnet run --project BCom.csproj
dotnet publish BCom.csproj -c Release -r <runtime-id> --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false
```

Replace `<runtime-id>` with `win-x64`, `osx-arm64`, `osx-x64`, or `linux-x64`. The active application is in `CrossPlatform/`; the legacy WinForms source and form resources remain in the repository as a migration reference and are excluded from the build.

### Serial event timeline and search (in development)

The next development branch adds a switchable **event timeline** to the
receive pane. Each low-level serial read or completed write is shown with
local-time millisecond precision, RX/TX direction, port name and hexadecimal
bytes. Events are **serial API chunks**, not decoded protocol frames.
The timeline has its own 128 KiB byte budget; the search bar finds the next
case-insensitive occurrence in the currently displayed window. Neither the
timeline nor the search includes evicted history. For full captures, use
`monitor --jsonl` and redirect output to a file.

## Automated serial integration testing

On Linux, the CI pipeline tests the **published** BCom executable against a
real pseudo-terminal (PTY). This verifies that CLI send/response and streaming
NDJSON monitoring actually exchange raw bytes through `System.IO.Ports`,
including non-printable bytes. The harness uses Python's standard library:

```bash
dotnet publish BCom.csproj -c Release -r linux-x64 --self-contained true -o publish
python3 Tests/serial_pty_integration.py --binary ./publish/BCom
```

The PTY tests do **not** validate USB serial driver compatibility, hardware
flow control, unplug/reconnect behavior, device-specific latency or sustained
high-baud throughput. Those still require real adapters and connected devices.

## Localization

The interface follows the operating system's language by default: Chinese-speaking systems use Simplified Chinese; other systems use English. Change it in the app's **Language** selector. The choice is saved in the user profile. Strings live in `CrossPlatform/I18n.cs` (English fallback and Simplified Chinese dictionary).

## Automated builds and releases

GitHub Actions builds packages for Windows, macOS Intel, macOS Apple Silicon, and Linux on pulls, pushes, and manual triggers.

### Publishing Releases

The v3.3.0 release adds bounded GUI receive history, reliable background serial
writes, custom baud rates, persistent connection profiles, streamed NDJSON output
and Linux PTY integration tests.

A maintainer can also publish directly from a reviewed merge into `master`
by including `[publish-release]` in the **merge commit message**. The release
workflow tags the exact built commit, and publishes Windows/Linux packages.
macOS DMGs are only attached when Apple notarization is configured and succeeds.


1. **Via Git Tag (Recommended)**:
   ```bash
   git tag v3.3.0
   git push origin v3.3.0
   ```
2. **Via GitHub Actions Manual Dispatch**:
   - Go to the **Actions** tab -> select **Build and release BCom**.
   - Click **Run workflow**, check `Publish as GitHub Release`, and enter the version tag (e.g. `v3.3.0`).

The pipeline validates package archives, generates SHA-256 checksums, and publishes Windows/Linux installers. macOS DMGs are published to the Release only after Developer ID signing and notarization; unnotarized DMGs are CI-only test artifacts.

If you have an Apple Developer account, configure the following repository secrets to automatically enable official Apple notarization:

- `MACOS_CERTIFICATE_BASE64`: Base64-encoded exported Developer ID Application .p12 (including private key).
- `MACOS_CERTIFICATE_PASSWORD`: Password for that .p12 export.
- `APPLE_ID`: Apple ID with notarization access.
- `APPLE_TEAM_ID`: Your Apple Developer team ID.
- `APPLE_APP_SPECIFIC_PASSWORD`: An Apple ID app-specific password for `notarytool`.

Developer ID signing and notarization require a paid Apple Developer Program membership. Never commit a certificate or password to this repository. If a release is tagged with a partially configured credential set, the macOS job fails rather than publishing an invalid package.

```bash
git tag v3.3.0
git push origin v3.3.0
```

See Apple's [Developer ID distribution guidance](https://developer.apple.com/macos/distribution) and [Avalonia macOS packaging instructions](https://docs.avaloniaui.net/docs/deployment/macos).

## Legacy screenshots

The following screenshots show the **original Windows Forms edition**, not the new Avalonia interface.

![Legacy main window](screenshot/mainform.PNG)

![Legacy toolbar](screenshot/mainform_tool.PNG)

## License

See [LICENSE](LICENSE).
