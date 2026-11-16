#!/usr/bin/env bash
# Packs dist/NO-RevokerECR-<version>-NOMM.zip for players who get the mod
# before it is in the NOMNOM registry: the NO-RevokerECR folder (DLL plus
# meta.json) to drag into the NOMM window, and a short German LIESMICH.txt.
# Deliberately no .nommpack: importing one disables every mod not in the pack.
# Needs build/NORevokerECR.dll first: scripts/build.sh
set -euo pipefail
cd "$(dirname "$0")/.."
[ -f build/NORevokerECR.dll ] || { echo "build/NORevokerECR.dll fehlt – erst scripts/build.sh" >&2; exit 1; }
version=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' src/NORevokerECR.csproj)
game_version=$(cat lib/refs/GAME_VERSION 2>/dev/null || echo "")

stage=$(mktemp -d)
trap 'rm -rf "$stage"' EXIT
mkdir -p "$stage/NO-RevokerECR"
cp build/NORevokerECR.dll "$stage/NO-RevokerECR/"
scripts/meta-json.sh "$version" "$game_version" > "$stage/NO-RevokerECR/meta.json"
# CRLF, so Notepad on Windows shows the lines.
sed 's/$/\r/' > "$stage/LIESMICH.txt" <<TXT
NO-RevokerECR $version (Testversion für Nuclear Option $game_version)

Fügt das EA-12 Revoker ECR hinzu, ein eigenes Flugzeug auf Basis des FS-12
Revoker. Es steht in jedem Hangar, der das FS-12 anbietet, und lässt sich
für 105m kaufen (Rang 4). Das normale FS-12 bleibt unverändert.

Multiplayer: Alle Spieler einer Sitzung brauchen denselben Mod-Stand
(dieselbe Version von NO-RevokerECR und dieselben anderen Flugzeug- und
Waffen-Mods). Der Mod hängt dafür seine Kennung an die Spielversion. Die
Lobby-Liste zeigt dir deshalb nur Lobbys mit genau denselben Mods, und
der Beitritt in eine andere endet mit "Incorrect version". Für öffentliche
Lobbys ohne den Mod: NO-RevokerECR in NOMM ausschalten.
Dedicated Server werden noch nicht unterstützt.

Installation mit NOMM:
  1. Zip entpacken.
  2. Den Ordner "NO-RevokerECR" ins NOMM-Fenster ziehen. NOMM legt ihn
     nach BepInEx/plugins/ und zeigt ihn als lokalen Mod an.
  BepInEx muss in NOMM eingeschaltet sein.

Ohne NOMM: den Ordner "NO-RevokerECR" nach "Nuclear Option/BepInEx/plugins/"
kopieren.

Wenn etwas nicht stimmt: im Spiel mit F1 bei NO-RevokerECR "DebugDump"
einschalten (oder in "BepInEx/config/xyz.geraldhofbauer.norevokerecr.cfg"
DebugDump = true setzen), das Spiel neu starten und danach
"BepInEx/LogOutput.log" an Gerry schicken.
TXT

out="$PWD/dist/NO-RevokerECR-$version-NOMM.zip"
mkdir -p dist && rm -f "$out"
(cd "$stage" && zip -qr "$out" NO-RevokerECR LIESMICH.txt)
echo "dist/NO-RevokerECR-$version-NOMM.zip"
