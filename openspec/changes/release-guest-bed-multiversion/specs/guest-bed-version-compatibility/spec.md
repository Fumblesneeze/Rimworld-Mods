## ADDED Requirements

### Requirement: One package supports both major/minor targets
Hospitality + Ideology Patch (`fumblesneeze.guestbedgizmo`) SHALL ship independently compiled 1.6 and 1.7 assemblies from shared source in one positive-allowlist package. Its supported-version metadata SHALL match those targets and no game pin beyond major/minor SHALL be accepted.

#### Scenario: The combined package is built
- **WHEN** the release matrix builds from clean source against the owned major/minor caches
- **THEN** both target DLLs and matching About metadata are included without game, Harmony, Hospitality, Gateway or test assemblies

### Requirement: Both targets preserve native bed ownership
The mod SHALL retain its four-choice owner menu and upstream conversion/confirmation behavior on each declared target, with no partial integration when activation prerequisites are absent or consumed interfaces are incompatible.

#### Scenario: Hospitality and Ideology are active
- **WHEN** the player opens the owner menu, converts selected beds to guests, cancels an owner-removal warning and then accepts a native prisoner conversion
- **THEN** all four choices are visible, guest rental controls remain, cancellation leaves beds unchanged and accepted conversion includes the correct room siblings

#### Scenario: Owner-command eligibility and room restrictions apply
- **WHEN** the player attempts prisoner conversion outdoors or includes an ineligible bed in a mixed selection
- **THEN** native rejection and eligibility filters preserve unsupported beds without duplicate or orphan replacements

#### Scenario: An activation prerequisite is absent
- **WHEN** the patch runs without Hospitality or without Ideology
- **THEN** the ordinary native bed controls remain usable and no guest-aware product command or partial patch is installed

### Requirement: Publish only the accepted combined package
The mod SHALL update its retained Public Workshop item through separate package-upload and page-update actions after reviewed native acceptance on both targets.

#### Scenario: The update is released
- **WHEN** both native target gates and package checks pass
- **THEN** one combined package is uploaded with an authored change note, the separate page update advertises 1.6 and 1.7 beta, unchanged content is verified, and publication remains at the main menu with isolated cleanup
