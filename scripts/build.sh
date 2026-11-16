#!/usr/bin/env bash
# Builds the plugin in a .NET SDK container (no local SDK needed), runs the
# Core tests and packs dist/NO-RevokerECR-<version>.zip: just the DLL at the
# archive root, the layout NOMM expects (it unpacks into
# BepInEx/plugins/<id>/ and adds meta.json).
# Needs lib/refs/ first: scripts/make-refs.sh
set -euo pipefail
cd "$(dirname "$0")/.."
[ -f lib/refs/Assembly-CSharp.dll ] || { echo "lib/refs fehlt – erst scripts/make-refs.sh ausführen" >&2; exit 1; }

SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:8.0
run() {
  if command -v dotnet >/dev/null; then "$@"; else
    docker run --rm -u "$(id -u):$(id -g)" -e HOME=/tmp -e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
      -v "$PWD":/src -w /src "$SDK_IMAGE" "$@"
  fi
}
run dotnet build src/NORevokerECR.csproj -c Release -o build
run dotnet test tests/NORevokerECR.Tests.csproj

version=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' src/NORevokerECR.csproj)
rm -rf dist && mkdir -p dist
(cd build && zip -q "../dist/NO-RevokerECR-$version.zip" NORevokerECR.dll)
echo "dist/NO-RevokerECR-$version.zip"
