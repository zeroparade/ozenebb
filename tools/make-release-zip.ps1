param(
    [string] $OutputZip = "ESOTERIC_EBB_VoiceOverride_Installer.zip"
)

$ErrorActionPreference = "Stop"

$RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$OutputZipPath = if ([System.IO.Path]::IsPathRooted($OutputZip)) {
    $OutputZip
} else {
    Join-Path $RepoRoot $OutputZip
}
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

if (Test-Path -LiteralPath $OutputZipPath) {
    Remove-Item -LiteralPath $OutputZipPath -Force
}

$zip = [System.IO.Compression.ZipFile]::Open($OutputZipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    function Add-Entry {
        param(
            [string] $Source,
            [string] $Entry
        )
        if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) {
            throw "Missing release file: $Source"
        }
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $script:zip,
            $Source,
            $Entry,
            [System.IO.Compression.CompressionLevel]::Optimal
        ) | Out-Null
    }

    Add-Entry (Join-Path $RepoRoot "installer/Install.bat") "Install.bat"
    Add-Entry (Join-Path $RepoRoot "installer/install.ps1") "install.ps1"
    Add-Entry (Join-Path $RepoRoot "installer/installer-config.json") "installer-config.json"
    Add-Entry (Join-Path $RepoRoot "installer/INSTALLER_README.txt") "README.txt"
} finally {
    $zip.Dispose()
}

Write-Host "Wrote $OutputZipPath"
