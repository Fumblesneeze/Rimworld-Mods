# Remembered home beds

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
S073 observable draft only. Portable-bed common settings and compatibility rules apply.

## ADDED Requirements

### Requirement: Home assignments survive travel as explicit remembered intent
HB01. A departing player-controlled colonist SHALL retain each valid nonmedical home-bed claim by default; optional release records remembered intent tied to the actual bed, map and ownership allowance.
The shared release-on-departure setting starts false under the composed S115 ownership policy.
When enabled, release eligible claims left behind on the departing home map, allowing ordinary use
while away, and remember each separately. A bed moving with its owner is not left behind. Retained
claims do not also create pending reclaim. Remembering alone grants neither exclusive access nor
an extra sleeping slot. An owned temporary camp rest/custody record must not replace home intent.
Visiting another home map or failing a reclaim does not erase it. Preserve each returning co-owner
of a multi-slot bed independently. Save/load restores the same unresolved intentions without acting
as a fresh map arrival or duplicating claims. Current-map temporary beds left behind remain physical.

Destroying or permanently removing the referenced bed invalidates that remembered destination;
do not guess a replacement from nearby position or a similar label. Later deliberate player bed
assignment or unassignment supersedes old remembered intent in the affected map/allowance; explicit
cancellation targets the selected memory. Choices on other maps and owned temporary rest transitions
do not cancel it. An ordinary new home assignment can become the remembered
destination on the next departure. Do not change graves, thrones or special resting assignments.

#### Scenario: Travel through another home map
- **WHEN** release-on-departure is enabled and a colonist leaves home A, rests temporarily elsewhere, visits home B and then returns to A
- **THEN** A's remembered bed remains pending until valid reclaim or explicit cancellation, and the camp bed does not become the new home destination.

#### Scenario: Double bed and save load
- **WHEN** release-on-departure is enabled, both owners of a double bed travel, and the game is saved and reloaded
- **THEN** both remembered claims remain separately visible without claiming three slots or replaying map departure.

#### Scenario: Player assigns a different bed
- **WHEN** the player deliberately assigns another bed in the remembered allowance or cancels that memory
- **THEN** the old intent does not reclaim itself on later arrival, and unrelated current assignments remain intact.

#### Scenario: Default departure retains ownership
- **WHEN** a colonist leaves home without enabling release-on-departure
- **THEN** valid home claims remain assigned and no redundant pending reclaim is created.

#### Scenario: Another map does not cancel memory
- **WHEN** release is enabled and a traveler with pending home intent deliberately claims a bed on another map
- **THEN** the new local claim does not cancel the remote remembered destination.

### Requirement: Reclaim validates the actual home bed and co-owners
HB02. On return to the remembered home map, the mod SHALL reclaim an available valid sleeping slot only after checking the pawn's current bed permissions, ownership allowance and current player intent.
Respect medical/prisoner/slave status, size, ideology, sharing, faction, access and other applicable
ordinary restrictions. Do not unclaim a different current bed before knowing the reclaim can succeed.
An occupied or unavailable bed stays visibly pending by default, preserving other owners; it can be
reconsidered when those conditions change or the player explicitly retries, without repeated notices.

The initially disabled occupied-slot option permits displacing at most one other current owner's
claim when the bed is otherwise valid and full. It never ejects an actual sleeper or displaces another
remembered returning co-owner. Choose the last eligible current owner in the displayed owner order, retain the others
and expose the reassignment through ordinary ownership information/messages. The option relaxes
ownership contention only, not bed eligibility or physical safety. If no such owner can be displaced,
keep the return pending. Successful reclaim ends that pawn's pending memory once.

#### Scenario: Someone else claims the bed
- **WHEN** a traveler returns to a full remembered bed with occupied-slot reclaim off
- **THEN** the current owners remain, the returner's memory stays pending, and no unrelated current bed claim is lost.

#### Scenario: Aggressive reclaim is not universal permission
- **WHEN** occupied-slot reclaim is enabled but the bed is medical, incompatible or currently occupied by a sleeper
- **THEN** no forced reassignment occurs; a valid unoccupied full bed can displace only one eligible owner while preserving other remembered co-owners.

#### Scenario: Both owners return
- **WHEN** both remembered owners return to a double bed temporarily assigned to unrelated pawns and the occupied-slot option is on
- **THEN** each can reclaim a valid slot once without the second arrival evicting the first returner.

#### Scenario: Displayed order differs from assignment history
- **WHEN** occupied-slot reclaim is enabled and several eligible owners could be displaced, with the most recently assigned owner appearing before another eligible owner
- **THEN** reclaim replaces the last eligible owner in the displayed order, not the most recently assigned one, while preserving sleepers and remembered returning co-owners.

### Requirement: Remembered ownership is contextual and cancellable
HB03. The remembered home bed and returning pawn SHALL expose whose claim is pending, why it cannot currently be restored, and Retry and Cancel actions for that individual memory.
Retry reevaluates current reclaim conditions without changing permissions or bypassing a failure.
Cancel removes only the selected pending intent, not another traveler's memory or the pawn's current
temporary/different bed assignment. Destruction or missing content clears invalid references with
an understandable status rather than an endless retry. No new settings page for individual owners,
bottom tab, unrestricted ownership override or hidden exclusive reservation is introduced.

#### Scenario: Cancel one double-bed memory
- **WHEN** the player cancels one returning owner's memory while that pawn owns a temporary camp bed
- **THEN** only that owner's home memory is removed; the other remembered owner and camp-bed assignment remain unchanged.

#### Scenario: Home bed no longer exists
- **WHEN** the remembered bed is destroyed or removed by a content change
- **THEN** the invalid destination is reported and retired without assigning another bed by position or mutating unrelated owners.
