# Library-wide performance audit (PR 3B)

Status: initial source review; **no performance improvement is claimed without measurements**. This is an audit and a set of reproducible investigation targets, not a bulk refactor.

## Relationship to the other work

- PR 1: repair virtualized select selection state and limit its hidden item list.
- PR 2: establish and validate reproducible benchmark infrastructure. Adapt it to each component and confirm that timed benchmarks include settled rendering and expected component counts before publishing speedup claims.
- PR 3: independently investigate removal of the select's unnecessary hidden backing components, with dedicated regression tests and before/after benchmarks.
- **PR 3B (this audit):** find independently actionable, measurable performance issues elsewhere. This audit can be reviewed separately and rebased onto the fork's `dev` after the preceding PRs are merged. Do not fold implementation for unrelated components into this PR.
- PR 4: asynchronous/paged data virtualization; explicitly out of scope here.

## First-pass source-backed candidates (not confirmed performance defects)

### MudComboBox — first detailed investigation

Files: `src/CodeBeam.MudBlazor.Extensions/Components/ComboBox/MudComboBox.razor` and `.razor.cs`.

- The presenter filters the registered `Items` components with `Where(...).ToList()` for chips during rendering, then iterates the resulting list with `CollectionsMarshal.AsSpan`. The span does not avoid the preceding allocation or scan.
- `ItemContent` presentation repeatedly calls `Items.FirstOrDefault(...)`; the multiselection branch iterates registered items and checks membership with `SelectedValues.Contains(...)`.
- The `SelectedValues` setter compares `Count()` and `All(... set.Contains(...))`; `UpdateDataVisualiserTextAsync` iterates selected values and searches the item registry, including `Items.Select(...).Contains(...)` and `FirstOrDefault(...)`.
- Determine which instances must actually be registered for public `Items`, templates, selection, autocomplete, keyboard navigation, search, and select-all. Do not transplant the MudSelect optimisation without establishing ComboBox's rendering and API contracts.

Measurements: first/settled render, selection change, input/search, open/close, chip and ItemContent presentation; varying item and selection counts; allocated bytes, component count, elapsed time, GC. Compare the same scenarios before and after each change.

Safety tests: comparer, null and duplicate values, single/multiselection, editable vs non-editable, templates, chips/removal, external selection updates, filtering, select-all, keyboard navigation, and public item registry where applicable.

### MudJsonTreeView — verify setter and tree-render costs

Files: `src/CodeBeam.MudBlazor.Extensions/Components/JsonTreeView/MudJsonTreeView.razor.cs` and `MudJsonTreeViewNode.razor.cs`.

- Assigning `Json` calls `JsonNode.Parse`; assigning `Root` calls `ToJsonString()`. Both setters also invoke `OnJsonChanged` and `StateHasChanged`. Check repeated or equivalent parent parameter updates, callback semantics, and opportunities to avoid redundant parsing/serialization without changing observable behaviour.
- Nested node rendering recursively materialises component fragments. Inspect expanded/collapsed behaviour and whether deep/large JSON incurs avoidable allocations or eagerly renders descendants; do not presume it does until measured.

Measurements: repeated equivalent parameters, different payload sizes and nesting depths, initial render and expand/collapse, allocations and latency. Tests must preserve change notifications, null/error cases, ordering and displayed content.

### MudTextFieldExtended — lightweight inspection only

Files: `src/CodeBeam.MudBlazor.Extensions/Components/TextFieldExtended/MudTextFieldExtended.razor` and `.razor.cs`, with `MudDebouncedInputExtended` and `MudInputExtended` as dependencies.

- No select-style duplicate collection/component tree is apparent from this initial review. Test whether any meaningful expense exists on frequent input/validation/visualiser updates before proposing a code change.
- Preserve debounce timing, validation, masking, accessibility and focus semantics. Small micro-optimisations that do not yield reproducible user-facing benefits should not generate separate PRs.

## Audit method and exit criteria

1. Search the remaining library for comparable patterns (hidden duplicate trees, `foreach` creating all child components, component instances as data/selection state, render-path LINQ allocations, repeat work in parameter setters and callbacks). Catalogue candidates with exact source location and a plausible hot path; **do not assert that every occurrence is a defect**.
2. Establish a baseline with representative and stress workloads. Ensure identical inputs, runtime, configuration and benchmark harness; separate microbenchmarks from full rendered/async-settled measurements.
3. For each proposed fix, create a focused issue or branch/PR with one component or closely related concern, failing or protective regression tests, a baseline, the change, and comparable after-results. Document negative results too.
4. Prioritise cheap, behaviour-preserving wins based on measured impact and code complexity. Avoid global rewrites, speculative caches or extra memory held merely to improve one metric.
5. Record findings and links to follow-up PRs here. This audit is complete when its scope and outcome are documented; its separate optimisation slices need not all be merged.

No external/upstream PR or merge is authorized or implied by this audit.