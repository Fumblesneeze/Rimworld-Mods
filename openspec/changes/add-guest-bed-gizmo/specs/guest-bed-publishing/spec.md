## ADDED Requirements

**Owning mod:** Hospitality + Ideoligy Patch (`fumblesneeze.guestbedgizmo`) at `mods/GuestBedGizmo`.

### Requirement: Product package is small and dependency honest

The mod SHALL ship under the exact player-visible title `Hospitality + Ideoligy Patch` as a RimWorld 1.6 product package with author `Fumblesneeze`, stable package ID `fumblesneeze.guestbedgizmo`, Harmony as its only hard dependency, and Ideology plus `Orion.Hospitality` as optional load-after activation prerequisites. Its internal assembly/project identity MAY remain `GuestBedGizmo`. Its package MUST contain no Harmony, Hospitality, RimWorld, Unity, test, compiler, host, or Dev Gateway assembly and MUST declare no Dev Gateway dependency or load-order hint.

#### Scenario: Release package is inspected
- **WHEN** the owning project builds in Release and the repository artifact is enumerated by ordinal relative path, size, hash, About metadata, and managed AssemblyRef
- **THEN** only intended product files are present, supported version is exactly 1.6, dependency metadata is honest, and the build has zero warnings or errors

#### Scenario: Stable identity receives the requested public name
- **WHEN** About metadata, Workshop title/copy, and presentation assets are generated for release
- **THEN** every player-visible title is exactly `Hospitality + Ideoligy Patch` while package ID remains `fumblesneeze.guestbedgizmo`

### Requirement: Workshop copy is concise and behavior specific

The mod SHALL include concise Workshop copy that explains the single unified owner-menu behavior, names Harmony, Ideology, and Hospitality requirements accurately, and makes no unverified compatibility or gameplay claim.

#### Scenario: Workshop copy is reviewed
- **WHEN** the checked-in title, short description, and full description are compared with the accepted package and live workflow
- **THEN** every player-facing claim maps to observed behavior and the copy does not describe a separate guest toggle, settings, saved state, or unsupported version

### Requirement: Steam preview depicts the verified owner menu

The mod SHALL include one deterministic 1164×655 Steam preview card below 1 MiB. The card MUST use a fresh exact-process in-game source frame that visibly contains the ordinary four-choice bed-owner menu, a short headline, and at most one concise supporting line. It MUST pin its ordered manifest, alt text, copy, source-frame hash, crop, font, colors, output hash, and reviewed package identity.

#### Scenario: Preview is rendered
- **WHEN** the reviewed live source frame and mod-owned manifest are passed through the deterministic presentation build
- **THEN** the output has the exact dimensions, size limit, opaque Steam-background corners, readable page-scale text, no clipped/colliding elements, and a pictured menu that supports the copy

#### Scenario: Preview receives independent visual review
- **WHEN** an independent reviewer receives only the fresh in-game source frames at materially different useful zooms and the unlabeled final card
- **THEN** the reviewer identifies a RimWorld bed ownership/guest-selection purpose and reports no perspective, outline, style, coherence, UI legibility, crop, or zoom defect

### Requirement: First Workshop publication uses the universal guarded publisher

The mod SHALL declare a strict universal release profile with no pre-existing Workshop ID and explicit first-publication opt-in. Preparation MUST produce a mutation-free exact-title owner scan and immutable candidate/dry-run identity; publication MUST wait for explicit digest-and-nonce confirmation, create at most one Private item, persist its ID, disable first-publication opt-in, reacquire the subscribed copy, and verify its native workflow before the release is accepted.

#### Scenario: Private first-publication plan is prepared
- **WHEN** the clean reviewed profile is prepared and the owner publishes no exact-title item
- **THEN** the result names `Hospitality + Ideoligy Patch`, Private visibility, exact content/presentation hashes, dependency graph, digest, nonce, and remote diff without mutating Steam
