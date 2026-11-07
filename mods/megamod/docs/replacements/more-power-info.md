# More power info

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin **replacement**, S102/P2-32. Source **arl85.MorePowerInfo**, author **arl85**,
[Workshop 2906529763](https://steamcommunity.com/sharedfiles/filedetails/?id=2906529763).
Inspected 2026-11-05. Installed 1.7 DLL **1.3.0.0**, SHA-256
`7FE7C4AEAE3DC74693BB8C46FC4FB9887F1A01922129152303F37FB5B0623236` matches the committed binary at
[author revision 7a837a8](https://github.com/angelolocritani/Rimworld-MorePowerInfo/tree/7a837a8b869bf72694e6b2e3745795e1df0565e5).
About supports 1.4–1.7 and requires Harmony; selected route is 1.7 plus root. No reuse license was
identified; independently implement, retain provenance and bundle no source code/assembly/image.

[Research](../refinement/2026-11-05-more-power-info-research.md) distinguishes public README/Steam
evidence (after an initially failed fetch), complete source/binary inspection, native evidence, inferences and open
questions. All behavior below is **specified only**; no implemented replacement or compatibility.

## Behavior inventory

| ID | Input → observable result | Source/options and owned deviation | Contract | Status |
| --- | --- | --- | --- | --- |
| MPI01 | Select a connected object → current circuit production, usage, balance and reserve | Source overwrites base network text; owned additive section preserves native/foreign text and current-load scope | [Inspection](../../../../openspec/changes/specify-megamod-power-inspection/specs/megamod-power-inspection/spec.md) | Draft |
| MPI02 | Inspect charged circuit → current-rate reserve estimate | Source hardcodes50% charging/5 W native loss and 0.6 W deadband; owned admitted actual efficiency/loss, full/empty/unknown states and no outage guarantee | Same spec, MPI02 | Draft |
| MPI03 | Read storage-only comparison → hypothetical duration without generation | Current loads held fixed; safe zero/finite/long-period presentation, no generator switching | Same spec, MPI03 | Draft |
| MPI04 | Switch/work/reconnect/load → refreshed actual information | Native membership and owned demand once; explicit outage/debug/EMP handling, no simulation mutation | Same spec, MPI04 | Draft |
| MPI05 | Co-install or changed semantics → one provider or isolated fallback | Original presence suppresses owned overlap; compatible changes admitted; totals survive forecast-only failure | Same spec, MPI05 | Draft |
| MPI06 | Inspect in any baseline language → readable contextual information | Source has English-only keys; owned seven-language labels/qualifiers, no new settings/tab | Same spec, MPI06 | Draft |

## Interactions and composed result

[Lights Out](lights-out.md) and [RePowered](repowered.md) own activity, draw and prospective work
admission. This readout consumes their actual resulting network values, never adds a second rate
multiplier or uses a pending-start reservation as current consumption. Direct owned composition
replaces any source-to-source patch stack. Reuse native PowerNet lists and shared power projection;
there is no new map Thing index. Common readable-number services supply presentation where relevant.

## UI audit and relocation

| Source surface | Purpose | Owned destination / difference |
| --- | --- | --- |
| CompPower inspection postfix | Read-only circuit information | Same ordinary inspection surface, additive contribution preserving other details |
| Derived battery/trader inspection | Native device information around base call | Keep capacity, efficiency, self-discharge, own draw/output, fuel and faults |
| Bottom tabs, windows, gizmos, settings, custom save fields | None in selected source | None introduced; selection is contextual UI state, not configuration |

No common-settings inheritance exception is needed because the feature introduces no setting.
All visible text uses the seven baseline languages and actual narrow-pane acceptance. No raster
resource, bottom-right toggle or debug control is necessary.

## Contracts consumed by other mods or saves

| Evidence / binding | Policy | Required proof |
| --- | --- | --- |
| Bounded installed `.dll/.cs/.xml` ordinary-string scan found only source-owned matches for package/type/key | No consumer facade or source identity spoofing justified; scan is not a complete UTF16/reflection proof | Reinspect any discovered real consumer before promising a drop-in contract |
| Source owns no settings or gameplay save data | No upstream settings/save migration | Save/load verifies stale display references only |
| Original package active | Source owns readout; suppress owned overlap with explanation | Separate exact original-present native inspection run |
| Other providers change native-compatible values | Read admitted actual values without package-name whitelist | Normal native plus variant capacity/efficiency cases |
| Custom battery/power model changes consumed semantics | Narrow package/shape adapter or unavailable forecast; reliable totals remain | Absent/supported/changed cases and per-contribution admission |

## Implementation and coverage

**Implemented: nothing.** Source inspection-text replacement, universal 50% efficiency, hidden 0.6 W
zeroing, unbounded integer period conversion, debug allocations and unguarded PatchAll are deliberate
omissions. Full-with-surplus is an explicit current state, not a precise lifetime calculation.
Future weather/fuel/work/load-shedding prediction, battery balancing and whole-game power-mod
compatibility are outside this feature. Core required; Harmony required with the hook; no DLC or
external power provider required. No XML proposed; XML Extensions becomes required if actually used.

The [design](../../../../openspec/changes/specify-megamod-power-inspection/design.md) records
guarded additive integration, read-only snapshots, bounded calculations, independent failure groups,
shared ownership and lifecycle. [Tasks](../../../../openspec/changes/specify-megamod-power-inspection/tasks.md)
remain unchecked. **MPI-Q1–3** in the [ledger](../refinement/specification-progress.md) retain exact
contracts, numerics/cost and implementation/native proof.

## Verification and acceptance

Only public/source/binary/native static research and document validation are available at draft
time. Focused RED/GREEN, actual build/package, independent implementation review and final native
selection/switch/work/charge/reconnect/load/provider/language workflows remain required. Finalized
battery values and unchanged power/fuel/work balance must be measured on the reviewed build.
Logs, loaded patches, a synthetic energy ratio or a prepared screenshot alone are not acceptance.

The documentation draft passed independent behavior and provenance reviews. The latter identified
a useful native residual-transfer detail, now explicitly covered and re-reviewed without findings.
Typed strict OpenSpec validation passes **124 items**. The draft contains **6 requirements,
26 scenarios and 15 unchecked implementation tasks**; this is not runtime acceptance.
