#!/usr/bin/env bash
# Builds a verifiable macOS application archive. Distribution DMGs are issued
# only when a Developer ID certificate AND Apple notarization credentials exist.
set -euo pipefail

: "${BCOM_RID:?missing platform RID}"
: "${BCOM_VERSION:?missing version}"
: "${BCOM_LABEL:?missing package label}"
: "${RUNNER_TEMP:?must run on a macOS Actions runner}"

app="package/BCom.app"
mkdir -p "$app/Contents/MacOS" dist
cp -R publish/. "$app/Contents/MacOS/"
chmod +x "$app/Contents/MacOS/BCom"
short_version="${BCOM_VERSION%%-*}"

cat > "$app/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleExecutable</key><string>BCom</string>
  <key>CFBundleIdentifier</key><string>io.github.bestyize.bcom</string>
  <key>CFBundleName</key><string>BCom</string>
  <key>CFBundleDisplayName</key><string>BCom</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>$short_version</string>
  <key>CFBundleVersion</key><string>$GITHUB_RUN_NUMBER</string>
  <key>NSHighResolutionCapable</key><true/>
</dict>
</plist>
PLIST

plutil -lint "$app/Contents/Info.plist"
test -x "$app/Contents/MacOS/BCom"
file "$app/Contents/MacOS/BCom"
if [[ "$BCOM_RID" == "osx-arm64" ]]; then
  lipo -archs "$app/Contents/MacOS/BCom" | grep -qw arm64
else
  lipo -archs "$app/Contents/MacOS/BCom" | grep -qw x86_64
fi

# Native libraries, host binaries and the app bundle must be signed inside-out.
sign_macho() {
  local identity="$1"
  local file_path
  while IFS= read -r -d '' file_path; do
    if file -b "$file_path" | grep -q 'Mach-O'; then
      if [[ "$identity" == "-" ]]; then
        codesign --force --sign - "$file_path"
      else
        codesign --force --timestamp --options runtime \
          --entitlements .github/macos/BCom.entitlements \
          --sign "$identity" "$file_path"
      fi
    fi
  done < <(find "$app/Contents/MacOS" -type f -print0)
  if [[ "$identity" == "-" ]]; then
    codesign --force --sign - "$app"
  else
    codesign --force --timestamp --options runtime \
      --entitlements .github/macos/BCom.entitlements \
      --sign "$identity" "$app"
  fi
  codesign --verify --deep --strict --verbose=2 "$app"
}

# No credentials are available to untrusted pull requests. Do not advertise an
# ad-hoc signature as Developer ID signing or a notarized downloadable release.
is_tag=false
if [[ "$GITHUB_REF_TYPE" == "tag" ]]; then is_tag=true; fi
configured=0
for value in "$MACOS_CERTIFICATE_BASE64" "$MACOS_CERTIFICATE_PASSWORD" \
             "$APPLE_ID" "$APPLE_TEAM_ID" "$APPLE_APP_SPECIFIC_PASSWORD"; do
  if [[ -n "$value" ]]; then configured=$((configured + 1)); fi
done

if [[ "$is_tag" == true && "$configured" -ne 0 && "$configured" -ne 5 ]]; then
  echo "::error::Partial macOS signing configuration: set all five required Actions secrets."
  exit 1
fi

if [[ "$is_tag" == true && "$configured" -eq 5 ]]; then
  keychain="$RUNNER_TEMP/bcom-signing.keychain-db"
  password="$(openssl rand -hex 20)"
  security create-keychain -p "$password" "$keychain"
  security set-keychain-settings -lut 14400 "$keychain"
  security unlock-keychain -p "$password" "$keychain"
  security list-keychains -d user -s "$keychain"
  printf '%s' "$MACOS_CERTIFICATE_BASE64" | base64 -D > "$RUNNER_TEMP/bcom.p12"
  security import "$RUNNER_TEMP/bcom.p12" -k "$keychain" \
    -P "$MACOS_CERTIFICATE_PASSWORD" -T /usr/bin/codesign
  security set-key-partition-list -S apple-tool:,apple:,codesign: \
    -s -k "$password" "$keychain"
  identity="$(security find-identity -v -p codesigning "$keychain" | awk '/Developer ID Application:/ { print $2; exit }')"
  if [[ -z "$identity" ]]; then
    echo "::error::No Developer ID Application identity in the supplied certificate."
    exit 1
  fi
  sign_macho "$identity"

  # Notarize and staple the app, then notarize the DMG containing that app.
  notary_zip="$RUNNER_TEMP/BCom-notary-$BCOM_RID.zip"
  ditto -c -k --sequesterRsrc --keepParent "$app" "$notary_zip"
  xcrun notarytool submit "$notary_zip" \
    --apple-id "$APPLE_ID" --team-id "$APPLE_TEAM_ID" \
    --password "$APPLE_APP_SPECIFIC_PASSWORD" --wait
  xcrun stapler staple "$app"
  xcrun stapler validate "$app"
  codesign --verify --deep --strict --verbose=2 "$app"

  stage="$RUNNER_TEMP/BCom-dmg-$BCOM_RID"
  mkdir -p "$stage"
  ditto "$app" "$stage/BCom.app"
  ln -s /Applications "$stage/Applications"
  dmg="dist/BCom-$BCOM_LABEL-$BCOM_RID.dmg"
  hdiutil create -volname "BCom" -srcfolder "$stage" \
    -format UDZO -ov "$dmg"
  codesign --force --timestamp --sign "$identity" "$dmg"
  xcrun notarytool submit "$dmg" \
    --apple-id "$APPLE_ID" --team-id "$APPLE_TEAM_ID" \
    --password "$APPLE_APP_SPECIFIC_PASSWORD" --wait
  xcrun stapler staple "$dmg"
  xcrun stapler validate "$dmg"
  hdiutil verify "$dmg"
  echo "Signed and notarized DMG: $dmg"
else
  sign_macho "-"
  stage="$RUNNER_TEMP/BCom-dmg-$BCOM_RID"
  mkdir -p "$stage"
  ditto "$app" "$stage/BCom.app"
  ln -s /Applications "$stage/Applications"
  dmg="dist/BCom-$BCOM_LABEL-$BCOM_RID.dmg"
  hdiutil create -volname "BCom" -srcfolder "$stage" \
    -format UDZO -ov "$dmg"
  hdiutil verify "$dmg"
  echo "Ad-hoc signed DMG created: $dmg"
  echo "::notice::macOS package created with ad-hoc signature. Users can open via Gatekeeper override (xattr -cr /Applications/BCom.app)."
fi
