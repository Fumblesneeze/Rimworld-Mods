## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
S103 replaces Need Bar Overflow. [Research](../../../mods/megamod/docs/refinement/2026-11-05-need-bar-overflow-research.md)
records source revision, selected binary/hash/MVID, native1.7 paths, settings, XML and optional inputs.
No runtime implementation, package change or acceptance is included in this change.

## Goals / Non-Goals

**Goals:** retain bounded legitimate reserves; preserve ordinary full and action eligibility;
one coherent policy for simulation, eligibility, consequences and owned UI/forecasts; explicit
settings and safe failure boundaries; no duplicated effects or new per-port pawn/Thing index.

**Non-goals:** make every installed unknown need safe automatically; refill resources for free;
change native schedules or drug side effects; add hemogen/psyfocus; replace existing need tabs;
promise all Steam compatibility claims; migrate source settings/types/conditions; copy source code,
art, public assembly identity or generic global UI/AI patches.

## Decisions

### Keep ordinary maximum and actual amount separate

Native `Need.MaxLevel` remains ordinary full; use an owned permitted upper bound for admitted
positive gains. Do not enlarge MaxLevel globally: it would change hunger targets, percentages and
external consumers. Amounts remain in native units; food uses current MaxNutrition, mech energy uses
race maxMechEnergy. Need.ExposeData already saves raw curLevelInt, so a second reserve balance is
unnecessary and would risk double counting. Amount-changing callbacks must continue to run.

At an admitted gain boundary, compute the finite candidate from the actual delta. If existing amount
exceeds the newly effective bound, accept no further positive delta but do not clip the amount.
Downward deltas and intentional native resets retain their semantics. The base setter alone cannot
distinguish every explicit assignment from an additive gain; inventory those writers and do not
claim blanket preservation through an indiscriminate setter patch. Initialization is not a gain.
Snapshot current ordinary maximum/eligibility per operation; native MaxNutrition caching remains
authoritative. Invalidate owned projections when maximum changes without converting stored amounts.

Unlimited is a configuration union value, not float infinity in state. Perform checked finite
arithmetic before writes. Reject NaN/non-finite inputs and unrepresentable owned products/sums
without overwriting the last valid state. Do not silently repair corrupt foreign/native saves.
Unavailable observations remain unavailable. Avoid arbitrary new hard gameplay caps to mask errors.

### Register admitted need policies, not every discovered subclass

One owned need-policy lookup describes the explicitly supported families. No catch-all reflected
subclass activation just because the type derives Need. Native and installed evidence identify
shared setter routes plus explicit Joy/Learning/Play clamps and seeker targets. Each family requires
its own gain/loss/reset and consumer analysis. Unknown providers can expose configuration only as
unavailable until their contracts are admitted. Dormant vanilla needs are not activated by this mod.

Food: cap=max(M*multiplier,M+bonus), NutritionWanted lower-bound0; validate actual ingestion quantity.
S085 partial ingestion supplies accepted portion effects; no second ingestion or fabricated leftovers.
Rest: preserve ordinary native wake and last-rest-effectiveness rules. Joy: accepted gain after
tolerance is bounded nonnegative and used for tolerance notification. Learning: replace its direct
field-clamp path without bypassing policy; Play: prevent its second ordinary clamp deleting reserves.
Mood/Comfort/Beauty: replace only target upper clamp, retaining native cached target calculations and
Need_Seeker interpolation. Indoors/Outdoors: change only gain cap, preserving floor/reset/freeze paths.
KillThirst: qualifying kill predicate remains native; only enabled policy converts reset1 to delta1.
Chemical setter category notifications remain intact. Other named needs use actual native units and
eligibility, with no new activity or gain created to fill unused reserve capacity.

For mood, admission includes inspiration's linear100% endpoint and related-child highest mood range.
No global clamp of all mood reads: actual reserves must remain observable. Learning overflow preserves
native linear contribution to GrowthPointsPerDay; S088 Global Learning Factor follows it once, and
growth-vat/history paths retain S088 rules. S031 toddler skills remain separate.

### Eligibility reads current pawn facts

Group precedence: actual mutant classification first; then humanlike colony membership/host status;
then animals; then remaining faction groups. Specify all branches/defaults in common settings. Active
gene, worn apparel, present health and race exclusions are evaluated on current pawn state, with
supported power-tracker adapter only when that package/content is admitted. Exclusion wins over
group permission, and targeted exclusions affect only named needs.

Prefer direct native trackers for these small lists. Do not copy RuntimeHelpers.GetHashCode caches,
600-tick delay, permanent Dictionary<Pawn,float> or a world scan per UI operation. If measurement
requires shared memoization, use pawn/need identity plus lifecycle/config/content revisions and
prove event invalidation before any gain/consequence query. No persisted live references, cross-save
cache or off-thread Verse access. Existing shared Thing index is not a reason to duplicate needs.

Saved exclusion IDs use resolved provider identity and Def identity, not translated labels. Use
searchable selectors with current labels and retained unresolved entries; toggles/entries use common
settings. Proposed owned exclusions are separate from upstream DefModExtension type identity.
Source extensions or modsOverflow do not justify a facade without proven consumers.

### Consequences are per-pawn, per-save derived behavior

Keep immutable owned declarative health metadata separate from current-save effect values. Never
rewrite shared HediffDef.stages/capMods on settings changes. Implement a narrow owned dynamic effect
path or per-instance component with admitted native hunger/healing/capacity integration; choose after
focused native shape review. Do not generate one global Def per save/configuration or mutate another
provider's Def. Derive adjusted fullness and current stage from current nutrition, after loss for
the resulting state. At each native interval, the current stage affects that interval's loss; then
refresh consequences for the resulting level. Frozen needs do not lose nutrition merely to refresh UI.

Own condition lifecycle includes actual accepted gain/loss, current ordinary capacity, trait/group/
gene/apparel changes, settings commit, load, need removal and pawn teardown. Identity must be owned;
one instance, no repeated additive stacking. Disable/ineligibility/zero scale cleans only owned
consequences and never clips reserves. Visual threshold is reversible; hidden active effects remain
explained in food tooltip. Simulated update bound150ticks is contractual, not a wall-clock timer.

The five independent columns use the spec's eight-stage dormant values and ranges. Native injury
healing is multiplicative; no immunity/treatment semantics inferred. Capacity reductions are offsets
and may legitimately incapacitate fragile pawns; do not invent a consciousness safety floor that
contradicts other health policy. Vomiting uses ordinary eligible event/job rules and reciprocal
frequency with zero disabled, not deterministic every-N-ticks scheduling. Stage/frequency updates
must not queue duplicate vomit jobs. Default all columns off resolves the stale Steam/raw XML claim.

Source slower/faster coefficient encoding is inconsistent; use separate enabled/value fields with
documented proposed dormant0.5. Apply formula to actual CurLevel/MaxLevel, never the default
CurInstantLevel sentinel. Current per-event excess=max(0,fraction−1); ordinary multipliers apply once.
Disabled/zero options are identity. Forecasts must use the same pure rate policy and piecewise
threshold model without simulating mutable game state or consuming randomness to predict it.
Also gate each optional rate by need-enabled, admitted and current eligibility; grandfathering a
reserve does not grandfather its modifiers. Disable/exclusion returns both simulation and forecasts
to ordinary effective rates before the next event, without clipping the reserve.

### Restrict only the documented food routes

Apply full-food restriction to ordinary inventory snacking and appropriate player ingest orders;
preserve hungry, drug and hemogen exceptions. No universal job-start blocker: caregiving, external
drippers, medical bills and binges have distinct semantics. Validate actor-by-actor when the native
menu represents multiple selections, not context.FirstSelectedPawn alone. If an adapter cannot
preserve native action/disabled reason, withhold that optional restriction instead of replacing the
whole menu. Disabling this restriction does not change native job selection.

VNPE's selected1.6 DLL is mapped to1.7 by LoadFolders. Its real TickRare threshold/cost/ingestion route
must be checked if integrating; no patch to nonexistent StartInspirationMTBDays and no post-feed
blanket clamp. Its own threshold remains its setting, not this feature's authority. Unknown external
gain/cost paths withhold affected overflow admission rather than assuming harmlessness.

### One presentation and configuration path

Detailed scale covers max(ordinary full,current,valid instant target), with separate100% boundary
and reserve region. Preserve native scaleBar behavior for small nutrition capacities where meaningful,
threshold positions, unit labels, learning icons, arrows, tooltip ownership and tutor highlighting.
Limit visual markers by available pixels, not numeric magnitude. Restore GUI/font/color/anchor state.
Compact UI may saturate fill while explicitly reporting actual reserve. Scope edits to need surfaces;
do not patch GenUI.BottomPart/TopPart/LeftPart/RightPart globally.

S001 mood display, S003 forecasts, S007 health display and future S127 RimHUD call the same owned
presentation/policy directly. Unsupported external renderer retains its own UI unless a narrow
adapter is admitted; overflow admission must account for mandatory affected native surfaces.
No new raster art is selected here. Any future new/selected raster must pass the repository asset
skill's exact-build live visual gates; reuse of native primitives does not imply that gate is waived.

Common settings store enabled/value separately and all scope metadata, defaults, validation and
translations centrally. Validate whole effective stage/rate configuration including inheritance.
Commit atomically through common settings, before next owned decision; malformed edits do not
partially update effects. Serialize invariantly, parse localized edits deliberately. Main-menu save
callbacks reject mutation; dormant overrides survive Use global. No pawn-specific state in settings.
Only developer toolbar contains owned need editing, using native action paths where possible and
explicit percentage-point units. Do not hook Character Editor just to reproduce hidden modifiers.

### Target-spec preparation and atomic boundaries

Before implementation, create/index a stable target document for every actual Harmony/XML patch,
linking the requirement, executable admission, tests and live evidence. This candidate inventory is
not executable admission and does not authorize unguarded PatchAll.

| Candidate boundary | Minimum semantic predicates and atomic result |
| --- | --- |
| Need amount/gain/loss | Ordinary max units, old value, actual argument/delta use, lower/upper bound writes, callbacks/resets; shared base admission plus per-family gain/loss consumers; failed shared primitive disables dependents |
| Food cap/restriction | Current capacity, positive consumed quantity, zero wanted, exact inventory/menu actor and drug/hemogen/hungry exceptions; restriction can fail independently only if gain/consequence safety remains valid |
| Rest/Joy/KillThirst | Correct positive delta and gain/loss branch, native tolerance/qualifying kill/frozen paths; optional rate transforms atomic with their consumed formula |
| Seeker/roof/child needs | Exact target/gain clamp or backing write, floors/reset/learning eligibility retained; mood dependent inspiration/thought safety in mood group |
| Food consequences | Admitted owned health identity, exact hunger/healing/capacity/vomit consumers, current-state lifecycle/cleanup; all required pieces atomic for each enabled consequence |
| Native need views | Consumed percentage/instant-target meaning, geometry/tooltip/context ownership, no unintended state mutation; required views included in admission for affected reserve family |
| Persistence/lifecycle | Native amount serialization and owned settings/condition cleanup; reference identity, no double awards, save-switch isolation |
| Optional adapters/XML | Canonical active package plus exact Def/type/member/semantic contract; merge unique own extension entries while preserving unrelated lists; content missing/drift fails affected adapter closed |

Check original and effective required semantics, relevant skip-original prefixes and transpilers.
Every transpiler checks its actual incoming stream every time and returns it unchanged on mismatch.
Tolerate benign locals/offset/compiler/unrelated changes and compatible foreign owners. Never use
full hashes/versions/no-other-patches as admission. Do not rerun arbitrary foreign transpilers to
probe. Unknown effects withhold the affected group; rollback only newly installed owned members.
Supported late repatching invalidates/revalidates affected groups. If safe continuity of an existing
reserve cannot be established after drift, stop owned writes/effects, retain saved amount, report
native/provider fallback and do not pretend to guarantee that unknown foreign behavior preserves it.
Bound startup warnings per skipped enabled patch; explicitly off options are not failed candidates.

Absent optional providers are ordinary absence. Present provider with unreadable exclusion semantics
must not silently become eligible. Known content exclusions use XmlExtensions.FindMod(packageId=true)
and Def/node shape checks. Unknown package IDs remain research gates. Require XML Extensions when
the first operations ship. Preserve other extensions/capacity lists; no destructive list replacement.

## Risks / Trade-offs

- Reserves alter food/drug/work balance, especially small bodies → source-derived defaults, explicit
  numeric examples and final native comparisons; optional penalties off, no new reward/cost invention.
- Many need writers and UI consumers → narrow per-family admission with real workflows, not universal
  compatibility from one setter. Existing unrelated changes are permitted when semantics remain valid.
- Optional learning overflow boosts growth → explicit S088 example; default disabled, no double factor.
- Cleanup after shape drift can be harder than startup → establish safe lifecycle before enabling;
  never claim a safe grandfathered reserve under an unknown foreign writer.
- Huge finite values stress floats/UI → checked arithmetic and bounded geometry, no unit-per-marker
  loops. Unavailable forecast is preferable to invalid numeric output.
- Source public APIs have no proven external callers → retain research trail, avoid speculative facade.

## Migration Plan

This change modifies documentation only. Later activation uses native saved amounts and owned scoped
configuration/health identity. Source co-installation suppresses owned overlap before registration.
Do not import source global settings, sign encodings, extension types or FoodOverflow health records.
Any requested migration needs a separate backup/atomicity/restoration/postcondition contract. Disable
owned consequences and preserve finite amounts while owned compatibility remains admitted; uninstall
or incompatible foreign fallback may apply native limits and must be documented honestly.

## Open Questions

NBO-Q1–5 in the research/queue cover effective admission, absent optional contracts/actual consumers,
finalized balance/settings, measured presentation/eligibility cost and reviewed implementation/native
acceptance. IN-Q2's amount/rate relationship is resolved here; effective external adapters remain
research, not a blanket compatibility promise. No question requires stopping this sequential pass.
