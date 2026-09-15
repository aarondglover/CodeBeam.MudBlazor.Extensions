[CmdletBinding()]
param(
    [string]$BaselineRef = "d0ae5981e80c4a493b29ccff7a7cb80b22fce0d5",
    [string]$FixedRef = "76b806ec54cdb43c9b5171bbbd10d4759014583f",
    [string]$ResultsDirectory,
    [string]$BenchmarkFilter = "*",
    [switch]$Quick,
    [switch]$ProbeOnly,
    [switch]$SkipProbe
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($ProbeOnly -and $SkipProbe) {
    throw "ProbeOnly and SkipProbe cannot be used together."
}

$repoRoot = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0) {
    throw "Unable to locate the repository root."
}

if ([string]::IsNullOrWhiteSpace($ResultsDirectory)) {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $ResultsDirectory = Join-Path $repoRoot "BenchmarkDotNet.Artifacts/virtualized-list-selection-state/$timestamp"
}

$ResultsDirectory = [IO.Path]::GetFullPath($ResultsDirectory)
New-Item -ItemType Directory -Path $ResultsDirectory -Force | Out-Null

function Resolve-Commit([string]$ref) {
    $sha = (& git -C $repoRoot rev-parse "$ref^{commit}").Trim()
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to resolve '$ref'."
    }

    return $sha
}

function Invoke-Checked([string]$command, [string[]]$arguments) {
    & $command @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed ($LASTEXITCODE): $command $($arguments -join ' ')"
    }
}

$baselineSha = Resolve-Commit $BaselineRef
$fixedSha = Resolve-Commit $FixedRef
$benchmarksSource = Join-Path $repoRoot "benchmarks"
$workRoot = Join-Path ([IO.Path]::GetTempPath()) "CodeBeamMudExtensionsBenchmarks-$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $workRoot -Force | Out-Null

$previousCi = $env:CI
$previousVariant = $env:BENCHMARK_VARIANT
$worktrees = [System.Collections.Generic.List[string]]::new()

function Invoke-Variant([string]$name, [string]$sha) {
    $worktree = Join-Path $workRoot $name
    $worktrees.Add($worktree)

    Write-Host "`n=== $name ($sha) ===" -ForegroundColor Cyan
    Invoke-Checked git @("-C", $repoRoot, "worktree", "add", "--detach", $worktree, $sha)

    $targetBenchmarks = Join-Path $worktree "benchmarks"
    New-Item -ItemType Directory -Path $targetBenchmarks -Force | Out-Null
    Copy-Item -Path (Join-Path $benchmarksSource "*") -Destination $targetBenchmarks -Recurse -Force

    $variantResults = Join-Path $ResultsDirectory $name
    New-Item -ItemType Directory -Path $variantResults -Force | Out-Null

    $project = Join-Path $targetBenchmarks "CodeBeam.MudBlazor.Extensions.Benchmarks/CodeBeam.MudBlazor.Extensions.Benchmarks.csproj"
    $probeOutput = Join-Path $variantResults "scale-probe.csv"
    $bdnArtifacts = Join-Path $variantResults "BenchmarkDotNet.Artifacts"

    $env:CI = "true"
    $env:BENCHMARK_VARIANT = $name

    (& dotnet --info) | Out-File -FilePath (Join-Path $variantResults "dotnet-info.txt") -Encoding utf8
    (& git -C $worktree show -s --format="%H%n%ad%n%s" --date=iso-strict HEAD) |
        Out-File -FilePath (Join-Path $variantResults "source.txt") -Encoding utf8

    if (-not $SkipProbe) {
        Invoke-Checked dotnet @(
            "run", "--project", $project, "--configuration", "Release", "--",
            "probe", "--output", $probeOutput
        )
    }

    if ($ProbeOnly) {
        return
    }

    $benchmarkArguments = [System.Collections.Generic.List[string]]::new()
    @(
        "run", "--project", $project, "--configuration", "Release", "--",
        "--filter", $BenchmarkFilter,
        "--artifacts", $bdnArtifacts,
        "--exporters", "GitHub", "CSV", "JSON",
        "--allStats",
        "--join"
    ) | ForEach-Object { $benchmarkArguments.Add($_) }

    if ($Quick) {
        $benchmarkArguments.Add("--job")
        $benchmarkArguments.Add("short")
    }

    Invoke-Checked dotnet $benchmarkArguments.ToArray()
}

try {
    @(
        "Baseline ref: $BaselineRef",
        "Baseline SHA: $baselineSha",
        "Fixed ref: $FixedRef",
        "Fixed SHA: $fixedSha",
        "Started: $([DateTimeOffset]::Now.ToString('O'))",
        "Benchmark filter: $BenchmarkFilter",
        "Quick: $Quick",
        "Probe only: $ProbeOnly",
        "Skip probe: $SkipProbe"
    ) | Out-File -FilePath (Join-Path $ResultsDirectory "run-info.txt") -Encoding utf8

    Invoke-Variant "baseline" $baselineSha
    Invoke-Variant "fixed" $fixedSha

    Write-Host "`nBenchmark results: $ResultsDirectory" -ForegroundColor Green
}
finally {
    $env:CI = $previousCi
    $env:BENCHMARK_VARIANT = $previousVariant

    foreach ($worktree in $worktrees) {
        if (Test-Path $worktree) {
            & git -C $repoRoot worktree remove --force $worktree | Out-Null
        }
    }

    & git -C $repoRoot worktree prune | Out-Null
    if (Test-Path $workRoot) {
        Remove-Item -Path $workRoot -Recurse -Force
    }
}
