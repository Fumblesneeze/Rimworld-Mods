# Bulk Load For Transporters

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target1.7 beta.
Origin replacement **S133**, `Ilarion.BulkLoadForTransporters`,
[Workshop3559292908](https://steamcommunity.com/sharedfiles/filedetails/?id=3559292908), Ilarion.
Installed flat assembly1.0.0.0 SHA256`aa46929c3cfe4e27528f6fbf877f160b3e18783f1ed1349e7a4f408bfb4a19e1`,
advertises1.6/1.7. Inspected2026-11-07. Public MIT license credits Ilarion2025; no public commit pinned.
[Research](../refinement/2026-11-07-bulk-load-transporters-research.md) records all73 code files,
JobDefs/settings, native beta intersections, exact hashes and bounded consumer search.

## Behavior inventory and coverage

All rows **specified only**; implemented portion **none**, no runtime or compatibility acceptance.
[Observable spec](../../../../openspec/changes/specify-megamod-bulk-transporter-loading/specs/megamod-bulk-transporter-loading/spec.md).

| ID | Input/action → selected result | Defaults, corrections and remaining proof |
| --- | --- | --- |
| BTL01 | Native pod/shuttle selection/edit → exact requested cargo | Variants and actual progress preserved; broader native quest requests retained |
| BTL02 | Worker picks several portions → one real allowance | S021 gear/supplies/hands mass and volume, current eligibility; native passengers remain separate |
| BTL03 | Several current/queued workers → coordinated quantities | Native plus owned workers, map/request isolation, released commitments and no unreachable-shortlist starvation |
| BTL04 | Arrive/deposit/distribute → accepted amount once | Per-member capacity, exact group total, safe partial/refused/merged transfers;3tick shared handling plus native duration |
| BTL05 | Context menu → one batch or chosen-request continuation | Recheck stale clicks; useful free cargo first; no nearby-group drift or unsolicited entire-bag dump |
| BTL06 | Native-eligible carrier unload → fitting protected portions | Coordinated extraction, native10tick handling, specialized carrier fallback, real storage/recovery |
| BTL07 | Interrupt/save/reload/disable → physical cargo retained | No save-time mass cancellation; S021600tick unchanged-failure recovery; no stale boarding blockade |
| BTL08 | Configure/switch/inspect → proper scope and actual views | Common defaults all true, bottom-right operational states, contextual orders, actual inspector tabs, developer toolbar |
| BTL09 | Start with changed/original provider → safe fallback | Semantic admission and bounded warnings; no binary/source migration or unproved optional support |

## Intentional differences and omissions

- **Portal loading excluded by the user's explicit2026-11-07 decision.** No portal adapter, menu,
  batch job, automatic cargo redirect or patch. Existing game portal behavior is outside this feature.
- Vehicle Framework loading deferred: an advertised adapter was not found in the inspected assembly.
  Serialized enable flag and package detection do not establish support.
- No second inventory, independent index, capacity inflation, extra last-hand-stack allowance or
  cheat switch. S021 supplies authoritative capacity, quantities, protection and recovery.
- Exact selections replace per-type matching/claims. Safe accepted-quantity accounting replaces
  early/whole-stack credit; distribution cannot knowingly overfill a member to finish a plan.
- Continuous orders stay with the chosen request; S125 owns ordinary opportunities. Source90/150-cell
  scanner and misleading Unlimited label are omitted, as are duplicated planner tuning settings.
- Persist/reconcile cargo rather than canceling jobs every save; no broad cleaner dropping whole
  bag stacks or unsupported promise that removing the whole mod mid-save is harmless.
- Native permission/quest/boarding rules stay authoritative. S135 separately owns work-category
  changes. No research, resource, travel or equipment-capacity bonus is introduced.

## UI and configuration

| Source surface | Owned destination / change |
| --- | --- |
| Cargo selection and pawn context loading menu | Existing transport UI; single/until-complete entity orders |
| Carrier unload work menu | Existing native eligible-carrier context; not an arbitrary strip-inventory command |
| Enable batching settings | Common initial defaults plus compact bottom-right current operational switches |
| Contents/Gear auto-open settings | Ordered Hauling / Transport loading preferences; defaults true, next-selection effect |
| Planner/radius/delay/cleanup/mass-cheat settings | Omitted or consolidated into S021/S125 and safe owned workflow |
| Debug category toggles | Top debug toolbar, developer mode only |

No new bottom main tab. Common global/save/Use global behavior applies to real settings; loaded-save
Use global stays editable while inherited, only its value editor disables. Main menu disables all
save controls/callbacks. Current operational choices persist per save with reset-to-default; entity
requests remain gameplay state. Actual carrier Gear supports S132; S126 uses the same physical
cargo. Seven languages and readable native controls remain acceptance tasks.

## External contracts and composition

Source public loadable/state interfaces, scanner/factory lists and static registration event were
inspected; bounded installed search found no incoming consumer, not universal absence. No legacy
assembly/type/package facade or saved-job/settings import is promised. Outgoing PUAH calls become
direct S021 composition. Other owned hauling, supply and visual features share authoritative data;
no internal patches against each other. Current provider/worker contracts need exact evidence.

Original `Ilarion.BulkLoadForTransporters` active → owned overlap withheld with explanation. Foreign
patches are not inherently incompatible. Core/Harmony required with implementation, transport DLC
and specialized providers optional; unsupported paths remain native. The chosen design uses owned
JobDefs/native entry points without source comp injection. Any future XML patch requires XML
Extensions, package/content/node guards, merge preservation and matching metadata. All actual
patches require executable semantic target specs, dependent-group rollback and startup diagnostics.

## Verification

[Tasks](../../../../openspec/changes/specify-megamod-bulk-transporter-loading/tasks.md) are unchecked.
[Progress](../refinement/specification-progress.md) tracks independent document review and validation.
BLT-Q2–Q5 retain runtime admission, optional consumers, composition/performance and native proof.
Final reviewed-build acceptance must show actual selection, loading, carrier extraction, interrupted
recovery and completed native transport workflow, plus UI/settings/absence/fallback cases. No source
assemblies copied, game launched, installation changed or gameplay accepted during specification.
