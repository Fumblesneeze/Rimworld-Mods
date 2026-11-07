## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
S013 / P1-10 replaces the researched behavior of **QualityBuilder Unofficial 1.6**,
`hatti.qualitybuilder`, Workshop 3512466087. This is a specification draft, not implemented code.
The [research journal](../../../mods/megamod/docs/refinement/2026-11-02-quality-builder-research.md)
records public-documentation-first inspection, pinned source, installed DLL/native hashes, save keys,
defaults, code-derived defects and actual consumer bindings. The
[coverage report](../../../mods/megamod/docs/replacements/quality-builder.md) distinguishes these
findings from the proposed result and eventual acceptance. Source support ends at 1.6; no inference
of working 1.7 integration is made from its similar API names.

Inspected upstream source: commit c063417d46b2252d15851bdd02a6712a5a971d0d, installed DLL SHA256
A9759C6C3675644AB165FE256EB58FE50633DF5E3FC516F8B113AEE60E141247. Pinned source's committed DLL
matches the installed binary; critical control paths were also checked in decompilation.
Native inspection uses 1.7.4995 rev309. These hashes identify evidence, not runtime admission rules.

## Goals / Non-Goals

**Goals:** source-derived skilled finishing and ordinary retry workflows; explicit safe cancellation,
retargeting and exhaustion; per-project persistence; common configuration; map-correct candidate data;
documented external contracts and direct composition with selected replacements.

**Non-goals:** guaranteed quality, altered rolls/costs/XP, instant upgrades, a separate bottom tab,
copying the source's map-settings architecture, speculative reflection facades, automatic old-save
migration, whole-source compatibility claims or implementation in this documentation slice.

## Decisions

### One owned project lifecycle rather than independent post-processing patches

Represent quality intent on the actual construction project through blueprint, frame and building
transitions, with an explicit run identity and state for pending removal, placement-blocked intent,
construction, satisfied, canceled and exhausted. Keep state local to this behavior; do not introduce
a general workflow engine. Persist only the minimal project choices, counters and durable identifiers
required for restoration. Re-resolve native references after load. Missing targets/configuration get
visible recovery, not replacement-by-cell guessing.

The source wraps `Toils_Construct.MakeSolidThingFromBlueprintIfNecessary`, attaches a finish action
to the final `JobDriver_ConstructFinishFrame` toil, and hooks `JobDriver_Deconstruct.FinishedRemoving`.
Native 1.7 has actual completion/failure results on `Frame`; a construction toil also ends after
5000 ticks or interruption. Therefore observing a final toil end is insufficient to identify a
successful building. Source failure fallback takes a comp-bearing object at the cell; replace that
heuristic with provenance from the actual native transition. Never share a static captured map across
callbacks as the source does. Discover the smallest reliable native seams before choosing hooks.

Retry removal increments once at initiation, independent of construction failures; recovery retains
the cycle. Ordinary unrelated auto-rebuild is a new run. An explicit exhaustion state and notification
marker prevent repeated messages/reload resets. The source's `desiredMinQualityReaced` flag is never
set false when raising an achieved target, which can schedule destruction then reject reconstruction.
Recompute the intended state transition instead of carrying that stale flag pattern forward.

### Separate owned removal from native manual orders

Track the exact removal and project that quality automation initiated. Native cancellation must
invalidate its pending continuation, including an already running removal job. Check that ownership
immediately before destructive work commits. Ordinary manual deconstruction is not acquired merely
because it targets a marked building. If native cancellation cannot be intercepted reliably, the
destructive feature group cannot be admitted; a log-only workaround does not satisfy the contract.

Cancellation after destruction cannot restore materials or the old Thing. Retain an existing ordinary
blueprint on disable, or discard an unplaced blocked intent visibly. Retry placement must be idempotent
and must not consume an additional cycle. Keep any needed native reservation cleanup exact to the
removed object and owned work; do not clear reservations on a new occupant of the same cell.

Startup rejection and original-provider overlap also require load-time reconciliation of saved owned
removal. Merely declining new hooks leaves persisted native deconstruction executable. Keep owned
intent distinguishable from native manual/source orders, quiesce its saved designation and active
job before simulation can commit destruction, and retain the intact building and run state as
suspended. This protective load path must function when the retry group is not admitted; it cannot
depend on that group's successfully installed destruction/reconstruction hooks. Establish a safe
serialization/load ordering contract before ever creating persistent owned removal. When support
returns, only explicit Resume reactivates the already charged cycle; no budget reset or automatic
destruction on load. QB-Q2 includes proving this path with a real save and rejected admission.

### Preserve consequential configuration before destructive work

Capture applicable native storage settings/group, bill definitions/order and ownership/assignment
choices before removal, using their actual supported transfer contracts. Native `Frame` already
transfers storage state on completion/failure; use those paths where sufficient. Do not assume the
upstream retry's same Def/Stuff/style recreation preserves anything else. Cooperate with the future
whole-list bill-linking owner rather than cloning another independent bill model here.

Determine support before designating removal. Unknown consequential mod state blocks automatic
replacement of that building with an explanation. This is a deliberate safety boundary, not a claim
that every custom building can be serialized generically. Preserve invalidated choices as explicit
recovery information where possible, without assigning a different pawn, recipe or group silently.
Native restrictions on contents/occupants remain in force. Minification/reinstallation preserves the
same Thing's intent; no old-cell callback may act on a different object. QB-Q1/Q2 cover exact seams.

### Eligibility is a map-local policy over native candidates

Use native `MapPawns`/work eligibility before adding derived data. The source caches best skill for
10000 ms on its settings object without a map key; inheriting maps share that object. Do not reproduce
that cache. If profiling justifies shared derived pawn facts, scope them to the map and invalidate
presence, skills, capacities, work enablement, mental/prisoner/downed status and assignment changes.
Never reuse a prior game's references or add a per-source full-map scanner. Thing/hauling facts,
if needed by a later consumer, use the foundation's shared map-owned data rather than this policy.

Evaluate ordinary finish eligibility at the native work boundary. Source `JobOnThing` can kick a
different worker while merely querying work, with a 600-tick per-frame cooldown. Our queries remain
pure; an active ordinary job checks its own eligibility before the next construction progress.
Forced jobs bypass only our policy, not native restrictions. This removes the need for the source's
static frame-ID kick dictionary. Explicit named assignment is saved map gameplay state, exposed
contextually, not stored in global/save configuration. Temporary invalidity falls back visibly.

### Shared configuration and contextual UI

Use the common configuration definitions/resolver/storage/row renderer; the capability states every
initial value, range, order and timing. Do not clone global defaults into independent map settings.
Actual source keys/defaults remain evidence in the journal, not adopted save names. Entity target,
counter, pending work and named map assignment are separate gameplay state.

Expose Set/Remove under Orders and target/enable/status actions on the relevant selection. Provide
explicit target selection as well as the source-like context menu, so right-click is not the only
discoverable route. Keep inspect access when the optional finished gizmos are hidden. Bulk operations
are per eligible Thing rather than constrained by the highest finished quality in a mixed selection.
No bottom tab or debug shortcut is introduced. Localize all labels/reasons and inspect the seven
baseline languages and relevant screen sizes in final native acceptance. New raster art requires
the repository asset workflow; these documents choose no icons or source artwork.

### External contracts require evidence, not package impersonation

Actual consumers found by installed source/binary scans:

| Consumer | Binding | Chosen boundary |
| --- | --- | --- |
| VEF, `OskarPotocki.VanillaFactionsExpanded.Core` | Package `hatti.qualitybuilder`, reflected `QualityBuilder._WorkGiver_ConstructFinishFrames.Postfix`; bypass for jobs whose giver derives from `VEF.Pawns.WorkGiver_ConstructionSkill_FinishFrames` | Its source-specific patch does not bind to megamod. Trace job.workGiverDef timing and intended provider restriction before a narrow optional adapter; no blanket mech exception |
| Selected Replace Stuff - Continued, `Memegoddess.ReplaceStuff` | Unqualified `CompQualityBuilder`, `CompProperties_QualityBuilderr`, designation `SkilledBuilder`; generated replacement-frame component/marking | S028 must compose quality state directly with material replacement. External co-installation support remains QB-Q3 until exact path verified; do not self-patch or spoof old IDs |
| QualityBuilder's outgoing Replace Stuff integration | Old package `Uuugggg.ReplaceStuff` or exact name `Replace Stuff`, `PlaceReplaceFrame` postfix copies enable/target | Neither detector matches the installed continued identity; replace detection guesses with canonical package/shape probes if an external adapter is actually needed |
| Retexture `JGH.qualitybuilder` | Hard dependency/load-after source package; Skilled, Skilled1…7, UnSkilled texture paths | Explicitly not an automatic drop-in contract. No source package impersonation or artwork copied |

The VEF work-giver is extension-based construction, not necessarily robotic. Its intended skip cannot
be claimed effective without checking when the job metadata exists. A reflection facade alone would
not satisfy the source package checks and would recreate internal source coupling; no facade is
proposed without a complete demonstrated contract. Original QualityBuilder co-installation disables
only our overlap at startup. Absence of optional providers is normal; detected changed shapes fail
only the relevant adapter/group with a bounded explanation. A changed custom work path must retain
native/provider restrictions even when our retry behavior is unavailable.

### Per-patch semantic admission and additive Def changes

Create real target-spec documents, tests and executable validators with the first implementation;
the following are research boundaries, not approved patch targets:

- Quality-bearing building/build-blueprint/frame availability and component shape. Prefer guarded
  XML Extensions additions for static Def changes. Generated Defs may require a lifecycle hook if
  XML cannot express the relationship; document why and validate the finalized shapes. Preserve
  unrelated components/designators; add each owned entry once. No wholesale list replacement.
- Native Orders/contextual UI insertion shape and callable contracts; presence may suffice only for
  a purely observational hook. No unconditional `PatchAll` or injection on every resolve without an
  idempotence condition.
- Finish eligibility result/forced semantics and active-work progress boundary. If a hook changes a
  parameter, validate the required consuming code/dataflow, not just method existence.
- Exact blueprint/frame/completed-building/failure handoff, owned removal/cancellation commit and
  placement return identity. Group destructive retry hooks atomically so missing cancellation or
  reconstruction support cannot leave only destruction installed.
- Native auto-rebuild and minification/reinstall state transfer; optional provider shapes and
  effective behavior after applicable foreign prefixes/transpilers.

Allow benign foreign patches and unrelated body changes. Account for skip-original behavior when
depending on native completion. A transpiler validates its actual incoming instructions every run
and returns them unchanged on ambiguity/failure. Roll back only newly installed owned hooks in a
failed atomic group. Log bounded startup warnings with patch/spec/target, failed predicate, affected
behavior and fallback. Disabled/co-installed overlap is an explicit policy, not a patch failure.
Any XML Extensions use makes its package a required dependency; eager Harmony use requires Harmony.
No dependency or metadata is changed by this draft.

## Risks / Trade-offs

- [Automatic replacement removes useful furniture and spends resources] → Keep source-derived
  three-cycle default, describe costs, retain the last result on exhaustion and require explicit restart.
- [Some quality targets are unlikely or impossible for a particular builder] → Never promise success;
  finite limit and cancellation remain visible. Unlimited is opt-in and still ordinary timed work.
- [Named/drafted best builder delays construction] → Source-like waiting is explicit, with map
  assignment, relative limit and deliberate forced-order escape routes.
- [Custom building state has no safe transfer] → Keep the building intact and report unsupported
  automatic replacement; add a narrow supported transfer only after a concrete contract exists.
- [Source bugs are code-derived, not reproduced] → Label evidence honestly and test our proposed
  cancellation/retargeting outcomes rather than claiming an upstream live failure.
- [1.7 or provider changes invalidate hooks] → Semantic admission and ordinary native fallback;
  exact predicates remain open until implementation research, not whole-assembly hashes.

## Migration Plan

No code, packages, existing saves or settings are changed now. Future first implementation introduces
owned state and common settings through focused vertical slices. Source `Quality`, `desiredMinQuality`,
`desiredMinQualityReaced`, `pendingQualityRebuild`, `qualityRebuildAttempts` and `qbUseMapSettings`
keys are documented but not automatically imported. Source removal does not prove migration safety.
If migration is requested later, specify backup, atomic conversion, failure restoration and verified
postconditions before offering it. Do not remove Workshop providers automatically. Rollback of an
implementation must keep ordinary native construction available and must not leave owned removal
work active without its reconstruction/cancellation handling.

## Open Questions

These are recorded research/acceptance tasks, not questions to interrupt the user:

- **QB-Q1 — native 1.7 seams/admission:** exact successful/failed construction identity, forced job
  metadata, active eligibility boundary, generated Def wiring and late patch invalidation; select
  minimal semantic predicates and atomic groups from effective code.
- **QB-Q2 — lifecycle/configuration:** native cancel during active removal, placement obstruction,
  minify/reinstall, map transfer, ordinary auto-rebuild and save/load; enumerate supported native
  configuration and provider-specific gaps before destructive implementation.
- **QB-Q3 — consumers/composition:** VEF giver metadata timing/intended exception; external continued
  Replace Stuff bindings and S028 direct composition; document unsupported retexture/source-save
  bindings and any actually necessary facade. No presumed compatibility.
- **QB-Q4 — native UI, balance and performance acceptance:** seven languages, batch orders and
  settings scope, realistic costly retry workflows, drafted/busy experts, multiple maps, optional
  provider runs and measured candidate-update cost. No optimization claim before measurements.
