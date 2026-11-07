# Armor beyond the shared ceiling

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **RimWorld1.7 beta**.
S075 specification draft only; no gameplay or compatibility accepted.

## ADDED Requirements

### Requirement: Retain ordinary armor ratings above the shared upper ceiling
UA01. Ordinary Sharp, Blunt and Heat armor ratings SHALL retain values above200% when produced by their normal calculations instead of being truncated by the shared inherited ceiling.
Remove that shared ceiling without raising base armor, altering quality/material factors, changing
the zero lower bound or adding a new feature-specific maximum. Ratings below the former ceiling
retain their previous values. A compatible change to the shared ceiling does not require the exact
original200% value to qualify. Additional armor content using the same supported ratings participates
without a list of named apparel mods. Independently specified limits on a particular rating remain
effective; unrelated stats and separate armor systems are not uncapped. This is not support for
infinite, invalid or unrepresentable values.

#### Scenario: High-quality armor exceeds the ceiling
- **WHEN** ordinary quality and other factors give an eligible armor rating of216%
- **THEN** its individual finalized rating is216% rather than200%, with the same item, quality, materials and costs.

#### Scenario: Ordinary lower rating
- **WHEN** the same supported calculations give180%
- **THEN** the rating remains180% with no multiplier or quality promotion from this feature.

#### Scenario: Compatible added armor and separate limit
- **WHEN** added apparel uses the ordinary inherited armor ratings while another rating has an explicit independent limit
- **THEN** the former receives the shared-ceiling change and the separate limit remains effective, without claiming that every armor system is uncapped.

### Requirement: Apply the same rule to apparel and natural armor
UA02. The inherited-ceiling rule SHALL apply wherever the supported armor ratings are used, including worn apparel and a pawn's natural armor, without a faction or colonist-only exception.
Existing items and pawns retain their identities, HP, quality, ownership, coverage and ordinary
equipment restrictions. Research, production, materials, work, availability, prices and movement
penalties do not receive compensating changes. No new equipment or protected body part is created.

#### Scenario: Enemy and animal armor
- **WHEN** a hostile pawn or an animal has a supported natural armor rating above the former ceiling
- **THEN** that rating benefits on the same terms as a colonist's supported armor, without changing its underlying base value or body coverage.

#### Scenario: Existing saved armor
- **WHEN** an existing item is loaded with this admitted feature
- **THEN** normal inspection uses its current uncapped inherited rating while the item, quality, HP and ownership remain unchanged; no replacement item or saved duplicate is created.

### Requirement: Preserve the armor system's hit and penetration behavior
UA03. Armor protection SHALL use the resulting rating through the active supported armor system while preserving that system's ordinary penetration, hit coverage, layer order, wear and damage rules.
Under the native model, subtract penetration before the existing deflection and damage-reduction
rolls; do not clamp the rating back to200% first. Higher rating can improve protection against
penetration but does not protect an uncovered part, stop armor-bypassing damage or grant a general
damage immunity. Armor still wears through its ordinary path, including when the wearer is protected.
Preserve native damage-type conversion and ordinary evaluation of subsequent layers. A supported
external combat provider keeps its own calculation rather than receiving native probabilities.

#### Scenario: Extra rating offsets penetration
- **WHEN** a covered native armor layer rated216% faces30 percentage points of armor penetration
- **THEN** the native armor roll uses186% residual rating rather than the170% that would result from clipping the rating to200% first.

#### Scenario: Coverage and wear remain real
- **WHEN** attacks strike a protected part, an uncovered part and an ordinary armor-bypassing path
- **THEN** each retains its normal coverage/bypass rules, and participating armor can still lose durability through normal wear rather than receiving free immunity.

#### Scenario: Admitted alternative combat calculation
- **WHEN** a supported provider consumes the ordinary armor rating using a different penetration or durability calculation
- **THEN** it receives that rating without this feature replacing its calculation or presenting native protection probabilities as the provider's result.

### Requirement: Keep individual ratings distinct from overall protection summaries
UA04. Ordinary individual armor information SHALL display the resulting current rating, including values above200%, without changing the meaning of the native Gear overall-armor summary.
The native overall summary remains its coverage-weighted, capped metric, not a sum of armor pieces
or a guarantee against a particular attack. It can remain at or below200% while an individual piece
exceeds200%. An integrated inventory UI must preserve that distinction if it offers both values.
Normal-quality recipe comparisons remain at Normal quality and consume current ordinary item stats;
this feature does not add a quality selector, combat forecast or another bottom tab.

#### Scenario: Different individual and overall numbers
- **WHEN** a pawn wears a piece whose individual armor rating exceeds200% and the player opens its information and Gear
- **THEN** the item shows its current rating while the native overall summary retains its existing capped calculation, without treating either as the other metric.

#### Scenario: Comparison remains Normal
- **WHEN** a player compares recipes that could later produce high-quality armor
- **THEN** the existing browser continues comparing its specified Normal-quality values rather than substituting Legendary armor or a summed outfit-protection score.

### Requirement: Admit only compatible inherited content and preserve provider authority
UA05. The change SHALL apply only when its documented required armor content and inherited-rating semantics are present, allowing unrelated compatible edits.
Already-uncapped compatible content is a successful no-op. Missing, malformed or ambiguous required
content leaves the affected contribution unapplied and logs a bounded startup warning naming the
target requirement, failed reason, lost behavior and ordinary fallback. A failed application leaves
no partial owned edit and never erases another provider's unrelated fields or lists. Repeated
initialization does not create duplicate bounds, repeatedly change ratings or spam logs.

When original UnLimitedArmor is active, it owns the behavior and the owned contribution remains
inactive with an explanation. When Combat Extended is active, this contribution is unavailable for
that armor model and does not modify its rules; unrelated megamod features remain usable. Do not
alter either provider's installed files, remove its own patches or change the player's mod list.
Yayo's presence alone neither proves compatibility nor justifies claiming a conflict; support depends
on its actual required rating contract. Separate provider limits remain separate.

#### Scenario: Already satisfied versus missing target
- **WHEN** the required compatible armor content is already uncapped, or instead the required content is missing
- **THEN** the former receives no redundant edit or failure warning, while the latter remains unchanged with a bounded skipped-feature explanation.

#### Scenario: Preserve another contribution
- **WHEN** another mod changes a compatible quality/material factor or adds unrelated stat content
- **THEN** that contribution remains effective and does not alone disable the inherited-ceiling change.

#### Scenario: Original provider or Combat Extended
- **WHEN** the original provider or Combat Extended is enabled
- **THEN** the owned ceiling contribution stays inactive with an explanation, and their behavior and unrelated megamod features remain untouched.

### Requirement: Introduce no private settings or saved combat state
UA06. This feature SHALL add no separate settings, runtime switch, saved rating list or item conversion.
It applies through the admitted content at a fresh load and uses ordinary current stats and native
contextual information. Switching saves cannot carry private derived armor state from the previous
save. No source settings or data need importing. Removing the contribution on a subsequent fresh
load restores the remaining loaded content's rules without rewriting items or other mods' policies.
Any later configuration proposal must use the common settings contract and specify its real effect
timing; it is not silently introduced as a global-only exception here.

#### Scenario: Switch saves and remove the contribution
- **WHEN** the player loads another save, or later starts a fresh session without this owned contribution
- **THEN** that session's loaded stat rules govern existing armor, with no stale private rating cache, save rewrite or fabricated source-state migration.
