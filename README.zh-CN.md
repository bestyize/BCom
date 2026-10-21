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

| 平台 | 下载文件 | 运行方式 |
| --- | --- | --- |
| Windows x64 | `BCom-*-win-x64.zip` | 解压后运行 `BCom.exe` |
| macOS Apple Silicon (M系列) | `BCom-*-osx-arm64.dmg` | 双击挂载后拖拽 `BCom.app` 至 Applications |
| macOS Intel (x64) | `BCom-*-osx-x64.dmg` | 双击挂载后拖拽 `BCom.app` 至 Applications |
| Linux x64 | `BCom-*-linux-x64.tar.gz` | 解压后执行 `./BCom` |

构建产物均为自包含包，无需另装 .NET Runtime。

### macOS 安装与 Gatekeeper 提示解决办法

macOS 对网络下载的应用有严格的 Gatekeeper 安全检查。若应用未配置付费 Apple 开发者证书公证，首次打开可能提示 **“无法打开”** 或 **“应用已损坏，你应该将它移到废纸篓”**：
1. 将 `BCom.app` 拖入 **应用程序 (Applications)** 目录。
2. 打开 Mac 自带的 **终端 (Terminal)**。
3. 执行以下命令清除隔离标记：
   ```bash
   xattr -cr /Applications/BCom.app
   ```
4. 重新双击 `BCom.app` 即可正常运行。也可以在系统「设置」->「隐私与安全性」中点击「仍要打开」。

若仓库配置了 Apple 开发者密钥（见下文），流水线将自动进行官方公证与装订，用户无需任何额外操作即可直接打开。

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

向 `master` 推送代码、发起 PR 或手动运行 GitHub Actions 时，都会构建 Windows、macOS Intel、macOS Apple Silicon 和 Linux，检查可执行文件、解包并做完整性校验。

### 发布 Release 的两种方式：

1. **推送版本 Tag（推荐）**：
   ```bash
   git tag v3.0.0
   git push origin v3.0.0
   ```
2. **在 GitHub Actions 页面手动触发**：
   - 进入 Actions -> 选择 **Build and release BCom** 工作流。
   - 点击 **Run workflow**，勾选 `Publish as GitHub Release`，输入标签名称（如 `v3.0.0`）。

流水线将自动校验构建产物、生成 SHA-256 校验和、编写包含安装说明的 Release 说明，并上传全部 4 个平台的完整安装包。若同时配置了 Apple 开发者密钥，macOS DMG 还将自动完成 Apple 官方公证。

要启用 Apple 官方公证（可选），在 GitHub Actions Secrets 配置：

- `MACOS_CERTIFICATE_BASE64`：含私钥的 Developer ID Application .p12 证书，转换为 Base64。
- `MACOS_CERTIFICATE_PASSWORD`：上述 .p12 文件密码。
- `APPLE_ID`：有公证权限的 Apple ID。
- `APPLE_TEAM_ID`：Apple Developer Team ID。
- `APPLE_APP_SPECIFIC_PASSWORD`：Apple ID 专用密码，供 `notarytool` 使用。

Apple Developer ID 及公证要求付费开发者账号。请勿把证书或密码提交到仓库。只配置了一部分密钥时，正式 macOS 打包会报错，避免发布错误产物。

```bash
git tag v3.0.0
git push origin v3.0.0
```

参考 [Apple 开发者分发指南](https://developer.apple.com/macos/distribution) 与 [Avalonia macOS 打包说明](https://docs.avaloniaui.net/docs/deployment/macos)。

## 旧版截图

下方图片是**原 Windows Forms 版本**界面，非新 Avalonia 版本。

![旧版主界面](screenshot/mainform.PNG)

![旧版工具栏](screenshot/mainform_tool.PNG)

## 开源协议

详见 [LICENSE](LICENSE)。
