# Original feature: bonded hunting companions

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
**Origin: original**, S065/P1-60. No selected source package, Workshop item, source license/API or
legacy migration. Hunt for Me was rejected. User provenance: all bonded Attack + Haul animals
accompany, one claims each shot/downed prey, finishes if needed, then hauls if destination/capacity
permit; finishing itself is independent of haulability.

Native 1.7.4998 rev1299 inspected 2026-11-04; assembly SHA256
`FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B`.
Status: user/native/comparator research and observable draft independently reviewed and validated.
**Nothing implemented or verified in game.**
[Research](../refinement/2026-11-04-hunting-companions-research.md),
[proposal](../../../../openspec/changes/specify-megamod-hunting-companions/proposal.md),
[design](../../../../openspec/changes/specify-megamod-hunting-companions/design.md),
[spec](../../../../openspec/changes/specify-megamod-hunting-companions/specs/megamod-hunting-companions/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-hunting-companions/tasks.md).

## Behavior inventory and coverage

Every row is **specified, not implemented**. HC IDs map to the observable spec.

| ID | Player workflow → observable result | Boundary / proposed policy |
| --- | --- | --- |
| HC01 | Train Attack+Haul and permit a bonded follower → eligible companion | All qualifying animals; no Hunting/AttackTarget trainable, breed cap or new bond |
| HC02 | Hunter begins native/station hunt → animals accompany physically | S066 directional idle movement, hunter root, needs/duties and actual cargo first |
| HC03 | Hunter hits current quarry, which collapses/dies → exact assistance opportunity | Effective hit, same active hunt, delayed collapse proposal; no collateral/old-corpse guessing |
| HC04 | Several candidates → one transferred prey/corpse claim | Nearest legal candidate, atomic handoff, fixed 2-hour pre-pickup budget and recovery |
| HC05 | Animal reaches downed prey → actual melee until valid completion | Normal damage/cooldowns/yield, recovery/protection stops, dangerous-death fallback |
| HC06 | Corpse exists → physical retrieval when possible | Native single-load versus pack capacity; S021/S049 actual limits and one indivisible corpse |
| HC07 | Hunter completes or either actor is interrupted → valid continuation or recovery | Natural handoff completion differs from player cancellation; no forced stale return or cargo loss |
| HC08 | Save/load → reconcile actual target/holder and permissions | No duplicate kill/load or refreshed deadline, missing/unsupported work stops safely |
| HC09 | Animal controls/settings → scoped permission and tuning | Native contextual choices; global/save availability/distance/budget, seven languages |
| HC10 | Other owned/provider features → one composed workflow | Direct calls/shared data; minimal admission, no all-provider/source drop-in claim |

## Proposed choices and interactions

Native master assignment and Follow during fieldwork are the proposed permission surface; existing
choices stay intact. This policy, 3-cell companion distance, 2-hour pre-pickup deadline, nearest
claimant, delayed-collapse attribution and normal-melee finishing remain HC-Q1–5 review questions.
They were not specified numerically or confirmed in the interview. All permitted bonded animals
can participate; no new Hunting or Odyssey Attack target ability is required.

A hauling dog with no pack inventory still has its normal single-load route. Actual melee can
leave a different corpse from careful human execution; no yield bonus or healed body is promised.
Finishing can occur without an accepting store or enough carrying capacity. One other eligible
companion can retrieve the corpse after the finisher releases it, never concurrently.

S064 station authority persists through handoff and cancels on its pause/filter/removal rules.
The human can resume ordinary work after handoff while the companion retains the original target.
For manual hunting, canceling a living prey's Hunt order stops delegated finishing; ordinary
designation cleanup at death leaves valid retrieval intact.
S066 supplies directional movement around the hunter; this does not implement the pen/room walker.
Its style toggle can fall back to admitted ordinary near-hunter movement without disabling
assistance or bypassing the shared needs/permission rules; no unproven fallback is promised.
S008 drafted hunting is separate. S021/S049 carry/demand rules, S033 common map facts, S030 recovery
while following is temporarily suspended, S126 real-cargo presentation and S117/S118 slaughter boundaries
remain explicit. Do not preserve an internal stack of patches between ports.

## UI audit

| Surface | Function | Selected placement |
| --- | --- | --- |
| Existing Animals and training controls | Assign master, enable fieldwork following | Native entity gameplay controls; no new tab or per-animal mod settings |
| Eligibility/current prey/retrieval/blocked reason | Explain actual companion behavior | Contextual animal/hunter inspection, no global alert spam or hidden attribution |
| Availability on, distance 3 cells, budget 2 hours | Configure feature policy | Common Hunting / Companions settings, Global default / Current save / Use global |

Save defaults inherit globals; dormant overrides survive; main-menu save controls and callbacks are
disabled. Numeric edits apply to subsequent decisions/opportunities, not fresh time on a held attempt.
Seven-language UI and all native workflows remain unimplemented. No raster asset is selected here.

## External contracts and omissions

No source facade, package impersonation, upstream settings/save import or source co-installation
guarantee. Compatible ordinary animal/weapon/training/storage data is input; unsupported executable
paths use canonical package/shape admission and explained fallback. Missing DLC is ordinary absence.
Hunt for Me, Pathfinding Lab and the Gateway are not dependencies. Runtime Harmony and actual XML
patches require their declared dependencies, semantic target checks and atomic fallback.

No general live-prey animal hunt, drafted combat retrieval, eating/butchering stage, extra speed,
damage, trainability, pack capacity, automatic bonding or safe-uninstall certification is included.
These omissions preserve the selected specific companion behavior; they do not narrow the separately
queued features. Ordinary native following, self-defense, training and animal needs remain.

## Verification and remaining work

HC-Q1–5 in the [progress ledger](../refinement/specification-progress.md) retain proposed policies,
effective native/provider and once-only outcome seams, movement/needs/hauling composition, contextual
UI and future acceptance. Focused RED/GREEN, reviewed package, material native workflows, real animal
attacks/cargo, save/interruptions, provider cases and finalized balance still need evidence. No
gameplay acceptance or optimization claim follows from this documentation draft.

The draft contains 10 requirements, 41 observable scenarios and 33 unchecked tasks. Independent
native and composition reviews reported no actionable findings. Strict OpenSpec validation passed
87 items with zero failures; local links and whitespace were checked across the 15 scoped documents.
Validation also reported an unrelated PostHog telemetry timeout; its process exit code was zero.
