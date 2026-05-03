$ErrorActionPreference = 'Stop'

$root = 'C:\Users\Archon\Desktop\StillMod'
$menu = Join-Path $root 'StillMenu'
$source = Join-Path $menu 'Scopable_App.cs'
$exe = Join-Path $menu 'Scopable_App.exe'
$backupDir = Join-Path $menu 'backups'

New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
if (Test-Path $source) {
  Copy-Item $source (Join-Path $backupDir ('Scopable_App_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.cs.bak')) -Force
}
if (Test-Path $exe) {
  Copy-Item $exe (Join-Path $backupDir ('Scopable_App_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.exe.bak')) -Force
}

$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
  $csc = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path $csc)) {
  throw 'Could not locate .NET Framework csc.exe'
}

$wpfCandidates = @(
  'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF',
  'C:\Windows\Microsoft.NET\Framework\v4.0.30319\WPF',
  'C:\Program Files\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8'
)

$presentationFramework = $null
$presentationCore = $null
$windowsBase = $null

foreach ($candidate in $wpfCandidates) {
  if (-not $presentationFramework -and (Test-Path (Join-Path $candidate 'PresentationFramework.dll'))) {
    $presentationFramework = (Join-Path $candidate 'PresentationFramework.dll')
  }
  if (-not $presentationCore -and (Test-Path (Join-Path $candidate 'PresentationCore.dll'))) {
    $presentationCore = (Join-Path $candidate 'PresentationCore.dll')
  }
  if (-not $windowsBase -and (Test-Path (Join-Path $candidate 'WindowsBase.dll'))) {
    $windowsBase = (Join-Path $candidate 'WindowsBase.dll')
  }
}

if (-not $presentationFramework -or -not $presentationCore -or -not $windowsBase) {
  throw 'Unable to locate required WPF assemblies with absolute paths.'
}

$cscArgs = @(
  '/nologo',
  '/target:winexe',
  '/langversion:latest',
  '/out:' + $exe,
  '/r:' + $presentationFramework,
  '/r:' + $presentationCore,
  '/r:' + $windowsBase,
  '/r:System.Net.Http.dll',
  '/r:System.Web.Extensions.dll',
  $source
)

& $csc @cscArgs
if ($LASTEXITCODE -ne 0) {
  throw "Compilation failed with exit code $LASTEXITCODE"
}

Write-Host "Built: $exe"
