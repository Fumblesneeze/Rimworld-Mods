# Optional non-player combat talents

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S083 specification only. Ideology is required for this capability, optional for the whole mod.

## ADDED Requirements

### Requirement: Optional talents retain real ability behavior
When Optional Ideology combat talents is enabled, eligible non-player humanlike combatants SHALL
be able to use supported Berserk trance and Combat command that they genuinely possess during
ordinary combat. An already possessed supported marksman command can likewise be used, but this
does not manufacture a new role or ability for every fighter. Existing ability eligibility, intended
recipients, actual warmup, duration, range, cooldown and ongoing-effect restrictions remain.
Do not recast an already active equivalent aura merely because an enemy is nearby, or create a
parallel cooldown to bypass the real one. The source-derived baseline aura radius is **9.9 cells**,
warmup **0.5 seconds**, duration **1000 real-time seconds at normal game speed** (24 game hours).
Normal trance duration is **126 such seconds** (approximately 3 game hours). Current supported native
ability values remain authoritative rather than stale copied values.

No player's manual or specialist-work autocast is taken over. Turning this option off stops new
owned automatic decisions; an already committed cast or effect resolves normally. Source-advertised
automatic commander/marksman grants have no connected generation rule in the inspected source and
are explicitly not supplied until their recipient/chance contract is separately resolved.

#### Scenario: Possessed command in combat
- **WHEN** an eligible non-player commander already has a supported ready combat command and enters combat
- **THEN** it can perform the ordinary cast and apply its actual aura to valid nearby allies, without duplicate cooldowns or repeated refresh while already active

#### Scenario: No invented commander ability
- **WHEN** a generated boss has no combat command or marksman ability
- **THEN** this feature does not invent one merely from the boss label, and ordinary combat remains available

### Requirement: Tribal berserker grants are explicit and do not erase other generation
With the option enabled, newly generated eligible non-player tribal berserkers SHALL have an initial
**35%** chance for the owned combat-trance talent. This is an explicit grant chance, separate from
ordinary unrelated implant generation; preserve existing implant choices, money and probabilities.
Unrelated implant selection and its available budget remain unchanged.
The talent is not a tradeable implant, craftable item or new surgery recipe. It grants no charges or
talents to unrelated pawn kinds, existing pawns or player-controlled starting characters.

A granted berserker can use the normal trance effect in combat, with the source special talent's
**1800-game-tick cooldown** (30 seconds at normal speed), ordinary **0.5-second warmup**, and normal
126-second effect duration. It cannot recast while the same trance is active even if cooldown expires.
The successful grant and its cooldown persist across save/load without another probability roll.

#### Scenario: Talent roll alongside other implants
- **WHEN** an eligible tribal berserker is generated with the option enabled
- **THEN** one 35% talent opportunity occurs independently of its existing implant selection, without replacing that pool or adding a physical implant item

#### Scenario: Reload does not reroll or reset trance
- **WHEN** the player reloads after a berserker's grant and committed trance cast
- **THEN** the same grant result, real cooldown and ongoing effect remain rather than granting another talent or immediately recasting

### Requirement: Defeat removes the owned talent without restoring recruitment
A pawn receiving the owned generated talent SHALL be visibly unrecruitable under ordinary recruitment,
as the source describes. Downing or death removes this owned granted talent and access to its future
casts; it does not remove unrelated implants, natural role abilities or other mods' effects. Existing
trance/aura effects follow their normal defeat/death termination rules. Recovery from being downed
does not regrant the talent. Its recruitment exclusion remains after defeat; removal of the combat
talent is not a promise of ordinary recruitability. Native and other provider recruitment restrictions
also remain. An explicit external recruitment override is not automatically undone by repeated polling.

#### Scenario: Defeated berserker
- **WHEN** a pawn with the owned generated talent is downed and later recovers
- **THEN** it no longer has that granted cast, receives no replacement implant or loot, and remains excluded from ordinary recruitment with a visible explanation

#### Scenario: Unrelated role and implants
- **WHEN** the same pawn also has a genuine role ability and unrelated implants
- **THEN** talent removal leaves those intact and does not claim to own their effects, cooldowns or recruitment policy
