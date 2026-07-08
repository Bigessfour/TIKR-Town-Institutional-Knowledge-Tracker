#!/usr/bin/env bash
# Shared helpers for updating gitignored .env files without printing secret values.
set -euo pipefail

env_file_upsert() {
  local file="$1"
  local key="$2"
  local value="$3"

  python3 - "$file" "$key" "$value" <<'PY'
import os
import re
import sys

path, key, value = sys.argv[1], sys.argv[2], sys.argv[3]
escaped = value.replace("\\", "\\\\").replace('"', '\\"')
line = f'{key}="{escaped}"\n'
pattern = re.compile(rf"^{re.escape(key)}=")

lines: list[str] = []
if os.path.isfile(path):
    with open(path, encoding="utf-8") as handle:
        lines = handle.readlines()

out: list[str] = []
found = False
for existing in lines:
    if pattern.match(existing):
        out.append(line)
        found = True
    else:
        out.append(existing)

if not found:
    if out and not out[-1].endswith("\n"):
        out[-1] += "\n"
    out.append(line)

with open(path, "w", encoding="utf-8") as handle:
    handle.writelines(out)
PY
}

ensure_docker_env_file() {
  local root="$1"
  local env_file="$root/docker/.env"
  local example="$root/docker/.env.example"

  if [[ -f "$env_file" ]]; then
    printf '%s' "$env_file"
    return 0
  fi

  if [[ ! -f "$example" ]]; then
    echo "Missing $example — cannot create docker/.env." >&2
    return 1
  fi

  cp "$example" "$env_file"
  printf '%s' "$env_file"
}