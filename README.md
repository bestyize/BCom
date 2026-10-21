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

Download packages from [GitHub Releases](https://github.com/bestyize/BCom/releases) or the latest successful [GitHub Actions build](https://github.com/bestyize/BCom/actions/workflows/build.yml).

| Platform | Package | Notes |
| --- | --- | --- |
| Windows x64 | `BCom-*-win-x64.zip` | Extract and run `BCom.exe` |
| macOS Apple Silicon | `BCom-*-osx-arm64.tar.gz` | Extract and open `BCom.app` |
| macOS Intel | `BCom-*-osx-x64.tar.gz` | Extract and open `BCom.app` |
| Linux x64 | `BCom-*-linux-x64.tar.gz` | Extract and run `./BCom` |

Builds are self-contained and do not require a separate .NET runtime. macOS builds are currently **unsigned and not notarized**; Gatekeeper may block first launch. On Linux, Avalonia requires a supported graphical desktop and appropriate native libraries. USB serial devices may require operating-system permissions (for example, membership in `dialout` on some Linux distributions). Available tools depend on your desktop environment.

### Launching on macOS

After extracting, open `BCom.app` from Finder. If macOS blocks the unsigned app, use Finder's **Open** context-menu action and follow macOS instructions for opening software from an unidentified developer. Do not bypass security checks for a package you do not trust.

### Launching on Linux

```bash
tar -xzf BCom-*-linux-x64.tar.gz
cd BCom
chmod +x BCom
./BCom
```

The default Linux backend needs X11 and related native libraries; see [Avalonia supported platforms](https://docs.avaloniaui.net/docs/supported-platforms).

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

GitHub Actions builds all four platform packages on pushes to `master`, pull requests, and manual runs. Tagged releases (`v*`, for example `v2.1.0`) upload all successfully built packages to GitHub Releases.

```bash
git tag v2.1.0
git push origin v2.1.0
```

## Legacy screenshots

The following screenshots show the **original Windows Forms edition**, not the new Avalonia interface.

![Legacy main window](screenshot/mainform.PNG)

![Legacy toolbar](screenshot/mainform_tool.PNG)

## License

See [LICENSE](LICENSE).
