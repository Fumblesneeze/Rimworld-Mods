## ADDED Requirements

### Requirement: Package history follows preserved visibility
Package-only preparation, admission and completion SHALL verify history according to the actual preserved page visibility, independently of a pending desired page visibility. A retained exact verified public package receipt SHALL remain valid preceding-package evidence when a later page update makes the item Private.

#### Scenario: Pending visibility change
- **WHEN** a profile requests Private while the existing page is Public, or the reverse
- **THEN** package upload preserves the current visibility and uses that visibility for history verification

#### Scenario: Page makes a public package private
- **WHEN** a verified public package is followed by a Private page update and another package upload
- **THEN** the earlier exact verified package receipt remains usable without requiring a fabricated Private history receipt

### Requirement: Package uploads and page updates are separate actions
The repository tooling `RimWorldModding.Mcp` SHALL expose independent package-upload and single-action page-update operations with immutable action-bound plans and shared exact-item recovery protection.

#### Scenario: Existing package is uploaded
- **WHEN** the agent uploads an existing mod package
- **THEN** only content and its authored change note change, and all page presentation and dependency fields are preserved

#### Scenario: Tested package targets another installed game version
- **WHEN** a previously tested canonical package targets 1.6 and the publisher Gateway runs on supported 1.7
- **THEN** publication stages and validates the exact already-built package without rebuilding, loading or retargeting it; build and gameplay acceptance remain separate prerequisites

#### Scenario: A completed legacy release has stale state
- **WHEN** a legacy `succeeded` state has an exact matching completed receipt proving its item, plan, remote metadata, successful native smoke and restored local package
- **THEN** a new authenticated plan may supersede it without rewriting history; missing receipts, different items and indeterminate states still block mutation

#### Scenario: First package creates a draft
- **WHEN** an explicitly opted-in first package upload has no prior identity and authenticated owner/title preflight finds no matching item
- **THEN** it creates one Private draft, uploads content with the returned ID, backfills the same profile/About identity, and enables subsequent page updates against that ID

#### Scenario: Page is updated once
- **WHEN** a page update is requested for a retained ID
- **THEN** the action submits description, primary preview, slideshow and metadata together, reconciles required relationships, preserves package content, and does not consume the package change note

#### Scenario: Private package follows a page update
- **WHEN** a Private package receipt is followed by a completed page update
- **THEN** the next package retains its verified preceding package change note independently from page completion state

#### Scenario: Recovery follows source edits
- **WHEN** source image inventories change after page submission
- **THEN** recovery verifies retained frozen image inputs without requiring the mutable working files

#### Scenario: Page has no slideshow manifest
- **WHEN** a profile declares a valid description and primary preview but no presentation manifest
- **THEN** its page update uses an empty additional slideshow

### Requirement: Inline images are independent committed GitHub content
Inline image inventory SHALL be separate from Steam slideshow inventory. The agent SHALL commit and push images before page publication and verify the GitHub mirror. Release tools SHALL NOT commit or push images. Immutable direct GitHub content URLs and exact byte hashes SHALL be validated before any Steam page mutation.

#### Scenario: Slideshow order changes
- **WHEN** slideshow images are reordered or removed
- **THEN** inline URLs and description images remain independent and no preliminary Steam image upload is required

#### Scenario: Mirror is missing or stale
- **WHEN** a description image is uncommitted, unavailable at its pinned GitHub commit, or has different bytes
- **THEN** page publication fails before mutating Steam

#### Scenario: Immersive Chefs workflow is verified
- **WHEN** the reviewed tooling uploads Immersive Chefs and updates its existing page
- **THEN** retained evidence proves separate transactions, unchanged content during the page update, working inline images and slideshow, exact item identity, and isolated process cleanup
