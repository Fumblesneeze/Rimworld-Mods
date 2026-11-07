# Pawn equipment and inventory presentation

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
S132 specification only; no runtime behavior or compatibility accepted.

## ADDED Requirements

### Requirement: Inspect all real equipment through interchangeable Gear layouts

GP01. Existing pawn Gear inspection SHALL offer Classic equipment slots, a Simple apparel grid
and a List layout, retaining the actual subject's equipment, apparel and inventory. Slots only
arrange information: they do not create equipment capacity, grant wearable body parts or change
layer conflicts. Each physical item has one actionable entry. Secondary coverage indications for
multi-part apparel are visibly subordinate, do not add quantity and cannot issue duplicate actions.
Extra weapons, unmatched apparel and competing items remain accessible in a fallback list.
Unsupported anatomy/layout information falls back to a complete list rather than losing items.
Animals, corpses and other subjects retain their ordinary supported inspection and control limits.

Classic arranges supported body regions and apparel layers; Simple shows the worn items without
requiring named slot coverage. Empty row/column space is compacted. Show all slots means declared
slots, never invented equipment positions. Background choices distinguish applicable empty slots
from the small ordinary default set. No setting hides occupied items. Malformed/custom layout
information cannot hang inspection or make an actual item inaccessible. Changing screen/UI scale,
layout or selected pawn keeps all controls reachable by fitting/reflowing/scrolling without overlap.

#### Scenario: Multi-part armor and unfamiliar equipment
- **WHEN** a pawn wears one item covering several displayed regions and another item with no recognized slot
- **THEN** the first has one actual entry plus subordinate coverage indications, the second appears in the fallback list, and both remain inspectable without duplicate quantities or extra equipment capacity.

#### Scenario: Colliding or unusable layout
- **WHEN** two items compete for one displayed slot or the subject's layout information cannot be used
- **THEN** every actual item remains accessible once through the layout or fallback list and inspection remains responsive.

#### Scenario: Change layout and viewport
- **WHEN** the player switches Classic, Simple and List, changes UI scale or inspects a different-sized inventory
- **THEN** all actual items and actions remain reachable, the scrollable extent follows the new contents, and the view never changes the pawn's equipment or inventory.

### Requirement: Show condition quality and equipment statuses without obscuring their meaning

GP02. Gear SHALL show current item identity, quantity, details and applicable condition, quality,
mass and equipment status. Quality frames use the same effective palette as other owned quality
displays; Gear has no separate seven-color palette. Condition, forced/locked/tainted indicators,
brawler weapon warning and hauling/supply status remain distinct and readable. An inability to
command the pawn does not by itself erase inspectable statuses. Quality-less items receive no
invented quality; items without meaningful durability receive no invented condition percentage.
Information stays current after wear, transfers, quality changes, language/palette changes and
selection changes, including while paused. Hover details do not hide the condition indicator.

The condition bar is red below20%, yellow from20% to below50%, teal from50% to below100%, and
green at full condition. Background-condition mode instead fills by remaining condition and uses
its worn appearance at or below50%. These visual thresholds do not change tattered thoughts or
durability. Exact HP and neutral readable descriptions remain accessible when colors coincide,
are transparent or cannot be distinguished. Quality color cannot recolor an unrelated warning.

#### Scenario: Boundary condition and hover
- **WHEN** an item is at20%,50% or full condition and the player hovers it
- **THEN** its bar is respectively yellow, teal or green, remains visible, and its tooltip shows actual condition without changing wear or tattered-apparel effects.

#### Scenario: Quality and several statuses coexist
- **WHEN** a quality-bearing item is forced or locked and also marked as hauling cargo or a personal supply
- **THEN** the quality treatment and every applicable status remain distinguishable, with neutral text explaining their meanings and unchanged quantities.

#### Scenario: Paused palette and subject change
- **WHEN** the player changes the effective palette while paused and then selects another pawn
- **THEN** the next Gear display uses the effective palette and that pawn's current items/statuses, without stale colors or retained information from the previous subject.

### Requirement: Preserve native item actions permissions and consequences

GP03. Every owned layout SHALL retain the ordinary applicable item information, forced-apparel,
drop and ingest controls, plus admitted provider actions. The same pawn/item/context must get the
same eligibility, disabled reason, confirmation and result as the current native/provider workflow.
Locked or quest-restricted apparel, incapacity, mental state, being carried, faction/control rules,
destruction-on-drop restrictions and required bandwidth confirmation remain authoritative where
applicable. Viewing a tile or a corpse grants no control. Commands recheck the actual item and pawn
when executed; a changed holder, vanished item or changed eligibility cannot affect a different item.

Dropping worn/equipped gear uses the actual ordinary removal/drop action; ingestion retains normal
eligibility, job, consumption and effects. Force/unforce changes only the selected valid apparel
order, never a global policy or an unrelated pawn. Applicable current native supply-order controls
remain reachable. An unavailable custom action must not be presented as successful. No new instant
equip, duplication, free consumption or automatic policy change follows from this presentation.

#### Scenario: Locked gear and bandwidth confirmation
- **WHEN** the player inspects locked apparel or tries dropping equipment whose ordinary removal requires bandwidth confirmation
- **THEN** locking is respected and the same confirmation precedes any removal, with cancellation leaving equipment and its effects unchanged.

#### Scenario: Real removal and ingestion
- **WHEN** an eligible player orders an apparel drop or ingests a permitted inventory item through Gear
- **THEN** the ordinary job/transfer/consumption path runs with its normal costs and effects, and the view updates to the actual result.

#### Scenario: Stale item menu
- **WHEN** an item's holder or the pawn's ability to perform an action changes after its menu opens
- **THEN** the action is re-evaluated or rejected with an explanation and cannot act on replacement equipment, another pawn or stale inventory.

### Requirement: Present authoritative weight temperature and armor information

GP04. All layouts SHALL expose the same applicable current weight/capacity, comfortable-temperature
range, vacuum resistance and armor information. Unavailable information is distinguished from a
measured zero; zero capacity, missing body parts and unsupported provider values remain readable.
No display changes carrying limits, clothing protection, research or combat behavior.

Individual armor ratings retain their real values, including values above200% under the owned
uncapped-armor behavior. The native overall armor summary retains its coverage-weighted capped
meaning. Individual contributions or layer sums must not be labeled as hit protection probabilities
or treated as that overall summary. An admitted alternative combat system retains its own units,
coverage and calculation; native percentages must not be substituted for unknown provider results.
Dead or otherwise inapplicable subjects do not receive fabricated living-pawn stats.

#### Scenario: Individual rating exceeds overall summary
- **WHEN** a pawn wears apparel rated216% while its native overall summary is lower
- **THEN** item details retain216%, the overall summary retains its ordinary calculation, and the two are clearly distinguished rather than implying immunity.

#### Scenario: Layout parity and unknown capacity
- **WHEN** the player switches layouts for a vacuum-equipped pawn, or inspects a subject with zero or unavailable capacity
- **THEN** applicable values and units remain consistent across layouts, and unavailable or zero-capacity information is shown without a misleading normal-capacity bar.

### Requirement: Compose cargo personal supplies and pawn policies directly

GP05. Gear SHALL expose the same physical inventory, cargo classification, carrying limits and
personal-supply orders used by the owned hauling, visible-cargo, medical, meal and drug-response
features. Supply quantities, effective minimums and contributors retain those features' existing
meaning; Gear does not add their targets together, introduce a second reserve or treat an intended
delivery as already transferred. Their contextual edit/copy/drop/ingest controls remain available
in every layout where the corresponding owned feature is active. Dropping a supply follows its
existing target-adjustment contract; merely viewing or switching layout changes no order.

Pawn supply/loadout/apparel choices remain entity gameplay state. HUD selection and Gear refer to
the same actual pawn. The Spacefarer policy retains its normal editor and manual exclusions; a
presentation choice never assigns an outfit or reverses a policy choice. No duplicate scan results,
private hauling inventory or enlarged capacity are introduced by the display.

#### Scenario: Medicine is also carried for delivery
- **WHEN** a pawn has personal medicine requirements and physically carries additional medicine as haul cargo
- **THEN** Gear distinguishes the relevant quantities/contributors using the existing owned rules, shows actual carried weight, and permits the same contextual orders without counting the physical medicine twice.

#### Scenario: Supply edit survives layout changes
- **WHEN** the player edits a pawn's personal-supply order, switches layouts and reloads that save
- **THEN** the same saved order and actual stock appear in each layout with the owning supply feature's normal effects; no global preference or other pawn is changed.

### Requirement: Keep presentation preferences and current views in their proper places

GP06. Configuration SHALL use ordered common settings under Interface / Gear presentation, with
the shared palette under Interface / Quality colors. Each setting has Global default, Use global
and Current save controls through the common model. New/missing save values inherit current globals;
turning inheritance on retains the dormant override and turning it off restores or seeds it. Use
global remains operable in a loaded save while inheritance disables only the save-value editor.
At the main menu all save-specific controls and callbacks are disabled, globals remain editable.
Edits validate before commit, retain the previous complete value on failure and cannot write a
different/stale save. Saved overrides never alter another save or the global default.

Defaults are Classic layout, no race-specific exception, preferred width706 and height500 UI units,
Show all slots off, applicable empty-slot backgrounds on, ordinary default-slot backgrounds on,
one-line temperatures on, condition bars on, quality frames on and thick frames off. Layout/race
defaults are presentation preferences; per-race choices are one inherited collection, with unlisted
races using the common layout default. Subordinate background/frame preferences remain retained
while irrelevant. Preferred dimensions must be finite and positive, fit within the usable screen
and never make controls unreachable; Tight/Wide fit is an explicit scoped preference action, not
an automatic settings write during display. Valid preference changes apply on the next display.

A compact bottom-right view control, available when Gear is applicable, switches current Classic/
Simple/List and portrait hat visibility and exposes return to configured layout. Current layout
overrides are remembered per race only within that loaded game session; portrait hats start hidden.
These view switches do not write default settings and reset on game change/reload. The portrait
starts facing south and can be rotated as a contextual inspection action without altering the pawn.
Developer equipment overrides belong only in the top debug toolbar and vanish outside developer
mode. No new bottom main tab is added. All controls, unavailable states and tooltips support the
seven required languages, legible text and current UI scale.

#### Scenario: Global inheritance and dormant override
- **WHEN** a loaded save uses global and the player unchecks Use global, edits its layout preference and later toggles inheritance on and off
- **THEN** the checkbox remains usable throughout, the dormant save choice is restored, and other saves and global defaults remain unchanged.

#### Scenario: Main menu and failed commit
- **WHEN** settings are opened without a game, or an attempted save edit becomes invalid or targets a no-longer-current save
- **THEN** save mutation is disabled or rejected, the previous complete value remains intact and the UI explains the failure without affecting global or another save's values.

#### Scenario: Current view and portrait
- **WHEN** the player selects Simple from the bottom-right control, shows a hat in the portrait and rotates the portrait
- **THEN** only inspection changes, entity orders and configured defaults remain unchanged, and loading another game resets the temporary view choices.

#### Scenario: Viewport fit and developer controls
- **WHEN** the player chooses Tight or Wide fit and uses a small viewport, or turns developer mode off
- **THEN** inspection remains fully reachable at the usable size, no preference is rewritten merely by drawing it, and no developer equipment override remains in ordinary Gear or the debug toolbar while developer mode is off.

### Requirement: Preserve admitted provider information and actions with truthful fallback

GP07. Optional integrations SHALL use the actual provider's data and authority. Combat Extended
stats, inventory actions and loadout controls are deferred outside initial support. The source CE
behavior remains research only and SHALL NOT appear as an admitted initial adapter.

For admitted RWoM, show actual enchantment descriptions and the provider's alignment-armor
information without granting enchantments. For Fortified subjects, retain the provider's actual
ordinary equipment-control decision and restrictions without a new species/faction privilege.
When optional information is absent, omit it rather than fabricate values. When essential provider
actions or permissions cannot be preserved, retain that subject's native/provider Gear workflow
and explain the unavailable owned layout. Cosmetic failures do not disable unrelated item actions.
An advertisement or provider name alone is not a compatibility guarantee.

#### Scenario: Magic or controllable artificial pawn
- **WHEN** an admitted enchanted item or Fortified-controlled subject is inspected
- **THEN** actual provider information/control is preserved without creating an enchantment or granting extra control, and ordinary provider restrictions still apply.

#### Scenario: Unrecognized essential provider change
- **WHEN** required provider actions or control semantics cannot be established
- **THEN** its ordinary inspection/actions remain available instead of being replaced by an incomplete owned screen, with a bounded explanation of unavailable owned presentation.

### Requirement: Admit compatible presentation without destructive replacement or identity impersonation

GP08. Each enabled integration SHALL apply only when its documented required behavior/content is
present, allowing unrelated edits and compatible existing patches. Unknown, missing, ambiguous or
failed prerequisites leave the affected contribution inactive without a partial owned replacement.
The startup log gives a bounded warning identifying the skipped contribution, failed reason,
unavailable behavior and fallback. Repeated initialization adds no duplicate tab/slot/action or
warning flood. Unrelated inspector tabs, third-party layout entries and item/content definitions
remain intact; replacing the one intended Gear entry does not replace its containing tab list.

When original or Revamped RPG Inventory is active, its presentation remains authoritative and the
owned overlapping tab/layout contribution is withheld with an explanation. Do not change the mod
list, foreign settings, provider files or another mod's patches. Unknown custom tabs stay intact.
Owned layout extensions preserve declared region/layer matching and empty-space intent, but this
feature does not promise legacy source-type discovery, foreign settings import or multiplayer/
sidearm integration that has not been separately admitted and verified. Removing the owned feature
on a subsequent fresh session restores underlying presentation without converting items or erasing
entity orders. New owned preferences start from defaults/inheritance, not guessed source settings.

#### Scenario: Benign patch and unrelated inspector tab
- **WHEN** compatible other patches or an unrelated inspector-tab entry coexist with the required Gear behavior
- **THEN** owned presentation remains eligible and those contributions are preserved; only the intended Gear entry is substituted where admitted.

#### Scenario: Failed integration or original provider
- **WHEN** required presentation admission fails, installation fails, or an original RPG provider is active
- **THEN** no partial owned replacement remains, the underlying workflow survives, and one bounded startup explanation identifies the unavailable owned behavior without modifying another provider.

#### Scenario: Fresh load after removal
- **WHEN** the game is subsequently started without this owned presentation
- **THEN** remaining native/provider Gear displays the same real items and saved gameplay orders, without needing an owned-item conversion or automatic foreign-settings migration.
