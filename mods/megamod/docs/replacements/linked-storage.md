# Linked stockpiles and shelves

## Identity and provenance

- Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Origin: **original**.
- Intake: P1-06 / S005; user explicitly requested native shelf linking for stockpiles and mixed
  groups, including priority. No external source mod, Workshop identity or upstream license applies.
- Inspected 2026-11-02: local Core **1.7.4995 rev309**, `Assembly-CSharp.dll` SHA256
  `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21`.
- Authoritative input: `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll` and
  `Data/Core/Defs/ThingDefs_Buildings/Buildings_Furniture.xml`, inspected read-only. Native game
  code is proprietary inspection material; do not redistribute decompiled source or assemblies.
- Public-doc search for native shelf linking preceded code inspection; results were community
  summaries, not a precise 1.7 contract. The findings below come from the installed game instead.
- Local decompilation: `artifacts/Refinement/megamod-spec-20261102/linked-storage/game/`.
  `RimWorld.StorageGroupManager` was an unsuccessful type lookup; the correct inspected type is
  `Verse.StorageGroupManager`. No empty lookup file is treated as evidence.

## Behavior inventory

All rows are **specified, not implemented or live-verified**. Source inspection establishes native
behavior, not the outcome of the proposed extension.

| ID | Action → result | Native evidence and intentional extension | Specification |
| --- | --- | --- | --- |
| LS-01 | Select storage and link → first eligible member supplies policy | `StorageGroupUtility.StorageGroupMemberGizmos`; extend eligibility to stockpiles | [Link and source selection](../../../../openspec/changes/specify-megamod-linked-storage/specs/megamod-linked-storage/spec.md) |
| LS-02 | Edit linked member → filter and priority shared | `Building_Storage.GetStoreSettings`, `StorageSettings`, `ThingFilter.CopyAllowancesFrom`; zones currently own independent settings | Same spec, complete editable policy |
| LS-03 | Select member of another group → only selected member moves | `StorageGroupUtility.SetStorageGroup`, manager removal | Same spec, moving members |
| LS-04 | Unlink → retain copied policy | Native utility explicitly handles `Building_Storage`; zone preservation needs extension | Same spec, unlinking |
| LS-05 | Copy/paste → effective policy copied without membership | `StorageSettingsClipboard`, building/zone `GetGizmos` | Same spec, clipboard |
| LS-06 | Group identify/rename/select → discover linked storage | `StorageGroup` rename/label, utility overlay/selection; add zone visuals | Same spec, group recognition |
| LS-07 | Enable chunks → ground accepts, shelf rejects | Core shelf fixed filter differs from `Zone_Stockpile.EverStorableFixedSettings`; native first-member parent filter is insufficient | Same spec, physical restrictions |
| LS-08 | Resize/delete/reload → coherent membership and storage | Zone cell notifications, group Scribe, building map change/removal, `BillUtility.Notify_ISlotGroupRemoved` | Same spec, lifecycle; LS-Q1 remains |

## Interactions and composed result

Presets will edit the same effective policy. Kanban-style refill quantities will count the same linked
group, per item type, without changing physical capacity. Quick stockpile creation must produce a
normal independent zone unless its own eventual spec says otherwise. Search/material counts/hauling
will share candidate indexing, with actual destination restrictions checked at use. These are direct
owned feature interactions, not patches against our own implementation. Growing zones and linked
workbenches have separate state and owning changes.

## UI audit and relocation

| Native surface | Purpose | Destination |
| --- | --- | --- |
| Storage inspect tab, priority and filter | Entity policy | Keep; mixed group uses the same effective policy |
| Link/unlink/copy/paste/select-linked gizmos | Entity actions | Keep native contextual location; include zones |
| Group label/member count/connections | Group identification | Existing selection/inspect view, extended to zone cells |

No new bottom tab, debug action or global configuration is needed. This is gameplay entity state,
not a settings inheritance exception. The original stockpile and shelf default policies remain
unchanged until linking. Linking intentionally adopts the first selected member's policy.

## Contracts consumed by other mods or saves

| Inspected consumer | Contract | Intended treatment |
| --- | --- | --- |
| `ITab_Storage` | Single store or shared group; priority/filter and parent restriction; bill-validity warning | Extend selection and mixed fixed-filter display without bypassing individual admission |
| `StorageSettingsClipboard` | `CopyFrom`, including priority and full filter | Preserve native effective-policy semantics |
| Native hauling / `StorageSettings.NotifyChanged` | Priority/filter updates notify destination/source management | Preserve notifications and real member eligibility |
| `BillUtility.Notify_ISlotGroupRemoved` | Validates bills after member/group changes, except gravship generation | Preserve notification and ordinary unlinked fallback; S014 whole-list bills use explicit unresolved-scope recovery. Exact bill/gravship edges LS-Q1 |
| Native saves | Group map/member/settings/label references; zone and shelf settings | Inspect adapter persistence and removal before making save-compatibility claims, LS-Q3 |

Installed external consumers have not yet been audited (LS-Q2). There is no third-party source to
co-install or spoof. Optional storage extensions need canonical package and shape admission.

## Implementation and coverage

Nothing implemented. All behavior rows await focused tests, admission documents and live evidence.
Deliberate extensions are zone membership, zone selection/overlays, and mixed intrinsic restrictions.
No increase in capacity, shelf protection for ground items, automatic grouping or unrelated container
support is included. Core required; Harmony only if used; XML Extensions required on first use.

## Verification and acceptance

Future native workflows: both selection orders; full filter/priority edit; copy/paste; move and unlink;
chunk hauling to ground only; delete/resize/reload; bill destination and counts; map transfer and
optional providers only after admission. Use focused honest host tests for policy transitions and
fresh isolated game runs for selection, jobs and persistence. Record actual item movement, not only
group objects/logs. Balance pass: this changes management, not capacity, haul costs or item durability.
Questions LS-Q1–3 are in the [refinement ledger](../refinement/specification-progress.md).
