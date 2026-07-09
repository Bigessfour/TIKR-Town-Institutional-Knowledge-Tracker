#!/usr/bin/env bash
# Build a Windows self-contained TIKR-Deploy folder for USB copy to a Dell laptop.
# Run on macOS/Linux with .NET SDK 10 (cross-publish win-x64). See docs/windows-thumb-drive-deploy.md
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

DEPLOY="${ROOT}/publish/TIKR-Deploy"
WIN="${ROOT}/deploy/windows"

echo "=== TIKR thumb-drive package (win-x64) ==="

if [[ "${SKIP_TESTS:-}" != "1" ]]; then
  echo "→ dotnet test (Release)"
  dotnet test TIKR.sln --configuration Release
fi

echo "→ dotnet publish win-x64 self-contained"
"${ROOT}/scripts/publish-tikr.sh"

echo "→ Assemble ${DEPLOY}"
rm -rf "${DEPLOY}"
mkdir -p "${DEPLOY}/Data/documents" "${DEPLOY}/TIKR.Api" "${DEPLOY}/TIKR.Web"

rsync -a "${ROOT}/publish/TIKR.Api/" "${DEPLOY}/TIKR.Api/"
rsync -a "${ROOT}/publish/TIKR.Web/" "${DEPLOY}/TIKR.Web/"

cp "${WIN}/appsettings.Production.Api.json" "${DEPLOY}/TIKR.Api/appsettings.Production.json"
cp "${WIN}/appsettings.Production.Web.json" "${DEPLOY}/TIKR.Web/appsettings.Production.json"

for f in Start-TIKR.bat Start-TIKR.ps1 Install-TIKR.ps1 Stop-TIKR.ps1 Deploy-To-NAS.ps1 README-QuickStart.txt tikr-secrets.ps1.example; do
  cp "${WIN}/${f}" "${DEPLOY}/${f}"
done

ZIP="${ROOT}/publish/TIKR-Deploy-win-x64.zip"
if command -v zip >/dev/null 2>&1; then
  echo "→ Zip ${ZIP}"
  (cd "${ROOT}/publish" && rm -f TIKR-Deploy-win-x64.zip && zip -rq TIKR-Deploy-win-x64.zip TIKR-Deploy)
fi

du -sh "${DEPLOY}" "${ZIP}" 2>/dev/null || du -sh "${DEPLOY}"

echo ""
echo "✅ Ready for USB:"
echo "   Folder: ${DEPLOY}"
echo "   Zip:    ${ZIP} (if zip available)"
echo "   On Dell: copy TIKR-Deploy → run Install-TIKR.ps1 (Admin) → Start-TIKR.bat"