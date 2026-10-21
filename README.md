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

## Downloads

**For current builds**, use the [GitHub Actions builds](https://github.com/bestyize/BCom/actions/workflows/build.yml). Releases published before v2.1.0 are the legacy Windows Forms edition; the historical 2.0 EXE is not a .NET 10 package.

| Platform | Package | Notes |
| --- | --- | --- |
| Windows x64 | `BCom-*-win-x64.zip` | Extract and run `BCom.exe` |
| macOS Apple Silicon | `BCom-*-osx-arm64.dmg` | Open DMG and drag `BCom.app` to Applications |
| macOS Intel | `BCom-*-osx-x64.dmg` | Open DMG and drag `BCom.app` to Applications |
| Linux x64 | `BCom-*-linux-x64.tar.gz` | Extract and run `./BCom` |

All packages are self-contained and do not need a separate .NET runtime.

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

## Localization

The interface follows the operating system's language by default: Chinese-speaking systems use Simplified Chinese; other systems use English. Change it in the app's **Language** selector. The choice is saved in the user profile. Strings live in `CrossPlatform/I18n.cs` (English fallback and Simplified Chinese dictionary).

## Automated builds and releases

GitHub Actions builds packages for Windows, macOS Intel, macOS Apple Silicon, and Linux on pulls, pushes, and manual triggers.

### Publishing Releases

1. **Via Git Tag (Recommended)**:
   ```bash
   git tag v2.1.0
   git push origin v2.1.0
   ```
2. **Via GitHub Actions Manual Dispatch**:
   - Go to the **Actions** tab -> select **Build and release BCom**.
   - Click **Run workflow**, check `Publish as GitHub Release`, and enter the version tag (e.g. `v2.1.0`).

The pipeline validates package archives, generates SHA-256 checksums, and uploads complete installation assets for all 4 platforms to the release.

If you have an Apple Developer account, configure the following repository secrets to automatically enable official Apple notarization:

- `MACOS_CERTIFICATE_BASE64`: Base64-encoded exported Developer ID Application .p12 (including private key).
- `MACOS_CERTIFICATE_PASSWORD`: Password for that .p12 export.
- `APPLE_ID`: Apple ID with notarization access.
- `APPLE_TEAM_ID`: Your Apple Developer team ID.
- `APPLE_APP_SPECIFIC_PASSWORD`: An Apple ID app-specific password for `notarytool`.

Developer ID signing and notarization require a paid Apple Developer Program membership. Never commit a certificate or password to this repository. If a release is tagged with a partially configured credential set, the macOS job fails rather than publishing an invalid package.

```bash
git tag v2.1.0
git push origin v2.1.0
```

See Apple's [Developer ID distribution guidance](https://developer.apple.com/macos/distribution) and [Avalonia macOS packaging instructions](https://docs.avaloniaui.net/docs/deployment/macos).

## Legacy screenshots

The following screenshots show the **original Windows Forms edition**, not the new Avalonia interface.

![Legacy main window](screenshot/mainform.PNG)

![Legacy toolbar](screenshot/mainform_tool.PNG)

## License

See [LICENSE](LICENSE).
