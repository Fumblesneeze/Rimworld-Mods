# Automatic baby names

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin: S044 Auto Name Babies replacement. Draft only; no implemented or verified gameplay.

## ADDED Requirements

### Requirement: Name confirmed unnamed player babies once
The feature SHALL automatically name a humanlike baby belonging to the player when its name is
confirmed to be a temporary native birth name. It SHALL use the ordinary full-name generator for
that baby, including its supported culture, gender and xenotype naming rules and generated nickname.
It SHALL preserve a successful result through duplicate notifications, language changes and reloads.
Non-player babies, animals, pawns beyond the supported baby developmental category and unsupported
naming systems SHALL remain unchanged. S031 toddlers still belong to that category: their confirmed
pending names SHALL remain eligible during catch-up or adoption. Supported alien childhood categories
SHALL follow their actual development and naming rules, without imposing human age cutoffs.

#### Scenario: Ordinary player birth
- **WHEN** a supported player baby is born with a confirmed temporary name
- **THEN** it receives one usable generated name before the birth notice is presented
- **AND** birth outcome, genes, parentage, health and care needs remain those of the ordinary birth

#### Scenario: Already processed baby
- **WHEN** the same birth is reported again or its successful name is loaded in another language
- **THEN** the same name remains and neither first name nor surname is rerolled

#### Scenario: Unowned or older pawn
- **WHEN** a foreign baby is born or a pawn beyond the supported baby category or an animal is encountered
- **THEN** this feature does not change its first name, surname, notification or development

### Requirement: Preserve chosen names and conservative existing-save recovery
The feature SHALL preserve names accepted by the player or supplied as genuine names by supported
providers, even when the first name is Baby. On initialization it SHALL process confirmed pending
player baby names across maps, contained pawns and retained world/caravan or deceased pawns once.
Ambiguous legacy names or damaged naming history SHALL remain unchanged, with native controls left
available under their ordinary rules. Recovery SHALL neither renew naming deadlines nor rewrite
historical names merely because they resemble a placeholder.

#### Scenario: Deliberate Baby
- **WHEN** a player has accepted Baby as the child's first name and later reloads
- **THEN** that name is retained and no automatic replacement is attempted

#### Scenario: Confirmed pending name outside the active map
- **WHEN** a save opens with a confirmed pending player baby in a caravan, container or another map
- **THEN** it is named once using current effective policy without moving it or requiring that map to be viewed
- **AND** any remaining ordinary naming opportunity is not extended

#### Scenario: Ambiguous old-language name
- **WHEN** an older save contains a possible placeholder whose temporary status cannot be established
- **THEN** the name is preserved and no successful-naming claim or hidden naming control is introduced

#### Scenario: Pending deceased baby
- **WHEN** initialization finds a retained deceased player baby with a confirmed pending name
- **THEN** it can receive a name without resurrection, changed death history or renewed naming permission

#### Scenario: Toddler with a pending name
- **WHEN** catch-up or ordinary adoption encounters an S031 toddler with a confirmed pending name
- **THEN** it receives a name under the same rules while retaining its toddler development and restrictions
- **AND** a toddler with a chosen name remains unchanged regardless of that name's spelling

### Requirement: Apply explicit parental surname policy
The feature SHALL offer Father, Mother, Random parent and Ideology policies, defaulting to Random
parent. Father SHALL try father, genetic mother, then birth parent; Mother SHALL try genetic mother,
birth parent, then father, taking the first usable surname. For new Random births it SHALL retain
native random parent-entry selection, including a distinct birth parent and native weighting when
parents share a surname. Random and Ideology SHALL preserve an already prepared nonempty baby
surname. With none prepared, random fallback SHALL select equally among distinct usable parental
surnames. With no usable parent surname it SHALL retain the existing surname, or a usable generated
surname if none exists. Unusable names SHALL use the failure behavior rather than incomplete output.

#### Scenario: Preferred parent lacks a surname
- **WHEN** Mother is selected, the genetic mother's surname is unavailable and the birth parent and father have usable surnames
- **THEN** the birth parent's surname is selected
- **AND** Father under the equivalent condition instead prefers the father's usable surname

#### Scenario: Native random parent weighting
- **WHEN** three eligible parent entries contain surnames A, A and B for a new Random birth
- **THEN** native parent-entry selection gives A two chances and B one
- **AND** the final automatic naming step keeps that selected surname

#### Scenario: Fallback without a prepared surname
- **WHEN** random fallback is needed and the distinct available parental surnames are A and B
- **THEN** each surname has equal chance regardless of how many parents share it

#### Scenario: Missing parental names
- **WHEN** no parent supplies a usable surname
- **THEN** an existing baby surname is retained, or the generator's usable surname is used if no existing surname is available
- **AND** automation does not replace a usable generated surname with an empty value

### Requirement: Follow supported Ideology marriage-name rules
The feature SHALL use a supported parental marriage-name rule for a new Ideology-policy surname.
The primary parent SHALL be the genetic mother, or birth parent when there is no genetic mother.
When the primary parent and father have different ideologies, each SHALL have 50% chance of supplying
the rule; otherwise prefer primary, father, then birth parent's available ideology. Always-man/woman
rules SHALL use native marriage-role assignment; usually/random rules SHALL use native marriage-name
selection rather than a fixed invented probability. Keep-names, no-change, missing or unsupported
rules SHALL fall back to random distinct parental surnames. No policy SHALL change parents' names,
ideology, marriage, gender or relationships.

For role selection the pair SHALL be the primary parent and father, substituting a distinct birth
parent for a missing or identical father. Native equal-gender ordering SHALL remain effective. A
missing preferred role surname SHALL try the other role then birth parent. Without a usable pair,
man's-name fallback SHALL be father→genetic mother→birth parent; woman's-name fallback SHALL be
genetic mother→birth parent→father. These fallbacks precede the general existing/generated surname rule.

#### Scenario: Different parental ideologies
- **WHEN** a new Ideology surname is needed and the primary parent and father have different ideologies
- **THEN** one rule is selected with equal probability and used once for this baby
- **AND** reading the birth letter does not select another rule

#### Scenario: Usually is not always
- **WHEN** the selected ideology usually prefers a woman's surname
- **THEN** the supported native marriage-name selection determines the outcome
- **AND** the feature does not turn that preference into an unconditional mother's surname

#### Scenario: Same-gender or incomplete parent pair
- **WHEN** the parents share a gender or one parent is missing
- **THEN** native marriage roles or the documented ordered fallback supply the surname
- **AND** no relationship or gender is changed to manufacture a preferred parent

#### Scenario: Unknown marriage-name rule
- **WHEN** a parent's ideology has no supported marriage-name rule
- **THEN** the feature uses the documented random parental fallback without changing that ideology

### Requirement: Keep birth information without a required naming prompt
After successful owned naming the feature SHALL retain the ordinary birth notification and history,
announcing the chosen full name instead of asking for a name by a deadline. Outcome, health, parent,
surrogacy and unrelated provider information SHALL remain intact. Naming SHALL not force the letter
open. Opening it SHALL provide ordinary Jump and Close behavior when applicable, without a required
Name baby or Postpone naming action; unrelated supported actions SHALL remain. Dismissal and archived
viewing SHALL not rename the pawn or duplicate/delete its history entry.

#### Scenario: Read a successful birth notice
- **WHEN** the player opens the notice after a successful automatic name
- **THEN** it announces that name and retains the birth outcome and other information
- **AND** the player can close it or jump to its available target without completing a naming dialog

#### Scenario: Dismiss and revisit history
- **WHEN** the player dismisses the notice and later opens its archive entry
- **THEN** the same birth/name information remains with appropriate native navigation
- **AND** no new name or second birth record is created

#### Scenario: Foreign notification additions
- **WHEN** a supported provider adds unrelated information or an action to the birth notice
- **THEN** it is preserved even if its displayed caption resembles a naming control
- **AND** an unsupported notification shape instead keeps the native event and naming fallback intact

### Requirement: Retain stillbirth, suppressed-letter and adoption boundaries
The feature SHALL name a supported confirmed pending player baby even when the birth results in a
corpse or intentionally emits no letter. It SHALL not invent a letter for a suppressed event or
change the outcome. Ordinary adoption SHALL only trigger naming for a confirmed pending baby after
it becomes player-owned; genuinely named adoptees SHALL keep their name and native notification
behavior. Adoption eligibility and all ordinary adoption effects SHALL remain unchanged.

#### Scenario: Stillbirth
- **WHEN** a supported birth produces a stillborn player baby with a confirmed temporary name
- **THEN** the baby is named and the ordinary stillbirth notice, when emitted, states the name and death outcome
- **AND** no survival, resurrection or extended naming opportunity results

#### Scenario: No-letter birth
- **WHEN** a supported birth deliberately suppresses its notification
- **THEN** the pending baby is named once without creating a replacement notification

#### Scenario: Adopted baby with an existing name
- **WHEN** the player legitimately adopts a baby with a genuine existing name
- **THEN** its name and ordinary adoption letter remain unchanged by this feature

#### Scenario: Adopted pending name
- **WHEN** ordinary adoption transfers a confirmed unnamed baby to the player
- **THEN** the feature names it and updates only the successful-naming part of that adoption notice
- **AND** it does not grant adoption of an otherwise ineligible pawn

### Requirement: Preserve manual naming and restore safely on failure
Automatic naming SHALL preserve ordinary voluntary renaming controls and their existing deadline;
the inspected native newborn window is 60,000 ticks, while stillbirth and expired names retain their
own native limits. It SHALL not provide a permanent first/last-name override. The displayed name and
successful-naming notice SHALL agree, with the prior name/notice preserved until both can change
successfully. A failed attempt SHALL leave the prior name and notice intact, preserve any intervening
chosen/provider name, and leave native naming available under unchanged native rules. No success
announcement or prompt suppression SHALL follow a failed or ambiguous attempt.

#### Scenario: Voluntary change after automatic naming
- **WHEN** the player uses ordinary pawn renaming during its remaining native naming window
- **THEN** the chosen name replaces the generated name and survives later notifications and reloads
- **AND** the feature neither renews that window nor removes native editing restrictions after it expires

#### Scenario: Generator or notification failure
- **WHEN** generation produces no valid name or the supported notification update fails
- **THEN** prior name/notification state is restored for changes made by this feature
- **AND** native naming controls and deadlines remain intact with no false success announcement

#### Scenario: Name changed before assignment
- **WHEN** the name is changed manually or by a provider before automatic naming finishes
- **THEN** the changed name is preserved and the automatic result is abandoned

### Requirement: Use common scoped settings and explicit DLC availability
Surname policy SHALL use the common global default, Use global and Current save controls in mod
settings. New/missing save choices SHALL inherit current globals; stored overrides SHALL remain
dormant while inheriting and resume when inheritance is disabled, with a first override seeded from
the current effective value. The save editor SHALL be inactive while Use global is checked.
Main-menu save controls and mutation actions SHALL be disabled. A
committed policy SHALL affect subsequent naming only. Without Ideology, stored Ideology SHALL remain
stored but resolve to Random parent with an explanation. Without Biotech this feature SHALL be
unavailable without disabling unrelated megamod features. Policy/help/notice text SHALL support all
seven baseline languages; individual names SHALL remain ordinary pawn state, with no new tab or toggle.

#### Scenario: Global changes and save isolation
- **WHEN** a global surname default changes while one save inherits and another overrides
- **THEN** future names in the inheriting save follow the new default and the overriding save keeps its policy
- **AND** already named babies in either save are not renamed

#### Scenario: Main menu and dormant override
- **WHEN** settings are opened without an active game
- **THEN** only global policy can be changed and no previous save is mutated
- **AND** returning to a save restores its inheritance and dormant override choices

#### Scenario: Ideology temporarily absent
- **WHEN** a stored Ideology policy is loaded without that DLC
- **THEN** Random parent is the explained effective policy while the stored Ideology choice remains recoverable

#### Scenario: Biotech absent or invalid policy
- **WHEN** Biotech is absent or a saved policy value is invalid
- **THEN** absent Biotech leaves this feature unavailable and other features usable
- **AND** invalid policy uses the documented Random default with a bounded diagnostic, without altering existing names

### Requirement: Unavailable automatic naming preserves ordinary behavior
The feature SHALL remain available alongside compatible unrelated changes. If required game or
provider behavior is unavailable, both owned automatic naming and its notification changes SHALL
be unavailable together, preserving ordinary native/provider behavior. The startup log SHALL identify
each enabled patch that could not be applied, its failed compatibility check, the unavailable player
behavior and fallback in one bounded warning. Active original Auto Name Babies SHALL suppress the
overlapping owned feature without altering the original provider. Existing names SHALL survive loss
of the owned feature; unrelated features SHALL continue working.

#### Scenario: Compatible patched game
- **WHEN** another compatible mod changes an unrelated aspect of the game
- **THEN** that alone does not prevent the owned feature from activating

#### Scenario: Missing required notification behavior
- **WHEN** required naming or notification behavior is unavailable
- **THEN** neither owned automatic naming nor its notification changes take effect and warnings identify the missing behavior
- **AND** native/provider names, prompts and unrelated features remain available

#### Scenario: Original provider or later loss of support
- **WHEN** the original provider is active or a supported later change invalidates the owned integration
- **THEN** no competing owned naming attempt runs and already assigned names remain
- **AND** the feature reports its availability without changing the foreign provider's patches or state
