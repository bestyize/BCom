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

| Platform | CI test archive | Official release asset |
| --- | --- | --- |
| Windows x64 | `BCom-build-*-win-x64.zip` | `BCom-v*-win-x64.zip` |
| macOS Apple Silicon | `BCom-build-*-osx-arm64-UNNOTARIZED.zip` | `BCom-v*-osx-arm64.dmg` (when Apple credentials are configured) |
| macOS Intel | `BCom-build-*-osx-x64-UNNOTARIZED.zip` | `BCom-v*-osx-x64.dmg` (when Apple credentials are configured) |
| Linux x64 | `BCom-build-*-linux-x64.tar.gz` | `BCom-v*-linux-x64.tar.gz` |

All packages are self-contained and do not need a separate .NET runtime. For Windows, extract the ZIP and run `BCom.exe`. For Linux, extract the tarball and run `./BCom`. Some Linux serial devices require membership in the `dialout` group or equivalent permissions.

### macOS installation and Gatekeeper

A **Developer ID signed and Apple-notarized DMG** is needed for a normal, warning-free installation from GitHub Releases. Mount the DMG and drag `BCom.app` to Applications. CI also builds explicitly named `UNNOTARIZED` test ZIPs; these are not official distribution packages and macOS Gatekeeper may reject them as damaged or from an unidentified developer. Renaming or recompressing an unsigned app does not solve notarization. Only use development archives on machines where you explicitly trust the source.

The release workflow publishes macOS DMGs **only after** successful Developer ID signing, Apple notarization, stapling, and verification. Until the maintainer configures the Apple credentials below, tagged releases automatically publish Windows/Linux packages only.

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

GitHub Actions runs a four-target matrix on pull requests, `master` pushes, and manual runs. It checks the published executable and tests archive extraction/checksums; Linux also runs a GUI startup smoke test. Pushing a `vMAJOR.MINOR.PATCH` tag creates a GitHub Release with SHA256SUMS.txt and the Windows/Linux archives. If the five Apple secrets below are configured, signed/notarized macOS DMGs are included.

Configure these GitHub Actions repository secrets to enable official macOS distribution:

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
