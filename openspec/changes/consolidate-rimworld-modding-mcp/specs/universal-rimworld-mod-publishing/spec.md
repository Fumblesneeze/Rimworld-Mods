## ADDED Requirements
**Owning mod:** RimWorld Dev Gateway (`fumblesneeze.rimworlddevgateway`) at `mods/RimWorldDevGateway`.

### Requirement: Every distributable mod uses one strict release profile
The universal publisher SHALL consume a strict `mods/<ModName>/Release/release.json` profile selected by path or canonical package ID and MUST contain no product-mod name or Workshop identity in executable code. The profile SHALL declare stable project/package/About identity, distribution class, game target, positive package allowlist, presentation inputs, Workshop metadata, dependency relationships, Steam required-item IDs, Steam required-DLC/application IDs, expected Steam owner, current published item ID or explicit first-publication opt-in, and native verification profiles. RimWorld hard dependencies SHALL be validated from the project-generated About metadata independently from both Steam relationship graphs: every hard Workshop dependency MUST appear in the Steam required-item graph, while optional RimWorld load-after integrations and DLC activation prerequisites MAY be Steam required items/applications without becoming About hard dependencies. Unknown fields, duplicate package IDs, identity mismatches, arbitrary command strings, escaping paths, or an absent item ID without opt-in MUST fail before side effects.

The Steam required-item graph MAY be empty for a self-contained mod; publishing content MUST NOT require a placeholder dependency ID.

#### Scenario: Two mods prepare through the same publisher
- **WHEN** callers prepare valid profiles for two different distributable mods
- **THEN** the same operation and implementation validates each profile and derives different candidates entirely from declared data

### Requirement: Candidate preparation is clean, positive, and immutable
`release_prepare` SHALL require a clean committed revision, validate the exact pinned Steam depot/RimWorld runtime/managed-assembly identities, run registered build/presentation/package operations, stage only allowlisted package files without traversing file or directory reparse points, validate About/project/profile/dependency identity and localization policies, and hash sorted content plus every presentation input into one candidate digest. The staged package MUST contain exactly its declared product assembly and MUST reject RimWorld, Unity, Harmony, compiler, Gateway, optional-mod, host, test, or undeclared assemblies and AssemblyRefs. Platform/game references, declared Harmony, and explicit profile hard-runtime references are the only admitted AssemblyRefs; optional integrations MUST remain absent-safe and MUST NOT use that hard-reference declaration. Dirty/uncommitted source MAY be built only through a separate non-publishable local mode and MUST NOT receive a publish nonce.

#### Scenario: Dirty candidate cannot reach Steam
- **WHEN** tracked or untracked release-scope bytes differ from the committed candidate revision
- **THEN** preparation refuses a publishable candidate and no Steam lookup, create, or update begins

### Requirement: Dry-run admission binds the exact remote mutation
Preparation SHALL perform an authenticated mutation-free Steam owner/title/identity query and emit a canonical dry-run containing candidate digest, source revision, presentation hashes, target item or exact first-publication title scan, visibility, tags, required-item graph, required-DLC/application graph, exact remote metadata/content/preview/dependency baseline and diff, a specific player-facing change note, nonce, and expiry. Existing public or unlisted items MUST verify the pinned previous note against Workshop change history; existing private items MUST bind the pinned previous note to the exact retained publisher worker result, successful receipt, item/package/plan identity, and complete-reviewed durable state from the preceding release. The private predecessor result and receipt SHALL be copied into the immutable plan inputs with exact source and frozen hashes, and their canonical successful state SHALL be revalidated immediately before irreversible dispatch. An explicit publication order SHALL authorize the exact subsequently prepared plan when its title, target item, visibility, both dependency graphs, and other material mutation match that order. Publication MUST require the exact digest and nonce as internal admission proof, MUST NOT require a second user confirmation in a later turn for that matching plan, and MUST reject changed bytes, state, identity, owner, remote baseline, subscriber manifest, or expired admission. It SHALL repeat those validations after any bounded startup/preflight delay and immediately before irreversible dispatch.

#### Scenario: First publication preparation continues under the standing order
- **WHEN** an opted-in profile has no item ID and the exact-title owner scan finds no item
- **THEN** the operation returns a Private first-publication plan and nonce without creating an item, and a caller already executing an explicit matching publish order proceeds directly to publication without another user prompt

#### Scenario: Candidate changes after preparation
- **WHEN** any staged, description, preview, dependency, or identity byte changes after dry-run
- **THEN** publication rejects the nonce and requires a new dry-run whose material mutation is revalidated against the standing publication order

#### Scenario: Private update proves its prior change note locally
- **WHEN** the retained item is Private and its public change-history page cannot expose the preceding note
- **THEN** preparation continues only when the canonical prior worker result and hashed successful receipt bind that note to the same item/package/plan and the durable release state is complete-reviewed, freezes both files into the new plan, and publication revalidates the unchanged source evidence immediately before dispatch

### Requirement: Steam mutation preserves identity and recovers admitted operations
`release_publish` SHALL use RimWorld's initialized Steam integration through the authenticated Gateway publisher, create at most one first item, update only the retained item thereafter, set content/title/description/preview/tags/visibility/required items/required DLC applications from the plan, and retain admitted operation identity across client timeout. First publication MUST be Private. Before launch it SHALL durably reserve the detached worker identity; before Steam dispatch it SHALL durably bind the exact Gateway run, plan, and callback state. Every durable transition SHALL be atomic. Once irreversible dispatch occurs, caller cancellation or worker loss MUST NOT abandon the admitted mutation; the configured MCP timeout SHALL exceed the operation's declared bound, and durable `release_status` MUST remain readable after plan expiry or later local profile/candidate mutation. A retry MUST reattach to the exact callback when it remains live; after a durable item ID exists it SHALL authenticated-query and poll only that same item's remote convergence before continuing verification. It MUST NOT issue another create or update call, and a lost first-create callback without a consistent durable ID MUST remain incomplete rather than guess.

#### Scenario: Admitted first publication succeeds once
- **WHEN** the caller passes a valid first-publication digest and nonce under an explicit matching publication order and Steam accepts creation/submission
- **THEN** one Private Workshop item is created and the run retains its item/update identity through completion

#### Scenario: Client times out after Steam admission
- **WHEN** submission was admitted but the MCP request times out
- **THEN** status/recovery addresses the same run and item instead of starting a second create or update

### Requirement: Successful publication persists and verifies the remote truth
Immediately after Steam reports success, before any later verification that can fail, the operation SHALL persist the Workshop ID in both the profile and `About/PublishedFileId.txt`, disable first-publication opt-in, and commit the identity-only change. It SHALL then verify remote title, owner, visibility, description, tags, primary preview bytes, required-item graph, required-DLC/application graph, and package content; reacquire the subscribed package into the Workshop cache; and run the prevalidated profile native subscriber workflow with the local package temporarily absent. It MUST then restore the local package and the user's requested normal mod-list state. Credentials and live bearer files MUST NOT enter durable evidence.

The subscriber workflow SHALL freeze its manifest/source inputs into the admitted plan and SHALL durably record its exact reserved game run, local package path, recovery backup, and normal configuration hash before moving or launching anything. A retry after worker loss SHALL gracefully stop that exact run when it exists, restore the exact local package without overwriting either copy, prove the normal configuration hash, and only then repeat verification.

#### Scenario: Published copy is independently reacquired
- **WHEN** remote metadata verification succeeds
- **THEN** the tool downloads the subscribed Workshop item, proves its package manifest matches the candidate, exercises the declared native player workflow from that copy, and restores the local development package afterward

#### Scenario: Gateway readiness precedes the playable map
- **WHEN** the subscribed-copy Gateway becomes reachable while RimWorld is still initializing its new game
- **THEN** the verifier waits for `ProgramState.Playing`, a non-null current map, and no active or waiting RimWorld long event before dispatching the first native subscriber workflow step, and every status request and retry delay remains bounded by the workflow deadline

#### Scenario: Gateway CLI screenshot returns its direct file result
- **WHEN** a subscriber evidence step invokes the Gateway client's native `screenshot --file` command and it returns its direct successful file metadata payload
- **THEN** the verifier validates that exact output path and nonempty file without requiring the JSON envelope used by HTTP-style Gateway commands

#### Scenario: Persistence or verification fails after upload
- **WHEN** Steam reports success but identity persistence, reacquisition, or native subscriber verification fails
- **THEN** the run reports an incomplete release against the known item ID, retains recovery evidence, and never claims the release verified or creates another item

### Requirement: Subscriber automation remains pending until personal review
Successful subscriber automation SHALL retain exact fresh screenshots and supporting results, but publication MUST report `awaiting-personal-review` rather than gameplay acceptance. A separate `release_accept_subscriber_evidence` operation SHALL require the exact publication receipt plus a concrete caller observation, validate and hash the retained screenshots, and only then mark the local release evidence complete-reviewed.

#### Scenario: Green subscriber workflow awaits visual inspection
- **WHEN** the reacquired Workshop copy completes its declared native workflow and captures the required screenshots
- **THEN** publication reports Steam verification plus subscriber evidence awaiting personal review and does not reuse the profile's expected observation as an observed result

#### Scenario: Caller accepts the exact reviewed frames
- **WHEN** the caller personally inspects the receipt's fresh screenshots and submits a concrete observation through `release_accept_subscriber_evidence`
- **THEN** the tool binds the observation and screenshot hashes to that exact receipt and advances durable status to complete-reviewed
