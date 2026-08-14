## ADDED Requirements

**Owning mod:** RimWorld Dev Gateway (`fumblesneeze.rimworlddevgateway`) at `mods/RimWorldDevGateway`.

### Requirement: Release manifests are explicit and repository-owned
The repository SHALL define one reviewable release manifest per publishable mod. It SHALL declare the package ID, an existing Steam Workshop item identity or one explicit first-publication bootstrap, authoritative supported RimWorld targets, current development target, required and optional mod relationships, exact Steam build/depot/manifest inputs directly or by committed target ID, per-target package mapping, presentation sources, and required verification profiles. Each uploaded RimWorld compatibility folder SHALL map to exactly one exact compile target; additional exact builds MAY be regression-only and reuse that folder's compiled product. A release command MUST reject undeclared targets, duplicate package IDs or compatibility folders, a development target outside the supported set, invalid dependency metadata, a missing publication identity without the explicit bootstrap, and manifest/source disagreement before downloading, building, launching, or publishing.

#### Scenario: Undeclared target is rejected without side effects
- **WHEN** an operator requests a RimWorld version absent from the selected mod's release manifest
- **THEN** the release command fails before it downloads game content, builds a package, launches RimWorld, or changes a Workshop item

#### Scenario: Two compile targets collide on one package folder
- **WHEN** a release manifest maps two exact compile targets to the same RimWorld compatibility folder
- **THEN** validation rejects the ambiguous payload before either target is built

### Requirement: Compatibility metadata drives every release projection
The per-mod release manifest SHALL be the single authoritative declaration of the mod's compatible RimWorld versions and current development target. The build matrix, generated compatibility-folder names, per-target C# symbols, XML projections, generated `About/About.xml` supported-version metadata, Workshop compatibility copy/tags, and regression matrix MUST be derived from that declaration and MUST NOT independently invent, omit, or retain a version.

#### Scenario: Initial current-only mod declares RimWorld 1.6
- **WHEN** a mod declares RimWorld 1.6 as both its development target and only supported compatibility folder
- **THEN** the release produces only the 1.6 binary/XML projection, lists only 1.6 in generated mod and Workshop metadata, and schedules only declared 1.6 verification

#### Scenario: Removed compatibility target cannot leak into release output
- **WHEN** a version is removed from the mod release manifest
- **THEN** validation rejects any stale generated folder, About entry, Workshop compatibility entry, compile symbol mapping, XML projection, or verification claim for that version

### Requirement: C# compatibility is current-first and legacy-conditional
Each target build SHALL define exactly one RimWorld compatibility symbol derived deterministically from its compatibility folder using a valid C# identifier such as `RIMWORLD1_6`. New feature and domain behavior SHALL remain on the unconditional current-development path. When a newer RimWorld target requires different engine-facing code, version directives SHALL be confined to a neutral RimWorld compatibility source area or exact exceptional files allowlisted with rationale in the mod manifest, and SHALL select only the necessary legacy implementations for older declared targets. The release source MUST NOT contain per-version subfolders or duplicate whole version-specific C# trees.

#### Scenario: A newer RimWorld API breaks the legacy build
- **WHEN** unconditional current-target feature code reaches a narrow engine seam that does not compile against a declared older RimWorld assembly set
- **THEN** the older build selects its declared legacy branch with that target's symbol while the feature/domain code and newer default behavior remain shared

#### Scenario: Every target receives one version symbol
- **WHEN** the release tool compiles all declared compatibility targets
- **THEN** each isolated compiler invocation receives its one matching RimWorld symbol and no symbol for another RimWorld compatibility folder

### Requirement: XML compatibility is generated from canonical sources
Canonical mod XML SHALL remain version-neutral in the ordinary mod source tree. When an older declared RimWorld target needs a different XML shape or value, the per-mod release metadata SHALL select explicit, ordered, XML-aware legacy override operations from a neutral compatibility source location. Targeted parsed operations SHALL be the default; an explicit whole-document replacement MAY be declared with rationale when a demonstrated legacy divergence cannot be maintained safely as targeted operations. The release pipeline SHALL apply overrides only to an ignored per-target projection, validate source selectors and output well-formedness, and record input/operation/output hashes. It MUST NOT use raw textual substitution, overwrite canonical XML, or require checked-in version-specific XML folders.

#### Scenario: Target requires no XML override
- **WHEN** a declared target has no XML compatibility operations
- **THEN** its generated XML projection is semantically identical to the canonical source and records that no override was applied

#### Scenario: Legacy XML shape differs
- **WHEN** the manifest maps an older target to an XML-aware override with the expected selector cardinality
- **THEN** only that target's ignored projection contains the transformed XML and other target projections remain canonical

#### Scenario: Legacy override no longer matches
- **WHEN** an XML override selector matches an unexpected number of nodes after canonical XML changes
- **THEN** that target build fails before packaging and reports the source file, selector, expected cardinality, and observed cardinality

### Requirement: Proprietary game inputs are exact, cached, and uncommitted
The release tooling SHALL acquire the exact declared RimWorld Steam content needed for compilation, verify it against Steam manifest metadata plus local file hashes, and project only the required managed assemblies into an ignored content-addressed cache. It MUST NOT commit, redistribute in a product package, or silently substitute locally installed or newer game assemblies for a declared target.

#### Scenario: Exact assembly cache is reused
- **WHEN** the declared Steam manifest and every required cached assembly hash already match
- **THEN** the tool performs no redundant game-content download and reports the same immutable dependency identity

#### Scenario: Cache corruption fails closed
- **WHEN** any cached managed assembly differs from its recorded hash
- **THEN** the tool quarantines or reacquires that cache entry and does not compile against it

### Requirement: Every supported target is built independently
For each supported RimWorld target, the release tooling SHALL perform a clean compilation against only that target's resolved managed assemblies and SHALL place outputs into that target's generated package folder. Repository source directories MUST remain shared and version-neutral, while version-named source folders, checked-in compiled DLLs, and cross-target output reuse are forbidden.

#### Scenario: Two targets produce an isolated package matrix
- **WHEN** a mod declares two supported RimWorld targets and the release build succeeds
- **THEN** the ignored staged package contains the correct per-target folders and each assembly's evidence names only its matching RimWorld dependency set

### Requirement: Package contents are allowlisted and reproducible
The release staging step SHALL assemble each mod from an explicit allowlist, reject source, symbols, test/Gateway assemblies, caches, credentials, and undeclared files, and emit a manifest of paths, sizes, and SHA-256 hashes. Repeating a build from identical source and declared inputs SHALL either reproduce the staged content or report the exact nondeterministic files.

#### Scenario: Developer assembly cannot enter a gameplay release
- **WHEN** a staged gameplay package contains a Gateway or test assembly
- **THEN** package validation fails before any publication operation is admitted

### Requirement: Distributable releases have complete localization catalogs

Every per-mod release manifest SHALL explicitly classify the mod as distributable or development-only. A distributable RimWorld mod SHALL declare and ship at least `English`, `German`, `Spanish`, `French`, `ChineseSimplified`, and `Russian`. Before a candidate is staged, the release gate SHALL parse its canonical keyed runtime strings, translatable Def source fields, conditional patch-added Def fields, and guarded runtime translation-key inventory; require structurally complete catalogs for every declared language; validate XML, duplicate/stale keys, placeholder parity, and rich-text tag parity; and reject guarded raw player-interface literals. English source Def values MAY supply the canonical English Def text, but all runtime keys require English keyed entries. Directory presence or nonzero translation-file count SHALL NOT satisfy the gate.

Development-only mods MAY explicitly opt out. RimWorld Dev Gateway SHALL be classified development-only and SHALL not be presented as a localized player product. A distributable mod's release evidence SHALL retain catalog hashes and its declared human/agent authorship review; an automated machine-translation service SHALL NOT be invoked by the release pipeline to fill missing entries.

#### Scenario: Product adds one untranslated setting

- **WHEN** a distributable mod adds an English setting key but omits it from German, Spanish, French, Simplified Chinese, or Russian
- **THEN** validation names every missing locale/key and fails before staging or publication

#### Scenario: Translation corrupts a placeholder

- **WHEN** one localized value drops, renames, or duplicates a canonical format placeholder or breaks a rich-text tag pair
- **THEN** the release gate rejects that catalog even though the key exists

#### Scenario: Development Gateway is enumerated

- **WHEN** repository release validation sees `fumblesneeze.rimworlddevgateway`
- **THEN** its explicit development-only classification exempts it from player-language coverage without weakening the rule for distributable product mods

### Requirement: Isolated regression runs can select an exact game build
The scenario and grouped E2E runners SHALL accept a declared RimWorld target, resolve or acquire its complete exact game build into an ignored cache, launch a fresh exact-PID process with isolated savedata and the target package layout, and record the game/depot/manifest/file identity in evidence. They MUST retain all existing configuration-hash, cleanup, player-action, and personal screenshot-inspection acceptance gates.

#### Scenario: Regression executes on a historical target
- **WHEN** an operator selects a cached historical target for an applicable scenario group
- **THEN** the runner launches that exact game build, uses the matching mod binaries, and records observable workflow evidence attributed to both identities

### Requirement: Workshop presentation is separate and generated
The release system SHALL treat in-game `About/About.xml` metadata and the Steam Workshop presentation as separate outputs. It SHALL compile the Workshop description from a versioned text template and structured content, and SHALL render banners and explanatory graphics deterministically from versioned templates, authored text, and repository-owned sprites without overwriting source art.

For each distributable mod, the structured Workshop content SHALL provide a concise overview plus separate complete inventories of implemented player-facing mechanics, portable things, and buildings, with one short behavior description for every entry. It SHALL provide a complete optional-compatibility inventory derived from the release manifest; every listed mod or ecosystem SHALL explain in player-facing language what changes when its exact supported package chain is active. The description SHALL identify the exact supported RimWorld line, distinguish the sole required third-party dependency from optional integrations, and read as a feature showcase rather than a technical design, patch report, or compatibility implementation document. Internal terms such as adapter ownership, package gating, absent safety, implementation seams, and state provenance SHALL NOT appear in player-facing copy when the same behavior can be explained directly.

The page SHALL use reviewed visual headers and explanatory feature images between the major text sections. Each image SHALL directly depict the behavior described by its adjacent heading and copy while retaining asset-led cards that show real shipped sprites. The primary Workshop preview SHALL NOT be repeated inside the description or as an additional preview. Directional shipped sprites used in presentation art SHALL use their reviewed player-facing orientation. Their versioned source templates, authored copy, repository-owned sprites, generated files, remote image identities, dimensions, and hashes SHALL remain reviewable. It SHALL end with a clearly titled `Author's Note` that candidly describes the AI-supervised development experiment, its maintainability/testability/performance-testing focus, the mod's role as a proving ground, its comparatively limited broad playtesting, and the invitation for player feedback. The note SHALL also truthfully identify shipped pre-generated AI-assisted content and whether the mod performs live generation. No heading or authored content may follow the Author's Note.

#### Scenario: Content graphic is rendered from sprites and copy
- **WHEN** a presentation definition names mod sprites, text blocks, and a graphic template
- **THEN** the generated image contains those inputs at the template-defined layout and its source/template/font/hash provenance is recorded

#### Scenario: Feature image explains the adjacent behavior
- **WHEN** a reviewer compares an explanatory image with its adjacent heading and bullets
- **THEN** its subjects depict those claims, real shipped assets remain showcased across the page, and the primary preview is not duplicated inline

#### Scenario: About text cannot silently become Workshop copy
- **WHEN** the in-game About description changes without a corresponding Workshop presentation change
- **THEN** the presentation compiler keeps the distinct Workshop copy and reports both outputs during review

#### Scenario: Immersive Chefs presentation is complete and candid
- **WHEN** the RimWorld 1.6 Immersive Chefs Workshop description is compiled
- **THEN** it uses visual feature sections and approachable player-facing language, includes every implemented mechanic, thing, building, and supported optional ecosystem with concise expected behavior, identifies Harmony as the only required third-party mod, and places its candid `Author's Note` and pre-generated-AI disclosure as the final section

### Requirement: Mod dependencies have one typed source of truth
Each per-mod release manifest SHALL classify every declared mod relationship as required or optional and SHALL record its canonical package ID, display name, presentation text, and Workshop item identity when it is needed for Steam publication. Generated `About/About.xml` SHALL contain every required mod in RimWorld's required dependency metadata. The generated Workshop description SHALL contain explicit Required Mods and Optional Mods sections, including an explicit empty state, and SHALL render the declared identities without maintaining a second hand-authored dependency list. A mod MAY additionally promote a reviewed subset of optional integrations into a player-facing `Recommended Mods` section immediately above `Optional Mods`; those entries SHALL link their Workshop identities when the exact description budget permits, SHALL retain any required package-chain guidance, and SHALL NOT be duplicated in `Optional Mods`. Recommended mods remain optional and MUST NOT be promoted to RimWorld or Steam required dependencies.

#### Scenario: Required mod appears everywhere required
- **WHEN** a release manifest declares a required mod with a package ID and Workshop item ID
- **THEN** the generated About metadata requires that package, the Workshop description lists and links it as required, and the publication candidate declares its Steam item as a required dependency

#### Scenario: Optional integration stays optional
- **WHEN** a release manifest declares an optional integration
- **THEN** the Workshop description lists it under Optional Mods while generated About metadata and the Steam required-item graph do not require it

#### Scenario: Required dependency lacks Steam identity
- **WHEN** a publishable Steam release declares a required mod without a Workshop item ID
- **THEN** validation fails before staging or publication and identifies the incomplete dependency

### Requirement: Presentation outputs are validated before upload
The presentation compiler SHALL validate required fields, BBCode/link policy, generated asset dimensions and file limits, missing or stale sprite references, text overflow, deterministic rendering, and a locally reviewable preview. Publication MUST consume only the reviewed presentation bundle identified by hash.

Each distributable mod MAY additionally declare an ordered gameplay-showcase inventory. Every showcase SHALL name its required active package chain, scene purpose, native player workflow, observable beats, capture crop and final Steam preview order. When a showcase requests both formats, one reviewed in-game screenshot and one reviewed GIF SHALL be produced from the same declared scene family. The screenshot SHALL retain readable gameplay detail. Each action beat SHALL use the closest fixed camera and crop that still includes the acting pawn, manipulated Things, destination and necessary immediate context; a whole-room or whole-colony overview SHALL NOT substitute for a legible action view. The GIF SHALL be no longer than five seconds, SHALL prefer tight hard-cut action views over camera panning, wide compound views, or long walking, and SHALL remain under Steam's one-MiB additional-preview limit. Both SHALL depict believable gameplay rooms and colonists rather than a cleared-map test fixture.

Before arranging a new showcase archetype, the repository workflow SHALL inspect and catalog a varied player-built visual corpus with whole-colony, close-workflow, optional-mod and contrasting examples. Scene rules SHALL identify whether they are mechanical, recurrent across independent colonies, archetype-specific, or unconfirmed candidates. Every arranged showcase SHALL retain a design record containing its brief, eligible reference IDs, applied classified rules, adjacency graph, placement rationale, rejected drafts and live visual observations. Real Ruins evidence SHALL use a reproducible bounded bulk-corpus operation derived from the current open-source client rather than a token handful of blueprints: it SHALL retain the complete metadata cohort, safely parse enough blueprint bodies to support the claimed placement statistics, report failures and unresolved modded Defs, and bind every promoted quantitative rule to its exact sample size and corpus identity. Local caches SHALL be preferred when populated; raw blueprints and downloaded reference images SHALL remain ignored research inputs, and no player blueprint SHALL be republished as release art.

The corpus analyzer SHALL treat every metadata response and compressed blueprint as untrusted input. It SHALL impose explicit request, compressed-byte, expanded-byte, XML depth/cell/item, concurrency and total-operation bounds; prohibit DTDs and external entity resolution; use independently generated local paths; resume without duplicating accepted bodies; and produce stable machine-readable summaries. It SHALL distinguish exact resolved Def contracts from unresolved names. A showcase SHALL NOT be arranged until the corpus has produced measured rules for its relevant geometry and the acting agent has visually inspected a stratified set of matching real layouts.

Gateway-assisted presentation capture SHALL support cropping a rendered frame around projected Things without selecting them, and SHALL support a camera-center crop with pixel width, height and X/Y offset. Capture MUST preserve the caller's selection, camera and visible UI state and MUST NOT add selection brackets, test overlays, debug windows, or direct state mutations to the recorded result. A bounded frame-sequence capture SHALL record source-frame identities and timing so a repository tool can encode a reviewed GIF reproducibly.

When a declared GIF uses hard cuts, every source segment SHALL be a complete capture record from the same exact held process, process-start identity, run and ordered package/build identity. The assembler SHALL consume only verified retained frames, SHALL hash and retain every segment record, SHALL require the ordered union of segment beats to equal the declared showcase beats, and SHALL reject cross-process splicing, unrecorded frames, synthetic transitions and a final duration above five seconds.

#### Scenario: Banner text overflows its template
- **WHEN** generated text exceeds the template's declared safe region
- **THEN** presentation validation fails with the template, field, and overflow bounds and no upload is attempted

#### Scenario: Thing-framed showcase does not select its subjects
- **WHEN** a presentation capture frames a cook, workbench and meal by their current handles
- **THEN** the crop contains their projected visible union while the game's pre-capture selection remains unchanged and no selection brackets appear in the output

#### Scenario: Camera-centered crop composes a fixed scene
- **WHEN** a showcase requests a fixed-width and fixed-height crop with a camera-centered X/Y pixel offset
- **THEN** the Gateway clamps that rectangle to the rendered frame, captures it at end-of-frame without moving the camera, and reports the exact applied rectangle

#### Scenario: Short gameplay GIF is ready for Steam
- **WHEN** a declared showcase records native gameplay beats for motion preview
- **THEN** the encoder uses only retained in-game frames, produces a GIF of at most five seconds and under one MiB, records its frame timing and palette/encoder provenance, and rejects unreadable output

#### Scenario: Hard cuts retain one causal game process
- **WHEN** an order, kitchen and delivery story needs several fixed camera views
- **THEN** each segment comes from the same exact held game process and reviewed package build, the segments preserve the declared beat order, and the final GIF contains only their verified native-game frames

### Requirement: Incremental releases publish meaningful change notes
Every update to an existing Workshop item SHALL provide a nonempty authored change note in player-facing language. The dry-run SHALL show the exact note, bind its UTF-8 bytes into the immutable publication plan, and distinguish it from the previous verified note. The publisher SHALL pass that exact note to Steam's item-update submission and SHALL retain the resulting change-note identity or remotely observed entry in the credential-free receipt. It MUST reject blank, whitespace-only, generic, automatically generated commit/file lists, and unchanged copied notes before mutation.

#### Scenario: Existing Workshop item receives an authored change note
- **WHEN** a reviewed incremental release is confirmed for a retained nonzero Workshop identity
- **THEN** its exact player-facing change note is submitted with the content update, appears in the Steam change history, and is recorded in the publication receipt

#### Scenario: Incremental release reuses a stale note
- **WHEN** the candidate's change note is empty, generic, or identical to the last verified published note
- **THEN** release validation fails before starting a Steam item update and identifies the required authored note

### Requirement: Steam publication is typed, guarded, and observable
The Dev Gateway SHALL expose an authenticated, loopback-only typed publication operation that runs only in a fresh isolated RimWorld process with Steam initialized. It SHALL bind one validated staged package, presentation bundle, and required-item graph to one declared Workshop item, use the native RimWorld/Steam publication path without relying on hidden UI options, expose bounded progress and terminal failure details, and require a separate explicit publish confirmation after dry-run inspection.

#### Scenario: Reviewed bundle is published deliberately
- **WHEN** an operator confirms publication of the exact dry-run package and presentation hashes for the declared Workshop item
- **THEN** the Gateway uploads those exact inputs, reports Steam's terminal result and published item identity, and retains a credential-free publication receipt

#### Scenario: Bundle changes after dry-run
- **WHEN** any staged file or presentation output changes after dry-run inspection
- **THEN** the Gateway rejects confirmation as stale and requires a new validation and review cycle

#### Scenario: Required Steam dependencies are reconciled deliberately
- **WHEN** the reviewed dry-run shows required Workshop items that must be added or stale required-item edges that must be removed and the operator confirms the exact dependency diff
- **THEN** the Gateway reconciles only those declared parent-child relationships, never adds optional mods, and reports each Steam dependency operation and its terminal result

### Requirement: Publication failure is retryable and cannot target another item
The publication workflow SHALL fail closed on Steam authentication, legal-agreement, connectivity, quota, callback, ownership, or item-identity errors. It MUST NOT create a new Workshop item implicitly or change a different item. It MAY create exactly one item when the reviewed manifest explicitly opts into first publication, the mutation-free dry-run declared no item ID, the user confirms that exact create operation, and no prior publication receipt or local item identity exists. Every first publication MUST submit the new item with Steam visibility `Private`, regardless of the eventual release visibility declared for later updates. Making that retained item Friends-only, Unlisted, or Public SHALL require a separate reviewed update after the user has inspected the private item. The returned nonzero identity MUST be persisted before upload continuation and every later release MUST be update-only. The workflow SHALL preserve the reviewed local bundle plus diagnostic state for an explicit retry without claiming rollback of an already accepted Steam update.

One cross-process lease SHALL cover identity recovery, mutation admission, remote reconciliation, dependency reconciliation, subscription, and receipt creation. Before an ID-less first publication, a bounded native query of every item published by the owning account SHALL prove that no exact-title RimWorld item exists; an incomplete query, one exact-title item, or more than one exact-title item SHALL fail before `CreateItem`. Every async Steam call MUST reject an invalid handle and correlate callback parent/child/item identities with the admitted request. A duplicate-create callback carrying one nonzero identity SHALL be treated as an already-created identity and persisted, not as permission for another create. Steam's legal-agreement flag SHALL stop before submission on creation as well as after submission. Definite callback failures MAY be retried from a fresh reviewed invocation; I/O failure, a timeout after admission, or a completed upload whose subscription/smoke/receipt phase did not finish is indeterminate and MUST block a different plan until the exact prior plan is reconciled. Existing receipts participate in identity recovery and conflicting identities fail closed. Remote acceptance SHALL require exact title, owner, app, visibility, description hash, metadata, tags, dependency set, and downloaded preview hash before subscription.

A clean committed descendant release-tool revision MAY reconcile an older immutable plan only when that exact plan has persisted a post-submit state whose nonzero Workshop identity exactly matches the recovered package/release identity. This recovery path MUST remain query/reconciliation-only: it MUST NOT call `CreateItem`, submit content, or admit a different or divergent plan. Its receipt SHALL identify both the immutable candidate source revision and the recovery-tool revision.

#### Scenario: Indeterminate update cannot be skipped by a newer plan
- **WHEN** a submit or dependency operation was admitted but its callback outcome is indeterminate
- **THEN** a release invocation for a different plan fails before mutation and identifies the exact prior plan that must be reconciled

#### Scenario: First publication is private for human review
- **WHEN** an explicitly confirmed ID-less release creates its one permitted Workshop item even though the eventual release manifest declares Public visibility
- **THEN** the created item and first content submission use Private visibility, and Public visibility is possible only through a later reviewed update of the retained nonzero item identity

#### Scenario: A release-tool fix completes an exact submitted plan
- **WHEN** an immutable plan has a persisted post-submit state and nonzero item identity but its original release tool failed during downstream reconciliation
- **THEN** a newer clean committed release tool may query and finish that exact plan without creating or resubmitting content, and the receipt records both revisions

#### Scenario: Concurrent release attempts cannot both mutate Steam
- **WHEN** two processes attempt to publish Immersive Chefs concurrently
- **THEN** exactly one acquires the release lease and the other fails before identity discovery or Steam mutation

#### Scenario: Steam requires a legal agreement
- **WHEN** Steam rejects or withholds publication until the account accepts an updated Workshop agreement
- **THEN** the operation reports the actionable agreement state, changes no alternate item, and leaves the same reviewed bundle available for explicit retry

### Requirement: The subscribed Workshop copy receives final player acceptance
After Steam reports a successful update and CDN propagation, the release workflow SHALL subscribe to or refresh the exact published item through the owning Steam client, reacquire it into the Steam Workshop content area, and compare its complete file manifest with the reviewed staged candidate. It SHALL then launch a fresh isolated RimWorld process using the subscribed Workshop package path rather than a repository-local or manually deployed copy, with the exact required dependencies and no Dev Gateway unless the verification profile explicitly tests it. The acting agent SHALL perform and personally inspect the mod's declared native player-workflow smoke test. Successful upload, remote metadata, subscription state, file presence, startup, logs, or diagnostics alone MUST NOT satisfy this final acceptance.

#### Scenario: Published Immersive Chefs is verified as a subscriber receives it
- **WHEN** the confirmed RimWorld 1.6 Immersive Chefs update reaches Steam
- **THEN** the exact item is subscribed or refreshed, its reacquired files match the reviewed candidate, and a fresh game loads that Workshop copy and passes the declared observable native cooking/dining smoke workflow

#### Scenario: Steam returns stale or different content
- **WHEN** the subscribed Workshop directory is absent, has not reached the published manifest, or differs from the staged candidate
- **THEN** release verification fails without substituting a local package or claiming the release accepted

### Requirement: Release evidence is complete and secret-free
Each release attempt SHALL record source revision and dirty-state policy, release-manifest hash, tool versions, exact game and required/optional mod dependency identities, per-target compile symbols and XML projection provenance, compilation/package manifests, verification results, presentation provenance, operator confirmation, Gateway process identity, Steam item/dependency results, and cleanup outcome. Durable evidence MUST exclude account passwords, Steam Guard codes, session credentials, bearer tokens, and live Gateway discovery files.

#### Scenario: Release receipt is audited
- **WHEN** a publication attempt reaches a terminal state
- **THEN** its evidence connects the exact reviewed source, dependencies, tests, package, presentation, target item, result, and cleanup without containing reusable credentials

### Requirement: Published Workshop identity is repository durable
After a mod receives a nonzero Steam Workshop identity, the distributable source tree SHALL check in `About/PublishedFileId.txt` and the release manifest SHALL declare the same exact value. Candidate staging SHALL require both identities, byte-for-byte package the checked-in About file, and fail before publication when either is absent, zero, malformed, or disagrees with the other, a retained receipt, or the remotely owned item. Later releases SHALL be update-only. A generated-at-publication identity is permitted only during the one explicitly confirmed first-publication transaction and MUST be promoted into the repository before any subsequent release plan is accepted.

#### Scenario: A published mod is cloned to another machine
- **WHEN** a clean checkout prepares an update for an already published mod without access to prior ignored artifacts
- **THEN** the checked-in `About/PublishedFileId.txt` and matching release manifest resolve the existing Workshop item and prevent accidental creation of a second item

#### Scenario: Repository identity is missing or inconsistent
- **WHEN** an existing-item release lacks the checked-in About identity or it differs from the release manifest, receipt, staged candidate, or remote item
- **THEN** release validation fails before any Steam mutation

### Requirement: Release and test tooling is noninteractive
Automated release, package, and regression tests SHALL NOT launch Notepad, a default file association, a browser, or any other unrelated visible desktop application as a dummy process. Process-identity fixtures SHALL use an owned hidden noninteractive child with redirected or suppressed shell UI, bind it by retained process/start identity, and terminate only that exact child in cleanup.

#### Scenario: Process-lease tests run on a developer desktop
- **WHEN** the focused release-tool tests exercise a retained process and PID/start-identity mismatch
- **THEN** no Notepad or other unrelated application window appears and the exact hidden fixture process is cleaned up

### Requirement: Workshop cards composite safely on Steam
Workshop feature-card sources SHALL retain their rounded card composition, but the uploaded raster's outer corner pixels SHALL be deliberately composited to the measured Steam Workshop page background color when Steam displays transparent PNG corners against an opaque white matte. The selected background color and observation date SHALL live in the versioned presentation template, and generated-card tests SHALL require that exact opaque corner color instead of accepting alpha that renders white on the published page. Illustrations inside the card MAY remain transparently composited before the final card render.

#### Scenario: A generated card is viewed on the Workshop page
- **WHEN** Steam renders an uploaded feature card in the description and preview carousel
- **THEN** its rounded outer corners visually blend into the surrounding dark Workshop page rather than appearing as white wedges or a white rectangle
