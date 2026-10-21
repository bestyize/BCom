# BCom

[English](README.md)

**BCom** 是免费、开源的跨平台串口调试助手，使用 **.NET 10 LTS 与 Avalonia UI 12** 开发，支持 **Windows、macOS、Linux** 桌面系统。

## 主要功能

- 枚举和选择串口，配置波特率、数据位、停止位、奇偶校验位。
- 文本与十六进制数据收发。
- 追加 CRLF、设置毫秒间隔定时发送、切换 RTS/DTR。
- 导入文本发送内容、导出接收内容。
- 中英文界面：默认跟随操作系统语言，也可以在应用内手动切换并保存。
- 按操作系统启动计算器、编辑器、终端、截图工具等（需系统安装对应工具）。

## 下载安装

**当前构建产物**可从 [GitHub Actions](https://github.com/bestyize/BCom/actions/workflows/build.yml) 下载。正式 Release 中的旧版 2.0 EXE 是 2019 年的 WinForms 版本，并非 .NET 10 程序。

| 平台 | CI 测试包 | 正式发布包 |
| --- | --- | --- |
| Windows x64 | `BCom-build-*-win-x64.zip` | `BCom-v*-win-x64.zip` |
| macOS Apple Silicon | `BCom-build-*-osx-arm64-UNNOTARIZED.zip` | `BCom-v*-osx-arm64.dmg`（需要 Apple 证书） |
| macOS Intel | `BCom-build-*-osx-x64-UNNOTARIZED.zip` | `BCom-v*-osx-x64.dmg`（需要 Apple 证书） |
| Linux x64 | `BCom-build-*-linux-x64.tar.gz` | `BCom-v*-linux-x64.tar.gz` |

构建产物均为自包含包，无需另装 .NET Runtime。Windows 解压 ZIP 后运行 `BCom.exe`；Linux 解压后执行 `./BCom`，串口设备可能需要 `dialout` 等权限。

### macOS 安装包损坏提示的原因

macOS 经互联网下载的应用需要有效的 **Developer ID 签名及 Apple 公证**才能在默认 Gatekeeper 设置下正常安装。此前打包脚本仅将 `.app` 压缩为 tar.gz，没有签名、公证，也未做安装验证，因此用户可能遇到“已损坏，无法打开”或“无法验证开发者”等提示。

新工作流会在提供 Apple 证书后自动签名、公证、staple 和验证 DMG，再将其发布到 Releases。没有证书时，只生成清楚标记为 `UNNOTARIZED` 的 CI **开发测试包**，不将其误当成官方 Mac 安装包发布。改扩展名或重新压缩无法解决公证问题。

### Linux 运行示例

```bash
tar -xzf BCom-*-linux-x64.tar.gz
cd BCom
chmod +x BCom
./BCom
```

详见 [Avalonia 支持的平台](https://docs.avaloniaui.net/docs/supported-platforms)。

## 本地开发

安装 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)：

```bash
dotnet restore BCom.csproj
dotnet run --project BCom.csproj
dotnet publish BCom.csproj -c Release -r <runtime-id> --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false
```

将 `<runtime-id>` 替换为 `win-x64`、`osx-arm64`、`osx-x64` 或 `linux-x64`。新的实际应用代码在 `CrossPlatform/` 下；旧 WinForms 源码及窗体资源暂时保留作迁移参考，但已排除在编译范围之外。

## 语言切换

应用默认跟随系统语言：中文系统显示简体中文，其他语言显示英文。也可通过界面右上方的 **Language / 语言** 选择器切换为英文或简体中文，并自动保存设置。翻译统一维护在 `CrossPlatform/I18n.cs`。

## 自动打包与发布

向 `master` 推送代码、发起 PR 或手动运行 GitHub Actions 时，都会构建 Windows、macOS Intel、macOS Apple Silicon 和 Linux，检查可执行文件、解包及校验；Linux 还执行 GUI 启动测试。推送 `vMAJOR.MINOR.PATCH` 标签后发布 Windows/Linux 包及 `SHA256SUMS.txt`；Apple 证书配置完整时另外发布双架构的签名公证 DMG。

要启用 macOS 正式安装包，在 GitHub Actions Secrets 配置：

- `MACOS_CERTIFICATE_BASE64`：含私钥的 Developer ID Application .p12 证书，转换为 Base64。
- `MACOS_CERTIFICATE_PASSWORD`：上述 .p12 文件密码。
- `APPLE_ID`：有公证权限的 Apple ID。
- `APPLE_TEAM_ID`：Apple Developer Team ID。
- `APPLE_APP_SPECIFIC_PASSWORD`：Apple ID 专用密码，供 `notarytool` 使用。

Apple Developer ID 及公证要求付费开发者账号。请勿把证书或密码提交到仓库。只配置了一部分密钥时，正式 macOS 打包会报错，避免发布错误产物。

```bash
git tag v2.1.0
git push origin v2.1.0
```

参考 [Apple 开发者分发指南](https://developer.apple.com/macos/distribution) 与 [Avalonia macOS 打包说明](https://docs.avaloniaui.net/docs/deployment/macos)。

## 旧版截图

下方图片是**原 Windows Forms 版本**界面，非新 Avalonia 版本。

![旧版主界面](screenshot/mainform.PNG)

![旧版工具栏](screenshot/mainform_tool.PNG)

## 开源协议

详见 [LICENSE](LICENSE)。
