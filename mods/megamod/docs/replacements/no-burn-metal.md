# No Burn Metal

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin replacement **S104/P2-34**. Source **`Unon.NoBurnMetal`**, Unon/Nonu,
[Workshop1923990111](https://steamcommunity.com/sharedfiles/filedetails/?id=1923990111),
[author source](https://github.com/Nonunon/NoBurnMetal), inspected2026-11-05 at revision
`2c8981324be33d662b6292545ad89b09ac9a8ac0`. GPL-3.0; retain Dr Zhivago/Antyhc attribution.
Selected **Common +1.7**, Manifest/InformationalVersion1.9.2, AssemblyVersion0.0.0.0, FileVersion1.9.2.0,
MVID`b1efb808-d3da-4952-94b0-8947f6c477c0`, DLL SHA256
`B0DCC1905D8B5031E5A200F2B352CC9E22B4CAC35B3016E6241E84FFAA5177C5`.

[Research](../refinement/2026-11-05-no-burn-metal-research.md) retains source/binary/native evidence,
curated identities, public claims, raw provider XML and unresolved gates.
[Proposal/design/tasks](../../../../openspec/changes/specify-megamod-metal-fire-resistance/) and
[observable specification](../../../../openspec/changes/specify-megamod-metal-fire-resistance/specs/megamod-metal-fire-resistance/spec.md)
own the replacement. **Nothing is implemented, packaged or natively verified.**

## Behavior inventory and coverage

All rows below are specified but unimplemented; evidence and owning scenarios are linked above.

| ID | Native action → observable result | Defaults and edges | Remaining work |
| --- | --- | --- | --- |
| NBM-01 | Enable a material and use existing/new objects made from it → ordinary material flammability removed | Steel/silver/gold and named exceptions on; other discovered metallic materials off | Guarded contribution and current/prospective/minified inspection paths |
| NBM-02 | Build/use selected conduit or switch → ordinary flammability0 with native power behavior | Three independent choices on; hidden conduit already0 | Direct-base path, power and incident workflows |
| NBM-03 | Play through fire/damage → no ordinary fuel from a resulting-zero object, independent hazards remain | Native150-tick fuel reassessment; no direct damage, floor or wearer immunity | Causal fire/damage/terrain acceptance |
| NBM-04 | Open catalogue → current provider-labelled material choices |36 curated IDs; actual content/shape gates; zero contributions hidden | Missing optional1.7 content and combined finalized evidence |
| NBM-05 | Change a choice or bulk scope → common global/save policy applied | Use global default; dormant overrides; explicit scope/count; atomic changes | Common persistence/UI and native save switching |
| NBM-06 | Search/collapse/show unavailable → readable contextual configuration | Inactive hidden by default; search transient; collapse/navigation global-only | Seven-language responsive common layout |
| NBM-07 | Turn off or switch saves → current ordinary behavior restored without stale values | No global Def snapshot restoration; no source settings import | Cache/lifecycle and failure groups |
| NBM-08 | Co-install original or encounter incompatible shape → truthful dormant/unavailable behavior | Original owns full overlap; optional failure isolated when independent | Semantic target specs and per-skipped-enabled-patch diagnostics |
| NBM-09 | Use other owned fire/damage/recipe features → one consistent result | No duplicate actions, metal-cost heuristic, category or fuel-filter edits | Direct composition and reviewed native workflows |

## UI audit and common settings

| Source surface | Purpose | Owned destination / change |
| --- | --- | --- |
| Mod settings page and item checkboxes | Gameplay configuration | Materials and fire section of common mod settings; Global default / Current save / Use global |
| All/provider on/off | Bulk configuration | Explicit global/save scope, all/provider target and affected count; add save Use global; search does not narrow scope |
| Search, expand/collapse, hide inactive | Navigation | Common view with truthful unavailable states; global-only navigation preference exception, transient search/scroll |
| Native stat inspection | Explain resulting flammability | Existing contextual inspection, matching current contribution and independent effects |

The source has no bottom tab. Add no bottom tab, bottom-right behavior toggle, debug override or
entity gizmo. Save controls and callbacks are disabled at the main menu; inherited editors are
disabled in-game. Seven baseline languages and crowded layouts require native verification.

## Contracts consumed by other mods or saves

| Consumer/evidence | Binding | Decision and verification gap |
| --- | --- | --- |
| Source Common XML | `NoBurnMetal.NBMTargetsDef`, `defNames`, `knownMods`; `NBMKnownMod` fields | Own catalogue instead; no external consumer established, no facade claim |
| Potential source API callers | Public `NBM`, `NBMTarget`, settings types/methods | Installed bounded scan found no actual runtime caller; reflection/UTF16/future consumers remain NBM-Q2 |
| YART evaluation mod lists | `unon.noburnmetal` sample package entries | Test-fixture metadata, not a callable runtime integration; no package spoof |
| Source global settings | `enabled`, `disabled`, `collapsed`, `hideInactiveMods` | No silent import; new common owned settings with provider-qualified identities and save inheritance |
| Existing game objects | Ordinary object identity and material | No new Thing class or object conversion; mid-save lifecycle/removal still needs native acceptance |

## Interactions, deviations and omissions

S072 structural damage, S079 fire response, S091 corpse burning, S096 processing and material/recipe
inspection consume current native effects and their own eligibility. Implement direct composition,
not self-patches or duplicate map caches. A per-content catalogue suffices; no private Thing index.

Original No Burn Metal owns the overlap if active. Optional content is absent-safe and individually
package/content/shape gated. Advertised provider support is not equivalent to verified1.7 behavior:
three providers and Anomaly were unavailable locally, while More Useful Materials advertises1.6.
Expanded Materials' inspected1.7 metals already have zero factors. Other metals from a known provider
remain off unless one of the36 named source defaults covers them.

Deliberate changes: shared save inheritance, current ordinary-value restoration, qualified identity,
meaningful-positive choices, accurate contribution/availability wording, explicit atomic bulk scope,
one invalidation per commit, and seven-language common UI. The source's global mutable Defs and
captured-value restoration are not copied. Direct flame immunity, floor edits, loose-resource base
rewrites, material-category/fuel-filter replacement, speculative API facades and automatic migration
are omitted. Existing independent positive flammability effects remain effective.

All implementation is missing. XML Extensions is required if guarded external XML operations are
shipped; Harmony is required with native hooks. No upstream binaries or art enter the package and
no product dependency on the Dev Gateway is introduced.

## Verification and acceptance

Both independent document reviews found no issues. A focused re-review confirmed the corrected
composition reference and the distinction between the source UI's Off label and its README wording.
Typed strict OpenSpec validation passes 126 items with zero failures; scoped link/format checks pass.
Static source/native comparisons support the specification but do not accept fire behavior,
optional compatibility, settings persistence, UI or balance. **NBM-Q1–4** retain these gates.
All tasks remain unchecked until focused RED/GREEN, reviewed runtime changes and the root native
player-workflow/evidence/cleanup requirements are fulfilled on the reviewed build.
