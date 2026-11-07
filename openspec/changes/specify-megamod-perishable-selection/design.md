## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target1.7 beta.
S114 is a documentation-only replacement. [Research](../../../mods/megamod/docs/refinement/2026-11-06-select-perishable-research.md)
records public descriptions, the complete four-class source binary and current native comparison.
No runtime1.7 compatibility follows from the source's1.6 metadata or successful decompilation.

Source adds34 pixels to storage-tab size, gates a global ThingFilterUI prefix with a static boolean
around FillTab, rescans all ThingDefs every draw and toggles every globally classified definition.
It does not intersect the parent filter. The owned change preserves the classifier and two-state
bulk edit, while making scope, layout, lifetime and mutation safe.

## Goals / Non-Goals

**Goals:** one contextual action, exact source category/decay meaning, intrinsic candidate bounds,
shared storage authority, atomic additive/subtractive edits and native downstream effects.

**Non-goals:** all deteriorating objects; refrigeration-only classification; a new special filter,
automatic ongoing classification rule, copied source API/texture, new bottom UI or settings framework.

## Decisions

### Classify current definitions, not physical stacks

Retain the source category identifiers: Foods, MeatRaw, PlantFoodRaw, PlantMatter, AnimalProductRaw,
Corpses, CorpsesHumanlike, CorpsesAnimal, CorpsesInsect, CorpsesMechanoid. Match actual current category
ancestry, not English names. Resolve optional/missing category branches without inventing content.
Bound ancestry traversal and deduplicate multiply categorized definitions; malformed cycles must
not hang startup or UI. Unknown classification is not permission to mutate a checkbox.

Within that category union, source uses GetStatValueAbstract(DeteriorationRate)>0 OR
ThingDef.HasComp(typeof(CompRottable)). Current native HasComp checks exact compClass equality;
do not silently claim custom-derived or unrelated spoilage implementations are recognized. A future
supported adapter requires inspected package/member/Def contracts and corresponding visible cases.
The predicate is type-level: temperature, shelves, freshness and currently spawned Things do not
change it. Positive deterioration includes survival meals; medicine remains outside the category
scope even if it rots. No rot duration or deterioration-rate balance change is selected.

Use finalized loaded definitions and existing owned definition metadata where suitable. Avoid the
source's full scan/stat computation every draw. A context snapshot on open plus invalidation/recheck
on relevant current-definition, parent/group and filter changes is a candidate; PER-Q2 measures
the actual need and shared metadata fit. Never use a separate map Thing index or copy Ctrl+F/hauling
data. No speculative new general-purpose cache framework or off-thread Verse access.

### Resolve the actual storage policy and editable scope

Reuse S005 effective storage/group authority and S050's intrinsic applicability/mutation path by
direct owned calls. Do not capture a zone's private settings when a shared policy owns the edit.
Parent/provider permissions and forced-hidden Defs bound the candidate set; current editable
checkboxes do not. For linked groups, use the S005 union of members' intrinsic type capabilities;
actual item acceptance still intersects the specific destination's rules. Ordinary text-search and
collapsed tree state are presentation-only and do not limit this action.

Let C be the nonempty applicable classified set. Select iff any c in C is currently disallowed;
otherwise Deselect. Empty C produces a disabled explanatory control, avoiding vacuous All(empty).
Compute state from ThingFilter.Allows(ThingDef), not Allows(Thing), because quality/rot/special
restrictions remain independent. Explain that distinction in the UI.

### Share one safe mutation path

Recheck stable policy identity, permissions and membership at activation; stale contexts cancel.
Stage current complete filter state without live callbacks, change only C's allowed type bits,
then commit once through the admitted authority and perform final native notifications/validation.
Preserve every unrelated type bit, special filter/range and S049 refill field. Never SetDisallowAll
or SetAllow(category) as a shortcut: these have broader collateral semantics.

Native SetAllow(ThingDef) also propagates virtualDefs and invokes callbacks. Inventory the current
variant closure before using it; preserve ordinary checkbox coupling within valid intrinsic rules,
deduplicate/bound cycles, and reject an unsafe inseparable closure rather than enable forbidden
types. This is a native checkbox consequence, not a new perishable family. Do not silently mutate
raw allowedDefs without the native notification/derived-state contract.

Reuse S050's stage/backup/commit/verify/rollback behavior. Store an in-memory prior snapshot,
restore and verify the prior filter/known derived state on failure, and never blindly retry against
another selection or roll back later unrelated edits. Foreign irreversible callbacks are not
assumed transactional; unsupported contexts fail closed. Preserve ITab_Storage's before/after bill
output validation around the final policy and ordinary hauling invalidation. Ordinary unlinked
bills retain native handling; S014 linked-workbench rows instead retain an unsuitable output scope
as unresolved and stop new starts until resolved. Compose directly with the existing recovery
contract, as S050 already requires; do not silently apply native destination fallback to linked rows.
No per-item transient notification storm or temporary empty policy. S049 computes new demand under
its existing rules; the action changes no threshold or refill mode.

### Allocate contextual UI space and scope it correctly

Use the shared contextual storage surface alongside S050 Presets, with flexible translated text.
Do not copy source34/36-pixel offsets,150-pixel width or overwrite the complete filter UI. Native
ITab_Storage.FillTab uses static WinSize for its content rectangle even when instance size changes;
constructor growth alone is not proof of correct layout. Preserve search, ranges, priority, scroll,
native allow/clear and other admitted controls.

If a scoped native hook is necessary, context must carry the actual owner/filter/parent identity
and unwind in guaranteed cleanup, including exceptions/nested draws. A single global boolean with
prefix/postfix cleanup is insufficient. Unrelated ThingFilterUI users must remain untouched. This
is gameplay-entity state, not a setting; there is no Use-global exception or configuration row.

### Semantic admission and provider policy

Before code, add indexed target documents linking the following consumed semantics to tests and
future native evidence. These are investigation boundaries, not accepted executable guards:

| Target family | Required semantics | Failure outcome |
| --- | --- | --- |
| ITab_Storage current owner/FillTab | Effective owner/filter/parent, real storage context, native before/after bill validation and lifecycle | No owned action on unsupported context |
| Storage layout/ThingFilterUI | Rect reaches the intended controls; enough space; original rendering/input still runs; no unrelated filter leakage | Original UI unchanged |
| Category/decay metadata | Resolved category ancestry, DeteriorationRate and exact native rot semantics; safe unknown/cycle handling | No unproven classification/edit |
| Filter mutation/storage notification | Exact type/variant changes, preserved specials/ranges, callbacks, derived hauling/bill effects and safe rollback | No partial bulk edit |
| Group/policy lifecycle | Correct current S005 authority, union intrinsic scope and stale membership detection | Reopen/cancel rather than retarget |

Treat presentation/context/mutation as one admitted feature group where needed to avoid a live button
with unsafe/no effect. Validate minimal effective semantics, not whole hashes, exact versions or
absence of other Harmony owners. Argument/rectangle mutation needs the consuming data flow, not just
a method name. Every transpiler validates its actual incoming stream and returns it unchanged on
failure. Consider skip-original prefixes and later repatches; never execute foreign transpilers as
speculative probes. Revalidation/installation failure removes only newly added owned patches in
the affected group. Emit the required bounded warning per skipped enabled patch; explicit disabled
features are not failures.

Detect source package Danoweb.SelectPerishable before registration and disable owned overlap;
do not unpatch or remove it. No actual external runtime consumer of source IsPerishable/GetPerishableDefs
was found in the bounded scan, so no facade or package spoof is selected. Optional storage adapters
remain package-ID/Def/member-resolved and absent-safe. Harmony is required with actual use, never
bundled; no XML selected. Any later XML Extensions use requires dependency/guards in that slice.

## Risks / Trade-offs

- Perishable can be read as refrigeration-only → explicit scope explanation and survival-meal/
  herbal-medicine boundary scenarios; do not adopt Workshop comments as new user requirements.
- Filter/provider callbacks have broad side effects → reuse admitted S050 transaction boundaries,
  inspect variant closure and fail unsupported mutations closed.
- Source scans and global UI flag are tempting shortcuts → current shared context/metadata and
  measured invalidation, exception-safe ownership and no unrelated-window leakage.
- Content/provider changes alter classification → recheck current definitions and eligibility;
  no universal custom rot-system support or unmeasured performance claim.

## Migration Plan

No source settings or custom save schema exists. Resulting choices remain native filter data and
persist normally; remove the enhancement without rewriting those choices. Source co-installation
does not trigger an automatic mod-list change. This says nothing about unrelated megamod save types.

## Open Questions

- **PER-Q1:** current implementation-revision owner/layout/mutation/notification semantics,
  semantic groups, virtual variants, safe rollback, optional providers and later repatches.
- **PER-Q2:** finalized category/decay comparators, shared definition metadata and invalidation
  costs, contextual layouts and any custom rot contracts actually selected for support.
- **PER-Q3:** focused TDD, seven languages, source/provider cases, reviewed-build native bulk edits,
  hauling/refill/bill consequences, persistence and cleanup evidence.
