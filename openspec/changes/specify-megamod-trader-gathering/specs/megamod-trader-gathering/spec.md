# Trader gathering

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Origin S039 [PL] Trading Spot. Specification only. S040 owns the dedicated trading area's cells,
designation tools and contextual gizmos; this capability owns how eligible visiting parties use it.

## ADDED Requirements

### Requirement: Visiting trade parties gather at the map's usable trading area

The mod SHALL direct an eligible on-map visiting trade caravan's ordinary arrival and waiting
behavior toward a usable destination in that map's dedicated trading area. The main trader, guards,
carriers and other party members SHALL retain their ordinary roles and physically travel through
normal paths. This SHALL not move the map-entry cell, spawn or teleport the party into the area,
recreate its trader or generate new stock. The original single-cell building SHALL not be required.
An area without a usable destination SHALL follow the fallback requirement rather than appear to
have successfully redirected the party.

#### Scenario: Caravan arrives with pack animals
- **WHEN** a normal visiting caravan enters a map with a usable trading area
- **THEN** it enters through its ordinary arrival path and walks toward the area's gathering destination
- **AND** the main trader and guards gather while carriers and escorted members retain their roles

#### Scenario: Trading identity and goods survive redirection
- **WHEN** the player meets and trades with a redirected caravan
- **THEN** its original trader, goods, currency, quantities, prices and ordinary trade eligibility remain
- **AND** choosing a different gathering destination neither duplicates nor deletes its inventory

### Requirement: Only ordinary visiting parties are redirected

The mod SHALL apply this gathering preference to ordinary visiting trade caravans and, when enabled,
ordinary colony-visitor parties. A small trader who belongs to an ordinary visitor party SHALL
follow the visitor preference, not gain the always-included caravan behavior merely by carrying
trade goods. Player caravans, hostile raids, orbital traders, unrelated transport/landing operations,
quest-managed escorts and unsupported provider-managed guests SHALL retain their ordinary behavior.
Another provider's guest rules SHALL not be taken over just because that party contains a trader.

#### Scenario: Ordinary visitor with goods
- **WHEN** a non-caravan visitor party has a small trader and visitor redirection is disabled
- **THEN** the party keeps its ordinary visitor destination even though trading is possible

#### Scenario: Other kinds of arrivals
- **WHEN** a player caravan returns, a raid arrives, an orbital trade occurs or an unrelated transport lands
- **THEN** this gathering preference does not redirect that event or its participants

### Requirement: Visitor preference uses the common settings model

The mod SHALL provide an Include ordinary visitors preference under Trading in mod settings,
with built-in global default **on**. It SHALL use common global/save inheritance, storage and UI:
new or missing save entries use global, inherited save editors are inactive, and Use global retains
the dormant override. At the main menu both save editing and Use global SHALL be inactive in
appearance and action; the global control SHALL remain available. Preference changes SHALL affect
eligible visiting parties at their next ordinary gathering decision, without requiring a new arrival
and without interrupting urgent work. Turning the preference off SHALL release already redirected
ordinary visitors to normal gathering; it SHALL not affect trade-caravan inclusion.

The dedicated area's cells and contextual controls SHALL remain map-specific gameplay state, not
global-default settings. No Requires work to place preference SHALL be introduced for the omitted
physical marker. This feature SHALL add no bottom tab or standalone bottom-right toggle.

#### Scenario: Global default and a save override
- **WHEN** save A overrides Include ordinary visitors to off and save B still uses global on
- **THEN** only B redirects ordinary visitors, and later global edits do not overwrite A's override

#### Scenario: Main-menu editing
- **WHEN** settings are opened without a loaded save
- **THEN** the global preference can change but no action creates or edits a previous save override

#### Scenario: Opt out during a visit
- **WHEN** the player turns visitor redirection off while an ordinary visitor party is waiting
- **THEN** it resumes ordinary gathering at its next gathering decision without being despawned,
  restarted or forced to abandon an urgent action
- **AND** trade caravans continue to use a usable trading area

### Requirement: Danger, rescue and departure take precedence

The mod SHALL preserve ordinary combat, fleeing, dangerous-temperature/weather reactions, rescue,
dismissal, hostility, carrier escort and departure. It SHALL never redirect a party already leaving
back toward the trading area, including a departure that first walks to an exit destination. A party
defending itself SHALL not be pulled away from that defense to the area's gathering point. If it
later returns to ordinary visiting, it SHALL use the then-current usable area without restarting
the visit. A location change SHALL not extend the ordinary stay, reroll its duration, repeat gifts
or repeat arrival/departure notifications.

#### Scenario: Visitors leave through an intermediate destination
- **WHEN** an ordinary visitor party starts walking toward its exit before leaving the map
- **THEN** it continues that departure even if the trading area is edited or remains elsewhere

#### Scenario: Caravan is attacked or conditions become dangerous
- **WHEN** a visiting party enters defense, rescue, emergency escape or ordinary dismissal/departure
- **THEN** its native response takes precedence and area redirection cannot keep it in place

#### Scenario: Repeated area edits near the end of a visit
- **WHEN** the player moves the gathering destination several times while the party's stay is ending
- **THEN** ordinary stay timing and departure remain in force, with no renewed gift or arrival reward

### Requirement: Changed destinations update existing parties without resetting them

The mod SHALL let already-present eligible parties adopt a changed usable trading destination at
their next ordinary gathering decision. An ordinary journey toward the old destination SHALL
eventually yield to the current one; editing SHALL not wait for the next caravan to arrive. It
SHALL retain party identity, membership, cargo, current visit progress and pending legitimate
transactions. Unchanged area state SHALL not repeatedly cancel/reissue jobs or create oscillation.
Temporary ordinary needs or a protected delivery SHALL finish or cancel under their own rules
before the next gathering decision.

#### Scenario: Move the area while traders are visiting
- **WHEN** the player commits a usable new trading location while a caravan is already waiting
- **THEN** that same party physically moves toward the new location when it resumes gathering
- **AND** it remains the same visit with the same trader, cargo and remaining stay

#### Scenario: No destination change
- **WHEN** the area remains unchanged while a party is already gathering appropriately
- **THEN** monitoring does not keep restarting movement, turning, trading or other legitimate actions

### Requirement: Unusable or removed areas yield to ordinary gathering

The mod SHALL reject a destination that the party cannot use under ordinary reachability, occupancy,
access and safety rules. It SHALL try other valid candidates from the current area without granting
new door, forbidden-area or provider permissions. If none are usable, or the area is empty, removed
or unavailable, the party SHALL use an ordinary usable gathering destination. If no such destination
exists, native failure/exit behavior SHALL remain in control. The area view SHALL explain the
unusable state without per-tick notification spam. The mod SHALL not send the party indefinitely
against a wall, teleport a stranded member, excavate a route itself or re-enable a departing party.

#### Scenario: A wall blocks the former destination
- **WHEN** the current gathering cell becomes inaccessible but another suitable area cell exists
- **THEN** the next gathering decision uses a suitable remaining cell and normal physical travel

#### Scenario: Remove the area or block all its cells
- **WHEN** no usable designated destination remains
- **THEN** the party returns to ordinary usable gathering or native failure/exit behavior
- **AND** a removed area is not silently retained as a permanent forced destination

#### Scenario: A carrier cannot use the route
- **WHEN** a route works for the trader but fails the party's ordinary carrier/access requirements
- **THEN** that route is not treated as valid by ignoring the carrier or granting it new permissions

### Requirement: Gathering is not a movement permission boundary

The mod SHALL use the dedicated area to choose ordinary gathering destinations while preserving
normal travel paths, party following/escort relationships and legitimate needs or emergency actions.
It SHALL not assign visiting pawns to a generic allowed area or treat the footprint as an invisible
wall. S040 SHALL constrain ordinary idle destinations to its footprint; this SHALL not force every
step of a route, every follower position or a temporary delivery to remain inside that footprint.

#### Scenario: Path crosses outside the footprint
- **WHEN** the normal path between usable destinations crosses cells outside the trading area
- **THEN** the party can follow that path if ordinary access rules permit it
- **AND** the area does not grant access to a path that is otherwise prohibited

#### Scenario: Following carrier near an edge
- **WHEN** a carrier following the trader temporarily stands outside the painted footprint
- **THEN** its normal following duty is preserved without teleporting it or converting it to a
  separately assigned allowed-area pawn

### Requirement: Courtesy delivery and gathering compose directly

The mod SHALL preserve S027's finite visitor-delivery behavior. An eligible courier SHALL be allowed
to leave gathering temporarily for its permitted delivery; repeated gathering updates SHALL not
cancel that assignment or cause its party's animals to follow the courier into storage. Afterward
the courier SHALL rejoin its party's current ordinary gathering if the visit is still active, or
follow the applicable departure/cancellation behavior. Delivery SHALL not restart the stay or
grant extra permissions, carrying capacity or additional delivery trips.

#### Scenario: Buy goods while the party is gathered
- **WHEN** S027 assigns a permitted received-goods delivery to a party member
- **THEN** that courier completes or cancels according to S027 while the trader and animals retain
  their gathering/following roles
- **AND** subsequent gathering uses the current area without a second handover or duplicate goods

#### Scenario: Visit ends during delivery
- **WHEN** ordinary or urgent departure begins while a courier has an assignment
- **THEN** S027's departure rules apply and gathering cannot pull that courier or party back into a new visit

### Requirement: Maps and saves retain their own gathering state

The mod SHALL use the party's actual map and that map's area, regardless of which map the player
currently views. Simultaneous parties SHALL keep independent visits, cargo and departure timing.
Saving and loading SHALL preserve ordinary visit progress and use the loaded map's current area
and visitor preference without duplicating arrivals, gifts or settings. Map/game removal SHALL
discard stale routing state. Loading another save SHALL not inherit destinations from the prior one.

#### Scenario: Two maps and two parties
- **WHEN** caravans visit different maps while the player views only one of them
- **THEN** each party follows its own map's area and neither changes the other's destination or progress

#### Scenario: Save during a visit
- **WHEN** the player saves and loads while a party is gathering or leaving
- **THEN** it resumes that visit/exit with existing goods and progress, without repeating arrival or gifts

### Requirement: Provider ownership and unavailable integration are visible

The mod SHALL leave provider-controlled guest parties under their provider's control unless a
specific cooperative integration is supported. Native ordinary parties SHALL remain eligible
according to this specification; merely installing Hospitality SHALL not turn its unrelated guests
into owned parties or disable every native trade caravan. If original PL Trading Spot is active,
owned overlapping redirection SHALL be withheld with a visible explanation and bounded startup
warning. Its markers, settings and behavior SHALL not be deleted, migrated or unpatched. The owned
area state SHALL remain available for later use without implying that it currently redirects parties.

If required game behavior cannot be integrated safely, the affected redirection SHALL remain
inactive, startup logs SHALL identify the unavailable behavior and fallback, and native/provider
routing SHALL remain in control. Compatible unrelated changes SHALL not by themselves disable it.

#### Scenario: Hospitality-managed guests and an ordinary trade caravan
- **WHEN** both are present on the same map
- **THEN** the guests remain under Hospitality's rules while the ordinary supported caravan can
  gather at the trading area

#### Scenario: Original provider active
- **WHEN** original PL Trading Spot is active alongside the megamod
- **THEN** owned redirection is visibly inactive and the original's objects/configuration remain untouched

#### Scenario: Unsupported game update
- **WHEN** the required routing behavior can no longer be integrated safely
- **THEN** the affected feature explains its unavailability and leaves ordinary routing intact
- **AND** it does not claim that a party has been moved merely because an area is drawn
