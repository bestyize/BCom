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

Cross-platform serial communication and debugging assistant built with **.NET 10 LTS and Avalonia UI 12**, supporting **Windows, macOS, and Linux**.

### 📦 Downloads

| Platform | Package | Notes |
| :--- | :--- | :--- |
| **Windows (x64)** | \`BCom-${tag}-win-x64.zip\` | Extract and run \`BCom.exe\` |
| **macOS (Apple Silicon M-Series)** | \`BCom-${tag}-osx-arm64.dmg\` | Open DMG and drag \`BCom.app\` to Applications |
| **macOS (Intel x86_64)** | \`BCom-${tag}-osx-x64.dmg\` | Open DMG and drag \`BCom.app\` to Applications |
| **Linux (x64)** | \`BCom-${tag}-linux-x64.tar.gz\` | Extract and run \`./BCom\` |

All packages are self-contained and do not require a pre-installed .NET runtime.

---

### 🍎 macOS First-Launch Notice (Gatekeeper)

If macOS displays a warning that the application is damaged or from an unidentified developer:
1. Open the built-in **Terminal** app.
2. Run the following command to remove the quarantine flag:
   \`\`\`bash
   xattr -cr /Applications/BCom.app
   \`\`\`
3. Double-click \`BCom.app\` in Applications to launch normally. Alternatively, click **Open Anyway** in **System Settings -> Privacy & Security**.

---

### SHA-256 Checksums

\`\`\`
${checksums}
\`\`\`
EOF
