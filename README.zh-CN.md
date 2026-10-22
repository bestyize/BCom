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
- **无头命令行（CLI）与 AI Agent 控制**：支持完整命令行控制、等待响应超时捕获与 JSON 机器可读格式，便于 Claude / AI Agent 自动化探测与交互。

## 命令行（CLI）与 AI Agent 控制技能

BCom 具有双重运行模式：无参数启动时运行现代 macOS 风格桌面图形界面；传入子命令时自动切换为无头命令行模式，供脚本或 AI Agent 调度：

```bash
# JSON 格式枚举可用串口
bcom ports --json

# 发送 AT 指令并等待 1000 毫秒捕获回复
bcom send /dev/tty.usbserial-10 "AT" --crlf --wait 1000 --json

# 发送 16 进制原始数据包（Modbus/传感器）
bcom send COM3 "01 03 00 00 00 02 C4 0B" --hex --wait 500

# 监听单片机日志输出 5000 毫秒
bcom monitor /dev/ttyACM0 --baud 115200 --duration 5000 --json

# 流式输出带时间戳的 JSON Lines，支持持续运行
bcom monitor /dev/ttyACM0 --baud 115200 --duration 0 --jsonl > capture.ndjson
```

图形界面的接收历史只保留最近 **128 KiB** 原始数据，以限制内存占用；界面“导出”仅包含当前显示窗口，而非完整历史记录。需要长期完整记录时，请使用 `monitor --jsonl` 命令并将输出重定向到文件。图形界面另外支持自定义波特率、自动跟随开关，并在成功连接后保存串口配置。

项目内置针对 Claude Code 的专属控制技能：[`.claude/skills/bcom-serial/SKILL.md`](.claude/skills/bcom-serial/SKILL.md)，AI Agent 能够直接调用该技能完成微控制器探测、固件问答以及设备调试。

## 下载安装

**当前构建产物**可从 [GitHub Actions](https://github.com/bestyize/BCom/actions/workflows/build.yml) 下载。正式 Release 中的旧版 2.0 EXE 是 2019 年的 WinForms 版本，并非 .NET 10 程序。

| 平台 | 下载文件 | 运行方式 |
| --- | --- | --- |
| Windows x64 | `BCom-*-win-x64.zip` | 解压后运行 `BCom.exe` |
| macOS Apple Silicon (M系列) | `BCom-*-osx-arm64.dmg` | 双击挂载后拖拽 `BCom.app` 至 Applications |
| macOS Intel (x64) | `BCom-*-osx-x64.dmg` | 双击挂载后拖拽 `BCom.app` 至 Applications |
| Linux x64 | `BCom-*-linux-x64.tar.gz` | 解压后执行 `./BCom` |

构建产物均为自包含包，无需另装 .NET Runtime。**新的正式发行版仅在 Apple Developer ID 签名与公证成功时包含 macOS DMG。** 未公证的 macOS 测试包仍可从 Actions 构建产物中获取，但不列入正式 Release 下载项。

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

### 多串口独立会话（开发中）

接收面板上方新增**连接会话**选择器。点击「新建连接」后在串口设置中选择
新的物理串口并打开，即可同时连接多个设备。切换会话不会关闭其他串口；
关闭操作仅作用于当前设置中选定的物理串口。每个会话拥有独立的接收缓存、
时间线、RX/TX 计数、发送草稿、发送模式、定时发送和日志搜索状态。

导出仅针对当前选中的会话。顶部的会话选择器用于切换查看对象，右侧串口
下拉框用于选择要打开或关闭的物理设备。本阶段暂不持久化多会话列表，
也不提供跨端口日志合并视图。

## 串口事件时间线与日志搜索（开发中）

接收区域新增可切换的**事件时间线**，以本地毫秒级时间戳、RX/TX
收发方向、串口名称和十六进制字节展示读写事件。时间戳来自串口 API
读写事件，**不是协议帧边界**。时间线独立限制为 128 KiB；
搜索栏仅查找当前显示内容（忽略大小写，并支持逐条跳转），不包括已淘汰的历史。
完整长期记录仍建议使用 `monitor --jsonl` 重定向到文件。

## 嵌入式诊断工作台（阶段 B–D）

**B · Quick Plot 实时曲线：**在已连接的串口会话中勾选「实时曲线」，
对以换行结尾的数值 CSV（例如 `12.4,0.5,-2\n`）进行实时绘制。
每个会话最多 8 个数值通道、保留最近 512 组采样；无效数值会被过滤。
曲线只供快速观察，完整原始记录仍应使用录制功能。

**C · 协议切帧和设备测试：**内置换行、定长、首字节长度前缀三种
跨接收回调的切帧算法，以及 Modbus RTU CRC16 校验。CLI 可以使用
「发送内容 => 预期响应」脚本逐步执行测试：

```text
AT\\r\\n => OK
HEX:01 03 00 00 00 0A C5 CD => HEX:01 03
```

```bash
BCom verify COM3 commands.txt --baud 115200 --wait 1500
BCom decode session.bcomcap lf
BCom decode session.bcomcap fixed:8 --crc
BCom decode session.bcomcap length
BCom template at "AT+GMR"
BCom template modbus-read 1 0 10
BCom template nmea-check '$ABC*40'
```

测试失败即停止；每步最多 4 KiB、全程最多 256 步，单步等待最多 30 秒，
不允许执行任意脚本代码。

**D · 本地 MCP 工具：**`BCom mcp` 提供 stdio MCP 服务，默认只有
`serial_list_ports`（列出可用串口）。写入必须同时显式开启权限，
并限定端口，例如 `BCom mcp --allow-write --port COM3`。
单次请求最多发送 4096 字节，写入命令固定使用 115200 波特率并限制超时，
每次单独打开设备，不复用已打开的 GUI 会话。只应让可信任的 MCP 客户端
访问该进程；操作审计信息写入标准错误输出（只包含时间、工具名、端口和结果，不记录数据内容）。当前没有细粒度的用户授权弹窗，也不支持任意 shell 命令。

### 原始数据录制与回放（阶段 A）

在已连接的会话中点击「开始录制」可保存 `.bcomcap` 文件，包含 RX / TX
方向、UTC 时间戳和完整字节。后台录制独立于 UI 日志，每 64 MiB 自动轮转
到 `.part001` 等文件。录制队列容量有限；磁盘来不及写入时会记录丢弃
字节数，不会无限增长内存。停止录制时会刷盘。

无需连接设备即可通过 CLI 回放单个文件：

```bash
BCom replay session.bcomcap
BCom replay session.bcomcap.part001
```

回放结果是 JSONL。注意时间戳记录的是串口 API 回调时间，而非每个字节
或协议帧的精确时刻；多分卷应按序读取。界面缓冲区有独立的容量限制。

## 虚拟串口集成测试

Linux CI 使用 Python 标准库创建伪终端（PTY），让**实际发布的 BCom CLI**
通过 `System.IO.Ports` 与模拟串口设备交换原始字节。测试覆盖发送/应答、
带 NUL 的 JSON 结果、NDJSON 持续事件输出和错误参数校验。

```bash
dotnet publish BCom.csproj -c Release -r linux-x64 --self-contained true -o publish
python3 Tests/serial_pty_integration.py --binary ./publish/BCom
```

PTY 不能替代真实 USB-UART 硬件测试：USB 驱动兼容性、RTS/CTS 硬件流控、
设备热拔插和持续高波特率压力测试仍需使用真实设备验证。

## 语言切换

应用默认跟随系统语言：中文系统显示简体中文，其他语言显示英文。也可通过界面右上方的 **Language / 语言** 选择器切换为英文或简体中文，并自动保存设置。翻译统一维护在 `CrossPlatform/I18n.cs`。

## 自动打包与发布

向 `master` 推送代码、发起 PR 或手动运行 GitHub Actions 时，都会构建 Windows、macOS Intel、macOS Apple Silicon 和 Linux，检查可执行文件、解包并做完整性校验。

### 发布 Release 的两种方式：

v3.3.0 新增有界接收历史、后台串口发送、自定义波特率、
连接配置持久化、NDJSON 流式监控与 Linux PTY 集成测试。
通过审核后合并到 `master` 时，若合并提交包含 `[publish-release]`，
CI 也会将构建通过的提交直接发布成带版本标签的 GitHub Release。
macOS 正式安装包仍以 Apple 签名及公证成功为前提。


1. **推送版本 Tag（推荐）**：
   ```bash
   git tag v3.3.0
   git push origin v3.3.0
   ```
2. **在 GitHub Actions 页面手动触发**：
   - 进入 Actions -> 选择 **Build and release BCom** 工作流。
   - 点击 **Run workflow**，勾选 `Publish as GitHub Release`，输入标签名称（如 `v3.3.0`）。

流水线自动校验构建产物并生成 SHA-256 校验和。Windows/Linux 安装包正常发布；macOS DMG 只有完成 Developer ID 签名及 Apple 官方公证后才进入 Release，未公证的包仅作为 CI 测试产物。

要启用 Apple 官方公证（可选），在 GitHub Actions Secrets 配置：

- `MACOS_CERTIFICATE_BASE64`：含私钥的 Developer ID Application .p12 证书，转换为 Base64。
- `MACOS_CERTIFICATE_PASSWORD`：上述 .p12 文件密码。
- `APPLE_ID`：有公证权限的 Apple ID。
- `APPLE_TEAM_ID`：Apple Developer Team ID。
- `APPLE_APP_SPECIFIC_PASSWORD`：Apple ID 专用密码，供 `notarytool` 使用。

Apple Developer ID 及公证要求付费开发者账号。请勿把证书或密码提交到仓库。只配置了一部分密钥时，正式 macOS 打包会报错，避免发布错误产物。

```bash
git tag v3.3.0
git push origin v3.3.0
```

参考 [Apple 开发者分发指南](https://developer.apple.com/macos/distribution) 与 [Avalonia macOS 打包说明](https://docs.avaloniaui.net/docs/deployment/macos)。

## 旧版截图

下方图片是**原 Windows Forms 版本**界面，非新 Avalonia 版本。

![旧版主界面](screenshot/mainform.PNG)

![旧版工具栏](screenshot/mainform_tool.PNG)

## 开源协议

详见 [LICENSE](LICENSE)。
