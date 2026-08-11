# Performance benchmarking

RimWorld Dev Gateway owns the repository's performance harness. Circinus is the canonical profiler for comparable local runs; Dubs Performance Analyzer is reserved for a separate, noncanonical diagnostic path. Neither profiler nor any performance-test assembly is a dependency of a distributable gameplay mod.

## Run the calibration

Circinus must be installed locally as package `astryl.Circinus`. The runner discovers marked fixture assemblies, validates their complete package order, builds and stages one test bundle, and launches one fresh minimized RimWorld process per benchmark and repetition:

```powershell
.\scripts\Invoke-RimWorldPerformanceTests.ps1 -DryRun -Output json
.\scripts\Invoke-RimWorldPerformanceTests.ps1 -Output json
```

Use `-BenchmarkId <exact-id>` for a focused canary and `-GroupId <exact-group-id>` for one exact matrix. `-WarmUpTicks`, `-SampleTicks`, and `-Repetitions` are recorded overrides; changing the source defaults requires a new workload version. Do not use performance runs as ordinary correctness tests.

The launcher uses isolated savedata and exact mod lists, forces Circinus into local-only/manual operation, disables music, enables background execution and developer mode, and starts the game minimized. It does not upload runs. It verifies exact process cleanup and restores the user's normal `ModsConfig.xml`, `Prefs.xml`, and Circinus settings.

## Evidence lenses and claims

Every comparable instrumentation family runs three fresh processes with identical fixture metadata and player-visible workload:

- `instrumented`: Circinus wrappers are armed and timing is enabled;
- `armed-disabled-wrapper`: the same wrappers remain attached but timing and recording are disabled;
- `fully-disarmed`: run-owned wrappers are removed while Circinus' recorder/sample collection remains present.

Compute the controls only within one compatible workload family:

- instrumented minus armed-disabled estimates active timing/sampling overhead;
- armed-disabled minus fully disarmed estimates wrapper overhead;
- instrumented minus fully disarmed estimates total method-instrumentation overhead.

The fully disarmed lens is not analyzer-absent. Circinus is still loaded and its recorder/sample collection is common to all three processes. These live-game differences are noisy control observations, not causal product costs or statistical rankings. Circinus method/patch/mod rows are gross attribution and must remain distinct from a semantically identical, explicitly declared product-present/product-absent net-system comparison.

Whole-assembly selection is a diagnostic opt-in. Prefer exact Harmony owners, methods, types, tick overrides, and curated targets because broad instrumentation can dominate the workload being measured.

## Calibration accepted on 2026-08-11

Run `20260811T202256042Z` used RimWorld `1.6.4871 rev591` and exact order `brrainz.harmony`, `ludeon.rimworld`, `astryl.circinus`, `fumblesneeze.rimworlddevgateway`. Gateway DLL SHA-256 was `B8200CA68C95D2116D39BD9D638B1B450EBE45037F96B4B33E4CDBA33D47E5FF`.

The `gateway-calibration/v4` fixture normalized each disposable quicktest before warm-up: world-grid tile index, sea-ice climate, local day/time, clear weather, roof removal including overhead mountain, removal/despawn of every map Thing, soil terrain, zero snow, camera center, and zoom. It applied declared seed `60161` to RimWorld's compressed global random state through an exact guarded shape, without violating the engine's push/pop stack, and restored the prior state during cleanup.

The calibration then attached 32,768 identical no-op `MapComponent` instances. RimWorld—not fixture code—called their override through its ordinary `MapComponentTick` loop during 300 superfast warm-up ticks and 3,000 measured ticks. This deliberately dense synthetic calibration forces disabled wrappers and active timing above RimWorld's superfast frame/tick floor; it is not a gameplay workload or a product-cost estimate. Screenshots were captured before and after, outside the measured window.

The three final frames were personally inspected and showed the same empty, roofless soil map, `10 C`, clear weather, `07:00`, `1 Aprimay 5501`, paused state, and camera framing. No developer-log window was open. The retained logs contain no unexpected error/warning; the expected Gateway unrestricted-execution warning remains part of dev-only startup. Every process cleaned its temporary components, RNG state, exact stage, process, credentials, and isolated configuration.

| Lens | Wall time for 3,000 ticks | Circinus recorded cycles | Profiler window | Samples | Run-owned sidecars |
| --- | ---: | ---: | ---: | ---: | ---: |
| Instrumented | 17,530.8828 ms | 104 | 4,637.385726 ms / 3,000 ticks | 17 | 3 |
| Armed, timing disabled | 16,563.9568 ms | 0 | 0 ms / 0 ticks | 16 | 3 |
| Fully disarmed | 4,343.7530 ms | 0 | 0 ms / 0 ticks | 4 | 0 |

The one-observation control differences were:

- active timing/sampling: `+966.9260 ms` (`+5.8375%`);
- wrapper: `+12,220.2038 ms` (`+281.3282%`);
- total method instrumentation: `+13,187.1298 ms` (`+303.5884%`).

The instrumented lens observed a `30.232558%` profiler duty share. Its hot native method sidecar retained `24,150,016` observed calls, `520,832` timed calls, and a final adaptive shift of `1`, proving that adaptive timing was active instead of merely copying an inspected policy constant. Circinus' native policy remains 60 recorded frames followed by roughly 240 skipped frames, adaptive timing above 64 calls per recorded frame, maximum shift 8, a 2,000-recorded-frame ring, a 7,200-sample run ceiling, and stopped-document caps of 3,000 patch rows and 1,000 method rows. The short-run observed values do not replace or redefine those native limits.

One observation per lens is sufficient to calibrate and prove the control paths, but insufficient for a historical baseline or regression threshold. Accepted baselines require repeated compatible samples, immutable reviewed candidates, explicit per-metric policies, and the comparison workflow tracked by the performance OpenSpec change.

Raw and normalized evidence is generated under ignored `artifacts/PerformanceRuns/<run-id>/`; it is not committed as product content.
