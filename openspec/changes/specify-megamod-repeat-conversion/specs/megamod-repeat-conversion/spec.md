# Recurring conversion assignments

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S092. Observable draft; nothing implemented or accepted.

## ADDED Requirements

### Requirement: Assign an existing conversion ability to one chosen pawn
The feature SHALL offer a contextual Keep converting command for a player-controlled humanlike
colonist who already possesses a supported conversion ability. The player SHALL select one eligible
other pawn, see the assigned target, and cancel that assignment from the guide's commands. Each
guide SHALL have at most one assignment, tied to the guide's ideology when assigned. No ability,
role, target, ideology or conversion success SHALL be granted by making the assignment. Assignment
SHALL remain possible during ability cooldown when the target is otherwise eligible.

#### Scenario: Choose a target during cooldown
- **WHEN** the player selects an eligible guide whose Convert ability is cooling down and assigns an eligible pawn of a different ideology
- **THEN** the guide's command shows that target and retains the assignment while waiting for ordinary readiness
- **AND** the target's certainty and the ability cooldown do not change merely from assignment

#### Scenario: Reject an ineligible selection
- **WHEN** the player points the assignment command at a baby, animal, self, same-ideology pawn or another target rejected by the supported native ability
- **THEN** no assignment to that target is created
- **AND** the normal targeting restrictions, including accepted prisoner exceptions, remain effective

#### Scenario: Cancel and choose again
- **WHEN** the player cancels the displayed assignment and chooses another eligible target
- **THEN** only the new target remains assigned and each other guide's assignment is unchanged

### Requirement: Repeat ordinary casts without duplicating or displacing orders
The feature SHALL consider a new assignment on the next simulation tick and reconsider waiting
assignments at least once per configured check interval. When eligible, it SHALL submit one ordinary
conversion job, immediately for an idle guide or after existing work and queued orders for a busy
guide. It SHALL neither clear the player's queue nor interrupt existing work to force conversion.
At most one automatic conversion job SHALL be pending or running per guide. Existing casts or
queued casts sharing the chosen ability's cooldown group SHALL prevent another automatic cast.
The selected ability's ordinary travel, warmup, target effects, certainty change, thoughts, messages,
charges and cooldowns SHALL remain unchanged. Native restrictions and the assignment's current
eligibility SHALL be rechecked before an automatic effect occurs.
Automatic casting SHALL also respect reasons that disable the ordinary manual command, including
incapability of required work, activity or ceremony restrictions and deathrest. Merely having an
ability off cooldown SHALL not bypass those restrictions. The currently running owned cast SHALL
not reject itself solely because it occupies its own place in the ability queue.

#### Scenario: Repeat after a normal attempt
- **WHEN** an assigned guide is eligible and completes a conversion attempt that does not yet convert the target
- **THEN** the normal certainty change, reaction and cooldown occur
- **AND** the assignment waits for readiness and later attempts another ordinary conversion

#### Scenario: Busy guide and frequent checks
- **WHEN** a guide has current work and queued player orders, and many checks occur before the conversion order can run
- **THEN** those orders remain intact and only one automatic conversion is queued behind them
- **AND** no repeated checks create a trail of duplicate conversion jobs

#### Scenario: Another ability already owns the opportunity
- **WHEN** the guide is casting or has queued an ability in the same cooldown group
- **THEN** automatic conversion waits rather than starting a competing cast or shortening that cooldown
- **AND** owned specialist-command automation observes the same ordinary cast ownership

#### Scenario: Conditions change during approach
- **WHEN** a target becomes ineligible, the guide loses the ability, or the assignment is cancelled before an automatic conversion effect
- **THEN** that automatic order performs no stale conversion effect
- **AND** already completed native effects and unrelated jobs are not undone

#### Scenario: Player redirects the guide
- **WHEN** the player drafts or redirects a guide approaching an automatic conversion
- **THEN** the player's order takes precedence under ordinary interruption rules
- **AND** automation does not immediately restore the interrupted job or clear the new order

#### Scenario: Guide cannot perform the required work
- **WHEN** a guide possesses Convert but the normal command is disabled because the guide cannot perform Social work
- **THEN** no automatic cast is submitted or completed merely because cooldown and target checks pass
- **AND** the assignment waits with the native unavailability reason

#### Scenario: Ceremony or activity disallows the command
- **WHEN** the guide's current ceremony or other native activity disallows an otherwise ready conversion ability, including after an automatic approach began
- **THEN** automatic conversion waits or stops before its effect without overriding that restriction
- **AND** the activity and unrelated manual orders remain under ordinary control

### Requirement: Retain assignments through temporary unavailability
The feature SHALL wait without clearing the assignment while the guide is drafted, downed, crawling,
in a mental state, not mobile or lying down; while the target is temporarily unconscious or in a
mental state; or while cooldown, ordinary cast restrictions, distance, allowed-area or safe
reachability checks prevent use. A temporary condition SHALL not override a separate terminal
reason to end the assignment. Sleeping targets SHALL be treated according to the waking setting;
that permission SHALL not bypass incapacity or other native restrictions. Waiting SHALL not produce
repeated rejection messages or retry loops.

#### Scenario: Mental break ends
- **WHEN** the guide or target has a non-terminal mental break and later recovers
- **THEN** the same assignment remains and can resume at a later eligible check
- **AND** no conversion is forced during the break

#### Scenario: Target beyond the configured range
- **WHEN** the target is beyond the enabled straight-line distance limit, outside the guide's allowed area, or unreachable without danger
- **THEN** the guide keeps the assignment without starting the trip
- **AND** a later eligible check can resume after those restrictions are resolved

#### Scenario: Sleeping target
- **WHEN** an otherwise eligible target sleeps with waking disabled
- **THEN** the assignment waits without waking that target
- **AND** enabling waking permits only the normal conversion approach and interaction

#### Scenario: Existing work or temporary ability refusal
- **WHEN** work, a shared cooldown or another temporary native ability restriction prevents conversion
- **THEN** automation retains the assignment and waits without manufacturing certainty loss or a completed cast

### Requirement: End assignments when their purpose or participants no longer apply
The feature SHALL finish an assignment when the target adopts the intended ideology. It SHALL
cancel when either participant dies, is destroyed or leaves the assignment's map; the target enters
cryptosleep, ceases to be humanlike or becomes hostile under the admitted targeting rules; or the
guide loses colony ownership, its intended ideology or the supported conversion ability. Terminal
changes SHALL be processed before another cast is submitted even during temporary unavailability.
Cancellation SHALL withdraw only this assignment's queued or unfinished automatic work, leaving
manual conversion orders and unrelated work alone. No subsequent return, resurrection or regained
role SHALL silently restore a cancelled assignment. Non-successful automatic cancellation SHALL
give one clear reason; ordinary conversion-success feedback SHALL not be duplicated as an error.

#### Scenario: Conversion completes
- **WHEN** this or another ordinary interaction converts the target to the assigned ideology
- **THEN** the assignment ends and its unused automatic conversion orders disappear
- **AND** later certainty changes do not silently restart it

#### Scenario: Guide loses the role or changes faith
- **WHEN** the guide loses the supported ability or changes away from the assigned ideology
- **THEN** the assignment cancels with its reason even if the ability was previously cooling down
- **AND** the target is not automatically pursued for the guide's new ideology

#### Scenario: Departure or cryptosleep
- **WHEN** either participant leaves the map, or the target enters cryptosleep
- **THEN** the assignment and its unused automatic work are cleared with one reason
- **AND** returning to the map or opening the casket does not recreate the assignment

#### Scenario: Cancel before or after an effect
- **WHEN** the player cancels while an automatic job is queued, approaching, warming up or already completed
- **THEN** unfinished owned conversion is withdrawn without cancelling a separate manual order
- **AND** a completed certainty change or native cooldown is retained, with no further automatic attempt

### Requirement: Configure timing and preferences through common inherited settings
The feature SHALL use the common Global default, Current save and Use global controls in the
Ideology / Repeated conversion settings section. New saves SHALL inherit global values. Settings
SHALL persist and affect the next decision, including revalidation of unfinished automatic casts.
The player SHALL see timing in understandable game-time units alongside ticks and distance in cells.
The distance control SHALL be inactive while its limit is off without erasing its saved value.
All save-specific controls and mutation actions SHALL be inactive at the main menu; inherited
per-save controls SHALL be inactive while Use global is checked.

| Preference | Built-in global default | Allowed values |
| --- | --- | --- |
| Check interval | 2,500 ticks, one in-game hour | Whole ticks from 1 to 60,000 |
| Limit trigger distance | On | On / off |
| Maximum trigger distance | 1,000 cells | Whole cells from 1 to 1,000; inclusive straight-line limit |
| Allow waking the target | On | On / off |
| Pause for a conversion ritual without repeat penalty | Off | On / off |

These SHALL be consistent defaults for new and missing owned values. Invalid user edits SHALL be
rejected with the prior value retained; invalid saved values SHALL recover to the corresponding
built-in default with a bounded diagnostic. Check interval SHALL never shorten ability cooldown.

#### Scenario: Global and save values differ
- **WHEN** one save inherits a global interval and another overrides it
- **THEN** changing the global default affects only the inheriting save
- **AND** returning to either save restores its own inheritance and effective values

#### Scenario: Bounds and main menu
- **WHEN** the player opens these settings without a game, or submits an out-of-range number
- **THEN** no save-specific value can be changed and the invalid number does not replace its prior valid value
- **AND** the reason and effective value remain visible

#### Scenario: Turn off distance restriction
- **WHEN** the player disables the distance limit
- **THEN** distance alone does not block an eligible conversion and the stored maximum remains available when re-enabled
- **AND** map, allowed-area, safe path and ordinary cast restrictions still apply

### Requirement: Reserve conversion opportunities for the matching ritual when requested
The feature SHALL pause automatic conversion when the ritual preference is enabled and the guide's
ideology has a matching conversion ritual whose repeat penalty is inactive. The matching ritual
SHALL correspond to the chosen ability's role and cooldown group. Absence of a matching ritual SHALL
not block conversion. This preference SHALL test repeat penalty rather than promise that a venue,
participants or every other ritual prerequisite is ready. It SHALL not start rituals, alter their
penalty periods, or block the player's manual conversion command.

#### Scenario: Ritual has no repeat penalty
- **WHEN** the preference is enabled and the matching ritual is outside its repeat-penalty period
- **THEN** the guide retains the assignment but performs no automatic conversion
- **AND** the status explains the ritual preference even if the player has not prepared a venue

#### Scenario: Penalty active or ritual absent
- **WHEN** the matching ritual has an active repeat penalty, is absent, or the preference is off
- **THEN** this preference does not block an otherwise eligible automatic conversion

#### Scenario: Optional leader conversion
- **WHEN** an admitted Exalted Priesthood leader uses the provider's conversion ability
- **THEN** the corresponding leader conversion ritual supplies this check
- **AND** an unrelated Moralist ritual does not incorrectly reserve that leader's opportunity

### Requirement: Preserve assignment intent across saves without hidden migration
The feature SHALL save each guide's target and intended ideology as contextual gameplay state,
separate from global/save configuration. Reloading SHALL preserve valid assignments, temporary
waiting states and at most one existing automatic job; it SHALL neither cast during loading nor
reinterpret the assignment for a newly adopted ideology. Missing, invalid or unresolved participants
SHALL leave no runnable assignment and produce a bounded explanation without preventing other
assignments from loading. New games SHALL start with no assignments. Source-mod saved assignments
or settings SHALL not be silently imported.

#### Scenario: Save with a pending or paused assignment
- **WHEN** the player saves while a guide is waiting, travelling or has one pending conversion order, then reloads
- **THEN** the same valid target and intended ideology remain with no duplicate order or load-time conversion
- **AND** ordinary cooldown and certainty state remain intact

#### Scenario: Ideology changed before saving
- **WHEN** the guide changes ideology after assignment and the game is saved before the next check
- **THEN** loading cancels the stale assignment rather than redirecting it to the new ideology

#### Scenario: Missing participant
- **WHEN** a saved assignment cannot resolve a living eligible participant
- **THEN** that assignment cannot act, other valid assignments load normally, and no repeated reference-error stream is produced

### Requirement: Keep contextual UI and optional providers correctly bounded
The feature SHALL show assignment, cancellation and a concise waiting reason on the selected guide's
existing command surface, with no new bottom tab or global bottom-right assignment toggle. Controls,
settings, tooltips and reasons SHALL support all seven baseline languages and compact command sizes.
Native Convert SHALL be preferred when the guide possesses it; otherwise an admitted Vanilla Ideology
Expanded leader conversion SHALL be eligible. Ability ownership and required memes SHALL remain
mandatory. Ideology absent SHALL leave this feature inactive while unrelated megamod features work.
Original Keep Converting loaded SHALL suppress owned overlap with a bounded startup warning; initial cooperative-original support is excluded.
Changed or missing integration prerequisites SHALL disable only the affected behavior with one
bounded startup warning per skipped enabled patch naming its target, reason and fallback. Compatible
foreign changes SHALL be allowed; original-provider presence SHALL not be mislabeled as missing playability.

#### Scenario: Inspect the assignment
- **WHEN** the player selects a guide with an assignment in a full-size or compact command layout
- **THEN** the target, cancellation action and current waiting reason are readable in the selected language
- **AND** other pawn and ability commands keep their ordinary behavior

#### Scenario: Supported provider supplies the ability
- **WHEN** the optional provider and required meme are present and its leader ability is admitted
- **THEN** the leader can use the same assignment workflow with the provider's real effects and cooldown
- **AND** no provider role or ability is granted by enabling automation

#### Scenario: Original provider or unavailable integration
- **WHEN** original Keep Converting is active, Ideology is absent, or an adapter's required semantics changed
- **THEN** the relevant owned controls and scheduling stay inactive with the appropriate status or bounded warning
- **AND** no duplicate conversion scheduler, altered foreign patch or unrelated feature failure is introduced
