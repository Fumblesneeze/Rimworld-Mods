## Context

Sole owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), 1.7 beta.
S129 is documentation-only. [Research](../../../mods/megamod/docs/refinement/2026-11-06-interaction-spot-on-x-research.md)
records author documentation, three public implementation files, complete selected assembly and
four native type extractions. Public commit `73f1f6ccc1d83a6f3951c5944033347b8cb75ad9` is not the
assembly's informational commit; inspected behavior agrees, identities remain separate.

## Goals / Non-Goals

**Goals:** permit only eligible chunk/tree interaction-position obstructions, retain complete native
and provider constraints, common settings, safe diagnostics and direct owned placement composition.
**Non-goals:** arbitrary obstacle bypass, chunk/tree mutation, new hauling or plant orders, changing
passability or pawn job eligibility, new buildable content, a generic placement framework or source
binary compatibility without a demonstrated consumer.

## Decisions

### Change the specific obstruction predicate, not the final report

Current native `GenConstruct.InteractionCellStandable(ThingDef,IntVec3,Rot4,Map,Thing)` enumerates
`ThingUtility.InteractionCellsWhenAt` without fallback cells, checks bounds, excludes `thingToIgnore`,
then rejects non-Standable or identical definitions and planned `entityDefToBuild` obstructions.
`CanPlaceBlueprintAt_NewTemp` calls it before other interaction-cell/terrain/footprint/worker checks.
The source postfix scans the cells again after any rejection and may replace the complete report
with acceptance. It omits bounds/ignored-object semantics and can overwrite another mod's rejection.

Prefer a guarded substitution at the actual current-obstacle passability branch, leaving the
remaining native loop and all rejection branches intact. The predicate exempts only an actual
selected chunk/tree obstruction; it must not suppress identical-object or planned-build checks.
No modification of ThingDef.passability or temporary despawn; no prefix which skips the complete
original validator. A copied whole validator or translated report-string comparison cannot prove
the failure is solely the permitted obstacle, especially with foreign prefixes/transpilers/postfixes.
Final approach must satisfy effective-code admission before runtime work is accepted.

Native `ThingRequestGroup.Chunk.Includes` currently checks direct `Chunks` or `StoneChunks` category
membership; do not invent name matching or recursively admit unrelated descendants. Trees use
Plant category plus non-null plant properties and IsTree. Native shape/actual loaded content is
the authority; added content satisfying it is eligible without a package-name list. Planned objects
are not themselves chunks/trees merely because their eventual Def resembles one. Native ignored
objects stay excluded before classification. Enumerate every required rotated cell and preserve
the all-cells requirement; never accept because just one cell has a chunk.

The native list-returning interaction-cell helper uses shared scratch storage. Do not retain it
across nested callbacks or sessions. Prefer existing loop insertion; if a bounded local snapshot
is needed, copy only this placement's cells. Native cell Thing lists already supply the small local
query. A second map-wide cache or shared-index scan adds no value; S033's common Thing index is not
needed for a local existing list. No asynchronous evaluation or stale preview authority.

### Direct owned composition and real use remain separate

S048 orientation supplies the final preview rotation, including the user's per-cell manual override.
S036 Architect provides the current designator; S028 replacement/planning owns ignored old buildings,
fog/rock planning and reveal revalidation. Call the same owned obstacle predicate at the admitted
placement boundary instead of patching these owned features. This feature adds no hidden-content
scan, fog permission or duplicate validation cache. Respect stronger provider restrictions.

S063 advance plant clearing retains its actual-work-footprint rule; permitting a tree at a separate
interaction cell does not silently extend that footprint or order cutting. S060 construction access
and ordinary pawn jobs keep their reachability, reservation and work requirements. If another owned
feature already issues appropriate native clearing, it remains its own action. This change does
not promise that construction or work can proceed through an obstacle. Native use/clear/use evidence
must demonstrate the distinction, including rejection where real access remains impossible.

### Common preferences, no entity state

Use two boolean definitions in Construction / Interaction positions: chunks=true, trees=false,
ordered as source. Common persistence/effective-value/row rendering supplies global defaults,
per-save overrides, dormant-value retention and disabled main-menu save callbacks. Apply effective
values to the next preview and commit without cached mode flags. No historical source-config import
is selected; source settings remain read-only. No new placement/entity save data or automatic plan
deletion. Removing the feature returns future checks to the native behavior while existing plans
remain ordinary game objects, possibly awaiting normal clearing.

Source `debugMode` is global persistent config; relocate to a temporary developer-only top-toolbar
diagnostic, initially false, cleared when developer mode is disabled or session ends. No per-repaint
logs. Prefer one detail record per committed native attempt and bounded distinct admission failures;
do not depend on a last matching blocker being non-null. This is diagnostic scope, not an exception
that places ordinary preferences outside the common settings model. Seven-language text required.

### Semantic admission and co-installation

Before implementation, create a stable target document for the obstruction-predicate hook. Check
the argument-to-cell/rotation flow, bounds-before-grid access, ignored-object exclusion, per-Thing
passability comparison, retained equality/planned-build checks and actual rejection branch. A
method-exists probe is insufficient because the hook changes acceptance. Admit unrelated code,
offset/local-number changes and compatible foreign owners; hashes identify evidence only.

Any transpiler validates its actual incoming stream every run and returns it unchanged on missing
or ambiguous matches. Account for skip-original prefixes and later changed semantics without
speculatively executing foreign transpilers. On uncertain effective behavior withhold the owned
group. Re-admit or withdraw on supported later repatching. Roll back only newly installed owned
hooks, warn once per skipped enabled patch with package/target/spec/reason/behavior/fallback, and
never unpatch foreign owners. Explicitly off preferences are not failed patch candidates.

Detect original `dragonissa.InteractionSpotOnX` by package ID and retain its authority when present;
withhold this owned overlap and explain settings inactivity. This avoids pretending we can preserve
foreign restrictions while the original broad postfix still overrides them. No dependency on the
original is required, and no binary facade is justified by the bounded search (only its own DLL hit).
Optional other adapters must be package/shape guarded. No XML is selected; future XML changes need
the XML Extensions dependency, content guards and merge-preserving contract before implementation.

## Risks / Trade-offs

- **Planning versus access:** the feature permits more plans, not instant work. Keep the boundary
  clear in settings descriptions and prove native construction/use after ordinary clearing.
- **Reconstructed validation:** whole-method copies drift and can erase provider restrictions.
  Use a narrow admitted branch and retain unrelated denials; unresolved effective shape disables it.
- **Odd modded content:** added chunks/trees may have other special rules. Keep additional checks
  authoritative, inspect representative loaded content and avoid universal compatibility claims.
- **Existing plans after disabling:** native checks can delay completion until clearing. Preserve
  the plan rather than deleting player work or silently keeping the old setting for that plan.

## Migration Plan

No runtime/data migration in this draft. Future feature starts with inherited source-derived
defaults and leaves original settings/objects untouched. No foreign-config import or automatic
map rewrite. Failure or removal withdraws only the owned exception; native plans and obstacle state
remain intact. Any later import requires a separate backup/atomicity/restoration contract.

## Open Questions

ISX-Q1 source inventory is resolved. ISX-Q2 retains exact effective beta hook/repatch proof; ISX-Q3
retains modded/multiple-position and owned construction composition; ISX-Q4 retains scoped-settings,
native plan/clear/use and diagnostic acceptance. None asks the user to explain source behavior.
