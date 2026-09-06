## ADDED Requirements

**Owning mod:** Hospitality + Ideology Patch (`fumblesneeze.guestbedgizmo`) at `mods/GuestBedGizmo`.

### Requirement: Product package is small and dependency honest

The mod SHALL ship under the exact player-visible title `Hospitality + Ideology Patch` as a RimWorld 1.6 product package with author `Fumblesneeze`, stable package ID `fumblesneeze.guestbedgizmo`, Harmony as its only hard dependency, and Ideology plus `Orion.Hospitality` as optional load-after activation prerequisites. Its internal assembly/project identity MAY remain `GuestBedGizmo`. Its package MUST contain no Harmony, Hospitality, RimWorld, Unity, test, compiler, host, or Dev Gateway assembly and MUST declare no Dev Gateway dependency or load-order hint.

#### Scenario: Release package is inspected
- **WHEN** the owning project builds in Release and the repository artifact is enumerated by ordinal relative path, size, hash, About metadata, and managed AssemblyRef
- **THEN** only intended product files are present, supported version is exactly 1.6, dependency metadata is honest, and the build has zero warnings or errors

#### Scenario: Stable identity receives the requested public name
- **WHEN** About metadata, Workshop title/copy, and presentation assets are generated for release
- **THEN** every player-visible title is exactly `Hospitality + Ideology Patch` while package ID remains `fumblesneeze.guestbedgizmo`

### Requirement: Workshop copy is concise and behavior specific

The mod SHALL include concise Workshop copy that explains the single unified owner-menu behavior, names Harmony, Ideology, and Hospitality requirements accurately, states that the patch is safe to add or remove during a save game, and makes no unverified compatibility or gameplay claim. The public description MUST omit the internal package ID and implementation-history commentary about Hospitality's legacy toggle.

#### Scenario: Workshop copy is reviewed
- **WHEN** the checked-in title, short description, and full description are compared with the accepted package and live workflow
- **THEN** every player-facing claim maps to observed behavior, the copy says the patch is safe to add or remove during a save game, and it contains no package-ID block, legacy-toggle implementation commentary, settings, saved-state claim, or unsupported version

### Requirement: Steam preview depicts the verified owner menu

The mod SHALL include one deterministic 1164×655 Steam preview card below 1 MiB. The card MUST use the reviewed exact-process in-game source frame and make the ordinary four-choice bed-owner menu the single dominant, page-scale-legible subject without additional promotional title, kicker, supporting copy, or context-scene competition. It MUST pin its ordered manifest, alt text, source-frame hash, crop, colors, output hash, and reviewed package identity.

#### Scenario: Preview is rendered
- **WHEN** the reviewed live source frame and mod-owned manifest are passed through the deterministic presentation build
- **THEN** the output has the exact dimensions, size limit, opaque Steam-background corners, a centered dominant menu with all four native labels readable at page scale, no clipped elements, and no competing promotional copy or context scene

#### Scenario: Preview receives independent visual review
- **WHEN** an independent reviewer receives only the fresh in-game source frames at materially different useful zooms and the unlabeled final card
- **THEN** the reviewer identifies a RimWorld bed ownership/guest-selection purpose and reports no perspective, outline, style, coherence, UI legibility, crop, or zoom defect

### Requirement: Workshop publication uses the universal guarded publisher

The mod SHALL declare a strict universal release profile targeting its retained Workshop item after first publication. The profile MUST distinguish the Steam required-item graph (Harmony and Hospitality Continued) from the required-DLC application graph (Ideology) without making Ideology or Hospitality a RimWorld hard mod dependency. Preparation MUST produce a mutation-free authenticated baseline and immutable candidate/dry-run identity. The user’s explicit update order authorizes the matching prepared plan; its digest and nonce are internal admission proof and MUST NOT introduce a second confirmation prompt. Publication MUST update only the retained Private item, reconcile both dependency graphs, reacquire the subscribed copy, and verify its native workflow before the release is accepted.

#### Scenario: Annotated Private item update is prepared
- **WHEN** the clean reviewed profile is prepared for the retained Private item after the title, copy, preview, and Ideology DLC requirement are corrected
- **THEN** the result names `Hospitality + Ideology Patch`, the same published file ID, Private visibility, exact content/presentation hashes, Workshop required-item graph, required-DLC application graph, digest, nonce, and authenticated remote diff without mutating Steam

#### Scenario: Annotated Private item update is published
- **WHEN** the exact prepared update is published under the standing user order
- **THEN** only the retained Private item is updated, Steam renders the corrected title and simplified page, Ideology appears as required DLC, Harmony and Hospitality remain required items, and the subscribed package's native four-choice workflow is verified before personal evidence acceptance

#### Scenario: Compatibility fix updates the existing public item
- **WHEN** the user requests publication of a verified compatibility fix and the authenticated Steam baseline reports item `3789536584` is already Public
- **THEN** the release profile and prepared update preserve Public visibility, the current title, description, preview and dependency graph; only the tested content, release metadata and specific change note are updated
