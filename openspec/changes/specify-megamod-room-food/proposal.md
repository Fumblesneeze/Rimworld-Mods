## Why

Food deliberately stocked beside a dining table or a patient should be useful there, even when a
more attractive meal exists elsewhere. Room Food supplies that preference; the owned version
must keep ordinary food permissions, physical access and existing feeding workflows reliable.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. Queue **S111**, origin **replacement**. This change specifies future
behavior only; it delivers no runtime implementation or gameplay acceptance.

## What Changes

- Prefer eligible food in the nearest qualifying dining room during ordinary map-food selection,
  and food in the recipient's room when a free colonist is feeding another humanlike.
- Retain food-policy, diet, reservation, danger and reachability constraints. Skip a nearby room
  whose food cannot actually be used; leave ordinary selection intact when no room qualifies.
- Treat the whole current enclosed room consistently. Intentionally correct the source's partial
  room search and opaque size cutoff, without treating open outdoor expanses as dining rooms.
- Preserve source-derived table-search controls: default 100 cells, editable 1–200, with extension
  to an already selected farther map food. Explain that extension and use common global/save settings.
- Compose directly with owned spoilage and preserved-food preferences inside the selected room,
  ordinary inventory comparison, and carried-food borrowing only after ordinary search fails.
- Preserve actual feeding, quantities, food effects and table choice; room preference does not
  create food, reserve it during a query, force an eating place or add new feeding eligibility.

## Capabilities

### New Capabilities

- `megamod-room-food`: permitted dining-room and recipient-room food preference, bounded search,
  common settings, direct food-selection composition and safe compatibility fallback.

### Modified Capabilities

None. Existing food-ranking, borrowing, hauling and routine-carrying capabilities retain their own
contracts; this change supplies a room preference within ordinary on-map food selection.

## Impact

- This change, [research](../../../mods/megamod/docs/refinement/2026-11-05-room-food-research.md),
  [coverage](../../../mods/megamod/docs/replacements/room-food.md) and the refinement ledger.
- Required game1.7 beta. Harmony becomes required with the first Harmony implementation, never
  bundled. No XML change is selected; any later XML Extensions use requires its dependency and
  guards in that owning slice. No hard DLC, source-mod, refrigerator, paste-network or Gateway dependency.
- Source `TheLordDrake.RoomFood`, Workshop `3530729319`, and original `Uuugggg.RoomFood` are
  references, not dependencies. An active original provider suppresses the owned overlap with a warning.
- No source package spoofing, assembly facade without a proven consumer, copied upstream code/art,
  automatic source-settings import, new tab/toggle or private per-port food cache. All seven baseline
  languages and native settings/feeding workflows remain future verification requirements.
