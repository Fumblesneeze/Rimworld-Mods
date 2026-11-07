## Why

Players should be able to follow a recipe, ingredient, research requirement or related creature
directly from the information they are reading. Specify Auto links as shared contextual information,
preserving existing navigation and avoiding duplicate or misleading relationships.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. S074 is a replacement reference, not a co-owner.

## What Changes

- Add two-way description links for recipes and their declared outputs, fixed ingredient types,
  research prerequisites, non-pawn recipe users, fixed body parts and body-part groups.
- Link ordinary object costs/research and extend the source's advertised buildable coverage to
  terrain. Include actual recipes supplied by a production building as well as recipes naming it.
- Link fertilized eggs with their hatchling species, and nonhuman corpses to the corresponding
  living species. Do not infer human identity, every optional material or arbitrary possible output.
- Preserve existing descriptions, links, contextual targets, discovery restrictions and native
  back navigation. Append missing relationships in a stable readable order without changing play.
- Feed the ordinary description-entry contract also consumed by Info Card Plus. Keep current
  content/difficulty relationships fresh through the shared recipe/content data, without a private
  map-item index or direct rewriting of other mods' Defs.

This is a **specification-only draft**. No runtime behavior, compatibility or performance is delivered
or accepted. Terrain, bench-owned recipes, current costs and malformed-link handling are explicit
proposed corrections to the selected source, not claims about its existing implementation.

## Capabilities

### New Capabilities

- `megamod-description-links`: relationship scope, additive description navigation, current information,
  native/provider contract preservation and compatibility fallback.

### Modified Capabilities

None. Existing recipe inspection opens ordinary information cards and retains its local choices.

## Impact

- Source **automatic.autolinks**, Workshop **2059389912**. Its installed1.6 binary is the static fallback
  selected for1.7; this is not evidence of source compatibility. Primary documentation, hashes,
  MIT attribution and native/consumer findings are in
  [research](../../../mods/megamod/docs/refinement/2026-11-04-auto-links-research.md).
- Future **required** dependency: Harmony for narrow native description-entry integration.
  The source has no active Harmony patches despite shipping an unused library. Do not bundle it.
- Info Card Plus (`kaamalauppias.infocardplus`, Workshop3776660810), source provider, other content
  providers and DLC are **optional**. No XML/PatchOperation is selected, so this feature introduces
  no XML Extensions requirement. Other megamod features retain their own dependency requirements.
- Preserve S010/S012 valid-context information-card return, S038 availability decisions and the shared immutable
  recipe/content dataset. No new settings, bottom tab, operational toggle, debug action, saved game
  state, research unlock, bill/job, source identity facade or Gateway/product-mod dependency.
