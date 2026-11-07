# S103 — Need Bar Overflow research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Research on 2026-11-05; static inspection only. No mod was executed, installed or modified and no
gameplay is accepted. [Owned change](../../../../openspec/changes/specify-megamod-need-overflow/),
[coverage](../replacements/need-bar-overflow.md), [queue](specification-progress.md).

## Evidence and identity

Public documentation first: [Steam](https://steamcommunity.com/sharedfiles/filedetails/?id=2566316158&l=english)
and [author repository](https://github.com/AmCh-Q/RimWorldMod_NeedBarOverflow). The first Steam request
returned429; the language-qualified page later succeeded. Generic scraped Steam banner/account text
is not installation or compatibility evidence. Public descriptions establish temporary reserves and
ordinary full thresholds; the selected binary resolves discrepancies in defaults.

| Input | Inspected identity |
| --- | --- |
| Installed source | `F:/Steam/steamapps/workshop/content/294100/2566316158`, package `AmCh.NeedBarOverflow`, author AmCh |
| Selection | No LoadFolders.xml; root content and `1.7/Assemblies/Need Bar Overflow.dll`; About modVersion1.5.0, supports1.2–1.7, Harmony required |
| Binary | AssemblyVersion1.4.6.0; SHA256 `ACC89788237624EA39C8B4AA4D961931978DA4C9CEB5FB67EEE25FFC9D78B52E`; MVID `6f288b1c-41e5-42f4-a93e-4da8e295795d` |
| Public revision | `0d368a91cf3dd1dadbc30d83363dd5711bd95e83`; installed Source tree matches this revision ignoring line endings; runtime defaults and important hooks also checked in decompiled selected DLL |
| Native | RimWorld1.7.4998 rev1299; Assembly-CSharp SHA256 `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |
| Retained local evidence | `artifacts/Refinement/megamod-spec-20261105/need-bar-overflow/`: upstream clone, binary/native/provider decompilations, inspection-manifest.json with source/metadata/XML/language hashes |
| Licensing | GPL-3.0 LICENSE. Steam illustrations credit Fonzawa with permission; no transferable illustration permission assumed. Independently implement; no upstream binary/code/art copied into product |

Read the complete selected patch/setting inventory, public/installed source and root XML. Disabled
source branches are not counted as active behavior: quest goodwill and deathrest-priority patches
are compiled out; old FloatMenuMakerMap and debug offset branches do not establish1.7 coverage.

## Actual behavior and defaults

`Settings`, `Setting_Common`, `Setting<T>`, `Setting_Food`, `OverflowStats_DrainGain<T>` and their
selected-binary counterparts establish these defaults, rather than older Workshop prose:

| Setting | Selected source default / semantics |
| --- | --- |
| Food, rest, recreation, comfort, beauty, chemical addiction needs, chemical interest/fascination | Enabled, multiplier2 of ordinary maximum |
| Mood, room size, outdoors, indoors, sadism, suppression, deathrest, kill thirst, learning, mech energy, play, authority, unknown need fallback | Disabled, dormant multiplier1 (`-2` encoded); generic/authority/sadism hidden in ordinary source settings |
| Food extra reserve |1 nutrition; cap is larger of `ordinaryMax*multiplier` and `ordinaryMax+bonus`, not their sum |
| Further eating restriction | Enabled at100% ordinary full; settings range50–1000%; hungry categories bypass; only inventory food and selected-pawn ingest-menu routes checked; drugs and hemogen-pack thought excluded |
| Food consequence scaling | Nonhuman25%; human Gourmand25%; otherwise100%; nonhuman branch takes precedence, not multiplicative stacking |
| Food condition display | Adjusted fullness threshold120%; source sets visibility permanently once exceeded until condition removed |
| Food health effects | All five off; eight stages below are dormant defaults. Raw root XML has different values but is rewritten when effects enable |
| Rest/recreation/kill-thirst faster loss / slower gain | Off; stored `-.5` conflicts with source disabled encoding `-value-1`, yielding a negative dormant value. Own proposed dormant coefficient0.5 explicitly corrects this |
| Group eligibility | Colonists/slaves/guests/prisoners hosted by colony on; other humans off; player animals on; wild/other animals off; player mutants on/others off; player other pawns on/others off |
| Manual exclusions | Four comma-text categories race/apparel/health/gene, initially disabled; all needs excluded for a matching entry. Def-name or translated-label matching; unknown tokens silently ignored |
| Eligibility refresh |600ticks, configurable1–100000; runtime hash code keys with possible collision/staleness, cleared on load |

Stage position is `1 + max(0, currentFood/ordinaryMax - 1) * consequenceScale`. Thus at180% actual
food, a quarter-scale pawn uses120% consequence fullness. Rows select the highest reached threshold;
they do not interpolate. Source severity is fullness minus1. Dormant source stage data:

| Adjusted fullness |100% |120% |140% |160% |180% |200% |300% |500% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Hunger factor |1 |1.05 |1.1 |1.2 |1.3 |1.5 |2 |5 |
| Natural healing factor |1.1 |1.2 |1.2 |1.2 |1.2 |1.2 |1.2 |1.2 |
| Moving reduction, percentage points |1 |2 |5 |10 |15 |20 |25 |30 |
| Eating reduction, percentage points |5 |10 |20 |30 |40 |50 |60 |70 |
| Vomiting average events/day |0 |0 |0 |0 |0.25 |2 |5 |6 |

Limits in source UI: hunger1–unbounded, healing1–10, reductions0–100percentage points,
vomiting0–24/day, scales0–1; ordered stage levels. Some source min calculations contradict their
intended positive ranges. The owned spec makes validation explicit and does not copy sign encoding,
Infinity as live need state, mutable-default arrays or culture-dependent load parsing.

## Native contracts and corrections

`Need.CurLevel` clamps to0..MaxLevel; MaxLevel remains ordinary full in the source. Saved `curLevel`
is the backing amount, not a normalized percentage. Food MaxLevel reads MaxNutrition (native15-tick
cache in play); body size/life stage is the non-playing fallback. Source raising the setter cap does
not make pawns seek a200% target. Downward writes preserve overflow, but positive writes after a cap
reduction can lower the existing value: own positive-gain policy must not do that.

| Native/source route | Observed semantics / owned disposition |
| --- | --- |
| Food nutrition wanted | Native MaxLevel−current can be negative; source lower-bounds at0. Preserve that observable safeguard without increasing hunger target |
| Food optional health | Source injection before loss uses old level for severity, can trim excluded reserves, caches pawn multipliers, modifies a shared Def and clears its capacity list. Own current-save effects are derived once from current pawn state; no shared-Def mutation, old-save leak or reserve deletion |
| Natural healing | `Pawn_HealthTracker` multiplies ordinary naturally-healable-injury gain by each non-sentinel stage factor. This is not automatic disease curing, immunity, regeneration or a cure for permanent injury |
| Rest | Native NeedInterval gain uses rest effectiveness and RestRateMultiplier; fall uses category and RestFallRateFactor. Source changes those positive deltas; ordinary wake/job logic is not extended to chase overflow |
| Recreation | Native GainJoy applies tolerance, clamps amount to1−current then writes the backing field. Source lifts clamp; optional rate math uses CurInstantLevelPercentage, but native Joy inherits the no-instant-level sentinel−1. Own rates use actual current fraction and tolerance records actual accepted gain, never negative gain |
| Kill thirst | Native qualifying melee/body/linked-hediff kill resets to1. Source changes to+1 then optional slowdown, even using disabled negative coefficient. Own enabled feature adds100percentage points for the same eligible kills, with slowdown only when enabled |
| Mood/comfort/beauty | Native seeker follows current target; source lifts target clamps. Preserve seeker rates, frozen conditions and body/game rules. Mood inspiration interpolation can go negative above1; cap its mood input at ordinary full. A highest child-mood range ending at1 must continue to include overflow |
| Indoors/outdoors | Native environmental gain has an additional Min1, losses have specific roof/bed floors. Source removes last Min1; own admission identifies the gain only and preserves native resets/floors |
| Learning/play | Native explicit gain clamps lifted by source. Learning's backing-field write must still pass owned policy; play's second clamp must not delete a reserve. Native learning point rate uses current learning directly, so optional overflow can increase growth; S088 then applies Global Learning Factor once |
| Chemical needs | Addiction setter delegates base then notifies category changes. Chemical fascination/interest has its own fall curve, mood categories and drug gains. Preserve all callbacks, addictions, overdoses, policies, drug side effects and native resets |
| Deathrest/mech energy | Existing setters, native losses/gains, charging/self-shutdown and gene rules remain. Mech ordinary maximum is race maxMechEnergy, not assumed1. No new recharge target, deathrest duration or hemogen capacity |
| Room size/suppression/authority/sadism | Retain genuine available native/provider gains only. Room space target curve is capped, Authority is frozen by the inspected zero fall result; do not invent new gameplay/need eligibility just to make a setting useful |

Native `Need`, `Need_Seeker`, all19 named concrete need classes, inspiration/related-child mood and relevant
age/health tracker paths are retained locally. Exact effective patched consumers still need admission
and live verification; reading native source is not proof that every provider cooperates.

Source faster-loss formula is `ordinaryLoss * max(1, 1 + (fraction-1)*coefficient)`; slower-gain
formula divides by the same factor. The owned spec uses nonnegative excess and nonnegative
coefficients. Source global ThinkNode threshold==1 override returns false for every such node;
this is too broad to preserve Tweaks Galore's reported Never sentinel. Own adapters must identify
that particular provider setting, leaving unrelated legitimate above-full conditions functional.

## Presentation, persistence and contracts

Source replaces Need.DrawOnGUI for overflow or developer controls, packs ordinary+reserve into the
same bar, adds cyan overflow, rescales thresholds/ticks and keeps ordinary tooltips/arrows. It scales
from current amount but not a higher instant target, so targets can collapse to the edge. Several
small views clamp presentation to full. Generic GenUI rectangle fractions are clamped globally,
affecting unrelated UI. Own bounded need presentation is local; retain native learning icons,
tooltips, highlighting, direction and layout state. Unit markers must not grow with a huge cap.

Source ± buttons remain in need bars; Character Editor adapters add Shift/Ctrl amounts. Its separate
`AFullNeeds` adapter changes the fill assignment from100% to1000% while Shift is held, still subject
to the admitted need cap. That enhanced fill-all gesture is deliberately omitted from the owned
replacement; the smaller explicit developer adjustments below do not claim to reproduce it. Owned
unrestricted ±1/10/100percentage-point actions go exclusively to the top debug toolbar. No bottom
tab or ordinary bottom-right toggle is needed. Settings use Needs → Overflow ordered sections,
common inheritance, localized help/units/invalid-state feedback and disabled main-menu save controls.

Source saves global settings by type full name under Setting_Common/overflow, per-need named blocks,
food stage strings and disablingDefs. Unknown types are discarded. Native saved need amounts are
ordinary game state; FoodOverflow is a source-specific health Def. No settings/type/hediff migration
is promised; never delete/rename another mod's condition. Owned removable consequences must have an
owned identity and explicit cleanup lifecycle. Optional migration would need a separate atomic,
backup/restoration contract.

Public `NeedBarOverflow.DisableNeedOverflowExtension.disableOverflowNeeds : List<Type>` supports
exact need types or base Need as an all-needs sentinel on race, worn apparel, health, active gene or
Ancients PowerDef. `Setting_Common.modsOverflow` supplies external defaults below saved overrides.
Own XML patches consume this extension; no third-party caller was found. Bounded ASCII binary/source/
XML scan of installed Workshop content found only source files plus Improved Need Indicator's About
recommendation and Modern Needs Tab's legacy `telardo.NeedBarOverflow` load hint. These are not
proven binary API consumers. Scan is not proof against reflection/dynamic/UTF16 consumers. No facade,
package spoofing, source-assembly dependency or automatic import is justified by this evidence.

## Optional providers

| Source integration | Evidence / planned boundary |
| --- | --- |
| Dust Bunnies race `Crows_DustBunnyRace` food; WVC `WVC_DeadStomach` food; Fungoid `VRE_MindCoalescence` mood | Source name-gated XML, providers absent from installed metadata inventory. Preserve intended scoped exclusion as pending adapter research, not accepted compatibility; do not guess package IDs |
| Ancients RedundantStomach food | Installed2654846754 `VanillaExpanded.VFEA`; passive PowerDef and worker named; no1.7/1.6 binary selection found, no LoadFolders. Source queries Pawn_PowerTracker.Get/AllPowers. Exact1.7 provider contract remains a gate |
| Empire techfriar crown rest/recreation | Installed2938820380 `OskarPotocki.VFE.Empire`,1.7 Def confirms crown and research-related needs regeneration. Exclude these needs while worn; package/content/behavior guards and live acceptance still needed |
| VNPE dripper | Installed2920385763 `VanillaExpanded.VNutrientE`, v1.7 LoadFolders selects1.6/VNPE.dll. Decompiled TickRare already checks configurable feeding threshold, pays pipe resource, ingests meal and adds nutrition. Source erroneously targets StartInspirationMTBDays on Building_Dripper, so its proposed blanket post-feed food clamp does not establish an active integration. Preserve paid legitimate ingestion; never trim existing reserve merely because pawn occupies linked bed |
| CM mood bar / RimHUD | S001/S127 direct owned presentation composition. Source CM hook clamps display; no selected dedicated RimHUD patch was found. Public compatibility claim is not accepted runtime proof |
| Character Editor / Tweaks Galore / CE / Alpha Books | Advertised or source-hook references; not interchangeable with accepted contracts. Keep external editing/AI/food behavior except narrow verified normalization. No blanket predicate suppression or blanket CE claim |

Any shipped exclusion XML uses package-ID XML Extensions activation plus Def/node/shape checks,
merges without duplicates and preserves unrelated extensions/lists. First use makes XML Extensions
required. The old display-name-only AddModExtension patches are not compliant target checks.

VNPE reproducibility: selected `1.6/Assemblies/VNPE.dll` is AssemblyVersion1.0.0.0, MVID
`54064b3c-1ff5-4b7e-82dd-352a62d02d2e`, SHA256
`D0CD5E5C82056F853B6C7E2782A753286B74CDB406DB077EEAC0E1A5EB2D7D29`.
Its `loadfolders.xml` SHA256 is
`CAD111CA18C319A1DFB058D4F695437F31A7D75EC12E9FC6FDAD991FE058FBCA`.
These provider fingerprints, About and retained decompilation hashes are recorded separately in
the local inspection manifest. They identify evidence, not runtime admission predicates.

## Composition and static balance pass

- **S003 / IN-Q2:** ordinary full remains native MaxLevel; actual amount can exceed it. Food forecasts
  traverse effective hunger stages; rest/recreation traverse current optional overflow rates once.
  Roof-need forecasts preserve direction/floors. No meaningful supported-rate model means explicitly
  unavailable forecast, not a fabricated fixed timer. No extra provider cache or self-patches.
- **S001/S002:** mood display remains bounded; recreation gain follows actual tolerance and rates,
  with no new autonomous target beyond ordinary full and no duplicate directed-recreation gain.
- **S031/S088:** optional play/learning reserves do not create toddler skill XP; learning affects
  ordinary growth rate and S088 multiplies once. Example ordinary100%-learning rate2/day,150%learning
  gives3/day;150% Global Learning Factor gives4.5/day, not6.75. Default learning remains off.
- **S085 and hauling/food policies:** partial ingestion must account only actual nutrition consumed;
  do not consume a second portion, fabricate reserve, bypass carried-food permissions or drug policy.
- **S045/S046/S062/S124/S134:** optional nutrition consequences are not treatment, immunity or
  consciousness ceilings. Display/medical composition must not duplicate their effects or trigger
  unrelated treatment. Existing health rules may still down a sufficiently impaired pawn.
- Static comparators: native nutrition maximum/hunger, ordinary rest recovery, native recreation
  tolerance/loss, high-comfort furniture target, chemical desire/side effects and learning growth.
  Reserves can extend work without further consumption;2x limits and1nutrition minimum favor small
  bodies deliberately. Source-default effects off avoids silently introducing penalties. These are
  draft balance choices, not measured finalized1.7 playability or a performance claim.

## Open work

| ID | Kind | Remaining gate |
| --- | --- | --- |
| NBO-Q1 | Admission | Exact effective gain/loss/reset/consumer contracts per need, safe atomic activation/repatch fallback, original-provider co-installation |
| NBO-Q2 | Optional contracts | Missing provider identities/1.7 shapes, actual reflection/extension consumers, provider exclusions and safe drift handling; no speculative facade |
| NBO-Q3 | Settings/balance | Finalized native comparisons and playtesting of source-default reserves, proposed coefficient0.5, stage thresholds/effects and altered lifecycle semantics |
| NBO-Q4 | Performance/UI | Bounded combined eligibility/rate/presentation costs, crowded bars and seven-language controls including huge finite reserves and differing maxima |
| NBO-Q5 | Acceptance | Focused TDD, independent reviewed implementation, real gain/loss/health/job/UI/save workflows in isolated1.7; source-settings/hediff migration remains unpromised |

Record these for later; do not interview the user about existing source behavior. All implementation
and native acceptance tasks remain unchecked.
