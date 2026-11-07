# Mining and deep-drilling priorities

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement S097/P2-26. Specification only. Explicit corrections below are selected draft
behavior, not claims that the original provider already behaves this way.

## ADDED Requirements

### Requirement: Improve automatic selection without changing mining permission
The mod SHALL rank only currently eligible targets when a worker naturally selects new automatic surface-mining or deep-drilling work.
Preserve work assignments, work-type order, schedules, designations, reservations, access, danger,
ideology and facility restrictions. Neither ranking nor a better worker arriving interrupts a valid
job already running. Explicit player orders retain ordinary precedence. Connected-ore and strip-mine
plans supply ordinary mining designations; this feature does not create extra designations, reveal
hidden deposits, move pawns or change mineable damage, work speed, experience, yield or resources.
Mining and drilling remain separate work selections, not one new combined work category.

#### Scenario: A scarce but inaccessible ore
- **WHEN** rare ore is designated but inaccessible while a less preferred designated rock is eligible
- **THEN** the worker can select the eligible rock without gaining access to the rare ore.

#### Scenario: Preserve ongoing and assigned work
- **WHEN** a pawn has another work type ahead of Mining, or is already mining a valid target when rarer ore becomes available
- **THEN** this feature neither moves Mining ahead of that work type nor interrupts the running job solely for the new target.

#### Scenario: Mining plans remain plans
- **WHEN** a player creates connected-ore or strip-mine orders
- **THEN** their existing extent and fog rules remain, and priorities apply only when ordinary eligible mining work is selected.

### Requirement: Rank surface deposits by rarity before progress
With Prefer rare surface resources enabled, eligible resource rocks SHALL precede ordinary nonresource rock; among resource rocks with valid rarity information, lower natural generation frequency ranks first, then smaller typical deposit size.
Rarity means natural occurrence, not sale price or the amount of that resource currently stored.
Resource rocks lacking usable positive rarity information follow those with known rarity but still
precede ordinary rock. With Prefer partly mined targets enabled, greater missing hit points rank
first only after all enabled rarity comparisons tie. This uses current damage regardless of its
cause, not percentage completion or a promise of the shortest remaining job. Disabling rarity removes
all its comparisons; disabling progress removes its comparison. Remaining ties prefer ordinary
horizontal proximity, then the game's ordinary tie behavior. Neither huge progress values nor very
close rarity values can reverse the declared comparison order.

#### Scenario: Rarer resource before nearer steel
- **WHEN** ordinary plasteel, steel and nonresource rock are all eligible with native rarity definitions
- **THEN** plasteel is selected before steel, and steel before ordinary rock, even when the latter are nearer.

#### Scenario: Equal frequency and different deposit size
- **WHEN** eligible compacted machinery and steel have the same generation frequency but machinery has smaller typical deposits
- **THEN** machinery ranks first before damage or proximity is considered.

#### Scenario: Progress is a later comparison
- **WHEN** an undamaged rarer ore and a heavily damaged common ore are eligible with both preferences enabled
- **THEN** the rarer ore remains first
- **AND** between otherwise tied targets the one missing more hit points is preferred.

#### Scenario: Resource without a rarity value
- **WHEN** an admitted resource rock has no positive generation frequency
- **THEN** it remains usable after known-rarity resources and before ordinary rock, without inventing a market-price ranking.

### Requirement: Rank deep drills by their next output and remaining finite deposit
With Prefer rare deep resources enabled, usable drills extracting finite deposits SHALL precede drills producing inexhaustible fallback rock; compare their next extractable resource by lower deep-generation frequency, then smaller typical deep-deposit size.
Finite resources without usable positive rarity information follow known-rarity resources but precede
fallback rock. The next output is the one the drill would actually extract, not a rarer resource
elsewhere beneath it. With Prefer drills nearer exhaustion enabled, fewer remaining finite resource
units across the drill's complete supported extraction footprint rank first after enabled rarity
comparisons tie. Count all finite resource types in that footprint; this is not just the current
cell, the full connected underground vein, or progress toward the next output portion. If rarity is
off but exhaustion preference is on, finite deposits still precede inexhaustible fallback drilling.
Clip the footprint to the map and preserve overlapping drills' real shared depletion. Remaining
ties use proximity and ordinary tie behavior. Both drilling preferences operate independently of
surface-mining preferences and do not alter extraction footprint, power, work or output.

#### Scenario: Exhaustion includes the outer part of the footprint
- **WHEN** two equal-rarity drills have 20 and 50 finite units remaining, including cells outside their immediate surrounding square but within their normal extraction reach
- **THEN** the 20-unit drill ranks first with exhaustion preference enabled.

#### Scenario: Next output rather than a hidden favorite
- **WHEN** a drill will next extract steel even though another resource occurs elsewhere within its footprint
- **THEN** its rarity comparison uses steel and does not change the native extraction order or reveal other deposits.

#### Scenario: Finite work before endless rock
- **WHEN** rarity preference is off, exhaustion preference is on, and a finite deposit competes with fallback rock production
- **THEN** the finite deposit is preferred; an inexhaustible drill is not treated as an almost-empty deposit.

#### Scenario: Drilling preferences are independent
- **WHEN** surface rarity and drill-exhaustion preferences are off but deep-resource rarity is on
- **THEN** eligible drills are still compared by their deep-resource rarity.

### Requirement: Reserve yield-sensitive work for sufficiently productive workers
When its corresponding restriction is enabled, a new automatic mining or drilling job SHALL require the worker's current Mining Yield to be at least the configured fraction of the highest suitable local worker's Mining Yield for that same work route and target.
The fraction initially is 100%, accepts values from 0% through 100%, and includes ties at the exact
threshold. This compares actual current yield, including health and supported modifiers; it is not
a skill-level cutoff, speed or passion comparison, and does not clamp yield to 100%. For surface
mining the restriction applies only where the actual resource output can vary with miner yield.
Ordinary non-wasteable rock and targets without a resource output remain unrestricted by this rule.
For deep drilling it applies to finite resource extraction, not inexhaustible fallback rock.

Suitable benchmark workers are living, spawned player workers on the target map with this actual
work route enabled, permitted worker type and required capabilities, and access to this target.
Exclude downed, drafted, mentally unavailable, wrong-map, incapable and target-ineligible workers;
a worker merely sharing the Mining work type is insufficient. Do not add animals, visitors or
prisoners to this automatic population without a separately supported work route. A capable colony
mech uses its actual yield and work capability, not an assumed human skill. Comparing surface
mining cannot reuse the drilling population or vice versa. A valid zero-yield comparison still
uses the threshold normally; unknown data is not a valid zero.

#### Scenario: Relative yield threshold
- **WHEN** the best suitable worker has 80% Mining Yield and the configured fraction is 75%
- **THEN** workers with at least 60% yield qualify and one below 60% does not.

#### Scenario: Yield beyond one hundred percent
- **WHEN** suitable workers have 113% and 100% Mining Yield with the default 100% fraction
- **THEN** only the 113% worker qualifies for new restricted automatic work, without changing either worker's actual yield.

#### Scenario: An unavailable expert cannot block the only usable miner
- **WHEN** a higher-yield pawn is downed, lacks this work route or cannot access the target, and another enabled worker can perform it
- **THEN** the unavailable pawn does not raise that target's benchmark.

#### Scenario: Different workers for drilling and mining
- **WHEN** a higher-yield pawn can operate drills but cannot perform surface mining
- **THEN** that pawn can affect the drilling comparison but does not block an eligible surface miner.

#### Scenario: Ordinary rock remains available
- **WHEN** a lower-yield eligible pawn has ordinary non-wasteable rock or fallback rock drilling available
- **THEN** this restriction does not prevent that work merely because a better miner exists.

### Requirement: Define busy-worker fallback without caller-dependent results
Ignore otherwise suitable workers busy with another job SHALL initially be off.
While off, an otherwise suitable expert doing other work, resting or satisfying needs still sets the
benchmark; reduced throughput while waiting for that expert is intentional. While on, exclude such
workers from the benchmark, but retain workers currently performing supported mining or drilling,
idle workers and workers now seeking this work. Being busy does not make an incapable worker
eligible. The same target, work route, settings and worker states give the same benchmark regardless
of which pawn's job search runs first. A candidate does not get a private easier benchmark simply
because it initiated the check. Existing committed work keeps its normal worker and progress.
If no suitable worker remains after the busy rule, show no available benchmark, without a numeric
threshold. This is a valid empty population, not unsupported data or permission to start work.
When a worker becomes ready, rebuild the comparison with all then-suitable workers before the next
automatic start; an earlier empty result must not exempt that worker from the yield restriction.

#### Scenario: Wait for the expert by default
- **WHEN** the best otherwise suitable miner is cooking and Ignore busy is off
- **THEN** a lower-yield miner below the configured threshold waits for restricted work but can perform ordinary rock mining or other assigned work.

#### Scenario: Permit a ready substitute
- **WHEN** Ignore busy is enabled while that expert cooks and another eligible miner is idle
- **THEN** the idle miner participates in the available benchmark and can qualify for new work.

#### Scenario: Reversed search order
- **WHEN** two ready miners are evaluated in opposite orders with unchanged state
- **THEN** both orders of evaluation produce the same benchmark and eligibility decisions.

#### Scenario: Everyone is busy
- **WHEN** Ignore busy is enabled and every otherwise suitable miner is occupied with unrelated work
- **THEN** contextual information shows no available benchmark and no numeric threshold
- **AND** when miners become ready, the next automatic start uses their current shared comparison rather than an exemption retained from the empty population.

### Requirement: Keep choices and explanations current
The mod SHALL use current relevant facts for the next automatic selection and contextual explanation, including while the game is paused.
Changes to health/yield, work assignment, busy/available state, map membership, access, settings,
resource damage or drill depletion cannot leave another map or work route using an obsolete result.
Show the effective threshold and a concise reason in contextual mining/drilling information when
the added restriction prevents automatic work. Existing native failure reasons retain precedence
where work is already impossible. A player-forced native mining or drilling order bypasses this
feature's automatic yield restriction and ranking, while preserving all ordinary forced-work
permissions. Explain that override; do not present a disabled native command as the only way to
discover the automatic restriction. Changing policy or loading a save does not perform work or
cancel a valid running job by itself.
The override also covers native renewals belonging to the same still-active explicit prioritized
work order. It ends when the game clears, replaces or expires that order; it does not extend its
lifetime or exempt unrelated later automatic work, another target or another worker.

#### Scenario: Update while paused
- **WHEN** the player changes Mining assignments or the threshold while paused
- **THEN** the next relevant explanation and subsequent automatic start use the changed policy without waiting for a periodic refresh.

#### Scenario: Manual lower-yield work
- **WHEN** the player explicitly orders an otherwise permitted lower-yield pawn to mine a designated valuable resource
- **THEN** that native forced job can proceed with the pawn's actual yield, while later automatic starts remain subject to the configured restriction.

#### Scenario: Renew an explicit drilling order
- **WHEN** a lower-yield pawn's explicitly prioritized drilling job reaches its ordinary renewal boundary while the same native order remains active
- **THEN** its permitted renewal retains the yield-policy override without extending the native order
- **AND** after that order ends, later automatic drilling uses the ordinary configured restriction.

#### Scenario: Map and save changes
- **WHEN** the player switches maps or loads another save with different miners and deposits
- **THEN** comparisons use that target's current map, work route and settings, not the previously inspected colony's best miner.

### Requirement: Configure policy through the common settings interface
The mod SHALL expose these preferences under Mining and drilling in the order and built-in defaults shown below.

| Preference | Default / range |
| --- | --- |
| Prefer rare surface resources | On |
| Prefer rare deep resources | On |
| Prefer partly mined targets | On |
| Prefer drills nearer exhaustion | On |
| Restrict yield-sensitive mining to the best miners | On |
| Restrict finite-resource drilling to the best miners | On |
| Ignore otherwise suitable workers busy with another job | Off |
| Minimum yield relative to the best suitable worker | 100%; 0–100%, whole-percent steps |

Each uses common Global default, Current save and Use global controls. New/missing save values
inherit dynamically; inherited per-save controls are disabled and dormant overrides retained.
Clearing Use global restores the override or seeds it from the current effective value. With no
loaded save, all save-specific controls and callbacks are inactive. Preferences affect the next
selection and explanation, not an already running job. Busy/fraction preferences remain stored but
have no effect while both yield restrictions are off. Disabling both ordering preferences for a
work route restores its remaining ordinary ordering. No player cache-refresh setting, new bottom
tab or separate mining window is added. Labels, scope, thresholds and reasons use all seven baseline
languages and distinguish rarity, yield, work availability and explicit override.

#### Scenario: Independent policy and inherited settings
- **WHEN** a save overrides the drilling restriction while retaining global surface-mining policy
- **THEN** later global changes affect only its inherited values, and another save's explicit choices remain unchanged.

#### Scenario: Disable ordering
- **WHEN** both surface rarity and partly-mined preferences are off
- **THEN** surface targets retain ordinary ordering while the separate yield restriction can remain active.

#### Scenario: Main menu
- **WHEN** settings are opened without a loaded game
- **THEN** global defaults remain editable and no Current save or Use global callback can mutate a previous or implicit save.

### Requirement: Preserve native behavior when an integration is unsupported
The mod SHALL activate each mining or drilling contribution only when its required behavior can be supported safely, allowing unrelated compatible modifications.
If another provider supplies a non-neutral ordering without an agreed combination, leave that whole
selection's ordering to the provider, including ties; independently valid yield restrictions can
remain. Unknown candidate/benchmark data must not become invented rarity or yield; the affected
contribution remains unavailable with an explanation and its remaining native/provider behavior.
Startup logs identify skipped enabled patches, failed requirements, unavailable behavior and fallback
with bounded warnings. With original or continued Mining Priority active, withhold all overlapping
owned S097 behavior and leave the provider untouched. Preserve actual work, deposits, drill progress
and saved common preferences. No source settings import, foreign package impersonation or remote
worker scheduling is implied.

#### Scenario: Unsupported ordering but valid restriction
- **WHEN** an external provider owns drill ordering and no supported combined order exists
- **THEN** its entire ordering remains, while an independently supported owned yield restriction can still govern new automatic starts.

#### Scenario: Original provider coexistence
- **WHEN** an original or continued Mining Priority provider is active alongside megamod
- **THEN** owned overlapping rules are inactive with a bounded startup explanation and no foreign patches, settings or active jobs are removed.

#### Scenario: A changed work path
- **WHEN** the required surface-mining integration cannot be admitted but drilling remains supported
- **THEN** surface mining retains the remaining game's behavior with an explanation, and supported drilling can continue without changed resources or progress.
