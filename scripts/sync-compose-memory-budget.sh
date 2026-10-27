#!/usr/bin/env bash
# Copy restart, mem_limit, and memswap_limit from a base Compose file onto
# an existing file. NAS-only mounts and entrypoints on the target stay.
# Usage: sync-compose-memory-budget.sh BASE TARGET
set -euo pipefail

if [[ $# -ne 2 ]]; then
	echo "usage: sync-compose-memory-budget.sh BASE TARGET" >&2
	exit 2
fi

base="${1}"
target="${2}"

if [[ ! -f ${base} || ! -f ${target} ]]; then
	echo "base and target compose files must exist" >&2
	exit 2
fi

tmp="$(mktemp)"
awk -v base_file="${base}" '
function tracked(name) {
	return name == "ollama" || name == "tikr-api" || name == "tikr-web"
}
function service_of(line, name) {
	if (line ~ /^  [A-Za-z0-9_-]+:$/) {
		name = substr(line, 3)
		sub(/:$/, "", name)
		return name
	}
	return ""
}
function flush_missing(svc) {
	if (svc == "") {
		return
	}
	if (!(svc in saw_restart) && (svc in restart)) {
		print restart[svc]
	}
	if (!(svc in saw_mem) && (svc in mem)) {
		print mem[svc]
	}
	if (!(svc in saw_swap) && (svc in swap)) {
		print swap[svc]
	}
	delete saw_restart[svc]
	delete saw_mem[svc]
	delete saw_swap[svc]
}
BEGIN {
	while ((getline line < base_file) > 0) {
		svc = service_of(line)
		if (svc != "") {
			current = tracked(svc) ? svc : ""
			continue
		}
		if (current == "") {
			continue
		}
		if (line ~ /^    restart: /) {
			restart[current] = line
		} else if (line ~ /^    mem_limit: /) {
			mem[current] = line
		} else if (line ~ /^    memswap_limit: /) {
			swap[current] = line
		} else if (line ~ /^[^ ]/) {
			current = ""
		}
	}
	close(base_file)
	current = ""
}
{
	svc = service_of($0)
	if (svc != "") {
		flush_missing(current)
		current = tracked(svc) ? svc : ""
		print
		next
	}
	if (current != "" && $0 ~ /^    restart: / && (current in restart)) {
		print restart[current]
		saw_restart[current] = 1
		next
	}
	if (current != "" && $0 ~ /^    mem_limit: / && (current in mem)) {
		print mem[current]
		saw_mem[current] = 1
		next
	}
	if (current != "" && $0 ~ /^    memswap_limit: / && (current in swap)) {
		print swap[current]
		saw_swap[current] = 1
		next
	}
	if (current != "" && $0 ~ /^[^ ]/) {
		flush_missing(current)
		current = ""
	}
	print
}
END {
	flush_missing(current)
}
' "${target}" >"${tmp}"
mv "${tmp}" "${target}"
