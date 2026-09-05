# Exploration discovery list can break baby placement

## Observed bug

On RimWorld `1.6.4871 rev591`, Rimworld Exploration Mode had 240 `WorldFeatureManager.learnedFeatures` entries for 241 `Find.World.features.features` entries. The last feature was **Iñocai Crater**. `VisibilityManager.UpdateGraphics()` indexed the missing flag and raised `ArgumentOutOfRangeException`. The caller was pawn registration during `Toils_Bed.TuckIntoBed`, reached through `Toddlers.JobDriver_BringBabyToSafety`. The adult's childcare job consequently failed to place the baby in its crib.

Inspected assembly: `RimworldExplorationMode.dll`, MVID `922d330d-1f20-4ee0-8695-816b73426fa8`, Workshop item `2941608795`. This identity records the investigation; the repair does not require the same MVID or full method IL. No downloaded file is modified.

## Mod constellation and prerequisites

The observed game included **Rimworld Exploration Mode** (`thelastbulletbender.rwexploration`), its declared dependencies **Harmony** and **Vanilla Expanded Framework**, **Biotech**, and **Toddlers** (`cyanobot.toddlers`, Workshop `2903359152`). Biotech/Toddlers explain the observed baby job, but the stale-list defect belongs to Exploration and can affect other pawn registration/visibility paths too.

Reproduction requires an initialized, non-null discovery list shorter than the current feature list, a pending `Full`/`Planet` update that visits features, and `RevealAll == false`. The inspected int-backed enum has `Full=2`, `Planet=3`. A normal pawn registration with no pending feature refresh does not exercise this defect. The investigation established the mismatch, but did not establish which world event originally appended the crater. Do not infer that Toddlers creates world features.

In a disposable test world, place a baby on the ground near a crib and an adult. Shorten the initialized discovery flags by one while preserving their prefix. Select the adult and use the native **Put [baby] somewhere safe** option. While the adult carries the baby, re-establish the missing final flag if an intervening update repaired it. Let the adult complete the native drop/tuck path. Without a repair, the out-of-range read can abort the job; with the repair, the baby rests in the crib. Save and load normally to check discovery preservation.

## Offending code

The following excerpts reconstruct the relevant operations from the installed assembly; they are not a claim to reproduce its complete source.

`WorldFeatureManager.FinalizeInit(bool)` initializes only a null list:

```csharp
if (learnedFeatures == null)
    learnedFeatures = Enumerable.Repeat(false, world.features.features.Count).ToList();
```

Later, `VisibilityManager.UpdateGraphics()` assumes one flag for every current feature:

```csharp
int i = 0;
foreach (WorldFeature feature in Find.World.features.features)
{
    var manager = Find.World.GetComponent<WorldFeatureManager>();
    if (!manager.learnedFeatures[i])
    {
        // Existing visibility calculation can set manager.learnedFeatures[i] = true.
    }
    i++;
}
```

An initialized list survives save/load without growing when features are later appended.

## Proposed fix

Immediately before the affected indexed read, ensure the entry exists:

```csharp
while (manager.learnedFeatures.Count <= i)
    manager.learnedFeatures.Add(false);
if (!manager.learnedFeatures[i])
{
    // Keep the existing discovery calculation and setter unchanged.
}
```

Existing flags retain their positions and values. Newly encountered features begin undiscovered; the original calculation may subsequently discover them. The list is never truncated, reordered or replaced. Null lists and negative indexes retain their original error behavior. A more general upstream feature-identity migration would be separate work.

The personal Harmony transpiler replaces exactly one `List<bool>.get_Item(int)` call, matched immediately after the `learnedFeatures` field and index-local loads, with `DiscoveryFlags.Read(List<bool>, int)`, implementing the capacity check above. Branch/exception metadata is retained. The following upstream setter is therefore safe for the same index.

## Activation and verification

Startup requires the expected signatures and one unambiguous relevant IL fragment. Existing target patches cause a warning and skip. A detached copy of the actual method runs against three fixture features and `[true, false]`; live world getters, rendering, component access and tile enumeration are substituted. Unknown calls/side effects make the probe inconclusive. Only the exact fixture bounds failure authorizes installation. The installed IL and a second copied-method assertion must both pass, otherwise only the personal owner is rolled back. If an upstream implementation already passes, this fix is `NotRequired`.

Ordinary tests cover flag preservation and decision/rollback policy. The isolated Harmony suite covers real copied synthetic IL, upstream-fixed code, local matching and foreign-owner interference. On 2026-09-05, 12 ordinary and 12 Harmony tests passed. Fresh minimized RimWorld `1.6.4871 rev591` verification completed the native baby safety command, observed the carer carrying the baby and the baby resting at the crib, grew 62 discovery flags to match 63 features, and retained the exact discovery sequence through native save/load. The acting agent inspected before/carry/resting/load screenshots. A separate target-absent process logged Absent and completed startup cleanly. Both isolated runs cleaned up and retained unchanged normal configuration hashes.

Evidence is local under ignored `artifacts/PersonalBugfixes/20260905/`; the earlier live hot repair is not acceptance evidence for this formal mod. Later builds require fresh verification. No upstream author has been contacted or given a compatibility claim beyond this tested target/workflow.
