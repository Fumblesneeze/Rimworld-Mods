# LightsOut

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement, **S052 / P1-46**. Source **LightsOut**, `juanlopez2008.LightsOut`, author
juanlopez2008, [Workshop2584269293](https://steamcommunity.com/sharedfiles/filedetails/?id=2584269293).
Inspected2026-11-03, selected root net10 DLL version2026.10.17.738; SHA256
`3495CA55336EF70B2A224B33CC414D6ACC66690FBE206FEBF76C59927BED42A7`.
Pinned [source](https://github.com/NathanIkola/LightsOut/tree/59e32b423be6ae71fb99061168c86c1d89ef9a02),
no license grant found; independent implementation, no source assembly/artwork copied.
[Research](../refinement/2026-11-03-lights-out-research.md) records read-only paths, native evidence,
source/binary limitations, all17 adapter families and actual consumers.

Status: **analyzed, observable specification independently reviewed and strictly validated**. Nothing
implemented, built, installed or accepted in game. The [change](../../../../openspec/changes/specify-megamod-lights-out/proposal.md)
contains the [design](../../../../openspec/changes/specify-megamod-lights-out/design.md),
[spec](../../../../openspec/changes/specify-megamod-lights-out/specs/megamod-lights-out/spec.md) and
[unchecked tasks](../../../../openspec/changes/specify-megamod-lights-out/tasks.md).

## Behavior inventory

All rows are specified, not implemented; evidence is source/native inspection in the research record.

| ID | Input/action → intended observable result | Defaults and edge cases | Owning requirement |
| --- | --- | --- | --- |
| LO01 | Last relevant pawn leaves / another enters → ordinary indoor lamps follow occupancy | Tool-using pawns of all factions; doorway/outdoor and continuous-device exclusions | LO01 in the linked spec |
| LO02 | Sleep/wake → lamps follow all-asleep state | Sleep lightingOff, animalsOff; enabled animals participate in sleep/wake | LO02 |
| LO03 | Empty-room delay → shutdown only if still eligible | 1.5 simulation seconds plus at most0.5 scheduling latency; cancellation, delay change and transfers | LO03 |
| LO04 | Per-lamp Keep On → exempt only that lamp | InitiallyOff, saved entity state; native flick/power/color limits preserved | LO04 |
| LO05 | Bill/research/drill/TV use → active consumption until last use ends | Already-at-target jobs, cancellation, no-power eligibility | LO05 |
| LO06 | Native charging/DLC/provider work → activity follows real demand | Eight Biotech device policies, rechargeables, ritual speakers and admitted autonomous printers/vats | LO06 |
| LO07 | Set rates / device phase changes → one current consumption calculation | S053 rates for its admitted devices; otherwise standby0%, active100%,0.01 W floor capped to smaller positive native demand; no gas sign assumption | LO07 |
| LO08 | Change defaults/save override/live lighting choice → correct scope | Six common settings, bottom-right selector, no new bottom tab | LO08 |
| LO09 | Save/load/move/inspect → current state and truthful information | Keep On and delay persistence, native work reconstruction, additive text, no automatic source import | LO09 |
| LO10 | Content/provider conditions change → admitted coverage or explained native fallback | Source co-install suppression, merge-preserving XML, accurate admitted consumer display | LO10 |

## Interactions and composed result

S053 [Turn It On and Off - RePowered](repowered.md) supplies costs for its admitted equipment,
using S052 actual-use activity for overlaps. One calculation replaces stacked multipliers; S052
rates remain for other equipment and as fallback when S053 budgets are disabled. The rate controls
identify their scope. External LightsOut/RePowered/RePowerReborn/original Turn It On and Off
suppresses the combined owned power group with an explanation; no internal source-mod patch stack.

Owned bill/work/specialist features provide real activity directly. Search/hauling share derived
map data only when needed; native room/listers come first. No source-specific Thing-cache stack.
Room temperature, grazing/hydroponics, continuous management and ship movement must preserve their
ordinary service. The source's17 adapters are not17 new content mods we implement or require.

## UI audit and relocation

| Source entry point | Purpose | Owned destination / change |
| --- | --- | --- |
| Global LightsOut settings | Lighting/sleep/animals/delay/resource rates | Six ordered common global/save rows; no settings inheritance exemption |
| Global FlickLights checkbox | Live lighting automation | Bottom-right On/Off/Use configured selector; separate configured default |
| Keep On lamp gizmo | Individual lamp policy | Native contextual control, saved with lamp; no global/save settings row |
| Standby inspect replacement | Explain current state | Additive status/current consumption preserving other inspect text |
| Debug filter/spam options | Diagnostics | Bounded developer-only top interface; no recurring log-spam option |
| Bottom tab | None found | No new bottom tab |

Main-menu Current save and Use global are disabled, including callbacks. Dormant overrides,
save switching, world view, input preservation and seven languages require native acceptance.

## Contracts consumed by other mods or saves

| Consumer / evidence | Binding | Decision and verification |
| --- | --- | --- |
| Relevant Stats In Description, Mlie.RelevantStatsInDescription; selected inspected1.6 code | Old package detection, reflected power postfix and active-rate property; cached min/max display; external RePowered preferred | Narrow current-value consumer adapter when admitted, no package spoof. Beta content/shape, phase values and settings/research cache refresh remain LO-Q2 |
| WOWGAG's Performance Patch, wowgag.guiperformancepatch; selected1.7 code | Reflects LightsOut ModCompatibilityManager.GetTypesToPatch, skips absent type | Do not recreate obsolete startup scan/facade. Preserve unrelated behavior; narrow coexistence still unverified |
| Source saves/settings | keepItOn comp state, global keys and automatic HugsLib recovery | No automatic import or rewriting. Later importer requires separate backup/atomic restoration/postcondition contract |
| Public source Resources/Tables/Lights/Glowers APIs | State/registration/lighting calls and mutable public collections | Documented surface; no further installed binary consumer found. No blanket type/assembly/package drop-in claim |

## Implementation and coverage

**Nothing implemented.** All LO01–10, guards, XML, translations, host evidence and native acceptance
are pending. Deliberate changes: current/shared activity wins over stale callbacks; animal-only
sleep transitions work; native phase demand/research and zero-power refusals survive; active rates
apply consistently to listed equipment rather than only selected upstream class categories;
unknown continuous devices stay native; inspect text is additive; controls use common scope rules.

The source's electrical standby floor is retained. Legacy gas consumption, missing canonical
provider identities, current ceiling/glower/heat/ship behavior and source API consumers require
specific admission, not optimistic compatibility claims. Planned XML adds only needed components
and preserves other entries using XML Extensions guards; Harmony and XML Extensions become hard
runtime dependencies with their actual use. Ideology, Biotech and external content remain optional.
Do not copy foreign assemblies or introduce a product dependency on repository mods/the Gateway.

## Verification and acceptance

Typed strict validation passed 74 items with no failures; all eight scoped documents' local links
and whitespace checks passed. Independent review resolved two P2 findings: new/missing live choices
now use configured values, and wall-lamp facing-room behavior has an observable scenario. Focused
re-review found no issues. That original pass covered 10 requirements, 53 scenarios and 28 unchecked
runtime tasks. The subsequent S053 composition adds two scenarios (55 total); its scoped review and
validation are recorded in the sequential ledger. LO-Q1–4 retain effective beta/native policy,
optional/consumer and composed-policy verification, plus live UI/lifecycle and causal acceptance.
No runtime test is implied by decompilation.

Semantic pass: savings change power economics but not production speed/yields, research, resource
delivery or occupant safety. Preserve native availability and0.01 W compatibility floor; compare
actual lamp, bench and autonomous-processor behavior on the reviewed build before accepting balance.
