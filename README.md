# BCom
开源的多功能串口调试助手
当前版本已升级为 .NET 10 LTS，提供 Windows x64 自包含便携包。旧版 Visual Studio 的“首选 32 位”选项不再适用。


主界面

![主界面](screenshot/mainform.PNG)

工具栏

![工具栏](screenshot/mainform_tool.PNG)

## 构建要求

BCom 已迁移到 **.NET 10 LTS**，使用 Windows Forms，因此仍仅支持 Windows。

- 本地开发：安装 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)，推荐 Visual Studio 2026，选择“使用 .NET 的桌面开发”工作负载。
- Windows x64 用户：可直接下载自包含 ZIP，无需单独安装 .NET Runtime。

本地构建：

```powershell
dotnet restore .\BCom.csproj
dotnet build .\BCom.csproj -c Release
dotnet publish .\BCom.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false
```

## Windows 自动打包

GitHub Actions 在 Windows 上使用 .NET 10 SDK 构建并发布 **win-x64 自包含便携版**（不是安装器）。解压 ZIP 后运行 `BCom.exe`，不需要预装 .NET。

- 向 `master` 推送提交、打开目标为 `master` 的 Pull Request，或在 **Actions → Build and package BCom → Run workflow** 手动触发。
- 成功后，在对应 Actions 运行记录底部的 **Artifacts** 下载 `bcom-win-x64`，其中包含 `BCom-*-win-x64.zip`。
- 推送以 `v` 开头的标签（如 `v2.0.1`）时自动创建或更新 GitHub Release，并上传 ZIP。

```bash
git tag v2.0.1
git push origin v2.0.1
```

> 此升级不意味着跨平台支持；Windows Forms 仍需要 Windows。请使用兼容的 64 位 Windows 系统。
