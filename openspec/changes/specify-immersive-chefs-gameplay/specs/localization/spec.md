## ADDED Requirements

**Owning mod:** Immersive Chefs (`fumblesneeze.immersivechefs`) at `mods/ImmersiveChefs`.

### Requirement: Every player-facing product string is translatable

Immersive Chefs SHALL route every player-facing string through RimWorld's keyed or Def-injected localization system. The inventory includes Def labels and descriptions, job/report strings, recipe work text and semantic ingredient requirements, research and filter text, settings headings/labels/enum values, inspect rows and culinary/thermal bands, alerts and explanations, float-menu actions, player failure messages, poisoning causes, and conditional compatibility content. Internal identifiers, package IDs, developer diagnostics, exception detail, and log-only messages MAY remain invariant English. RimWorld Dev Gateway is a development-only mod and is explicitly outside this product-localization contract.

#### Scenario: A runtime screen is audited

- **WHEN** settings, inspect panes, alerts, float menus, work reports, and player messages are rendered in a non-English language
- **THEN** no Immersive Chefs user-facing literal bypasses a translation key and no missing-key marker or unintended English fallback appears

### Requirement: Six complete context-authored languages ship

The package SHALL provide complete English, German, Spanish, French, Simplified Chinese, and Russian coverage using RimWorld folder names `English`, `German`, `Spanish`, `French`, `ChineseSimplified`, and `Russian`. English source Def values and English keyed entries form the canonical meaning. The five translated catalogs SHALL be written by the implementing agent from the gameplay context, not produced by a machine-translation service, and SHALL preserve placeholders, numeric units, rich-text tags, proper names, tone, and the distinction between cookware, tableware, cutlery, sanitation, culinary quality, food poisoning, and toxic buildup. Identical proper names and technical tokens MAY remain unchanged where translation would be incorrect.

#### Scenario: Compare required catalogs

- **WHEN** the six packaged language catalogs are compared
- **THEN** every canonical keyed entry and every translatable Immersive Chefs Def field has one valid entry in every required locale, with matching placeholders and tags

#### Scenario: Agent authors a contextual term

- **WHEN** a term such as `cookware set`, `dirty-ware fallback`, or `culinary quality` appears in several screens
- **THEN** each language uses one contextually appropriate, internally consistent translation rather than an unreviewed literal machine substitution

### Requirement: Distributable-mod release checks reject localization drift

The repository's release/package gate SHALL explicitly classify distributable RimWorld mods and SHALL exempt development-only tools such as `fumblesneeze.rimworlddevgateway`. For every distributable mod, the gate SHALL parse package source/output XML, derive the canonical keyed and translatable Def-field inventories, require all six language catalogs, validate XML plus placeholder/rich-text parity, and reject missing, duplicate, malformed, stale, or raw guarded user-interface strings. A directory's presence or a nonzero file count is insufficient. Immersive Chefs SHALL pass this gate before a release candidate can be accepted.

#### Scenario: A new setting is added only in English

- **WHEN** a distributable mod adds a keyed setting label without adding the corresponding five translated entries
- **THEN** the release/package gate fails and identifies the missing key per language

#### Scenario: Gateway remains development-only

- **WHEN** the same gate enumerates repository mod projects
- **THEN** it requires an explicit distributable/development-only classification and does not demand player localization from RimWorld Dev Gateway

### Requirement: Required languages receive live rendering checks

Final localization acceptance SHALL use the reviewed packaged build in fresh isolated RimWorld processes. The acting agent SHALL select each required language through the native language setting or an equivalent isolated launch configuration and personally inspect representative native settings, Def/info-card, job/report, alert, and runtime action text. Static coverage tests remain necessary but SHALL NOT substitute for observing the localized strings rendered by RimWorld.

#### Scenario: German through Russian are inspected in game

- **WHEN** the final reviewed package is opened under English, German, Spanish, French, Simplified Chinese, and Russian
- **THEN** representative Immersive Chefs text renders in each language without missing keys, tofu, broken placeholders/tags, clipping that destroys meaning, or unintended fallback text
