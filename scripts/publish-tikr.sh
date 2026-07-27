#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="${ROOT}/publish"

# Self-contained win-x64 — no global .NET install on the laptop.
# PublishTrimmed stays off (Syncfusion + Blazor). Set PUBLISH_SINGLE_FILE=0 to troubleshoot.
SINGLE="${PUBLISH_SINGLE_FILE:-true}"

# ErrorOnDuplicatePublishOutputFiles=false: Syncfusion.PDF.OCR.Net.Core (clerk OCR)
# and DocumentSDK.AI.AgentTools both ship identical Tesseract/leptonica natives.
dotnet publish "${ROOT}/src/TIKR.Api/TIKR.Api.csproj" \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile="${SINGLE}" \
  -p:PublishTrimmed=false \
  -p:ErrorOnDuplicatePublishOutputFiles=false \
  -o "${OUT}/TIKR.Api"

dotnet publish "${ROOT}/src/TIKR.Web/TIKR.Web.csproj" \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile="${SINGLE}" \
  -p:PublishTrimmed=false \
  -p:ErrorOnDuplicatePublishOutputFiles=false \
  -o "${OUT}/TIKR.Web"

echo "Published:"
echo "  ${OUT}/TIKR.Api/TIKR.Api.exe"
echo "  ${OUT}/TIKR.Web/TIKR.Web.exe"
