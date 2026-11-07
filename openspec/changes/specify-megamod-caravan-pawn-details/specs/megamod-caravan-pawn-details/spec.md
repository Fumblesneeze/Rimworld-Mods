# Caravan pawn details

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Draft observable contract for S116. Nothing implemented or accepted by this document.

## ADDED Requirements

### Requirement: CPD01 Identify pawns in one-way transfer lists
The feature SHALL preserve the ordinary pawn name and add its localized gender and completed
biological age in years in native one-way transfer lists, including caravan formation/reformation,
caravan splitting and transporter loading. Individually named nonhuman pawns SHALL also identify
their species. It SHALL leave items, corpses and ordinary two-way trading presentation unchanged.

#### Scenario: Individual colonist or child
- **WHEN** a one-way transfer list displays a colonist aged 8.8 biological years
- **THEN** the row retains the ordinary name and shows the localized gender and age 8
- **AND** chronological age does not replace biological age.

#### Scenario: Named nonhuman and gender-none
- **WHEN** an individually named nonhuman pawn is listed, including one with no gender
- **THEN** its species accompanies the retained name and its ordinary localized gender description
  and biological age are shown
- **AND** gender-none is not relabeled male or female.

#### Scenario: Other supported transfer windows
- **WHEN** the player reforms or splits a caravan, or opens the ordinary transporter loading list
- **THEN** its eligible pawn rows receive the same details without changing who can be transferred.

#### Scenario: No unrelated label changes
- **WHEN** the player views an item, corpse or ordinary two-way trade row
- **THEN** this feature adds no pawn details to it and does not duplicate details already supplied by
  the game's two-way trading display.

### Requirement: CPD02 Summarize grouped rows truthfully
The feature SHALL describe the available pawns in a native grouped row without changing grouping,
selection or quantities. With a common gender and known ages, it SHALL show that gender and either
one completed age when all agree or the minimum–maximum completed ages. A grouped range SHALL
explain that it covers available row members, not a promised age selection for a partial transfer.

#### Scenario: Same completed age
- **WHEN** a native grouped animal row contains ages 2.1 and 2.9 biological years
- **THEN** the summary shows age 2 once, without splitting the row or changing its quantity.

#### Scenario: Different completed ages
- **WHEN** a native grouped row contains ages 2.9 and 3.1 biological years
- **THEN** the row shows the completed-age range 2–3 rather than only the first animal's age
- **AND** hovering the row explains that the range describes its available members.

#### Scenario: Partial transfer
- **WHEN** the player selects one pawn from a grouped row showing ages 2–3
- **THEN** the summary still describes the row's available members and makes no promise whether the
  native transfer will choose the two-year-old or three-year-old
- **AND** the native selection and transfer rules remain authoritative.

#### Scenario: Missing or incompatible aggregate data
- **WHEN** an otherwise usable row lacks required age/gender data or a provider supplies a mixed
  pawn/item or mixed-gender group that this description cannot summarize reliably
- **THEN** the owned addition leaves its ordinary/provider label intact without fabricated facts
- **AND** it does not prevent native selection or transfer or emit repeated row warnings.

### Requirement: CPD03 Preserve native search ordering and controls
The feature SHALL let the native search and existing Name sort use the enhanced label under their
ordinary matching and ordering rules, without adding a numeric-age sorter or changing other sort
choices. It SHALL preserve transfer controls, information access and native quantity constraints.

#### Scenario: Search the displayed details
- **WHEN** the player enters a query matching the displayed species, localized gender or age text
  under the native search rules
- **THEN** matching pawn rows remain discoverable through that native search
- **AND** clearing the query restores the ordinary list.

#### Scenario: Existing Name sort
- **WHEN** the player selects the existing Name sort
- **THEN** rows follow the game's text ordering of their enhanced labels
- **AND** this is not represented as youngest-first or oldest-first sorting.

#### Scenario: Inspect and transfer
- **WHEN** the player opens a pawn's information card or changes its transfer quantity
- **THEN** the same native control and eligibility rules apply, with no changed transfer limit,
  forced selection, pawn mutation or new interaction required by the label.

### Requirement: CPD04 Keep descriptions readable current and transient
The feature SHALL use the existing transfer-row surface, preserve its control hit areas, and make
the full description readable by hovering when row space is insufficient. Reopening or ordinarily
refreshing the native list SHALL use current membership and pawn facts consistently for display,
search and ordering. The feature SHALL add no settings, tab, operational toggle or persisted state.

#### Scenario: Narrow row and translated text
- **WHEN** long names or translations exceed the row's available label space
- **THEN** transfer and information controls remain usable and hovering exposes the full description
- **AND** native tooltip information is retained alongside any owned group explanation.

#### Scenario: Ordinary refresh or reopen
- **WHEN** a pawn's name, gender, completed biological age or row membership changes and the native
  list is ordinarily refreshed or reopened
- **THEN** its description, native query results and selected sort use the current facts
- **AND** a prior dialog's summary does not leak into another dialog or save.

#### Scenario: Localization
- **WHEN** the player uses English, German, Spanish, French, Simplified Chinese, Russian or Japanese
- **THEN** native gender/species text uses that language and owned group explanations are localized
- **AND** the completed-age meaning remains the same.

#### Scenario: Disable or remove
- **WHEN** the owned display feature becomes inactive
- **THEN** ordinary or selected-provider labels remain and no pawn data, transfer quantities or save
  contents need conversion or restoration.

### Requirement: CPD05 Select one known detail provider
The feature SHALL avoid adding its details when the original Show gender/age provider or an
admitted enabled TinyTweaks equivalent supplies them. It SHALL preserve other compatible label
changes and never alter foreign settings or remove foreign behavior to take ownership. Unknown
overlap state SHALL leave the affected owned addition inactive with a bounded explanation.

#### Scenario: Original provider active
- **WHEN** the original Show gender/age in caravan form dialog provider is active
- **THEN** the owned addition is suppressed and does not append another set of gender/age details
- **AND** the provider's own presentation, including its group-age limitations, remains authoritative.

#### Scenario: TinyTweaks setting changes
- **WHEN** an admitted TinyTweaks provider has its matching detail option disabled
- **THEN** the owned details can appear
- **AND** enabling that option causes a refreshed description to use the provider without an owned
  duplicate; disabling it permits the admitted owned presentation again.

#### Scenario: Unknown overlap or compatible name modifier
- **WHEN** an optional provider's overlap state cannot be established safely
- **THEN** the affected owned addition stays inactive with a bounded reason
- **AND** other compatible modifications to ordinary names are preserved; unrelated modifiers alone
  do not disqualify the feature.

### Requirement: CPD06 Fail safely when the required target contract is unavailable
Every enabled patch for this feature SHALL have a documented minimal target contract checked before
application. Unrelated code changes and compatible other patches SHALL be allowed. A false or
unknown required condition, checking exception or installation failure SHALL leave the affected
owned group inactive, retain native/provider behavior and warn once per skipped enabled patch at
startup. Repeated initialization and supported later changes SHALL remain safe and idempotent.

#### Scenario: Compatible target changes
- **WHEN** unrelated game code changes or another compatible patch preserves the required row,
  label and tooltip behavior
- **THEN** the feature remains eligible without requiring an exact game hash or exclusive ownership.

#### Scenario: Failed contract or partial installation
- **WHEN** a required condition cannot be proved or only part of the owned group installs
- **THEN** the affected group remains inactive and only its newly installed owned patches are removed
- **AND** each skipped enabled patch has one bounded startup warning identifying package, patch,
  target/spec, failed reason, unavailable details and ordinary/provider fallback.

#### Scenario: Later invalidation and repeated initialization
- **WHEN** initialization repeats or a supported later patch/provider change invalidates the contract
- **THEN** no duplicate owned details or patches accumulate, foreign patches remain untouched, and
  affected owned behavior resumes only after successful revalidation.
