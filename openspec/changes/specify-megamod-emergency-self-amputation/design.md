## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target1.7 beta.
S056/P1-50 replaces independently researched Cut It Off behavior. Read the [proposal](proposal.md),
[observable contract](specs/megamod-emergency-self-amputation/spec.md) and
[exact-input research](../../../mods/megamod/docs/refinement/2026-11-04-cut-it-off-research.md).
Selected source1.7 DLL and current native1.7.4998 are identified there. No implementation exists.

## Goals / Non-Goals

**Goals:** one explicit desperate medical action, trustworthy anatomy/medicine/outcome, source-derived
costs, coherent self-tend/cargo composition, minimal compatibility predicates and safe saved work.

**Non-Goals:** general self-surgery, elective harvesting, extra loot, automatic retries, medical AI
priority redesign, rescue automation, source assembly/Def impersonation or another product dependency.
No speculative health framework or per-mod medicine cache. No code/assets copied without a license.

## Decisions

### 1. One contextual action and clinical predicates

Use a narrow Pawn.GetGizmos observation hook to add an owned command, preserving all other gizmos.
Check actor existence/map/lifecycle before querying infections. Keep eligibility/part/medicine-choice
rules in one cohesive feature module called by presentation and job validation. A read-only preview
must not reserve, transfer, spend or mutate. No duplicated policies in UI/driver patches.

The user selected actual available medical help, not physical solitude. Resolve a same-map
player-controlled humanlike helper through ordinary caring/Doctor assignment, capacity, applicable
skill, patient/operation eligibility, reachability and required accessible supplies. Count sleeping
helpers only where ordinary work can wake them. Do not exclude a low-skill pawn automatically if
the actual required care has no higher skill prerequisite. Do not count incapable children, locked
routes or disabled Doctor work as available care. Evaluate without scheduling a job or claiming
supplies; recheck before irreversible completion. S046/S042 remain current owned policies.

Resolve actual BodyPartRecord identity/ancestry, present natural tissue, exact native WoundInfection
and anatomy tags/known eye-ear-nose roles. No substring whitelist for arbitrary anatomy. Determine
intrinsic fatality through native anatomy/health contracts, not a hard-coded human-only name list
or trial damage. Unknown vital/removal semantics exclude only that candidate. Do not exclude a
supported race merely because it came from a mod. Current injuries/bleeding can still kill despite
nonessential anatomy; confirmation explains this.

### 2. Medicine selection is a plan, then one real transfer

Use native medicine access, reservations and health-care category semantics. Inventory first;
otherwise reachable map candidates with nearest-path preference, potency tie and stable identity.
Shared facts/native listers may narrow candidates; final permission/reservation/reachability and
capacity remain action-specific. No per-pawn scan cache or off-thread Verse calls. Invalid potency
or unknown ownership does not establish a usable medicine unit.

The confirmed plan names part, medicine or explicit none, cost and chance. Resolve an exact unit
claim; do not reserve a whole stack. Ground retrieval uses native carry/transfer and physical
capacity. Evaluate the actual unit, not another inventory search. S021 owns shared cargo/kept
quantity attribution and S024 owns interruption recovery; call them directly. No private oversized
inventory, borrowing from other pawns or transport cargo is introduced.

If terms drift before work, cancel or rebuild the confirmation. At work start persist fixed chance
and agreed medicine choice. Loss of the unit or permission cancels before resolution. No Care
rejects the action; no-medicine policy/absence allows the explicit unmedicated skill curve.
The former0.2 potency proposal is rejected, not a source fact to preserve.

### 3. Persist the small job lifecycle and irreversible boundary

Use an owned JobDef/driver with persisted exact part, medicine/no-medicine choice, progress, fixed
chance and resolution state. Use native interruption/claims rather than a background scheduler.
Confirmation precedes stopping work/clearing queued jobs; disclose both effects. On load resolve
the same part against this pawn's anatomy and the same medicine/holder with shared claim restoration.
Missing/changed state cancels safely; don't replace it with another part or unit.

Sequence: preview → confirmation → revalidation/claims → collection → work → final revalidation →
commit one cost/outcome → report → cleanup. Chance uses the EA05 curve: linear50–90% unmedicated,
and medicine takes max(base +0.10*potency,0.65*potency*(1+0.05*skill)), then clamps0..1. Work4800 simulation ticks equals1.92 game hours; no SurgerySpeed bonus.
Cancellation before commit preserves medicine and health. Saved active work resumes; reordered work
starts fresh. No automatic retry when interrupted.

At commit, claim and remove one physical medicine unit before irreversible health mutation, record
resolution state, and resolve one random outcome on the game thread. Preflight all references and
outcome definitions before that point. Once mutation begins, exceptions must not repeat damage,
restore a limb, refund consumed cost or manufacture success. Preserve actual result, issue one useful
diagnostic and finish/cancel recovery. Save reconstruction must not execute completed closures twice.
This is saved-job consistency, not prevention of deliberately loading an older save.

### 4. Native removal physiology without a second surgery roll

Native Recipe_RemoveBodyPart.DamagePart establishes SurgicalCut/explicit-part removal. Full
ApplyOnPawn also runs CheckSurgeryFail, harvest products/thoughts and reporting; do not call that full
recipe for an already-resolved emergency outcome. Select the smallest semantic native removal seam,
validate effective part propagation/lethal/bleeding consequences, and verify missing part before
living success. Preserve graphics/health notifications. Do not bypass an incompatible provider by
manufacturing a missing-part hediff and calling that acceptance.

Failure retains ordinary Cut with integer Rand.Range(15,30), unspecified hit part and native
mitigation. It can incidentally sever tissue or kill; report failure, not a second success branch.
Reject source success Cut(maxHealth*1.01,penetration0), which does not establish removal. A passed
roll with unconfirmed native outcome reports unknown, no success memory/tale and no repeat attempt.

Own declarative Thought/Tale Defs: success+30/3days, failure-18/0.5day, stack1, opposite removed,
matching duration refreshed. Do not copy source text/art. Moodless pawns skip memory creation;
survival tale requires actual removal while alive. No XP, aftercare, loot or unrelated infection cure.
Keep source-reference coefficients and other constants named. The three EA10 values use two common setting rows: an atomic endpoint pair and a positive medicine-benefit value. Both use
common load/store/rendering with Use global, Current save, Global default and main-menu guards. Actor policy and selected part are contextual state, not settings.

### 5. Minimal grouped compatibility admission

Create stable patch-target documents and executable checks before implementation. These planned
IDs are design candidates, not installed patches or finished target specifications.

| Candidate / group | Minimum consumed semantics | Failure behavior |
| --- | --- | --- |
| EA-GIZMOS: Pawn.GetGizmos observer | Callable pawn instance and enumerable gizmo return; incoming commands preserved; no original-body identity needed merely to append | No owned command; native commands preserved |
| EA-ACTION: owned command/job plus native health/medicine | Actor/part/care meaning, one-unit reserve/transfer/spend, saved-job cleanup, exact selected-part removal, owned outcome Def shapes | Command and resumable work disabled together; unfinished job safely cancelled |
| Any later argument-changing/invalidation hook | Required argument use/control flow/operands; validate actual incoming transformed instructions when applicable | Unchanged stream/owned rollback; never a name-only semantic patch |

EA-GIZMOS and EA-ACTION are one admission group, preventing a partially functional command.
Native methods merely called also need their consumed effective mutation contract established.
Do not invent broad health patches. Account for relevant skip-original prefixes/transpiler changes
without rejecting all Harmony owners. Do not repeatedly invoke foreign transpilers as probes.
Benign IL edits/offsets/local numbering and compatible observers remain admissible. Hashes identify
research, not runtime compatibility gates.

Every patch needs its target document, validator, tests and bounded startup warning with package/
patch/spec IDs, target, failed predicate, unavailable behavior and fallback. False/unknown/exception/
install failure disables the atomic group; rollback only newly installed owned patches. Revalidate
supported late repatching and current conditions before mutation. No per-tick warning. Deliberate
disable is not a failed enabled patch; absent optional content stays quiet. Unknown anatomy for
one candidate does not disable normal surgery.

Active `LegendaryMinuteman.CutItOff` suppresses our overlap; leave its patches/commands untouched.
The bounded installed scan found no external executable consumer. Potential helpers/job names do
not justify an unsupported facade or source package spoof.

### 6. Dependencies, packaging and proof

Harmony becomes required at first integration despite source metadata omission. Owned Job/Thought/
Tale XML is declarative. No foreign PatchOperation planned; later XML patches need XML Extensions,
exact package/content/node guards, additive merge preservation and required `imranfish.xmlextensions`.
No required DLC, source mod or other repository product. Never package upstream DLLs/icons or the
Gateway. Translate contextual text in all seven baseline languages; future art follows asset gates.

Unit covers deterministic eligibility/ranking/chance/outcome rules without game globals. Harmony
tests acquire only the owned hook with guaranteed cleanup. Defs tests use real constructed anatomy
only if all dependencies fit that honest environment. Loaded Core medicine/anatomy/XML, health,
saves and jobs require isolated1.7 processes. No fake active mod or direct-state-mutation acceptance.
Focused RED/GREEN precedes each implementation slice.

Final acceptance follows actual contextual click/confirmation, collection and elapsed work, then
personally inspected Health/missing part, medicine count, messages/mood and continuing physiology.
Retain before/action/after screenshots and reviewed build/PID/mod list. Cover success, failure,
cancel, reload, arriving colonist, source-present fallback, herbal/industrial/glitterworld/no medicine
and supported anatomy. Seeded setup can arrange preconditions/randomness, never the outcome.

## Risks / Trade-offs

- Source population/physical checks overblock rescue options → actual available helper eligibility,
  current access/supplies and precommit recheck; do not fabricate helper work.
- Irreversible health effects → exact part, disclosed queue clearing, revalidation, one resolution,
  no unsafe compensating health mutation.
- Modified anatomy/removal → semantic admission and real optional runs, no generic support claim.
- Strong mood/glitterworld chance → source-derived values, clamp, no healthy farming/stacking;
  finalized live balance remains pending. The more forgiving curve is a selected draft requiring playtesting.
- No explicit source license → independent engineering, no copied code/art.
- Later medical features unresearched → retain S124/S134 questions; preserve current claims/policy
  without inventing conflicts or internally patching our own features.

## Migration Plan

Documentation only now. Future rollout uses focused tests, owned identifiers and guarded startup.
No automatic source-job/memory/tale import or foreign-save editing. Later explicit migration needs
backup, atomicity, failure restoration and verified postconditions. Disabled/unsupported owned work
cancels safely; prior actual amputations/earned memories do not reverse. Removing the whole package
from saves with owned data is not promised safe.

## Open Questions

EA-Q1–4 in [research](../../../mods/megamod/docs/refinement/2026-11-04-cut-it-off-research.md)
retain solitude/anatomy, effective native contracts, finalized balance/medical composition and final
implementation/acceptance. No user question is blocking sequential specification.

## Probability calibration rationale

The old no-medicine proposal gave13/19.5/26% at Medical0/10/20. Inspected source herbal gives
39/58.5/78%, industrial65/97.5/100%, glitterworld100/100/100% after clamping. The selected no-medicine
50/70/90% interpolation substantially increases survival opportunity while keeping some risk at20.
Taking the greater of the preserved medicine formula and a positive bonus above the unmedicated
curve prevents a perverse worse-with-medicine result without nerfing the source medicine outcomes.
These are draft calibration choices grounded in those comparators and the user's direction, not
user-selected exact percentages or finalized live balance acceptance.


Curve endpoints share one Use-global selection and are validated/restored as a pair, preventing a global edit from combining with a local endpoint into a decreasing curve. The medicine-benefit range is1–100 percentage points so a positive finite potency improves chance below the cap.
