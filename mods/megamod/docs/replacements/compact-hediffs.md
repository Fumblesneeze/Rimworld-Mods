# Compact Hediffs

Later user addition **S137** now has a [separate draft](condition-medical-bills.md) for condition-driven
medical recipe filtering, including transitive body-part effects. This unimplemented S007 draft is
amended to compose it directly: primary clicks filter when active, with a labelled Combat log action
retaining event navigation. Without S137, ordinary event-click behavior remains. Exact grouped and
folded replacement identities are shared; no internal patches or double-dispatched care/info clicks.

## Identity and provenance

- Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Origin: replacement.
- Source **Compact Hediffs**, author PeteTimesSix, package `PeteTimesSix.CompactHediffs`,
  [Workshop 2031734067](https://steamcommunity.com/sharedfiles/filedetails/?id=2031734067), P1-07 / S007.
- Inspected **2026-11-02**. Installed About.xml and LoadFolders declare **1.1–1.6**, no 1.7 entry.
  Inspected `F:/Steam/steamapps/workshop/content/294100/2031734067/v1.6/Assemblies/PeteTimesSix.CompactHediffs.dll`,
  assembly/file version **1.0.0.0**, SHA256
  `E223A52401B18C06BE9D5F7B7B5BD6BDC1C9D3D75957DD30301A5A7F44418196`.
- [Author repository](https://github.com/PeteTimesSix/CompactHediffs), inspected commit
  `0971051c045803ee53f1e754a89d9565b7320ce6`. Its packaged v1.6 DLL has the same hash. Read settings,
  patch files, custom health renderer, info UI, utility and optional-adapter source; decompiled the
  installed DLL and checked corresponding settings, ordering and rendering paths.
- MIT license, copyright 2020 PeteTimesSix. Independent implementation planned; retain required
  notices if substantial source is reused. Separate asset provenance/acceptance is still necessary.
- Local read-only research: `artifacts/Refinement/megamod-spec-20261102/compact-hediffs/{upstream,decompiled,game}`.
  Exact native comparison: **1.7.4995 rev309**, assembly hash
  `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21`.
  Read native `Hediff` severity/visibility semantics and health-card combat-log actions/bleeding units.

Steam documentation was read first and establishes the main presentation purpose. Source adds the
details below. Workshop comments report 1.7 patterned-bar rendering and Waylith blood-loss problems;
these are **unverified reports**, tracked as CP-Q1. A linked community fix (`3829731117`) was not
accessible through the web tool; no secondary automated summary is treated as source evidence.

## Behavior inventory

All rows are **specified only**, with no megamod implementation or live evidence.

| ID | Native input/action → observable result | Source evidence / defaults | Owning specification |
| --- | --- | --- | --- |
| CP-01 | Open health → compact parts and condition groups | `CustomHealthCardUtility.DrawHediffRow`; group by part then Def, full label if equal else base label and count | [Compact health display](../../../../openspec/changes/specify-megamod-compact-health-display/specs/megamod-compact-health-display/spec.md), grouping |
| CP-02 | Inspect replacement → folded part name with info/tooltip | `GetReplacingPart`, `MakeBodyPartText`; added parts first, optional core mutations; folding on | Same spec, replacement identity |
| CP-03 | Inspect wounds → urgency order and per-instance icons | `ReorderHediffGroups`, row sorting, bleeding/tending icon functions; sorting and enhanced icons on | Same spec, sorting and icons |
| CP-04 | Inspect damaged part → remaining-health bar | `getPartMaxHealth`, native condition colors; enabled; source position field exists but is not honored by renderer | Same spec, part health |
| CP-05 | Inspect progress → severity/immunity comparison | `DrawSeverityBar`, `GetMaxSeverityForHediff`; left-to-right, above, high contrast and textures on | Same spec, progress scale/modes |
| CP-06 | Inspect treatable condition → cumulative progress and pulse | `DrawCumulativeThreatmentBar`; enabled for total-tend requirement | Same spec, cumulative treatment |
| CP-07 | Inspect blood loss → distinct color/pattern | `GetHediffColor`, `DrawSeverityBar`; special handling on | Same spec, blood-loss emphasis |
| CP-08 | Hover/click → per-instance tooltips, info or combat log | Native delegates and `CustomInfoCardButtonWidget`; 1.7 `EntryClicked` opens/highlights Log event | Same spec, native navigation |
| CP-09 | Configure layout → dimensions/separators/preview | `CompactHediffs_Settings`; width +100 (0–300), height +0 (0–200), separators on/dark, alternate shading off | Same spec, settings and layout |
| CP-10 | Toggle compact view → ordinary display | Source global Enabled; no active bottom-right hook (`PlaySettings_Patches` is commented out) | Same spec, operational view button |
| CP-11 | Developer actions → remove/edit specific conditions | Source god-mode row buttons and hidden-progress setting | Same spec, relocated developer controls |
| CP-12 | Optional care/mutation/health providers → additional details/actions | Source `ModCompat` and `UI_compat` delegates | Same spec, optional-provider support; CP-Q2 |

## Defaults and edge cases

Source initially enables folding, missing-part italics, all three separator types, dark separators,
treatment sorting, part health bars, enhanced bleeding/tending, special blood loss, high contrast,
patterns and cumulative treatment. Alternating shading and hidden-progress display start off.
Progress mode offers Off/LeftToRight/EdgeToMiddle/MiddleToEdge and above/below positions. Source
sorting uses `TendableNow(true)`, ignoring the current tend timer; the spec preserves that behavior
and distinguishes it from currently available treatment/pulsing. Source-specific malnutrition,
blood-loss, Anomaly cube and Pawnmorpher patterns are retained as observable cues, not copied assets.
Source persists these as global ModSettings. Separator widths (2), bar heights (4) and icon geometry
are internal layout constants, not exposed player settings; the spec does not invent controls for them.

Bleeding bands are source `BleedRate` thresholds 0.1, 0.25, 0.5, 1.25, 2.25 with the first inclusive
and subsequent upper boundaries exclusive. Native 1.7 health UI prints this quantity as percent/day.
Tend bands have asymmetric boundaries at 0.01, 0.25, 0.5, 0.75 and 1; the observable spec preserves
them explicitly. Source bleeding icons pulse with rate. General progress cues pulse on currently
tendable living, non-permanent conditions, faster with expired tending. Source blood-loss pulsing is
overwritten by a later color assignment; no working blood-loss pulse is claimed as observed behavior.

## Deviations, omissions and composition

- Correct source row measurement: it budgets the first condition's bars but draws every instance,
  potentially overlapping subsequent rows. Detail overflow must remain readable; CP-Q3 selects UI.
- Correct cumulative treatment above 100%, which source renders as zero. Clamp displayed geometry
  without falsifying actual details; unknown maxima must not fabricate a percentage.
- Correct source unconditional body-part sorting, which ignores both Enabled and sorting-off;
  disabling our view/sorting restores the complete intended scope. Constructor sizing must respect it.
- Preserve meaningful variant identities such as different dependency substances rather than merge
  them solely because their Def matches. This is an intentional readability improvement, CP-Q2.
- Use the actual applicable blood-loss scale; source color uses raw severity with 0.65/1 boundaries.
  Preserve the color progression on a normalized scale, not a species-specific false fraction.
- Follow UI rules: current view button bottom-right, ordinary customization in common mod settings,
  debug details/edits in top developer controls. No extra bottom tab or ordinary-row cheat buttons.
- Hidden-progress is diagnostic information about already visible conditions, not permission to
  reveal undiscovered conditions. Source exposes it in normal settings; ours limits it to developer mode.
- No new below-part-health-bar option: source stores a position field but renders bars above and
  exposes no corresponding editor. Above/below remains available for condition progress bars.
- Smart Medicine is separately selected: compose care behavior directly once specified, not through
  an internal Harmony adapter. Other optional integrations remain narrow external adapters.

## UI audit and settings scope

| Source surface | Purpose | Megamod placement |
| --- | --- | --- |
| Native pawn Health tab | Observe conditions and ordinary native actions | Same contextual tab; not a new bottom-bar tab |
| Global Enabled checkbox | Switch current presentation | Small bottom-right view control; initial default configured separately |
| Global options/selected-pawn preview | Presentation preferences | Ordered Health display section, common global/save controls and main-menu rules |
| God-mode row buttons | Remove conditions, set severity/duration | Top developer toolbar with explicit pawn/condition selection |
| Show hidden progress setting | Diagnostic numeric information | Top developer controls, off initially and unavailable outside developer mode |
| Care menu/icons from optional providers | Condition-specific medical choices | Contextual condition controls; gameplay state, not mod settings |

Ordinary settings use common inheritance; no per-pawn configuration menu is invented. The current
compact/ordinary view choice persists per save as operational state; changing its default does not
overwrite an established choice. No automatic source-settings migration is included.

## External contracts and saves

The [design](../../../../openspec/changes/specify-megamod-compact-health-display/design.md) lists exact
source package IDs and consumed delegates for Smart Medicine, Choose Your Medicine, EBF and
Pawnmorpher. They are **outbound source bindings**, not evidence of verified megamod compatibility.
Source `getPartMaxHealth` has an annotation explicitly preserving its name for Harmony targets;
actual incoming compiled consumers still need inspection. Installed `*.cs`/About text search found
only the source's metadata reference, which cannot rule out DLL/reflection consumers (CP-Q2).

Source saves global settings only and reads native health objects; our display must not rewrite
condition or medical-order saves. Provider-specific care state remains provider/owned-care behavior.
Original provider present → suppress megamod compact rendering with warning initially. Core required;
Harmony required when used; optional providers absent-safe. No upstream assembly/package identity
spoofing or blanket drop-in claim. XML Extensions required if later declarative operations use it.

## Verification and acceptance

Nothing implemented. All twelve behavior rows await semantic target specs, focused tests, independent
review and native in-game acceptance. Required workflows include switching views; narrow/localized
rows with multiple conditions/bars; replacement info; actual combat-log click; separate dependencies;
care menu target/result with exact providers; save switching; developer off/on; and actual 1.7
patterned-bar/scale inspection. New raster assets additionally require the asset skill's live gate.
No game was launched for this document-only draft. CP-Q1–3 remain in the
[refinement ledger](../refinement/specification-progress.md).
