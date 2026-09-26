# Virtualized selection benchmarks

This benchmark harness measures the `MudSelectExtended` / `MudListExtended` virtualization scenarios behind CodeBeamOrg/CodeBeam.MudBlazor.Extensions issues #493 and #608 and the initialization regression fixed by #583.

The project deliberately lives outside the unit-test project. NUnit + bUnit tests remain the correctness/regression suite; BenchmarkDotNet is used only for repeatable performance measurements.

## What is measured

`CodeBeam.MudBlazor.Extensions.Benchmarks` uses bUnit to execute the real Blazor component lifecycle and BenchmarkDotNet to measure elapsed time, managed allocations, and Gen 0/1/2 collection rates.

The benchmark matrix covers:

- direct `MudListExtended` initial renders with 10, 100, 1,000 and 4,000 items, with virtualization both off and on;
- initial `MudSelectExtended` render with 10, 100, 1,000 and 4,000 items;
- `Virtualize=false` and `Virtualize=true` select renders;
- 1, 5 and 20 simultaneous virtualized selects with 4,000 items each;
- 1, 2, 10, 30 and 100 selected values from a 4,000-item collection.

Each benchmark invocation creates and disposes its own bUnit context. This keeps iterations isolated and makes allocation measurements reproducible. The constant bUnit setup cost is intentionally present in both baseline and fixed runs; the dataset-size scaling is the primary comparison.

A separate `probe` mode records Blazor-specific shape metrics which BenchmarkDotNet does not understand natively:

- hidden shadow-list DOM item count;
- materialized `MudSelectItemExtended` component count;
- bUnit render count;
- generated markup length;
- one-shot render time and total allocated bytes;
- an explicitly labelled **approximate** retained-memory delta after a forced full GC while the rendered component remains alive.

The probe is diagnostic evidence, not a substitute for BenchmarkDotNet statistics.


## Three-stage evidence

The runner can optionally measure an intermediate Git graph point as well as the normal baseline and candidate:

1. **Upstream baseline** — the exact upstream `dev` commit used as the reference.
2. **Stage 1 / first fix** — `76b806ec54cdb43c9b5171bbbd10d4759014583f`, the historical implementation point that the benchmark harness originally treated as its fixed side.
3. **Current candidate / HEAD** — the current implementation being evaluated.

Run the three-stage structural probe with:

```powershell
./benchmarks/run-virtualization-benchmarks.ps1 \
    -ProbeOnly \
    -BaselineRef 7b5faf7ebb7666558d13c447313e9b09c92a110d \
    -Stage1Ref 76b806ec54cdb43c9b5171bbbd10d4759014583f \
    -FixedRef HEAD
```

When `Stage1Ref` is supplied, the runner writes `comparison-summary.md` alongside the raw CSV files. This is intended to document how the implementation evolved; it is **not** the merge/regression baseline.

The authoritative final comparison remains **upstream `dev` -> current HEAD**. CI therefore includes Stage 1 in the structural probe, but the focused BenchmarkDotNet regression job compares only upstream baseline versus HEAD.

## Run baseline and fixed code on the same workstation

From the benchmark branch:

```powershell
./benchmarks/run-virtualization-benchmarks.ps1
```

The script creates temporary detached Git worktrees for the requested graph points, copies the exact same benchmark harness into each, and runs the variants sequentially on the same machine. It records every resolved SHA and `dotnet --info` alongside the output. `Stage1Ref` is optional; without it the runner behaves as the normal two-way baseline/candidate comparison.

For a faster smoke run:

```powershell
./benchmarks/run-virtualization-benchmarks.ps1 -Quick
```

`-Quick` asks BenchmarkDotNet to use its short job. Do not use quick-run numbers as final PR evidence when a normal run is available.

Results are written below `BenchmarkDotNet.Artifacts/virtualized-list-selection-state/<timestamp>/` unless `-ResultsDirectory` is supplied.

## Choosing what to run

The benchmark harness is intentionally opt-in. It should complement, not replace, the normal correctness suite.

Recommended policy:

| Context | Recommended command | Purpose |
| --- | --- | --- |
| Every PR/build | normal unit/regression tests | Required correctness gate |
| Performance-sensitive PR, manual CI request, or `perf-benchmark` label | `./benchmarks/run-virtualization-benchmarks.ps1 -ProbeOnly` | Fast structural/scaling check |
| Manual CI or developer smoke test | `./benchmarks/run-virtualization-benchmarks.ps1 -SkipProbe -Quick -BenchmarkFilter "*SelectInitialRenderBenchmarks*"` | Short BenchmarkDotNet comparison |
| Final performance evidence | `./benchmarks/run-virtualization-benchmarks.ps1 -BaselineRef <baseline-sha> -FixedRef <candidate-sha>` | Full same-machine before/after run |

Do not make elapsed-time thresholds from shared hosted runners a required merge gate. Runner hardware and contention vary. Component counts, shadow-item counts, allocations and scaling shape are more useful CI signals; final timing evidence should come from repeated runs on the same workstation or a stable dedicated runner.

### Developer workstation

Requirements are Git, PowerShell 7+ and the .NET 10 SDK. Run from the repository root.

Fast structural probe:

```powershell
./benchmarks/run-virtualization-benchmarks.ps1 -ProbeOnly
```

Focused smoke benchmark:

```powershell
./benchmarks/run-virtualization-benchmarks.ps1 `
    -SkipProbe `
    -Quick `
    -BenchmarkFilter "*SelectInitialRenderBenchmarks*"
```

Full before/after evidence against an explicit baseline:

```powershell
./benchmarks/run-virtualization-benchmarks.ps1 `
    -BaselineRef <baseline-sha> `
    -FixedRef HEAD
```

`FixedRef` defaults to `HEAD`, so a developer can simply check out the candidate branch and run the script. For published evidence, pass both refs explicitly and retain the generated `run-info.txt` and per-variant `source.txt` files.

### Conditional CI example

A downstream GitHub Actions pipeline can keep benchmarks disabled by default and enable them for a manual dispatch or a PR label:

```yaml
- name: Virtualized selection performance probe
  if: >
    github.event_name == 'workflow_dispatch' ||
    contains(github.event.pull_request.labels.*.name, 'perf-benchmark')
  shell: pwsh
  run: >
    ./benchmarks/run-virtualization-benchmarks.ps1
    -ProbeOnly
    -BaselineRef "${{ github.event.pull_request.base.sha }}"
    -FixedRef "${{ github.sha }}"
    -ResultsDirectory "${{ github.workspace }}/benchmark-results"

- name: Upload benchmark evidence
  if: always()
  uses: actions/upload-artifact@v4
  with:
    name: virtualization-benchmark-results
    path: benchmark-results
```

The same pattern works in other CI systems: put the script invocation behind an explicit pipeline variable/parameter such as `RUN_VIRTUALIZATION_BENCHMARKS=true`, a performance label, a changed-path rule, or a manually selected stage. The script itself stays CI-agnostic.
## Interpretation

The key expected scaling characteristic is that a virtualized select should not instantiate an item component for every member of `ItemCollection` merely to retain selected-value presentation state. Large changes in allocated bytes, retained component count and GC pressure are therefore meaningful. Small absolute timing differences for 10- or 100-item collections should be treated cautiously.

The direct-list cases are intentionally included as a control: the `MudListExtended` selection-state correction should preserve reasonable list-render scaling rather than trading the select improvement for a list regression.

GitHub-hosted runners are useful for build validation and preliminary measurements, but final before/after numbers should preferably come from repeated runs on the same workstation because hosted-runner hardware and contention can vary.

## Reference baseline used by this PR

For the final PR1/PR2 comparison in this branch, the baseline is pinned to upstream `CodeBeamOrg/CodeBeam.MudBlazor.Extensions` `dev` at:

`7b5faf7ebb7666558d13c447313e9b09c92a110d`

This is the upstream `dev` commit immediately before the PR1 candidate branch diverges for this comparison. The fixed side defaults to the checked-out benchmark branch `HEAD`, which contains the exact PR1 implementation under test plus the benchmark harness.

For historical three-stage evidence, Stage 1 is pinned to:

`76b806ec54cdb43c9b5171bbbd10d4759014583f`

Stage 1 is supplemental evidence only. Performance regression conclusions and final BenchmarkDotNet numbers must compare the upstream baseline directly with the candidate `HEAD`.

The runner records all resolved SHAs in `run-info.txt` and each variant's `source.txt`, so any published result can be traced to an exact point in the Git graph.
## Reproducibility

Final before/after results must be produced from a benchmark branch containing the exact PR1 candidate being evaluated. Record the candidate commit SHA with the results so benchmark evidence cannot drift from the implementation under review.
