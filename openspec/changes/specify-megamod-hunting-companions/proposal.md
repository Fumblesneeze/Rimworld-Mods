## Why

A hunter's bonded, trained animals should accompany the hunt and help recover its prey. One animal
can rush to a downed target, finish it and retrieve the actual corpse, while the hunter and other
companions avoid duplicating that work.

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
**Origin: original**, S065/P1-60. **Specification only; no gameplay delivered.**
The user rejected Hunt for Me and selected this custom behavior. Provenance is the
[interview refinement](../../../mods/megamod/docs/refinement/2026-11-01-handwritten-notes.md).
[Research](../../../mods/megamod/docs/refinement/2026-11-04-hunting-companions-research.md) separates
those decisions, inspected native behavior and proposed defaults/questions HC-Q1–5.

## What Changes

- Permit all qualifying bonded animals with ordinary Attack + Haul training to accompany their
  hunter. Add no Hunting training, dog-only restriction, speed bonus or pack-inventory capacity.
- Keep companions near the hunter using S066's separately specified directional wandering, with
  purposeful regrouping and normal needs/orders taking precedence.
- Recognize the active hunter's actually shot quarry when it becomes downed or dies, including
  supported delayed collapse, and give one currently eligible companion its finishing/retrieval claim.
- Separate finishing from haulability. Proposed finishing uses the animal's actual melee attacks;
  normal damage/body condition remains, and dangerous melee-death prey stays with ordinary ranged
  hunter handling. A real destination and current native/composed capacity are required for hauling.
- Coordinate human/animal reservations, interrupted handoffs, recovery, save/load and cancellations
  with S064 stations, native hunting, S021 hauling and S049 storage limits.
- Preserve native animal-management controls as the proposed permission surface and add contextual
  eligibility/activity explanations. Common tuning uses inherited global/save settings.

Master/follow permission, exact shot attribution, claimant selection, finishing policy, follow
distance and assistance deadline are proposed choices, not additional interview confirmations.
S066 owns the wandering algorithm and pen/room extension. S008 drafted party hunting keeps its own
finishing policy; this change does not silently send animals to every drafted combat target.

## Capabilities

### New Capabilities

- `megamod-hunting-companions`: bonded eligibility, accompanying, shot-prey handoff, animal finishing,
  physical retrieval, cancellation/recovery, contextual controls and compatible fallback.

### Modified Capabilities

None in the archived baseline. Existing drafts' ordinary priorities, physical quantities and
capacity remain authoritative. S064's declared companion handoff is refined by this separate change.
S030's recovery draft distinguishes a companion's temporary following suspension from an animal
that is still actively following, preserving its existing thresholds and saved permission.

## Impact

Future owned job/intent state, animal decision routing, guarded damage/death/hunting integrations,
UI text and localization belong to megamod. No source API, source package impersonation or source
save import is needed. Ordinary supported native wildlife/training/weapon/storage inputs participate;
custom executable providers need narrow optional adapters and separate acceptance.

Harmony becomes required when eagerly used. XML Extensions becomes required at the first actual
XML patch; native animal decision trees must be extended additively with presence/shape guards, not
replaced wholesale. No DLC, rejected Hunt for Me, Pathfinding Lab or Dev Gateway runtime dependency.
No art is created or selected here. All implementation/TDD, effective target documents, independent
implementation review, balance and final native acceptance remain open.
