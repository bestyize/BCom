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

在 [GitHub Releases](https://github.com/bestyize/BCom/releases) 或 [GitHub Actions 构建页面](https://github.com/bestyize/BCom/actions/workflows/build.yml) 下载。

| 系统 | 打包文件 | 运行方式 |
| --- | --- | --- |
| Windows x64 | `BCom-*-win-x64.zip` | 解压后运行 `BCom.exe` |
| macOS Apple Silicon | `BCom-*-osx-arm64.tar.gz` | 解压并打开 `BCom.app` |
| macOS Intel | `BCom-*-osx-x64.tar.gz` | 解压并打开 `BCom.app` |
| Linux x64 | `BCom-*-linux-x64.tar.gz` | 解压后执行 `./BCom` |

所有构建都是自包含的，无需单独安装 .NET Runtime。**macOS 版本暂未签名或公证**，首次启动可能受到 Gatekeeper 拦截；请仅在信任安装包来源时按照系统提示选择打开。Linux 需要图形桌面环境与 Avalonia 对应的原生依赖，某些发行版还需为串口设备授予用户访问权限（例如加入 `dialout` 用户组）。

### Linux 运行示例

```bash
tar -xzf BCom-*-linux-x64.tar.gz
cd BCom
chmod +x BCom
./BCom
```

Linux 默认使用 X11 图形后端；更多要求见 [Avalonia 支持的平台](https://docs.avaloniaui.net/docs/supported-platforms)。

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

向 `master` 推送代码、发起 PR 或手动运行 GitHub Actions，都会构建上述四个平台的安装包。推送 `v*` 标签时自动将打包文件发布到 GitHub Releases。

```bash
git tag v2.1.0
git push origin v2.1.0
```

## 旧版截图

下方图片是**原 Windows Forms 版本**界面，非新 Avalonia 版本。

![旧版主界面](screenshot/mainform.PNG)

![旧版工具栏](screenshot/mainform_tool.PNG)

## 开源协议

详见 [LICENSE](LICENSE)。
