## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
This is a documentation-only S061 replacement draft. Read the [proposal](proposal.md),
[observable contract](specs/megamod-roof-safe-removal/spec.md) and
[research](../../../mods/megamod/docs/refinement/2026-11-04-smarter-deconstruction-research.md).
All implementation, focused RED/GREEN and reviewed-build player acceptance remain open.

The inspected 1.7.0 source uses two unguarded job-toil postfixes, a prepended planning action,
native RemoveRoof jobs and a partial clone of the interrupted job. Cleanup uses static coordinate
lists without map/save/operation ownership. Native 1.7.4998 removal includes uninstalling through
the shared base class; mining destroys its target during the final strike. Roof work and support
destruction have different effects and must not be mistaken for one interception boundary.

## Goals / Non-Goals

**Goals:** make the source's roof-preparation intent explicit; preserve physical work and ordinary
permissions; compose owned removal/quality/replacement paths; keep player area edits and saved
continuations recoverable; admit only the effective semantics used by each integration.

**Non-goals:** automatic protection from combat/fire; a new mountain drawing tool; importing old
settings/jobs; new roof materials, salvage or research content; a general construction planner;
an upstream package/type impersonation layer; blanket provider/multiplayer/performance claims.
No new runtime framework, XML operation or art asset is implemented in this change.

## Decisions

### 1. Separate facts, removal intent and authorized actions

Use native roof/edifice/area/designation grids as authoritative inputs. The source's 13.8-cell
collection and 6.9-cell subtraction are evidence, not a sufficient oracle: origin-only support,
mixed designation types, connected roof continuations and concurrent work can change the result.
Native roof support uses 6.9-cell connectivity and complete actual edifice occupancy; collapse also
checks disconnected roof components. Evaluate the affected connected consequences without invoking
the mutating collapse routines as a prediction API. No fixed search limit may return safe when the
unexamined remainder matters. If a bounded evaluation cannot finish, retain a visible pending state
and resume bounded work or disable the unsupported case; never turn incomplete analysis into safety.

Represent the specific removal target plus map/logical-level identity and relevant pending support
removals. Gather ordinary mining/MineVein/deconstruct/uninstall and owned removal intent through
their actual contracts. A designation need not mean imminent work, so this conservatively prepares
more roof when multiple supports are marked; record that balance tradeoff. Recompute after meaningful
roof/support/order/provider changes and immediately before mutation. Do not cache pawn eligibility.
Any shared derived Thing facts use the common map index with S121/hauling; do not add a per-port
Thing scan or persistent duplicate index. Small transient connected-cell work belongs to this action.

Alternative rejected: port the source flood fill unchanged. It cannot establish the broader safety
contract now specified and its two source constants are not an independent native compatibility test.

### 2. Use native physical jobs, but retain complete continuation ownership

The source `JobInfo` stores only Def, forced flag and targets A/B/C. Do not copy that pattern into
owned work: preserve the real intent and all fields relevant to that exact admitted job, including
worker/provider/queue choices and actual removal progress. Prefer retaining the native driver where
possible; otherwise use a narrowly specified continuation for that concrete path. No generic job
serializer or arbitrary reflection clone is selected. Unsupported external job forms are not resumed
with silently dropped fields. Honor native cancellation, designation, forbidden, faction, reach,
danger, area, work capability and ceiling/target reservations at use time.

The first worker can prepare only when it is actually allowed to do roof work. Otherwise expose
ordinary eligible roof work and hold the original target. Native ordered-job creation alone does not
prove work capability. Reserve only current actionable work, release on every exit, and avoid the
source's eager reservation of an entire future roof queue. Farther-first is a preference subordinate
to valid geometry and safe intermediate consequences. Native removal may clear adjoining unsupported
thin roof; do not separately charge or duplicate jobs for already removed cells. Check mountain
permission on every worker acting for an owned preparation, not only the initiating worker.

Preserve the inspected 65 roof-work units and ConstructionSpeed * 1.7 tick rate provisionally,
including for enabled thick roof. Use native values/modifiers through admitted semantics rather
than overwriting Def stats. Mining HP and removal-driver work are different progress forms. A
completion-time hold must precede damage/destruction/results and cannot repeatedly award work XP,
reroll failure, duplicate yields or replay completion notifications. Actual completion seams remain
an implementation task, particularly effective foreign final-strike/completion changes.

### 3. Treat roof areas as player intent with an owned temporary overlay

Maintain map-owned preparation records with stable target/job identity, logical level, unresolved
cells and the displaced player area choices. Shared cells need an owner set/reference count only
for real concurrent consumers. Preserve explicit player edits as changes of intent even if the
visible boolean equals an earlier temporary value. Reuse admitted native area edit paths to observe
those edits; don't infer intent from one boolean comparison. Exclude our own writes from those
notifications. Save only serializable unresolved intent, not delegates, live reservations or static
global coordinates. Reload resolves actual maps/targets and rechecks current permission/safety.

Apply the temporary NoRoof/BuildRoof effect coherently. No unguarded NoRoof mark may allow another
worker to continue owned mountain removal after its research/permission is revoked. Each worker
must pass the owned permission check; remove/suspend no-longer-authorized temporary work before it
can be taken. Player-owned independent roof orders remain outside that ownership. An explicit
player edit can stop this preparation; do not fight it by automatically rewriting the same mark.

Release the last related owner on completion, explicit cancellation/abandonment or loss of target.
Restore only unchanged displaced intent, with no rebuild between remaining related removals. Map
teardown and save switching cannot affect another map. Destruction of the feature's target while
roof work is in flight must invalidate that continuation. Physical removed roof remains removed.
The native Build roof system decides whether restored orders can be built after support changes.

Alternative rejected: copy the source static lists and last-toil cleanup, or restore every captured
area on finish. Both lose ownership when another operation or the player edits the same cell.

### 4. Keep configuration explicit and distinguish missing content

Use the common settings definitions/load/store/effective-value/UI path with stable IDs under this
feature. Seven rows and defaults are in the spec. Worker filters intentionally separate player
animals from Other workers; wild humans follow the latter. This corrects the source's requirement
to enable both booleans for a player animal. Preserve all ordinary native eligibility afterward.

Store research as an explicit None or stable selected-project identity, retaining unavailable
identity/label for display. The selected default is None, matching the inspected installed set
without one of the source's three candidate projects. Do not retain source `explicitNull` state or
the serialization-time auto-selection heuristic. Any future provider-specific automatic research
choice needs its own inspected canonical package/Def contract and amended observable spec.
Required project missing/unfinished means permission denied, not None. UI search must retain the
selected row even outside search results. Settings changes apply at action boundaries, with saved
held work remaining stopped when its family is disabled. Explicit Retry/Resume reauthorizes a held
intent; returning a setting to enabled alone does not recreate a canceled hazardous attempt.

Mountain cancellation and actual permission are separate. Forced work or Cancel=false bypasses
only unremovable-mountain cancellation; it never makes a missing required project finished. With
Cancel=true, stop the correct local Mine/MineVein/Deconstruct/Uninstall intent. For S013/S028,
hold the parent target so its automation cannot recreate the same canceled attempt. Ordinary
construction also needs a held-parent case: current GenConstruct.HandleBlockingThingJob creates
Deconstruct or Mine with ignoreDesignations=true, and blueprint/frame workgivers can recreate that
child without a standalone designation. Track the actual parent and blocker, preserve project
resources, persist the hold and suppress repeat attempts/messages while the obstruction is unchanged.
Changed roof/support/permission invalidates the verdict; ordinary continuation requires resolving
the obstruction. Contextual Retry rechecks; parent cancellation releases only that hold. A newly
forced native action accepts only the documented mountain risk. Disabling the family or losing
admission still follows the separate explicit-resume rule. S008 accepted plans create ordinary
designations once; cancel the relevant cell without inventing a persistent mining parent. A foreign
worker cannot delete the colony's order. Keep reason/notification bounded by the stopped intent,
not one message per tick or repeated job scan.

### 5. Compose owned features at their actual removal boundary

S008 contributes selected mining cells and ordinary designations, not a second roof patch. S013 contributes
quality-cycle state and cost; roof waiting/cancellation preserves that same cycle budget. S028
contributes material replacement, different-building upgrade and minified install-over transactions.
Determine whether each transaction actually loses roof support, including any temporary destruction
interval that native collapse would observe. A same-footprint supported swap is exempt only if the
effective transaction preserves support continuously. Do not destroy first and try to restore later.
If preparation cannot complete, keep old building, contents and supplied materials recoverable.

S060 construction access still applies to its own covered finishing paths, independently of this
roof-removal check. Its minified-install exclusion does not exempt removing an old roof support
during S028 install-over. Implement ordinary owned calls, not Harmony patches against our features.
Avoid widening S013's best-builder restriction to roof work or deconstruction; native roof-worker
eligibility remains separate. No new work priority beyond the prerequisite relationship is selected.

### 6. Admission covers effective mutation semantics, not merely two method names

Each future patch needs a stable target document, executable validator, owning scenario and tests.
Candidate boundaries below are design obligations, not claims that a patch is already implemented.

| Target family | Minimal required semantics and failure boundary |
| --- | --- |
| RemoveBuilding job path and concrete completion | Correct target/map and job lifecycle; actual inherited deconstruct/uninstall/deconstruct-for-blueprint path; veto before support despawn, notifications and results; retain ordinary work/eligibility |
| Mine final strike and continuation | Target/remaining HP, designation or forced permission, destruction/yield/mine-vein expansion boundary; no final damage or reward before the gate |
| Roof work selection and effect | Actual roof cell/map, ceiling reservation, ordinary work and removal/collapse effects; owned permission checked before action, including another worker taking temporary work |
| Area edits, lifecycle and persistence | Player edit intent distinguishable from owned writes; cancellation/death/map/load callbacks and ownership release remain available |
| Optional logical-level/roof provider | Canonical package plus exact callable/Def shapes; real level/band/gutter/roof restrictions and coordinate conversion, not merely mod presence |

Treat the required cross-path safety/lifecycle patches as one atomic feature group unless a narrower
group is demonstrated safe by its contract. Roll back newly installed owned members on failure,
never foreign owners. Effective prefixes skipping originals, transpilers changing final mutation,
or later repatches matter; a whole-assembly hash, no-foreign-owner rule or bare MakeNewToils existence
cannot certify the contract. Tolerate unrelated instructions and compatible foreign modifications.
If a future transpiler is necessary it validates its actual incoming stream every time and returns
that stream unchanged on failure; do not execute foreign transpilers repeatedly as speculative probes.
False/unknown predicates, exceptions and install failures produce one bounded startup warning per
enabled skipped patch with patch/spec/target, reason, unavailable behavior and ordinary fallback.
Unsupported optional maps do not silently use flat-map assumptions. Saved owned held work stays
stopped/recoverable; ordinary new native jobs remain available without an owned safety guarantee.

### 7. External consumers require an owned adapter rather than a false facade

Bounded installed ASCII/UTF16 inspection found one runtime consumer family: As above, So below II,
`astryl.AsAboveSoBelow2`, Workshop 3776015553, selected 1.7 assembly. Its adapter discovers
`SmartDeconstruct.SmartDeconstructMod.CheckForRoofsBeforeJob(IEnumerable<Toil>, JobDriver)`, wraps
source-assembly-owned init delegates, then clamps scoped native flood fills to a logical band and
excludes gutters. A method-signature facade alone would not reproduce those delegate/scoped-fill
assumptions. Do not emit that upstream type or spoof its package/assembly. Share the concrete
logical-level provider boundary with S060 when implemented, using direct owned geometry decisions;
full provider APIs/admission and exact provider-present live proof remain open.

The other scan hits are metadata/replacement mappings and test mod lists, not callable contracts.
Both original packages (`legodude17.smartdecon`, `Mlie.SmarterDeconstructionAndMining`) cause owned
overlap to stand down; do not unpatch them or modify configuration. Source global settings and
static temporary-list state have no selected migration. Presence scans are bounded evidence, not
proof that no other external consumer exists.

## Risks / Trade-offs

- More complete roof analysis and final checks cost work → use native facts, scoped dirty state and
  measured combined-consumer costs; no performance claim before benchmark/native workflows.
- Marked-but-distant support removals can clear roof earlier → explicit conservative plan semantics;
  preserve safe roofs and expose the real work/collapse consequences during acceptance.
- Roof-capable labor can delay miners or mechs → retain permissions and visible wait; never create
  a job capability just to make the queue progress.
- Mountain removal at ordinary roof cost changes terrain/progression substantially → off by default,
  explicit research choice and provisional work contract; compare native thin roof, source thick
  preparation and an admitted provider before accepting balance.
- Source co-installation or drift stops owned continuation → keep actual work/choices recoverable,
  require explicit new authorization to resume and document absence of replacement compatibility.
- Roof providers and connected collapse can widen effects → inspect exact APIs and native intermediate
  consequences; no synthetic state-only assertion substitutes for player acceptance.

## Migration Plan

No runtime or data migration occurs now. Future feature slices add Harmony only when actually used,
retain positive package allowlists and keep the Gateway dev-only. Use the new owned schema for new
preparation; no silent import of source globals or native queues. A future importer requires its
own backup, atomic write, restoration-on-failure and verified postconditions. Feature disable/source
presence retains held owned work and performs ownership-aware temporary cleanup, not blind deletion.
Safe mod removal from arbitrary saves is unclaimed until explicitly specified and verified.

## Open Questions

- **DM-Q1:** retain or revise the proposed worker-option clarification, None research default and
  ordinary 65-work mountain cost after user review and measured progression comparisons?
- **DM-Q2:** prove effective final-strike/completion gates, exact native continuation fields,
  connected-roof bounds and area-edit intent across every selected removal path.
- **DM-Q3:** inspect canonical level/roof/worker providers, direct shared adapters and all actual
  consumed shapes; no inherited source bridge or universal compatibility promise.
- **DM-Q4:** perform focused honest RED/GREEN, independent runtime review, shared-data/performance
  measurements and reviewed-build native removal/roof/reload/concurrency/settings workflows.
