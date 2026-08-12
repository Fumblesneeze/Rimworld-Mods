## Context

The repository currently proves correctness but has no repeatable way to measure the cost of its Harmony patches, work scanners, jobs, ThingComps, map/world components, and ticks under load. The original design selected Dubs Performance Analyzer (DPA), but implementation had not started. Circinus is now installed locally as Workshop item `3773680130`, package ID `astryl.Circinus`, and is purpose-built around timestamped, comparable run documents rather than an interactive profiling window.

The inspected RimWorld 1.6 Circinus assembly is `Circinus.dll`, assembly version `1.0.0.0`, product build `1.0.0+456db42b8b74de26f4d330f55d89f0a668fcee71`, MVID `397fdc63-4b94-4548-8990-c319d9c32484`, 381952 bytes, and SHA-256 `C597B6FC56E77E817E38AE63826F71FD7AC8830CFDE1AD32C59C15FACCDBAFA1` as of 2026-08-11. Its local run-document schema is `1.15`. The package requires Harmony and declares `loadAfter` for `brrainz.harmony` and `astryl.ModernDevTools`.

For comparison, the inspected DPA 1.6 package is `Dubwise.DubsPerformanceAnalyzer.steam`; its `PerformanceAnalyzer.dll` has assembly version `1.0.0.0`, product build `1.0.0+b6d6595b7457219357c25e8cef1082652454b7f7`, MVID `894501e4-b113-48af-9928-8da64293a38c`, 238080 bytes, and SHA-256 `A1758774137F5EFF19F6B98D8D29F46AE5F8247C3EADBF9F611D851568D471A8`. DPA's strength is interactive deep diagnosis: built-in category tabs, internal-call profiling, transpiled-IL investigation, and raw per-entry captures. Its weaknesses for this repository's canonical benchmark are UI-oriented lifecycle, private mutable result stores, per-entry binary saves, and no whole-run JSON carrying mod list, hardware, errors, frame/tick samples, and profiler policy together.

The product-level comparison also used Circinus' published [run metadata](https://circinus.sh/api/v1/meta), [attribution guidance](https://circinus.sh/authors), and [local-versus-shared data policy](https://circinus.sh/privacy), plus DPA's official [profiling and `Analyzer.xml` documentation](https://github.com/Dubwise56/Dubs-Performance-Analyzer). Exact automation contracts below come from read-only inspection of the installed assemblies, not from assuming either UI is a stable API.

Circinus is the better primary dependency for historical mod-impact measurement because it already records:

- one versioned JSON run with mod/package identity, hardware, environment, errors, markers, and sample context;
- roughly one-second TPS, target TPS, FPS, mean/max/P95 frame time, mean/max tick time, managed heap, and pawn-count samples;
- exact Harmony patch identity and owner-package attribution plus per-mod, per-patch, and explicitly armed method costs;
- patch-row calls/timed-calls and method-row calls/total/mean/max time in JSON, plus live per-method timed-call/adaptive state through `DevProfiler` before stop;
- temp-file local persistence, a partial flush every 30 samples, and explicit incomplete recovery when a durable partial document survives an interrupted process;
- native run comparison semantics that refuse incompatible or insufficient evidence.

Circinus' limitations remain material. Its native profiler records a duty cycle of 60 frames on followed by roughly 240 frames off, adaptively times only a fraction of calls above 64 calls per recorded frame with `DevProfiler.MaxSampleShift=8`, retains a 2000-recorded-frame ring, auto-sheds non-hand-armed methods below its native `0.02%` floor, trims stopped run documents above 3000 patch rows or 1000 method rows, and automatically stops at 7200 samples (about two hours). It identifies transpilers but its ordinary per-mod target arms runtime prefixes, postfixes, and finalizers rather than attributing the cost of transformed IL. Those are framework facts to retain in every report, not limits invented by this repository.

Circinus' attributed per-mod/per-patch time is gross execution cost, not a complete causal net-impact measurement. A skip-capable prefix can replace vanilla work, two targets may share one patch method, and another mod or a larger colony can induce work inside the measured code. The repository therefore keeps gross attribution, profiler overhead, paired product-present/product-absent deltas, and version-over-version regression as distinct claims.

This capability is owned by RimWorld Dev Gateway and depends on the exact-mod grouping, dynamic test loading, reset, process safety, and artifact collection defined by `add-rimworld-e2e-harness`. Circinus and benchmark fixtures are test-only dependencies. Gameplay mods never reference Circinus or Gateway.

## Goals / Non-Goals

**Goals:**

- Run a reproducible, substantial game workload under exact optional-mod combinations.
- Profile the repository mod's runtime Harmony patches and relevant hot runtime methods without manually operating a profiler UI.
- Retain raw Circinus measurements and normalized summaries with enough environment/build identity for historical comparison.
- Detect both direct mod-method regressions and broader TPS/frame/pathing/work-scan regressions.
- Let future repository mods contribute their own benchmark scenarios and method selectors.

**Non-Goals:**

- Claiming laboratory-grade microbenchmark precision from a live Unity game.
- Making Circinus a shipping dependency, copying its assembly into a repository mod, or uploading benchmark runs to `circinus.sh`.
- Automatically accepting a new baseline after a regression.
- Profiling every method in every assembly by default; instrumentation overhead would dominate the workload.
- Replacing DPA or an external profiler when a regression needs internal-call, transpiled-IL, allocation, native, GC, or rendering diagnosis beyond Circinus' saved contract.

## Decisions

### 1. Performance runs extend E2E groups but use a separate command and contract

Performance fixtures use the same complete ordered package-set declaration and one-process-per-group orchestration as E2E tests. A separate `RimWorldDevGateway.PerformanceTesting` contract marks the staging owner, measured subject package, benchmark scenarios, warm-up/sample phases, workload scale, method selectors, evidence lens, and comparison policy. `scripts/Invoke-RimWorldPerformanceTests.ps1` performs discovery, group launch, repeated samples, reports, and comparison. Normal E2E runs do not load Circinus or performance assemblies.

Keeping performance separate prevents slow, noisy benchmarks from becoming ordinary correctness tests and permits different repetition, timeout, and reporting semantics.

The host-safe declaration surface reuses existing repository ceilings: 1,024 discovered declarations, 64 packages in one exact matrix, 128 characters per ASCII package ID, 256 characters for ordinary identities, 1,024 characters for a method selector, 256 selectors, and 64 throughput checkpoints. These are repository validation bounds, not Circinus, Unity, or HTTP limits. They are public constants with exact/over tests. Package sequences are keyed by length-prefixed values before hashing so embedded delimiters cannot alias two exact groups.

Each comparison ID owns exactly three profiler-overhead lenses—instrumented, armed-disabled-wrapper, and fully disarmed—and their complete workload/selector/checkpoint metadata plus staging owner must match. A net product-present/product-absent comparison is narrower: both sides come from the same Gateway-owned, product-reference-free fixture implementation, while only the exact active product package differs. Product-owned feature fixtures report gross attribution and historical regression but do not acquire a synthetic net-control claim by pointing at an unrelated Gateway fixture.

### 2. Circinus is controlled through one exact reflection and schema adapter

The Gateway detects only active package ID `astryl.Circinus`, resolves assembly `Circinus`, records its full identity/MVID/hash and local schema version, and validates the exact declaring types, member kinds, accessibility, parameter types, and return types used by the inspected build before doing anything. The initially supported shape is:

- static property `Circinus.Session.RunRecorder.Current : RunRecorder`; instance methods `bool Start(string)`, `RunDocument Stop()`, and `void AddMarker(string,string)`; and instance properties `bool Recording`, `string ActiveId`, and `RunDocument Document`;
- static methods `bool Circinus.Profiling.Instrumenter.ArmMethod(MethodBase,string)`, `int Arm(ProfileTarget,bool)`, `bool DisarmMethod(MethodBase)`, `int Disarm(ProfileTarget)`, `bool IsPatched(MethodBase)`, and `void DisarmAll()`;
- public static volatile fields `bool Circinus.Profiling.ProfilerRegistry.Enabled` and `bool Recording`; static methods `DevProfiler Find(MethodBase)`, `List<DevProfiler> All()`, and `void ResetAll()`; and static properties `int CycleCount`, `int RecordedFrames`, `int SkippedFrames`, and `double DutyPct`;
- public readonly field `MethodBase Circinus.Profiling.DevProfiler.Method`, public field `bool HandArmed`, and instance properties `int SampleShift`, `long TotalCalls`, `long TotalTimedCalls`, `bool Empty`, and `int CyclesSeen`;
- static method `ProfileTarget Circinus.Profiling.TargetCatalogue.Get(string)` plus the resolved target's public instance fields `string Key` and `string Category`, and public getter-only instance property `int MethodCount`;
- static methods `List<PatchRef> Circinus.Identity.HarmonyIndex.ForPatchMethod(MethodBase)` and `MethodRef RefOf(MethodBase)`, plus public instance fields `string Circinus.Contract.PatchRef.Key` and `string Circinus.Contract.MethodRef.Key`;
- instance method `string Circinus.Contract.RunDocument.ToJson()` and public fields `string Id`, `int SchemaMajor`, and `int SchemaMinor`.

The adapter uses Circinus' own recorder, instrumenter, profiler, attribution, and JSON writer. It does not reimplement its profiler, call name-only lookalikes, or depend on the hosted service. The stopped `RunDocument.ToJson()` value and the Circinus-persisted isolated `Circinus/Runs/<id>.json` file are both retained and must agree on schema/run identity. Circinus writes a temporary file, deletes an existing destination, and moves the temporary file into place; the repository does not mislabel that sequence as atomic replacement.

Before launch, the host stages `Circinus.Bootstrap.CircinusSettings` with public fields `bool autoStartProfiler=false`, `bool autoArmProfiler=false`, `List<string> armedTargetKeys=[]`, `bool autoProfile=false`, `int autoProfileAsked=1`, `bool showWarmupWindow=false`, `int warmupSeconds=0`, `bool ingestEnabled=false`, `int consentVersion=2`, and `bool autoRecord=false`. Live access is guarded through public static field `Circinus.Bootstrap.CircinusMod.Settings : CircinusSettings`. The prompt versions are guarded against public constants `Circinus.UI.Window_Consent.DisclosureVersion=2` and `Circinus.UI.Window_AutoProfile.AskedVersion=1`. The live adapter revalidates those fields before arming. It never changes the user's normal Circinus settings and never enables the hosted ingest endpoint.

When Circinus updates and its guarded shape or schema major changes, the affected benchmark group fails with the observed package/assembly/schema identity and missing member. Core E2E and gameplay remain unaffected. There is no silent fallback to DPA or invented timing code.

### 3. Method selection combines runtime discovery with an explicit manifest

For each selected repository product assembly the benchmark adapter registers:

- every currently attached runtime Harmony prefix, postfix, and finalizer owned by that package's exact Harmony ID;
- every product-declared `Tick`, `TickRare`, `TickLong`, `CompTick`, `MapComponentTick`, `WorldComponentTick`, and `GameComponentTick` override;
- product-owned work-giver, job-giver, think-node, alert/report, inspect-string, and map-query methods selected by the benchmark manifest;
- explicitly named methods or types contributed by the benchmark fixture;
- selected Circinus curated system targets needed to explain overall tick, work, pathing, update, GUI, or rendering movement.

Every explicitly armed product method is marked `HandArmed` so Circinus' automatic below-floor shedding cannot silently remove it. Registration is deterministic and rejects unresolved, generic-open, duplicate, compiler-generated-only, or unsupported methods. The exact resolved method identities and selection reasons are retained.

Harmony ownership discovery is itself exact-shape guarded against the loaded `0Harmony` assembly: public static `Harmony.GetAllPatchedMethods() : IEnumerable<MethodBase>` and `Harmony.GetPatchInfo(MethodBase) : Patches`; the public instance `Patches.Prefixes`, `Postfixes`, `Finalizers`, and `Transpilers` `ReadOnlyCollection<Patch>` fields; public `Patch.owner : string`; and public getter `Patch.PatchMethod : MethodInfo`. The global provider walk aborts above 65,536 patched targets or 65,536 total patch attachments. Those deliberately high values are repository protection against a corrupt/nonterminating provider, not Harmony or Circinus limits; exact/over host tests pin them, while Circinus' much smaller 1000-method/3000-patch emitted-row limits remain the actual accepted-run ceiling.

Manifest `Method` selectors use exact `Full.Type::Method` syntax and must add the full comma-separated parameter type signature when overloads exist; `Type` selects its supported declared methods, `TickOverrides` names one exact loaded product assembly and selects only real overrides of the seven declared tick/component names, and `WholeAssembly` remains explicit opt-in. Cross-selector duplicates collapse to one exact module/MVID/token/signature identity while retaining every sorted category, reason, and patched target. Transpilers retain their patch method and every target with the runtime-timing refusal, but never enter the hand-armed set.

Every attached transpiler is still discovered and retained with its target, owner, and unsupported-direct-timing reason. A transpiler method normally executes while patching rather than during the workload, so timing that method during play is meaningless. When transformed-IL attribution is necessary, the investigation uses a separate DPA diagnostic run; that run is never mixed into a Circinus baseline or canonical group.

Profiling an entire product assembly remains an explicit diagnostic opt-in because it can add excessive overhead.

### 4. Capture Circinus' direct and system-level measurements without erasing its sampling semantics

Raw output preserves Circinus' complete local JSON document: samples, patch rows, method rows, mod-cost rows, patch/method identities, calls, patch-row timed calls, total, mean per recorded profiler cycle, maximum recorded-cycle time, shared/skip-capable cost, ambiguous-target flags, profiler cycles, profiler-window milliseconds/ticks, duty percentage, sampled flag, auto-shed count, dropped-detail counters, markers, errors, and environment/context data. Circinus' ordinary `MethodStat` JSON does not contain `timedCalls` or adaptive-sampling state. Immediately before stopping or disarming, the Gateway therefore snapshots a run-owned sidecar for every exact armed `DevProfiler`: resolved `Method`, `HandArmed`, `SampleShift`, `TotalCalls`, `TotalTimedCalls`, `Empty`, and `CyclesSeen`.

After stop, each non-empty sidecar is correlated by Circinus' own exact `HarmonyIndex.ForPatchMethod(...).Key` or `HarmonyIndex.RefOf(...).Key` identity to either the `PatchStat` emitted for a Harmony patch method or the `MethodStat` emitted for an ordinary method. Circinus deliberately emits one patch row keyed by the first `PatchRef` for a patch method and sets that row's `AmbiguousTargets` flag when the method has multiple attachments; the sidecar therefore retains that same first key plus the native attachment count, and counts it as one—not multiple—against the 3000 emitted-patch-row ceiling. `ProfilerRegistry.All()` is used only to enumerate the exact profilers newly created by one curated-target arm; it is differenced against the pre-arm registry snapshot so unrelated profiler state is never claimed as run-owned. Every empty/uninvoked sidecar is retained with explicit `noRowReason=empty-or-uninvoked`, because Circinus intentionally omits it from `ProfilerRegistry.Active()` and therefore from the stopped rows. A non-empty sidecar with no matching row, an empty sidecar with a row, an ambiguous/duplicate match, or an unexplained trimmed required row invalidates the sample. The host derives normalized gross share of measured frame time only from each run's own positive denominator and records the formula. Disabled lenses with a zero profiler window retain their zero totals and calls but omit the unobserved share; all emitted metrics are finite. It does not relabel that share as causal net cost or mean-per-cycle as mean-per-call.

The run also retains selected Circinus curated target metrics and its TPS/FPS/frame/tick/heap/pawn time series. Gateway-owned checkpoints add elapsed game ticks and wall time, GC collection counts, managed-memory checkpoints, workload counts, throughput assertions, errors, and control-mode measurements. Circinus' native patch-row `timedCalls`, live `DevProfiler.TotalTimedCalls`, `ProfilerDutyPct`, and adaptive sampling policy are never rewritten to look like continuously timed raw calls.

Reports never compare unlike bases as if they were the same. Tick-category measurements remain distinct from rendered-frame/update measurements. Cross-machine share-of-frame data may be informative, but repository regression acceptance defaults to the same compatible hardware/runtime fingerprint.

Every product benchmark distinguishes four evidence lenses, each in a fresh process with the same declared workload and checkpoints:

- the Circinus-instrumented product workload has run-owned wrappers armed and `ProfilerRegistry.Enabled=true`, and reports gross attributable code cost plus system behavior;
- the armed-disabled control keeps the same wrappers attached but holds `ProfilerRegistry.Enabled=false` and `Recording=false`, isolating the wrapper-only cost that remains even while method timing is disabled;
- the fully disarmed control removes all run-owned Circinus wrappers before sampling while leaving the same recorder/sample collection active, providing an unwrapped recorder control;
- a separately declared product-absent control may report a net system delta only when both groups run the same semantically valid workload and checkpoints.

Instrumented minus armed-disabled estimates active method-timing/sampling overhead; armed-disabled minus fully disarmed estimates wrapper overhead; instrumented minus fully disarmed estimates total method-instrumentation overhead. Circinus recorder/sample collection remains common to all three, so none is mislabeled as analyzer-absent. These are noisy live-game deltas and remain separate from Circinus' gross code attribution and from any paired product-present/product-absent net system delta.

An Immersive Chefs-specific cooking/dishwashing workload is not falsely subtracted from vanilla when the absent product cannot perform those jobs. A smaller Gateway-owned neutral colony fixture, which has no product reference and declares Immersive Chefs only as the measured subject, is paired present/absent for load-time Harmony and passive runtime overhead. Its retained pawns use one native long-lived wait job during the measured window: ordinary map, pawn, animal, need, health, Harmony, and mod-component ticking remains active, while frame-scheduled autonomous think-tree choices cannot make nominal repetitions semantically different. Bounded start/terminal state rows identify pawns and food by stable fixture order, Def, position, and state rather than process-local generated IDs. The sample-start manifest additionally proves exact global Rand and unique-ID anchors; terminal comparison deliberately excludes those process-global counters because ordinary frame-rate-dependent ambient logging, tales, messages, rendering, and other non-workload systems may consume them without changing the retained neutral workload. Product-owned feature workloads remain present-only and measure feature-path version regressions.

### 5. The benchmark lifecycle has explicit calibrated phases

Each scenario declares a deterministic seed, exact workload version, warm-up ticks, sample ticks, game speed, and repetition count. Defaults are stored in the scenario source and may be changed only as a versioned workload change; the CLI may override them and records every override.

The controlled lifecycle is:

1. launch the exact ordered `brrainz.harmony`, `ludeon.rimworld`, `astryl.Circinus`, selected product/optional mods, and Gateway group in isolated savedata;
2. verify Circinus' exact shape/schema and local-only settings, then disarm/reset stale profiler state;
3. arrange the fixture while paused and warm it through ordinary frames/ticks with Circinus recording disabled;
4. resolve and arm the exact hand-armed method set, reset counters after warm-up, start one labeled Circinus run, add a sample-start marker, and enable the profiler;
5. advance ordinary game frames/ticks at the declared native speed while verifying throughput checkpoints;
6. disable profiling, add a sample-end marker, snapshot every run-owned live `DevProfiler` sidecar before stop/disarm, stop Circinus, retain both raw JSON forms before cleanup, and collect Gateway control metrics;
7. disarm only the run-owned methods, restore the captured isolated settings/control state, and shut down the exact process.

The runner validates that the declared sample did not silently overflow Circinus' 2000-recorded-frame profiler ring, 7200-sample run ceiling, or output-detail caps. A longer workload is split into explicit compatible windows/repetitions instead of accepting only the tail. It does not call `DoSingleTick`, directly invoke profiled methods to inflate counts, construct expected terminal metrics, alter Circinus' duty cycle, or bypass its native recorder.

### 6. Immersive Chefs gets one large versioned workload

The initial workload creates a deterministic multi-room colony with long corridors and separated storage/service areas; 36 human pawns distributed among cooking, assistance, cleaning/hauling, nursing/patient, and ordinary dining roles; 24 animals; multiple simultaneous simple/fine/lavish cooking bills; prep and four linked support station types; domestic and industrial dishwashers; microwaves; dirty/clean stockpiles; several patients and nurses; guest/service activity when the relevant package is active; and two world caravans with pawns, animals, meals, plates, and cutlery. Exact counts and Def identities live in a versioned manifest and the result verifies them before sampling.

Optional-mod variants add only behavior owned by their exact group: Processor/Dubs dishwashing, Gastronomy/Hospitality service, Common Sense cleanup, variety/provenance, VNPE paste, Expanded Materials, Royalty/Biotech, and an all-supported matrix. The base workload remains useful with only Harmony, Core, Circinus, Immersive Chefs, and Gateway.

### 7. Historical comparison is explicit and reviewable

Each report writes untouched Circinus JSON, normalized JSON, CSV, and a human Markdown summary. Every metric is labeled as gross attribution, instrumentation overhead, paired net system delta, or version regression. A baseline is an immutable reviewed report named by workload version, game version, exact package order, product commit/build hash, Circinus assembly/schema/profiling-policy identity, evidence lens, and relevant hardware/runtime fingerprint. The runner compares only compatible identities unless the caller explicitly requests a cross-version informational diff.

Thresholds are configured per metric selector as absolute and/or relative limits in tracked baseline policy; the runner does not invent one global percentage. Missing metrics, workload drift, new exceptions, incompatible units, profiler-policy drift, or Circinus refusal/incomplete status fail comparison. Creating or replacing a baseline requires an explicit command and writes a candidate for review; it never overwrites an accepted baseline silently.

### 8. DPA is a separate diagnostic fallback, not a second benchmark backend

DPA remains useful after a Circinus regression identifies a category, patch, or method. Its internal-method and transpiled-IL tools can answer a narrower diagnostic question that Circinus intentionally cannot. `scripts/Invoke-RimWorldPerformanceTests.ps1 -DiagnosticProfiler Dpa` therefore remains a bounded automated path rather than requiring manual operation of the DPA window.

The diagnostic command launches a fresh exact `brrainz.harmony`, `ludeon.rimworld`, `Dubwise.DubsPerformanceAnalyzer.steam`, selected product/optional mods, and Gateway process without Circinus. A separate reflection adapter first records and guards DPA's package/assembly/MVID/hash and the exact installed registration, start, stop, snapshot, and cleanup member shape. It registers only the requested exact method, nested/type, mod, built-in category, internal-call, or transpiled-IL selectors; follows DPA's own programmatic lifecycle; retains its raw entries and Gateway workload/checkpoint identity; then unregisters only run-owned state. Absent, inactive, or changed-shape DPA fails the diagnostic only.

The initial internal-call adapter pins DPA's exact public `Analyzer.BeginProfiling()`, `EndProfiling()`, and asynchronous `Cleanup()` methods; public profiling/cleaning/log properties; exact `ProfileController` profile and handle collection types; exact `Utility.PatchInternalMethod(MethodInfo,Category)` registration method and registration/cache fields; exact generated-entry registry; the `Dubwise.DubsProfiler` Harmony transpiler identity; and each profiler's method/type/key/label, 2000-slot time/hit rings, index, and empty flag. Registration runs with DPA's own threaded patching disabled, then proves one exact native transpiler and exact retained selector before sampling. The run also installs and proves DPA's exact native `Root_Play.Update` or `TickManager.DoSingleTick` prefix/postfix measurement-cycle pair; `BeginProfiling()` alone is insufficient because only those boundaries flush accumulated inner-call timings into DPA's rings. DPA's internal-call UI activates only the most recently selected outer entry, while each raw row retains the exact measured inner callee but no outer-owner type. The adapter therefore accepts exactly one outer selector per run, retains it as run-level context, and preserves its inner-callee rows without inventing multi-outer attribution. Additional outers require separate diagnostic runs. Cleanup claims DPA's public cleaning state before queueing its asynchronous native cleanup, waits for that generation to finish, proves every guarded native/Harmony store is clean, restores the prior threading setting, and retains a retryable owner whenever completion cannot be established.

The adapter stops discovery above 1,024 loaded assemblies and accepts exactly one outer selector per internal-call diagnostic; this selector count reflects DPA's native active-entry behavior rather than an arbitrary capacity limit. Raw capture stops above 4,096 profiler entries or 1,000,000 nonzero ring rows. Workload identity is limited to 256 UTF-8 bytes, with at most 128 checkpoints of 256 UTF-8 bytes each; DPA keys and labels are limited to 4,096 UTF-8 bytes. The remaining numbers are reviewed repository host-safety ceilings, not DPA, HTTP, artifact, or Unity limits. DPA's exact native ring remains separately identified as 2000 slots.

DPA output is labeled diagnostic, has no accepted-baseline command, cannot create or satisfy a canonical performance result, and is never normalized as though it used Circinus' schema or sampling policy. Circinus and DPA are not loaded together for benchmark acceptance or diagnosis because double instrumentation would change the workload and denominator.

## Risks / Trade-offs

- **Circinus internal API or schema changes** → Exact package/assembly/member/schema guard, actionable failure, and no effect on product gameplay.
- **Duty/adaptive sampling hides a rare regression** → Hand-arm every required method, retain the live pre-stop adaptive/timed-call sidecar, use repeated fresh processes and throughput checkpoints, and keep distinct armed-disabled and fully disarmed controls.
- **Instrumentation changes the result** → Register a bounded relevant method set, retain the exact set, compare fresh instrumented, armed-disabled, and fully disarmed-recorder processes, and report active-timing, wrapper, and total method-instrumentation overhead separately without calling the recorder control analyzer-absent.
- **Gross patch time is mistaken for net mod impact** → Label gross/shared/skip-capable/ambiguous costs, use a paired product-absent delta only for a semantically identical neutral workload, and never subtract unlike feature workloads.
- **Automatic Circinus UI, recording, or sharing contaminates automation** → Pre-stage and live-verify isolated settings; fail if a consent/auto-profile window or automatic run remains; never enable ingest.
- **A fast result is caused by stalled gameplay** → Assert actor/building/caravan/job throughput and absence of errors before accepting each sample.
- **Transpiled code cannot be attributed directly by Circinus** → Retain transpiler identities and targets; use the separately guarded automated DPA diagnostic only when the product actually has a relevant transpiler or internal-call question.
- **Optional mods change workload availability** → Exact group-specific fixture branches and expected workload counts; no inferred or downloaded-only activation.
- **Performance artifacts become enormous** → Store raw data under ignored artifacts with stable summaries tracked only when explicitly accepted as baselines; retain Circinus' native truncation counters and fail rather than silently accepting dropped required rows.

## Migration Plan

1. Keep the accepted E2E harness as the process/grouping substrate.
2. Add the performance contract and host discovery/report skeleton with a fake Circinus adapter for host tests.
3. Pin the inspected Circinus assembly/schema/member shape and add exact reflection-shape tests.
4. Implement isolated local-only settings, runtime arming, start/mark/live-sidecar/stop, raw JSON capture, three instrumentation-control modes, and one tiny calibration fixture.
5. Add the full versioned Immersive Chefs workload and base matrix.
6. Add optional-mod groups incrementally, the separate guarded automated DPA diagnostic, then baseline/diff commands and documentation.

Rollback is omission of the performance startup flag and removal of marker-owned staged performance bundles. Every process uses isolated savedata and an isolated mod list; the runner verifies the user's normal `ModsConfig.xml`, `Prefs.xml`, and Circinus settings hashes in `finally` and stops only the exact launched PID.

## Open Questions

The exact minimum sample duration and repetition count will be calibrated during the tiny live fixture rather than guessed in this design. The result must be long enough to exercise Circinus' native duty cycle and stable workload throughput, yet remain below its retained profiler-window caps. Any accepted defaults become part of the versioned workload contract.
