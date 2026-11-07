# Refinement decisions — round two

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), RimWorld **1.7 beta**.
Recorded **2026-11-07** from the user's answers11–19. This continues the
[round-one decisions](2026-11-07-decisions-round-1.md). Accepted choices below supersede the earlier
recommendations and conflicting draft defaults. They do not claim implementation or acceptance.

## Accepted answers

| Question | Accepted decision | Bounds and consequences for specification |
| --- | --- | --- |
| 11 | No free equipment for the player. Existing player-owned pawns entering vacuum must bring real suitable gear. | Do not satisfy arrival protection by materializing a suit for a player-owned pawn. Ordinary starting loadouts, actual carried supplies and genuine NPC arrival outfits remain distinct from a new free-equipment grant. A pre-existing NPC/world pawn reused for an arrival is not excluded from question10's protection requirement merely because it was generated earlier. Keep the protection goal with actual equipment and ordinary travel/arrival rules. |
| 12 | Generation proceeds in the background while simulation continues. Announcements/cards appear only after generation completes. Discard the pending result if its source or requested action is no longer valid. | The proposed automatic simulation hold is rejected. Cover raid notices, caravan ambush notices and hostile-map entry cards. Examples of invalidation: destination map abandoned, originating caravan gone, or caravan no longer targeting that map entry. Answer19 clarifies that the suggested one-to-two seconds is a rough real-time estimate, not a limit. |
| 13 | Drug Response uses **Use global per condition**. | A local override for one condition does not freeze unrelated inherited condition rules. Each condition's complete rule, including its response slots, has its own inheritance choice; individual field-level inheritance was not requested. Specify explicit disable/removal, missing conditions and global additions/deletions without confusing them with inheritance. |
| 14 | Recycling recovery scales with remaining condition. Nearly destroyed equipment should yield barely any usable material. | Apply remaining HP fraction to the accepted25% basic construction-material recovery. Reconcile rounding and any separately declared fixed salvage so it cannot silently defeat the near-destroyed-item intent. Complex ingredients remain excluded by the initial policy. |
| 15 | Make unmedicated emergency self-amputation substantially more forgiving. | The13%/19.5%/26% proposal and its0.2 potency factor are superseded. Choose and document a more generous skill-dependent curve; no exact replacement percentages were selected. Preserve visible chance, explicit player choice, actual cost and ordinary consequences. This remaining calibration is specification/balance work, not another request for the user to explain source behavior. |
| 16 | Colonists continue packing their meals during food scarcity. | Do not introduce the proposed one-day communal-food cutoff or a shortage-based ban on optional packing. Actual available supplies, reservations, carrying capacity, dietary rules and ordinary eating/sharing still apply; this creates no food or duplicate claims. |
| 17 | New hunting stations default to animals with **0% chance to retaliate**. | Replace the empty initial filter. This is the initial selection, not a prohibition on later explicit player choices. Research the applicable native retaliation data for the species chooser; unknown or merely rounded-down probabilities must not be treated as demonstrated zero. Preserve map-possible-species scope and entity-local choices. |
| 18 | Medical-operation matches have **no visible distinction** between generally established and heuristic matches. | Remove the separate Possible matches group, confidence badge and opt-in checkbox. Use one ordinary result list and ordinary sorting for both. Keep internal provenance, researched exclusions and corrective exceptions for fixing bad matches. This does not authorize unavailable operations, change treatment effects, guarantee success or make opening the filter order an operation. |
| 19 | The suggested one-to-two seconds means real elapsed time and is only the user's estimate. | Do not turn it into a code or test assertion, hard deadline, timeout, cancellation trigger or performance acceptance threshold. Measure actual responsiveness and completion without treating this estimate as a guaranteed bound. |

## Generation cancellation and publication

The readiness check must cover the whole encounter or destination, including required gear and
contents. Merely finishing a notification or preparing part of the group is not completion.
Nothing should reveal the new encounter through its announcement before that preparation completes.
At publication, recheck that the same source and requested action are still valid. Do not deliver a
late raid to an abandoned map, an ambush to a vanished caravan or a map-entry card after that caravan
has changed its destination/intent. Such cancellation is an intentional discarded result, not a
successful encounter, a reroll, or permission to retarget it silently.

Cleanup must distinguish temporary generated results from pre-existing pawns, player possessions
and already published world state. Discarding preparation must not destroy an existing world pawn
borrowed for the operation, consume player equipment or tear down an unrelated map. Release only
that pending operation's temporary work/claims. These are required design and verification concerns;
no thread model or safe asynchronous game API has yet been established by this decision.

Preserve the player's own pause/speed choices. Continuing simulation during preparation rejects an
automatic pause introduced by smoothing; it does not authorize undoing a manual pause. Save/load
must preserve valid pending work once and recognize invalidated work as canceled. The former blanket
promise that every prepared result eventually appears needs this explicit invalid-context exception.

## Timing clarification — resolved

**Answer19:** The user means **real elapsed time**, but the one-to-two-second figure is a rough
estimate, **not a requirement to enforce or assert in code or tests**. The previous phrasing as a
maximum delay or selected bound was incorrect and is superseded. Retain it only as explanatory
context. Do not derive a hard timeout, cancellation rule, performance promise or pass/fail threshold
from it. Actual generation measurements can inform implementation and review without testing this
number. Continued simulation, complete-before-notice ordering and cancellation of invalid contexts
remain behavioral requirements. Slowness alone does not make a valid source or request invalid.

The remaining reviewed backlog is predominantly source/API research,
UI verification and playability calibration. No further broad questionnaire is needed now. Ask again
only when a concrete unresolved product choice or demonstrated conflict remains after research.

## Completed draft reconciliation

The following amendments are applied together with round one. Preserve upstream reports as
source facts and document our deviations; do not rewrite what the inspected original mod does.

| Affected change | Required amendment |
| --- | --- |
| `specify-megamod-weather-ready-arrivals`, `specify-megamod-spacefarer-policy` | Combine all-humanlike vacuum protection with the no-free-player-equipment rule and actual inventory/supply semantics. |
| `specify-megamod-generation-smoothing` | Continue simulation, notices after readiness, source/intent revalidation, discarded invalid results, ownership-safe cleanup, and save/quest/arrival accounting for intentional cancellation. Question19 is resolved: the one-to-two-second real-time estimate must not become an enforced or asserted timing criterion. |
| `specify-megamod-drug-response` | Per-condition inheritance, UI rows and storage/edit/reset/global-add/delete behavior instead of a whole-collection override. |
| `specify-megamod-equipment-recycling` | Condition-scaled returns, fractional rounding and fixed-salvage treatment consistent with the near-zero recovery intent. |
| `specify-megamod-emergency-self-amputation` | More generous unmedicated curve, examples and settings/constants, alongside round-one available-medical-help eligibility. |
| `specify-megamod-routine-meal-carrying` | Explicitly retain ordinary personal packing during food scarcity; no proposed shortage cutoff. |
| `specify-megamod-hunting-stations` | Initialize species choices from the native0%-retaliation criterion; preserve later player choices and define uncertain/missing/provider data behavior. |
| `specify-megamod-condition-medical-bills` | One visually unified result list, with inference/exception provenance kept internal and ordinary medical permission checks retained. |

Affected proposals, observable scenarios, designs, tasks and replacement coverage now carry these
choices. [Reconciliation audit](2026-11-07-decision-reconciliation-audit.md) records verification;
no runtime task is completed by these document amendments.
