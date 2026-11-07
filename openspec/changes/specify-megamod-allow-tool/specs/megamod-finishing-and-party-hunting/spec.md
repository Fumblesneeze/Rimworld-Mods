# Finishing and drafted party hunting

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Specification draft; nothing implemented. Source: Allow Tool, S008 / P1-08.

## ADDED Requirements

### Requirement: Deliberately finish downed targets
The mod SHALL offer Finish off for downed living pawns through area/selected-target orders and a
selected worker's right-click menu, whether drafted or undrafted. Colonists, nonhostile-faction pawns
and colony prisoners require Shift when giving the order; previously authorized designations can
be performed without holding Shift. The menu explains friendly-target protection and worker skill
or violence restrictions. A worker must be capable of violence, have a usable melee attack and be
able to reserve and reach the target. Finishing non-animals requires Melee 6; animals have no Melee
threshold. Bypassing the skill check is a deliberate developer-toolbar override, inactive outside
developer mode, and does not bypass violence incapacity. This is separate from item destruction.

#### Scenario: Protected friendly target
- **WHEN** the player tries to finish a downed colony prisoner without Shift
- **THEN** no finishing order is created and the restriction is explained
- **AND** Shift permits the explicit order, with caution feedback for a mass action including friendlies

#### Scenario: Worker eligibility
- **WHEN** a Melee-5 worker tries to finish a non-animal, or a violence-incapable pawn tries to finish an animal
- **THEN** the action is unavailable with the relevant reason
- **AND** an otherwise eligible Melee-5 worker can finish a downed animal

### Requirement: Finishing is a visible interruptible job before the killing blow
The mod SHALL have the worker approach, prepare for 60 game ticks with a visible weapon effect,
then perform a finishing blow that kills a still-valid target. If the target recovers, dies, leaves
the map, loses its order, or the job is interrupted before the blow, no delayed kill occurs.
Recovery, death or leaving the map removes that target's finishing order; it must not reactivate
if the pawn is later downed again. Interrupting only the worker while the target remains valid
leaves the explicit order available to another eligible worker until the player cancels it.
Friendly executions retain the applicable execution thoughts, colony-prisoner execution tale and
animal-slaughter record. By default, finishing unforbids selectable forbiddable objects in the
target's cell and its eight adjacent cells; disabling that setting prevents this convenience action.
Non-player-forced finishing can chain to another already-designated eligible target less than
eight cells away. A player-forced single finishing action does not automatically chain.
The source's occasional humanlike skull bubble (25% chance) and weapon glint are presentation only;
their presence or absence does not alter damage, timing or consequences.

#### Scenario: Recovery during preparation
- **WHEN** the target stands up or the player cancels its designation during the 60-tick preparation
- **THEN** the worker stops without executing the recovered or canceled target

#### Scenario: Recovery invalidates the authorization
- **WHEN** a designated target recovers and is later downed again
- **THEN** its earlier finishing mark has been removed and a new explicit order is required
- **AND** merely interrupting the worker while the target remains downed does not cancel that valid order

#### Scenario: Consequences and nearby items
- **WHEN** an explicitly authorized eligible friendly target is finished with nearby-unforbidding enabled
- **THEN** the target dies, the applicable execution consequences occur, and nearby eligible forbidden
  items in the 3×3 area become allowed
- **AND** a separate player-forced order does not execute a second nearby target automatically

### Requirement: Drafted hunting is individual pawn state
The mod SHALL give eligible player-controlled drafted pawns a Party hunt toggle on their own controls.
It is initially available, displays violence incapacity where applicable, and persists membership
with the pawn while drafted. Undrafting clears membership; redrafting starts with the toggle off.
Disabling availability ends membership. With membership on, a pawn waiting in drafted combat and
not busy attacks the closest eligible wild animal it can see while Fire at will is enabled. Prey
must be revealed, factionless and within the weapon's minimum/maximum range; melee acquisition uses
two cells but does not extend the actual attack's reach. Animals designated for taming are excluded.
Attacking keeps active hunters from being automatically undrafted merely for waiting.

#### Scenario: Fire-at-will and tame protection
- **WHEN** a waiting party hunter has two visible wild animals in range and the nearest is marked for taming
- **THEN** the other eligible animal is targeted
- **AND** disabling Fire at will prevents new automatic attacks without changing party membership

#### Scenario: Undraft and reload
- **WHEN** a party hunter is saved while drafted and the save is reloaded
- **THEN** its membership remains on that pawn
- **AND** undrafting and redrafting clears membership rather than silently resuming party hunting

### Requirement: Drafted hunting policies coordinate attacks and finishing
The mod SHALL expose save-inheritable hunting policies in mod settings: automatically finish prey
on, hunt only designated prey off, and unforbid corpses from party-hunter kills off initially. The designated-only filter
applies to both automatic attacks and newly chosen finishing targets. When auto-finish is on,
hunters stop shooting downed prey and eligible commoner-work-capable hunters can approach downed
prey within 20 cells after all party members within 20 cells cease being busy. They use ordinary
finishing eligibility and return to their previous position after finishing/chaining, unless a new
player order, undrafting or an invalid return destination supersedes the return. This does not
guarantee safety from nonparty gunfire. Fire at will governs shooting; already-authorized finishing
can proceed with it off. Tame-marked prey is excluded from automatically chosen finishing too, an
intentional correction to the source. When auto-finish is off, commoner-work-capable hunters can
continue attacking downed prey; hunters unable to perform commoner work do not gain finishing work.

The finish-nearby-unforbidding setting applies to finishing kills independently of the party-hunter
corpse setting. The latter allows any corpse from a kill attributed to a current party member,
including ranged, melee and finishing kills and non-prey victims, but does not allow adjacent items.
It does not expand automatic target eligibility. The tooltip explains these scopes and their overlap.
Party hunting creates no hunter seat, hunting area or animal companion;
those separately specified features will compose through ordinary work and reservations.

#### Scenario: Wait for the party before approaching
- **WHEN** an eligible hunter downs prey within 20 cells while another nearby party member is still busy attacking
- **THEN** automatic finishing waits until nearby party members are no longer busy
- **AND** finishing then uses a reservation and the normal approach/preparation/blow, followed by return

#### Scenario: New command supersedes return
- **WHEN** the player orders the hunter elsewhere or undrafts it during automatic finishing
- **THEN** no stale return order later overrides that command

#### Scenario: Preserve taming and designation policies
- **WHEN** auto-finish is enabled near a downed tame-marked animal, or designated-only is on near
  unmarked prey
- **THEN** no new automatic finishing order is placed on that animal
- **AND** explicit authorized Finish off orders remain a separate player decision

#### Scenario: Independent corpse policies
- **WHEN** unforbid corpses from party-hunter kills is off but nearby-unforbidding after finishing is on
- **THEN** a shooting kill does not automatically allow its corpse through the party-hunt option
- **AND** a finishing kill still performs its explicitly configured 3×3 unforbidding action

#### Scenario: Corpse option also covers melee and finishing kills
- **WHEN** the party-hunter corpse option is on and nearby-unforbidding after finishing is off
- **THEN** a corpse from that party member's melee or finishing kill becomes allowed without allowing adjacent items
- **AND** a non-prey victim killed by that member also has its corpse allowed, without becoming
  eligible for automatic party hunting before death
