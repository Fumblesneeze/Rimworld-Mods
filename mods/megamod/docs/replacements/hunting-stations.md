# Original feature: hunting spot and raised seat

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
**Origin: original**, S064/P1-58–59. No source mod/package/Workshop/API/license or legacy save migration
applies. User notes/interview specify physical waiting, range-only detection initially about40 cells,
departure to hunt and a grouped map-specific species filter including absent possible species.
Native1.7.4998 rev1299 was inspected2026-11-04, assembly SHA256
`FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B`.

Status: user/native/comparator research and observable draft independently reviewed and strictly validated.
**Nothing implemented or verified in game.**
[Research](../refinement/2026-11-04-hunting-stations-research.md),
[proposal](../../../../openspec/changes/specify-megamod-hunting-stations/proposal.md),
[design](../../../../openspec/changes/specify-megamod-hunting-stations/design.md),
[spec](../../../../openspec/changes/specify-megamod-hunting-stations/specs/megamod-hunting-stations/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-hunting-stations/tasks.md).

## Behavior inventory and coverage

All rows are **specified, not implemented**. HS IDs refer to the owned observable contract.

| ID | Player workflow and result | Boundary / proposed choices |
| --- | --- | --- |
| HS01 | Place a free marker or build a raised seat → actual waiting position | One cell/one hunter; provisional50-Woody/900-work seat, normal comfort/quality, no elevation attack bonus |
| HS02 | Open species chooser → possible map wildlife, including absent species | Multi-biome/coastal/pollution/habitat/arrival and revealed-observation knowledge; no hidden quest/animal disclosure |
| HS03 | Select species in grouped contextual filter → that station's authorization | Predator and small/big categories, proposed adult-size cutoff1.0, initially exact-zero retaliation species selected, later new species disallowed |
| HS04 | Eligible Hunting worker uses station → walks and waits | Manual Hunt precedence, ordinary priorities/needs/equipment, no free experience or forced work |
| HS05 | Prey enters radius → one current target claim | Default40-cell circle, no station LOS, revealed/allowed wild prey, actual access and legal exit, no public automatic Hunt marks |
| HS06 | Hunter leaves → ordinary hunt and physical corpse handling | Native weapon/retaliation/5000-tick pursuit/180-tick safe finish; actual capacity/destination, no duplicate yields |
| HS07 | Hunt finishes/fails → ordinary reassessment, return or other work | Proposed station/prey retry delay1 game hour, no stale forced return or timeout reset |
| HS08 | Pause/filter/edit/remove/reload → revoke stale intent and preserve state | Current target permission, saved phase, actual cargo and minified-seat choices retained safely |
| HS09 | Tune radius/grouping/retry → common global/save settings | Use global/dormant overrides/main-menu guards; entity choices remain contextual |
| HS10 | Compose owned and optional behavior → one authorized actor/load | Separate S008/S065/S066 scopes, shared data/hauling, minimal admission and explained fallback |

## Selected custom decisions and interactions

The user did not choose a precise small/big threshold, seat cost, target/return policy or retry delay.
Those are explicitly proposed and tracked in HS-Q1–5. The seat's50 material/900 work/150HP/mass6 and
stool-like comfort are derived from static basic-furniture comparisons, not finalized balance.
Neither station changes wildlife generation or makes hunting safe. Detection can see around walls
only in the sense of knowing eligible revealed prey is in range; the hunter still leaves and uses
ordinary legal movement/fire. Initial range does not tether an admitted chase or extend a weapon.

S008 party hunting remains drafted pawn state with its own visibility/range/finishing rules; drafting
ends station assignment. S065 will own bonded Attack+Haul companion selection and post-shot handling,
S066 its directional wandering. Both must share current prey/corpse claims without internal patches;
neither future feature is implemented by this station draft. Hunt for Me remains rejected.
S065's later handoff refinement permits the human to reassess/return once the companion takes
responsibility, before animal delivery; the delegated job still obeys its originating station's
filter/pause/removal rules even when the hunter acquires another target.
S021/S049 preserve physical capacity and destination/refill limits. S033 and native listers provide
common facts; each station does not get its own whole-map scan/cache. Existing construction/quality/
replacement/orientation/repair/access/clearing contracts also apply to the proposed seat.
Its positive-work blueprint follows S028 rock/fog planning and reveal validation, including S063's
deferred first clearing; it provides no watching/detection before valid construction. The instant
spot still requires revealed legal ground.

## UI and settings audit

| Surface | Purpose | Selected placement |
| --- | --- | --- |
| Architect station placement | Actual building/marker mechanic | Existing Architect hierarchy, integrated with its owned organization |
| Station selected filter, pause control, range and status | Entity gameplay choices and observation | Native contextual inspect/gizmo/filter surface; no station settings rows or bottom tab |
| Work tab Hunting priority | Worker job allocation | Existing native work type/column |
| Radius40, size cutoff1.0, failed-hunt retry1h | Configuration | Common Hunting / Stations settings with Global default, Current save, Use global |

Global edits affect inheriting saves at the specified next acquisition/grouping/new-failure timing.
Overrides preserve dormant values and never mutate another save. At the main menu all save editors
and mutation callbacks are disabled. Station filters/enabled state are saved with each entity;
observed species belong to that map. Reinstallation keeps choices but not claims, and a newly placed
marker selects exactly-zero-retaliation map-possible species (empty only if none qualify). Seven languages and all real UI workflows remain implementation work.

## External contracts and omissions

There are no demonstrated upstream consumers to preserve for an original feature and no package
identity to spoof. Compatible native data from installed wildlife/weapon/map providers is expected
input; exact custom APIs require package/shape admission and separate provider-present acceptance.
Possible-species catalog coverage is explicit when arbitrary scripted generators cannot be described.
Missing DLC is absent-safe. There is no hard dependency on another repository mod, experimental
pathfinding, rejected Hunt for Me or the Dev Gateway.

No automatic butchering, new animal training, universal quest-spawn prediction, all-provider
compatibility, multiplayer certification, save import or safe-uninstall claim is made. Actual XML
patches require XML Extensions presence/shape guards and merge preservation at first use. No raster
asset was created/selected; future station art follows the required asset/live visual gate.

## Verification and remaining work

HS-Q1–5 in the [ledger](../refinement/specification-progress.md) retain proposed policy/balance,
species-rule completeness, grouping/UI/art, effective native/adapters and composed acceptance.
Focused honest RED/GREEN, semantic target documents, package checks, independent implementation
review, actual material/quality comparisons and personally observed native placement/configure/
wait/detect/depart/hunt/corpse/return/cancel/reload workflows remain open. This draft's source reads
and schema checks do not prove a functioning station, performance improvement or compatibility.

Documentation checkpoint:10 requirements,42 scenarios,31 unchecked tasks. Native review found no
issues. Composition review corrected the seat's fog-planning conflict with S028; the resolution
check found no remaining issues. Typed strict validation passed86 items with0 failures. No runtime
task is closed by these document checks.
