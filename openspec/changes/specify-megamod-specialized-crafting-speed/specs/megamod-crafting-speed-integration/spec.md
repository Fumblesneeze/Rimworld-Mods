# Crafting-speed integration

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), RimWorld1.7 beta.
Origin replacement **S122 De-generalize Work**. Specification only; no compatibility accepted.

## ADDED Requirements

### Requirement: Conservative integration is the default

With an admitted Vanilla Skills Expanded provider, **Conservative** SHALL be the built-in mode.
Its art and tailoring recipe speeds SHALL retain their own expertise modifiers and include the
corresponding specialized sculpting or tailoring speed once. Weapon and component production
SHALL retain their respective expertise and include smithing speed once. General-labor effects
SHALL NOT be applied both directly and again through the specialized parent.

#### Scenario: Tailoring expertise and a tailoring trait
- **WHEN** general labor is100%, a specialized tailoring offset is+50%, and a VSE tailoring offset is+50% with no other modifiers
- **THEN** a VSE tailoring recipe uses225% speed in Conservative mode, and a recipe using only the specialized tailoring speed uses150%

#### Scenario: Distinct weapon and component expertise
- **WHEN** a pawn has smithing improvements and component expertise but no weapon expertise
- **THEN** both supported production types receive smithing improvements once, and only component production receives the component expertise bonus

### Requirement: Consolidated art and tailoring share their modifiers

The player SHALL be able to select **Consolidated** for an admitted provider combination. In this
mode, supported art and tailoring recipes SHALL each use a single corresponding speed domain.
Recognized art/tailoring expertise and health modifiers SHALL contribute exactly once alongside
the corresponding specialized modifiers. Unrelated bonuses SHALL remain independent. This mode
intentionally changes modifier composition relative to Conservative and SHALL explain that difference.

#### Scenario: A single tailoring offset domain
- **WHEN** general labor is100%, the specialized tailoring offset is+50%, and a supported VSE tailoring offset is+50% with no other modifiers
- **THEN** eligible tailoring recipes use200% speed in Consolidated mode rather than225%, and the setting explains that modifiers are combined instead of multiplied through separate speeds

#### Scenario: An expertise has several effects
- **WHEN** a supported expertise improves art speed and product quality separately
- **THEN** the art-speed improvement contributes once to consolidated sculpting while its independent quality effect retains the provider's existing rules

### Requirement: Consolidation preserves meaningful existing bindings

Consolidation SHALL retain existing supported art/tailoring stat observations as references to the
same effective consolidated value; they SHALL NOT become missing, stale, independently boosted
or recursively dependent values. Known modifiers SHALL retain their numbers and meaning. If a
modifier, alternate evaluator or conflicting destination cannot be reconciled unambiguously,
the entire optional consolidation SHALL remain inactive, with its reason visible. No unknown
bonus SHALL be silently discarded to force admission.

#### Scenario: A third-party display uses the older speed
- **WHEN** a supported stat display observes the VSE tailoring speed under admitted Consolidated mode
- **THEN** it reports the same value used by eligible tailoring recipes and explains the contributions once

#### Scenario: An unrecognized contribution prevents consolidation
- **WHEN** a loaded provider supplies an art-speed contribution whose combination is unsupported
- **THEN** the optional consolidation is not partially applied, the player's requested mode is retained, and a startup warning and settings status explain the unavailable integration and fallback

### Requirement: Supported bionics retain their separate expertise

With an admitted FSF bionics provider and VSE, bionics crafting SHALL retain its specific expertise
while including smithing speed once. Its expertise progression, multiplier and recipe assignments
SHALL remain the provider's. Supported art/tailoring health effects in Consolidated mode SHALL
move into the same modifier domain without changing unrelated health effects or multiplying a
single contribution twice.

#### Scenario: Bionics specialization
- **WHEN** a pawn has an effective bionics expertise offset+10%, smithing speed150% and no other bionics modifiers
- **THEN** a supported bionics recipe uses165% speed, and the bionics bonus does not improve unrelated weapon or tailoring recipes

#### Scenario: A supported implant affects tailoring
- **WHEN** a supported implant improves tailoring and also improves an unrelated health or work attribute
- **THEN** admitted consolidation includes its tailoring effect once and preserves the unrelated effect

### Requirement: Compatibility choices use shared restart settings

The compatibility mode SHALL appear in the ordered **Production / Specialized crafting** section
of shared mod settings, with clear descriptions, built-in Conservative default, active mode,
pending mode and restart requirement. It SHALL be global-only because startup-wide crafting
definitions are shared by every save in the process. The common settings surface SHALL show
save controls and Use global as inapplicable and disabled with that reason, including at the main
menu. Changing the choice SHALL NOT alter the active game's speeds until restart.

#### Scenario: Change pending while playing
- **WHEN** the player selects Consolidated while Conservative is active
- **THEN** the global pending choice is stored, active production remains Conservative, and the interface clearly requires restart

#### Scenario: Main menu and save switching
- **WHEN** the player opens settings at the main menu or switches between two saves
- **THEN** the same active process-wide mode is shown, the global editor remains available, and neither save can edit or store an independent mode

### Requirement: Missing or invalid options are explicit

Without the optional provider, the mode row SHALL explain that its integration is unavailable
while preserving a previously valid pending choice for a future session. Missing or invalid saved
values SHALL use Conservative, with a bounded diagnostic for invalid values. The interface SHALL
NOT offer the source's nonfunctional Heavy hand mode or silently pretend a failed Consolidated
request is active Conservative. No source settings or saves SHALL be automatically imported or rewritten.

#### Scenario: Provider removed and later restored
- **WHEN** the player starts without VSE after previously selecting Consolidated, then restarts with an admitted VSE setup
- **THEN** the absent-provider session shows no active VSE integration, retains the pending choice, and the restored session attempts that choice subject to normal admission

#### Scenario: Invalid saved choice
- **WHEN** an owned compatibility setting contains an unknown value
- **THEN** the feature uses the Conservative default and reports the invalid choice instead of entering an undocumented mode

### Requirement: Existing modifiers are preserved without identity impersonation

Admitted equipment, traits and displays referring directly to the three specialized speeds SHALL
continue to affect or observe those speeds. A provider that requires the original package to
activate separate content SHALL NOT be treated as active merely because equivalent stats exist.
Unsupported package-gated content SHALL be documented explicitly; the feature SHALL NOT
recreate disabled traits, substitute itself for the original package identity or claim full parity.

#### Scenario: A direct equipment bonus
- **WHEN** admitted equipment supplies a smithing-speed bonus
- **THEN** that bonus improves eligible smithing recipes once and remains visible through the ordinary stat explanation

#### Scenario: A package-gated trait stays unavailable
- **WHEN** Additional Traits withholds a trait because the original De-generalize Work package is absent and no separately admitted adapter supplies that contract
- **THEN** this feature does not silently resurrect the trait, and its replacement coverage identifies the missing package-gated behavior

### Requirement: Original and alternate providers have one coherent owner

When De-generalize Work itself is active, the original SHALL own the overlapping crafting feature;
owned stats, recipe changes and dependent adapters SHALL not be applied again. Other providers
of equivalent specialized speeds SHALL be reused only when the complete required behavior is
compatible. An incompatible or partial collision SHALL disable the affected owned atomic group
with a startup warning, preserving the foreign provider's content and the rest of the megamod.

#### Scenario: Co-installed original
- **WHEN** the original De-generalize Work package is active with the megamod
- **THEN** its crafting implementation runs once, owned overlap is skipped with a clear startup explanation, and unrelated megamod features remain available

#### Scenario: Only one conflicting speed exists
- **WHEN** another provider supplies an incompatible tailoring speed without the other required speeds
- **THEN** the affected owned crafting group is left inactive rather than creating duplicates, overwriting foreign content or leaving a partially remapped recipe set

### Requirement: Startup admission preserves unrelated content

Every enabled patch SHALL apply only when its documented semantic target conditions are true.
Compatible foreign changes SHALL be allowed; unrelated factors, recipe fields and list entries
SHALL remain intact. Missing content, ambiguous mappings, cycles or installation failures SHALL
leave the declared atomic group inactive without partial edits. A bounded startup warning SHALL
identify the patch/spec, reason, unavailable behavior and fallback. Failed optional VSE integration
SHALL leave that provider's recipes/modifiers unchanged while independently admitted base mappings
continue; it SHALL NOT silently substitute another integration mode.

#### Scenario: A compatible provider adds another factor
- **WHEN** a supported speed includes its expected general-labor factor and a separate unrelated factor
- **THEN** the integration replaces only the intended relationship, preserves the unrelated factor once and retains the intended resulting production speed

#### Scenario: A later required edit cannot apply
- **WHEN** one operation needed for the chosen integration fails after its group has begun
- **THEN** no partial owned edits from that group remain, the warning identifies its unavailable behavior, and foreign and independent base behavior remain intact

### Requirement: Information remains on appropriate native surfaces

Stat observations SHALL remain in pawn and recipe information; configuration SHALL remain in
shared mod settings. This feature SHALL add no bottom tab or bottom-right toggle. New headings,
descriptions, status and disabled explanations SHALL support all seven baseline languages.

#### Scenario: Discover the feature without another main tab
- **WHEN** the player inspects a pawn, examines a recipe and opens mod settings in a supported language
- **THEN** the corresponding speed, explanation and compatibility controls appear in those contexts with localized labels and no new main-tab entry
