# Priority Treatment

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`),1.7 beta.
Origin replacement S134. Dreadarm.PriorityTreatment, [Workshop3813351382](https://steamcommunity.com/sharedfiles/filedetails/?id=3813351382),
version0.1.22; installed1.7 assembly SHA256
`aec9d8db18a1e080ce42f9baac584aecb654d65a2f083b084078c6abb3570f1a`.
Read-only installed metadata, complete compiled behavior, public guide/API and native1.7 inspected
2026-11-07; [research](../refinement/2026-11-07-priority-treatment-research.md) gives all input hashes,
paths, exact source/native findings, license and uncertainties. All rights reserved: independent
behavior specification, no source/binary/art/translation copied. No runtime compatibility claim.

## Behavior inventory

All rows point to the owned [observable spec](../../../../openspec/changes/specify-megamod-priority-treatment/specs/megamod-priority-treatment/spec.md).

| ID | Player-observable result | Defaults, edges and evidence | Status |
| --- | --- | --- | --- |
| PT01 | Ready priority1 doctor responds without abandoning orders/care | Wake/meals on; rest<=10% or starvation blocks; injured patient-doctor only rescue/urgent; source readiness/native inspected | Specified, unimplemented |
| PT02 | Native rescue/tending composed with S058/S062 | Imminent danger before routine rescue/tending; equal urgency rescue first; routine sickness off, urgent treatable disease included; valid slots/permissions | Specified, unimplemented |
| PT03 | Injured doctor uses actual operation capability | No blanket injury/scar veto or corresponding setting; native risks and permissions retained | Specified, unimplemented |
| PT04–05 | Incapacitated dependent patient gets one real coordinated dose | On; policy, access, ordinary costs,15 bills; shared S124 reasons/holds/actual consumption | Specified, unimplemented |
| PT06 | Eligible patient receives ordinary quill operation | On with supported Odyssey; visible quills,600 base work, no ingredients/anesthesia in inspected recipe; patient stop/resume | Specified, unimplemented |
| PT07–08 | Common configuration, contextual status and safe startup/load | Six common controls, no bottom main tab, preserved foreign state and bounded fallback | Specified, unimplemented |

## Interactions and composed result

S124 DR09–10/13 owns joint drug orders; S134 provides an incapacitated-dependency reason with explicit
precedence and biological trigger, not a parallel bill queue. S058 ranks within selected care;
S062 owns condition permissions, medicine, operation inventory and necessary field care; S042 owns
self-tend permission. S080 stabilization retains its own admission. S007 and owned bill view host
Health controls; S137 filtering never schedules surgery. Native/shared candidates are reused with
fresh action checks. No source-on-source patches survive as patches against owned code.

## UI audit and relocation

| Source entry | Purpose | Owned destination and difference |
| --- | --- | --- |
| Seven global ModSettings checkboxes | Configuration | Health / Priority treatment, six common global/save/Use global controls; injury-veto setting omitted |
| Enable checkbox | Current behavior plus configuration | Inherited initial default and separate saved bottom-right operational switch |
| Native automatic bills | Patient work | Native Health/bills plus shared reasons, restriction explanations and explicit Resume |
| No source main tab or debug UI | None | No new main tab; any developer diagnostic stays top debug |

Loaded-game Use global stays editable; save-value editor inactive while inheriting. Main-menu save
controls/callbacks disabled, global editor usable. New saves inherit, globals remain live, dormant
overrides persist. Defaults affect future decisions; disable withdraws only owned unstarted reasons
and does not abandon carried patients. Patient holds/choices are entity state, not settings.
Seven-language layout/input verification remains pending.

## Contracts consumed by other mods or saves

| Contract / evidence | Disposition and verification boundary |
| --- | --- |
| Public Api.V1 ContractVersion2, three bool policy/readiness methods | Documented; package+assembly+type binder prevents automatic drop-in. Ability Loadout named by author, absent in installed metadata; actual consumer integration deferred |
| WorkGiver/ThinkNode/Def identities | No verified installed consumer; no source identity spoof or broad facade selected |
| Seven global settings keys | No automatic import; owned common settings use their own identity |
| Native medical bills, source workgiver job references | Native bills preserved; source removal/save safety not claimed without exact native tests |

Migration, if later selected, needs backup, atomicity, failure restoration and checked postconditions.

## Implementation and coverage

**Nothing implemented.** [Proposal/design/tasks](../../../../openspec/changes/specify-megamod-priority-treatment/)
describe future work. Deliberate deviations: shared dose authority, cancel holds, current scheduling
eligibility, nearest eligible supplies, S058/S062 composition, common UI and guarded admission.
No source license assets, source-setting migration, universal API facade or arbitrary surgery
automation. Original source co-installation withholds owned overlap; unsupported external dose
ordering remains uncoordinated and is conservatively withheld per S124.

Runtime requires Core, Harmony and XML Extensions for guarded additive scheduling XML; Biotech and
Odyssey optional. Check package/content/node shape, preserve siblings and avoid duplicate addition.
No other repository mod or Gateway dependency. Stable target docs, executable admission, tests and
live evidence are future tasks, not satisfied by this research.

## Verification and acceptance

Documentation review/strict validation tracked in the [progress ledger](../refinement/specification-progress.md).
PT-Q2–6 remain: balance, consumer/save bindings, effective targets/optional content, shared lifecycle,
performance and runtime acceptance. Future focused RED/GREEN, package/review, fresh minimized
Gateway native workflows and personal causal screenshot inspection must show physical care,
consumption/effects, cancellation/hold/resume and UI/save behavior on the reviewed build.
Semantic balance pass must compare urgency-first rescue/tending, actual doctor capability,
ordinary costs and actual patient/job reachability. No gameplay task has been checked.

## Reconciled policy — 2026-11-07

Our contract uses imminent danger before routine rescue/tending and actual doctor capability instead
of a blanket injury/scar veto. It has six common settings; the injury-veto control/patch is omitted.
S058 shares urgency; S062 supplies field care for the selected casualty. Source descriptions above
remain provenance wherever they describe the rejected original ordering or restriction.
