## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: original, S014 / P1-11. The user confirmed synchronization of the entire bill list, including
add/remove, order and every bill's configuration. Source provenance is the interview, not a selected
upstream linking implementation. The [research journal](../../../mods/megamod/docs/refinement/2026-11-02-workbench-linking-research.md)
records bounded native inspection; [coverage](../../../mods/megamod/docs/replacements/linked-workbenches.md)
separates confirmed intent, proposed refinements and unimplemented work.

Inspected native game: **1.7.4995 rev309**, Assembly-CSharp SHA256
**56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21**. Read-only decompilation is
under ignored `artifacts/Refinement/megamod-spec-20261102/workbench-linking/game`, with reused
ITab_Bills/BillUtility evidence from the earlier Quartermaster slice. No game has been launched for
this original-feature draft. Hashes identify evidence, not runtime compatibility predicates.

## Goals / Non-Goals

**Goals:** one whole-list gameplay contract; distinct native execution objects; consistent edits and
remaining-order accounting; ordinary and unfinished/autonomous/mech production; explicit membership
operations, work preservation and failure recovery; direct composition with selected owned features.

**Non-goals:** medical bill linking, remote inter-map production, free ingredient transfer, copied
machine/unfinished progress, changed recipe economics, partial per-bill links alongside local rows,
new bottom tabs/settings architecture, or adopting all future BWM/provider behavior before research.
This slice writes documents only. It does not implement an event bus, generic synchronization engine
or runtime compatibility facade.

## Decisions

### One authoritative logical list with real local native bills

Use a map-owned group record with stable group and row identities, ordered shared choices, repeat
accounting and concrete member/work associations. Each physical member still owns real native Bills
in its own BillStack. The native `Bill.billStack` has one giver; using the same Bill object in multiple
stacks would overwrite map/position/lifecycle context. Conversely, periodically cloning independent
lists would erase object identity, unfinished bindings and completion history. Keep native objects
stable while their logical rows persist, applying deliberate changes to those objects.

Separate shared choices from local execution state. Include all actual editable fields: recipe,
precept/style/globalStyle/graphicIndexOverride, supported special choice such as xenogerm, name,
suspended, ThingFilter/special filters, radius, worker/range restrictions, repeat and target values,
count filters/scope and output mode/group. `Bill.Clone` omits graphicIndexOverride in the inspected
build; class-specific Clone is therefore not proof of a complete portable contract. Do not copy
`nextTickToSearchForIngredients`, bound unfinished Thing/worker, active job or autonomous state as
shared configuration. Retain those in their native owners.

Use a concrete representation adequate for the observed bill families. An optional custom family
needs a narrow adapter for its actual choices and lifecycle, not reflection-copying every private
field. Unsupported consequential choices must be reported before linking/replacing a list. The goal
includes native autonomous/mech families; do not silently redefine support as ordinary Bill_Production
only merely because its state is simpler.

### Transactional membership and edits through owned operations

Create/join/merge selects one authoritative list and validates all resulting members and rows first.
Hold exact prior configuration/membership/accounting until commit and verify the resulting mappings
before work can observe them. On failure restore that state and remove only newly created owned
objects; preserve physical materials and existing job references. No filesystem save rewrite or
external migration is proposed. Work with an existing source row keeps native identity. A destination
with displaced active/bound work is rejected, avoiding implicit cancellation, resource recovery or
heuristic migration by matching recipe name. Merging absorbs the entire destination group.

Whole-list replacement is intentional user scope. The preview states which destination bills/counts
are discarded; concatenation or merging by recipe would violate it. This is gameplay state, not an
XML Def-list replacement. Native deletion must use its actual notification contract: `BillStack.Clear`
only clears a list, while Delete marks the Bill and notifies the giver. For autonomous stations that
notification ejects contents and clears the active bill. Replacing idle lists still respects those
native object lifetimes; do not make stale windows/jobs dereference a new row by index.

Centralize direct owned Add/Edit/Delete/Reorder/membership actions and compose selected browser/bill
features through them. For native UI writes (many are public field assignments in Dialog_BillConfig,
Bill.DoInterface and repeat-mode menus), choose bounded interception/reconciliation at the actual
edit and job boundaries. Do not implement a whole-map every-tick equality scan or a nested Harmony
stack between our features. External direct writes need an explicitly supported contract; detect
unsupported consequential drift and suspend the affected group before it can execute divergent
orders. WB-Q1 must establish coverage of all supported edit paths, including paused UI, rename,
style variant, storage validation and provider-specific fields.

Stable row IDs and a group revision distinguish duplicate recipes, deleted rows and concurrent/stale
dialogs. Apply the intended field edit to current state, preserving unrelated newer fields; reject
stale deletion/membership actions instead of resurrecting rows. The common catalog is the intersection
of supported recipes/behaviors across members. This is a proposed semantic choice, not an upstream
fact. Class/support loss retains a recovery row and stops only that row's new starts; local power or
ingredient shortages remain ordinary per-bench workability.

### Finite quota accounts for committed iterations, not replicated counters

Define remaining R to include unfinished admitted iterations C, with available-to-start R minus C.
For an unchanged finite order maintain 0 <= C <= R. A successful native iteration decreases R once;
its unfinished commitment is removed once. An aborted non-resumable iteration releases C without
decreasing R. A still-resumable unfinished item/process keeps its commitment when a pawn job ends.
Queries and UI previews never acquire commitments. Admission occurs only after actual native job
eligibility/reservation succeeds and before the iteration can consume/start physical work; failure
cleanup cannot leave a ghost commitment. Resuming exact existing work reuses its identity.

This is needed for two benches with one remaining repetition: copying `repeatCount = 1` admits both.
Simply synchronizing decrements at completion is too late. Preserve ordinary bill/subclass methods
and recipe-worker notifications once, but project their visible remaining counts from one group
record. Native count completion is an iteration, not number of items; a bulk recipe can yield many
items for one repetition. Lowering R below C is rejected explicitly, and changing from unlimited/
target mode to finite reconciles existing commitments before accepting the count.

Ordinary `Toils_Recipe.FinishRecipeAndStartStoringProduct` creates/consumes outputs and calls
Notify_IterationCompleted before hauling. Autonomous/mech machines can create a held result during
Notify_FormingCompleted, before the later native collection/completion callback. Select a proven
per-family irreversible production/completion point, record its credit exactly once, and distinguish
remaining collection from permission to start a new iteration. A ready local process must remain
finishable when logical new-work quota is zero; do not force another product to be made merely to
extract an existing one. Native recipes legitimately producing zero objects can still complete an
iteration; no generic “count spawned Things” heuristic is adequate.

Group creation from an already working source reconciles unfinished versus already produced-but-
uncollected work in the preview. Do not charge completed machine output a second time when it is
collected, or treat it as an unmade finite commitment. WB-Q2 owns these exact class-specific seams,
initialization and load/interrupt cases. Maintain only the minimal completion/commitment identities
needed for idempotence; no generalized production scheduler or off-thread game-object access.

### Target counts use one common stock scope and native counting

Keep targetCount, pauseWhenSatisfied, unpauseWhenYouHave and paused state authoritative at group
level. Count with the same native RecipeWorkerCounter/context and selected map/storage scope once,
not the sum of identical counts from each projected bill. Use native listers/resource counters first.
Any genuinely shared derived stock data belongs to the existing map-owned shared-data policy with
other search/hauling consumers, not a cache per bill or member. Validate physical stock freshness at
use time and measure combined update cost before claiming an optimization.

Do not reserve speculative future target-mode stock or invent exact-count scheduling: already
admitted ordinary work can overshoot, as specified. Hysteresis is common, so reading another member
does not create a separate pause latch. Uncountable recipes keep native Repeat/Forever behavior.
StorageGroup/SlotGroup references remain concrete entities; new linked stockpiles extend that owner,
not a second storage aggregation in this feature.

### Physical work and deletion stay native

Native JobDriver_DoBill stores workLeft, timing, a concrete Bill and station/ingredient targets;
it reserves the actual station and fails on deletion, despawn or unusability. UnfinishedThing stores
real ingredients, creator, work and one reciprocal bound Bill. Its explicit native cancel returns
randomly rounded 75% of ingredient quantities; deleting its bill is not that cancel action. Never
clone a physical unfinished Thing or promise full material recovery as part of list synchronization.

Bill_Autonomous carries forming stage/time and a local station activeBill; Bill_Mech additionally
binds a mechanitor, bandwidth-dependent work and gestation cycles. Resurrection keeps an actual
corpse/mech identity. Preserve those local states while editing shared choices. Independent benches
can work concurrently under the shared plan; no single boundUft field may serialize the whole group
accidentally. Existing work keeps committed materials/worker, while subsequent starts use new
filters/restrictions. Current style/output choices take effect only at the native stage where they
are still choices; never mutate a physical formed result to retroactively match a new preference.

Row deletion invokes ordinary family-specific cleanup across members once. Lost ordinary work remains
lost; ingredients/items are retained/ejected/unbound according to native semantics. Cancellation of
an actual unfinished item is a separate player action. Work resumes through native paths and creator/
provider rules, including after a member is lost. Bindings to an exact logical row prevent one of two
identical recipes from acquiring the other's completion credit.

### Separation preserves the total finite remainder

Before unlink/dissolve, partition every finite row's commitments by the physical station/work that
will own them. Assign the uncommitted remainder to one explicitly chosen successor; default to the
continuing group on Leave and the selected bench on Dissolve. Show the resulting counts. The sum of
successor remainders equals the prior R, and each C belongs to exactly one successor. Other shared
choices are copied into complete independent lists. This is an explicit proposed refinement rather
than the ordinary naive copy that duplicates quota.

Keep each station's native bill objects/work where possible; the operation changes logical ownership,
not physical contents. Target/Forever rows become independent plans with the stated preview; their
current choices/latch are copied, then evolve independently. A single remaining member receives the
whole remaining plan without a redundant group wrapper or silent quota loss. No source-of-truth
member election is required during ordinary operation because the group itself owns the plan.
Independent local plans must retain the minimal pending completion-credit/collection handling until
already credited output is collected. Native Bill_Production.ShouldDoNow returns false at a zero
repeatCount, including the base check in Bill_Autonomous; dropping all owned state at separation
would strand a Formed result. Leave, Dissolve and last-member conversion therefore transfer that
local continuation state without retaining synchronization or permitting another production start.

### Lifetimes and recovery require a load path independent of successful patches

Save stable group/row IDs, member references, choices, counts/latch and real work associations through
owned save state. Reconcile after native cross-references resolve but before production can resume.
Do not infer membership from equal names/recipes/cells. Use exact same-Thing identity for minify/
reinstall and an explicit handoff for supported quality/material reconstruction. A missing member's
surviving unfinished product stays a group commitment if resumable; released/lost processes do not
count as success, and already-produced output never restores quota.

Same-map unplaced members remain dormant and receive saved configuration updates. Transfers require
partitioning at the actual map departure/arrival, not letting old map-local storage/pawn references
leak into a new run. A whole moving group retains its internal structure and finite R; partial moves
keep unstarted counts on the original side. Transferred plans are suspended until the player reviews
unavailable references and explicitly resumes. Native gravship special handling is an implementation
research item, not permission to bypass this contract.
Partition by the physical work's actual location, not merely its former bench pointer. If a bench
transfers but its standalone unfinished Thing stays and an original group survives, keep that item
and commitment with the original logical row. If the entire group moves without that item, retain
one unavailable commitment in the moved plan and expose its location/recovery; do not grant a fresh
start against it until actual recovery, explicit native cancellation or ordinary irrecoverable loss.
No item is teleported, duplicated or implicitly refunded. WB-Q3 must prove exact reassociation and
teardown events without allowing either side to count the same physical work twice.

Native `ValidateSettings` can clear a missing pawn/include/store reference and change missing output
storage to DropOnFloor. Group recovery instead retains the invalid choice as information and asks
for an explicit in-game replacement/fallback, while existing physical output follows safe native
placement. Handle these validation events consistently across projections without broadening one
member's filter. User-facing recovery is contextual gameplay, not an interview question or settings
inheritance exception.
S005's native invalid-destination fallback is explicitly scoped to ordinary unlinked bills. For
linked rows this owner's unresolved-choice policy takes precedence, including a dissolved storage
group formerly used for output or counting. The storage owner still issues its normal notifications.

If startup admission fails, projected native copies must not resume as independent full quotas.
The always-available owned load/recovery path suspends the affected group before unsafe work/new
admission, retains physical native state and reports why. It must not require the rejected patch
group to have installed successfully. Restore support and reconcile first, then require explicit
Resume. Do not auto-unlink or erase saved group data to obtain an apparently compatible game.

### UI and composition

Use selected-station/Bills controls for group actions and explicit scope labels in bill details.
No new configuration row or main bottom tab is needed. New benches are unlinked by default; all
membership/count choices are entity gameplay state. Translate preview/recovery/status text in all
seven baseline languages and verify long names, many members and unsupported rows in real UI.

The integrated browser uses the common recipe catalog and commits exactly one logical row with
its selected material. Per-bench/type/save recipe hiding remains browsing state under S011, not
bill deletion or a linked-bill setting. Existing list edits use the same owned operations. S013
quality reconstruction and later Replace Stuff use an explicit member replacement handoff. Later
BWM/Everybody Gets One/Most Skilled work can extend the shared row's specified choices and policy;
do not preempt their source research or implement internal Harmony adapters between owned features.

There is no original source package or old linking schema to impersonate. Native Bills remain their
correct supported classes and callable lifecycles. Any external provider integration must name its
actual canonical package, fields/callbacks, consumers and unsupported cases when researched. Unknown
third-party mutations are not evidence for a blanket conflict claim or a generic reflection facade.

### Semantic admission and failure isolation

Before implementation create stable per-patch target documents and executable validators for the
chosen native/provider seams. This design lists evidence needs, not approved hook signatures:

- Actual native Add/Delete/Reorder and contextual edit boundaries, bill family/recipe availability,
  correct clone/init identity and all supported choice propagation, including direct field writes.
- Job reservation/start/continuation, physical unfinished/process binding, production/iteration
  credit and native deletion. A query cannot be treated as actual commitment or completion.
- Count/hysteresis and ready-local-result continuation when new admission is disallowed; validate
  consumed result/control flow, not only matching a method name or returned type.
- Native save resolution, group suspension, member movement/destruction/reconstruction and reference
  validation. Missing cancellation/accounting support prevents partially installing a working
  replication path that can multiply production.

Allow compatible foreign patches/unrelated edits. Account for skip-original and effective transpiler
changes where semantics depend on the body; every transpiler validates its actual incoming stream
on each execution and returns it unchanged on failure. Use explicit atomic groups for interdependent
projection/admission/accounting hooks, roll back only newly added owned hooks and log one bounded
startup warning per skipped enabled patch with target/spec/reason/behavior/fallback. Optional absence
is normal; changed adapters isolate their unsupported path without disabling unrelated native bills.
Do not reject by whole-assembly hash/version or by any other patch owner's presence.

Dynamic group/bill gameplay cannot be expressed as static Def edits alone. Prefer XML Extensions
guarded additive operations for any actual declarative UI/Def addition, preserving unrelated entries;
none is selected yet. First Harmony/XML Extensions use updates required metadata in the same slice.
Keep product-independent Gateway evidence and downloaded assemblies out of the package.

## Risks / Trade-offs

- [Synchronization mistaken for multiplying production] → One shared finite quota and once-only
  physical completion; target mode explicitly retains ordinary in-flight overshoot.
- [Whole-list join discards destination choices/work] → Concrete preview, full validation/rollback,
  reject displaced physical work and preserve authoritative source jobs.
- [Special machine work is treated like ordinary bill copying] → Native family-specific state and
  exact completion/recovery contracts, with scope retained and implementation prerequisites recorded.
- [Allocation on Leave differs from copying the same remaining number] → Preview finite counts,
  keep commitments with their work and explicitly select the successor for unstarted repetitions.
- [Common catalog limits a mixed group] → Explain missing support before join/add and allow unlink;
  never silently create a member-only row or substitute a different recipe.
- [Direct provider/UI writes bypass a central operation] → Enumerate supported paths, reconcile
  before work, retain drift visibly and add only concrete narrow adapters after their source turn.
- [Admission failure leaves independent saved copies active] → Protective owned load/recovery path
  before simulation; no automatic dissolution or resume after support returns.

## Migration Plan

No runtime state is changed by this draft. A future first implementation starts existing benches
unlinked. The player explicitly creates groups with reviewed in-game previews; keep prior state
until each operation commits and restore it on failure. No automatic import from BWM or another
provider is promised. Such migration needs a separate backup, atomic conversion, failure restoration
and verified-postcondition contract. Product removal from an already linked save is not claimed safe
by merely disabling patches; document an explicit dissolve/export recovery before offering removal.

## Open Questions

Record these for later research/acceptance; do not interrupt the user:

- **WB-Q1 — supported mutation and UI boundaries:** exact 1.7 field/edit/native rename/style paths,
  common-catalog and stale-dialog handling, supported special/provider choices and atomic restoration.
- **WB-Q2 — production accounting:** precise per-family admission, unfinished continuation, already
  formed output, zero-output completion, cancellation, shared counts/hysteresis and once-only credit
  across initialization/save/load. Preserve physical rules while allowing ready local completion.
- **WB-Q3 — membership lifetimes and composition:** minify/caravan/gravship/map teardown, partial/full
  transfers, surviving unfinished work, supported S013/S028 reconstruction, and later selected bill
  modifiers/external consumers. No guessed provider compatibility or source schema import.
- **WB-Q4 — playability/UI/performance:** evaluate the proposed common catalog, finite allocation and
  invalid-reference recovery in native workflows; seven languages, paused UI, realistic mixed lists
  and measured multi-member/shared-data costs before performance or gameplay acceptance.
