## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S084 replaces `zylle.ExtraAlerts`, Workshop1926923167, author Zylle/Zylleon. Public source revision
`1937247e488842cb39f7b1ac8666fc55ab3a7560` contains the same 1.7 DLL as the installed distribution.
All compiled alert/settings classes and selected current native paths were inspected read-only.
[Research](../../../mods/megamod/docs/refinement/2026-11-04-extra-alerts-research.md) distinguishes
source facts, corrected behavior and unverified provider/lifecycle work. No runtime work is delivered.

## Goals / Non-Goals

**Goals:** actionable persistent notices for actual current conditions, stable native navigation,
one consistent report per condition, meaningful settings and explicit hidden-information separation.

**Non-goals:** auto-treatment/capture/execution/trading, duplicating ordinary native rescue alerts,
adding every stale translation key as a feature, a new dashboard/tab, a custom alert scheduler or
universal provider reflection layer, another Thing index, or changes to illness/mood/combat balance.

## Decisions

### 1. Use native alert registration and lifecycle

Current `AlertsReadout` discovers leaf `Alert` subclasses and creates them without an XML registry
or Harmony patch. Source follows that route and has no Harmony/Def patch code. Implement the
ordinary catalog through owned alert subclasses and small shared reporting logic, leaving native
activation, initial grace, storyteller suppression, priority, pulse and click behavior intact where
it satisfies the spec. Do not introduce a global patch manager merely to port these classes.

Native polling updates a twenty-fourth of the ordinary alert list per frame after the initial600
game-tick/tutorial gate; hover recalculates and click requests a report again. Thus a cached list
in a source instance is not a reliable independently queried explanation. Build a consistent bounded
report from current eligible subjects; label/count/explanation/navigation must share its exact deduped
set and map context. Revalidate navigation immediately. Retain no cross-save/map stale references and
do not persist alert results. Do not run world mutations from a getter or report. Existing native
critical activation feedback must not be replayed by refresh, collection churn or each draw.

Normal target visibility applies before aggregation, not just drawing: source filters hidden pawns
only in its enemy pair, leaving other lists capable of exposing unseen subjects. Centralize the
applicable known-subject rule across names, counts, messages and navigation. Player-owned known
off-map colonists can be represented through their caravan/transporter, while ordinary fogged enemies
cannot leak through the same report. Validate current native camera/world handling before acceptance.

Native Alert_Critical creates a separate Message with captured LookTargets. Later message/archive
navigation is not protected merely by refreshing Alert.GetReport. Inspect that separate lifecycle;
owned activation feedback must retain safe known-location targets rather than a live hidden-pawn
tracking reference. Preserve the native activation cadence/appearance, using the existing virtual
message-control surface and a narrow owned emission path if needed rather than patching all messages.
Prove save/archive invalidation and no hidden selection in the final native workflow.

### 2. Conditions follow semantic facts, not misleading names

- Enemy presence uses actual hostility, alive/spawned/nondowned and player visibility. Downed
  classification prioritizes entities, then hostile humanlikes, non-hostile faction visitors and
  unaffiliated humans. Source can overlap entity/human/factionless categories; owned output is unique.
  Source `NeedsRescue` also suppresses any ready queued job. Omit that unreliable proxy: a job need
  not mean care, and the observable contract waits for physical carrying/bed/containment or recovery.
- Mood reports use native thought eligibility/nullification and a real negative effect; do not infer
  them from apparel/trait/bed labels alone. Source Alert_Thought already avoids absent mood/nullified
  thoughts, but the custom ascetic loop dereferences mood and uses stage>=5 without those protections.
  Current Ascetic stages5+ are the negative impressive-room outcome; resolve its meaning, not an
  unexplained index. Deduplicate and exclude suspended pawns consistently.
- Animal alerts retain the source's minor-or-worse threshold, native stage2 at severity0.2 for both
  illnesses. Providers changing stage semantics need explicit mapping/admission. Native
  `Alert_HypothermicAnimals` concerns factionless wild animals in developer mode; it is not duplicate
  ordinary colony-animal coverage. Quills retain the Odyssey gate and actual hediff presence.
- Exposed-electrical uses native shortCircuitInRain candidates and roof exposure rules. Despite its
  name, current `allBuildingsColonistElecFire` is populated outside ListerBuildings.Add's player-faction
  branch, so apply actual ownership explicitly. Ordinary exposure uses Position, not an invented
  whole-footprint rule. Provider-specific exposure needs its actual contract.
- Unarmed means eligible free colonists with no primary weapon, violence enabled, manipulation and
  not downed; optional juveniles obey the same checks. Do not turn it into a ranged-hunter requirement.

### 3. Trade opportunities use the real provider contract

Source visiting traders use `Pawn_TraderTracker.CanTradeNow`. Current native evaluation includes
actual willingness, casual interaction, alive/spawned/nondowned/nonprisoner, nonhostile nonplayer
faction and tradeable goods or the relevant favor currency. Preserve that behavior without caching
per-pawn negotiator eligibility. It is an opportunity notice, not guaranteed reachability.

Source orbital detection accepts any passing ship and exact defName CommsConsole, ignoring whether
the ship trades or another terminal implements the same function. Narrow it to actual TradeShip/
supported equivalent contracts and owned installed communicators on the same map. Native
Building_CommsConsole's CanUseCommsNow checks power/electricity conditions; GetCommTargets and the
actual float-menu/job path determine real contact. Recognized native-derived terminals can use that
contract; unrelated custom terminals need a package/shape-guarded adapter. A text label or defName
substring does not prove support. Public reports of missed compact consoles identify research work,
not verified compatibility with that provider.

Keep ship presence during temporary outage, explicitly labeled unavailable for contact. This
preserves the source's reminder value without claiming readiness. Native player negotiation still
checks path, talking, electricity and actual target. Provide map/ship context and valid terminal
navigation instead of the source's untargeted boolean, but never issue a contact job from an alert.

### 4. Native and shared facts, with no alert-specific persistent index

Use PawnsFinder/native map plant, building, passing-ship and equipment/health/relationship facts.
Batch shared candidate enumeration across enabled reports where native update costs justify it;
reuse one bounded snapshot for display consistency, not independently rescanning every map per label
and tooltip. Integrate any genuinely shared derived Thing fields through the existing map-owned
Ctrl+F/hauling owner with explicit lifecycle/invalidation/native fallback. Do not build a second
long-lived index merely because several source alerts call ToList.

Per-pawn mood, visibility, faction, reservations/action access and medical eligibility are current
decision checks, not durable cached permissions. Clear transient reports on map/game teardown and
save switch. No Verse/Unity reads off-thread. Measure candidate enumeration and update/memory cost
before an optimization claim; no benchmark or performance acceptance has happened.

S069 blight cutting changes actual plant state: designation alone must not clear the warning.
S039/S040 trader gathering and S027 deliveries remain direct owned behaviors; alerts do not move
traders. S062 medicine/other medical work and bonded hunting companions remain the only action
owners. Observe their final native state without a self-Harmony patch stack.

### 5. Settings and debug state have distinct ownership

The source has21 serialized booleans: twenty alert kinds plus child inclusion. Hidden-enemy field
initialization is true but Scribe default false. Entity boolean is never read by its report and is
absent from the UI; that report reads cb_enemyRescue. Source greyed child/hidden rows still invoke
CheckboxLabeled, so grey color is not a mutation guard.

Owned common settings have19 ordinary alert enable rows, all On, plus child inclusion Off. Add the
missing independent entity row, unavailable DLC explanation and true dependent-child control guard.
Use the shared load/store/effective-value/rendering path with global/save/Use global, dormant
overrides and protected main-menu/reset callbacks. A config change does not mutate the colony.
Native alert preferences are configuration, not gameplay entity state or a new bottom-right toggle.

Hidden-enemy presence is an information override under the user's UI decision tree. Put a read-only
list behind an explicit top debug-toolbar command, initially closed and requiring developer mode.
No persistent checkbox, automatic alert, critical notification or implicit reveal when developer mode
is enabled. Keep transient debug view data separate from ordinary report snapshots; clear on mode
off/save switch/main menu, enforce callback guards and never supply hidden camera targets. The normal
source setting's parent dependency is intentionally removed with this UI relocation.

Source has English/French/Japanese/Russian/ChineseSimplified/PortugueseBrazilian keyed files but no
complete baseline German/Spanish coverage. Re-author coherent seven-language messages/placeholders,
no stale unused label inventory and no source English-only error text. Inspect narrow real surfaces.

### 6. Admission, external consumers and provider ownership

No actual incoming runtime consumer was found in bounded installed XML/C#/JSON and ASCII DLL searches.
ASCII search is bounded evidence; it does not exhaust UTF-16 package/reflection strings in .NET #US.
Broaden metadata/user-string inspection before any actual facade/compatibility claim.
The extra package matches are RimPy database identities (including a translation package), not call
contracts. Public source types/helper methods/DefOf/settings keys are inventory, not a reason for
facades or package spoofing. No original settings/save migration is promised. Original package active
means its overlapping alert reports/hidden feature own the behavior; suppress ours with one clear
provider explanation, never edit original booleans, unpatch it or globally clear native alerts.

Use lazy optional content resolution and isolate unsupported report kinds. Anomaly data is absent
in this inspection environment; entity classification and containment need a genuine DLC-present
workflow before claiming support. Unknown stage/visibility/communicator semantics do not authorize
guessing; known benign provider changes must remain compatible.

If any actual Harmony/XML bridge becomes necessary, first create/index its stable target document
and executable validator linked to observable scenarios and tests. Validate only consumed semantic
prerequisites. An observation may need existence/callable shape; argument-changing prefixes require
the actual operation/data-flow/control context. Account for effective code and skipped originals;
incoming transpilers validate every invocation and return unchanged input on failure without probing
foreign transpilers. No whole-method hashes/exact-version/unpatched-owner admission. Revalidate or
invalidate after supported later repatches. False/unknown/exception/install failure leaves only the
affected report/group inactive; roll back newly installed owned patches only. Emit a bounded startup
warning per skipped enabled patch naming package/patch/spec/target, predicate, lost behavior and
fallback. Optional absence and disabled preferences are not failures.

Prefer additive/duplicate-free XML if declarative changes are genuinely needed, guarded with XML
Extensions for actual content/nodes and package-ID FindMod for external content. Require XML Extensions
with first use, inspect actual operations first and never replace entire categories/lists to add a
report. Do not add unused dependencies; this design currently needs neither Harmony nor XML patches.

## Risks / Trade-offs

- Constant alerts can cause fatigue. Preserve independent preferences, precise current conditions
  and native suppression; no invented grace delay or hidden automatic dismissal.
- Informational visibility affects tactical knowledge. Normal reports exclude hidden subjects and
  the source hidden feature is explicitly relocated under developer control.
- Third-party thought/illness/comms semantics can change. Use supported mappings and local failure,
  not false universal compatibility or silent incorrect alerts.
- Source booleans and typoed key cb_porqupineQuills are not an owned persistence contract. Fresh owned
  defaults are explicit; migrations need separate evidence and failure-restorable design.
- Source byte identity and 1.7 declaration are not runtime acceptance. Polling, repeated activation,
  navigation, saved travel, multi-map behavior and translated layout still need real native proof.

## Migration Plan

Implement one observable alert slice at a time with focused honest-tier RED/GREEN, then review and
fresh isolated native UI workflows. Reports/debug state are transient and common settings persist
normally. Do not import original config or save objects; any future migration requires its own
backup/atomicity/restoration/postcondition contract. No game launch or installation in this phase.

## Open Questions

XAL-Q1–5 retain provider semantics, lifecycle/performance, trade compatibility, UX/balance and complete
implementation/native evidence. Record them in refinement rather than asking the user to explain
existing-mod behavior or interrupting the sequential pass.
