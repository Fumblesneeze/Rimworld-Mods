## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S108 is specification only. [Research](../../../mods/megamod/docs/refinement/2026-11-05-rescuee-joins-plus-research.md)
separates the selected1.6 DLL, older public1.4 code and native1.7.4998 rev1299.
See [proposal](proposal.md) and [observable contract](specs/megamod-rescuee-join-consent/spec.md).

The source has four runtime classes: startup, summary formatting and two transpilers. One replaces
the guest recovery join branch; the other replaces a recruitment call inside the rescue-promise
method. The latter allows subsequent native memories and a success letter to run before the player
answers. A map lookup assumes the pawn is spawned, despite native recovery admitting a carried pawn.
No source settings, XML patches, custom saved state or custom artwork are involved.

## Goals / Non-Goals

**Goals:** Keep consent at both actual joining points; show useful ordinary pawn information;
preserve route-specific effects and native rescue eligibility; make pending decisions durable and
idempotent; support carried/map-held pawns and absent-safe optional information.

**Non-goals:** Change rescue probabilities, grant all guests recruitment, intercept every faction
change, redesign prisoner recruitment, add ritual/wanderer coverage, grant hidden information,
alter rescue jobs or quest rewards, introduce a recruitment framework, or prove source1.7 support.

## Decisions

### Two narrow routes with one owned decision presentation

Recovery route: retain the native eligibility, seeded willingness comparison, Rescued memory and
cleanup. Intercept only the successful automatic SetFaction/letter branch. Native1.7 uses50% in
safe outdoor temperature without toxic fallout or active Biotech noxious haze, otherwise100%.
These are inspected native comparators, not new tunables or a reason to reroll. Remove the source's
god-mode forced-willingness behavior: god mode alone does not alter this feature's ordinary policy.

Promise route: the current native OfferHelp/FreePrisoner float menu schedules JobDriver_OfferHelp,
which moves the actor to touch range and calls Pawn_MindState.JoinColonyBecauseRescuedBy. That method
clears WillJoinColonyIfRescued, checks AnythingPreventsJoiningColonyIfRescued, recruits with audiovisual
effects false, grants rescue memories, and sends a rescue-finished letter. Capture a player-colony
humanlike request without allowing its acceptance-only tail to run early. Preserve independent
physical-rescue/quest facts already produced by the native action. On Accept, perform the admitted
route's continuation once; on Reject, consume the offer without recruitment or its completion letter.
Do not generalize the guest route's prisoner exclusion to this route: freeing a hostile captor's
prisoner can be the actual native rescue promise.

Replacing every DoRecruit or SetFaction call would affect unrelated recruitment and is rejected.
Merely swapping DoRecruit for a delayed callback repeats the source's premature side effects and
is also rejected. Exact legal continuation boundaries and effective caller interactions remain
RJP-Q1; no delegate replay or complete old-method copy is assumed safe.

### Request state belongs to the saved game and identified pawn

Use one small owned request record: game-local ID, pawn reference, route, originating map/holder
context, rescue actor where required, native opportunity identity and resolution state. No static
shared letter text, live callback serialization, all-pawn cache or periodic world scan. Deduplicate
the same opportunity, including competing routes for the same pawn; retain the first admitted route
and consume duplicate entry without granting twice. Record resolved opportunities so redown/recovery
and reload cannot re-offer a rejected recovery join. Preserve native leftAfterRescue semantics; do
not manufacture a new rescue offer when that native opportunity is consumed. A genuinely new
explicit native/provider offer must have separately validated identity rather than a timer reset.

Show a forced-pause contextual dialog with a one-second real-time input delay, matching the inspected
native dialog. More info leaves the request open. Escape/Enter/outside clicks do not decide it.
Queue simultaneous requests; do not discard or conflate them. Persist pending/resolved identity if
the game is saved through a supported path while a decision is outstanding; reconstruct presentation
after load rather than persisting window/delegate objects. No timeout or automatic default decision.
On game exit clear live UI/references; loading another save cannot expose the previous game's request.

At choice time revalidate the exact pawn, route, game and held map, faction/guest eligibility and
rescuer when required. Being held by a carrier on the original map is valid; changing the selected
map is irrelevant. Actual removal/transfer to another context, death, capture, incompatible faction
change or missing required rescuer invalidates the offer, with a readable explanation. An external
join already completed makes the old request stale and does not replay benefits or undo that join.
Missing optional summary data must never invalidate a valid decision.

Mark resolution before callbacks can reenter, and verify actual membership before announcing success.
If preflight fails before the continuation starts and before any effects, retain a blocked request
with explanation and safe Reject; do not silently accept. Membership alone is not an effect boundary:
RecruitUtility unlocks apparel, replaces relevant royal titles and clears guest status before
SetFaction. Once the continuation has begun, any failure must account for these native changes as
well as foreign effects. Mark the attempt resolved as incomplete, remove Accept/Reject callbacks,
reconcile actual membership and report the observed incomplete result. An acknowledgement can close
the message; it must not replay recruitment or masquerade as a clean refusal. Do not blindly retry
or roll back native/foreign effects. Exactly-once observation and retained evidence are required,
not claimed here; admission includes enough observation to distinguish preflight from entered work.

### Preserve native action semantics and provide precise feedback

Recovery acceptance uses its admitted native faction transition, not the broader recruit utility.
Promise acceptance preserves RecruitUtility guest cleanup, apparel unlock/title replacement,
recruiter tales/records, RecruitedMe and quest Recruited signals where the native route produces them,
plus its rescue memories and correctly titled completion letter. Do not synthesize these on Reject.
Refusal itself adds no DeniedJoining/Banished thought or goodwill penalty; existing independently
triggered quest, rescue, ideology and faction rules remain authoritative. Do not promise all quests
are penalty-free. Rejection text says the person will not join; it must not promise immediate exit
when downed, detained, blocked, on a site or otherwise governed by native care/exit behavior.

### Ordinary information with safe omissions

Use native label/age/story/relationship/work/trait/passion information and the ordinary information
card. Report actual visible work restrictions, including admitted life-stage/provider restrictions,
instead of only the source's backstory-and-trait mask. Empty passions display None, not a blank
heading. Optional/missing story, skills or mood show an accurate unavailable/empty distinction.
Use native visibility rules for hidden traits/conditions; no hidden anomaly or covert state is exposed.
Race/kind labeling is data-driven and does not require a case-sensitive HAR package-name test.
Do not add a hard HAR dependency, special alien reflection or a separate portrait cache.

Every owned heading, decision, unavailable reason and outcome has English, German, Spanish, French,
ChineseSimplified, Russian and Japanese coverage. Use grammatical pawn arguments instead of hardcoded
male pronouns/lowercasing arbitrary translated labels. This is a contextual gameplay decision, not
a setting, debug action, bottom tab or bottom-right toggle. No new options/default framework is needed.
Share presentation with S109 only when its separately researched requirements justify direct reuse.

### Admission and compatibility

Planned target groups below are design identifiers, not completed patch-target documents. Before
implementation create executable target specs and link tests/evidence. Admission must include all
required receipt/lifecycle/continuation paths in a route; no half-enabled consent workflow.

| Planned group | Minimum semantic admission |
| --- | --- |
| RJP-recovery-consent | Effective eligibility and seeded comparison lead to one known player-faction join and its letter; independent recovery memory/cleanup remain preserved; incoming IL admits unambiguous replacement |
| RJP-promise-consent | Player rescue actor/target and native eligibility can be captured before recruitment; pending state, flag consumption, acceptance-only tail and rejection behavior can be separated coherently |
| RJP-request-lifecycle | Save/load and native UI callbacks preserve exact identity, deduplication, stale resolution and one-time outcomes; observable foreign reentry/failure handling is admitted |

Do not gate on exact hashes/versions, branch opcode sizes or a patch-free method. Validate minimal
effective semantics, relevant prefixes that skip the original, incoming transpiler streams on each
run and later repatch revalidation. On false/unknown/exception leave that route inactive, preserve
incoming instructions and roll back newly added owned patches only. One bounded startup warning per
skipped enabled patch names owner/patch/spec/target, reason, missing consent behavior and native fallback.
Native fallback can still auto-join; communicate that limitation rather than claiming protection.
Active `ogliss.rescueejoinsplus` owns its overlap; do not install owned duplicates or unpatch it.
Compatible unrelated owners are allowed. HAR and optional quest/race integrations use canonical
package IDs and shape-guarded narrow adapters only where an actual contract needs them.

Core/Harmony are required at implementation. No XML mutation or XML Extensions use is planned;
any future XML addition must follow the foundation's guarded merge-preserving rules and dependency
policy. Never ship original/Harmony/Gateway binaries or depend on another repository product.

## Risks / Trade-offs

- Delayed native quest continuation can misreport success or duplicate rewards → admit a coherent
  branch including its tail; preserve physical rescue facts separately and prove both outcomes live.
- Third-party code can change faction during a callback → resolve by observed identity/membership,
  forbid blind retries, and retain bounded diagnostics instead of destructive rollback.
- A modal request temporarily pauses play → matches the source's decision point; no forced timer,
  added bottom tab or constant alert loop. Multiple requests use isolated records.
- Rejecting a useful colonist changes colony growth → explicit intended player control, with native
  willingness, rescue work, costs, risks, memories and ordinary recruitment retained.
- No source license located → independently implement behavior and credit Ogliss; do not copy assets,
  strings, code or bundled Harmony. Public1.4 source is not the installed1.6 revision.

## Migration Plan

No source custom save/settings schema exists to import. Ordinary native pawn/faction/guest state
continues to load; do not infer retroactive requests for earlier rescues or remove existing colonists.
Ship a versioned owned request schema with load validation when implemented. Disabling a route while
the mod remains loaded does not silently accept an outstanding request or undo a completed join;
unresolved owned decisions retain an unavailable explanation and native care continues. Arbitrary
uninstallation with pending owned save records is not claimed safe or capable of displaying that
explanation. Any deliberate import/removal tooling needs a separate backup, atomicity, restore and
verified-postcondition contract. No migration or package change is performed now.

## Open Questions

- RJP-Q1: Exact effective native continuation/admission, caller and foreign prefix/transpiler cases.
- RJP-Q2: Pending/resolved identity, supported save path, competing route deduplication and partial
  native/foreign callback failure reconciliation.
- RJP-Q3: Optional providers, visible work/trait data, real API consumers and license scope.
- RJP-Q4: Seven-language UI, native recruitment/quest/rescue balance comparisons and reviewed-build
  action-to-result acceptance, including carried recovery and rescue-site acceptance/refusal.
