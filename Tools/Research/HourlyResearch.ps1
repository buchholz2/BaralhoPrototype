param(
    [int]$DurationMinutes = 15,
    [string]$OutputRoot = "$(Join-Path $PSScriptRoot '..\..\Temp\research')"
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$deadline = (Get-Date).AddMinutes([Math]::Max(1, $DurationMinutes))
if (-not (Test-Path $OutputRoot)) {
    New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
}
$outputDir = (Resolve-Path $OutputRoot).Path

$cachePath = Join-Path $outputDir 'seen_repos.json'
$latestJsonPath = Join-Path $outputDir 'latest_candidates.json'
$logPath = Join-Path $outputDir 'hourly_research_log.md'
$queuePath = Join-Path $outputDir 'implementation_queue.md'

$seen = @{}
if (Test-Path $cachePath) {
    try {
        $items = Get-Content $cachePath -Raw | ConvertFrom-Json
        foreach ($i in $items) {
            $seen[$i] = $true
        }
    } catch {
        $seen = @{}
    }
}

$queries = @(
    'unity card game framework language:C# stars:>10',
    'unity tcg ccg language:C# stars:>10',
    'unity card drag drop language:C# stars:>5',
    'unity netcode multiplayer card game language:C# stars:>5',
    'unity deckbuilder language:C# stars:>5',
    'unity addressables ui sprites language:C# stars:>10',
    'unity object pooling ui language:C# stars:>10',
    'unity input system drag ui language:C# stars:>10',
    'unity shader card highlight stars:>5',
    'unity turn based card game language:C# stars:>5'
)

function Is-RelevantRepo {
    param($repo)

    $nameText = if ($null -ne $repo.name) { [string]$repo.name } else { '' }
    $descText = if ($null -ne $repo.description) { [string]$repo.description } else { '' }
    $text = ($nameText + ' ' + $descText).ToLowerInvariant()

    $must = @('card','deck','tcg','ccg')
    foreach ($k in $must) {
        if ($text.Contains($k)) { return $true }
    }
    return $false
}

function Score-Repo {
    param($repo)

    $score = 0.0
    $score += [Math]::Min(45, [Math]::Log10([Math]::Max(1, [double]$repo.stargazers_count)) * 12)

    $nameText = if ($null -ne $repo.name) { [string]$repo.name } else { '' }
    $descText = if ($null -ne $repo.description) { [string]$repo.description } else { '' }
    $text = ($nameText + ' ' + $descText).ToLowerInvariant()
    $keywords = @(
        'multiplayer','netcode','performance','pool','optimization',
        'addressables','mobile','ui','drag','input','shader','card','deck'
    )

    foreach ($k in $keywords) {
        if ($text.Contains($k)) { $score += 3 }
    }

    if ($repo.archived) { $score -= 20 }
    if ($repo.fork) { $score -= 8 }

    return [Math]::Round($score, 2)
}

function Search-GitHub {
    param(
        [string]$Query,
        [int]$PerPage = 8
    )

    $uri = 'https://api.github.com/search/repositories?q=' + [uri]::EscapeDataString($Query) + '&sort=stars&order=desc&per_page=' + $PerPage + '&page=1'
    $headers = @{ 'User-Agent' = 'BaralhoPrototype-Research-Bot' }
    try {
        return Invoke-RestMethod -Method Get -Uri $uri -Headers $headers -TimeoutSec 25
    } catch {
        return $null
    }
}

$candidates = New-Object System.Collections.Generic.List[object]
$checkedQueries = 0

foreach ($q in $queries) {
    if ((Get-Date) -ge $deadline) { break }
    $checkedQueries++

    $result = Search-GitHub -Query $q
    if (-not $result -or -not $result.items) { continue }

    foreach ($repo in $result.items) {
        if ((Get-Date) -ge $deadline) { break }
        if (-not $repo.full_name) { continue }
        if ($seen.ContainsKey($repo.full_name)) { continue }
        if (-not (Is-RelevantRepo -repo $repo)) { continue }
        if ([int]$repo.stargazers_count -lt 5) { continue }

        $entry = [pscustomobject]@{
            full_name   = $repo.full_name
            html_url    = $repo.html_url
            description = $repo.description
            stars       = [int]$repo.stargazers_count
            updated_at  = $repo.updated_at
            language    = $repo.language
            license     = if ($null -ne $repo.license -and $null -ne $repo.license.spdx_id) { $repo.license.spdx_id } else { 'N/A' }
            query       = $q
            score       = Score-Repo -repo $repo
        }
        $candidates.Add($entry)
        $seen[$repo.full_name] = $true
    }
}

$ranked = $candidates | Sort-Object -Property @{ Expression = 'score'; Descending = $true }, @{ Expression = 'stars'; Descending = $true }
$top = $ranked | Select-Object -First 12

$ranked | ConvertTo-Json -Depth 4 | Out-File -Encoding utf8 $latestJsonPath

$timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("## Run $timestamp")
$lines.Add("- Duration requested: ${DurationMinutes}min")
$lines.Add("- Queries checked: $checkedQueries")
$lines.Add("- New candidates: $($ranked.Count)")

if ($top.Count -eq 0) {
    $lines.Add('- No new candidates found this run.')
} else {
    foreach ($c in $top) {
        $desc = if ([string]::IsNullOrWhiteSpace($c.description)) { 'No description' } else { $c.description.Replace("`r", ' ').Replace("`n", ' ') }
        $row = "- [{0}]({1}) | score={2} | stars={3} | lang={4} | from=""{5}""" -f $c.full_name, $c.html_url, $c.score, $c.stars, $c.language, $c.query
        $lines.Add($row)
        $lines.Add("  - $desc")
    }
}
$lines.Add('')

if (-not (Test-Path $logPath)) {
    "# Hourly Research Log`n" | Out-File -Encoding utf8 $logPath
}
$lines | Add-Content -Encoding utf8 $logPath

$queue = New-Object System.Collections.Generic.List[string]
$queue.Add("# Implementation Queue")
$queue.Add("")
$queue.Add("Last update: $timestamp")
$queue.Add("")
if ($top.Count -eq 0) {
    $queue.Add("- No high-confidence candidates in this run.")
} else {
    $pick = $top | Select-Object -First 5
    foreach ($c in $pick) {
        $queue.Add("- $($c.full_name) ($($c.html_url))")
        $queue.Add("  - Why: score=$($c.score), stars=$($c.stars), lang=$($c.language)")
        $queue.Add("  - Action: inspect architecture and extract reusable patterns for this project.")
    }
}
$queue | Set-Content -Encoding utf8 $queuePath

$seen.Keys | Sort-Object | ConvertTo-Json | Out-File -Encoding utf8 $cachePath

Write-Host "Research complete. Added $($ranked.Count) candidates."
Write-Host "Log: $logPath"
Write-Host "Latest JSON: $latestJsonPath"
Write-Host "Queue: $queuePath"
