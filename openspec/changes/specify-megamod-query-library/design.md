## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
S121/P2-51, TD Find Lib - Continued, `Memegoddess.TDFindLib`, Workshop3529443295.
This is a documentation-only design. No implementation, saved-data conversion or compatibility
acceptance is delivered. [Research](../../../mods/megamod/docs/refinement/2026-11-06-td-find-lib-research.md),
[catalog](../../../mods/megamod/docs/refinement/2026-11-06-td-find-lib-catalogue.md) and
[40 samples](../../../mods/megamod/docs/refinement/2026-11-06-td-find-lib-samples.md) separate exact
installed source facts from the observable owned choices.

Main1.7 DLL SHA256 `A151E6D3AF87B4C02A344B1ED4F093D5E24A5199F9E33EBBAC74E38F6405E4CB`;
public commit `6718073a19d3c7c8dc412bee28e488dab2be8e64` has a different DLL blob, though retained
source files match the pinned tree. The installed DLL is authoritative. Native reference is
1.7.5000rev1275. The on-disk inventory is122 queries plus5 preselection records, excluding16 category
definitions; one RealFOW query is not selected on1.7. Generated category helper classes do not add
distinct menu features. MIT copyright2025 Alex Tearse-Doyle; preserve attribution without copying
upstream DLLs/assets. Hashes identify evidence, not runtime compatibility gates.

## Goals / Non-Goals

**Goals:** complete documented criterion families; one owned query meaning across search/production;
faithful persistence; safe library operations; explicit scope and knowledge boundaries; actual
consumer-based external exchange; separately admitted developer inspection.

**Non-goals:** a runtime library mod, replacement of the source assembly identity, universal foreign
codecs, automatic source-save migration, duplicate caches/schedulers, arbitrary code execution,
automatic alerts/orders, replacing LTO/CAI/CE/HAR, or runtime acceptance in this documentation phase.

## Decisions

### 1. Extend the concrete S033 query model, not the source inheritance framework

Use the same owned query representation in S033 and S018. Keep behavior-specific clauses and their
editor/persistence together; use small shared operators only for real repeated Boolean, range,
identity, collection and reference-binding rules. Do not copy the entire `ThingQuery` subclass
hierarchy or expose a generic plugin platform preemptively. Ordinary observations come from current
native listers/known contents and the one shared map-owned Thing data subsystem. Share candidates,
not evaluation decisions or pawn-specific eligibility. Wrapper/inner evaluation returns one physical
identity, as S033/S018 already require.

Native state and provider APIs are observations, not permission to run jobs, allocate reservations,
open unknown containers or mutate global game state. Filters that call stateful helpers need an
admitted observational path. Short-lived per-evaluation scratch belongs to that evaluation; avoid
source static temporary gene lists and reentrancy hazards. Do not create one persistent cache for
each filter, availability chooser or consumer. Measure combined consumers, invalidation/update cost
and allocations before claiming a performance improvement.

### 2. Keep the normative catalog separate from the source inventory

All127 declarations are accounted for across the ordinary, expansion, debug and existing S033
capabilities. Preserve obsolete mineable semantics through conversion rather than a duplicate menu.
MarketValue, Mechlink, work-mech, mech-energy and no-xenogene shortcuts expand to visible ordinary
clauses. Generated Health/ThingDef/Gene/Ability/Style/Accessible category helpers retain the subject
of the parent query. `Query_Onscreen` case differs from its class name but native lookup ignores
case; it is not a broken type. RealFOW's always-true dormant stub is deliberately unsupported.

Explicit owned corrections include any-occurrence visible health severity, known-only category and
suggestion data, pawn-policy category matching, positive nonempty universal mech relationships,
usable title-range defaults, explicit weight-class choice, genuine None-can-designate semantics,
safe map edges, stable identities and visible unbounded ends. S018 already owns unrestricted initial
skill and biological age up to50 with no lower limit. Other finite ranges in the new specs mean the
written finite bounds; the source's hidden slider-endpoint infinity is not copied. Imported source
endpoints require lossless conversion against their proven absolute bounds, otherwise rejection.

Body-part condition applicability is not inferred from one recipe's first fixed part. TDL-Q2 must
resolve a supported native/content rule; unsupported per-part modes remain unavailable while exact
condition/severity works. This is separate from S137's future condition-to-medical-recipe workflow.
Do not accidentally implement that later feature through this query library.

### 3. Separate definitions, bindings, working state and results

A serializable definition contains stable clause kind, all operands, include/enabled/group mode,
listing, unit/range semantics and binding intent. A use context resolves current maps/entities.
Results are transient evaluation state, never persisted as authoritative counts. Resolved and
unresolved definitions remain distinguishable; an unresolved enabled clause invalidates the whole
evaluation, including nested/multi-map/negated cases. Explicitly disabled clauses are different.

Use owned stable entity identities scoped to their save, stable Def/package identities for content,
and explicit placeholders for cross-save templates. Label-based source identities (areas/policies,
ideologies/groups and category strings) are not sufficient. No silent current-map fallback from an
empty chosen-map list. Retain all permit faction/readiness, ideology preference, masks, enum modes,
schedule and units: the source omits some clone/save overrides and has differing fresh/load defaults.
Preserve explicit imported values; never reinterpret an omitted source field as a fresh-editor default.

Library edits use a separate working copy and explicit commit/discard. S018 already owns stricter
bill-editor Apply/Cancel/Escape behavior; do not replace it with the library dirty-close prompt.
Source `SearchEditorRevertableWindow` edits live state and restores only selected fields on cancel;
the owned boundary commits all name/listing/maps/clauses together. All callbacks recheck lock, active
save, exact destination identity and receiver lifetime. Source `DrawIcons` imports can bypass a
normal lock because they check `permalocked`; that path is corrected. Collection operations resolve
all collisions first; a later callback cannot target a renamed/replaced sibling by label.

### 4. Persist and exchange data without instantiating imported code

Use a versioned owned schema with registered clause/data kinds. Validate bounded sizes/depth/counts
and operand types before allocating/resolving an entire transfer; parser does not execute named
classes or arbitrary members. A working destination changes only after every selected entry and
binding is validated and all collision decisions are known. Preserve a recoverable previous version,
write/validate the candidate, atomically replace, reread/postcondition-check, and restore/report on
failure. Never overwrite the sole corrupt source with defaults. TDL-Q3 fixes concrete file/bounds/
recovery UX before that slice, including crash versus ordinary write-failure cases.

The source recognizes text prefixes `TD_Find_Lib.QuerySearch`, `TD_Find_Lib.SearchGroup` and
`List_TD_Find_Lib.SearchGroup`, then delegates to Scribe, can force-stop unrelated Scribe and logs
raw input. Do not reproduce that loader. A legacy codec must parse only recognized inert structures,
convert every clause including hidden load defaults, and distinguish exact support from stored
unresolved data. Do not log whole clipboard/query contents. Clipboard/foreign data does not imply
permission to invoke member chains or alter the source provider's settings/save.

Automatic old settings migration is not included. If later requested, separately specify original
backup/hash, all-or-nothing conversion, restoration and exact verified postconditions. Keeping the
real optional TD library installed for external consumers is not a migration or drop-in claim.

### 5. Implement only the demonstrated external seam

Current installed consumers were rehashed against prior complete decompilations:

| Consumer | Current DLL SHA256 | Actual seam |
| --- | --- | --- |
| Ctrl F - Continued, `Memegoddess.CtrlF`,3529520961 | `B363D2418BD892718996DF86FDEB271CCE3CD41D7CF302AEF34340187AB25480` | `CtrlFReceiver`, tag `Ctrl-F`, Use clone, CanReceive only with CurrentMap, opens `MainButtonWorker.OpenWith` |
| Everybody Gets One Continued, `Memegoddess.EverybodyGetsOne`,3530806680 | `51259BBC102B2566066CFC956A9B7C6E7F74CB686F0433B33E2FF4A927C8C433` | `PersonCounterEditor` implements receiver, tag `TD.EGO`, Use clone; registers PostOpen, deregisters PreClose, imports into live query; world component owns bill→query persistence |

The bounded installed About/DLL literal scan found only the library and these two consumer roots;
it cannot exclude reflection or uninstalled consumers. Existing S033/S018 co-install policies select
the original overlapping feature. The owned library is independently usable alongside the real TD
library; no package spoof, replacement `TD_Find_Lib` assembly or copied source dependency.

Source registry interfaces: `ISearchProvider`, `ISearchReceiver`, `ISearchGroupReceiver`,
`ISearchLibraryReceiver`, plus optional float-menu variants. Provider offers None/Single/Group/Library;
receivers supply label/tag/CanReceive/CloneArgs. `SearchStorage` clones before dispatch. Save/Edit
clones are inactive and omit map refs; Use activates and optionally binds map(s). Comma tags suppress
intersections, null suppresses nothing, `=` restricts source; empty tags can throw upstream. Validate
adapter tags and receiver lifetime, exclude self-feedback, and offer only exact supported shapes.

A narrow optional adapter must bind the actual library object graph through package/type/member
guards, register/unregister once, and translate complete supported definitions. Conversion support
must be exhaustively declared and round-tripped; merely creating a query object does not prove
semantics. The external consumer's native restrictions remain authoritative (e.g. CtrlF no current
map). Owned S018's ability to count non-pawn physical units and explicit multi-map choices remains;
do not incorrectly reduce it to pawn-only/bill-map-only after a library transfer.

### 6. Optional modules and UI placement

Selected1.7 CAI adapter consumes `Krkr.rule56`, `Finder.Settings.FogOfWar_Enabled`,
`CompCache.GetComp_Fast<MapComponent_FogGrid>` and `IsFogged(PositionHeld)`. Source disabled fog
returns false and missing grid returns true; the latter cannot satisfy owned known-only rules.
Installed CAI continuation3673768803 declares1.6 and selects1.6/CombatAI.dll, requiring Harmony and
Prepatcher. Do not claim1.7 support from the TD adapter's folder name. Its effective visibility
provider must be inspected/admitted before dependent ordinary knowledge is trusted.

LTO adapter gates `DerekBickley.LTOColonyGroupsFinal`, reads `TacticUtils.AllPawnGroups`, group.pawns,
`TryGetGroups` and `curGroupName`. Source None requires successful lookup plus zero PawnGroups.
No actual matching installed LTO provider was found in the bounded metadata scan; TDL-Q4 requires
its real stable-identity/membership contracts before activation. RealFOW is a dormant stub and is
not supported by a package load-after hint. Missing optional providers preserve unavailable data.

Float Sub-Menus (`kathanon.FloatSubMenu`, optional `_steam` alias) is presentation only. Installed
2864015430 declares through1.6. TD's1.7 adapter folder/DLL is absent; its assembly-presence gate
disables the submenu path. Main code reflects `FloatSubMenus.FloatSubMenu` constructors and
`TDFloatSubMenu.FloatSubMenuAndRefresh`, plus `FloatMenuSearch(true)`. Native owned searchable choices
are sufficient; no new dependency or unsupported1.7 adapter is promised. No source bottom tab
exists to copy. Ordinary library is contextual/settings management, debug lives only in top toolbar,
Only available uses common inherited settings. Query/area/bill choices are not settings.

### 7. Developer inspection is an admitted observation surface

Do not evaluate arbitrary reflection chains on the game thread. First bind an allowed field/read-only
property or explicitly inspected helper; a getter/extension is executable code, not safe merely
because it lacks arguments. Source suggestion scanning admits nonpublic extensions independently
of ordinary private-field visibility; owned admission applies to every segment. No setters/actions,
constructors or imported class execution. Keep private/reveal-hidden modes explicit and debug-only.

Collection visits need cycle detection, finite traversal/depth/result budgets, bounded cooperative
evaluation and failure reporting. A synchronous foreign getter cannot be forcibly made safe by a
stopwatch after it hangs; exclude it unless its bounded non-mutating contract is proven. TDL-Q5
requires actual admission inventory and user-visible limit values before implementation. Do not
advertise arbitrary reflection parity. The six debug samples are definitions only until every
segment is admitted; CE/HAR member shapes are not presently claimed supported.

### 8. Examples and intentional differences

Own explicit Add examples:34 ordinary plus6 separate developer templates, initially none installed.
This replaces source first-use global population, prevents unwanted automatic recreation and does
not register alerts. Preserve source-specific floats/annual bounds explicitly where meaningful;
rename overstated labels (danger, fixable, gear, can reform) to what is observed. Exact source
imports retain their actual semantics, not rewritten sample intent. Newly supplied examples use
unbounded high skill/value comparisons instead of stale20/9,999,999 ceilings, known-only implants,
correct designation identity and gender-independent partner exclusion. Gauranlen/food/HP thresholds
remain literal documented sample choices; they do not rebalance plants, needs or damage.

## Risks / Trade-offs

- Breadth hides dropped operands → full per-kind edit/copy/codec/reload truth tables and inherited
  S018/S033 composition tests; failed clauses cannot become ignored or zero counts.
- Identity differs from old translated labels → deliberate binding/unsupported conversion, original
  data retained; no automatic save migration or blanket safe-removal promise.
- Inspection reveals hidden data or calls stateful code → ordinary/debug separation, admitted helpers,
  bounded traversal and real native verification of no actions/visibility leaks.
- Beta/provider drift → semantic guards, exact evidence identities, fresh provider-present runs;
  no hash/version-only admission and no unsupported1.7 inference from folders.
- Expensive category/nearby/gene/royal-food observations → use current native/shared candidates,
  measure combined workloads; never bypass ordinary restrictions to make a query faster.

## Migration Plan

No runtime migration in this draft. Future slices add the owned model and persistence, focused tests,
review, then exact isolated native UI workflows. If any hooks are required, author target documents
first: method-only observers can use existence/shape when sufficient; parameter-changing hooks need
effective argument-use semantics; transpilers revalidate their actual input every time. Unknown
semantics fail local/atomic groups closed, with one bounded startup warning per skipped enabled
patch, owned-only rollback and later revalidation. No blanket rejection of other owners.

The library itself has no source patches and needs no invented patch layer. Prefer direct owned
composition. Declarative content edits use guarded merge-preserving XML; any XML Extensions use
requires `imranfish.xmlextensions` in metadata. Harmony is required if actually used. No product
references/ships the Gateway, other repository products or downloaded assemblies.

## Open Questions

- **TDL-Q1:** Revalidate effective1.7 observation/provider seams, exact guard groups, late admission,
  actual codec/receiver contracts and any newly discovered external consumers before their slices.
- **TDL-Q2:** Establish trustworthy repeated-part health applicability and content-stable thought
  stage/category bindings, plus finalized native observation semantics where currently source-derived.
- **TDL-Q3:** Select and document bounded versioned storage/codec limits, durable recovery behavior,
  unresolved-data editing and complete source conversions; no implicit arbitrary class loading.
- **TDL-Q4:** Obtain actual supported LTO/CAI provider contracts and exact active-game workflows;
  installed1.6 providers/missing adapters do not establish1.7 compatibility.
- **TDL-Q5:** Admit a concrete bounded non-mutating debug observation set, private/hidden limits and
  optional CE/HAR sample support; exclude unproven getters rather than wrapping arbitrary reflection.
- **TDL-Q6:** Focused TDD, seven-language UI, shared-index measurements, reviewed package and final
  native search/library/bill/save/provider/debug acceptance. All remain future work.

Semantic playability pass: no new products, work, costs, tech or automatic actions. The key balance
boundary is ordinary disclosed knowledge and the quantity/map units passed to production. Source
query/library and current native data are comparators, not finalized live proof. Sample thresholds
do not become game tuning. Actual optional content, hidden data, production consequences and combined
performance require reviewed-build native workflows before any acceptance task is checked.
