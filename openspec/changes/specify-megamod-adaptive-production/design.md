## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Specification-only replacement of **Everybody Gets One - Continued**, queue S018. No runtime edits,
tests or live acceptance are supplied by these documents. Read the
[research record](../../../mods/megamod/docs/refinement/2026-11-02-everybody-gets-one-research.md)
for exact source pins, hashes, paths, license and code-derived observations.

Inspected installed DLL is 1.7, assembly/file 1.0.0.0, SHA-256
51259BBC102B2566066CFC956A9B7C6E7F74CB686F0433B33E2FF4A927C8C433. Public source pin
7475bc5d16398ed8a71eca5c257cfa8a21899c86 has thirteen normalized matching source/XML inputs but a
different binary. Installed decompilation takes precedence. Native inspection is 1.7.4995 rev309;
hashes identify evidence, never runtime admission. Source license MIT, copyright Alex Tearse-Doyle;
any later reuse needs its own provenance/license treatment. Do not ship upstream assemblies.

Source modes TD_PersonCount, TD_XPerPerson and TD_WithSurplusIng use Bill_Production fields and a
world Bill-to-QuerySearch dictionary. The dependency supplies query evaluation/editor/transfer.
Source BWM UI adaptation widens DrawFilters to person modes; older MirrorBills bridges copy queries.
The installed conditional 1.7 bridge directory is absent. None of these observations proves a
working beta interaction. Source private UI/transpiler shapes are not our architecture.

## Goals / Non-Goals

**Goals:** preserve the three observable modes and real query-based selection, compose directly
with S014 shared plans and S017 unique-stock rules, clearly distinguish trigger from reserve,
support faithful optional query exchange, and fail safely without silently changing production.

**Non-goals:** runtime implementation in this change; copying source classes/package identities;
automatic foreign-save/settings import; every TD Find Lib predicate before its S121 deep dive;
debug raw-field search in ordinary bill UI; new recipe yields, materials, research or worker powers;
a separate bottom tab or unused source sample setting.

## Decisions

### 1. Owned modes with one production decision

Use owned additive BillRepeatModeDefs, provisionally MRGA_PopulationOffset, MRGA_QuantityPerMatch
and MRGA_IngredientSurplus. These are distinct from the source's saved Def names. Preserving source
Def names alone would silently claim that its separately serialized QuerySearch survived migration;
it has not. Record that unsatisfied save contract rather than loading a source bill as default people.
No source package spoof or unneeded Assembly-CSharp/Everybody_Gets_One facade is justified by the
bounded consumer scan. Do not inject repeat definitions by runtime mutation.

One owned evaluator produces an availability/result record for the logical row: query quantity,
product/ingredient stock, effective target/resume, paused state and explanation. UI and start
decisions consume that same current result. Ordinary modes and unsupported counters retain their
native behavior. Use widened checked arithmetic, not source int multiplication; numeric operands
retain the public bounds and clear units specified in the capabilities. For P >= 0:

- offset target T = max(0, P + A); multiplier T = P * A;
- raw resume uses the same transform; R = clamp(raw resume, 0, max(0, T - 1));
- T = 0 never requests new work; missing results are not numeric zero;
- native fresh target/resume operands 10/5 and source base adjustment one remain explicit;
- invalid destination operands block a mode switch pending explicit corrected input, not auto-clamp.

Alternative separate automatic defaults/hidden per-mode operand memories were rejected: source
reuses numeric fields and the user did not request an additional configuration model. Keep dormant
query and product choices, but show that the current shared operand changes meaning on mode switch.
Signed Details and large finite labels are owned controls, avoiding positional native-field rewrites.
Native modifier-key values still require exact-build verification before implementation (EG-Q3).

S014 defines physical commitment and independent local work; do not introduce a second quota system.
New-start decisions use fresh counts before commitment. An already committed UFT/autonomous process
or ready output has a separate continuation/collection decision. Native WorkGiver checks ShouldDoNow
before those paths, so a single false blanket prefix cannot implement this contract. Trace exact
bill family overrides and native start boundary, preserve creator/subject/reservation/bandwidth,
and guard the minimum consumed semantics. Native 1.7 no longer contains the old target-specific
JobDriver post-hauling branch: do not port its unproven compiler-generated two-field heuristic.

### 2. Shared query data, explicit complete scope

Persist a versioned owned query value on the bill/logical row: name, listing, map scope, condition
tree, enabled/inverted flags and stable referenced identities. Default is Bill map + spawned pawns
+ IsColonist - IsSlaveOfColony - IsQuestLodger. Never derive bill scope from Find.CurrentMap.
Default IsColonist does not imply IsFreeColonist: an arrested player colonist remains included;
foreign-faction prisoners are excluded by faction. Pawn_GuestTracker.SetGuestStatus preserves an
ordinary colonist's faction on arrest. Core S018 condition families are explicitly specified; add later S121 predicates to the same query
model with their own observable contracts. No per-mod QuerySearch clone/cache or runtime library mod.

Use native map listers and the common map-owned derived-data subsystem already required by S017 and
future Ctrl+F/hauling. Share candidate identities/metadata and revisions, not job eligibility. Query
result keys include committed query identity/revision, map identities and relevant source revisions;
product counting separately keys filters/scopes/carrier policies. Preserve source condition matching
as outer OR represented inner (minified item or corpse pawn), then apply include/exclude once. Each
condition can inspect the represented properties while the result has one canonical counting identity;
the corpse's pawn is not promoted to a living Include equipped candidate. Deduplicate physical identities
and minified/corpse representations before summing actual units. Do not conflate query units with product
stock units or nutritional ingredient consumption. Ordinary listing follows source category meaning;
unknown holder contents and debug-only lists are not exposed as ordinary known candidates.

Invalidation must cover spawn/despawn/death, faction/guest/slave/developmental/skill/passion/trait/age
changes, stack/holder/visibility transitions, map removal, query edits and save teardown. Time-derived
predicates need their next meaningful boundary as well as events. Source FloatRangeUB/IntRangeUB end
stops can mean unbounded values: represent bounds explicitly, not a literal maximum of 20/200/1000.
Default condition selections are documented in the observable spec; editor changes remain provisional.
Re-evaluate before display/work
while paused when relevant state changed; a game-tick-only cache is insufficient. Require an explicit
unavailable result for unresolved conditions, references or evaluation failures, including one failed
map in a multi-map query; do not sum partial successes. Disabled-by-player differs from unresolved.
Keep Unity/Verse access on its owning thread, no persisted live cache references. Benchmark combined
query/stock/hauling consumers including invalidation/memory before claiming optimization.

Editor operates on a detached draft. Apply validates and commits all fields once; dismissal discards
all fields. This corrects source Import/revert's retention of map/name. Pausing is temporary UI state
with previous-speed restoration. Context is the actual bill owner, independent of current selection.
No fixed 750x320 layout promise: fit ordinary UI scales and translate labels/help in seven languages.

### 3. Compose product carrier selection and query transfer directly

S017 owns base stock, additional product union, local inventory preference, away-origin association
and dedup. For adaptive modes, the Include equipped carrier provider becomes the query's directly
matched pawns across its searched maps. Explicit selection overrides the ordinary faction/nonhuman
carrier policy for this one addition, as source EGO deliberately selects its own carrier population.
Read inventory/equipment/apparel independently rather than source's all-three-tracker requirement.
Other S017 paths remain independent and visible. Do not patch our own counter or layer additions
that double-count stock. CountAway can overlap a query from another map; physical-unit dedup wins.
Special native/provider counters retain their own contract and receive only supported extensions.

Copy/paste uses S017 snapshots and atomic preflight; whole-list edits use S014 logical-row identity.
Bill map is relative; explicit map references are not discarded by an inactive clone. Merge/separate
previews preserve row/query identity and do not clone physical commitment. Deleting the last owner
releases query state; building movement/minification alone does not. Save-switch clears temporary
clipboard/editor receiver state. Missing refs remain recoverable; no fall back to viewed map.

Optional TD Find Lib - Continued integration uses canonical package Memegoddess.TDFindLib and exact
consumed types/interfaces: QuerySearch, QuerySearch.CloneArgs, ISearchReceiver and
SearchTransfer.Register/Deregister. Its own TDSBugFixes dependency does not become our core dependency.
Isolate assembly binding in an adapter loaded only after package/shape admission. Never ship the DLL.
Register a distinctly owned receiver only while a real editor is open, clean it up on every close,
failure and save teardown. Source TD.EGO identity is recorded, not impersonated. Match documented
Receive/CanReceive lifecycle and clone ownership; verify actual sender behavior in a provider-present
run. Translate only complete losslessly supported query trees and scopes, preview destination binding,
and reject the whole tree if any required clause/semantics is unavailable. Unknown types are not run
as arbitrary reflection against our core engine. Full library catalogue/import codecs are S121 work.
Core editor and owned transfers work without this external provider. No universal drop-in claim.

### 4. Surplus uses a resource trigger, not the product counter

Inspect ResourceCounter's actual eligibility, then share its eligible candidates/freshness rather
than copying its delayed or map-summed result blindly. Source sums physical resource units and
does not require nutrition conversion. Preserve Bill.IsFixedOrAllowedIngredient semantics: a fixed
entry bypasses unrelated recipe/bill filters just as native WorkGiver ingredient acquisition does.
Native Details hides fixed-only filter controls, so inventing an additional fixed-ingredient filter
gate would strand legitimate work without a repair UI. For nonfixed alternatives intersect the entry,
recipe restrictions and current bill filter. Requiring all currently eligible alternatives to be resource-
countable is a proposed safety deviation from source's one-entry-only admission. It avoids a partial
count looking like all eligible stock. Restricting away the unsupported alternative can restore it.
Do not silently extend to multi-entry recipes or use the minimum of incomparable resource totals.

Count stored fresh/unfogged resources and supported resource containers on the plan's map once.
Product Count inventories/loose/away and warehouse filters do not redefine this mode. Ordinary
batch acquisition still checks actual reachable/permitted ingredients; a high map total alone
cannot create a job. The strict > trigger can be consumed below its threshold and concurrent starts
can overshoot. No reserve-lock implementation or refund is introduced. Preserve native suspension
and separate physical continuation/collection from new starts as above.

### 5. Admission groups and candidate target specs

Before implementation, convert every selected candidate below into a stable patch-target document,
validator, focused tests and evidence links. This table is not a claim that validators already exist.
Prefer owned direct calls/native extension points; a row needs patches only when the native boundary
cannot otherwise supply the observable contract. No unguarded PatchAll or whole-assembly hash gate.

| Candidate | Consumed semantic target and minimum guard | Failure boundary |
| --- | --- | --- |
| EG-T1 mode menu/Defs | Owned repeat Defs resolve uniquely; menu attaches options to the actual Bill_Production, supported counter/recipe checks and atomic choice mutation remain intact | Hide new choices; saved affected modes retained but suspended |
| EG-T2 overview/details | Actual owner/row and callback identity; numeric edits feed the intended operand, labels show effective values, unrelated controls preserved. Any rewrite validates its incoming stream, operand data flow and unique matches | Withhold adaptive or surplus UI together with dependent starts |
| EG-T3 work eligibility | ShouldDoNow/family overrides and actual new-start/continuation paths consume the admitted row result; original skip-prefix effects and active/UFT/output distinctions accounted for | No new affected work; preserve independently safe native collection |
| EG-T4 pause and counts | CanUnpause and count/display callers consume transformed targets/resume and one S017 product result; compatible native/custom counter invocation retained | Adaptive group unavailable, not native numeric reinterpretation |
| EG-T5 state/clipboard/link ownership | Expose/load/delete/copy/paste/movement callbacks with actual bill/group identity and lifetime; copy chosen maps and condition refs before mutation | Preserve saved state, disable unsafe transfer/start rather than lose conditions |
| EG-T6 query/index freshness | Exact state-change callbacks and required notification timing; observer-only postfix may need just callable existence, mutation interception checks consumed field/control flow | Unavailable affected query/count unless a proven fresh native fallback exists |
| EG-T7 surplus resources | One-entry fixed-or-allowed ingredient semantics, editable alternative filters, ResourceCounter-compatible storage/freshness/container rules and new-start/committed-work separation | Surplus atomic group unavailable |
| EG-T8 optional TD exchange | Canonical active package plus full consumed interface/member shapes, clone ownership, lifecycle and complete supported translation | Adapter alone disabled; core matching UI remains usable |

Adaptive core requires EG-T1/2/3/4/5/6 for its selected implementation; surplus requires its used
T1/2/3/5/6/7 paths. Optional exchange T8 must not disable core. Prefer dependency-based grouping over
one all-or-nothing group for unrelated features. A failure within a group rolls back only newly
installed owned patches, never foreign owners. Produce one bounded startup warning per skipped
enabled patch with package/patch/spec ID, target, failed predicate, unavailable behavior and fallback.
Disabled optional absence is not an error candidate; detected incompatible enabled provider is.

Distinguish original code and effective transpiler input; tolerate unrelated instructions, locals,
offsets and compatible patches. Every transpiler validates its actual incoming stream on each run
and returns it unchanged on failure. Do not rerun foreign transpilers speculatively. Document startup
ordering and later repatch revalidation/invalidation. Prefixes changing parameters require proof of
the consumed parameter use, not mere method existence. Source active packages
Memegoddess.EverybodyGetsOne and Uuugggg.EverybodyGetsOne withhold our overlap; do not unregister them.
Install/read recovery protection before jobs can start with persisted owned modes. If even that
safety boundary cannot be established, reject activation with an explicit load/recovery failure;
do not allow unknown repeat modes to run or destroy saved data. Return requires explicit Resume.

No existing Def/list mutation is selected: three additive owned XML Defs are inert until their runtime
behavior is admitted. Keep them identifiable for saved recovery even if selection/start is withheld.
If implementation adds XML patches, declare XML Extensions required then, check nodes/Defs and
external package/content with supported XML Extensions operations, preserve unrelated membership and
add individual target specs/diagnostics. No destructive wholesale filter replacement is authorized.

## Risks / Trade-offs

- Wider query support spans S121 → keep inspected primitives concrete, record unsupported conditions
  and full-catalogue gap; never advertise EGO/library parity until its dependent cases are accepted.
- Source mode Def/save and TD.EGO identity deliberately not impersonated → document no automatic
  drop-in migration; any future migration needs backup, atomic commit/restore and verified postconditions.
- Camera-relative queries, god-mode fog bypass and permissive partial failures change → label these
  as proposed refinements, show scope/errors, test no silent widening and paused-state freshness.
- Strict all-resource surplus eligibility narrows source's one-entry menu → explain non-resource
  alternatives and permit explicit ingredient-filter repair; no resource threshold invented for corpses.
- Linked/away/foreign-pawn equipped paths overlap → compose S017 once and test individual items across
  scope transitions, not a sum of per-feature cached counts.
- A correct arithmetic model does not prove engine wiring → final native production, UFT/autonomous
  collection, query UI, save/reload and provider exchange are mandatory on the reviewed build.

## Migration Plan

No runtime migration occurs in this specification change. Later implement focused vertical slices
with guarded modes and owned persistence, then enable only accepted paths. Preserve unknown owned
payloads for recovery. Never rewrite normal saves/config during testing; use disposable isolated data.
No automatic source query/settings import, foreign package removal or destructive save downgrade.
Rollback of failed activation disables new work without silently changing modes or deleting choices.

## Open Questions

EG-Q1 records proposed numeric/control refinements for later UX review. EG-Q2 retains full shared
predicate/library coverage under S121. EG-Q3 retains exact beta custom-counter/start/holder/provider
proof and native keyboard multipliers. EG-Q4 retains executable guards, focused RED/GREEN, languages,
independent implementation review and final action-to-result acceptance. These are recorded work,
not questions that stop the sequential specification queue.

Semantic playability pass: compare ordinary fixed targets, both adaptive targets and surplus in the
same real recipe. Costs/yields/research/skills are unchanged; selected population or away products can
intentionally suppress production, negative offsets can request none, and surplus is not a reserve.
No new balance constants need invented economic comparators, but job reachability and completion
under changed thresholds require real workflows. Measure combined shared-data cost before any
performance claim; source code inspection cannot establish it.
