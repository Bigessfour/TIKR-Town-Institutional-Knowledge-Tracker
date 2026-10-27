#!/usr/bin/env bash
# Proves the 16 GB budget (8g/3g/1g) replaces an old fence without dropping NAS overrides.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
sync="${ROOT}/scripts/sync-compose-memory-budget.sh"
tmp="$(mktemp -d)"
trap 'rm -rf "${tmp}"' EXIT

fail() {
	echo "FAIL: $*" >&2
	exit 1
}

# One matching key inside a service. Value is the text after the colon.
assert_one() {
	local file="$1"
	local service="$2"
	local key="$3"
	local value="$4"
	local count
	count="$(
		awk -v service="${service}" -v key="${key}" -v value="${value}" '
			function trim(s, t) {
				t = s
				gsub(/\r/, "", t)
				sub(/^[[:space:]]+/, "", t)
				sub(/[[:space:]]+$/, "", t)
				return t
			}
			function name_of(line, t) {
				t = trim(line)
				if (t != "ollama:" && t != "tikr-api:" && t != "tikr-web:") {
					return ""
				}
				sub(/:$/, "", t)
				return t
			}
			{
				svc = name_of($0)
				if (svc != "") {
					current = svc
					next
				}
				if (current != service) {
					next
				}
				line = trim($0)
				prefix = key ":"
				if (index(line, prefix) == 1) {
					got = line
					sub(/^[^:]+:[[:space:]]*/, "", got)
					if (got == value) {
						hits++
					} else {
						other++
					}
				}
			}
			END { print hits + 0 }
		' "${file}"
	)"
	[[ ${count} == "1" ]] || fail "${service} ${key} expected one ${value}, found ${count} in ${file}"
}

cat >"${tmp}/base.yml" <<'EOF'
services:
  ollama:
    image: ollama/ollama:latest
    restart: unless-stopped
    mem_limit: 8g
    memswap_limit: 8g
  tikr-api:
    image: ghcr.io/bigessfour/tikr-api:1.0.3
    restart: unless-stopped
    mem_limit: 3g
    memswap_limit: 3g
  tikr-web:
    image: ghcr.io/bigessfour/tikr-web:1.0.3
    restart: unless-stopped
    mem_limit: 1g
    memswap_limit: 1g
EOF

cat >"${tmp}/nas.yml" <<'EOF'
services:
  ollama:
    image: ollama/ollama:latest
    restart: on-failure:5
    mem_limit: 1792m
    memswap_limit: 1792m
  tikr-api:
    image: tikr-api:corpus-fix
    restart: on-failure:5
    mem_limit: 1536m
    entrypoint: ["/bin/custom"]
    volumes:
      - /volume1/Town of Wiley Shared Documents:/town-docs:ro
  tikr-web:
    image: tikr-web:corpus-fix
    restart: on-failure:5
    mem_limit: 512m
EOF

bash "${sync}" "${tmp}/base.yml" "${tmp}/nas.yml"
assert_one "${tmp}/nas.yml" ollama mem_limit 8g
assert_one "${tmp}/nas.yml" ollama memswap_limit 8g
assert_one "${tmp}/nas.yml" tikr-api mem_limit 3g
assert_one "${tmp}/nas.yml" tikr-api memswap_limit 3g
assert_one "${tmp}/nas.yml" tikr-web mem_limit 1g
assert_one "${tmp}/nas.yml" tikr-web memswap_limit 1g
assert_one "${tmp}/nas.yml" tikr-api restart unless-stopped
grep -q 'image: tikr-api:corpus-fix' "${tmp}/nas.yml" || fail "image tag changed"
grep -q 'entrypoint: \["/bin/custom"\]' "${tmp}/nas.yml" || fail "entrypoint dropped"
grep -q '/volume1/Town of Wiley Shared Documents:/town-docs:ro' "${tmp}/nas.yml" || fail "mount dropped"
if grep -q '1792m\|1536m\|512m\|on-failure:5' "${tmp}/nas.yml"; then
	fail "old memory fence still present"
fi

cp "${tmp}/nas.yml" "${tmp}/nas-once.yml"
bash "${sync}" "${tmp}/base.yml" "${tmp}/nas.yml"
cmp "${tmp}/nas-once.yml" "${tmp}/nas.yml" || fail "second run changed the file"

# Tabs and a limit nested under deploy.resources. The old key must be updated in place.
printf 'services:\n\tollama:\n\t\trestart: on-failure:5\n\t\tmem_limit: 1792m\n\t\tmemswap_limit: 1792m\n\ttikr-api:\n\t\trestart: on-failure:5\n\t\tdeploy:\n\t\t\tresources:\n\t\t\t\tlimits:\n\t\t\t\t\tmemory: 1536m\n\t\tvolumes:\n\t\t\t- /volume1/town-docs:/town-docs:ro\n\ttikr-web:\n\t\trestart: on-failure:5\n      mem_limit: 512m\n      memswap_limit: 512m\n' >"${tmp}/tabs.yml"
bash "${sync}" "${tmp}/base.yml" "${tmp}/tabs.yml"
assert_one "${tmp}/tabs.yml" ollama mem_limit 8g
assert_one "${tmp}/tabs.yml" tikr-api memory 3g
assert_one "${tmp}/tabs.yml" tikr-web mem_limit 1g
grep -q '/volume1/town-docs:/town-docs:ro' "${tmp}/tabs.yml" || fail "tabbed mount dropped"
if grep -q '1792m\|1536m\|512m' "${tmp}/tabs.yml"; then
	fail "tabbed fence still present"
fi
api_mem_lines="$(awk '
	function trim(s, t) { t = s; sub(/^[[:space:]]+/, "", t); sub(/[[:space:]]+$/, "", t); return t }
	trim($0) == "tikr-api:" { on = 1; next }
	on && (trim($0) == "tikr-web:" || trim($0) == "ollama:") { on = 0 }
	on && trim($0) ~ /^(mem_limit|memory):/ { n++ }
	END { print n + 0 }
' "${tmp}/tabs.yml")"
[[ ${api_mem_lines} == "1" ]] || fail "tikr-api gained a second memory key (${api_mem_lines})"

# The repo compose file is the 16 GB budget. An old NAS file must pick up those caps.
cp "${ROOT}/docker/docker-compose.prod.yml" "${tmp}/repo-base.yml"
cat >"${tmp}/old-nas.yml" <<'EOF'
services:
  ollama:
    restart: on-failure:5
    mem_limit: 1792m
    memswap_limit: 1792m
  tikr-api:
    image: tikr-api:corpus-fix
    restart: on-failure:5
    mem_limit: 1536m
    memswap_limit: 1536m
  tikr-web:
    restart: on-failure:5
    mem_limit: 512m
    memswap_limit: 512m
EOF
bash "${sync}" "${tmp}/repo-base.yml" "${tmp}/old-nas.yml"
assert_one "${tmp}/old-nas.yml" ollama mem_limit 8g
assert_one "${tmp}/old-nas.yml" tikr-api mem_limit 3g
assert_one "${tmp}/old-nas.yml" tikr-web mem_limit 1g
grep -q 'image: tikr-api:corpus-fix' "${tmp}/old-nas.yml" || fail "repo base replaced the NAS image tag"

cat >"${tmp}/not-tikr.yml" <<'EOF'
services:
  other:
    image: example:latest
EOF
cp "${tmp}/not-tikr.yml" "${tmp}/not-tikr-before.yml"
if bash "${sync}" "${tmp}/base.yml" "${tmp}/not-tikr.yml"; then
	fail "missing services should fail the deploy"
fi
cmp "${tmp}/not-tikr-before.yml" "${tmp}/not-tikr.yml" || fail "failed sync rewrote the target"

echo "sync-compose-memory-budget: pass"
