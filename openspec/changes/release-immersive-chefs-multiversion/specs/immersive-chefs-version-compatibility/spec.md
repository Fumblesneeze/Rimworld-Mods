## ADDED Requirements

### Requirement: Immersive Chefs supports two verified runtimes
Immersive Chefs SHALL ship independently compiled 1.6 and 1.7 payloads from shared sources, preserving existing kitchenware, cooking, dining and cleaning behavior and requiring no Dev Gateway product dependency.

#### Scenario: Colonists use the kitchen on either target
- **WHEN** a player orders cooking and dining using the version-selected package in each exact supported runtime
- **THEN** pawns perform native jobs with the expected kitchenware/utensil effects and observable cleaning lifecycle, proven by personally inspected action/result evidence

#### Scenario: Player installs the combined Workshop item
- **WHEN** RimWorld selects the declared version folder
- **THEN** only that runtime's product assembly loads and About/Workshop metadata truthfully lists the two verified versions
