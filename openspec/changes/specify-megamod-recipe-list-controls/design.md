## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
This S011 change selects contextual recipe-list behavior from Nice Bill Tab - Expansion. The
[research journal](../../../mods/megamod/docs/refinement/2026-11-02-nice-bill-tab-expansion-research.md)
records documentation-first investigation, the installed binary hash and source paths. This is a
specification draft; no target, runtime build or compatibility acceptance exists.

S009 owns recipe/style/material identity, finite ingredient allocation, skills and native bill
creation. S010 owns read-only inspection. S012 composes search, product-category pills, relevant
stat menus and the final interaction layout. Resolve that composition before implementing these
controls. The source is a UI reference, not authorization to import every queue or provider feature.

## Goals / Non-Goals

**Goals:** recoverable scoped hiding; exact row-add identity; honest shortages; paused freshness;
usable narrow layouts; consistent inherited configuration; ordinary/native provider fallbacks.

**Non-Goals:** a replacement Bills queue; automatic subtask or loadout bills; Dubs clone import/export;
new native recipe availability restrictions; copying upstream caches, assemblies or icons; binary
impersonation; automatic import of upstream settings/save data; an extra bottom tab.

## Decisions

### 1. Compose direct owned policies, not a nested patch stack

Use one browser state and one add action with S009/S010. The source patches NiceBillTab methods,
constructor, sorting, static fields and patch methods; those hooks disappear inside megamod. Our
runtime integration is with actual game/provider boundaries only. A click identifies the concrete
recipe/style/material independently of a mutable global selection; row-add and main Add call the
same validated action. Do not call source SelectRecipe with material:null then restore shared state.
Keep inspection immutable with respect to an existing Bill. Handle an input once before other row
actions can interpret it, and verify this through the actual UI, not only a direct action call.

The original Add bill composition owns the principal add gesture. This slice's optional row control
defaults off, preserving deliberate inspection while still making the source convenience available.
When row space is insufficient, use accessible disclosure or a responsive action area; the source's
width check that merely omits its button is not the target behavior.

### 2. Make hiding records explicit and additive across scopes

Key records by real recipe identity (not product or localized text), plus bench identity, bench Def
identity, or game scope. Style/material variants share a recipe record. Evaluate the union of
applicable records. This deliberately differs from the source's global CurrentScope choosing one
independent collection, because changing the next action's scope should not resurrect hidden rows.
Scope is a contextual action choice, not a global setting. Initially offer this bench. The recovery
view enumerates actual applicable records so an unhide action never silently removes broader or
narrower records. State broad effects before the player removes a type/save record.

Bench-specific state must follow the same Thing through minification/reinstallation, not its map
cell. Type/save-wide records belong to the saved game. New benches get no copied bench records.
Persist stable identifiers, retain dormant unknown recipe names without creating substitute Defs,
and reject malformed records without affecting unrelated choices. Exact serialization and lifecycle
hooks remain NE-Q1. Do not promise automatic migration by reusing the source's Scribe keys. No
destructive bulk cleanup or cross-save migration is part of this change.

Keep hidden-only mode ephemeral and keyed to the complete live browser context. Closing, changing
bench/map/save clears it. The source's static lastThingID/view flag has no established save reset;
do not copy it. Filter transitions call the same S010 selection invalidation used by search/pills.
Mode controls and clear-filters remain outside the filtered rows. A hidden recipe is still legal
to inspect or explicitly add. Existing native bills never consult these browsing records.

### 3. Read shortages from the same allocation result as comparison

Consume S009's result keyed by actual recipe, style, material, ingredient roles and relevant native
context. Every candidate unit can satisfy at most its allocated requirement. Render per-role missing,
known-enough, unknown or updating states; distinguish ingredient state from the same-colonist skill
check. Do not match missing roles to arbitrary icon batches by overlapping allowed Defs. Use text
or tooltips as well as native visual cues, retaining all seven baseline languages.

Reuse native listers and the shared map-owned Thing index planned for Ctrl+F/hauling. The inspected
1.7 ListerThings.GetAllThings(lookInHaulSources:true) includes registered haul sources' direct held
Things, not arbitrary nested inventories. This is a candidate source, not permission to count fogged,
forbidden, inaccessible or provider-restricted items as S009 supply. Native job eligibility remains
action-time authority; displayed supply does not reserve items or promise an executable job.

Do not transplant source 90/240-game-tick caches, per-frame optimistic false results, a 16384-entry
standalone shortage cache, or per-requirement fallback sums. Do not allocate an uninitialized partial
Bill after a constructor fails. Unsupported semantics remain unknown and preserve native planning.
Freshness comes from the shared data's revisions/invalidation, including paused changes; expose
updating until the result matches current inputs. Inspect exact native paths before picking targets.

### 4. Preserve ordering intent without recreating the source layout

Filter the ordered distinct recipe/style list without rebuilding a conflicting category order on
each hide. S012 chooses the base/stat ordering, stable ties and category-pill rules. A current result
keeps the user's search and still-applicable filter/sort choices. If hiding or a visibility transition
leaves no recipe supporting the active stat, clear it with the S009 explanation and resume ordinary
order; do not retain an irrelevant sort merely to preserve state. Changes to the underlying
recipe/stat inputs may legitimately change rank, but must not masquerade
as an explicit material or recipe choice. No silently substituted active variant.

The source category stabilizer captures holder references and first-product category paths; tree
refresh resets that capture. Neither it nor skipping NBT's Sort proves vanilla order in all modes.
Avoid claiming exact native ordering without native comparison. Prefer a responsive contextual view
with scrolling/disclosure over fixed 420/484-pixel source columns or a duplicate whole-queue view.
Supported resolutions/scales and readable translated layouts remain NE-Q4, with native evidence
required. No new raster assets are assumed; use native assets or the mandatory asset workflow.

### 5. Use two ordinary settings and leave entity choices contextual

Register Show row Add action (false), then Show ingredient shortage cues (true), under Crafting /
Add bill using the foundation's common settings path. Stable IDs, validation, global storage, save
inheritance/dormant overrides and main-menu guards belong to that common mechanism. Resolve the
effective values on view redraw; a setting change never edits a bill or hiding record. No inheritance
exception is needed. S012 owns browser enablement; these are subordinate controls, not extra entries.

The source's new settings object leaves ShowMissingIngredientSlashes false while Scribe's missing
field default is true. Native Mod.GetSettings/LoadedModManager.ReadModSettings confirms those can
differ for a fresh versus already-loaded record. Our uniform true is deliberate and explicit in the
observable spec. Disabling the decoration does not disable S009 ingredient facts or craftability.
Do not reproduce ShowHideRecipeButton as a setting that can remove the restoration path. Do not
route our cog into NiceBillTabMod.instance, which is what the source's cog does.

### 6. Establish semantic admission and preserve optional actions

No source patch target is adopted automatically. Create individual target documents and index
entries before code, with effective-code/foreign patch interactions, atomic groups, rollback,
startup warning and later repatch policies from the foundation. Hashes below identify evidence,
not compatibility. Compatible foreign patches and unrelated body edits are allowed.

| Boundary | Minimal proof required before use | Failure behavior |
| --- | --- | --- |
| Add bill entry / add action | Correct live bench and recipe/style/material context, callable shape and consumed native action semantics; skip-original interactions accounted for | Disable unsafe enhancement as an atomic group; preserve native/foreign entry and queue |
| Hiding persistence | Same Thing identity through save/minification; safely readable/writable game-owned record schema and lifecycle callback | Do not expose unpersistable hiding; preserve existing data and ordinary list |
| Recipe/stock freshness | Required callbacks and candidate ownership; ingredient/filter meanings for supported choices | Unknown/updating cue, or disable affected display; no optimistic sufficient result |
| UI row handling | Required event/action path and distinct actionable regions, with effective transpilers checked if used | No partial row hook or duplicate action; ordinary Add stays available |
| Optional provider | Canonical package, consumed members/Defs and their semantics, not only a type-name hit | Absent normal; changed adapter disabled with one bounded diagnostic, preserving working native route |

A body rewrite or argument-changing prefix needs the semantic snippet/data-flow it relies on, not
just a signature. Each transpiler validates its incoming stream every run and returns it unchanged
if unsupported. Do not copy source topbar assumptions about Stloc_0, a nearby selection-count branch
or every 110f literal. No XML mutation is selected; any later one needs existence/package/shape
checks and additive XML Extensions operations, with required dependency metadata on first use.

Installed canonical provider identities are BWM `falconne.BWM` (935982361), Best Mix `Mlie.BestMix`
(2195986094), Dubs Mint Menus `Dubwise.DubsMintMenus` (1446523594), and Compositable Loadouts
`Wiri.compositableloadouts` (2679126859). Installation is not active/support evidence. Their actual
source bindings and failure paths are in the research/report. BWM and Best Mix have later selected
owners; compose their owned actions directly. Dubs clone UI and Compositable tag-generated bills
are not selected here. Retaining native queue entry points avoids creating replacement obligations
for those extra actions. Any future interception that suppresses a provider entry must first preserve
it or decline the enhanced path; a displayed but nonworking fallback is not enough.

No installed external incoming Expansion binding was identified by the bounded name scan, but this
does not prove none exists. No facade or upstream package identity is introduced. Active originals
that prevent safe composition retain their own view while our affected enhancement skips with a
warning; merely finding either package or another patch owner is not a rejection predicate.

## Risks / Trade-offs

- **Hiding scope surprises:** union policy differs from upstream; explicit applicable-scope recovery
  and per-save boundaries make that difference visible. NE-Q2 retains it as a proposed UI detail.
- **Persistence loss during relocation:** prove exact Thing lifecycle before implementation;
  unsupported persistence disables hiding rather than promising retention based on source names.
- **Information changes player decisions:** never treat pending/failure as enough stock; preserve
  native production/research/skill restrictions and all-material planning semantics.
- **Provider controls disappear:** do not replace the queue; require a working original route before
  accepting an enhanced entry that cannot represent a special recipe/action.
- **Cost of detailed previews:** share results with comparison and other Thing consumers; measure
  combined redraw/update/load costs before claiming improvement over source preload/caches.

## Migration Plan

No deployment occurs in this draft. Later work adds megamod-owned visibility records and settings,
with no import of Nice Bill Tab - Expansion keys. Disabling the enhanced browser while retaining
megamod makes the ordinary list unrestricted and preserves dormant owned records. Existing bills
remain native. Removing and then saving without megamod does not promise retaining its extra records;
no migration/uninstall guarantee is inferred. A requested import or bulk conversion needs a separate
backup, atomicity, failure-restoration and verified-postcondition contract before implementation.

## Open Questions

- **NE-Q1:** exact 1.7 browser/persistence/row-action targets and same-Thing minification/map-transfer
  lifetime; ingredient/stat gaps coordinate with QM-Q1 rather than introducing fallback guesses.
- **NE-Q2:** S012 final composition, responsive layout and proposed additive hiding/recovery wording;
  final gestures must preserve one action per input and the user's material-choice contract.
- **NE-Q3:** precise optional-provider present-run behavior, any further incoming bindings and reuse
  provenance. Type/package observations alone do not establish compatibility or license permission.
- **NE-Q4:** shared-data paused freshness/cost, supported resolutions/scales, seven-language readability
  and reviewed-build native acceptance. No source preload or static schema result proves performance.
