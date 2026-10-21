#!/usr/bin/env bash
set -euo pipefail

: "${BCOM_TAG:?missing release tag}"

tag="$BCOM_TAG"
checksums=""
if [[ -f dist/SHA256SUMS.txt ]]; then
  checksums="$(cat dist/SHA256SUMS.txt)"
fi

mkdir -p dist
cat > dist/RELEASE_NOTES.md <<EOF
## BCom ${tag}

跨平台串口调试助手（.NET 10 LTS + Avalonia UI 12），支持 Windows、macOS 和 Linux。

### 📦 安装包下载 / Downloads

| 平台 / Platform | 下载文件 / Package | 说明 / Notes |
| :--- | :--- | :--- |
| **Windows (x64)** | \`BCom-${tag}-win-x64.zip\` | 解压后直接运行 \`BCom.exe\` |
| **macOS (Apple Silicon M系列)** | \`BCom-${tag}-osx-arm64.dmg\` | 双击打开 DMG，将 \`BCom.app\` 拖入 Applications |
| **macOS (Intel x64)** | \`BCom-${tag}-osx-x64.dmg\` | 双击打开 DMG，将 \`BCom.app\` 拖入 Applications |
| **Linux (x64)** | \`BCom-${tag}-linux-x64.tar.gz\` | 解压后终端运行 \`./BCom\` |

---

### 🍎 macOS 首次运行提示“无法打开”或“已损坏”解决办法
如果 macOS 提示应用已损坏或来自未识别的开发者（Gatekeeper 安全防护机制）：
1. 打开 Mac 系统自带的 **终端 (Terminal)**；
2. 复制并执行以下命令（清除下载隔离属性）：
   \`\`\`bash
   xattr -cr /Applications/BCom.app
   \`\`\`
3. 再次在“应用程序”中双击 \`BCom.app\` 即可正常启动。

---

### 校验和 / SHA-256 Checksums
\`\`\`
${checksums}
\`\`\`
EOF
