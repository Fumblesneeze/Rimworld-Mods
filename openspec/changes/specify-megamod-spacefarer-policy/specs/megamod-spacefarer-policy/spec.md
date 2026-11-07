# Dynamic Spacefarer policy

Sole owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S130 replacement; future observable contract only. Odyssey is required for this feature.

## ADDED Requirements

### Requirement: Add qualifying apparel without replacing the policy
The feature SHALL treat an apparel type as a candidate when it declares a finite vacuum-resistance
value greater than zero, either for the apparel itself or as a contribution while worn. Either
positive declaration is sufficient; this is not a calculation of the pawn's net protection. Zero,
negative-only, unavailable or malformed declarations SHALL not qualify; non-apparel SHALL not
qualify. Added content meeting the same rule SHALL be considered without a fixed list of names.
Eligibility SHALL not imply complete vacuum protection, craftability, availability in stock or
permission for a particular pawn to wear the item. An item whose resistance exists only through
an unrecognized conditional/material effect SHALL not be guessed eligible.

#### Scenario: Native and added positive declarations
- **WHEN** loaded native or added apparel has a qualifying positive declaration
- **THEN** it is a candidate even if not already listed as Spacefarer gear
- **AND** this classification makes no promise that it alone protects a pawn in vacuum.

#### Scenario: Zero, invalid or unrelated item
- **WHEN** an item has only zero/negative resistance, invalid resistance, no qualifying declaration or is not apparel
- **THEN** it is not newly included by this feature, without removing any existing player allowance.

### Requirement: Initialize the fresh native Spacefarer policy additively
On a new game with Odyssey, the feature SHALL track the actual newly generated native Spacefarer
policy and include eligible apparel before the player first uses it, subject to the linked-item
safeguards below. It SHALL retain ordinary
starting allowances, including utility apparel, and all other filter rules such as quality,
condition and tainted-apparel restrictions. It SHALL not create a duplicate policy, rename one,
change the default policy or assign it to any pawn. Repeated initialization SHALL not repeat additions.

#### Scenario: Fresh Odyssey game
- **WHEN** the game generates its starting Spacefarer policy with the feature available
- **THEN** eligible apparel is included alongside its ordinary allowances and restrictions
- **AND** the existing pawn assignments, default policy and other apparel policies remain unchanged.

#### Scenario: Empty initial candidate set
- **WHEN** the fresh tracked policy initially has no qualifying added candidates
- **THEN** it keeps its ordinary contents and remains able to consider new qualifying content on a later load.

### Requirement: Add each newly qualifying type once and retain manual exclusions
For the tracked policy, each eligible apparel type SHALL be considered once when first encountered
at new-game initialization, explicit enrollment or a later game load with changed content. Newly
encountered eligible types SHALL be allowed automatically after enrollment when the linked-item
safeguards permit the addition; already considered
types SHALL retain their current allowance. Manually excluding an item or a category SHALL remain
effective for previously considered types across reload. Unrelated filter changes SHALL be preserved.
An excluded type returning after temporary provider removal SHALL retain its history and remain
excluded unless the player permits it again. Previously considered items SHALL not be automatically
removed or rewritten merely because their resistance later changes.

#### Scenario: Exclude after automatic addition
- **WHEN** the player excludes a previously included item and reloads the save
- **THEN** it remains excluded while a genuinely new qualifying apparel type can be added
- **AND** quality, condition and special filter preferences remain unchanged.

#### Scenario: Temporarily remove and restore content
- **WHEN** a previously considered, manually excluded apparel type disappears with its provider and later returns under the same content identity
- **THEN** it is not treated as new and its exclusion is not silently reversed.

#### Scenario: Content changes eligibility
- **WHEN** a previously unconsidered type gains a qualifying declaration on a later load
- **THEN** it can be added once, while already considered types retain the player's current allowance even if their declarations change.

### Requirement: Preserve choices when native item toggles are linked
Existing exclusions and explicit enrollment declines SHALL take precedence over a new automatic
addition when the game links several item types to one filter operation. An addition SHALL not
silently allow a previously considered excluded type, a declined proposal or an unrelated ineligible
type. Its complete affected item set SHALL be shown in the enrollment preview. A proposal that
cannot satisfy those choices SHALL be visibly held back with the reason before confirmation;
confirming the review SHALL commit only the nonconflicting selected additions. The blocked proposal
SHALL remain unconsidered unless the player explicitly declines it, and the conflicting item choices
SHALL remain untouched. Existing exclusions SHALL not become editable merely as a hidden side effect
of enrollment; the player can change them through the ordinary policy editor.

Automatic maintenance SHALL apply the same precedence, adding the nonconflicting candidate set
as a verified transaction and reporting held additions in the policy view with bounded diagnostics.
It SHALL not repeatedly retry a blocked item during redraws; another load or explicit enrollment
review can reconsider it after the underlying conflict changes.

#### Scenario: New parent linked to an excluded child
- **WHEN** allowing a new qualifying parent type would also re-allow a previously considered child that the player excluded
- **THEN** that parent is held back with an explanation, the child's exclusion remains and other independent eligible additions can proceed.

#### Scenario: Decline a linked proposal in enrollment
- **WHEN** the player declines a child type in a preview where a selected parent would also enable that child
- **THEN** the parent is visibly held back before confirmation and neither is silently allowed
- **AND** confirming records the child's explicit decline while leaving the blocked parent available for a later review.

#### Scenario: Linked ineligible content
- **WHEN** an otherwise eligible addition would also enable an unrelated ineligible type
- **THEN** the preview and automatic maintenance hold back that addition instead of broadening its eligibility rule.

### Requirement: Follow policy identity rather than its displayed name
Automatic maintenance SHALL follow exactly one selected policy per game by its identity, surviving
rename, language change and list reordering. Another policy with the same name, a copied filter or
a replacement created after deletion SHALL not inherit automatic maintenance. Deleting the tracked
policy through an allowed native action SHALL stop maintenance without recreating it or choosing
another. Explicitly releasing it SHALL stop additions without changing existing allowances or
discarding its consideration history. Re-enrollment of that same surviving policy SHALL preserve
the history; selecting another policy SHALL require explicit enrollment of that policy.

#### Scenario: Rename and duplicate
- **WHEN** the tracked policy is renamed or the game language changes and another policy has the old name
- **THEN** only the original tracked policy receives later new-type additions.

#### Scenario: Delete and recreate
- **WHEN** the tracked policy is legally deleted and a new policy receives its name or reused native identifier
- **THEN** no automatic tracking transfers to that new policy and no deleted policy is recreated.

#### Scenario: Release and resume
- **WHEN** the player releases a tracked policy and later explicitly resumes it
- **THEN** no changes occur while released and previously considered exclusions remain respected on resumption.

### Requirement: Adopt an existing policy through a reviewable transaction
An existing save without reliable owned tracking history SHALL remain playable without automatic
guessing from policy names or overwriting unknown prior exclusions. The native apparel-policy editor
SHALL offer enrollment for the selected policy, identify any currently tracked policy and preview
the currently disallowed, not-yet-considered eligible types that enabling maintenance would add.
These proposed additions SHALL initially be selected unless held back by the linked-item safeguards;
the player can decline individual entries.
Already allowed eligible types SHALL be recorded without rewriting their allowance. Confirming
SHALL add only the nonconflicting selected proposals, record successfully considered candidates
including explicit declines but excluding held proposals,
and switch the single tracked target. Canceling SHALL change neither policy nor tracking history.
Re-enrollment SHALL preview only new unconsidered types, not reset previous exclusions.

The operation SHALL retain a recoverable pre-change snapshot of the affected filters and tracking,
revalidate the target/candidates on confirmation, apply the change as one transaction and verify
the resulting allowances and identity. Failure or changed preview inputs SHALL leave or restore
the prior state and explain that no complete enrollment occurred. No source-mod history SHALL be
silently imported or assumed to prove the identity of an existing policy.

#### Scenario: Old save with renamed policies and prior exclusions
- **WHEN** the player selects an existing policy and requests automatic vacuum-gear maintenance
- **THEN** the preview identifies that policy and the exact proposed additions without changing it
- **AND** confirming with one proposed item declined preserves that exclusion on later reloads.

#### Scenario: Cancel, stale preview or failure
- **WHEN** enrollment is canceled, the target/content changes before confirmation, or applying/verifying the transaction fails
- **THEN** the prior filters, tracked target and consideration history remain or are restored intact
- **AND** a stale or failed preview does not silently commit only some additions.

### Requirement: Keep policy controls contextual and preserve native wearing behavior
Tracking, release, enrollment and item choices SHALL appear in the existing apparel-policy workflow
as policy-specific gameplay state. They SHALL not be global/save mod settings, add a bottom tab or
use the top debug toolbar. Without an active game the policy mutations SHALL be unavailable.
The UI SHALL distinguish automatic inclusion from complete vacuum protection and show which policy
is tracked. Owned text SHALL support the seven baseline languages. The feature SHALL not equip,
spawn, craft or assign apparel, change stats, or bypass native policy/race/body/quality/condition,
forced-apparel or job restrictions. Removing the feature SHALL leave ordinary policy allowances
and pawn assignments intact, with automatic maintenance no longer occurring.

#### Scenario: Assign the maintained policy normally
- **WHEN** the player assigns the tracked policy through the usual apparel controls
- **THEN** ordinary pawn clothing selection uses its actual allowances and wearing rules
- **AND** the feature does not manufacture gear or force a pawn to wear an otherwise invalid item.

#### Scenario: Open outside an active game or remove the feature
- **WHEN** no game is loaded, or the feature is removed from a save
- **THEN** no implicit policy is mutated in the first case, and existing native policy choices remain usable without new automatic additions in the second.

### Requirement: Maintain safely across unavailable content and incompatible providers
Without Odyssey or its required vacuum content, this feature SHALL be inactive without disrupting
other megamod functions or editing policies. Unexpected target/data incompatibility for an otherwise
enabled integration SHALL produce one bounded startup warning identifying the failed target,
unavailable maintenance and fallback; no partial policy update SHALL be retained. Compatible
unrelated changes SHALL not themselves disable it. Original Dynamic Spacefarer Policy co-installation
SHALL withhold owned overlapping maintenance and explain its inactive controls with a warning,
preserving the original provider without modifying or unpatching it. Missing or corrupt tracking
identity SHALL leave policies unchanged and offer explicit re-enrollment rather than guess a target.

#### Scenario: Required DLC absent or target drifted
- **WHEN** Odyssey is absent or an enabled required integration has changed incompatibly
- **THEN** ordinary policies and other megamod features remain usable, with the latter case reporting a bounded warning and no partial update.

#### Scenario: Original provider or unusable tracking history
- **WHEN** the original provider is active or owned tracking cannot identify its policy reliably
- **THEN** owned automatic maintenance does not guess or apply, and its contextual UI explains the relevant fallback.


### Requirement: Policy maintenance and arrival protection retain distinct authority

S130 SHALL maintain policy allowances only. S004 SHALL use shared apparel information for actual
vacuum-arrival protection, without equating positive declared resistance with a sufficient outfit.
Neither feature SHALL rewrite manual policy exclusions or manufacture gear for existing player-owned
pawns to satisfy the other. S004's supported SOS2 arrival protection SHALL NOT require this Odyssey
policy feature to be active. Contextual enrollment applies to colonies started with megamod; it does
not promise initial support for importing a pre-megamod or original-provider save.

#### Scenario: Candidate apparel provides partial resistance
- **WHEN** an apparel type qualifies for the maintained policy but cannot by itself protect an arriving pawn
- **THEN** it may remain allowed by the policy while S004 still requires an adequate real outfit, with no forced policy edit or free player equipment.
