## Context

Sole owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, **1.7 beta**.
S067 source version 1.1.4 consists of two unconditional XML appends; no compiled behavior or saved
state. [Research](../../../mods/megamod/docs/refinement/2026-11-04-floor-order-shortcuts-research.md)
records exact inspected inputs, native 1.7.4998 behavior and the public-documentation access gap.
This document selects an implementation approach for later work, not implemented patch targets.

## Goals / Non-Goals

**Goals:** expose the two native commands in complementary categories, merge content independently,
preserve native actions/current eligibility, and give the added Orders Remove Floor predictable
mouse access without claiming the wood-harvest key. Compose with owned Architect/removal policies.

**Non-Goals:** new removal jobs, stat/refund changes, a third bottom tab, entity settings, source
package impersonation, arbitrary foreign-list deduplication, global shortcut reassignment, custom
art or a Thing cache. S136 floor sampling remains separate. No implementation in this change.

## Decisions

### 1. Keep declarative membership in XML

Use XML Extensions guards and merge-preserving XML additions to existing DesignationCategoryDefs.
Reuse the native `Designator_Deconstruct` and `Designator_RemoveFloor` classes. Runtime category-list
mutation, a replacement designator subclass and copied native removal logic are unnecessary.
Retain the command in its original category and preserve all unrelated entries/fields.

Check a unique intended category and unambiguous specialDesignatorClasses shape. Recognize the
supported bare/qualified spellings of the same native class; a matching label is insufficient.
If a valid unique category lacks the list, add the container and entry together in one mutation.
If it already has one equivalent entry, contribute nothing. Existing foreign duplicates remain
foreign input; this change promises no *additional* duplicate, not global list normalization.
Reject malformed/ambiguous shape without an empty partial container or list replacement.

XML Extensions is required when these operations ship. Use inspected Conditional/Log operations,
and FindMod with packageId=true for source-provider selection. No metadata/dependency change now;
the installed dependency advertises only 1.2–1.6 and has no explicit 1.7 LoadFolders entry. Verify a
real supported 1.7 route before claiming the planned package playable. Never bundle upstream DLLs.

### 2. Separate three admitted contributions

| Proposed target ID | Minimum target facts and mutation | Failure boundary |
| --- | --- | --- |
| `MM.Floors.Deconstruct` | Unique Floors category/list shape, supported native Deconstruct declaration; append missing member only | No owned Floors addition; original Orders command remains if provided by the game |
| `MM.Orders.RemoveFloor` | Unique Orders category/list shape, supported native RemoveFloor declaration; append missing member only | No owned Orders addition; original Floors command remains if provided by the game |
| `MM.Orders.RemoveFloorBinding` | Supported category-resolve/presentation path produces distinct native command instances; Orders instance identity and its hotKey actually govern displayed/handled keyboard activation | No owned binding mutation; existing native/provider command/key allocation remains, with an explicit correction-unavailable warning |

Each needs its own documented executable target spec before implementation. The two XML operations
must validate required Def/content nodes rather than assume a package implies content. A declaration
does not prove a callable runtime class: the implementation must preflight actual native type shape
and correlate final category resolution before accepting the navigation workflow. A malformed native
runtime must not be certified compatible on the basis of XML success alone.

The keyboard correction is an independent admitted patch, **not** a transaction spanning XML loading
and later UI startup. Its failure does not roll back successful native membership additions. That
fallback deliberately retains source-like native key allocation, which is less predictable under
reordering; warn that only the correction is unavailable. This avoids invasive removal of Def data
or a second hidden command filter solely to enforce a larger all-or-nothing group.

All groups allow unrelated fields/instructions and compatible foreign patches. A changed body is
not automatically incompatible. Validate the effective facts actually used, not a whole-body hash,
exact version or absence of other patch owners. False/unknown checks and exceptions skip only the
affected enabled contribution, rolling back only its newly owned operations; emit one bounded
startup warning per skipped patch containing package/patch/spec ID, target, reason, unavailable
behavior and fallback. No per-frame logs. Intentional provider selection and already-present
membership are successful no-op states, not failed target checks.

### 3. Contextual keyboard policy on native instances

Current Deconstruct uses its native dedicated binding. RemoveFloor and PlantsHarvestWood both use
Misc1; Command rendering grants a key to the first displayed claimant. Setting only a supported
Orders RemoveFloor instance's hotKey to null gives mouse access without changing the global binding
or the separate Floors instance. No extra configurable key or setting is proposed.

Prefer a narrow post-resolution policy after native category construction, shared with S036's
command presentation. If a hook changes the hotKey field, existence alone is insufficient: document
and test the distinct-instance/category relationship and the effective render/input use. Reapply
on actual re-resolution, avoid persisting live command references, and handle supported repatching
or provider replacement through the shared admission lifecycle. If the necessary consumer semantics
cannot be proven, leave the correction off. Do not execute foreign transpilers merely as probes.
Any actual transpiler must validate incoming instructions every invocation and return the original
stream unchanged on mismatch. No transpiler is selected just to implement this small policy.

S036 consumes the same native instance and contextual binding rule, whether its optimization is on
or off. Filtering/pagination cannot create a second key claim or recreate the instance with a
forgotten default. Direct calls/policy composition replace patches against our own feature.
Preserve native class identity, game-rule/research filtering, labels, icons and action paths.
No claim about uninspected external consumers depending on a subclass or custom facade.

### 4. Original-provider selection and other integrations

When the canonical original package is active, select it as placement provider and skip our two
XML additions independent of load order. Its unconditional appends then cannot duplicate our own
contribution. Apply an independently admitted contextual binding policy only to the actual supported
native Orders RemoveFloor instance; never remove source entries or patches. A missing command in a
changed original is reported as a provider capability gap, not silently supplemented under an
unverified ordering assumption. No warning that package presence itself means incompatibility.

Other providers with an already-present native entry are retained without another append. Later
foreign mutations are outside static XML admission; final active-list acceptance and supported
revalidation must state that limit. No broad label matching or arbitrary foreign duplicate removal.
Scoped consumer scan found only RimPy catalog identity, no source runtime API. Keep megamod identity.

S008 ordinary order extensions and S061 roof-safe removal keep their direct owned action policies.
This feature must not restore buttons excluded by current game rules/research or bypass removal
sequencing. S136 sampling can select construction without causing a removal designation. Existing
Architect data suffices; no per-port map scan, spatial index or cached removal eligibility.

### 5. UI, state and localization

Use existing Architect Floors/Orders categories and native tooltips/icons. Actions/designations are
gameplay state, not mod settings. There is no user-configurable source policy or saved value to port;
the fixed navigation/binding choice does not warrant a new settings framework or inheritance
exception. No new bottom tab, debug button or small toggle. Native god mode retains its existing
effects without making the ordinary command a new cheat. New player-facing diagnostics/explanations
must cover all seven baseline languages; use existing native translations where applicable.

## Risks / Trade-offs

- Dependency beta support is unproven → validate exact 1.7 loading before implementation acceptance.
- Shared shortcut behavior changes intentionally → mouse-only added Orders control is explicit;
  Floors/global mappings and other commands retain their current rules. FOS-Q1 records review choice.
- Binding adapter fails while XML succeeds → retain native command allocation, explain the narrower
  unavailable correction; never report corrected keyboard behavior without its evidence.
- External late list/instance changes → no universal co-installation guarantee; guard effective
  supported shapes and test selected exact providers. Do not remove their contributions.
- Navigation mistaken for new powers → verify actual native designation, cancellation and pawn work,
  fog/support restrictions, separate foundation removal and native instant-removal exceptions.

## Migration Plan

No source-specific save or settings state exists. Ship guarded additions/admission only after focused
RED/GREEN, dependency/package checks, independent review and native acceptance. Restart/reload rebuilds
menus from the active providers; removing this feature must not erase native pending designations.
There is no automatic rewrite of user mod lists or saves. Rollback removes owned contribution/code,
preserving original native locations and external providers. No deployment is performed here.

## Open Questions

FOS-Q1–3 in research track the proposed key policy, exact effective/dependency/lifecycle proof and
remaining tests/native acceptance. Remaining uncertainty is recorded, not delegated to the user as
a request to explain source behavior.
