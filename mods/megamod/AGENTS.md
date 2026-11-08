# Make Rimworld Great Again — development instructions

These instructions apply to `mods/megamod` and its owning feature work. Read the root `AGENTS.md`
and required repository skills first; all root acceptance, package, and safety invariants remain in
force. The sole owner is **Make Rimworld Great Again**, package ID `fumblesneeze.megamod`, folder
`mods/megamod`, author `Fumblesneeze`. Initial target: **RimWorld 1.7 beta**.

Start with [README.md](README.md), the [foundation change](../../openspec/changes/add-megamod-foundation/)
and its proposal/design/specs/tasks, then the specific feature's owning OpenSpec change. All
OpenSpec files stay at the repository root. A feature is not owned by its upstream source mod.

## Coordinated implementation exception — this mod only

The user explicitly authorized parallel multi-feature chunks with orchestrator-run integrated
verification. Read the [shared architecture and delivery protocol](../../openspec/changes/plan-megamod-parallel-implementation/design.md),
[roadmap](../../openspec/changes/plan-megamod-parallel-implementation/roadmap.md) and
[governing tasks](../../openspec/changes/plan-megamod-parallel-implementation/tasks.md) alongside each
assigned feature change. Sequential source research below remains the refinement process; it does
not prohibit parallel implementation after contracts are specified.

- **Orchestrator:** owns part/chunk allocation, small shared-contract milestones, edit leases,
  integration, central registrations/metadata, canonical build/install and final acceptance. It works
  agile: revise the plan when evidence changes, including splitting/reordering chunks, reassigning
  work and coordinating shared-design/dependency changes. Record reasons, affected feature/contract
  IDs, superseded assignments, invalidated evidence and remaining work in the roadmap revision log;
  update tasks and refinement progress and notify affected implementers. Do not silently omit a
  feature, reverse a user decision or loosen acceptance. No new permission is needed for routine
  planning changes within accepted intent.
- Commit focused, consistent checkpoints as work becomes reviewable; do not wait for the entire
  mega-mod to become one large change. The user explicitly permits this incremental history.
  Coordinate active leases, preserve build/dependency consistency, and keep implementation status
  distinct from native acceptance. A commit never closes an unverified feature or bypasses a gate.
- **Implementers:** stay within assigned behavior and disjoint path leases. Read referenced source
  mods/documentation or read-only decompilation and existing owned helpers first to find relevant
  game paths and edge cases efficiently. Do not implement blindly from zero, copy or translate
  upstream implementation code/XML/assets, or replace our specs/design with upstream behavior.
  Required game/integration signatures, Def identities and observed behavior are research inputs. Record
  differences and return cross-chunk/shared-contract problems to the orchestrator with evidence.
- Implementers follow focused vertical RED/GREEN, honest host tiers and independent review. They
  author executable native verification scenarios with fixture setup, actual player actions,
  observable results, capture points and cleanup. Real loaded-mod tests belong in the isolated game;
  do not fake them in Unit/Defs or report authored scenarios as executed. Hand off contract checks
  for external integrations actually used, review dispositions, scenario registration and blockers.
- After **all selected chunks of a part** are ready, the orchestrator independently reviews the
  integrated candidate, runs the part's isolated loader/native scenarios, and **personally observes**
  the action-to-result behavior on the reviewed build. It returns product/scenario failures to the
  responsible implementers and reruns affected workflows after reviewed fixes. Implementers do not
  launch separate acceptance games. An orchestrator diagnostic before the gate is not final proof.
- Ready for integration means **implemented-unverified**, not accepted. Leave feature tasks that
  include live verification unchecked, or split implementation and acceptance obligations with
  traceability. Only the orchestrator closes acceptance from valid native evidence, including
  affected later compositions. Runtime edits invalidate affected older evidence. Root safety,
  package independence, visual gates and semantic/playability review remain mandatory.

## Specify actual behavior before implementing

1. For a replacement, inspect the exact original mod read-only. Record package/Workshop ID,
   version/hash, source locations, license/attribution, UI workflows, edge cases, options/defaults,
   observable persistence behavior and dependencies. Separate observed
   facts, inferred behavior, and unresolved questions. Never treat "implement Allow Tool" as a spec.
   **Research existing mod behavior yourself; do not ask the user to explain it or reconfirm each
   upstream feature/default.** Inspect documentation and source (or decompile read-only when needed)
   before interviewing. Ask only about unresolved custom changes, user preferences absent from the
   sources, or demonstrated ambiguities/conflicts that research cannot settle. Record remaining
   source-analysis work as research, not as a questionnaire for the user. The user moved this work
   into specification on 2026-11-02: deep-dive **one source mod or custom feature at a time**, with
   at least one owned OpenSpec change per source mod/custom feature. Read public documentation
   (including Steam) first, then source or read-only decompilation to fill gaps. Track progress and
   open questions in the refinement documents; do not stop midway to ask them. Keep specs limited
   to observable behavior, intent in proposals, and implementation analysis in designs. Refinement
   remains separate evidence; a drafted spec does not authorize or prove implementation.
2. Use [the replacement template](docs/replacements/TEMPLATE.md). Convert findings into specific
   input/action/result scenarios before code. Record assumptions about balance, durations,
   optimizations, extra fixes, and compatibility guards in the spec, not only comments.
3. For original features, record `origin: original` and follow the same spec → focused RED/GREEN →
   code → independent review → final native in-game verification process. No upstream source is needed.
4. Analyze interactions across the selected source set. If source B changes source A, specify the
   combined result, conditions/options, defaults, and precedence. Implement it with direct owned
   calls/policies. **Never retain patches against our own implementation as an internal mod stack.**
   Resolve ambiguous composition in the spec before coding; retain provenance for both sources.
5. Original-mod compatibility is outside the requested work: do not audit its API consumers, build
   compatibility facades, preserve its assembly/type/package identity or saved schema, or pursue
   drop-in/coexistence claims (including Cherry Picker). Research the source's observable behavior
   independently and implement our specified result. This does not waive contracts for genuine
   external integrations we actually use: those remain package/Def-resolved, absent-safe and
   shape-guarded under the root rules. Game-target semantic admission and our own save persistence
   remain required. Do not spoof an original provider's identity.
6. Initial support is for **new colonies, single-player, without Combat Extended**. Retaining and
   loading saves created with megamod remains required; importing existing non-megamod colonies or
   original-mod state is outside initial scope. Multiplayer and CE support are deferred, not implied
   by upstream compatibility research or adapter sketches.
   If an **original provider is loaded**, emit a bounded startup log warning identifying it and the
   corresponding owned features, and make a **best-effort** attempt to suppress those features through
   their ordinary feature settings/effective-disable path. Use the same behavior and application
   timing as manually switching those features off; do not promise rollback of all C#, XML, Def,
   UI or job effects or complete coexistence safety. Keep stored global/save preferences and
   inheritance choices intact; automatic suppression is not a permanent settings rewrite. Merely
   having an original mod installed but inactive does not trigger this policy. Preserve unrelated
   features and shared services needed by them. Never disable, uninstall, edit or unpatch the
   original provider. The warning must describe best-effort disablement, not claim compatibility.
   The 2026-11-08 clarification supersedes earlier all-phase suppression and original-mod
   compatibility requirements; do not build a full original-provider compatibility test matrix.
   Compatible unrelated mods still pass ordinary game-target semantic admission checks.
   Any later migration of our own save/settings schema needs its own contract with backup, atomicity,
   restoration on failure, and verified postconditions.

The 2026-11-07 decision records ([round one](docs/refinement/2026-11-07-decisions-round-1.md),
[round two](docs/refinement/2026-11-07-decisions-round-2.md)) record accepted refinements now reconciled
in the active drafts; see the [audit](docs/refinement/2026-11-07-decision-reconciliation-audit.md).
Follow those reconciled contracts when implementing affected behavior;
do not mistake superseded draft defaults or rejected interview recommendations for user preferences.

## Every patch must have a target spec and runtime admission check

Before adding any Harmony or XML patch, create its stable-ID document from
[the target template](docs/patch-targets/TEMPLATE.md), index it, and link its owning OpenSpec
scenario, implementation, validator, tests, and eventual live evidence. Documentation without an
executable check does not comply. Use one guarded, idempotent startup path; no unguarded `PatchAll`.

- Check the **minimal semantic prerequisites**, not an exact whole method, assembly hash/version,
  or absence of other Harmony owners. Compatible patches and unrelated changes are allowed.
- A postfix that only observes a callback may require just the method's existence and the callable
  shape it consumes. If execution of the original body matters, account for skip-original prefixes.
- A prefix changing an argument must check that the required snippet still uses that argument in
  the intended operation/control-flow context. A name/signature alone is insufficient. Match
  structural IL/data flow and required operands/cardinality, tolerating unrelated instructions,
  offsets, and compiler-local numbering. Missing/ambiguous required semantics fail closed.
- Specify original versus effective code, relevant prefix/transpiler interactions, startup ordering,
  and supported later repatch invalidation/revalidation. Original IL cannot prove semantics removed
  by another transpiler. Every transpiler must validate its actual incoming stream on every run and
  return it unchanged on failure. Do not repeatedly execute foreign transpilers as a speculative probe.
- Apply only when the target spec is true. False/unknown predicates, missing targets, validator
  exceptions, or patch-install failures leave the affected patch or declared atomic feature group
  inactive. Roll back only newly added owned patches in that group; never unpatch other owners.
- Log one bounded **startup warning per skipped enabled patch**: package/patch/spec ID, target,
  failed predicate/reason, unavailable player behavior, and fallback. No per-tick/job log spam.
  Explicitly disabled features are not failed candidates. State optional-provider activation policy.
- Test supported and incompatible shapes, benign code edits, compatible foreign patches, changed
  argument use, ambiguous matches, repeated initialization, group rollback, and warning contents in
  the appropriate honest environment. Tests alone do not accept real patched gameplay.

## XML-only features: guard content and preserve other entries

- Use XML Extensions to check that the required Defs/content and target nodes exist with the needed
  shape before mutation. External content also requires `XmlExtensions.FindMod` with
  `packageId=true`; an active package alone does not prove it still supplies the targeted Def.
- The first `XmlExtensions.*` use makes `imranfish.xmlextensions` (Workshop `2574315206`) a required
  runtime dependency, with metadata/load order updated in the same slice. Inspect actual supported
  operations instead of guessing API names. Keep declarative changes in XML, not runtime Def mutation.
- Prefer additive/merge-preserving operations whenever possible and sensible. Preserve vanilla and
  third-party entries and unrelated fields, and avoid duplicate membership. **Do not replace woody
  categories or fuel-filter lists merely to add our Thing.**
- Whole-list replacement is allowed only for an explicitly intended replacement in the spec. Explain
  why merging is unsuitable, resulting semantics/order, what is discarded, and possible conflicts.
  Verify both retained and removed content. Guarded replacement is still destructive and needs intent.
- XML patches have the same per-patch target document, startup diagnostics, failure isolation, and
  no-partial-application requirements as Harmony patches. Verify actual finalized Defs and native
  player behavior with and without the relevant external content, including other list additions.

## UI placement overrides upstream menus

Audit each source's bottom tabs and other controls, including dynamic/injected ones. Record what
each function actually does and apply [our UI decision tree](docs/UIPlacement.md); never copy an
upstream bottom tab merely for parity. Split mixed-purpose windows into their constituent functions.

1. Debugging or unrestricted cheat/override? Put it in the **top debug toolbar**, alongside native
   developer controls, and hide it when developer mode is off. A Character Editor-style appearance
   override outside sensible game mechanics belongs here, even if it looks cosmetic.
2. Mod configuration/settings, including save-specific configuration? Put it in **mod settings**. Use
   stable, clearly ordered feature/category sections and clearly labeled **Global default** and
   **Current save** controls. Document defaults, persistence, scope, and effect timing. With no active
   game at the main menu, visibly disable all save-specific controls and mutation callbacks,
   including resets/imports, with an explanation; never mutate an implicit or previous save.
3. Only toggles a current-play view or behavior? Use a **small bottom-right control**, like vanilla
   automatic rebuilding. Keep its state visible. Configure its defaults/parameters in settings;
   persisting the operational toggle does not by itself turn it into a settings window. Cheat
   toggles still follow rule 1.
4. Opens a window for controlling an ordinary game mechanic? Only this qualifies for a **bottom
   tab**. A window wrapper around settings or a boolean toggle does not qualify. Other functions
   should use the appropriate existing native contextual surface rather than inventing a tab.

Record intentional navigation changes in the replacement ledger. Source API compatibility is not
part of the UI audit. Verify the real entry-point/action/result path, settings scope separation,
main-menu disabled state, save switching and loaded-game world view, plus developer mode on/off.
Translate headings, scope labels, tooltips and disabled explanations in all required languages.

Choices specific to a pawn, area/zone, bill, building, or another gameplay entity are **gameplay
state, not settings**. Use the appropriate native contextual controls or mechanic's management
window, not the mod settings menu or inheritance model below. Save persistence alone does not make
something a setting. Unrestricted entity overrides still follow the debug/cheat rule.

## Common settings model

For actual mod configuration, the default is an editable global default plus a per-save override.
New/missing save entries **use global by default**. All features must use one common path for loading,
effective-value resolution, storage, and UI rendering; no separate settings implementations per port.
Definitions describe stable IDs, types, validation, built-in defaults, category/order and scope.

Each applicable row shows **Use global** (checkbox), **Current save** (per-save control), and
**Global default** (global control), clearly labeled. Disable the per-save editor when Use global
is checked. At the main menu, disable both the per-save editor and Use global, always; leave the
global editor usable. Enforce these restrictions in mutation callbacks as well as rendering.

Resolve inherited values from the current global default, not a copy captured when the save was
created. A save override never writes global state or another save. Store globals in global mod
configuration and inheritance/overrides with their save. Checking Use global retains a dormant
override; unchecking restores it, or seeds a first override from the current effective global value.
Specify application timing and verify reload/save switching, missing entries, and global changes
with both inherited and overridden saves.

Original-provider detection may make the corresponding feature effectively off through this same
ordinary path, with its documented timing and limitations. It must not overwrite the player's
stored global default, save override or Use global choice. Once the provider is inactive on a later
startup, resolve those retained preferences normally, subject to ordinary target admission.

Exceptions are allowed where this model is impossible or nonsensical, with the concrete reason,
actual scope/persistence, and UI behavior documented in the feature spec and definition. Exceptions
still use the common path and cannot enable save-scoped editing at the main menu. Implement this
shared mechanism with the first real setting rather than creating an empty framework now.

## Share data without sharing stale decisions

Inspect native listers first. Put genuinely shared derived Thing data in one map-owned subsystem
used by all relevant features, including future Ctrl+F-style search and hauling. Do not create one
scan/cache per source-mod port. Specify dataset keys, consumers, indexed fields, freshness,
invalidation, thread/lifecycle ownership, memory cost, and native fallback before implementation.

Share candidates, not pawn-specific eligibility. Recheck reservations, reachability, forbidden/access
state, ownership, and provider rules through the native action path at use time. Invalidate relevant
spawn/despawn/destruction/movement/transfer/stack/metadata changes; clear on map/game teardown and
rebuild after load. Invalidation patches also need target specs. Do not persist live references or
access Verse/Unity objects off-thread. Benchmark combined consumers plus update/memory costs before
claiming an optimization. No speculative cache framework before concrete consumers need it.

## Keep status, packages, and acceptance honest

Maintain [replacement coverage](docs/replacements/README.md) for every source: implemented portions,
interactions, observable deviations/possible conflicts, omissions, and not-yet-implemented work.
Record our own persistence and genuine external integrations without claiming original API parity.
Keep candidate/analyzed/specified/implemented-unverified/verified/deferred/omitted distinct. Update
the ledger and feature docs in the same slice; do not call the entire mod replaced for one feature.

Use repository typed operations for builds, package checks, tests, and isolated Gateway runs. Add
real tests under root `tests/` when their first behavior exists; no zero-test scaffold or tests that
only mirror constants. The inert initial scaffold needs build/schema/package checks, not invented
gameplay acceptance. Every subsequent behavior must pass the full root final live player-workflow
gate, personally performed by the orchestrator at its integrated part gate, on the reviewed build,
plus the balance/playability pass. Keep evidence local under ignored
`artifacts/`; include causal actions, observations, build identity, and cleanup.

Maintain all seven baseline languages with each UI feature. Add Harmony as a hard dependency when
eagerly using its types; do not bundle it or upstream assemblies. Keep the developer Gateway out of
product dependencies, assembly references, load hints, and packages. No Workshop publication or
feature acceptance is implied by a scaffold profile or successful build.
