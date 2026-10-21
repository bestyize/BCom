---
name: bcom-serial
description: Control serial communication through BCom CLI. Discover ports, ping devices, send ASCII/HEX payloads with wait-reply, and monitor serial streams from connected microcontrollers (ESP32, Arduino, STM32, Modbus, 4G/GPS modems, USB-UART adapters). Use whenever the user wants to talk to a serial device, send AT commands, read hardware logs, or debug microcontrollers.
---

# BCom Serial Automation Skill

This skill allows Claude and AI agents to interface directly with hardware serial ports (COM ports on Windows, `/dev/tty.*` on macOS, `/dev/ttyUSB*` / `/dev/ttyACM*` on Linux) using the **BCom CLI**.

## Finding the BCom Executable

- **Local project build / development**:
  `dotnet run --project BCom.csproj -- [command] [options]`
  or published binary:
  - macOS: `./publish/BCom` or `package/BCom.app/Contents/MacOS/BCom`
  - Linux: `./publish/BCom`
  - Windows: `publish\BCom.exe`
- **Installed system app**: `bcom [command] [options]`

---

## Command Reference

### 1. List Available Serial Ports
Always run this first to discover connected microcontrollers or adapters:

```bash
bcom ports --json
```

**JSON Output Example**:
```json
{"count":2,"ports":["/dev/tty.usbserial-10","/dev/tty.usbmodem14101"]}
```

Plain-text output:
```bash
bcom ports
```

---

### 2. Verify / Ping Port
Checks if a port can be opened at the specified baud rate:

```bash
bcom ping /dev/tty.usbserial-10 --baud 115200
```

---

### 3. Send Data and Capture Response (Query-Response)
Sends ASCII or HEX data to the device and waits up to `--wait <ms>` (default: 1000ms) for the response:

#### A. Standard ASCII / AT Command (with CRLF):
```bash
bcom send /dev/tty.usbserial-10 "AT" --baud 115200 --crlf --wait 1000 --json
```

**JSON Output Example**:
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

#### B. Hexadecimal / Binary Protocol (Modbus, Sensor, Firmware):
```bash
bcom send /dev/tty.usbserial-10 "01 03 00 00 00 02 C4 0B" --baud 9600 --hex --wait 500 --json
```

#### C. Flow Control Signals (RTS / DTR):
Toggle hardware flow control signals before sending (useful for ESP32/Arduino reboot or bootloader triggers):
```bash
bcom send /dev/ttyUSB0 "RESET" --dtr --rts --crlf
```

---

### 4. Monitor / Capture Serial Stream
Captures live serial data for a specified duration in milliseconds (default: 5000ms):

```bash
bcom monitor /dev/tty.usbserial-10 --baud 115200 --duration 3000 --json
```

**JSON Output Example**:
```json
{
  "port": "/dev/tty.usbserial-10",
  "total_bytes": 42,
  "text": "[BOOT] ESP32-S3 initializing...\nWiFi OK\n",
  "hex": "5B 42 4F 4F 54 5D 20 ..."
}
```

---

## Agent Workflow Patterns

1. **Step 1: Discover Devices**
   Run `bcom ports --json`. If no ports are returned, instruct the user to check the USB cable or driver (e.g. CH340, CP2102, FTDI).

2. **Step 2: Detect Firmware or Protocol**
   Send an interrogation command matching the device type:
   - Modems/cellular/Bluetooth: `"AT"`
   - GRBL / CNC: `"$$"` or `"$I"`
   - 3D Printer / Marlin: `"M115"`
   - Custom MCU CLI: `"\r\n"` or `"help"`

3. **Step 3: Analyze and Parse**
   Use `--json` on all commands to reliably parse responses into structured conclusions for the user.
