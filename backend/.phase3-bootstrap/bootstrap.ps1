# One-off helper for the Phase 3 build session. Not part of the repository; deleted at the end of Phase 3.
# 1. Resolves the latest stable NuGet versions for Directory.Packages.props and the dotnet-ef tool.
# 2. Restores the backend solution and the dotnet-ef local tool into a folder next to this script.
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$backend = Split-Path -Parent $here

function Get-Latest([string]$id, [int]$major = 0) {
    $index = Invoke-RestMethod "https://api.nuget.org/v3-flatcontainer/$($id.ToLowerInvariant())/index.json"
    $stable = $index.versions | Where-Object { $_ -notmatch '-' }
    if ($major -gt 0) { $stable = $stable | Where-Object { $_ -match "^$major\." } }
    ($stable | Sort-Object { [version]($_ -replace '^(\d+\.\d+\.\d+).*$', '$1') } | Select-Object -Last 1)
}
$ids = [ordered]@{
    'Npgsql.EntityFrameworkCore.PostgreSQL' = 10
    'EFCore.NamingConventions' = 10
    'Microsoft.EntityFrameworkCore.Design' = 10
    'Microsoft.AspNetCore.OpenApi' = 10
    'Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore' = 10
    'Dapper' = 0
    'Scalar.AspNetCore' = 0
}
$versions = [ordered]@{}
foreach ($id in $ids.Keys) { $versions[$id] = Get-Latest $id $ids[$id]; Write-Host "$id = $($versions[$id])" }
$versions['dotnet-ef'] = $versions['Microsoft.EntityFrameworkCore.Design']   # tool must match the design package
$versions | ConvertTo-Json | Set-Content "$here\versions.json"
foreach ($file in 'Directory.Packages.props', '.config\dotnet-tools.json') {
    $path = Join-Path $backend $file
    $text = Get-Content $path -Raw
    foreach ($id in $versions.Keys) { $text = $text.Replace("@@$id@@", $versions[$id]) }
    Set-Content $path $text -NoNewline
}

Set-Location $backend
$env:NUGET_PACKAGES = "$here\packages"
dotnet --list-sdks | Set-Content "$here\sdks.txt"
dotnet restore SarifHub.slnx *> "$here\restore.log"
$restoreExit = $LASTEXITCODE
dotnet tool restore *>> "$here\restore.log"
Get-ChildItem "$here\packages" -Recurse -Filter *.nupkg | ForEach-Object { $_.FullName } | Set-Content "$here\nupkgs.txt"
"restore=$restoreExit tool=$LASTEXITCODE" | Set-Content "$here\DONE.txt"
Write-Host "Bootstrap finished. You can close this window."
