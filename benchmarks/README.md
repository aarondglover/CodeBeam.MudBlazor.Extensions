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

## Run baseline and fixed code on the same workstation

From the benchmark branch:

```powershell
./benchmarks/run-virtualization-benchmarks.ps1
```

The script creates temporary detached Git worktrees for the baseline and fixed SHAs, copies the exact same benchmark harness into each, and runs both variants sequentially on the same machine. It records the resolved SHAs and `dotnet --info` alongside the output.

For a faster smoke run:

```powershell
./benchmarks/run-virtualization-benchmarks.ps1 -Quick
```

`-Quick` asks BenchmarkDotNet to use its short job. Do not use quick-run numbers as final PR evidence when a normal run is available.

Results are written below `BenchmarkDotNet.Artifacts/virtualized-list-selection-state/<timestamp>/` unless `-ResultsDirectory` is supplied.

## Interpretation

The key expected scaling characteristic is that a virtualized select should not instantiate an item component for every member of `ItemCollection` merely to retain selected-value presentation state. Large changes in allocated bytes, retained component count and GC pressure are therefore meaningful. Small absolute timing differences for 10- or 100-item collections should be treated cautiously.

The direct-list cases are intentionally included as a control: the `MudListExtended` selection-state correction should preserve reasonable list-render scaling rather than trading the select improvement for a list regression.

GitHub-hosted runners are useful for build validation and preliminary measurements, but final before/after numbers should preferably come from repeated runs on the same workstation because hosted-runner hardware and contention can vary.
