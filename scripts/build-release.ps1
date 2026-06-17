param(
    [string]$Version = "0.0.1"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..")
$ProjectFile = Join-Path $ProjectRoot "src\SoundMonitor\SoundMonitor.csproj"

$DistDir = Join-Path $ProjectRoot "dist"
$PortableDir = Join-Path $DistDir "portable-win-x64"
$SingleExeDir = Join-Path $DistDir "single-exe-win-x64"
$InstallerDir = Join-Path $DistDir "installer"

$PortableZip = Join-Path $DistDir ("SoundMonitor-portable-win-x64-v{0}.zip" -f $Version)

Write-Host "[1/6] Cleaning dist directory..."
if (Test-Path $DistDir) {
    Remove-Item -Path $DistDir -Recurse -Force
}
New-Item -ItemType Directory -Path $PortableDir -Force | Out-Null
New-Item -ItemType Directory -Path $SingleExeDir -Force | Out-Null
New-Item -ItemType Directory -Path $InstallerDir -Force | Out-Null

Write-Host "[2/6] Restoring dependencies..."
dotnet restore $ProjectFile

Write-Host "[3/6] Building Release..."
dotnet build $ProjectFile -c Release --no-restore

Write-Host "[4/6] Publishing self-contained portable folder..."
dotnet publish $ProjectFile `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o $PortableDir `
    /p:PublishSingleFile=false `
    /p:PublishReadyToRun=true `
    /p:PublishTrimmed=false

Write-Host "[5/6] Publishing self-contained single EXE..."
dotnet publish $ProjectFile `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o $SingleExeDir `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true `
    /p:PublishTrimmed=false `
    /p:DebugType=None `
    /p:DebugSymbols=false

if (Test-Path $PortableZip) {
    Remove-Item $PortableZip -Force
}
Write-Host "[6/6] Creating portable ZIP..."
Compress-Archive -Path (Join-Path $PortableDir "*") -DestinationPath $PortableZip -Force

$IsccCommand = Get-Command iscc.exe -ErrorAction SilentlyContinue
if ($null -ne $IsccCommand) {
    $InstallerScript = Join-Path $ProjectRoot "installer\SoundGuard.iss"
    if (Test-Path $InstallerScript) {
        Write-Host "Building installer with Inno Setup..."
        & $IsccCommand.Path $InstallerScript "/DMyAppVersion=$Version" "/DSourceDir=$PortableDir" "/DOutputDir=$InstallerDir"
    }
}
else {
    Write-Warning "Inno Setup (iscc.exe) was not found, skipped installer build."
    Write-Warning "Install Inno Setup to generate setup EXE automatically."
}

Write-Host ""
Write-Host "Artifacts ready:"
Write-Host "- Portable folder: $PortableDir"
Write-Host "- Single EXE:      $(Join-Path $SingleExeDir 'SoundMonitor.exe')"
Write-Host "- Portable ZIP:    $PortableZip"
if (Get-ChildItem -Path $InstallerDir -Filter "*.exe" -ErrorAction SilentlyContinue) {
    Write-Host "- Installer EXE:   $InstallerDir"
}
