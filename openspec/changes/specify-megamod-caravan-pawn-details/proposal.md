## Why

Caravan and loading lists should identify a pawn's gender and biological age without opening each
info card. Specify the selected display mod's actual one-way-transfer scope and make grouped rows
truthful when their members have different completed ages.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. S116 is a replacement reference, not a co-owner.

## What Changes

- Add ordinary localized gender and completed biological years to pawn labels in native one-way
  transfer lists, including caravan formation/reformation, splitting and transporter loading.
- Retain species identification for individually named nonhuman pawns, as in the source/native
  trading display. Leave items, corpses and ordinary two-way trading labels unchanged.
- Show one age for uniform grouped rows or the minimum–maximum completed ages for a mixed-age row;
  explain that the summary covers the row's available pawns, not a promise about a selected subset.
- Preserve names, native controls, grouping, search and sort choices. Missing optional data must
  not become a fabricated gender/age or prevent use of an otherwise valid native row.
- Compose with native/common transfer presentation and optional providers without duplicate details,
  unguarded patching, new tabs/settings, persistent pawn edits or a second pawn index.

**Specification-only draft.** No runtime behavior, migration or compatibility is delivered or accepted.

## Capabilities

### New Capabilities

- `megamod-caravan-pawn-details`: one-way transfer pawn details, grouped age summaries, native
  presentation/search/control preservation and provider/admission fallback.

### Modified Capabilities

None. Later S133/S135 loading features must use the same labels and actual transfer rows; this
change does not define their loading mechanics or patch our own code.

## Impact

- Source **Show gender/age in caravan form dialog**, arl85, `arl85.ShowGenderAgeCaravanFormDialog`,
  Workshop2914157264; selected1.7 payload. Exact public/source/binary/native evidence is in
  [research](../../../mods/megamod/docs/refinement/2026-11-06-caravan-pawn-details-research.md).
- Harmony becomes required with actual patch use. No DLC or source mod is required. TinyTweaks
  and external transfer UI providers are optional, absent-safe and shape-guarded.
- No Def/XML changes, XML Extensions use, copied assembly/art, automatic legacy import, runtime
  dependency on another repository product, or product dependency on the Dev Gateway.
