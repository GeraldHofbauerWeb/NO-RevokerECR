#!/usr/bin/env bash
# Prints the meta.json NOMM keeps next to an installed mod.
# No dependencies: the EA-12 needs neither Blueprinter nor the Configuration
# Manager. Every player in a multiplayer session needs the same version.
# Usage: scripts/meta-json.sh <version> <gameVersion>
set -euo pipefail
cat <<JSON
{"id":"NO-RevokerECR","artifact":{"fileName":"NO-RevokerECR-$1.zip","version":"$1","category":"release","type":"plugin","gameVersion":"$2","dependencies":[],"incompatibilities":[]}}
JSON
