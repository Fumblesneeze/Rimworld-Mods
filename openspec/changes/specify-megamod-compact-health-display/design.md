## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), 1.7 beta.
Steam description first, then installed metadata, author repository and installed assembly inspection.
[Source report](../../../mods/megamod/docs/replacements/compact-hediffs.md) binds the evidence and
records coverage. The source advertises through 1.6; source parity is not a 1.7 compatibility claim.

## Goals / Non-Goals

**Goals:** legible compact health presentation, per-instance detail, reliable limits/layout, native
navigation, disciplined optional adapters and the user's required UI/settings organization.

**Non-goals:** alter medical work, health values or surgery rules; copy source assemblies; invent a
public facade without a consumer; retain destructive health-row replacement merely because source
used it; ship new raster assets during documentation work.

## Decisions

1. **Separate presentation records from conditions.** Derive visible part/condition groups from
   native visibility and current health state. Keep each Hediff reference in a row for tooltip,
   state icon, care action and progress identity. Grouping only by `HediffDef` loses variant identity
   for cases such as substance dependencies; refine the grouping key using actual condition/provider
   contracts (CP-Q2). Never merge health objects to simplify drawing.
2. **Measure what will actually be drawn.** Source row height allocates progress from only the first
   instance but rendering iterates every instance. Build one bounded layout description for both
   measurement and drawing, accounting for all bars, wrapped labels, tooltips and provider controls.
   Source icon calculation also differs from rendering and reserves fewer developer buttons than
   can appear. Moving cheat actions out of normal rows reduces that pressure but does not replace
   real layout tests at narrow widths and UI scales.
3. **Normalize against the actual condition contract.** Source chooses positive lethal severity,
   then finite max severity, then pregnancy=1, else -1; clamps negative fractions but not overflow.
   Source cumulative treatment maps fractions greater than one to zero. Our displayed fractions
   clamp to valid geometry and preserve actual detail values, with unknown scales represented as
   unknown rather than zero/full. Native 1.7 `Hediff.SeverityLabel` and `IsLethal` distinguish part
   threat; inspect affected overrides/species before promising exact blood-loss normalization.
4. **Make disable/sort options complete.** Source `VisibleHediffGroupsInOrder` postfix always
   reorders, ignoring both Enabled and tendPrioritySort. Source constructor applies extra size even
   when its persisted Enabled is false. Our operational toggle gates layout, ordering and resizing
   together; sort-off affects both row levels. Preserve its `TendableNow(true)` eligibility for
   sorting: ignore current tend cooldown, unlike the pulse cue's `TendableNow(false)`. The spec
   distinguishes a severe treated condition from a minor untreated wound. Global/default settings use the common
   resolver, while a save's current-view choice is operational state. No separate settings loader.
5. **Keep diagnostic and gameplay actions distinct.** Source god-mode buttons delete all members
   of a row or edit one condition's severity/duration; move them to top developer actions with
   explicit pawn/instance selection. The hidden-progress option moves there too and resets when
   developer mode ends; it must not change native discovery. Without S137, normal clicks keep
   `HealthCardUtility.CanEntryBeClicked`/`EntryClicked` combat-log navigation. With the owned
   condition-medical-bills feature active, primary clicks pass exact visible row instance sets or
   folded replacement identity to its filter; a separate labelled event control retains native log
   navigation. Info and care controls consume only their own gestures. Compose directly with S137,
   without a patch against this renderer; layout must budget all controls together.
6. **Compose Smart Medicine directly later.** S007 records the source adapter, while Smart Medicine
   has its own queued change. When both behaviors are owned, call the owned care policy/view directly.
   External Smart Medicine remains an optional admitted adapter, never a patch against ourselves.
   EBF, Choose Your Medicine and Pawnmorpher remain isolated optional adapters with canonical package,
   exact consumed member/Def shapes, absence behavior and a bounded failure diagnostic.
7. **Do not add a broad cache for a small view.** Derive only the visible pawn's presentation, reuse
   it within a draw cycle and invalidate on pawn/condition/settings changes. No global Thing scan or
   independently persisted medical cache is justified. Any later shared index use must follow the
   common map-owned system and recheck action eligibility, never stale medical decisions.
8. **Independent rendering and assets.** Source MIT code can be studied; copied substantial code
   would require notices, and asset provenance must be checked independently. No texture is selected,
   generated or accepted here. Future player-visible raster work must use the repository asset skill
   and its live visual gate. Use native affordances where suitable. Avoid brittle translated-label
   regex replacement for anatomy names; preserve side/position explicitly.

### Optional contracts found in source

| Provider ID | Consumed contract | Risk / intended handling |
| --- | --- | --- |
| `Memegoddess.SmartMedicine` | `PriorityCareSettingsComp.Get`, `GetPawnMedicalCareCategory.GetCare`, `HediffRowPriorityCare.CreateCareMenuOptionsWithList` and `GetElementsByList` | Source reflection/delegates and right-click rows; audit exact installed provider, then compose owned replacement directly |
| `Kopp.ChooseYourMedicine` | `ChooseYourMedicine.CompatibilityCompactHediffs.DrawButtonToAssignMedManually(Rect,Pawn,IGrouping<HediffDef,Hediff>)` | Method/name-only detection is insufficient; preserve meaningful per-condition selection with changed grouping |
| `V1024.EBFramework` | `EBF.VanillaExtender.GetMaxHealth_Cached(BodyPartDef,Pawn,BodyPartRecord)` | Part-specific maximum; source private `getPartMaxHealth` is explicitly marked as a foreign Harmony target |
| `tachyonite.pawnmorpher`, `tachyonite.pawnmorpherpublic` | Core-mutation Defs, `RemoveFromPartCompProperties.layer`, `PatchHealthCardUtilityDrawHediffRow.Tooltip` | Source assumes enum ordinal and namespace shape; replace with guarded semantics or omit only unsupported enhancement |

These are source-observed outbound bindings, not verified incoming consumers of our implementation.
Read-only search of installed `*.cs`/About metadata found no external source reference to Compact
Hediffs; that does not inspect compiled consumers. CP-Q2 keeps that audit open. Do not claim drop-in
assembly/type compatibility or spoof `PeteTimesSix.CompactHediffs`.

### Patch target preparation

Source hooks are `HealthCardUtility.VisibleHediffGroupsInOrder`, `DrawHediffRow`,
`DrawPawnHealthCard`, and the `ITab_Pawn_Health` constructor. Its destructive row prefix and literal
`0.375f` replacement are not sufficient target specs. For our actual seams document native/effective
row data, consumed control flow, size ownership, interaction dispatch and callback original-skip
assumptions. Prefer a composable native seam; if replacement is necessary specify every suppressed
native behavior and foreign integration. Validate actual incoming streams for transpilers; compatible
owners and unrelated edits are allowed. Admit the compact-rendering group atomically, restore only
newly owned patches on failure and issue bounded per-patch warnings. Optional adapters fail locally.

## Risks / Trade-offs

- **1.7 rendering/scale changes** → CP-Q1 exact-method inspection and later native captures; a source
  comment about a community fix is a research lead, not compatibility evidence or a dependency.
- **Grouping obscures dependency identity** → preserve per-instance details and separate meaningful
  targets; CP-Q2 needs actual provider examples, not only guessed label strings.
- **Readability options multiply layouts** → cover the modes, above/below, repeated conditions and
  overflow with focused cases and actual localized UI; do not exhaustively replay unrelated gameplay.
- **Visual grouping could target the wrong medical action** → carry explicit instance sets; verify
  menu target and resulting care state through native actions, with exact optional-mod runs.

## Migration Plan

No implementation or original settings import. Native health and orders remain unchanged by display
settings. New settings receive common defaults; the current display choice persists independently.
Any later original-settings migration needs backup, atomicity, failure restoration and postconditions.

## Open Questions

- **CP-Q1 (1.7 research):** exact full health-view/texture behavior and species-dependent severity
  scales, including Waylith blood loss; investigate the reported 1.7 rendering patch from primary source.
- **CP-Q2 (contracts):** actual installed compiled consumers, meaningfully distinct condition keys,
  EBF and medicine/mutation adapters, and Smart Medicine's later owned composition.
- **CP-Q3 (presentation):** accessible non-overlapping overflow details, minimum usable widths,
  localized replacement names and pattern/icon provenance; settle before implementing visuals.
