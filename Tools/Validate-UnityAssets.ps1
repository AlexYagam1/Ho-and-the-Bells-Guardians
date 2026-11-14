param([string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent), [switch]$ChangedOnly)
$ErrorActionPreference = 'Stop'
Push-Location -LiteralPath $ProjectRoot
try {
    $known = [System.Collections.Generic.HashSet[string]]::new()
    $owned = @{}
    $issues = [System.Collections.Generic.List[string]]::new()
    $metaFiles = @(rg --files Assets Packages Library/PackageCache -g '*.meta' --hidden)
    foreach ($path in $metaFiles) {
        $content = Get-Content -LiteralPath $path -Raw
        $match = [regex]::Match($content, '(?m)^guid: ([a-f0-9]{32})\s*$')
        if (-not $match.Success) { $issues.Add("Malformed GUID: $path"); continue }
        $guid = $match.Groups[1].Value
        [void]$known.Add($guid)
        if ($path -match '^Assets[\\/]') {
            if ($owned.ContainsKey($guid)) { $issues.Add("Duplicate GUID: $path and $($owned[$guid])") }
            else { $owned[$guid] = $path }
            if (-not (Test-Path -LiteralPath $path.Substring(0, $path.Length - 5))) { $issues.Add("Orphan meta: $path") }
        }
    }
    $assets = @(rg --files Assets -g '*.cs' -g '*.prefab' -g '*.unity' -g '*.asset' -g '*.mat')
    if ($ChangedOnly) {
        $changed = @((git -c core.quotepath=false diff --name-only HEAD), (git -c core.quotepath=false ls-files --others --exclude-standard)) | ForEach-Object { $_ }
        $assets = @($assets | Where-Object { ($_ -replace '\\','/') -in $changed })
    }
    foreach ($path in $assets) {
        if (-not (Test-Path -LiteralPath "$path.meta")) { $issues.Add("Missing meta: $path") }
        $content = Get-Content -LiteralPath $path -Raw
        foreach ($match in [regex]::Matches($content, 'm_Script: \{fileID: 11500000, guid: ([a-f0-9]{32})')) {
            if (-not $known.Contains($match.Groups[1].Value)) { $issues.Add("Unresolved script GUID: $path : $($match.Groups[1].Value)") }
        }
    }
    [pscustomobject]@{ Passed = ($issues.Count -eq 0); ChangedOnly = [bool]$ChangedOnly; CheckedAssets = $assets.Count; AssetGuids = $owned.Count; Issues = @($issues.ToArray()) } | ConvertTo-Json -Depth 4
    if ($issues.Count -gt 0) { exit 1 }
}
finally { Pop-Location }
