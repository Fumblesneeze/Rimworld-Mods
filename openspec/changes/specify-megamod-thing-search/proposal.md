## Why

Finding a particular item, pawn or group should not require visually scanning a colony. The user's
Ctrl+F notes call for one useful search experience whose underlying map facts are shared with
hauling and construction counts, without confusing visibility, stored stock and pawn eligibility.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target: **RimWorld 1.7 beta**. S033, origin: replacement. This change **specifies future behavior only**.

## What Changes

- Provide new text search, find-like-selection and reopen shortcuts, an editable search and a
  detachable results view with selection, highlighting and camera navigation.
- Specify the basic name/type/material/zone criteria, listing categories, grouping, map scopes,
  known contents, quantity totals, refresh timing and unavailable-result behavior explicitly.
- Use one shared Thing-data owner with hauling, buildable material counts and production queries;
  retain each consumer's own visibility, storage and eligibility rules.
- Restore the advertised Enter-to-first-result workflow, make stale-result actions safe, and
  prevent missing criteria or a failed map evaluation from silently broadening a search.
- Relocate the upstream bottom-tab button to a small bottom-right search-view control. Put its
  visibility preference in common settings; keep query choices in the search workspace. Ordinary
  searches do not gain hidden information merely because developer/god mode changes.
- Define search-library/exchange boundaries with S121 TD Find Lib and S018 adaptive production.
  S121 still needs its own complete predicate, library and adapter specification; this change is
  not a claim that its entire catalogue is already specified.

## Capabilities

### New Capabilities

- `megamod-thing-search`: explicit basic search criteria, datasets, map scopes, quantities and
  failure/visibility boundaries.
- `megamod-search-workspace`: entry points, editing, results, navigation, refresh, lifecycle,
  settings and supported query exchange.

### Modified Capabilities

None. Foundation shared-data, settings, UI-placement and patch-admission contracts apply.

## Impact

- Reference **Ctrl F - Continued**, `Memegoddess.CtrlF`, Workshop **3529520961**, with its consumed
  **TD Find Lib - Continued**, `Memegoddess.TDFindLib`, Workshop **3529443295**. See
  [research](../../../mods/megamod/docs/refinement/2026-11-03-ctrl-f-research.md).
- Neither source is a required runtime dependency of the owned core. TD Find Lib is an **optional**
  exchange provider, usable only through a separately admitted adapter with supported mappings.
  Its own TDS Bug Fixes dependency is not an owned-core dependency. No DLC is required by this slice.
- Native UI/data invalidation hooks may require **Harmony**, `brrainz.harmony`; declare it required
  when actually used. No XML patch operation is planned here. Any later XML Extensions use must
  add its required dependency and guarded target contracts in the same implementation slice.
- Preserve meaningful query exchange where proven; do not spoof package IDs or ship source
  assemblies, assets or an unsupported binary compatibility facade. Source co-installation suppresses
  the overlapping owned search workspace with a startup warning, without disabling other owned consumers.
- No implementation, tests, acceptance or migration is delivered. Future native verification must
  prove the actual keyboard/mouse workflows and cross-consumer consistency on the reviewed build.
