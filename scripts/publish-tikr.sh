#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="${ROOT}/publish"

dotnet publish "${ROOT}/src/TIKR.Api/TIKR.Api.csproj" \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o "${OUT}/TIKR.Api"

dotnet publish "${ROOT}/src/TIKR.Web/TIKR.Web.csproj" \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o "${OUT}/TIKR.Web"

echo "Published:"
echo "  ${OUT}/TIKR.Api/TIKR.Api.exe"
echo "  ${OUT}/TIKR.Web/TIKR.Web.exe"
