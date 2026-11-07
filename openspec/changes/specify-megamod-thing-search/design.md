## Context

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`. Target **1.7 beta**.
S033 reference: Ctrl F - Continued. Specification only. [Research and hashes](../../../mods/megamod/docs/refinement/2026-11-03-ctrl-f-research.md)
separate current compiled paths, public claims, native inputs and proposed corrections. The small
wrapper delegates most search/editor/result behavior to TD Find Lib, separately selected as S121.

## Goals / Non-Goals

**Goals:** owned searchable map facts, useful keyboard/results workflow, correct known-content and
multi-map handling, explicit status/freshness, common configuration and truthful supported exchange.

**Non-goals:** implementing anything in this change; copying source windows/assemblies; source package
spoofing; automatic foreign save/settings migration; complete S121 predicate/library parity by
implication; hidden-info cheats in ordinary search; assigning jobs through the search result list.

## Decisions

### Compose one owned query path

Use owned query values and the same evaluator primitives required by S018 adaptive production.
Represent listing, scope, enabled/include state, nested Boolean/threshold groups and exact bound
references explicitly. S033 specifies its basic name/type/material/zone criteria and result workflow;
S121 must specify its additional predicate catalogue and library/codecs before those dependent tasks
can complete. Library UI is a client of the same owned search definitions, not a second runtime mod.
Avoid a clone of TD Find Lib types solely to satisfy its entire API. Direct owned bill-to-search
calls copy values and show the declared destination scope; they do not patch our own code.

### Share candidates without sharing eligibility

Follow the [foundation data contract](../add-megamod-foundation/specs/megamod-shared-thing-data/spec.md)
and [S032 stock contract](../specify-megamod-buildable-material-counts/specs/megamod-buildable-material-counts/spec.md).
Inspect native listers/holders first. One map-lifecycle owner maintains only genuinely shared facts:
physical identity/lifetime, map/holder chain and position, Def/Stuff, quantity and relevant revisions.
Search-specific label/criterion/visibility views may derive from it; they must document their keys,
invalidation and reason for additional materialization. Do not store a second full map scan for this
source port. S032 still uses native stored-resource membership/freshness, not arbitrary inventory.
Hauling still rechecks capacity, access, forbiddenness, reservations and destination at action time.

Evaluate on the game thread. Deduplicate by physical identity, normalize minified/corpse rows without
double-counting inner objects, and guard recursive traversal against repeated/cyclic holders. Rebuild
an invalid shared generation once or update incrementally; reconcile spawn/despawn, holder/map
transfer, stack split/merge, name/material/zone/knownness and every indexed criterion-field change.
Neither live references nor computed results persist across load/teardown. Snapshot publication is
atomic across the selected maps. A failed clause or map is unavailable, not an empty successful set.

Manual refresh, query edits, reopen and accept force a current generation, including paused/same-tick
changes. Continuous refresh uses the specified 60-game-tick interval and owns one registration per
workspace; old query replacement removes old work. Closing only a separated editor leaves the
result view's refresh registration alive; closing results or leaving the game removes it.
Snapshot freshness/status remains visible.
Action-time checks protect against movement/destruction/visibility changes after evaluation.
If invalidation admission fails, a correct native evaluation is the fallback; inability to establish
correctness makes the feature unavailable. A plausible stale cache is never a fallback.

### Keep a game-scoped workspace and native controls

Own editor/list state with the current game session; avoid the source static singleton retaining old
maps. Preserve focus and paired/detached lifecycle without recursive close/reopen side effects or
duplicated refresh registration. Use native selection/camera commands and their current cap, with
current-map-only bulk selection. Enter-to-first-result is an owned restoration of documented intent;
current compiled source has no caller. Use Shift for add/remove selection as the compiled source does.
Keep exact behavior in observable specs, including no off-map cell highlighting, holder navigation,
locked imports, plain text clipboard handling and a safe Plan/no-selection fallback.
Lock protects edits/import/clear to the current query; explicit New search shortcuts deliberately
start a new unlocked working query, preserving any originating bill/library definition.

Source MainButtonDef TD_Ctrl-F toggles an information view. The user decision tree relocates that
toggle to a small bottom-right control; do not recreate a tab just to mirror metadata. Show search
control is a bool true in the common Search settings section, with global/default/save inheritance
and next-redraw application. Native keybindings remain native global preferences for the documented
reason. Queries, map binding, auto-refresh and separation are workspace state, not settings.
Ordinary queries never inherit DebugSettings.godMode's hidden-information permissions. Any S121
debug listing/custom code must have a separate top-toolbar entry and explicit debug provenance.

### Preserve proven exchange through a narrow optional adapter

Actual upstream binding: TD_Find_Lib.ISearchReceiver with Source, ReceiveName, CloneArgs,
CanReceive and Receive(QuerySearch), registered through SearchTransfer. Ctrl F uses tag `Ctrl-F`;
EGO uses `TD.EGO`. Inspect this real surface and any binary/reflected callers before claiming a
facade. With Memegoddess.TDFindLib present and admitted, a narrow adapter can register an owned
receiver and translate only supported whole query representations. No eager references when absent.
Unknown clause types, incompatible types/enum semantics or unresolved exact references reject the
whole transfer before mutation. Do not run arbitrary foreign code merely to test a shape or convert
a query, and do not discard unknown clauses. Export must likewise reject lossy mappings.

The adapter's name/tag policy and full codec tests are CF-Q3/S121 work. Never register a duplicate
`Ctrl-F` receiver while the original is active; package spoofing cannot satisfy its namespace/assembly
bindings. Source co-installation policy covers inspected current Memegoddess.CtrlF and original
Uuugggg.CtrlF identity from installed replacement metadata. Suppress only our overlapping workspace,
not the shared owned library, production predicates, material counts or hauling. The source's
TD.ShowSearchButton setting and Scribe XML library are not imported automatically.

### Admission and dependency boundaries

No implementation hooks are chosen just to reproduce source patches; the wrapper itself has none.
Each required native/invalidation/optional hook gets a stable patch target document and executable
minimal semantic check. Existence/call shape can suffice for observers; changed parameter semantics
or body transformations need their actual data-flow prerequisites. Account for effective foreign
patches, later repatching and atomic feature rollback, without treating another owner as failure.
Per-skipped-enabled-patch startup warnings name target/spec/reason/behavior/fallback. Optional provider
drift disables its adapter alone. Hashes pin research evidence, never whole-method admission.

Source/library dependencies are not owned-core dependencies. Harmony becomes required if used;
XML Extensions becomes required if its guarded operations are introduced. No declarative XML
mutation is currently planned. Any later external Def change needs package/content shape guards and
merge-preserving operations under foundation rules. Product remains independent of the Dev Gateway.

## Risks / Trade-offs

- **Inherited source bugs mistaken for parity** → retain public/source/binary disagreements in research
  and label owned corrections; no claimed upstream live reproduction.
- **Large predicate catalogue delays safe shared use** → basic explicit criteria now; S121 and each
  claimed external mapping must be specified and verified, never approximate by dropping clauses.
- **Nested containers or visibility providers change discovery** → exact knownness adapter contracts,
  deduplication, lifecycle tests and native absence/presence runs; no universal provider promise.
- **Faster queries hide expensive maintenance** → measure combined query/update/allocation/memory
  costs against native baselines with real churn and multiple consumers before optimization claims.
- **UI changes affect muscle memory/external entry points** → retain useful keyboard semantics,
  document relocation/Shift/Enter corrections, inspect real callers rather than spoof source IDs.

## Migration Plan

No automatic upstream migration, existing save rewrite or deployment in this change. New owned
settings use common missing-entry inheritance. New owned saved queries belong to S121's versioned
library contract; working queries are disposable. Future migration requires its own backup,
atomicity, restoration and verified postconditions before execution. Failed startup/optional admission
must leave stored configuration/library entries intact.

## Verification plan and open questions

CF-Q1–4 in the research/progress ledgers retain exact native targets, shared candidate performance,
S121/external codecs and implementation/acceptance. Focused Unit tests cover pure matching/scope/
snapshot/import decisions without populated game state; constructed Def fixtures use the Defs tier;
owned patch tests use Harmony with guaranteed cleanup. Real loaded content, final knownness,
input/focus, selection and holder/camera behavior require fresh isolated native player workflows.
Do not substitute direct selection/state mutation for clicks and keypresses in acceptance.
Capture paused edit/refresh, Enter, Shift drag, double/select-all, multi-map/holder/stale-result,
detachment, settings/save switching, absent/present source/provider and cross-consumer workflows.
All seven languages and UI scales need live review. No gameplay task is checked by this draft.
