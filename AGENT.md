# AGENT.md - BCom Automation & Development Guidelines for AI Agents

This document guides AI agents (Claude Code, Cursor, Copilot, autonomous workflows, etc.) in controlling BCom programmatically and contributing to its codebase.

---

## 1. Project Overview

- **Name**: BCom (Cross-Platform Serial Communication Assistant)
- **Version**: 3.0.0
- **Framework**: .NET 10 LTS + Avalonia UI 12
- **Supported Platforms**: Windows (x64), macOS (Apple Silicon arm64 & Intel x86_64), Linux (x64)
- **Execution Modes**:
  1. **GUI Mode**: Native macOS-inspired visual desktop app (launched when no CLI arguments are passed).
  2. **Headless CLI Mode**: Fast, structured automation interface returning machine-readable JSON for AI agents.

---

## 2. Invoking BCom via Command-Line Interface (CLI)

### Binary Paths

- **From source**:
  ```bash
  dotnet run --project BCom.csproj -- [command] [options]
  ```
- **Published binary**:
  - macOS: `package/BCom.app/Contents/MacOS/BCom` or `./publish/BCom`
  - Linux: `./publish/BCom`
  - Windows: `publish\BCom.exe`
- **Installed system app**: `bcom [command] [options]`

---

## 3. CLI Command Reference for AI Agents

Always pass `--json` to receive structured, machine-parsable responses.

### 3.1 Discover Available Ports
```bash
bcom ports --json
```
**Response Schema**:
```json
{
  "count": 2,
  "ports": ["/dev/tty.usbserial-10", "/dev/tty.usbmodem14101"]
}
```

### 3.2 Ping / Test Port
```bash
bcom ping <port> [--baud <rate>]
```
**Success Response**:
```json
{"status": "open", "port": "/dev/tty.usbserial-10", "baud": 115200}
```
**Error Response**:
```json
{"status": "error", "port": "/dev/tty.usbserial-10", "message": "Permission denied"}
```

### 3.3 Send Data and Await Reply (Query-Response)
```bash
bcom send <port> <data> [--baud <rate>] [--crlf] [--hex] [--wait <ms>] [--rts] [--dtr] [--json]
```
- `--baud <rate>`: Baud rate (default `115200`).
- `--crlf`: Append `\r\n` (CRLF) to the payload.
- `--hex`: Interpret `<data>` as space/comma-separated hex digits (e.g. `"0A FF 12"`).
- `--wait <ms>`: Listen for reply up to `<ms>` milliseconds (default `1000`).
- `--rts`, `--dtr`: Enable hardware flow control lines.

**Response Schema**:
```json
{
  "success": true,
  "port": "/dev/tty.usbserial-10",
  "sent_bytes": 4,
  "received_bytes": 6,
  "response": "OK\r\n",
  "hex": "4F 4B 0D 0A"
}
```

### 3.4 Monitor Serial Stream
```bash
bcom monitor <port> [--baud <rate>] [--duration <ms>] [--hex] [--json]
```
- `--duration <ms>`: Capture duration in milliseconds (default `5000`).

**Response Schema**:
```json
{
  "port": "/dev/tty.usbserial-10",
  "total_bytes": 48,
  "text": "[BOOT] System ready\r\n",
  "hex": "5B 42 4F 4F 54 5D 20 53 79 73 74 65 6D 20 72 65 61 64 79 0D 0A"
}
```

---

## 4. Hardware Automation Patterns for Agents

### Pattern A: Modem / Cellular / Bluetooth (AT Commands)
1. Run `bcom ports --json` to detect the device port.
2. Query module status:
   ```bash
   bcom send <port> "AT" --crlf --wait 1000 --json
   ```
3. Query signal quality:
   ```bash
   bcom send <port> "AT+CSQ" --crlf --wait 1000 --json
   ```

### Pattern B: Microcontrollers (Arduino / ESP32 / STM32)
1. Trigger reset or bootloader if needed:
   ```bash
   bcom send <port> "RESET" --dtr --rts --crlf
   ```
2. Monitor initial boot logs:
   ```bash
   bcom monitor <port> --baud 115200 --duration 3000 --json
   ```

### Pattern C: Industrial Sensors & Modbus RTU (Binary HEX)
1. Read Holding Registers (Function Code 03):
   ```bash
   bcom send <port> "01 03 00 00 00 02 C4 0B" --baud 9600 --hex --wait 500 --json
   ```
2. Inspect the returned `"hex"` field for the payload bytes.

---

## 5. Codebase Architecture & Extension Guidelines

```
CrossPlatform/
├── Core/                              # Headless business logic (Zero UI dependencies)
│   ├── CliHandler.cs                  # CLI command dispatch and JSON serialization
│   ├── SerialConfig.cs                # Configuration models and standard options
│   ├── SerialDataConverter.cs         # HEX/UTF-8 conversion and CRLF handling
│   ├── SerialService.cs               # Background serial communication and polling
│   └── SystemLauncher.cs              # System utility execution
├── UI/
│   ├── Theme/MacTheme.cs              # Apple HIG design tokens (Day/Night palettes)
│   ├── Dialogs/MacDialog.cs           # Reusable modal alerts and QR dialogs
│   └── Components/
│       ├── TopNavBar.cs               # Header, status pill, language & theme controls
│       ├── MonitorView.cs             # Console output, input box, badges, hex toggles
│       └── SettingsPanel.cs           # Inspector panel for serial port parameters
├── MainWindow.cs                      # Window coordinator & event wiring
├── I18n.cs                            # English / Chinese dictionary & settings persistence
└── Program.cs                         # Application entry point (CLI routing + Avalonia App)
```

### Critical Development Rules for AI Agents:

1. **Trimming & Native Compatibility**:
   - `BCom.csproj` enforces `<PublishTrimmed>true</PublishTrimmed>` and `<TrimMode>partial</TrimMode>`.
   - **Do NOT introduce dynamic reflection** (`Type.GetType`, unannotated `Activator.CreateInstance`).
   - For JSON serialization, use source generators or direct string formatting to keep binary size under 17MB.

2. **Localization**:
   - Add all UI strings to both English and Chinese dictionaries in `CrossPlatform/I18n.cs`.
   - Default language follows the operating system (`auto`).

3. **Day/Night Theme Adaptation**:
   - Never hardcode color hex values in UI components.
   - Reference `MacTheme.TextPrimary`, `MacTheme.BgCard`, `MacTheme.BorderMuted`, etc.
   - Implement `ApplyTheme()` on components to react dynamically to `ActualThemeVariantChanged`.

4. **Comments**:
   - All code comments and XML documentation must be in **English**.

5. **Packaging & Releases**:
   - Version bumps must update `<Version>` in `BCom.csproj`.
   - Release tags must follow semver `vMAJOR.MINOR.PATCH` (e.g. `v3.0.0`).
   - macOS packaging script automatically generates DMG with ad-hoc signing fallback.
