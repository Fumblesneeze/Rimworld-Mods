# Simple Stockpile Presets replacement

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, **S050 / P1-44**, plus the user's broader storage/UI requirements.
Status: **source/native analysis complete; observable draft awaiting independent review/validation**.
Nothing implemented or accepted in game.

## Source and owning contract

**Simple Stockpile Presets (Continued)**, Lanilor / continuation Mlie,
`Mlie.SimpleStockpilePresets`, [Workshop2189006791](https://steamcommunity.com/sharedfiles/filedetails/?id=2189006791).
Installed About/Manifest1.7.0; selected1.7 DLL matches the published repository binary at pinned
source HEAD. Source is MIT; original author/continuation notices are retained in
[research](../refinement/2026-11-03-storage-presets-research.md), along with complete six-type analysis,
hashes, stale informational revision, native filter effects and bounded consumer scan.

[Proposal](../../../../openspec/changes/specify-megamod-storage-presets/proposal.md),
[design](../../../../openspec/changes/specify-megamod-storage-presets/design.md),
[observable contract](../../../../openspec/changes/specify-megamod-storage-presets/specs/megamod-storage-presets/spec.md),
[tasks](../../../../openspec/changes/specify-megamod-storage-presets/tasks.md).

## Coverage and deviations

| Source capability | Owned behavior | Difference / omissions | Status |
| --- | --- | --- | --- |
|15 fixed presets | Full catalog specified: freezer/no-meals/meals, general storage/medical/drugs, outside materials/materials/various others, shells/weapons/clean apparel/tainted apparel/chunks/rotting | Explicit category membership/exceptions; negative selections win across overlapping categories | SP-03 drafted |
| One stockpile-zone gizmo/menu | One Presets selector in native storage configuration, including supported shelves/groups/containers | Proposed resolution of the user's UI alternatives; no additional bottom tab or per-preset gizmos | SP-01 drafted |
| Unfiltered menu | Meaningful intrinsic type/state intersection; partial scope and empty state explained | No dependence on currently owned items or current editable allow-list; unknown providers fail closed | SP-02 drafted |
| Clear then allow mutations | Atomic item-list replacement and only named special-filter changes | Preserve other specials, quality/condition/risk ranges, priority and refill choices; opposite named states explicit | SP-04/05 drafted |
| Captured zone settings | Effective shared S005 policy and stale target/membership rejection | Shelves and groups are intentional extension, no internal patches or copied per-member policies | SP-05/06 drafted |
| Native persistence | Save/manual edits/copy/unlink retain resulting filter | No stored preset identity, reset or catalog-driven reapplication | SP-06 drafted |
| Ordinary storage | Preserve creation, native hauling and final bill-output validation | Reported placement bug not reproduced; no claim that source hooks caused it | SP-07 drafted |
| Missing content / original provider | Explicit per-context and per-preset admission/fallback | Original package suppresses owned integration; no source facade or repair claim | SP-08 drafted |
| Editable-preset fork | None | Custom editor/import/export deliberately outside this selected source replacement | Not implemented/planned here |

All implemented portions are **none**. Patch targets, code, tests and live evidence remain future work.

## UI, settings and interactions

The source already uses a single menu gizmo on stockpile zones; it has no bottom tab or settings page.
Our compact contextual selector broadens its useful scope and shows only meaningful choices. Names,
descriptions, partial applicability, preserved restrictions and errors need all seven baseline
languages. Selecting a preset changes entity gameplay state, not global/save mod configuration;
this feature adds no settings rows or inheritance exemption.

S005 supplies effective linkage, complete-policy copy and member restrictions; S049 retains refill
policy and recomputes qualifying stock after filter changes. S014 retains unresolved output choices
for whole-list linked bills, while ordinary native bills retain their normal handling. S021 hauling
uses the final filter without gaining priority/access/capacity. Native/shared data owns any reusable
catalog/membership projection; there is no reason for a separate per-port map cache.

No added physical capacity, weather immunity, item repair/spoilage or content category mutation.
Allowed item-list replacement is explicitly intended by choosing a preset; it is not an XML list
overwrite. Required dependencies when implemented: Core and Harmony. External storage/content is
optional and shape-admitted; no XML Extensions use is proposed.

## Contracts and coexistence

The bounded installed binary search found only the source's own versioned DLLs, with no demonstrated
external consumer. Source Command/patch/DefOf identities are recorded without claiming drop-in binary
compatibility. The product does not spoof Mlie's package or import source assemblies/textures.
The source has no mod-specific save data, settings or editable preset format to migrate. Native
filters remain intact when adding/removing this feature until a player explicitly selects a preset.
With original Mlie.SimpleStockpilePresets active, owned preset integration is inactive, preserving
foreign ownership and ordinary manual editing without promising repairs to original behavior.

## Verification and remaining work

SP-Q1–4 retain exact effective hooks, finalized content/provider applicability, UI/composition and
honest-tier/native acceptance. The semantic pass preserves item mass, capacity, normal work and
player-chosen restrictions; category-based labels do not promise new protection or balance values.
Independent source/native/contract review found no issues; strict validation passed 72 items, zero
failed. No game workflow or compatibility is accepted.
