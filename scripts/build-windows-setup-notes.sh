#!/usr/bin/env bash
# Print the Windows Setup-TIKR.exe build steps (payload on Mac/Linux; ISCC on Windows).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DEPLOY="${ROOT}/publish/TIKR-Deploy"
VERSION="${TIKR_SETUP_VERSION:-1.0.1}"

echo "=== TIKR canonical Windows Setup (IT checklist) ==="
echo ""
echo "1) Build payload (this machine):"
echo "   SKIP_TESTS=1 ${ROOT}/scripts/package-thumb-drive.sh"
echo "   Expect: ${DEPLOY}/TIKR.Api/TIKR.Api.exe"
echo "           ${DEPLOY}/TIKR.Web/TIKR.Web.exe"
echo ""
if [[ -x "${DEPLOY}/TIKR.Api/TIKR.Api.exe" ]] || [[ -f "${DEPLOY}/TIKR.Api/TIKR.Api.exe" ]]; then
  echo "   Status: payload PRESENT"
else
  echo "   Status: payload MISSING — run package-thumb-drive.sh first"
fi
echo ""
echo "2) Copy repo (or at least installer/ + publish/TIKR-Deploy) to a Windows PC with Inno Setup 6."
echo ""
echo "3) Compile Setup-TIKR.exe:"
echo "   cd path\\to\\repo\\installer"
echo "   & \"C:\\Program Files (x86)\\Inno Setup 6\\ISCC.exe\" \`"
echo "     /DMySourceDir=\"${DEPLOY}\" \`"
echo "     /DMyAppVersion=\"${VERSION}\" \`"
echo "     .\\tikr-setup.iss"
echo ""
echo "   Output: installer\\Output\\Setup-TIKR.exe"
echo ""
echo "4) Give Deb/Paige Setup-TIKR.exe → docs/clerk-windows-install.md"
echo "5) Smoke: docs/clerk-windows-smoke.md"
echo ""
echo "Auth: off (trusted shared PC). Data: C:\\ProgramData\\TIKR"
echo "Ollama: included in Setup (Ensure-Ollama.ps1). Optional offline: INCLUDE_OLLAMA_SETUP=1 package-thumb-drive.sh"
echo "Thumb drive for clerks: copy Setup-TIKR.exe (not the whole publish tree unless USB mode)."
