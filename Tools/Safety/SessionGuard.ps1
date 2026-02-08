[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("start", "checkpoint", "finish", "status", "list-checkpoints")]
    [string]$Action,

    [string]$Task = "",
    [string]$Plan = "",
    [string]$Result = "",

    [ValidateSet("resolved", "not_resolved")]
    [string]$Status = "not_resolved",

    [string[]]$Paths = @("Assets/Scripts", "Assets/Scenes", "ProjectSettings")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Ensure-Dir {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function Get-Root {
    $root = Join-Path $PSScriptRoot "..\.."
    return (Resolve-Path $root).Path
}

function Get-StateFile {
    param([string]$DocsPath)
    return (Join-Path $DocsPath "CurrentSession.json")
}

function Get-HistoryFile {
    param([string]$DocsPath)
    return (Join-Path $DocsPath "SessionHistory.csv")
}

function Load-State {
    param([string]$StateFile)
    if (-not (Test-Path -LiteralPath $StateFile)) {
        return $null
    }
    return (Get-Content -LiteralPath $StateFile -Raw | ConvertFrom-Json)
}

function Save-State {
    param(
        [string]$StateFile,
        [pscustomobject]$State
    )
    $State | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $StateFile -Encoding UTF8
}

function Write-History {
    param(
        [string]$HistoryFile,
        [string]$SessionId,
        [string]$Event,
        [string]$Info
    )

    if (-not (Test-Path -LiteralPath $HistoryFile)) {
        "timestamp,session_id,event,info" | Set-Content -LiteralPath $HistoryFile -Encoding UTF8
    }

    $ts = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    $line = ('"{0}","{1}","{2}","{3}"' -f
        $ts,
        ($SessionId -replace '"', '""'),
        ($Event -replace '"', '""'),
        ($Info -replace '"', '""')
    )
    Add-Content -LiteralPath $HistoryFile -Value $line -Encoding UTF8
}

function Add-Section {
    param(
        [string]$SessionFile,
        [string[]]$Lines
    )
    Add-Content -LiteralPath $SessionFile -Value "" -Encoding UTF8
    foreach ($line in $Lines) {
        Add-Content -LiteralPath $SessionFile -Value $line -Encoding UTF8
    }
}

function New-Checkpoint {
    param(
        [string]$Root,
        [string]$CheckpointPath,
        [string[]]$CopyPaths
    )

    Ensure-Dir -Path $CheckpointPath
    $missing = New-Object System.Collections.Generic.List[string]

    foreach ($relative in $CopyPaths) {
        if ([string]::IsNullOrWhiteSpace($relative)) { continue }
        $source = Join-Path $Root $relative
        if (-not (Test-Path -LiteralPath $source)) {
            $missing.Add($relative)
            continue
        }

        $target = Join-Path $CheckpointPath $relative
        $targetParent = Split-Path -Parent $target
        if (-not [string]::IsNullOrWhiteSpace($targetParent)) {
            Ensure-Dir -Path $targetParent
        }

        $item = Get-Item -LiteralPath $source
        if ($item.PSIsContainer) {
            Copy-Item -LiteralPath $source -Destination $target -Recurse -Force
        }
        else {
            Copy-Item -LiteralPath $source -Destination $target -Force
        }
    }

    if ($missing.Count -gt 0) {
        $missingFile = Join-Path $CheckpointPath "_missing_paths.txt"
        $missing | Set-Content -LiteralPath $missingFile -Encoding UTF8
    }
}

$root = Get-Root
$docs = Join-Path $root "Docs"
$sessionLogs = Join-Path $docs "SessionLogs"
$checkpoints = Join-Path $docs "Checkpoints"
Ensure-Dir -Path $docs
Ensure-Dir -Path $sessionLogs
Ensure-Dir -Path $checkpoints

$stateFile = Get-StateFile -DocsPath $docs
$historyFile = Get-HistoryFile -DocsPath $docs

switch ($Action) {
    "start" {
        $id = (Get-Date).ToString("yyyyMMdd-HHmmss")
        $started = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
        $taskText = if ([string]::IsNullOrWhiteSpace($Task)) { "not informed" } else { $Task }
        $planText = if ([string]::IsNullOrWhiteSpace($Plan)) { "not informed" } else { $Plan }

        $sessionFile = Join-Path $sessionLogs ("Session-{0}.md" -f $id)
        @(
            "# Session {0}" -f $id
            ""
            "- start: {0}" -f $started
            "- task: {0}" -f $taskText
            "- initial_plan: {0}" -f $planText
            "- status: in_progress"
            ""
            "## Progress"
            "- session created"
        ) | Set-Content -LiteralPath $sessionFile -Encoding UTF8

        $checkpointPath = Join-Path $checkpoints ("{0}-start" -f $id)
        New-Checkpoint -Root $root -CheckpointPath $checkpointPath -CopyPaths $Paths

        $state = [pscustomobject]@{
            session_id = $id
            started_at = $started
            session_file = $sessionFile
        }
        Save-State -StateFile $stateFile -State $state

        Write-History -HistoryFile $historyFile -SessionId $id -Event "start" -Info $taskText
        Write-History -HistoryFile $historyFile -SessionId $id -Event "checkpoint" -Info ("{0}-start" -f $id)

        Write-Output ("session_id={0}" -f $id)
        Write-Output ("session_file={0}" -f $sessionFile)
        Write-Output ("checkpoint={0}" -f $checkpointPath)
    }

    "checkpoint" {
        $state = Load-State -StateFile $stateFile
        if ($null -eq $state) {
            throw "No active session. Run with -Action start first."
        }

        $stamp = (Get-Date).ToString("yyyyMMdd-HHmmss")
        $cpName = "{0}-{1}" -f $state.session_id, $stamp
        $checkpointPath = Join-Path $checkpoints $cpName
        New-Checkpoint -Root $root -CheckpointPath $checkpointPath -CopyPaths $Paths

        Add-Section -SessionFile $state.session_file -Lines @(
            "## Checkpoint {0}" -f $stamp
            "- paths: {0}" -f ($Paths -join ", ")
        )
        Write-History -HistoryFile $historyFile -SessionId $state.session_id -Event "checkpoint" -Info $cpName
        Write-Output ("checkpoint={0}" -f $checkpointPath)
    }

    "finish" {
        $state = Load-State -StateFile $stateFile
        if ($null -eq $state) {
            throw "No active session. Run with -Action start first."
        }

        $ended = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
        $resultText = if ([string]::IsNullOrWhiteSpace($Result)) { "not informed" } else { $Result }
        $finalState = if ($Status -eq "resolved") { "resolved" } else { "not_resolved" }

        Add-Section -SessionFile $state.session_file -Lines @(
            "## Finish"
            "- end: {0}" -f $ended
            "- status: {0}" -f $finalState
            "- result: {0}" -f $resultText
        )

        Write-History -HistoryFile $historyFile -SessionId $state.session_id -Event "finish" -Info ("{0}: {1}" -f $finalState, $resultText)
        Remove-Item -LiteralPath $stateFile -Force

        Write-Output ("session_id={0}" -f $state.session_id)
        Write-Output ("status={0}" -f $finalState)
    }

    "status" {
        $state = Load-State -StateFile $stateFile
        if ($null -eq $state) {
            Write-Output "no_active_session"
        }
        else {
            Write-Output ("session_id={0}" -f $state.session_id)
            Write-Output ("started_at={0}" -f $state.started_at)
            Write-Output ("session_file={0}" -f $state.session_file)
        }
    }

    "list-checkpoints" {
        Get-ChildItem -LiteralPath $checkpoints -Directory |
            Sort-Object LastWriteTime -Descending |
            Select-Object Name, LastWriteTime, FullName
    }
}
