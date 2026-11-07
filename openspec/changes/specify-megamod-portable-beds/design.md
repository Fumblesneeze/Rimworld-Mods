## Context

Sole owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, target **1.7 beta**.
This document designs future implementation; no runtime acceptance is claimed. See the
[proposal](proposal.md), three observable capabilities and
[source/native research](../../../mods/megamod/docs/refinement/2026-11-04-use-bedrolls-research.md).
The selected 1.7 source binary is authoritative over dormant root code and commented XML.

## Goals / Non-Goals

Goals: one physical portable-bed transaction, native rest and ownership eligibility, recoverable
temporary state, shared capacity/personal-supply composition, multiple remembered home owners and
ordinary native bedroll production. Preserve source intent while specifying its unsafe edge cases.

Non-goals: a replacement rest AI, world-caravan sleep engine, extra inventory, source assembly/API
identity, automatic migration, private bed search framework, new beds/art or universal compatibility.
S115 is now researched and composed in [bed ownership](../specify-megamod-bed-ownership/design.md).
Do not solve still-queued S135 by guessing its source behavior.

## Decisions

### Direct owned state and native work

Use one small owned portable-bed controller with map-scoped pending deployment/recovery records,
referencing pawn, exact inner bed identity, current wrapper/holder, intended cell/rotation, transport
custodian, shared rest participants, current phase and cancellation reason. Keep only the state
needed to preserve actual outstanding work. Store persistent identities/intent with the save, not
cached reachability or derived candidate lists. One bed cannot enter two active deployment intents.
The transport custodian is distinct from native bed owners; do not copy the source's pawn→bed plus
bed→firstpawn assumption. No internal Harmony patches between owned features.

Keep job selection read-only. Reserve and validate only after accepting work. Donor handoff is an
explicit transaction at physical contact, with permission, retained quantity, manifest and capacity
rechecks; record success before continuing. Use ordinary holder transfer APIs with their failure
restoration, verifying the actual resulting holder. ThingOwner max stack capacity is not a mass
limit. S021/S126 shared mass accounting includes hands/equipment/personal stock, unlike blindly
trusting an inventory container or allocating a second allowance. Never SplitOff then ignore TryAdd.

Use native rest eligibility/result as an input, including lord/mental/rope restrictions on bed use.
A nonnull ground LayDown job alone does not authorize a bed. Recheck medical and S062 field-care
intent before an optional distance override. Use current timetable/need logic instead of cloning
GetRest thresholds into another scheduler. Rest episode state spans temporary disturbances and is
ended by actual completed rest or superseding intent, not simply an instantaneous priority flip.

Search native listers first; consume the existing shared map-derived item index where required.
Candidate roles include inner bed Def/body-size/slots, wrapper/holder, map/level and commitment.
Do not cache pawn-specific usability. Invalidate spawn/despawn/destruction, holder transfer, minify/
install, commitment and ownership/medical changes. Query current zone/footprint/hazards/permissions
at commit. Map teardown/load clears/rebuilds derived views; no Verse access off-thread. Benchmark
combined consumers and update/memory cost before any performance claim.

The selected source sets an unassigned installed bed's ForOwnerType to Slave before claiming it
for a colony slave. Native DeSpawn resets that type to Colonist and CanUseBedNow requires a match.
Represent this intended ordinary-bed owner type during candidate validation, then set it only after
successful installation and before claim, rechecking all intended sharers and player edits. Do not
convert an existing claimed, medical or prisoner bed to make it available. Preserve this permitted
transition explicitly rather than either rejecting every packed slave bed or skipping all owner checks.

The two source footprint orientations, indoor4/12 then outdoor4/12 and pathcost<500 remain explicit
initial policy. Reject a path unless found. Actual sleeper reach and final install geometry are
separate checks. The native Blueprint.TryReplaceWithSolidThing calls WipeExistingThings and removes
plans after unwrapping. Therefore CanPlaceBlueprintAt alone is insufficient: prove the complete
final footprint cannot destroy unrelated content or erase plans before committing the wrapper.
Do not reuse S028 install-over to clear a camping space. No global BlocksConstruction pawn override;
prefer ordinary clear placements. A needed narrow exception must first have its own observable
contract and semantic admission. S060 CT08 excludes installing already-built minified furniture;
retain that scope here. These footprint checks do not imply frame-completion enclosure protection.

Native Blueprint_Install returns the original inner Thing and destroys its emptied wrapper. The
source delivery sequence materializes it immediately without a second fabrication timer. Preserve
that deployment behavior and native identity/records/sounds once; do not add a guessed150-work
construction toil because Blueprint_Install.WorkTotal exposes150 in inspection. On failure retain
the actual bed and release only owned temporary intent, not unrelated designations/plans.

Reuse the ordinary uninstall job/toils/completion through a narrow owned continuation. The source
subclass shadows the native persisted work fields and duplicates removal logic, losing inherited
save progress and native completion effects. Avoid a copied work loop. Preserve reservations,
CannotTouch checks, faction notification, designation cleanup, ThingsUninstalled increment and real
minification. Connect S061 directly before support disappears; forced commands retain its existing
mountain-risk exception and all other preparation. A packed item is recovered by inner identity,
not definition/position. Complete the recovery record only after verified transfer to the custodian.

On unavailable capacity/occupied bed/blocked access keep physical state and a reason; retry only on
relevant state changes or explicit retry. Use existing scheduler opportunities with a remembered
failure condition, not a new per-tick all-map search or arbitrary sleep timer. A player reassignment
or canceled temporary installation must supersede that owned intent. Do not clear successful work
or repeatedly grant work/XP while waiting at a commit boundary.

Pending portable work exposes ordinary Retry on its custodian and surviving bed/blueprint; this
invalidates only the remembered failed decision and rechecks permission. Keep it distinct from the
drafted forced-pickup command and S061's mountain-risk exception. Home-memory Retry remains separate.

### Home memory is separate from ordinary bed ownership

Use game-scoped saved pending pawn→(home map, ownership allowance, physical bed, original co-owner
identity) records, with a derived bed→set index, not a single reverse owner. This is travel intent,
not a reservation. S115's common policy manages native owner lists across allowances. Retain claims
by default; release-on-departure false is one shared setting. If enabled, remember each eligible
left-behind home claim separately; do not create a pending record for retained/moving claims.
Restore references after the admitted ownership load reconciliation before
any attempted reclaim. An owned transition token distinguishes temporary camp assignment/unclaim
from explicit player edits. Audit native bed UI, map-exit, death/despawn and external assignment
entry points; do not infer all ClaimBedIfNonMedical calls are deliberate user changes. Deliberate
edits supersede memory only in the affected allowance or explicitly selected memory. A temporary
bed in an already claimed allowance is an eligible rest/custody target without another normal
ownership claim; disable that path if the native rest/claim distinction cannot be safely admitted.

ClaimBedIfNonMedical checks medical status but otherwise unclaims the current bed and evicts the
last owner when full. Preflight real RestUtility ownership/bed eligibility, plus current physical
occupancy and remembered co-owners, before calling it. For occupied-slot mode snapshot the displayed
owner order and choose its last eligible owner, excluding other remembered returners. Native list
order is by identity, not assignment chronology. Remove that claim only as part of
successful ordinary reassignment and preserve remaining owners. The option must not bypass size,
medical/prison/slave/ideology/faction/access checks as the source aggressive branch can. Roll back
only this attempted claim if a later owned step fails; never overwrite concurrent manual changes.

Pending memory on the wrong home map stays pending. A temporary packed-bed recovery must not
reclaim a remote remembered bed, as the source completion can do. Explicit cancel edits memory
only; never call UnclaimBed against an unrelated currently owned bed. Permanent removal/destruction
invalidates the exact destination rather than adopting the next bed at the same coordinates.

### Supply and caravan composition

S062 remains the sole owner of targets, counts, scarcity, manual Drop and retention. Temporary
deployment adds one commitment for the actual bed: real carried count can become0 while that
commitment covers one requested unit and pauses its replacement errand. Exclude it from free colony
supply and other jobs. Remove the commitment on loss/release/reassignment and let S062 recompute
the actual shortfall. Borrowing must not consume a donor's protected target. Do not invent a target
for a recipient merely because it borrowed a bed; custody is temporary task intent.

Native CaravanEnterMapUtility.Enter can subsequently drop all inventory, flag individual unloading
or transfer shuttle cargo. Hooking a five-argument prefix alone does not prove the resulting holder.
Select a committed entry phase where transfers respect those choices, preserve manifests and use
per-recipient compatibility/capacity. Do not reorder/defeat native unloading, and do not exempt an
unrequested bed as personal stock. Run at most once for a real arrival; SpawnSetup on load is not
an arrival. No new world sleep or remote stocking behavior. S135's later loading research must
consume the same item commitments rather than patching this feature internally.

### Declarative tailoring and compatible content

Implement production through XML Extensions with content/shape guards. Required dependency begins
with first XML operation; Harmony is required when its types are used. Guard Bedroll/BedrollDouble,
both bench Defs, Crafting, GeneralLaborSpeed, unfinished product and recipe support. Use the native
recipeMaker pipeline: Make_Bedroll / Make_BedrollDouble identities where safely generated; native
material categories, cost count, RecipeDefGenerator, GenRecipe quality/style and minification.
Do not add another product or duplicate an equivalent existing recipe. A foreign conflicting
recipeMaker/generated name disables the owned conflicting product addition, preserving its fields.

Per product, commit recipeMaker and missing WorkToMake together or neither. Proposed WorkToMake
600/1100 is the product's base crafting work, aligned with current construction anchors. Keep the
recipeMaker.workAmount default of-1 so RecipeDef.WorkAmountForStuff resolves that stat with the
actual Stuff; do not turn it into a fixed recipe work override. Native effects/modifiers still apply.
Generated Stuff recipes use IngredientValueGetter_Volume:40/85 are base material-volume requirements,
not universal physical stack counts. Preserve actual VolumePerUnit conversion and applicable bill
rules, including admitted small-volume materials. Do not overwrite unrelated statBases, recipeUsers,
categories or research.
If an equivalent admitted recipe already exists, merge only missing compatible bench memberships.
Test XML phase/finalized recipe generation and repeated initialization; admission cannot depend on
an exact source hash. External content requires FindMod(packageId=true) plus actual content checks.
Existing XML Extensions guards/conditional list operations must be inspected for exact supported
syntax when implemented, not guessed from this design.

### Admission groups and actual effective behavior

The following are candidate boundaries, not approved runtime target specs. Before each patch,
create its stable-ID target document, predicate/tests, owner and capability links. One atomic
deployment group must include safe acquisition/materialization/ownership/recovery/saved lifecycle;
home memory and each tailoring product are independently admitted only when their dependencies hold.

| Candidate boundary | Minimal semantics to prove / fallback |
| --- | --- |
| GetRest result / bed eligibility | Original/effective decision preserves result provenance and bed-permission vetoes; skip-original prefixes must not fabricate authority. Failure disables new deployment, retaining original rest. |
| Accepted job, holder transfer and install | Exact selected inner item remains owned; final footprint validation precedes destructive unwrap/spawn; actual success/cleanup observable. Failure stops deployment before mutation. |
| Ordinary uninstall and continuation | Persisted progress, native removal completion, original identity, occupancy and roof preflight precede loss; failed transfer preserves holder. Missing dependency stops owned recovery visibly, leaving native manual handling. |
| ExitMap / SpawnSetup / bed ownership | Observing callable exists may suffice only for pure observation; committed exit/arrival and actual claim changes must be proved where behavior depends on them. Load and canceled exits cannot act as successful travel. |
| Caravan committed entry | Snapshot/transfer/entry continuation respects real recipients, manifests, shuttle/drop mode and current map; uncertainty disables redistribution, not native entry. |
| Contextual gizmos and narrow message suppression | Actual receiver/state and owned intentional transition; never wrap all bed-loss messages by map faction. Optional cosmetic suppression failure leaves ordinary messages. |
| Think-tree recovery insertion / job Defs | Correct Humanlike branch/work opportunity, owned bounded giver, exact JobDef references; no duplicate insertion or source class reuse. |
| Per-product tailoring XML | Compatible real product/bench/recipe shape and generated identity, idempotent merged lists and all-or-none contribution. Native construction survives failure. |

No unguarded PatchAll or rejection because another Harmony owner exists. Use semantic operand/data
flow/cardinality checks on required snippets when changing decisions/arguments. Validate every
transpiler's actual incoming stream on every invocation, returning it unchanged on failure. Account
for effective prefix/transpiler ordering and later repatch invalidation; do not run foreign
transpilers speculatively. Unknown effective semantics disable the affected group and stop new
owned transactions. Roll back only newly installed owned patches, never other owners. Log a bounded
startup warning per skipped enabled patch with package/patch/spec/target, reason, behavior and
fallback; explicitly disabled features are not failed candidates. Saved physical work remains
readable/recoverable even when automation cannot resume. Recovery requires its own admitted path.

### Provider authority and consumed contracts

Case-normalized package detection: Memegoddess.UseBedrolls, Uuugggg.UseBedrolls and
TheLordDrake.UseBedrolls designate the known source lineage. Old identities are catalog evidence,
not analyzed binary compatibility. Active source gets authority over new overlapping automation and
recipes; XML authority must be established before contributions, not undone after Def load.
Do not alter source options, unpatch it or claim its save component/job schemas. Owned state already
loaded under co-installation is suspended with explicit recovery; handing it to an unknown provider
is not migration. No wholesale package alias for SmartMedicine's presence check: S062 exposes its
own admitted native bed catalogue directly. The external original UI can still hide that option.
Unknown consumers remain a reported limitation, not grounds for speculative facades.

## Risks / Trade-offs

- Safer spare/capacity/footprint rules can decline source deployments that previously succeeded by
  overriding intent or clearing things. Retain native rest and explain actionable blocking reasons.
- Exact rest-episode completion and manual versus automatic ownership need native lifecycle proof.
  Do not accept a no-error log or prepared bed screenshot as that proof.
- The600/1100 proposal increases source tailoring work by about71%/83%; it matches current building
  work but uses a different skill/speed channel. Measure final ingredients/work/value with cheap,
  common and expensive eligible materials before balance acceptance.
- Protected personal beds reduce available lending; a borrowed unrequested bed can unload normally
  on return. Do not invent permanent ownership or unbounded retention to hide that trade-off.
- Source-save portability and absent optional level/bed contracts remain unsupported until explicitly
  investigated. No migration, network determinism, safe removal or full-provider guarantee is implied.

## Migration Plan

This draft changes documentation only. Future implementation adds namespaced state/Def/job identities
and seven-language UI, with missing settings inheriting globals. Keep source saves backed up and source
jobs completed/canceled before removing a provider. Any later importer needs its own approved contract,
backup, atomicity, failure restoration and verified postconditions. Feature disabling stops new work
and retains physical recovery/status; removing the mod entirely is not the same operation.

## Open Questions

PBR-Q1–5 in the refinement ledger retain exact effective target/XML phase proof, rest/claim lifecycle
and custom provider support, proposed production balance, external contracts/migration, and reviewed
implementation/settings/languages/native acceptance. None is an invitation to guess existing behavior
or stop the sequential research for another user interview.
