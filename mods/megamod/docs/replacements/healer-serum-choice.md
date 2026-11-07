# Healer Mech Serum Choice

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement. S089/P2-18. **Specified draft; nothing implemented or live-verified.**

## Identity and provenance

Syrus, `syrus.hmschoice`, [Workshop2714095848](https://steamcommunity.com/sharedfiles/filedetails/?id=2714095848).
Selected1.6 DLL1.6.0.0; About1.3–1.6, Harmony dependency. Complete source/binary/native and consumer
analysis, hashes, revision/license limits and HMSC-Q1–5 are in
[research](../refinement/2026-11-05-healer-serum-choice-research.md).
[Change](../../../../openspec/changes/specify-megamod-healer-serum-choice/proposal.md),
[design](../../../../openspec/changes/specify-megamod-healer-serum-choice/design.md),
[spec](../../../../openspec/changes/specify-megamod-healer-serum-choice/specs/megamod-targeted-healing-items/spec.md),
[tasks](../../../../openspec/changes/specify-megamod-healer-serum-choice/tasks.md).

## Behavior inventory

| ID | Input/action → observable result | Options / timing / edge cases | Evidence and spec coverage | State |
| --- | --- | --- | --- | --- |
| HMSC-01 | Ordinary use or doctor administration -> recipient's chooser | After native work; animals included; retain actor/patient constraints | Source usable/dialog, native job/recipe; Choose after ordinary treatment preparation | Specified |
| HMSC-02 | Inspect eligible rows -> distinct part/condition/severity and cure scope | No hidden/good/uncurable/immune conditions; replacement/missing-parent exclusions | Source predicate, native cure/restoration; Show eligible conditions with their consequences | Specified |
| HMSC-03 | Confirm -> one item and selected native cure | Same-condition group and limb descendants remain native; feedback once | Source effect, native consumption; Confirm one paid native treatment | Specified |
| HMSC-04 | Cancel/stale/save/switch -> unused dose retained, no replay | No automatic replacement target, duplicate confirmation or cancelled-bill retry | Owned corrections to source lifetime; Cancellation and stale choices preserve unused doses | Specified |
| HMSC-05 | Admitted optional item -> same chooser with actual item | Archotech/medieval adapters pending exact identity; other items unaffected | Source XML/public description; Isolate optional healing and automation | Specified boundary; adapter research open |
| HMSC-06 | Automatic shaman healing -> native event result | No chooser, carried-serum cost or changed event payment | Actual MFI component consumer; Isolate optional healing and automation | Specified; native subtype verification open |
| HMSC-07 | Original/changed provider or multiplayer -> bounded fallback | One provider; no package spoof or unverified local-only cure | Actual MP binding, admission policy; Publish only admitted single-provider behavior | Specified |

## Interactions and composed result

Smart Medicine, Drug Response and Priority Treatment compose explicit medical policy directly; they
do not silently authorize rare-item use or choose a condition. FoodTracker's partial-ingestion path
does not apply. Use native/current health facts and shared candidate sources, not another cache.
Source HMSChoice suppresses owned overlap. Optional item failure is isolated; mutations which would
break a proven Core component consumer are not admitted merely because its provider is optional.

## UI audit and settings

| Source entry | Purpose | Owned destination / change |
| --- | --- | --- |
| Item use command and Health medical bill | Ordinary treatment | Retain native contextual controls and doctor/patient distinction |
| Modal part/condition/severity chooser | Select a paid treatment target | Contextual gameplay window, actual item/recipient identity and cure-scope explanation; no bottom tab |
| No source settings/toggle/debug entry | No configuration | Add none; pending patient choice is gameplay state, so global/save inheritance is inapplicable |

Translate all new labels/reasons in seven baseline languages and verify scrolling/long labels,
Cancel/Confirm, save switching and no main-menu patient editing. Source settings migration: none.

## Contracts consumed by other mods or saves

| Consumer / evidence | Contract | Disposition |
| --- | --- | --- |
| MFI MysticalShaman source | Native CompUseEffect_FixWorstHealthCondition lookup and direct DoEffect | Preserve assignability and automatic meaning; separately owned selected operation. Exact binary/native proof pending |
| Multiplayer Compatibility source | Original package, reflected dialog, SelectedHediff, Close(Hediff), DoWindowContents and first matching window | No drop-in claim; own chooser inactive in multiplayer until explicit synchronized adapter exists |
| Core saves and item identities | Existing health, item Def, native medical jobs | No health rewrite/source migration; transient choice cancelled before save/teardown |
| Source public dialog and choice field | Exposed types/members; MP is an actual consumer | No speculative façade or package identity spoof; unsupported bindings documented |

## Implementation and coverage

| Implemented | Deviation / possible conflict | Missing / omitted |
| --- | --- | --- |
| Nothing | Explicit custody, revalidation, no duplicate cure, lifecycle cancellation and truthful interrupted-treatment status | All runtime code, focused tests, reviewed-build native evidence |
| Nothing | Native assignable effect proposed instead of source's fragile shaman transpiler; keep ordinary automatic call separate | MFI actual provider/native verification and optional item adapters |
| Nothing | Guarded merge-preserving scalar XML, required XML Extensions on use; preserve unrelated fields/effects | Patch documents/executable predicates and supported effective-provider matrix |
| Nothing | Actual item name/group cure explanation; no extra tab/settings | Seven-language UI and native medical workflow acceptance |
| Nothing | No implicit multiplayer support | Owned synchronization deliberately deferred; source MP adapter is insufficient |

No mutagenic serum, resurrector serum, Anomaly Unnatural Healing, new items, price/availability/cure
rebalance, foreign assembly reuse or automatic rare-treatment policy. No per-source cache or source
settings import. No DLC required; Core/XML Extensions required when implemented, Harmony required
on actual use. External providers remain optional and independently admitted. No Gateway dependency.

## Verification and acceptance

Document validation/review are recorded in the [ledger](../refinement/specification-progress.md).
All gameplay tasks stay unchecked. Native direct and doctor use must visibly connect selected
condition, cure result and exact dose cost, including animal, group and restoration cases. Cancellation,
staleness, save/holder/stack lifecycle and exact optional providers need separate causal evidence on
the reviewed build. Retain normal-config hashes and isolated cleanup. Static Core comparators guide
the final balance pass; neither decompilation nor a clean startup accepts this feature.
