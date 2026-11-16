#!/usr/bin/env bash
# Makes lib/refs/ (reference-only assemblies: public API, no game code) from
# YOUR installed game. Needed before the first build and after game updates.
# lib/refs/ is not in the repo: even stripped, it is the game's assembly.
set -euo pipefail
cd "$(dirname "$0")/.."
GAME="${GAME:-/mnt/games/SteamLibrary/steamapps/common/Nuclear Option}"
MANAGED="$GAME/NuclearOption_Data/Managed"
DLLS="Assembly-CSharp.dll Mirage.dll UniTask.dll Unity.TextMeshPro.dll Unity.Addressables.dll Unity.ResourceManager.dll"

mkdir -p lib/refs
# Refasmer writes next to its input, so it works on a copy inside the container.
docker run --rm -v "$MANAGED":/managed:ro -v "$PWD/lib/refs":/out mcr.microsoft.com/dotnet/sdk:8.0 bash -c "
  dotnet tool install -g JetBrains.Refasmer.CliTool >/dev/null
  mkdir /w && cp /managed/*.dll /w/ && cd /w && mkdir o
  ~/.dotnet/tools/refasmer --omit-non-api-members=true -O o $DLLS
  cp o/*.dll /out/ && chown -R $(id -u):$(id -g) /out"
cat "$GAME/build-hash.txt" > lib/refs/GAME_BUILD
# The version string only appears in the game's own log (Proton prefix on Linux).
PLAYER_LOG="${PLAYER_LOG:-$GAME/../../compatdata/2168680/pfx/drive_c/users/steamuser/AppData/LocalLow/Shockfront/NuclearOption/Player.log}"
version="${GAME_VERSION:-$(grep -m1 -oP 'Nuclear Option \K[0-9.]+(?= Starting)' "$PLAYER_LOG" 2>/dev/null || true)}"
[ -n "$version" ] && echo "$version" > lib/refs/GAME_VERSION
echo "lib/refs aktualisiert (Build $(cat lib/refs/GAME_BUILD), Spiel ${version:-Version unbekannt})"
