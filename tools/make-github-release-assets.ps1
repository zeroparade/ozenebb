param(
    [string] $ReleaseDir = "release",
    [string] $BuiltPlugin = "src/bin/Release/net6.0/EsotericEbbVoiceOverride.dll"
)

$ErrorActionPreference = "Stop"
$RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$ReleaseDirPath = if ([System.IO.Path]::IsPathRooted($ReleaseDir)) {
    $ReleaseDir
} else {
    Join-Path $RepoRoot $ReleaseDir
}
$BuiltPluginPath = if ([System.IO.Path]::IsPathRooted($BuiltPlugin)) {
    $BuiltPlugin
} else {
    Join-Path $RepoRoot $BuiltPlugin
}
if (-not (Test-Path -LiteralPath $BuiltPluginPath -PathType Leaf)) {
    throw "Built plugin not found: $BuiltPluginPath"
}

New-Item -ItemType Directory -Force -Path $ReleaseDirPath | Out-Null
$pluginAsset = Join-Path $ReleaseDirPath "EsotericEbbVoiceOverride.dll"
$checksumAsset = "$pluginAsset.sha256"
$installerAsset = Join-Path $ReleaseDirPath "ESOTERIC_EBB_VoiceOverride_Installer.zip"

Copy-Item -LiteralPath $BuiltPluginPath -Destination $pluginAsset -Force
$hash = (Get-FileHash -LiteralPath $pluginAsset -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath $checksumAsset -Value "$hash  EsotericEbbVoiceOverride.dll" -Encoding ASCII
& (Join-Path $PSScriptRoot "make-release-zip.ps1") -OutputZip $installerAsset

Write-Host "Release assets:"
Write-Host "  $pluginAsset"
Write-Host "  $checksumAsset"
Write-Host "  $installerAsset"
