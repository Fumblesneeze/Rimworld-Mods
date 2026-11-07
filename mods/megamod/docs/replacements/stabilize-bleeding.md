# Stabilize Bleeding

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Origin replacement **S080/P2-09**. Source **Defi.StabilizeBleeding**, Defi,
[Workshop3521178066](https://steamcommunity.com/sharedfiles/filedetails/?id=3521178066), advertises1.6.
Inspected2026-11-04: DLLStabilize1.17.0.0 SHA256
`1A32A748F7873638B68BE115D44229395FB79CD5F6C238CC4A8AEC9119439616`.
Public documentation, full small binary, JobDef/keys and current1.7 native medical paths inspected.
No source/license distributed in the inspected root; no reuse rights inferred or assemblies copied.
Do not substitute the older **Stabilize**, Workshop2023407836, `Linkolas.Stabilize`.

Detailed provenance, discrepancies and remaining questions:
[research](../refinement/2026-11-04-stabilize-bleeding-research.md).
[Proposal](../../../../openspec/changes/specify-megamod-stabilize-bleeding/proposal.md),
[design](../../../../openspec/changes/specify-megamod-stabilize-bleeding/design.md),
[observable spec](../../../../openspec/changes/specify-megamod-stabilize-bleeding/specs/megamod-stabilize-bleeding/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-stabilize-bleeding/tasks.md).

## Behavior inventory and coverage

Every row is **specified only; no implemented portion, test or native acceptance evidence**.

| ID | Input/action → observable result | Selected boundary |
| --- | --- | --- |
| SB01 | Contextual/queued order → in-place bleeding-only treatment | One doctor, drafted or undrafted, no bed/medicine fetch required |
| SB02 | Doctor/patient selection → current medical eligibility | Medical1 for ordinary doctors, permitted mutant path; downed outsiders/animals and eligible mobile colony patients; actual manipulation/self permission |
| SB03 | Treat → greatest permitted current bleeding first | Include fresh missing parts/modded bleeding; exclude NoCare/non-bleeding/untendable; stop no-progress loops |
| SB04 | Actual work → ordinary unmedicated result | 600/speed ticks, native quality baseline0.3 potency/0.7 cap/self0.7; no supply consumption or instant cure |
| SB05 | Cancel/move/change orders → local cleanup | Bed retained, only owned wait/reservation released, no unrelated replacement-job cancellation |
| SB06 | Save/load → valid remaining work | No repeated completed treatment/credit or automatic foreign-job import |
| SB07 | Owned medical composition → one explicit action | Individual care and self permission, no errands, CE temporary stabilization distinct |
| SB08 | Source active or unsupported seam → owned action inactive | Bounded diagnosis, ordinary/provider care remains, no foreign unpatch or identity spoof |

## Deviations and possible conflicts

- The source checks whether bleeding exists but calls general native finalization without filtering
  candidates. The current1.7 routine can choose a dangerous non-bleeding disease first. We select
  its advertised bleeding-only intent, not this source-like port result.
- Menu only accepts bleeding injuries while driver also accepts other bleeding conditions; include
  fresh missing parts and compatible treatable bleeding. Avoid endless attempts on untreatable or
  unchanged conditions and unearned completion credit.
- Retain source actual wider patient scope and Medical1 floor; its description is narrower. Add
  current manipulation/medical eligibility, respect S062 NoCare and self permission throughout,
  reject aggression consistently instead of a menu-only Scaria exception. These are explicit draft
  choices; SB-Q1 retains policy/balance review.
- Source generic wait cleanup and unsaved path mode are not copied. Preserve the actual current
  order and position across interruption/load; do not cancel a newer patient wait or evict bed rest.
- No faster tending, instant removal of blood loss, guaranteed survival, automatic job giver,
  source settings/save migration or holding-platform entity treatment selected.

Direct owned composition: S007 health presentation; S024 emergency errands suppressed; S042
self-tend permission; S058 automatic ranking yields to explicit order; S062 current care and native
medical consequences. This action chooses its own bleeding-only batch/ranking without overriding
ordinary care globally. CE temporary stabilization remains deferred research, with no initial
adapter or shared-provider treatment claim. S124/S134 later analyses resolve their own drug/triage behavior.

## UI audit and settings

| Source entry | Function | Owned destination |
| --- | --- | --- |
| Right-click provider | Order actual treatment of one patient | Native patient context, explicit without-medicine wording |
| Job/progress report | Observe real work | Native job report/progress and Health results |
| No settings/tab/gizmo | No source control to reproduce | No invented configuration or bottom tab |

Patient order/progress is gameplay state, not configuration. There is no new setting or inheritance
exception. Later tunable policy must amend the spec and use common global/save/Useglobal handling.
All seven languages cover action/report/rejection text; technical IDs and log-only diagnostics stay
invariant. Native main-menu/settings controls are unaffected by this feature.

## External contracts, persistence and dependencies

Source assembly/namespaceStabilize, publicBase/provider/DefOf and unprefixedStabilizeHere JobDef are
shared-looking names with the older separate provider. Bounded text and binary-symbol search does
not establish a real incoming consumer or prove none exist. No fake source package, DLL or aliases
are justified. Source in-progress jobs remain foreign, with no automatic conversion/removal promise.
Owned jobs need tested patient identity/remaining-work serialization and invalid-job cleanup.

OriginalStabilizeBleeding active => owned overlap off; provider remains untouched. Other medical
providers need exact optional package/shape resolution and supported native workflows. Base action
requires Core only; Harmony required if used, XML Extensions required if guarded XML patches are
used. An owned static JobDef is declarative; no destructive list patch or new map cache is proposed.
Every actual patch needs executable effective-semantic admission and a stable target-spec document.
No product Gateway or other repository-mod dependency/reference/load-order hint.

## Verification status

SB-Q1–4 retain explicit policy/balance, effective targets/lifecycle, real consumers/providers and
implementation/native acceptance. Scoped document review/schema results belong in the
[progress ledger](../refinement/specification-progress.md). All runtime tasks remain unchecked.
Final proof must show actual menu order→doctor work→selected health change, unchanged unrelated
conditions and supplies, cancellation/save/queue/rejection/provider cases and localized UI on the
reviewed build. Static source inspection or job construction is not that proof.
