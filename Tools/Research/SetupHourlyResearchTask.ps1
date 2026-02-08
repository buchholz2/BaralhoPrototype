param(
    [string]$TaskName = 'BaralhoPrototypeHourlyResearch',
    [int]$DurationMinutes = 15
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$researchScript = Join-Path $repoRoot 'Tools\Research\HourlyResearch.ps1'
if (-not (Test-Path $researchScript)) {
    throw "Research script not found: $researchScript"
}

$startTime = (Get-Date).AddMinutes(1).ToString('HH:mm')
$taskNameArg = $TaskName.Replace('"', '')

$launcherDir = 'C:\BaralhoResearch'
if (-not (Test-Path $launcherDir)) {
    New-Item -ItemType Directory -Path $launcherDir -Force | Out-Null
}
$launcherCmd = Join-Path $launcherDir 'RunHourlyResearch.cmd'
$launcherContent = @(
    '@echo off',
    "cd /d ""$repoRoot""",
    "powershell.exe -NoProfile -ExecutionPolicy Bypass -File ""$researchScript"" -DurationMinutes $DurationMinutes"
)
$launcherContent | Set-Content -Encoding ASCII $launcherCmd

schtasks /Create /TN $taskNameArg /SC HOURLY /MO 1 /ST $startTime /TR $launcherCmd /F | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Failed creating task. schtasks exit code: $LASTEXITCODE"
}

Write-Host "Scheduled task created: $taskNameArg"
Write-Host "Starts at: $startTime and runs every hour"
Write-Host "Command: $launcherCmd"
