# Description relationships

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
S074 observable draft only; no behavior or compatibility accepted.

## ADDED Requirements

### Requirement: Recipe descriptions connect the declared production and operation facts
DL01. Recipe information SHALL provide two-way description links between a recipe and its declared positive outputs, fixed ingredient types, required research, non-pawn production users, and fixed body parts or body-part groups.
Both single and multiple research prerequisites participate. A fixed ingredient means one allowed
item type in that recipe's default ingredient choice; a category resolving to one type also counts.
A choice among several materials does not add links to every alternative. A player's bill filter
does not rewrite these type-level relationships. Do not infer unpredictable butchery/smelting or
other special outputs, ingredient substitutions, anatomy not declared by the recipe, or pawn races
as workbenches. Research need not be completed to be described; the link grants no availability.

Production users include both recipes naming a non-pawn production building and recipes explicitly
provided by that building. A valid relation appears once even if declared both ways. This deliberately
extends the source's recipe-side-only lookup. Generated recipes obey the same rules as authored ones.

#### Scenario: Follow a production chain
- **WHEN** a recipe has a declared output, a fixed ingredient and two research prerequisites
- **THEN** its description links to all four related entries, and each related description links back to the recipe without claiming it can currently be performed.

#### Scenario: Choice of material and anatomy
- **WHEN** a surgery has a fixed body-part group and a requirement accepting several medicine types
- **THEN** the anatomy and recipe link both ways, while the automatic description links do not list every medicine or every pawn species that could receive surgery.

#### Scenario: Recipe declared on its building
- **WHEN** a production building explicitly offers a recipe that does not name that building itself
- **THEN** the building and recipe descriptions still link both ways, without a duplicate if another declaration later states the same relation.

### Requirement: Object and terrain descriptions expose current fixed costs and research
DL02. Ordinary object types and buildable terrain SHALL link both ways with their declared research prerequisites and currently applicable positive fixed material costs.
Include terrain floors as an explicit extension of the source's object-only pass. Preserve current
difficulty-dependent fixed costs where the game supplies them. Generic selectable Stuff is not a
fixed named ingredient and does not create a link for every wood, leather, textile, stone or metal.
Links describe the type's requirements, not salvage, current stock, an instance's damage or the
materials already consumed to create it. No costs, category memberships or research are changed.

#### Scenario: Floor and fixed resource
- **WHEN** a researched floor requires a particular resource type
- **THEN** the floor, resource and research descriptions expose the appropriate two-way relationships, including before the player has the resource or completes the research.

#### Scenario: Current difficulty changes a fixed cost
- **WHEN** the player changes an applicable difficulty rule from one fixed material requirement to another
- **THEN** the next affected information view reflects the current relationship in both directions, while unrelated original links remain.

#### Scenario: Selectable building material
- **WHEN** a building uses a selectable material category plus a fixed component cost
- **THEN** the automatic links include the component and do not enumerate every selectable material or imply a salvage quantity.

### Requirement: Egg and corpse links preserve their actual direction and species
DL03. A fertilized egg with a declared hatchling species SHALL link to that species and the species description SHALL link back to that egg.
Use the declared living species, not an arbitrary individual pawn or every possible member of a
faction. A nonhuman corpse type links to its declared living species in one direction; this feature
does not add the reverse corpse link or add human-corpse identity links. Do not infer a hatchling for
an unfertilized or otherwise non-hatching egg. Existing independently supplied links remain intact.

#### Scenario: Fertilized and unfertilized eggs
- **WHEN** one egg has a declared hatchling and another has no hatching behavior
- **THEN** the first and its species link both ways, and this feature invents no hatchling relation for the second.

#### Scenario: Nonhuman corpse
- **WHEN** the player opens a nonhuman corpse's information
- **THEN** it links to the corresponding living species without selecting or creating a pawn, and this feature adds no reverse corpse or human-identity relationship.

### Requirement: Automatic relationships add to existing descriptions without replacing them
DL04. Automatic links SHALL appear with ordinary description information while preserving its original text, existing links, their order and contextual destinations.
Append only missing owned relationships, sorted by the current language's displayed destination
label with stable ties. Do not deduplicate different destinations merely because labels match.
Existing material-, pawn- or faction-specific navigation remains distinct from a generic type link.
Do not add self-links or multiple owned links to the same destination; an equivalent existing link
satisfies that relationship. Reopening, rebuilding information or loading a save does not multiply it.
Pre-existing repeated links are not silently deleted as a side effect of adding our relationships.

Missing, removed or malformed owned endpoints are omitted without destroying unrelated information.
Long lists remain reachable through the native scrollable description area, without a silent arbitrary
link cap. Labels/icons and native contextual behavior remain available; there is no new bottom tab,
debug tool, operational toggle, search window or individual relationship setting.

#### Scenario: Existing native product link
- **WHEN** a generated recipe already has its product link and the feature discovers the same relation
- **THEN** that existing link remains once in its original position and only missing owned relationships are appended.

#### Scenario: Same label with different context
- **WHEN** two distinct related types share a translated label and an existing link names a specific material or pawn
- **THEN** distinct destinations remain usable, the contextual link is retained, and neither is collapsed solely because the displayed labels match.

#### Scenario: Many relationships and repeated opening
- **WHEN** the player repeatedly opens an ingredient used by many recipes and scrolls through its description
- **THEN** all valid relationships remain reachable in stable order without accumulating extra owned copies or replacing the description.

### Requirement: Following a link performs ordinary information navigation only
DL05. Activating a description link SHALL use the ordinary information-card navigation for that target, preserving native back history and contextual return.
Generic research links open research information; they do not start a project or promise the separate
research-selection action. Type-level item links use ordinary default material presentation where
needed, without claiming a particular physical item's stats. Native hidden/discovery restrictions,
unknown labels and blocked activation remain; this feature does not reveal a hidden target through
a custom click path. No new broader spoiler policy is inferred for target kinds that the game does
not itself hide. Removed content is never restored to make a link work.

Opening or following links does not create a bill, assign research, issue a job, select an existing
map item, reserve/consume resources, change a filter or edit gameplay settings. Returning to S010/S012's
Add bill view preserves its still-valid recipe/material/comparison choices and pending user intent.
If availability, filtering or the browser context changed while cards were open, apply that browser's
ordinary invalidation rules: explain and clear affected choices, retain only valid choices where
permitted, and reset on a changed bench/map/save. Do not revive old state or silently substitute
another choice. An explicit Find on map action remains a separate feature with its existing
visibility and selection rules.

#### Scenario: Recipe to research and back
- **WHEN** the player follows a recipe's research information link and uses native Back
- **THEN** research information opens and the previous card returns without starting research or creating production work.

#### Scenario: Hidden target
- **WHEN** the ordinary game treats a linked item type as undiscovered
- **THEN** native unknown presentation and blocked activation remain in force rather than exposing it through an owned direct navigation action.

#### Scenario: Return to Add bill
- **WHEN** the player follows several related information cards from the Add bill browser and closes them with the browser context and choices still valid
- **THEN** the browser retains the original recipe, material and comparison choices and has not added a bill.

#### Scenario: Return after a choice becomes unavailable
- **WHEN** a supported refresh removes a selected recipe while its information cards remain open
- **THEN** returning applies the browser's ordinary clearing/comparison rules with an explanation, retains only permitted still-valid choices and neither restores the removed recipe nor adds or silently substitutes a bill.

### Requirement: Relationships reflect current content without becoming map-stock claims
DL06. Owned relationships SHALL reflect the current finalized content and applicable cost rules when an affected information view is next opened or refreshed.
After a supported content refresh, difficulty change, language change or save switch, retain no
owned link to a removed target or obsolete cost relationship. Do not let a previously open card
activate a stale owned endpoint after its content is removed. Rebuild current information or leave
that owned link unavailable with an explanation; the rest of the card remains usable.

Changing map stock does not add or remove type-level recipe relationships. All loaded compatible
content, including generated recipes and eligible modded objects/terrain, participates without a
manually curated mod list. Content exclusions remain authoritative for availability, but excluding
future production is not the same as removing its information: still-present excluded content may
retain its descriptive links and ordinary inspection. Following them cannot re-enable the content,
offer a blocked bill or change its applied exclusion. Truly missing content contributes no owned link.
Invalid isolated relations do not prevent other valid relationships; an unsupported relationship
family leaves that family's ordinary information available without fabricated additions.

#### Scenario: New generated recipe and removed content
- **WHEN** finalized supported content adds a recipe and removes another target
- **THEN** the next affected views show the new valid relations and omit stale owned ones without restoring the removed content.

#### Scenario: Stock disappears
- **WHEN** the last map item of a linked ingredient is consumed
- **THEN** its type-level information link remains, without claiming another item exists or dispatching a restocking job.

#### Scenario: Excluded content still exists
- **WHEN** a content choice blocks new production but an existing item and its information remain
- **THEN** ordinary inspection and descriptive relationships remain available without restoring production, offering an excluded recipe in Add bill or changing the exclusion.

#### Scenario: Change saves and language
- **WHEN** the player switches to a save with different applicable costs and another language
- **THEN** owned links reflect that save's current relationships and labels, without inheriting the previous save's cost state or persisted private link list.

### Requirement: Native information consumers receive the same added relationships
DL07. Supported interfaces that display the ordinary description information SHALL receive the same admitted automatic relationships, including the inspected Info Card Plus description pane.
Preserve the provider's layout, native links, toggles, selection/history and pause behavior. Do not
install a competing information window or require the original Auto links package identity for
integration. If the provider no longer exposes the required description contract, withhold that
unsupported owned enhancement with an explanation while leaving the provider's ordinary view intact.
Provider presence alone is neither proof of compatibility nor a reason to reject it.

No source settings or gameplay data need migration; the source supplies neither. Consumers reading
the source's modified raw definition lists outside the supported description path are not promised
automatic compatibility. A newly demonstrated binding requires its own analysis before a facade is
added. No source assembly, package alias or generic interface-replacement guarantee is selected.

#### Scenario: Info Card Plus shows descriptions differently
- **WHEN** the admitted provider displays a description's links while hiding its body text
- **THEN** the same owned relationships remain available through that provider's normal link rows without an extra window or altered pause preference.

#### Scenario: Changed consumer contract
- **WHEN** an optional information UI stops consuming the supported description contract
- **THEN** its ordinary information remains available and the unsupported owned integration is reported rather than silently claiming complete link coverage there.

### Requirement: Compatibility admission fails with a bounded ordinary fallback
DL08. Each integration SHALL run only when its documented required content and information behavior remain available, allowing unrelated compatible changes.
An unknown required prerequisite, admission exception or installation failure disables the affected
enabled patch or atomic dependency group and logs a bounded startup warning per skipped patch with
its target requirement, reason, unavailable enhancement and fallback. Malformed content diagnostics
are aggregated rather than repeated every repaint. Keep native descriptions/navigation and unrelated
admitted relationship families. Do not keep a partially published two-way owned relation when its
other required endpoint or family cannot safely be provided.

With the original Auto links provider active, it owns overlapping automation and this entire owned
feature remains inactive with an explanation, including the proposed extensions. Do not undo its
links, alter its load choices or remove its patches. Later loss of a required contract stops new
owned augmentation and retires only owned transient information; native/provider information survives.
The feature adds no independent settings or saved orders. Missing optional content is not an error.

#### Scenario: Compatible change and missing required behavior
- **WHEN** another mod changes an unrelated stat, or instead removes required description-link behavior
- **THEN** the unrelated change remains compatible while the missing requirement disables the affected enhancement with a bounded warning and ordinary information fallback.

#### Scenario: Original provider active
- **WHEN** Auto links and megamod are both enabled
- **THEN** only the original supplies this feature's relationships, owned augmentation remains off with an explanation, and the source's existing links are untouched.

#### Scenario: Invalid isolated relationship
- **WHEN** one malformed source reference cannot identify a valid endpoint
- **THEN** that owned relationship is omitted in both directions, valid independent relations remain, and repeated viewing does not produce an unbounded warning stream.
