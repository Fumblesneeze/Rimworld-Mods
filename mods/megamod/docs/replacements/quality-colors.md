# Quality Colors (Continued)

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement **S131**. Source **DawnsGlow**, **DawnsGlow.qualcolor**,
[Workshop3513846773](https://steamcommunity.com/sharedfiles/filedetails/?id=3513846773).
Selected1.7 QualityColors.dll1.0.0.0 SHA256
`3d3003b6fa4d6e31b4dac1f728f530c6cd67a23159378e22dc28139aecedd1de`.
Author documentation preceded complete installed decompilation, nine native types and two consumer
paths. Public source retrieval failed; no license established, no source/code/assets redistribution.
[Research](../refinement/2026-11-06-quality-colors-research.md). **Nothing implemented or accepted.**

## Behavior inventory

| ID | Action → result | Defaults/deviation | Status |
| --- | --- | --- | --- |
| QC-01 | View supported native item labels → quality colors and adjacent condition annotation | Seven categories; preserve text/status/actions, no color for absent quality | Specified only |
| QC-02 | Enable full inspect title → complete single-item title tint | Initially off, multiple selection unchanged, no draw-state leak | Specified only |
| QC-03 | Choose preset/edit color → consistent customized palette | Six exact presets, Default initial, RGBA/HSL/hex/wheel/swatches, coherent full lightness range | Specified only |
| QC-04 | Edit global/save preference → scoped persistent result | Palette collection and full-title rows use common inheritance; atomic preview/save/cancel | Specified only |
| QC-05 | Shorten/localize/change palette or actual item → current readable display | No English substring matching, stale paused cache, broken markup or occurrence collision | Specified only |
| QC-06 | Use other owned quality displays → same palette | Direct S132 composition; condition/status and actions retain their own contracts | Specified only |
| QC-07 | Unsupported target/original provider → underlying display and bounded diagnostic | Minimal semantic admission, owned-only rollback; no provider impersonation | Specified only |

[Proposal/design/tasks](../../../../openspec/changes/specify-megamod-quality-colors/) and
[observable contract](../../../../openspec/changes/specify-megamod-quality-colors/specs/megamod-quality-colors/spec.md)
contain the exact colors, controls, scope, failure behavior and future verification.

## Composition and UI audit

Source has mod settings and an embedded color editor; no bottom tab. Owned Interface / Quality
colors uses global/save/Use global, with seven editable colors inside one inherited palette
collection and a separate title boolean. Without a game, save-value controls and Use global are
disabled; while a loaded save inherits, only the save-value controls/callbacks are disabled and
Use global remains clickable. Preset commits target one explicit scope; cancel/failure/stale editors cannot
partially alter settings. No entity policy, cheat or bottom-right view toggle is added.

S132 inventory uses the same effective quality palette directly; distinct durability/warning/cargo
meanings remain independent. No internal patches or per-feature Thing cache. No source settings
import. All seven languages required. Harmony will be required by actual hooks; original provider,
RPG Inventory and external consumer mods are optional. No DLC/XML change selected.

## External contracts and compatibility limits

| Contract | Result |
| --- | --- |
| Native quality category, item label, condition and actions | Preserve meanings and permissions; pure formatting |
| Source colors/fullLabel global settings, QualityColors assembly/package identity | No impersonation, mutable facade or automatic import |
| Dynamic Trade Interface source-package gate and AddColors reverse-patch | Not supplied; selected binding already mismatches selected QC1.7 type/member. Native transfer coverage does not prove custom-column coverage |
| Nice Inventory Tab package/assembly discovery and manual Match palette copy | Not supplied; no source identity spoofing or unsolicited foreign-setting writes |
| Source public QColor/cache/CSV helpers | No demonstrated required binary binding besides the documented consumers; no speculative compatibility layer |
| Original source active | Withhold owned overlap and warn; do not unpatch/remove/reconfigure original |

Concrete consumer paths and exact DLL identities are in research. Future optional bridges require
separate observable contracts and exact-provider native tests. Their omission is an acknowledged
drop-in limitation, not a compatibility success. Unknown external displays keep their underlying
behavior; only admitted native/owned surfaces are claimed.

## Deviations, omissions and verification

Corrected by specification: source preset aliasing, incomplete invalid-setting recovery, global-only
scope, English quality matching, mutable substring color maps, tick-dependent stale caches,
RGB/alpha inconsistency, picker range/model mismatch and leaked full-title draw state. Preserve
the source's six palette mappings, including duplicate colors, without implying accessibility for
every player. Neutral editor labels and hover information keep quality identifiable.

No debug CSV UI, global text-repair service, automatic source settings migration or external
palette-copy bridge is selected. All seven behavior groups and every runtime task remain open.
No changes to item stats, costs, research, carrying capacity, sorting, trade or equipment authority.
Executable per-patch semantic targets, focused TDD, independent review, native item/settings/action
workflows, optional-provider runs and root live acceptance remain required. QC-Q2–Q5 retain gaps.
