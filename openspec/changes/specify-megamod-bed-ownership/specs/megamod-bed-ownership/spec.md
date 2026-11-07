# Bed ownership and assignment

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **RimWorld 1.7 beta**.
S115 observable draft. Nothing implemented or accepted.

## ADDED Requirements

### Requirement: Bed claims belong to the place they serve
BO01. An eligible pawn SHALL retain one ordinary bed assignment on each surviving map, with a separate allowance for one bed wholly on gravship substructure on that map.
The two allowances mean ground-based and substructure-based, not one allowance per individual ship.
A bed only partly on substructure belongs to the ground-based allowance. Without supported
substructure content there is only the ordinary allowance. Assigning a different bed replaces only
the pawn's claim in that same allowance; other maps and the other allowance remain unchanged.
Animals retain eligible animal-bed assignments; this does not allow humans into animal beds or
animals into human beds. Ordinary medical, faction, prisoner/slave, size, ideology, sharing and
special-building restrictions remain applicable. No extra sleeping slots or forced sleep are granted.

Deathrest casket assignments have their own corresponding allowances when that content is present.
They do not erase ordinary beds, bypass deathrest eligibility/binding limits, or make every modded
resting building a supported casket. Thrones, graves and meditation spots retain their native
ownership scope; generic assignment controls below do not give them per-map duplicate ownership.

#### Scenario: Camp and home beds
- **WHEN** a traveler with a home bed is assigned a bed on a second map
- **THEN** both assignments remain, and changing the second-map bed does not unassign the home bed.

#### Scenario: Two local allowances
- **WHEN** a pawn owns a ground-based bed and a bed wholly on substructure, then receives another substructure bed on that map
- **THEN** only the old substructure assignment is replaced; the ground-based bed remains assigned.

#### Scenario: Animals and special assignments
- **WHEN** the player tries a human/animal mismatch or assigns an eligible deathrest casket
- **THEN** the mismatch is rejected with the ordinary reason, while a valid casket assignment preserves the ordinary bed and its native binding restrictions.

#### Scenario: Other assignable buildings
- **WHEN** the player assigns a different throne through the enhanced controls
- **THEN** native throne reassignment applies; the pawn does not gain a throne on every map.

### Requirement: Preferred local beds and remote bedroom information are distinct
BO02. The player SHALL be able to mark one currently assigned bed or casket as a pawn's preferred bed, or clear that preference, from the selected bed's contextual controls.
The choice is individual pawn/bed gameplay state, saved with the game. Setting another preference
replaces the first. Removing that assignment clears its preference; it does not reassign the bed.
Where multiple assigned candidates serve the pawn's current map, prefer the chosen candidate,
otherwise a ground-based candidate before one on substructure. Resolve equal choices consistently
across redraws and reloads. Ordinary eligibility and care instructions still decide actual bed use;
a preference cannot authorize a forbidden bed or override medical/deathrest requirements.

Bedroom information that needs one main assigned bed uses the current map's selected assignment
when present, including the pawn's containing/carrier map. With none, use the oldest surviving home
map with an assignment, then the oldest other map with an assignment; apply the same local preference
within that map. This preserves meaningful ownership/bedroom information while traveling without
teleporting a pawn, reserving a remote bed for a local job, or borrowing its comfort/healing at camp.
Partner-bed searches use the sleeper's map. A remote partner's local assignment can be considered
only when ordinary sharing and bed-use rules permit it.

#### Scenario: Prefer the ship bed
- **WHEN** a pawn owns both local beds and the player marks the substructure bed preferred
- **THEN** it becomes the first eligible assigned choice; clearing the preference restores ground-based priority.

#### Scenario: Preference does not grant access
- **WHEN** the preferred bed becomes ineligible for the requested rest
- **THEN** ordinary eligible alternatives or ordinary fallback remain available without using the forbidden bed.

#### Scenario: Caravan bedroom information
- **WHEN** a pawn with surviving home assignments travels without a current-map bed
- **THEN** main-bedroom information remains tied to a real assigned home bed, while any actual camp sleep uses local facilities and their actual effects.

#### Scenario: Partner on another map
- **WHEN** a sleeper's partner is away but owns an eligible shared bed on the sleeper's map
- **THEN** the local bed can be considered under normal sharing rules; a bed on the partner's remote map is not a local sleep destination.

### Requirement: Assignment search preserves owners and meaningful rejection reasons
BO03. Ordinary assignment dialogs SHALL provide transient case-insensitive literal search by the displayed short pawn name.
Start empty on each newly opened dialog; clearing restores its normal candidates. Keep existing
owners visible and individually unassignable regardless of the query. Show each pawn once.
Among unassigned candidates matching the query, put current love partners of an existing owner
first, then other candidates, alphabetically within each group. Label the relevant relationship
without concealing identity or native rejection text. Matching native candidates that cannot be
assigned remain visible and disabled with their reason; search does not add otherwise absent pawns.
An empty result explains that no candidates match and leaves the owners and clear-search control
accessible. Search, sorting and opening the dialog do not change assignments.

Preserve normal mouse/keyboard operation, scrolling, close behavior and other admitted dialog
controls. Desktop focus can enter search on opening; controller/Steam Deck interaction must not
force an unwanted keyboard on opening. One dialog's text/focus must not leak to another or a new save.

#### Scenario: Find a partner without hiding owners
- **WHEN** a double-bed owner remains assigned while the player searches for that owner's partner
- **THEN** the owner remains visible, the matching partner appears ahead of other matching candidates, and no ownership changes until assignment is chosen.

#### Scenario: Ineligible matching candidate
- **WHEN** a name matches a native candidate who is too large for the bed
- **THEN** that row remains disabled with its size reason rather than disappearing or becoming assignable.

#### Scenario: No match and a new dialog
- **WHEN** a query matches nobody and the player clears it or opens another building's assignment dialog
- **THEN** candidates return for the cleared/new dialog, without duplicated owner rows, stale text or changes to either building.

### Requirement: Contextual assignment is explicit and revalidated
BO04. Selecting a player pawn or animal and right-clicking an ordinarily assignable building SHALL offer an assignment action for eligible selected candidates, whether drafted or undrafted.
Assignment itself need not order the pawn to walk there, and an animal need not accept drafted
movement orders. An already assigned single pawn needs no duplicate assignment action. Respect the
building's ordinary visibility, candidate set and restrictions; medical/prisoner controls are not
unlocked merely by this shortcut. Other context-menu actions remain intact.

For a multi-pawn action, identify the exact selected pawns affected. Do not silently discard an
ineligible selected player pawn and present the remainder as the whole group. If the group cannot
fit or cannot share, show an unavailable action and reason. Pawns already assigned count once.
On activation, recheck the current selection intent, building, capacity, candidates and restrictions
before changing any claim. An invalid or changed request leaves all assignments unchanged.

An explicit valid reassignment to a full ordinary bed can make room by removing only the necessary
nonselected owners. For a single incoming pawn, prefer removing a nonpartner; otherwise use the
last eligible nonselected owner in the displayed owner order. For a group, remove nonselected owners
from the end of that order. Preserve selected existing owners and unrelated beds. This changes
ownership only; do not forcibly relocate an occupant or grant extra concurrent sleepers. Other
building types retain their ordinary reassignment behavior and restrictions.

#### Scenario: Assign an animal
- **WHEN** the player selects an animal and right-clicks its eligible animal bed
- **THEN** an assignment command works without requiring the animal to accept ordinary drafted orders and without removing unrelated menu actions.

#### Scenario: Group includes an existing owner
- **WHEN** an existing double-bed owner and their eligible partner are selected and assigned together
- **THEN** both occupy the two assignment slots once, without temporarily evicting the selected owner or losing that pawn's remote bed.

#### Scenario: Ineligible or oversized group
- **WHEN** the selected group includes an incompatible pawn or exceeds the building's total slots
- **THEN** the group action explains why it is unavailable and assigns no silent subset.

#### Scenario: State changes after the menu opens
- **WHEN** ownership permissions or capacity change before the displayed assignment is activated
- **THEN** the action is rechecked and a now-invalid request changes no claims.

#### Scenario: Deliberate full-bed reassignment
- **WHEN** a valid incoming pawn is explicitly assigned to a full bed containing their partner and a nonpartner
- **THEN** the nonpartner's claim on that bed is replaced, while the partner, remote claims and actual sleeping-slot limit remain intact.

### Requirement: Unassignment and owner navigation stay local to their targets
BO05. An ordinarily assignable building SHALL expose Unassign all and navigation to each current owner through contextual controls.
Unassign all is unavailable when empty; otherwise it removes only that building's current claims
and applicable preference/memory for those claims. Individual unassignment has the same local scope.
It must not unclaim a different bed merely because that is the pawn's current-map/main bed.
Go to owner selects and navigates to the actual owner using ordinary map/world navigation; if the
owner has no navigable location, show the reason without losing unrelated selection or changing
ownership. Support separate co-owners and ordinary assignable thrones/caskets where controls exist.
No new bottom tab or bottom-right toggle is added. These choices are entity state, not settings.

#### Scenario: Unassign a remote owner's bed
- **WHEN** the player unassigns a home bed while its owner uses another bed elsewhere
- **THEN** only the selected home claim and its remembered return/preference are cleared; the other bed is untouched.

#### Scenario: Navigate among co-owners
- **WHEN** the player chooses a particular double-bed owner's navigation control
- **THEN** the game selects/navigates to that pawn's real supported location, without reassigning either owner.

#### Scenario: Empty building or unavailable owner
- **WHEN** a building has no owners, or a recorded owner has no navigable location
- **THEN** the relevant action is disabled with an explanation rather than causing an error or silently clearing another selection.

### Requirement: Relationship and eligibility changes update the affected assignments
BO06. When ordinary relationship logic elects to share a bed, the feature SHALL consider compatible vacant slots in both partners' assigned ordinary beds across maps and allowances.
For an allowance, consider the initiating bed owner's compatible vacant bed before the other
partner's; make at most one shared choice there. Do not bounce between two competing beds, evict
other owners automatically, exceed capacity, or bypass eligibility/ideology. Sharing records a valid
claim; it does not send an absent partner to that map. Do not repeatedly enforce automatic sharing
after a deliberate player unassignment. Deathrest caskets are not partner-sharing beds by default.

When a relationship actually ends through breakup, divorce, rejected proposal causing breakup or
replacement of a lover/fiancé relation, remove one of that pair from each still-shared ordinary bed.
Choose either former partner with equal probability for each affected bed, once for the completed
event. Preserve other co-owners and all nonshared beds; a rejected proposal that leaves the relation
intact does not trigger this cleanup. Later redraw/load must not repeat the random removal.

On an actual status, ideology, life-stage/body-size or supported building-eligibility change, release
only assignments that are no longer valid, including incompatible cribs on other maps. A real native
all-ownership-release event clears all affected bed/casket claims; it is not inferred merely from
travel. Preserve normal unrelated grave/throne effects of that event. Never turn one invalid bed
into a reason to erase all the pawn's valid claims.

#### Scenario: Partners have beds on two maps
- **WHEN** ordinary sharing begins and each map has a compatible free slot in one partner's bed
- **THEN** the pair can share those beds without evicting other owners or creating a local travel job for an absent partner.

#### Scenario: Two competing beds in one allowance
- **WHEN** both partners already own separate compatible beds with free slots in the same allowance
- **THEN** the initiating owner's bed is the shared choice, and the process does not immediately move them back to the other bed.

#### Scenario: Relationship remains or ends
- **WHEN** a proposal is rejected without breakup, or instead the relationship actually ends
- **THEN** the first case leaves beds unchanged; the second removes one former partner per shared bed once and preserves unrelated owners/assignments.

#### Scenario: Child outgrows a crib
- **WHEN** a child becomes too large for a crib assigned on another map
- **THEN** that invalid crib claim is removed while eligible assignments remain.

### Requirement: Moving and loading beds preserve identity without inventing capacity
BO07. Saving and loading SHALL preserve valid assignments and preferences by actual pawn and physical bed identity, without replaying travel, sharing or assignment actions.
Destroyed beds, permanently removed maps, dead/removed owners or unavailable content retire invalid
claims and explain relevant losses through ordinary ownership information/messages. Loading a
different save must not retain the previous game's owners, preferences or search state.

Packing a bed suspends its installed claim; unpacking the same physical bed can restore the previous
eligible owner only if the destination allowance remains free and no newer player choice supersedes
it. An unrelated bed of the same type or at the same position is not that bed. S073 temporary custody
and recovery remain distinct from ordinary owner count. Normal destruction warnings are preserved;
only a deliberate owned transition's expected release message can be suppressed narrowly.

A supported gravship move carries the same bed and eligible claims to its actual destination. On
arrival or substructure reclassification, if two claims collide in one allowance, retain the already
established destination claim as active and show the moved/reclassified claim as pending. If no
established destination claim exists, keep one consistent active choice and mark the rest pending.
The player can choose which valid claim to retain or cancel the pending one. Pending intent is not
an extra owner/slot or an invisible reservation; do not evict unrelated owners to resolve it. Missing
or ambiguous transfer support leaves affected automation inactive with an explanation.

#### Scenario: Save and switch games
- **WHEN** a game with local/remote claims and a preference is saved, reloaded, then another save is opened
- **THEN** the first game restores its actual claims once and the second inherits none of them.

#### Scenario: Packed bed and later player choice
- **WHEN** a bed is packed, the player assigns a different bed in its allowance, and the original is unpacked
- **THEN** the later choice remains; the original bed does not silently take it back.

#### Scenario: Gravship arrives beside another ship bed
- **WHEN** a moved assigned substructure bed arrives on a map where the pawn already has a substructure assignment
- **THEN** the destination claim stays active, the moved intent is visibly pending, and choosing one does not disturb the ground-based claim.

#### Scenario: Bed destruction is local
- **WHEN** one assigned bed is destroyed while its owner is elsewhere
- **THEN** that claim/preference is retired with the normal loss notification, preserving other beds and without adopting a replacement at the same cell.

### Requirement: Portable beds and remembered home claims use one ownership policy
BO08. The combined feature SHALL retain home claims on departure by default, including while a temporary bed is used elsewhere.
The common release-on-departure setting starts false. If explicitly enabled, release only applicable
nonmedical home claims left behind on the departing map and retain per-map/per-allowance remembered intent
under S073's guarded reclaim, pending reason, Retry and Cancel rules. Do not clear thrones/caskets or
other maps through this option. Setting changes affect subsequent departures; they do not instantly
release or recreate claims. Do not manufacture a pending reclaim for a claim that remained intact.

An owned temporary deployment does not replace a different allowance's ordinary claim, preference
or home memory. Within the same allowance, use an eligible vacant temporary bed without creating a
second ordinary ownership claim; preserve the ordinary home assignment and recover the actual
temporary item through its custody record. A deliberate player assignment/unassignment supersedes
remembered intent only for the affected allowance or explicitly selected remembered bed. Another
map's deliberate choice does not silently cancel the home memory. Optional occupied-slot reclaim
retains S073's protection of actual sleepers, returning co-owners and all ordinary restrictions.

#### Scenario: Ordinary trip with portable sleep
- **WHEN** a pawn leaves home with default settings and uses an owned temporary bed at camp
- **THEN** the home claim remains, the physical portable bed is counted once, and no redundant home reclaim is queued.

#### Scenario: Optional release with two home allowances
- **WHEN** release-on-departure is enabled for a pawn with ground-based and substructure home beds left behind
- **THEN** both released claims are remembered separately and reclaim only under their own still-valid allowance and S073 contention rules.

#### Scenario: Same-map temporary bed
- **WHEN** home deployment is enabled and a pawn temporarily uses a portable bed while retaining an ordinary bed in that allowance
- **THEN** the temporary rest and recovery do not erase the ordinary claim or create a second ordinary owner claim.

#### Scenario: Manual camp assignment preserves remote memory
- **WHEN** a pawn with a pending home claim is deliberately assigned a bed on another map
- **THEN** that local choice remains, and the remote home memory is canceled only by a choice affecting its own allowance or explicit cancellation.

### Requirement: Comfortable seat rest is a bounded ordinary-rest fallback
BO09. With seat sleeping enabled, a pawn with an ordinary comfort need SHALL prefer an eligible nearby sittable building with positive comfort over ground sleep when no usable ordinary bed or eligible S073 deployment is selected.
Search the nearby area within4 cells first, then within12 cells if none qualifies. This does not
promise the most comfortable seat or a colony-wide search. Respect actual walkability/reachability,
allowed areas, forbidden/reserved cells and existing rest/safety constraints. Recheck at job start.
If no valid seat remains, use ordinary ground-rest behavior rather than getting stuck retrying.
Keep emergency/medical rest, duties, timetables, drafted restrictions and existing environmental
self-preservation authoritative. A chair does not become an owned bed, medical bed or deathrest
casket. Apply only the actual seat's normal comfort and normal rest effects, with no added bonus.

Repeated reservation for the same current rest must be harmless without treating another job's
reservation as success or bypassing other pawns' reservations. Disabling seat sleeping affects new
rest choices; it does not repeatedly interrupt an already valid sleep episode.

#### Scenario: Seat instead of ground
- **WHEN** an eligible pawn would otherwise sleep on the ground and a permitted comfortable seat is within4 cells
- **THEN** ordinary rest uses that seat with its actual comfort, without claiming it as a bed.

#### Scenario: Outer search and ordinary fallback
- **WHEN** no inner seat qualifies but one within12 cells does, then that seat becomes unavailable before work starts
- **THEN** the outer candidate can first be chosen, but the changed conditions cause fresh ordinary fallback rather than forced reservation or repeated failed jobs.

#### Scenario: Portable bed and care take precedence
- **WHEN** an eligible portable bed can be deployed, or medical rest is instructed
- **THEN** the chair fallback does not replace that deployment or care instruction.

### Requirement: Rest settings use shared inheritance and contextual ownership controls
BO10. Seat sleeping SHALL start enabled and use the common settings path under Pawns / Rest and portable beds, after the shared home-claim options.
Use global defaults, per-save overrides and Use global with the existing shared loading, storage,
resolution and row rendering. New/missing saves inherit current globals; checking Use global retains
a dormant override, and clearing it restores that value or seeds from the current effective value.
Disable the per-save editor while inheriting. At the main menu disable both per-save and Use global
controls and their callbacks while keeping the global editor available. Save switching resolves the
correct values. Apply changes at the next relevant rest/departure decision without rewriting claims.
Use one release-on-departure row shared with S073, default false, not conflicting duplicate options.

Search, owner choices, preference and pending claims are contextual state and never mod settings.
Translate owned controls, explanations, settings and reasons in English, German, Spanish, French,
Chinese Simplified, Russian and Japanese. Keep native contextual entry points, accessible row
content and ordinary UI scaling; add no debug override or new bottom tab.

#### Scenario: Main menu and inherited save
- **WHEN** settings are opened at the main menu, then in a save using global values
- **THEN** save mutation is unavailable at the menu, and the save's individual editor stays disabled until Use global is cleared.

#### Scenario: Dormant override and later departure
- **WHEN** a save's release setting is overridden, temporarily set to Use global, then restored
- **THEN** its dormant override returns without changing another save/global value or immediately releasing an existing bed.

### Requirement: Compatibility failures leave ownership changes under one authority
BO11. Each enabled patch SHALL apply only when its documented minimal behavioral prerequisites hold, allowing compatible changes from other mods.
For a failed, unknown or ambiguous prerequisite, validation error or installation failure, leave that
patch or its declared dependent group inactive and log one bounded startup warning per skipped
enabled patch identifying the package, patch/spec, target, reason, unavailable behavior and fallback.
Support later revalidation without duplicate application or removal of another owner's patches.
Ownership mutation/load reconciliation must not run partially without the matching claim readers,
writers and recovery support. Unaffected native actions and separately admitted controls remain.

If the original Sensible Bed Ownership provider is active, leave overlapping owned behavior off and
warn; never spoof its identity, disable it automatically or alter its settings/patches. Evaluate
overlapping S073 automation against the admitted ownership authority before it runs. Existing owned
records remain readable; unknown authority stops new owned assignment/reclaim work, preserves
physical objects and reports unavailable recovery instead of replaying changes through another mod.
No automatic legacy settings/save import or safe mid-save provider removal is promised. An explicit
future migration requires its own backup, atomicity, failure restoration and postcondition contract.
Optional missing or changed content disables only the affected adapter, not unrelated beds.

#### Scenario: Benign and broken changes
- **WHEN** another mod changes an unrelated bed effect, or instead removes a required claim-reconciliation behavior
- **THEN** the first can remain admitted; the second disables the dependent owned ownership behavior with bounded diagnostics rather than applying half a policy.

#### Scenario: Original provider returns
- **WHEN** a save with owned claims is opened with the original source provider enabled
- **THEN** overlapping owned automation stays off, physical beds remain, and unresolved owned intent is readable without silently rewriting it under the source provider.

#### Scenario: Optional content is absent
- **WHEN** deathrest or gravship content is absent or an optional custom assignment provider has changed incompatibly
- **THEN** unsupported special behavior remains unavailable with the applicable explanation while independently valid ordinary bed behavior remains available.
