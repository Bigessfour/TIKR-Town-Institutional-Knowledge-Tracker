# Called elevated by the Inno Setup installer (or manually as Administrator).
param(
    [int]$ApiPort = 5000,
    [int]$WebPort = 8080,
    [ValidateSet("Add", "Remove")]
    [string]$Action = "Add"
)

$ErrorActionPreference = "Stop"

function Set-TikrRule {
    param([string]$Name, [int]$Port, [string]$Mode)

    $existing = Get-NetFirewallRule -DisplayName $Name -ErrorAction SilentlyContinue
    if ($Mode -eq "Remove") {
        if ($existing) {
            Remove-NetFirewallRule -DisplayName $Name -ErrorAction SilentlyContinue
            Write-Host "Removed firewall rule: $Name"
        }
        return
    }

    if (-not $existing) {
        New-NetFirewallRule `
            -DisplayName $Name `
            -Direction Inbound `
            -Action Allow `
            -Protocol TCP `
            -LocalPort $Port `
            -Profile Any `
            -Description "TIKR — Clerk's Vault (municipal local-first app)" | Out-Null
        Write-Host "Added firewall rule: $Name (TCP $Port)"
    }
    else {
        Write-Host "Firewall rule already present: $Name"
    }
}

Set-TikrRule -Name "TIKR API ($ApiPort)" -Port $ApiPort -Mode $Action
Set-TikrRule -Name "TIKR Web ($WebPort)" -Port $WebPort -Mode $Action
