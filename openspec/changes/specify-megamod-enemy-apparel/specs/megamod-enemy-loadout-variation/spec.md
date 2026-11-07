# Enemy loadout variation

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S083 specification only; source-derived generation values are provisional until measured.

## ADDED Requirements

### Requirement: Add equipment choices without replacing existing pools
When expanded loadouts are enabled, ordinary eligible non-player pawn generation SHALL consider
jump packs, hunter packs and turret packs wherever defensive utility packs are ordinarily eligible,
provided the respective DLC and item exist. Preserve all existing candidates and ordinary faction,
body, material, layer, locked/required apparel, availability and budget rules, except for the explicit
native torso substitutions below. These additions do not
force a pack onto every pawn or create new items. Do not introduce duplicate memberships or repeat
generation to obtain a preferred result.

Where an item has no explicit generation-allow chance, the initial extra pack allowance is **15%**;
an existing explicit provider value takes precedence. This is a candidate allowance within ordinary
selection, not a promise that 15% of all enemies wear that pack. Existing budget semantics, including
required equipment, remain; there is no independent raid-point or charge multiplier.

Grenadier, phoenix and locust armor also join appropriate spacer-military candidates. The following
source-derived alternate candidates apply to matching kinds; listed chances are consideration chances
for each eligible alternative under ordinary generation, not final equipment shares for an entire raid.

| Pawn family | Added alternative | Initial consideration chance |
| --- | --- | --- |
| Empire cataphract | Phoenix armor | 50% |
| Empire champion | Locust armor | 50% |
| Odyssey traders-guild elite and salvager elite, with Royalty | Grenadier or locust armor | 50% per eligible alternative |
| Odyssey traders-guild slasher and salvager scrapper, with Royalty | Locust armor | 10% |

For these named alternatives, phoenix may replace the Empire cataphract's ordinary cataphract torso
armor, locust may replace the champion's ordinary plate armor, and grenadier/locust may replace the
guild/salvager elite's ordinary marine torso armor. If no valid alternative is selected, the ordinary
required torso remains. Preserve required helmets, shield belts and all other unrelated requirements;
the result has one legal torso loadout, not two overlapping suits.

Existing body/layer/color/style restrictions and additional or custom provider locks remain
authoritative. These narrow native substitutions do not authorize replacing a provider's custom
required gear or bypassing required hazard protection. If those constraints conflict, retain the
provider's valid loadout. Psychic/shard lances and deadlife packs receive no new spawn-pool
membership from this feature; they can still be used when acquired through existing valid generation.

#### Scenario: Expanded pack candidate
- **WHEN** an eligible non-player pawn is generated with expanded loadouts enabled
- **THEN** supported added packs can compete with the original utility choices under the same ordinary restrictions, rather than replacing that pool

#### Scenario: Custom allowance and required clothing
- **WHEN** another mod supplies an explicit pack generation allowance or incompatible required torso apparel
- **THEN** its allowance remains and its required valid clothing is not removed to force an owned alternative

#### Scenario: Missing DLC
- **WHEN** Royalty or Odyssey is absent
- **THEN** the absent equipment and dependent cross-DLC alternatives are omitted while existing ordinary equipment generation remains intact

#### Scenario: Native torso alternative
- **WHEN** a cataphract's valid phoenix alternative is selected, or no valid alternative is selected
- **THEN** it wears phoenix instead of the ordinary cataphract torso in the first case and keeps the ordinary torso in the second, preserving required headgear and other compatible equipment

#### Scenario: Salvager descendants
- **WHEN** an Odyssey salvager elite or scrapper is generated with Royalty and expanded loadouts enabled
- **THEN** it receives the same stated alternative opportunity as the corresponding guild elite or slasher, with its own remaining restrictions intact

### Requirement: Generation edits apply to new pawns and preserve final readiness
Loadout settings SHALL affect new eligible generation, not rewrite existing pawns, refill equipment,
replace corpses' loot or reroll a saved encounter. Generated items retain ordinary quality, durability,
charges and loot consequences. A normal subsequent equipment change is allowed and does not grant
the removed item's action permanently. When owned weather/hazard preparation is active, select a
final wearable loadout satisfying its required protection; an added combat candidate cannot silently
displace necessary vacuum/temperature protection. No duplicate disposal/refund or extra pawn is created.
Owned generation smoothing publishes the fully prepared group once, with no additional generation
or trait/gear roll on save/reload or staged continuation.

#### Scenario: Disable after a raid arrives
- **WHEN** the player disables expanded loadouts after an equipped raid is present
- **THEN** those pawns keep their actual equipment and charge balances, while future generation stops adding this feature's candidates

#### Scenario: Required hazard protection
- **WHEN** the selected added armor conflicts with active required arrival protection
- **THEN** the final pawn keeps a legal protected loadout, dropping the incompatible extra choice without duplicating items or rerolling the group

### Requirement: Configuration uses clear inherited behavior settings
The feature SHALL expose the following common global/default-save settings under Combat / Non-player
equipment, replacing source implementation-switch labels. Each has Global default, Current save and
Use global controls. These are configuration choices, not a new live-play toggle or bottom tab.

| Setting | Built-in default | Effect timing |
| --- | --- | --- |
| Automatic offensive apparel use | On | Next uncommitted combat decision |
| Tactical equipment jumps | On | Next uncommitted jump decision |
| Expanded non-player loadouts | On | New pawn generation only |
| Optional Ideology combat talents | Off | New grants and new automatic talent decisions; existing effects settle normally |

New/missing save entries inherit current globals dynamically. Overrides affect only their save.
Checking Use global retains its dormant override; unchecking restores it or seeds the first override
from the effective global value. The save editor is inactive while Use global is checked.
At the main menu disable both the save editor and Use global,
including resets and mutation callbacks, while global controls remain usable. Scoped resets affect
only that scope. Disabling new automation does not undo committed projectiles/jumps, restore spent
charges, remove real equipment or replay grants. Missing DLC settings remain visible as unavailable
with an explanation while their stored choices remain dormant. All UI is localized in seven languages.

There is no source-style "Keep mod abilities" compatibility switch: one supported action preserves
manual use and charge accounting without choosing between duplicate implementations. The explicit
generation/tactical numbers in these specs are named policy values; any future configurable numeric
row must use this same inheritance and timing model rather than a separate settings store.

#### Scenario: Separate use and generation choices
- **WHEN** automatic offensive use is disabled but expanded loadouts remains enabled
- **THEN** future eligible pawns can still receive the extra equipment, but this feature does not initiate their offensive equipment casts

#### Scenario: Inheritance and current save
- **WHEN** a global setting is changed at the main menu and two saves are loaded in turn
- **THEN** the inherited save follows it, an overridden save keeps its own choice, and no save mutation was possible from the main menu

#### Scenario: Disable during flight
- **WHEN** tactical jumps are disabled after a charged jump has started
- **THEN** that flight resolves once with its ordinary spent charge and no new automatic jump starts under the disabled policy
