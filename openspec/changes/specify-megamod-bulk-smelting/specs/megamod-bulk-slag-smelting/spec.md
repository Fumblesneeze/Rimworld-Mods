# Bulk slag smelting

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Origin: replacement of Bulk Smelting (Continued), S015 / P1-12. This is an observable contract for
later implementation. Output parity with piecewise smelting is user-selected; same-kind batches remain the initial scope;
BS-Q1–4 in the refinement journal retain outstanding choices and verification work.

## ADDED Requirements

### Requirement: Additional contextual smelting recipe
The mod SHALL offer one three-chunk slag recipe at the ordinary electric smelter alongside its other recipes when this feature's support checks succeed.
The entry shall be clearly distinguishable from single-chunk smelting. It shall use the existing
Add bill and bill-configuration surfaces, including the integrated browser when available, without
adding a bottom tab, global toggle, cheat command or separate settings window. It shall not add an
extra research requirement or change how the electric smelter becomes available.

#### Scenario: Choose a batch at a smelter
- **WHEN** the player opens Add bill at a supported electric smelter
- **THEN** the three-chunk recipe is available as one entry beside ordinary single-chunk smelting
- **AND** choosing it adds an ordinary configurable production bill.

#### Scenario: Unrelated workstation
- **WHEN** the player opens a workstation that has no independent support for this recipe
- **THEN** this feature does not add slag smelting to that workstation.

#### Scenario: Accessible translated presentation
- **WHEN** the player inspects the recipe or its work in any of the seven baseline languages
- **THEN** its label, explanation and job text identify the three-chunk batch without raw translation keys
- **AND** the controls remain on the contextual production surface.

### Requirement: Three permitted chunks of one kind
The recipe SHALL require three steel-slag chunks or three mechanoid-slag chunks, all of the same kind, within the bill's permitted ingredients.
It shall not accept stone chunks, weapons, apparel or arbitrary smeltable items. The player can
restrict either allowed slag kind through ordinary bill controls. Native access, ingredient radius,
forbidden status, reservations and physical availability remain applicable; an availability display
does not reserve chunks or authorize an otherwise invalid job.

#### Scenario: Steel-slag batch
- **WHEN** a worker starts the bill with three permitted, accessible steel-slag chunks
- **THEN** the batch uses those three physical chunks and requires no mechanoid slag.

#### Scenario: Mechanoid-slag batch
- **WHEN** a worker starts the bill with three permitted, accessible mechanoid-slag chunks
- **THEN** the batch uses those three physical chunks and requires no steel slag.

#### Scenario: Insufficient homogeneous supply
- **WHEN** only two steel-slag chunks and one mechanoid-slag chunk are available for the bill
- **THEN** the bill cannot start a batch from that supply
- **AND** it does not consume a partial batch or substitute an unrelated item.

#### Scenario: Bill restriction excludes the available kind
- **WHEN** the bill permits only steel slag but the available three chunks are mechanoid slag
- **THEN** the worker does not start that bill using the excluded chunks.

### Requirement: Explicit paid work and output
A completed batch SHALL consume three permitted same-kind chunks and produce exactly the combined products and quantities of smelting those same chunks individually under the current admitted ordinary recipe rules.
The inspected baseline is **45 steel for 1100 base work**, adjusted by ordinary smelting work speed;
three ordinary singles produce45 steel for1200 work. This is an efficiency improvement only, with
no extra yield. Keep the1100 base-work choice explicit; output changes do not silently change work.
The feature adds no quality roll, skill minimum, extra experience or production outside actual
completed work. Native power, operating conditions and output delivery remain effective.
If the ordinary outcomes cannot be established or represented faithfully for both permitted kinds,
withhold the owned bulk recipe with a bounded startup warning instead of guessing products.

#### Scenario: Complete the physical baseline batch
- **WHEN** a worker collects three eligible chunks and completes ordinary bulk work under the inspected baseline
- **THEN** the three chunks are consumed and45 steel is physically produced for1100 base work
- **AND** output follows the bill's ordinary delivery choice.

#### Scenario: Supported ordinary output changes
- **WHEN** an admitted change makes each corresponding single operation produce20 steel
- **THEN** the bulk operation produces60 steel for the same three chunks, with no additional yield multiplier.

#### Scenario: Unsupported output relationship
- **WHEN** permitted chunk kinds have ordinary outcomes that the one bulk recipe cannot faithfully represent
- **THEN** the owned bulk addition stays inactive with a startup warning, leaving ordinary single smelting available.

#### Scenario: Smelter cannot operate
- **WHEN** the smelter cannot operate under ordinary rules
- **THEN** the feature does not bypass that restriction or produce output immediately.

### Requirement: Ordinary bill counting and control
The recipe SHALL use ordinary repeat, target-stock, suspension, worker, ingredient and delivery controls, with one completed batch counting as one execution.
Target-stock counts shall measure steel items in the bill's selected counting scope, not batches or
chunks. The full batch is produced when a target bill starts below its target, so ordinary overshoot
is allowed. Whole-list linked workbenches shall use their shared order and counting rules; this feature
shall not create separate per-bench quotas or a new counting cache.

#### Scenario: Two repetitions
- **WHEN** a finite bill for two batches completes twice with sufficient permitted ingredients
- **THEN** it has consumed six chunks, produced 90 steel and fulfilled two executions
- **AND** it does not start a third execution from that finite order.

#### Scenario: Target reached partway through a batch quantity
- **WHEN** a bill targeting 100 steel counts 90 steel in its chosen scope and completes one batch with no concurrent stock changes
- **THEN** its counted stock becomes 135 steel if the output remains within that scope
- **AND** the next execution is not started while its ordinary target conditions remain satisfied.

#### Scenario: Shared finite order
- **WHEN** two linked electric smelters share a finite order of two bulk batches
- **THEN** they can fulfill two batches in total under the whole-list linking rules
- **AND** each smelter does not independently receive two additional batches.

#### Scenario: Suspend or interrupt ordinary work
- **WHEN** the player suspends the bill or its ordinary work is interrupted
- **THEN** native suspension and interruption rules apply without an invented unfinished slag product
- **AND** this feature does not grant an extra batch, refund or completed repetition.

### Requirement: Preserve existing content
Adding this recipe SHALL preserve existing single-chunk smelting, other recipes and other mods' unrelated list entries, ingredient categories and filters.
It shall not replace the smelter's recipe list or broaden an existing recipe's inputs. Changes to
unrelated labels, ordering, recipes or ordinary single-recipe balance shall not alone reject the bulk
addition when its own prerequisites remain true. Bulk outputs follow the admitted ordinary single-operation outcomes; its1100 base work remains
the explicit initial efficiency choice. Unknown output semantics disable the bulk addition.

#### Scenario: Another mod adds a smelter recipe
- **WHEN** another compatible mod adds its own recipe to the electric smelter
- **THEN** the extra recipe, the existing vanilla recipes and the bulk entry all remain available.

#### Scenario: Single-recipe rebalance
- **WHEN** another mod changes single-chunk smelting's output or work without breaking bulk prerequisites
- **THEN** that single recipe retains the other mod's change
- **AND** the bulk entry follows the admitted piecewise outputs without overwriting the single recipe, or is withheld if faithful output cannot be established.

### Requirement: Saved ordinary bills retain their identity
The admitted recipe SHALL retain the stable identity needed by ordinary saved bills created with our recipe, preserving their saved bill choices and remaining repetition counts on reload.
No new custom save record, automatic active-mod-list edit or foreign settings import is introduced.
Continuity applies when the supported recipe and its required content are available. It does not
promise repair of already missing references or continuity after the recipe is unavailable.

#### Scenario: Reload a configured bulk bill
- **WHEN** the player saves and reloads a supported game with a configured bulk bill
- **THEN** its allowed slag kinds, repeat/target choice, remaining count, suspension and delivery choice persist
- **AND** no duplicate bill or completed batch is created by loading.

#### Scenario: Existing original-mod colony
- **WHEN** a colony predating megamod contains the original provider's bills
- **THEN** initial support does not promise importing that colony; matching recipe identity alone is not a migration guarantee.

### Requirement: Guarded support and bounded startup diagnostics
Every enabled addition SHALL check its documented content prerequisites before taking effect and leave the affected addition inactive when a required target is missing, ambiguous or incompatible.
For this recipe the electric smelter, steel output, both supported slag kinds and the required ordinary
smelting facilities must support the specified behavior. A failed addition shall not leave a partial
recipe or remove unrelated content. One bounded startup warning per skipped enabled patch shall
identify the feature, patch/target specification, failed prerequisite, unavailable behavior and
fallback. Checks shall tolerate compatible unrelated modifications; warnings shall not repeat per
tick, bill inspection or job search. Saved bills that cannot resolve because support is unavailable
are outside the continuity guarantee and shall not be represented as successfully preserved.

#### Scenario: Missing required slag content
- **WHEN** required slag content is absent at startup
- **THEN** the owned bulk recipe is not added with a broken ingredient reference
- **AND** the startup warning explains that bulk smelting is unavailable while existing content is left intact.

#### Scenario: Changed required support
- **WHEN** a required target no longer supports the documented recipe behavior or the check fails unexpectedly
- **THEN** the affected addition remains inactive and reports the reason once
- **AND** unrelated megamod features can continue.

#### Scenario: Repeated initialization or display
- **WHEN** support is checked again or the player repeatedly opens Add bill during the same startup session
- **THEN** at most one owned recipe entry is present
- **AND** the same skipped-patch warning is not repeatedly emitted.

### Requirement: One provider and honest external compatibility
The mod SHALL withhold its bulk recipe when either known original provider is active or another provider already supplies the stable recipe identity.
The existing provider's recipe and balance shall remain untouched, and a bounded startup diagnostic
shall explain which owned overlap was disabled. Merely being installed but inactive shall not select
that provider. The mod shall not impersonate an upstream package or automatically disable another
mod. A matching recipe name alone shall not be claimed as proof of complete external compatibility.

#### Scenario: Original provider active
- **WHEN** Bulk Smelting (Continued) or its known predecessor is active
- **THEN** our overlapping addition is withheld with one explanatory startup diagnostic
- **AND** this feature neither adds a second recipe nor rewrites the source's content.

#### Scenario: Unknown recipe-identity collision
- **WHEN** another provider already supplies the recipe identity
- **THEN** our addition is skipped with a collision warning and the existing recipe is left untouched.

#### Scenario: Source files installed but inactive
- **WHEN** the source exists on disk but is not active and no recipe identity collides
- **THEN** that installed copy alone does not prevent our supported addition.
