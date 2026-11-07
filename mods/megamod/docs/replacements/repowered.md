# Turn It On and Off - RePowered

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target1.7 beta.
Origin **replacement**, S053/P1-46. Source Mlie, `Mlie.TurnOnOffRePowered`,
[Workshop2228219259](https://steamcommunity.com/sharedfiles/filedetails/?id=2228219259).
Inspected2026-11-03, modVersion1.6.8; installed1.6 reference DLL SHA256
`A647D5C9E60D2BB71426AADAA2B0ADA6C1351CECCFFA6185C6CC8A5286DC2592`.
[Pinned source](https://github.com/emipa606/TurnOnOffRePowered/tree/88447b6019dcbb10f0b8719ac91deed942e35eb8),
MIT copyright2020 Mlie; retain notices for any future reuse. No source code/assemblies/assets shipped.
[Research](../refinement/2026-11-03-repowered-research.md) retains full current-source read coverage,
selected compiled comparisons, native/provider samples, real consumers and unverified boundaries.

Status: **analyzed, observable draft independently reviewed and strictly validated**. Nothing implemented
or accepted in game. [Proposal](../../../../openspec/changes/specify-megamod-repowered/proposal.md),
[design](../../../../openspec/changes/specify-megamod-repowered/design.md),
[spec](../../../../openspec/changes/specify-megamod-repowered/specs/megamod-repowered/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-repowered/tasks.md).

## Behavior inventory

All rows are source-researched and specified, not implemented or runtime verified.

| ID | Action → intended observable behavior | Defaults/edges | Owning requirement |
| --- | --- | --- | --- |
| RP01 | Enable budgets / supported work → one activity and cost policy | Player equipment; S052 fallback when disabled; excluded devices native | RP01 in linked spec |
| RP02 | Work, viewing, bed occupation → device/facility active until last use ends | Actual work, already-at-target start, valid shared links, no ordinary reservation-only activity | RP02 |
| RP03 | Door movement, turret target, planted basin → appropriate state | Both door directions, held/blocked idle, target lifecycle, crop occupancy and continuous-service limits | RP03 |
| RP04 | Rate selection → one predictable draw | 10 W idle,2.5× active,10× doors, four source presets; explicit > external > preset > generic | RP04 |
| RP05 | Electrical bill starts → native eligibility plus configured power forecast | On,500-tick-equivalent reserve horizon; per-network incremental load, required/optional facilities, topology changes, simultaneous starts, no running-job preemption | RP05 |
| RP06 | Search/edit/reset settings → scoped persistent type choices | Common inheritance, Automatic or atomic pair, absent-type retention, protected main menu | RP06 |
| RP07 | World view/load/transfer/inspect → current demand and information | All loaded maps, no stale admissions/references, accurate descriptions | RP07 |
| RP08 | Optional defined activity → supported work/reservation/schedule | Explicit combined gate; actual provider state/costs, no inspection-text heuristic | RP08 |
| RP09 | Changed prerequisite/co-installed controller → coherent native fallback | Bounded warning, no partial forecast/rate group, no source data mutation | RP09 |

## Interactions and composed result

[LightsOut](lights-out.md) supplies actual-use activity for overlapping work equipment; RePowered
supplies its costs once. Other LightsOut equipment and lamps retain their own policy. Disabling
budgets restores shared S052 and exclusive native rates. Failed combined admission instead keeps that
device native, suppressing both owned rate policies and the added forecast with truthful unavailable
inspection. Owned bill/hauling/work features supply activity directly through the normal work path,
without patches to our own features. Shared map
data starts from native listers, not one cache/scan per source port.

Presets intentionally alter power economics, with especially high smelter/research active loads;
the source and Core comparators are recorded, while finalized balance/native reachability remain
open. Explicit pairs are absolute and say so. Generic multipliers preserve current native baseline
adjustments; independent provider costs remain protected. Native blackout/work restrictions win.

## UI audit and relocation

| Source entry point | Purpose | Owned destination / deviation |
| --- | --- | --- |
| General settings sliders/checks | Configure cost and work-start policy | Ordered common Power → Equipment budgets rows, global/save inheritance; explicit reserve horizon |
| Devices tab, label/mod/Def search, numeric pair/reset | Configure all instances of a building type | Common searchable scoped rows, Automatic or atomic pair, source labels, absent-type retention |
| CompPowerTrader inspection | Current and opposite-state W | Additive current state/draw/rate source, preserving native/provider information |
| verboseLogging | Developer diagnostics | Bounded top developer interface, no per-tick error flood |
| Bottom bar | No source bottom tab found | None added; individual flick/door controls stay contextual |

Every save editor/Use global/reset callback is disabled without an active save. Common dormant
overrides and global changes behave as foundation specifies. No inheritance exception is needed.

## Contracts consumed by other mods or saves

| Actual consumer/evidence | Binding | Decision / required verification |
| --- | --- | --- |
| Relevant Stats In Description, inspected1.6 | Original package check, reflected PowerLevels getter, signed Vector2 range, cached descriptions; ignores source custom overrides | One shared S052/S053 narrow consumer adapter for actual effective values, refresh and phase semantics; no facade-only/package-spoof claim;1.7 admission pending |
| RePower Patch Pack, pinned source metadata/XML | Hard source-package dependency plus RePower.RePowerDef class/targetDef/signed pairs; real reservable declaration | Unchanged pack unsupported; actual producer adaptation needs its own admitted loading contract, not duplicate aliases or downloaded-file edits |
| EPOE Forked historical1.0–1.4 XML | Old display-name FindMod plus RePower named-definition replacements/removal | Historical binding only; no inferred beta support |
| RePowered settings/HugsLib | Global keys and negative custom Vector2 dictionary; automatic destructive source importer | No automatic import or file mutation; future migration needs backup/atomicity/restoration/postconditions |
| Other public Utility methods/sets | AddBuildingUsed, initialization/rebuild APIs, mutable sets and alias | No further consumer found in bounded installed scan; do not recreate a speculative source API surface |

Exact runtime APIs/provider identities and evidence hashes are retained in research/design. No whole
source-mod drop-in claim. Original LightsOut/RePowered/RePowerReborn/Turn It On and Off suppress the
combined owned group with a warning, leaving provider patches intact.

## Implementation and coverage

**Nothing implemented.** RP01–09, guards, translations, provider adapters, focused RED/GREEN and
native acceptance remain pending. Deliberate deviations include working reservation/schedule
declarations, actual-use ordinary equipment, both door directions, ordinary medical beds, world-view
updates, incremental forecast/equality, preserved absent-type settings and explicit absolute-rate
semantics. Rate-only external conflicts are explained rather than last-definition-wins.

VVE/Doors Expanded/PRF/self-lit adapters remain conditional on actual1.7 content/shape proof. Their
source reference entries do not promise every machine is supported. Unknown continuous equipment
stays native. Declarative adaptation uses package/content/node guards and merge-preserving XML
Extensions; its first use requires the real dependency. Harmony is required with admitted Harmony
code. DLC/content/description integrations remain optional. No Gateway or repository-mod dependency.

## Verification and acceptance

Strict validation passed75items, zero failed;14 scoped documents passed local link/whitespace
checks. Independent review's three P2 findings were fixed, and focused re-review found no issues:
positive sub-floor readiness, per-network forecast accounting and failed-group versus intentional-
disable fallback. RP01–09 contain51scenarios and28unchecked tasks. See the
[sequential ledger](../refinement/specification-progress.md). No game/build/test run occurred for
this documentation slice. RP-Q1–4 retain effective targets, actual optional bindings, balance and
implementation/native acceptance. Source inspection proves neither compatibility nor performance.
Final workflows must demonstrate normal work, power deficits/recovery, doors/targets/crops, shared
facilities, all-map state, settings/save scope and admitted provider/consumer behavior on the reviewed
build, with personally inspected before/action/after evidence and cleanup under root AGENTS.md.
