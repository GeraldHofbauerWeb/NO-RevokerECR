#!/usr/bin/env bash
# Installs the freshly built zip the way NOMM would: BepInEx/plugins/NO-RevokerECR/
# with a meta.json, so NOMM lists it and can switch it on/off.
set -euo pipefail
cd "$(dirname "$0")/.."
GAME="${GAME:-/mnt/games/SteamLibrary/steamapps/common/Nuclear Option}"
version=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' src/NORevokerECR.csproj)
zip="dist/NO-RevokerECR-$version.zip"
[ -f "$zip" ] || { echo "$zip fehlt – erst scripts/build.sh" >&2; exit 1; }
game_version=$(cat lib/refs/GAME_VERSION 2>/dev/null || echo "")

# Wherever NOMM currently keeps it (enabled or disabled), replace it there.
target="$GAME/BepInEx/plugins/NO-RevokerECR"
[ -d "$GAME/BepInEx/disabledPlugins/NO-RevokerECR" ] && target="$GAME/BepInEx/disabledPlugins/NO-RevokerECR"
rm -rf "$target" && mkdir -p "$target"
unzip -q "$zip" -d "$target"
scripts/meta-json.sh "$version" "$game_version" > "$target/meta.json"
echo "installiert: $target"
