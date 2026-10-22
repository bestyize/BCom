#!/usr/bin/env bash
set -euo pipefail

: "${BCOM_TAG:?missing release tag}"

tag="$BCOM_TAG"
checksums=""
if [[ -f dist/SHA256SUMS.txt ]]; then
  checksums="$(cat dist/SHA256SUMS.txt)"
fi

mkdir -p dist
release_highlights=""
if [[ -f ".github/release-notes/${tag}.md" ]]; then
  release_highlights="$(cat ".github/release-notes/${tag}.md")"
fi
mac_rows=""
if [[ "${MACOS_NOTARIZED:-false}" == "true" ]]; then
  mac_rows="| **macOS (Apple Silicon)** | \`BCom-${tag}-osx-arm64.dmg\` | Notarized DMG |"
  mac_rows="$mac_rows
| **macOS (Intel)** | \`BCom-${tag}-osx-x64.dmg\` | Notarized DMG |"
  mac_note="Apple Developer ID signed and notarized macOS installers are included in this release."
else
  mac_note="macOS installers are not included in this release because Apple notarization was unavailable. Ad-hoc signed builds are retained as CI test artifacts only."
fi

cat > dist/RELEASE_NOTES.md <<EOF
## BCom ${tag}

${release_highlights}

Cross-platform serial communication and debugging assistant built with **.NET 10 LTS and Avalonia UI 12**, supporting **Windows, macOS, and Linux**.

### 📦 Downloads

| Platform | Package | Notes |
| :--- | :--- | :--- |
| **Windows (x64)** | \`BCom-${tag}-win-x64.zip\` | Extract and run \`BCom.exe\` |
${mac_rows}
| **Linux (x64)** | \`BCom-${tag}-linux-x64.tar.gz\` | Extract and run \`./BCom\` |

All packages are self-contained and do not require a pre-installed .NET runtime.

---

### 🍎 macOS Availability

${mac_note}

---

### SHA-256 Checksums

\`\`\`
${checksums}
\`\`\`
EOF
