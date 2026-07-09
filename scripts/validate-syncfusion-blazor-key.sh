#!/usr/bin/env bash
# Test which Keychain entry holds a Blazor v34-valid SYNCFUSION license (never prints keys).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$ROOT/scripts/.validate-blazor-license"
mkdir -p "$PROJECT"

cat > "$PROJECT/Validate.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Syncfusion.Licensing" Version="34.1.29" />
  </ItemGroup>
</Project>
EOF

cat > "$PROJECT/Program.cs" <<'EOF'
using Syncfusion.Licensing;
var key = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
if (string.IsNullOrWhiteSpace(key)) { Console.WriteLine("MISSING"); return 2; }
key = key.Trim();
SyncfusionLicenseProvider.RegisterLicense(key);
string? err = null;
var ok = SyncfusionLicenseProvider.ValidateLicense([Platform.Blazor], out err);
Console.WriteLine(ok ? "VALID" : $"INVALID:{err}");
return ok ? 0 : 1;
EOF

trim_key() {
  local key="$1"
  key="${key//$'\r'/}"
  key="${key//$'\n'/}"
  key="${key#"${key%%[![:space:]]*}"}"
  key="${key%"${key##*[![:space:]]}"}"
  printf '%s' "$key"
}

try_lookup() {
  local svc="$1" acct="${2:-}"
  local raw=""
  if [[ -n "$acct" ]]; then
    raw=$(security find-generic-password -s "$svc" -a "$acct" -w 2>/dev/null || true)
  else
    raw=$(security find-generic-password -s "$svc" -w 2>/dev/null || true)
  fi
  trim_key "$raw"
}

echo "Checking Keychain entries for Blazor 34.1.x license validity..."
dotnet build "$PROJECT/Validate.csproj" -c Release -v q >/dev/null

CANDIDATES=(
  "SYNCFUSION_LICENSE_KEY|SYNCFUSION"
  "SYNCFUSION_LICENSE_KEY|SYNCFUSION_LICENSE_KEY"
  "com.wileyco.syncfusion.license|stephenmckitrick"
  "com.wileyco.syncfusion.license|"
  "Syncfusion License Key|syncfusion"
)

found_valid=""
for pair in "${CANDIDATES[@]}"; do
  svc="${pair%%|*}"
  acct="${pair#*|}"
  key="$(try_lookup "$svc" "$acct")"
  [[ -z "$key" ]] && continue
  label="$svc"
  [[ -n "$acct" ]] && label="$svc (account: $acct)"
  result=$(SYNCFUSION_LICENSE_KEY="$key" dotnet "$PROJECT/bin/Release/net10.0/Validate.dll" 2>/dev/null || echo "ERROR")
  chars=${#key}
  echo "  • $label — ${chars} chars — $result"
  if [[ "$result" == "VALID" ]]; then
    found_valid="$svc|$acct"
  fi
done

echo ""
if [[ -n "$found_valid" ]]; then
  echo "Use the VALID entry above in Passwords. sync-syncfusion-license-key.sh prefers service SYNCFUSION_LICENSE_KEY first."
  exit 0
fi
echo "No Keychain entry validated for Platform.Blazor v34. Generate a Blazor 34.1.x key at https://www.syncfusion.com/account/downloads"
exit 1