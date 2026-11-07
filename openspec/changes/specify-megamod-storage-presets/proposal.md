## Why

Simple Stockpile Presets supplies useful filter shortcuts, but only through stockpile-zone gizmos
and without checking whether each choice makes sense for the storage. Specify the full catalog
with one compact contextual selector, meaningful options and deliberate preservation of unrelated
storage choices.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin: replacement, **S050 / P1-44**, Simple Stockpile Presets (Continued),
`Mlie.SimpleStockpilePresets`, Workshop2189006791, plus the user's broader storage/UI requirements.
This change delivers **specification only**. No implementation or live behavior is accepted.

## What Changes

- Specify all15 source presets by their allowed contents and exceptions, including freezer, medical,
  material, clean/tainted apparel and disposal choices. Preserve category-based modded content.
- Choose one Presets selector inside contextual storage configuration, extending beyond ground
  stockpiles to shelves, linked groups and admitted containers. This resolves the user's open
  dropdown-gizmo-versus-storage-menu sketch as an explicit proposed design choice.
- Hide presets with no meaningful intersection with the storage's intrinsic restrictions, without
  basing availability on the colony's currently stocked items or its current editable filter.
- **BREAKING relative to the source:** replace allowed item types intentionally, but change only the
  named special-filter choices; preserve unrelated filters, ranges, priority, linkage and S049 refill
  policy. Applying a preset is one validated operation, not a sequence of partly visible clear/add edits.
- Compose directly with S005 shared policy, S049 counts/refill, and native hauling/bill validation.
  Retain normal storage creation, manual editing and existing physical items.
- Record original-provider exclusion, exact content/semantic admission and coherent failure with
  bounded diagnostics. No custom preset editor or source assembly facade is inferred from a fork link.

## Capabilities

### New Capabilities

- `megamod-storage-presets`: applicable contextual catalog, exact filter effects, linked atomic
  application, native persistence/gameplay and safe provider fallback.

### Modified Capabilities

None of the archived capabilities. Existing unimplemented S005/S049 drafts supply the shared policy
and refill contracts consumed here; this feature adds filter presets without replacing those policies.

## Impact

Core is required; Harmony becomes required when this feature is implemented. This C# contextual
workflow proposes no XML patch or XML Extensions dependency. Optional storage/content providers
remain package-resolved and shape-guarded; the original preset mod is a reference, not a dependency.
No source code, assemblies, textures or saves are imported or mutated. Seven baseline languages,
per-patch target documents, independent implementation review and native player acceptance remain
future work. See [research](../../../mods/megamod/docs/refinement/2026-11-03-storage-presets-research.md)
and [replacement coverage](../../../mods/megamod/docs/replacements/storage-presets.md).
