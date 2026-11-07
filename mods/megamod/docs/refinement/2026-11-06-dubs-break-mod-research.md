# S119 — Dubs Break Mod

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Origin replacement, S119/P2-48. Research date **2026-11-06**. Documentation and complete selected
binary analysis only; no original/replacement runtime behavior has been played or accepted here.

## Documentation first and exact provenance

- [Steam](https://steamcommunity.com/sharedfiles/filedetails/?id=1722398508) describes a requirement
  for a sufficiently negative thought, severity reduction when that requirement is unmet, editable
  limits, optional colonist-only scope and preserved mood thresholds. It recommends loading below
  Common Sense and names Harmony. Generic page visibility/incompatibility banners are not evidence
  about this binary's actual operation.
- [Changelog](https://steamcommunity.com/sharedfiles/filedetails/changelog/1722398508) records the
  October13 1.7 update. The installed metadata supports1.0–1.7, version1.0.2152, author Dubwise,
  package `Dubwise.DubsBreakMod`; PublishedFileId1722398508. Metadata itself does not declare the
  Harmony dependency/load order; do not copy that omission.
- [Public repository](https://github.com/Dubwise56/Dubs-Break-Mod/tree/27cff8ce9e0392085d46790691104c47c1803559)
  at commit `27cff8ce9e0392085d46790691104c47c1803559` (2026-10-17) contains versioned binaries,
  metadata and README, no `.cs` source project or LICENSE/COPYING. GitHub license metadata is null.
  Decompilation is the implementation evidence; a public binary is not a redistribution license.
- Selected installed `1722398508/1.7/Assemblies/DubsBreakMod.dll`:8192 bytes, assembly version
  **1.0.9781.38431**, SHA256 `CAD38FD20478F2B5FFCDC27A0C0C947CC5C23BAAE7C4049C1D21BAB46E3AB9C6`.
  Its Git blob `2b3b0ed616aa0b918c15523c0aa531ab4743235e` equals the pinned public1.7 binary.
  Native default loading selects root+1.7; no LoadFolders.xml. Older version DLLs are not the input.
- Source bundles `0Harmony.dll`, including1.7. Independent implementation must declare Harmony and
  ship none of those assemblies. The sole English keyed XML is empty; settings are literal English.
  No gameplay Defs, patches XML, new content, bottom tab or custom save component exists.
- Native input **1.7.5000rev1275**, Assembly-CSharp.dll15,636,480 bytes, SHA256
  `D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`.
  Ten successful native type extracts cover selection, thoughts, workers/definitions, state start,
  ideology cache, settings loading and weighted-selection consumption. Two wrong-namespace empty extracts are marked unsuccessful.
  Current Pawn classification and folder selection reuse the same-hash S118/S117 native extracts.

Ignored evidence: `artifacts/Refinement/megamod-spec-20261106/dubs-break-mod/`, with public pages/API
records, exact binary match, input/extract identities and decompiled source. Never execute inspected
assemblies or copy code/binaries/art into the product.

## Complete selected binary inventory

| Type / surface | Actual behavior / contract |
| --- | --- |
| `MentalManagementMod` constructor | Creates Harmony owner `Dubwise.DubsBreakMod`, unconditionally PatchAll, reads Settings and immediately writes them |
| `SettingsCategory` / `DoSettingsWindowContents` | Native mod-settings category and delegation, no gameplay window/tab |
| Public static `Settings`, unused `optionsWindow` | Settings are accessible fields; unused window is not an implemented gameplay feature |
| `Harmony_CurrentPossibleMoodBreaks.Prefix` | Only patch: replaces the private native getter; ColonistsOnly bypass; gets native desired intensity and grouped effects; delegates to native intensity selection |
| Private `DetermineHighestIntensity` | Tests each group's effect against Extreme/Major/Minor in that order; keeps highest qualifying severity; stops once Extreme is found |
| `Settings` fields / controls | Three independent integer-cast sliders−100..0; initializer−10/−15/−20; ColonistsOnlyfalse; labels are hardcoded English |
| `Settings.ExposeData` | Global keys MinorLimit/MajorLimit/ExtremeLimit use default0; ColonistsOnlyfalse, LastVersion0; no per-save overrides |
| `Settings.ResetSettings` | Empty method, not a functioning reset feature |
| `ReloadableAttribute` | Marker on Prefix; no additional behavior or source-specific runtime loader in this assembly |

The source has four code types, one prefix, no transpiler/postfix, no custom thought/break Defs,
no persistent pawn cache and no additional mental-state worker. Public API-shaped fields/methods
are inventoried without assuming they are consumed by another mod.

## Source semantics and native trace

Source gets `GetDistinctMoodThoughtGroups` then `MoodOffsetOfGroup`, not every individual memory's
baseMoodEffect and not a sum of unrelated negatives. Current native grouping uses `GroupsWith` and
stage compatibility. Group effect averages current instance effects and applies the native stacked
multiplier series. Ordinary memory/situational modifiers, nullification and admitted fading therefore
matter. Social opinion is separate. Empty groups never create a qualifying thought; if zero is
configured, only an actual eligible group with nonpositive current mood effect can qualify.

Define thought ceiling as highest tier meeting any group's configured inclusive limit. The source
then selects the lower of that ceiling and the native desired mood tier, or None. Thresholds are
independent: Extreme−5 can authorize lesser severity even when Minor−30 would not pass separately.
This is retained and explained rather than silently enforcing an order the source never required.

Native sequence:

1. `MentalBreakerTickInterval` updates its mood-duration counters on150-tick checks, retains ordinary
   life-stage/active-state/debug gates, and runs its ordinary chance/trait branches.
2. `TestMoodMentalBreak` uses normal recovery cooldown and more than2000 ticks below thresholds;
   current Extreme/Major/Minor opportunity means are0.5/0.8/4 days respectively. Desired-tier lookup
   uses at least2000. These native values are evidence, not new owned settings or a new scheduler.
3. `TryDoRandomMoodCausedMentalBreak` checks ordinary eligibility, gets CurrentPossibleMoodBreaks,
   chooses by `CommonalityFor(...moodCaused:true)`, picks a native final-straw thought and calls the
   normal start. A capped Minor candidate can still be chosen at an Extreme-mood opportunity rate.
4. `CanHaveMentalBreak` retains awake/not-downed/not-already-in-state, life-stage, mutant/quest and
   blocking rules. Nonplayer eligibility uses **native desired Extreme**, so globally changing that
   property would alter population eligibility beyond source behavior.
5. `GetBreaksForIntensity` first tries eligible ideology-cached alternatives, then ordinary exact-tier
   candidates with native anomaly choice/fallback; if the ordinary pool is empty it steps downward.
   Weights and actual workers remain native. Zero weighted selection need not produce a break.
   The non-list `GenCollection.TryRandomElementByWeight` consumer makes one reservoir-selection
   pass but does not dispose the enumerator or catch weight exceptions. Owned contextual scope
   cannot rely on iterator disposal after a weight callback throws while suspended; use per-step
   scope or a guarded outer cleanup boundary and test that exact failure.
6. `RandomFinalStraw` considers individual negative thoughts near the most negative individual
   effect, with weighting; it need not name the group that permitted the severity. Source leaves
   this message selection unchanged.
7. Direct `TryGetRandomMentalBreak`, explicit starts and non-mood trait/gene/incident/ability paths
   do not use the patched getter. Recovery/counselling happens afterwards and is not modified.

ColonistsOnly uses `Pawn.IsColonist`, including currently secured player slaves classified as
colonists, not `IsFreeNonSlaveColonist`. It requires player faction/humanlike/not-subhuman but does
not directly exclude HostFaction/location. Ordinary outsider prisoners, visitors, raiders and
animals bypass when not classified colonists. No-mood entities must not gain an invented tracker.

## Intentional corrections and preserved boundaries

| Finding | Owned decision |
| --- | --- |
| Fresh no-file settings return `new T()` and use initializers; an existing record with absent threshold keys uses0 | Common global/save definitions consistently use−10/−15/−20 built-ins and validated inheritance; no silent legacy import |
| Native ideological branch does not test requested intensity, even None | Keep eligible ideological preference only for recognized Minor/Major/Extreme within the effective ceiling; exclude unset/unknown severity too; when all are excluded, allow ordinary fallback; no thought-supported tier means no ordinary mood episode |
| No current Core/DLC or installed XML/source reference to PreceptComp_MentalBreak found in bounded searches | Describe an executable extension-path risk, not a reproduced Core precept bug; concrete optional fixture remains DB-Q2 |
| Getter replacement can lose unrelated future/foreign logic | Minimal semantic insertion/scoped adapter; preserve compatible candidate changes, fail safely if required effective semantics are unknown |
| Source has no no-mood defensive branch after scope/desired checks | Safe absent-mood native fallback, without fabricating eligibility |
| Global English-only settings, no-op Reset, bundled Harmony | Common inherited/localized UI, consistent resets through shared mechanism, declared external Harmony only |

The ideological correction is an explicit proposed compatibility/behavior choice to enforce the
source's severity promise. It must not globally mutate an ideology, block direct forced episodes
or remove all ideological alternatives. Anomaly choice happens once per native evaluation, not
again after filtering. If an unsafe partial evaluation has already consumed random/stateful work,
abort that opportunity without replay or recovery reward and use safe fallback subsequently.

## Static playability comparators

Core XML anchors, assuming actual current unmodified effects equal these base values:

| Example | Source base mood effect | Default maximum thought-supported tier |
| --- | --- | --- |
| AteWithoutTable |−3|None|
| MyLoverDied |−16|Major|
| MyHusbandDied / MyWifeDied |−20|Extreme|

These are not finalized game observations; traits, stages, nullification, fading, grouping and other
mods can change current effects. KnowColonistDied has base−3 and a stack limit5, demonstrating why
the group calculation cannot be replaced with a single base-value lookup. Native labels/negative
effects continue to affect mood regardless of ceiling. Low mood remains dangerous through timing,
allowed milder episodes and non-mood events. No production/research/resource/XP change is introduced.
Combined effects with memory fading and recreation require playtesting; this is not neutral QoL.

## Contracts, composition and UI

Bounded installed text/binary searches for package/type/assembly markers found only source-version
DLLs, RimPy catalog/load-after Common Sense metadata and layout-harness modlists. No actual compiled
or reflected consumer of the source Settings/Prefix types was found. Common Sense's current DLL
matches the previously analyzed S024 hash; its selected code contains no MentalBreaker/getter patch.
The source's load-after recommendation is recorded, not treated as a hard dependency or evidence of
a current conflict. No package/assembly spoofing, facade or old-save conversion is justified.

S068's current fading contributes through native/owned mood effects once. S024's ordinary need/mood
effects remain upstream; S100 handles already-active episodes; S117/S118 only determine outcomes
after a selected slaughterer state takes its own job. S001 mood bars and ordinary alerts stay based
on native mood thresholds; this feature does not recolor them to the thought ceiling. Source-present
selects source behavior and disables owned overlap, without claiming owned fixes for the source.

Four settings belong in the structured mental-break settings section with shared globals/save
overrides/Use global, dormant overrides, current-global inheritance, main-menu disabled save callbacks
and loaded-world active-save behavior. Limits affect next evaluation; no thought/history reset or
active-state cure. Per-pawn mood/thought/state facts are gameplay state, not extra settings. All seven
languages explain grouping, limits, precedence, scope and warning/timing distinctions. No new tab,
bottom-right toggle, debug override, pawn cache or Thing-index scan.

## Status and open work

[Proposal](../../../../openspec/changes/specify-megamod-thought-gated-breaks/proposal.md),
[design](../../../../openspec/changes/specify-megamod-thought-gated-breaks/design.md),
[observable spec](../../../../openspec/changes/specify-megamod-thought-gated-breaks/specs/megamod-thought-gated-breaks/spec.md)
and [coverage](../replacements/dubs-break-mod.md). DB-Q1–4 in the
[ledger](specification-progress.md) retain effective admission, concrete providers, finalized
playability/UI/cost and implementation/native acceptance. No interviews are needed for source facts.

Nothing implemented. No product code/tests/build/install/game launch, normal config/save writes,
Workshop modifications, art selection, commit or publication. Native acceptance later requires
actual player gameplay → current thoughts/mood opportunity → observed episode/continued behavior;
directly assigning the expected mental state or a successful patch diagnostic is not proof.
