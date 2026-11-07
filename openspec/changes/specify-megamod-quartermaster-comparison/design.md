## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
S009 is a source-inspired comparison slice, not a request to reproduce every Quartermaster window.
The [research journal](../../../mods/megamod/docs/refinement/2026-11-02-quartermaster-research.md)
records the public-docs-first inspection, installed 1.7 assembly hash, native 1.7.4995 contracts and
open questions. The [replacement report](../../../mods/megamod/docs/replacements/quartermaster.md)
maps the whole inspected source surface to selected/adapted/omitted behavior.

Only documentation changes here. No target spec is claimed executable and no runtime task is accepted.
The future integrated browser (S012) owns navigation/search/category layout and composes this capability
with independently researched Nice Bill Tab and Expansion slices (S010/S011). Their source identities
remain provenance, not runtime dependencies or patches against one another inside megamod.

## Goals / Non-Goals

**Goals:** reliable product-stat comparison, understandable supply/skill status, material-preserving
native bills at the selected bench, shared candidate data, honest unknown values and native fallbacks.

**Non-Goals:** equipment/set optimization; pawn loadout assignment; global item/material exclusions;
custom combat-role presets; existing-gear ranking; apparel-policy creation; a new bottom tab;
Quartermaster package/assembly emulation; copied heuristics or balance changes; unresearched CE support.
Multi-product/dynamic-output ranking is explicitly unavailable pending an additional observable rule,
while access to those recipes remains native. A batch of one product is not a multi-product recipe.

## Decisions

### 1. Treat the recipe variant, not the product Def, as the comparison identity

Use the actual selected `IBillGiver`/recipe/style tuple from the native Add bill path. The inspected
`ITab_Bills.FillTab` checks `AllRecipes`, `AvailableNow`, `AvailableOnNow` and ideology-style choices.
Keep that admission path authoritative. `RecipeDef.ProducedThingDef` is only a convenient single-product
case; it is null for multiple/special products. Do not collapse alternative recipes by product as
Quartermaster does. Use direct owned calls from S012 to this evaluator, not Harmony between features.

Each result needs the actual recipe, style, candidate Stuff, comparison value/state, per-ingredient
sufficiency and skill state. These can be small records local to this behavior. No generic plugin
framework, role optimizer or separate UI hierarchy is justified by this slice.

### 2. Use native stat evaluation where its request has a defined meaning

`StatRequest.For(BuildableDef, ThingDef, QualityCategory.Normal)` supports an explicit Normal basis.
Check stat applicability and the relevant `StatWorker`/parts before treating an abstract value as the
output value. Prefer native formatted values/info-card links and distinguish numerical output stats
from declared equipped/worn offsets. Do not add raw stat bases and pawn modifiers into an unlabeled
total. A hediff's first stage is not a universal prediction of its eventual effect.

Do not spawn temporary items, pawns or equipment to get a preview; some constructors and stat workers
have side effects. A stat that needs an actual actor/context is unsupported until a narrow safe adapter
defines its meaning. Catch failures per recipe/material/stat and carry a typed unknown state; never
convert an exception to zero. If an unknown candidate could beat a known one, report incomplete
automatic selection instead of silently dropping it. Explicitly chosen materials can still use the
native bill path when that path is independently supported.

The chosen attribute and direction determine both per-recipe material choice and row ordering.
No armor weighting, AP multiplier, range-band DPS estimate, default-material substitution or
precious-material demotion is copied. Label/material order then stable Def/recipe/style keys provide
deterministic ties; keys are internal, never player-facing technical labels. S012 will define the
initial ordinary order and prioritized catalog of relevant stat choices (QM-Q2).

### 3. Define supply estimates separately from executable jobs

Use the actual recipe ingredients, ingredient value getter, mixing and fixed-role rules for **one
execution**. Native `IngredientCount.CountRequiredOfFor` can depend on `RecipeWorker.GetIngredientCount`
and bill context; unowned custom workers require evidence before claiming support. The result must
allocate finite quantities without double counting overlapping requirements. Supply estimates must
be conservative for ambiguous/unsupported provider rules and expose unknown rather than hide the row.

Inspect the new native bill's default filter/search radius without inserting it into a live BillStack
or invoking job-taking side effects. Constructor safety itself is part of QM-Q1. If a provider requires
mutable initialization just to estimate, keep the estimate unknown and leave native creation usable.
Resource quantities and supported haul-source contents come from the shared map Thing data system,
not `ResourceCounter`: the latter sees resource-designated stock in slot groups and updates every 204
ticks, omitting valid loose ingredients. Honor fog, faction-wide forbidden state, filter, radius and
container-access constraints known without choosing a pawn. Do not reuse another feature's search
results as already eligible ingredients.

The user requested a colonist skill check, not a second job scheduler. Match all skill requirements
on one current-map living free colonist using the appropriate native predicate. Report supplies/skills
as such. Native jobs still decide work capability/assignment, power, allowed areas, reachability,
reservation and mechanitor/other worker rules. Preserve native missing-skill/mechanitor warnings when
queuing. No automatic priority, work assignment or quality guarantee follows from a green indicator.

### 4. Add through the native bill factory and enforce only a proven material choice

Use the selected recipe's `MakeNewBill(precept)` behavior, not a hard-coded choice between ordinary
and unfinished bills. The inspected factory also has mech-resurrection, mech-production and autonomous
variants. Preserve native defaults, style, concept/tutorial notifications and applicable bill limits.
Construct/validate the requested bill before adding it; on failure remove only a newly added owned
bill, never rewrite the pre-existing stack. Report the actual result, not a successful button click.

For supported single-product material recipes, constrain the output-determining ingredient choice
without deleting required fixed ingredients or narrowing unrelated roles. Native fixed ingredients
bypass the editable filter, so the initial worry that filtering Stuff necessarily removes fixed metal
is not correct. However `GenRecipe` derives product Stuff from the dominant ingredient; a broad filter
edit is not sufficient proof for every recipe family. Prove role/filter/dominant-ingredient semantics
for ordinary and unfinished jobs, and use the explicit ordinary-bill fallback for unsupported cases.
Do not invent a hidden postfix that changes the finished item's material after paying different costs.

An alternate material is a choice for one bill, with visible stock/value differences; it is not the
source's global per-product material pin. All-material mode ignores stock only for comparison. It
does not change counts, ingredient selection during actual production or the player's bill after
creation. No reservation is made by browsing or adding a bill. Linked-workbench propagation belongs
to S014's explicit composition; this slice writes only to the opened bench and must not create a
second independent propagation path.

### 5. Share candidate data and invalidate comparison context

First inspect native listers; any genuinely shared derived Thing index is map-owned and also serves
Ctrl+F and hauling. Relevant fields include Def/Stuff, quantity, held/map position, container membership,
visibility and metadata needed to reject disallowed candidates. Native eligibility remains at the
consumer/action boundary. Clear references on map removal/game switch; never serialize live Thing
references as an optimization or read Verse/Unity objects from a worker thread.

Keep immutable Def/stat results separate from stock and colonist eligibility. Suggested derived keys:
recipe/style/product/material/stat/Normal plus Def/provider generation, and a map data revision for
supply results. Invalidate spawn/despawn, transfer, stack/forbidden/container changes, research,
skill and provider state that can affect a result. Language changes invalidate labels/order, not
quantity truth. Refresh visible affected rows and identify pending evaluation; do not scan every item
per row/per frame or retain Quartermaster's separate owned-gear/material/weapon caches.

Revalidate before add. If an available automatic material becomes stale, update the row and require
an explicit subsequent choice instead of substituting silently. Exact work budget/freshness bound
needs measurement under QM-Q4; the observable requirement is automatic update with honest pending
state and an action-time check. Do not claim an optimization from reduced code or static inspection.

### 6. Settings, presentation and semantic admission

Use the common settings mechanism for the default-on enable flag, Crafting / Add bill section,
applying on next view opening. The common row owns global/save loading, dormant overrides and the
main-menu guard. Browsing state is tied to one open view and reset when freshly opened; it is not
a global exclusion list or save policy. Material filters are persisted by their native bill.

The user's explicit placement governs: comparison, its Use all materials control and alternate choice
are contextual inside Add bill. No `MainButtonDef` or bottom-right global toggle is added for these
controls. Translate new status text, unknown explanations, material labels, settings scope and tooltips
in all seven baseline languages. Reuse native product art; any new raster needs the asset skill gate.

No XML changes are presently justified for dynamic UI/evaluation. Before any actual Harmony or XML
hook, register its stable target spec, owner, minimum semantic predicates, rollback group and fallback.
Possible integration seams are selected-table recipe enumeration, the add action and necessary shared
data notifications; the final choices remain QM-Q1. A callback observer can require only its needed
call shape; changing an argument/filter requires checking the relevant use and effective control flow.
Allow unrelated edits and compatible other patch owners. Validate incoming transpiler streams, isolate
failures, restore only owned additions and log one bounded startup warning per skipped enabled patch.
Do not use hashes, whole-body identity, mod-presence bans or a pristine-Harmony assumption as admission.

### 7. Co-installation is explicit, not a counterfeit replacement API

The inspected Quartermaster exposes its own MainButtonDef and public manager/evaluator classes.
Installed text and assembly-string searches found no confirmed incoming binding; the RimPacts hit is
an unrelated internal `RptWarOffice.Quartermaster` enum value. This limited audit cannot prove that no
consumer exists. Do not ship a `Quartermaster.dll`, alias its package ID/Def, or import its settings.
If retained alongside megamod, its own window/settings/bills remain under its control. Verify an exact
present-provider run before claiming coexistence. Research Nice Bill Tab and Expansion before choosing
their competing Add bill entry-point policy. No speculative adapter is added just for a familiar name.

## Risks / Trade-offs

- **Supply estimate mistaken for job availability** → Explicit supplies/skills label, independent
  shortages, native job checks and live blocked/reachable examples.
- **Provider-specific cost, output or stat behavior** → Per-result unknown state and explicit native
  fallback; add an adapter only after inspecting the package and consumed shape.
- **Material filter does not determine final Stuff** → Prove resource-to-output causality through actual
  production; no post-production material swap or silent substitute.
- **Large recipe/material catalog stalls UI** → Shared data, bounded visible work and measured update
  cost; pending state and stale-action checks before a performance claim.
- **Scope accidentally expands to the source's entire optimizer** → Coverage table distinguishes
  adopted comparison from omitted equipment/set/policy workflows; S012 owns the composed view.

## Migration Plan

No deployed feature or data migration exists in this documentation change. Future enable/disable
retains ordinary bills and ingredient filters. No upstream settings import, upstream-save migration,
global preference rewrite or uninstall guarantee is claimed. If later requested, migration needs a
separate observable backup, atomicity, restoration and verified-postcondition contract.

## Open Questions

QM-Q1–QM-Q4 are maintained in the [research journal](../../../mods/megamod/docs/refinement/2026-11-02-quartermaster-research.md).
They distinguish implementation evidence, composed UI decisions, provenance/contracts and measurement.
No gameplay task can be checked from source inspection or OpenSpec validation alone.
