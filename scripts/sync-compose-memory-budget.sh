#!/usr/bin/env bash
# Copy restart, mem_limit, and memswap_limit from a base Compose file onto
# an existing file. Indentation may be spaces or tabs. NAS-only mounts and
# entrypoints stay. Exits non-zero when the target has no ollama, tikr-api,
# and tikr-web services, so a deploy cannot keep a silent old memory fence.
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
function trim(s, t) {
	t = s
	gsub(/\r/, "", t)
	sub(/^[[:space:]]+/, "", t)
	sub(/[[:space:]]+$/, "", t)
	return t
}
function service_name(line, t) {
	t = trim(line)
	if (t != "ollama:" && t != "tikr-api:" && t != "tikr-web:") {
		return ""
	}
	sub(/:$/, "", t)
	return t
}
function key_of(line, t) {
	t = trim(line)
	if (t !~ /^(restart|mem_limit|memswap_limit|memory):/) {
		return ""
	}
	sub(/[[:space:]]*:.*/, "", t)
	return t
}
function value_of(line, t) {
	t = trim(line)
	sub(/^[^:]+:[[:space:]]*/, "", t)
	gsub(/\r/, "", t)
	return t
}
function flush_missing(svc) {
	if (svc == "") {
		return
	}
	if (!(svc in saw_restart) && (svc in restart_line)) {
		print restart_line[svc]
	}
	if (!(svc in saw_mem) && (svc in mem_line)) {
		print mem_line[svc]
	}
	if (!(svc in saw_swap) && (svc in swap_line)) {
		print swap_line[svc]
	}
}
BEGIN {
	while ((getline line < base_file) > 0) {
		svc = service_name(line)
		if (svc != "") {
			current = svc
			continue
		}
		if (current == "") {
			continue
		}
		key = key_of(line)
		if (key == "restart") {
			restart_line[current] = line
			gsub(/\r/, "", restart_line[current])
		} else if (key == "mem_limit") {
			mem_line[current] = line
			gsub(/\r/, "", mem_line[current])
			mem_value[current] = value_of(line)
		} else if (key == "memswap_limit") {
			swap_line[current] = line
			gsub(/\r/, "", swap_line[current])
		}
	}
	close(base_file)
	current = ""
	for (svc in mem_value) {
		base_services++
	}
	if (base_services < 3) {
		print "compose memory budget: base file is missing mem_limit for ollama, tikr-api, or tikr-web" > "/dev/stderr"
		exit 1
	}
}
{
	gsub(/\r/, "", $0)
	svc = service_name($0)
	if (svc != "") {
		flush_missing(current)
		current = svc
		seen[svc] = 1
		print
		next
	}
	key = current == "" ? "" : key_of($0)
	if (key == "restart" && (current in restart_line)) {
		print restart_line[current]
		saw_restart[current] = 1
		next
	}
	if (key == "mem_limit" && (current in mem_line)) {
		print mem_line[current]
		saw_mem[current] = 1
		next
	}
	if (key == "memswap_limit" && (current in swap_line)) {
		print swap_line[current]
		saw_swap[current] = 1
		next
	}
	if (key == "memory" && (current in mem_value) && value_of($0) ~ /^[0-9]/) {
		indent = $0
		sub(/memory.*/, "", indent)
		print indent "memory: " mem_value[current]
		saw_mem[current] = 1
		next
	}
	print
}
END {
	if (base_services < 3) {
		exit 1
	}
	flush_missing(current)
	if (!seen["ollama"] || !seen["tikr-api"] || !seen["tikr-web"]) {
		print "compose memory budget: target is missing ollama, tikr-api, or tikr-web; left the file unchanged" > "/dev/stderr"
		exit 1
	}
}
' "${target}" >"${tmp}"
mv "${tmp}" "${target}"
