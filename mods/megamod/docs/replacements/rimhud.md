# RimHUD

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Origin: replacement, queue **S127**. Source **RimHUD**, Jaxe, `Jaxe.RimHUD`, Workshop
[1508850027](https://steamcommunity.com/sharedfiles/filedetails/?id=1508850027). Installed **1.17.5**
root DLL SHA256 `D24BB116949DA4F2EEFCF350CB73E20B014A9DB28D922DDAE256D11D59DB206F`, advertised
support through1.6. Public source pinned **6f1dbfc3ba1196ab081ee4d382101ddd6b23badb**. No license file
found in the inspected tree; independently engineer, no upstream source/assets/DLL redistribution.

[Research](../refinement/2026-11-06-rimhud-research.md) separates author claims, inspected bodies,
inferences and open work. [Proposal/design/tasks](../../../../openspec/changes/specify-megamod-pawn-hud/)
and the two capabilities below specify future behavior. **Nothing implemented or accepted.**

## Behavior inventory

| ID | Input → observable result | Contract / source evidence | Status |
| --- | --- | --- | --- |
| RH-01 | Select a pawn → applicable integrated/floating summary, safe native fallback | [HUD spec](../../../../openspec/changes/specify-megamod-pawn-hud/specs/megamod-pawn-hud/spec.md), State/Layout/InspectPane | Drafted |
| RH-02 | Hover/click identity, health, mental and need details → truthful warnings, full tooltip, native tab | HUD spec; Models/Tooltips, explicit thresholds and native knowledge | Drafted |
| RH-03 | Inspect skills, training, work, equipment, prisoner and extra information → current values and meaningful shortcuts | HUD spec; Values/Bar/Selector catalog; direct owned cargo/number composition | Drafted |
| RH-04 | Change allowed area, current-hour schedule, food/apparel policy or eligible header control → native pawn state changes once | HUD spec; captured subject/hour and native permission correction | Drafted |
| RH-05 | Hide HUD or resize/move it → usable native tabs, letters, log, tutorial and contextual controls | HUD spec; view toggle and geometry/log/letter correction | Drafted |
| RH-06 | Edit presentation/color/layout → current scope changes with inheritance and main-menu restrictions | [Customization spec](../../../../openspec/changes/specify-megamod-pawn-hud/specs/megamod-pawn-hud-customization/spec.md); Theme and editor inventory | Drafted |
| RH-07 | Save/apply/import preset → explicit scoped transaction, preserved omitted fields and origin | Customization spec; Presets/LayoutPreset/Persistent corrections | Drafted |
| RH-08 | Add a supported external widget → documented label/bar/value/selector/custom region, isolated failure | Customization spec; published v1 and exact source callbacks | Drafted; unchanged XML/binary loading still a gate |
| RH-09 | Unsupported source/provider/target → bounded warning and usable native/remaining HUD | Both specs; no foreign unpatching or reset | Drafted |

## Interactions and composed result

S007 Compact Hediffs remains inside Health; S137's requested condition-to-operation filter is a
separate queued feature, not an automatic response to a HUD health-summary click. S034 Readable
Numbers formats structured quantities; S103 Need Bar Overflow supplies real above-full values.
S021/S126 own cargo facts and carrying limits. S128 Passion Icons and S132 RPG inventory retain
separate source changes and directly compose with the owned HUD. No patches against owned code.

The HUD does not add jobs, medical actions, skills, needs, training, or stat changes. Native
visibility and permissions are mandatory. It does not infer conflict merely from another Harmony
owner. Original RimHUD, if active, remains the sole renderer for this overlap; skip owned HUD
patches and warn without removing/changing the original mod or its configuration.

## UI audit and relocation

| Source surface | Purpose | Owned destination / intentional change |
| --- | --- | --- |
| Inspect summary or floating pane | Pawn inspection and contextual actions | Existing inspection; retain native tab/button access, no new bottom tab |
| Bottom-right toggle | Show/hide HUD; Shift also opens config | Show/hide stays bottom-right; optional navigation leads to the same mod-settings section |
| Hover gear / separate config dialog | Presentation, content, color and preset settings | Ordered common mod settings, global/save/Use global controls; save mutations disabled at main menu |
| Header/selectors | Individual pawn policies and native management | Contextual native pawn controls; never inherited mod settings |
| Config footer timing toggle | Debug performance overlay | Top debug toolbar, developer-only, temporary and initially off |
| Floating-mode log/letters/tutorial handling | Ordinary game information | Preserve access; do not suppress tutorials or hide non-overlapping letters |

Settings apply on the next display refresh. Both layouts are collection-valued common settings;
named-preset library is explicitly global-only for cross-save reuse. One combined presentation
choice prevents incompatible inherited flags; global resets restore section defaults and save
resets restore inheritance while retaining dormant overrides. Show/hide is saved operational
view state, not an inherited layout preference. All seven baseline languages remain required.

## Consumed contracts and coverage limits

| Consumer / contract | Preservation decision and verification needed |
| --- | --- |
| Published v1 widget/preset formats | Preserve admitted semantics, identifiers and callback order; exact Def-load/facade resolution and representative provider workflow still required |
| Readable Numbers styled decimal postfix | Direct S034 formatting; no self-patch or blanket text replacement |
| VSE / Passion Icons | Direct S128 passion data/presentation, not the source private constructor IL; external package dependency remains unsatisfied by name alone |
| Useful Marks, current1.7 assembly | Preserve its native header-button path once; package+RimHUD assembly binding is not automatically satisfied |
| Vehicle Framework / SmashTools inspected1.6 assemblies | Native specialized inspection and extension paths; current1.7 evidence remains required |
| GUI Performance Patch private size/probe hooks | No private-performance-facade promise; benchmark our combined code independently |
| Orassans legacy1.3 PawnModel hook | Obsolete private contract deliberately unsupported |
| Source Pawn Rules / Multiplayer adapters | Canonical provider/shape/action research pending; no foreign settings writes and no unsynchronized owned shortcuts |
| Foreign RimHUD settings | Read-only input; no automatic migration. Explicit imports back up/validate/commit atomically and restore failure |

The published callback signatures and exact consumer members are in the design, not presented to
players as technical UI. No drop-in guarantee follows from an identical-looking display.

## Implementation and verification

All nine behavior groups are **specified only**. Tests, runtime code, executable target predicates,
localization, native UI/failure/provider workflows and performance measurements remain unchecked.
Source-derived defects intentionally corrected include stale menu targets, false mech-shutdown
text, missing/swapped custom selectors, the custom-bar documented default, letter suppression,
preset ambiguity/partial resets and
whole-HUD error cascades. Whole upstream implementation/API internals are not copied.

Two independent document reviews and their focused follow-ups resolved the recorded source/scope
findings. Strict typed validation passed148 items with0 failures;18 requirements and38 scenarios
are drafted, with27 implementation tasks still unchecked. This does not accept runtime behavior.
Final implementation requires focused
RED/GREEN, scoped independent review, and personally inspected native workflows on the reviewed
build in minimized isolated games, plus package and playability checks. RH-Q2–RH-Q6 retain native,
provider, layout/language and composition questions; source behavior is researched without asking
the user to explain it.


Initial support is single-player without Combat Extended. Source CE/MP adapter details are retained research only; their adapters, controls and acceptance are deferred to a future scoped change.
