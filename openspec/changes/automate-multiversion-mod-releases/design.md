## Context

This repository currently builds a mod against one locally installed RimWorld tree. `Directory.Build.props` supplies a default `RimWorldPath`, and `Zlepper.RimWorld.ModSdk` copies an SDK-built package into an ignored artifact directory. The SDK already understands a `RimWorldVersion`, a managed-assembly path, a generated version folder, and a generated root `About/About.xml`; the missing layer is an exact-version input catalog and an orchestrator that invokes the SDK in clean, target-isolated builds.

The release inputs and outputs span four trust boundaries:

1. repository-owned source, manifests, templates, and art;
2. proprietary Steam depot content cached outside the repository;
3. generated, ignored build/presentation/evidence artifacts; and
4. a remote Steam Workshop item changed through an initialized Steam client session.

The owning mod is RimWorld Dev Gateway (`fumblesneeze.rimworlddevgateway`). Product mods, including `fumblesneeze.immersivechefs`, remain unaware of and independent from the Gateway. Version folders are a required RimWorld package layout, but they exist only in ignored build/staging output and the uploaded Workshop payload, never as source or checked-in binaries.

Research established the following constraints:

- [DepotDownloader](https://github.com/SteamRE/DepotDownloader) can request an exact app/depot/manifest and select individual files, making it suitable for a small compilation projection. Paid and historical manifests still require an owning Steam account and may no longer be accessible.
- SteamCMD's exact `download_depot` command is a useful fallback, but it downloads a depot rather than a selected managed-assembly set. [RimSort's version-download work](https://github.com/RimSort/RimSort/pull/2298) confirms that depots should be handled separately and supplies a useful discovery catalog; mutable third-party catalog data is not strong enough to be a release input until reviewed and pinned here.
- Steam does not require RimWorld to run when a Workshop item is uploaded with SteamCMD's [`workshop_build_item`](https://partner.steamgames.com/doc/features/workshop/implementation). That route requires separate credentials/session handling and does not cover the desired reviewed presentation workflow as well as a typed in-game publisher, so it is a recovery path rather than the primary path.
- RimWorld 1.6's inspected `Verse.Steam.Workshop.Upload` path uses SteamUGC, creates an item when the ID is invalid, supplies an auto-generated change note, and only sets the description on item creation. Calling it unchanged would violate update-only identity safety and cannot maintain the Workshop description.
- SteamUGC exposes the lower-level update primitives needed by the Gateway: `StartItemUpdate`, title/description/tags/content/preview setters, additional preview operations, progress, result callbacks, and legal-agreement status. Steam documents that an update cannot be cancelled after submission, so the package must remain immutable and the state machine must survive host polling timeouts. See the official [`ISteamUGC` API](https://partner.steamgames.com/doc/api/isteamugc).
- Existing RimWorld projects demonstrate useful individual ideas rather than a complete solution: [PublisherPlus](https://github.com/Jaxe-Dev/PublisherPlus) added in-game payload exclusions, and [RimWorld Multiplayer's Workshop workflow](https://github.com/rwmt/Multiplayer/blob/master/.github/workflows/build-workshop.yml) separates tag-driven package creation from source. The proposed tool uses a positive package allowlist and does not inherit an archived runtime patch or credential-heavy CI publication pattern.

## Goals / Non-Goals

**Goals:**

- Reproduce a release from pinned source, exact Steam manifests, pinned tools, and repository-owned declarations.
- Download only the managed files required for compilation while supporting complete exact-version installations for regression runs.
- Compile each declared RimWorld target independently and merge only verified outputs into the generated RimWorld package layout.
- Keep ordinary feature development current-first while deriving legacy C# symbols and XML projections only from each mod's declared compatibility metadata.
- Generate and review Workshop copy and graphics independently from in-game About metadata.
- Generate consistent required/optional mod disclosures, packaged required dependencies, and Steam required-item relationships from one declaration.
- Create exactly one first Workshop item only when a manifest explicitly opts into first publication and the confirmed dry-run has no item ID; thereafter update only that retained identity and verify the remote result.
- Preserve secret-free evidence that connects source, inputs, builds, player workflows, presentation, publication, and cleanup.

**Non-Goals:**

- Committing RimWorld files, generated version directories, or compiled mod assemblies.
- Redistributing a cached RimWorld installation or cached depot content.
- Automatically creating Workshop items, publishing from an ordinary build/tag alone, or storing Steam credentials in CI/repository artifacts.
- Making the Gateway a dependency of a product mod or including it in a product package.
- Claiming compatibility from compilation, startup, log inspection, or synthetic Gateway state alone; each claimed target still needs its declared player-workflow acceptance.
- Implementing food preservation, food waste, or other gameplay scope.

## Decisions

### 1. Use repository-owned manifests with two levels of identity

Add a global target catalog at `release/rimworld-targets.yaml` and a per-mod manifest at `mods/<ModName>/Release/release.yaml`.

The target catalog records a stable target ID, exact game version/build, RimWorld compatibility folder (for example `1.6`), app ID, platform/architecture, ordered base/DLC depot and manifest IDs, required compilation file selectors, expected `Version.txt`, and approved SHA-256 hashes. Branch names and external catalogs may discover candidates, but a release consumes only committed manifest IDs and approved content hashes.

The per-mod manifest records package/project paths, whether the mod is distributable or a development-only tool, required player languages for distributable products, an existing Workshop item ID or one explicit `allowFirstPublication` bootstrap, publication language/title/tags/visibility, one current development target, supported compile target IDs, optional additional regression target IDs, typed required/optional mod relationships, shared and target-specific package allowlists, exceptional C# compatibility-seam paths, XML legacy-override mappings, verification profiles, presentation sources/templates, and upload policy. Each uploaded compatibility folder maps to exactly one compile target. Additional exact patch builds may be regression-only and exercise that folder's compiled product, but cannot create a second payload for the same folder. A JSON Schema (or equivalently strict typed validation) rejects unknown properties, an omitted distribution classification, duplicate compatibility folders, an invalid development target, incomplete dependency identities, a missing item identity without the explicit first-publication flag, and package IDs that disagree with About/project metadata before side effects.

This separates shared game-build identity from a mod's compatibility and publication claims. Putting depot details in every mod manifest would duplicate mutable security-sensitive input; deriving them from a local installation would make the release non-reproducible.

### 2. Build one repository-local .NET release CLI

Implement `tools/RimWorldReleaseTool` with a thin `scripts/Invoke-RimWorldRelease.ps1` entry point. Proposed phases are `targets`, `acquire`, `build`, `test`, `present`, `stage`, `publish --dry-run`, `publish --confirm`, and `verify`. Each phase reads and writes versioned machine-readable contracts so it can be rerun without hiding state in shell variables.

A typed CLI is preferred over a large PowerShell script because manifest validation, file hashing, process leases, Steam adapter state, and evidence models need unit tests and stable error behavior. Existing build/E2E scripts remain the execution engines where appropriate; the release CLI orchestrates them rather than duplicating their safety rules.

Publishing requires a clean committed revision. An explicit dirty-source override may produce a local candidate, but that candidate is marked non-publishable and the Gateway refuses it.

### 3. Use DepotDownloader for selective acquisition and an immutable external cache

Pin and verify a DepotDownloader release at tool-bootstrap time; do not vendor its binary or source. Authenticate interactively using an owning Steam account/QR flow, never a password command-line argument, and keep downloader session state out of durable evidence.

Compilation requests only `Version.txt` and the declared `RimWorldWin64_Data/Managed` files from the base depot unless a target explicitly declares more. Each selected-depot entry is stored outside the worktree, by default under `%LOCALAPPDATA%/Fumblesneeze.RimWorldRelease/cache`, with an override for shared storage. Its key contains app, depot, manifest, platform, and a normalized file-selection hash. A local index records every path, size, and SHA-256. A mismatch quarantines the entry and reacquires it; the tool never substitutes the ordinary Steam installation.

Full regression installations use a separate cache namespace keyed by the ordered base/DLC manifest set and download the complete required depots. SteamCMD is retained as a documented diagnostic/full-depot fallback, not silently mixed into one cache identity. A historical manifest that Steam no longer permits is an onboarding/release failure, not permission to compile against a nearby version.

Hashing files in addition to pinning Steam manifests gives local corruption detection and auditable evidence. Hashes do not make proprietary files publishable; cache directories, downloader state, and all projections remain ignored.

### 4. Project compile inputs and isolate every target build

For each compile target, materialize a short-lived projection containing the approved `Version.txt`, managed DLLs, and generated target XML in the directory shape expected by `Zlepper.RimWorld.ModSdk`. Invoke MSBuild once per compatibility folder with explicit `RimWorldVersion` set to that folder (not the full patch/build string), exactly one derived RimWorld `DefineConstants` symbol such as `RIMWORLD1_6`, plus explicit `RimWorldPath`, `RimWorldManagedPath`, output root, and intermediate root. Do not share `obj`, incremental state, or generated package output between target invocations.

The SDK produces that target's ignored `<RimWorldVersion>/Assemblies` and content. The release tool verifies assembly references and target evidence before merging generated target trees into a final staged package and generating one root About file whose supported-version list comes only from successful, verified compatibility folders. Multiple exact regression builds for one compatibility line run against this one compiled folder rather than producing colliding upload payloads.

The alternative—multi-targeting one MSBuild invocation into one output tree—risks stale references and cross-target reuse. Reimplementing the SDK package layout would duplicate behavior already present in the repository's build dependency.

### 5. Stage from a positive allowlist and make the candidate immutable

Create candidates under `artifacts/Releases/<package-id>/<run-id>/` with separate `targets`, `package`, `presentation`, and `evidence` directories. Staging copies only declared paths and then rejects source, symbols, caches, credentials, test assemblies, Gateway assemblies in a product package, and undeclared files. It emits a sorted path/size/SHA-256 manifest plus one candidate digest.

After dry-run, publishing reads from an immutable leased candidate and rehashes it before admission and immediately before Steam submission. A changed byte invalidates the dry-run nonce. Steam receives the uncompressed package directory because its Workshop pipeline computes file deltas; zip files may be produced separately for non-Steam distribution but are not the Workshop content root.

### 6. Extend existing runners with exact target selection

The release CLI resolves a full-install cache entry and passes its exact executable, data path, matching compatibility-folder output, and build identity into the current scenario/grouped-E2E runners. This permits several exact regression builds to exercise the one binary selected for their compatibility line. Each group still declares its entire ordered mod list. The runner launches a fresh exact PID with a unique savedata folder, records the depot/file identities, and retains all repository acceptance requirements, including native player action and personal inspection of before/action/after screenshots.

The user's normal `ModsConfig.xml` is never selected. Its hash is recorded before and after as an additional invariant. Any temporary configuration lives under the isolated savedata folder and is removed with the exact process lease. Graceful shutdown precedes bounded exact-PID fallback; camera, selection, speed, developer/god state, and disposable world state are restored or discarded with the isolated run.

### 7. Generate presentation from structured content and deterministic web templates

Keep reusable templates under `release/templates/` and mod-owned presentation sources under `mods/<ModName>/Release/workshop/`. Structured YAML/JSON content names copy, source sprites, templates, ordering, and accessibility/asset metadata. The presentation model receives compatible versions and required/optional mod relationships from the release manifest rather than duplicating them in authored copy. A BBCode template produces `description.bbcode`, including explicit Required Mods and Optional Mods sections; HTML/CSS templates produce the primary preview, text banners, and feature/mechanics cards through a pinned Playwright Chromium plus pinned repository-licensed fonts.

HTML/CSS is chosen over manually compositing pixels because it supports reusable layouts, typography, sprite placement, masks, and a local review page. The renderer disables network access, time/random input, animations, and device-dependent fonts; it uses a fixed browser/tool version, viewport, device scale, and OS runner. It checks declared text regions using measured scroll/client bounds and fails on overflow, missing sprites, unexpected network loads, nondeterministic rerendering, wrong dimensions, or file-size limits.

The Workshop compiler and About generator may read common factual data, but their authored copy and templates remain distinct. Generated output never overwrites source sprites or presentation definitions.

### 8. Use reviewed Workshop feature art and reconcile its remote image identity

SteamUGC supports a primary preview and additional preview files. Workshop BBCode supports `[img]` URLs, while established mod pages commonly use wide visual headers and feature sheets to make dense mechanics approachable. Immersive Chefs therefore keeps deterministic wide feature cards with its release sources and reconciles those exact files as additional previews before embedding their queried remote URLs in the final description.

Publication is an explicit two-phase update: upload or replace the reviewed additional previews, query and retain their remote URLs/original names, compile or finalize the BBCode against those identities, then submit the content/metadata update. Evidence records that the operation is not atomic and binds both phases to the same item and reviewed local image hashes. It MUST NOT silently upload to an anonymous or ad-supported third-party host. A future repository-owned immutable static host remains an acceptable replacement if Steam changes additional-preview URL behavior.

The player-facing copy is benefit-led and concrete. It describes what colonists do and what players will see; terms used to implement or test the integration stay in the technical documentation. The final `Author's Note` is intentionally candid about the AI-supervised experiment and comparatively limited broad playtesting without turning the rest of the Workshop page into engineering notes.

### 9. Publish through a typed, update-only Gateway state machine

Launch a fresh isolated current supported RimWorld process with Core and the Dev Gateway only (plus the minimum publisher fixture if needed), and bind all discovery, HTTP calls, screenshots, diagnostics, and cleanup to its PID/start identity. The Gateway remains authenticated and loopback-only.

The dry-run endpoint accepts a validated candidate receipt, queries the exact declared PublishedFileId and current account ownership, remote metadata, and required-item graph, computes a bounded diff, and returns a short-lived nonce bound to the candidate, presentation, dependency graph, remote-item, and preflight hashes. It does not call `StartItemUpdate`, `AddDependency`, or `RemoveDependency`.

Confirmation must present that nonce and the same hashes. The ordinary path rejects ID zero/invalid. The one-time bootstrap path calls `CreateItem` only when the manifest explicitly allows first publication, the dry-run declared no item ID, the user confirmed that exact creation, and no prior receipt or local PublishedFileId exists; its successful callback persists the returned identity before any content submission. That first submission is forced to Private visibility so the user can inspect it before a later reviewed promotion. On the Unity main thread the workflow then calls `StartItemUpdate`, verifies every SteamUGC setter result, sets update language/title/description/visibility/tags/content/primary and additional previews plus a metadata fingerprint, and calls `SubmitItemUpdate` with the authored change note. It then reconciles the confirmed required-item edges with Steam's dependency operations, checking every asynchronous result; optional relationships are never submitted as dependencies. The HTTP request returns an accepted operation ID rather than blocking the Unity thread. Bounded status endpoints expose progress and terminal result while the Steam callback-owned operation remains unique and retry-safe.

The state machine distinguishes pre-submit cancellation from post-submit uncertainty. After submission it keeps the stage lease until a terminal callback, checks both `EResult` and the legal-agreement flag, and refuses a second publication while an earlier callback may still arrive. Host polling timeouts do not destroy Gateway state or imply failure. No reusable Steam or Gateway credentials enter receipts.

Calling RimWorld's existing `Workshop.Upload` was rejected because it can create an item implicitly, ignores setter results, generates the change note, and cannot update the description on the inspected 1.6 update path. SteamCMD publication remains a manual recovery option when the in-game Steam session cannot initialize, but must consume the same immutable stage and cannot be reported as Gateway-verified.

### 10. Verify the remote outcome, not only the submit callback

On success, query the item through SteamUGC and compare item identity, title, description, tags, metadata fingerprint, preview inventory, and exact required-item graph with the reviewed bundle. Inspect the packaged `About/About.xml` to confirm supported versions and required package dependencies match the same manifest. After CDN propagation, reacquire the user's own item into a separate ignored verification directory and compare its file manifest with the staged package. Record discrepancies as a failed verification even when `SubmitItemUpdate` returned success.

Implementation tests use a narrow fake SteamUGC adapter to exercise identities, stale nonces, setter failures, timeouts, late callbacks, and legal-agreement states. Final acceptance of publication requires explicit user authorization and a dedicated unlisted/private test Workshop item; it cannot use a production item or synthetic mutation. Every claimed target needs its native player-workflow acceptance, and at least one separate product-mod acceptance run must omit the Gateway entirely.

### 11. Make the mod manifest authoritative for compatibility

The mod manifest identifies one `developmentTarget` and an ordered set of supported compile targets. At project inception, Immersive Chefs declares only the current RimWorld 1.6 target. The release tool derives target folders, compile inputs, C# symbols, XML projections, `About/About.xml` supported versions, Workshop compatibility text/tags, and verification groups from that one declaration. Generated or authored outputs that name a version outside the manifest fail validation.

When RimWorld 1.7 or later arrives, add and verify its exact target catalog entry, make it the mod's development target, and keep 1.6 in the supported set only if the mod will continue to test and ship it. Ordinary feature work builds and proves the current target first. Full older-version compilation and native workflow runs happen during the deliberate compatibility/release pass, matching the repository policy that the full matrix is not replayed for every feature slice. Any legacy fixes found there are separate focused TDD slices before the release candidate is rebuilt.

This is preferred to discovering compatibility from source directives or existing output folders: metadata declares the promise, while builds and live runs prove it.

### 12. Keep C# current by isolating legacy conditional seams

Derive a valid preprocessor identifier from each compatibility folder: `1.6` becomes `RIMWORLD1_6`, `1.7` becomes `RIMWORLD1_7`, and so on. Every target invocation defines exactly its one RimWorld compatibility symbol. The normal local build resolves the manifest's development target and uses the same symbol rules as release builds.

Core feature/domain code remains unconditional and follows the newest declared development target. Only a narrow engine-facing adapter, signature difference, or data-shape seam may use directives. Put those adapters under the owning project's neutral `Compatibility/RimWorld/` source area by default; when a declaration site cannot be moved safely, the mod manifest must allowlist that exact exceptional file with a rationale. Once 1.6 becomes legacy, a typical seam may select a `#if RIMWORLD1_6` implementation and use the current implementation otherwise; the feature calling that seam remains unchanged. Do not create per-version subfolders, surround whole features, duplicate source trees, or add speculative legacy branches before a real older compile/test failure demonstrates the divergence.

Compiling each target against only its exact assemblies is the enforcement mechanism: an unguarded new API reference fails the older build, while a stale legacy branch fails its own target build. Static validation additionally rejects target invocations with zero or multiple `RIMWORLD<major>_<minor>` symbols.

### 13. Generate legacy XML through parsed target projections

Canonical Defs and Patches remain in their ordinary version-neutral mod source locations. Most targets copy this XML unchanged. If an older engine needs a changed element, class name, XPath, or value, the mod manifest maps that target to one or more ordered override files under a neutral `Release/compatibility/xml/` source directory; it never creates a `1.6/Defs`-style source tree.

Use a small typed XML operation model over parsed XML rather than textual `#if` macros or string replacement. Each targeted operation names the canonical source file, an XPath selector, expected match cardinality, and a typed add/replace/remove action. Targeted operations are the default. If a real legacy engine divergence makes them less maintainable than a document replacement, permit an explicitly declared replacement file in the same neutral directory with a rationale and the same validation/provenance; this is an escape hatch, not a parallel version tree. The tool applies operations to an ignored target projection, preserves deterministic ordering/format policy, validates well-formedness and duplicate Def identities, and records source/operation/output hashes. A selector whose cardinality changes fails closed, making canonical-source drift visible.

These build-time transforms are distinct from RimWorld `PatchOperation` files used at runtime for optional mods. The implementation slice should prove the typed operation set with real representative Def fixtures before expanding it; if it cannot express a necessary legacy difference safely, extend the typed model or use the reviewed document-replacement operation rather than falling back to raw textual preprocessing.

### 14. Reconcile one dependency graph across RimWorld, presentation, and Steam

The per-mod manifest contains a typed `dependencies` collection. Every entry has `kind` (`required` or `optional`), canonical package ID, display name, presentation note, and a Workshop PublishedFileId when Steam publication requires a link or dependency edge. Optional entries may also declare a load-order hint only when the owning compatibility design needs one; that hint never turns them into a hard dependency.

The release tool projects this graph three ways:

1. required entries become RimWorld `modDependencies` in generated `About/About.xml`;
2. both classes become linked, explicitly separated Required Mods and Optional Mods sections in the Workshop description; and
3. required entries only become Steam parent-child required-item relationships through `ISteamUGC.AddDependency`/`RemoveDependency`.

The dry-run shows additions and removals in all three projections and binds the exact graph hash into confirmation. Because Steam dependency operations are asynchronous and separate from content submission, the workflow does not claim atomicity: a partial result remains unverified, preserves its candidate/remote snapshot, and retries only the unresolved confirmed edges after querying remote state. Remote verification must match the exact required graph; optional items appearing as Steam requirements are a failure.

Maintaining separate hand-authored lists was rejected because required/optional drift could make the mod unloadable, mislead users, or cause Steam to install optional integrations as hard requirements.

### 15. Treat complete localization as staged product content

Every distributable mod manifest declares the repository's baseline release languages: `English`, `German`, `Spanish`, `French`, `ChineseSimplified`, and `Russian`. A development-only tool may explicitly opt out; `fumblesneeze.rimworlddevgateway` uses that classification because it is not a player distribution product. The stage validator parses canonical keyed text, Def source fields, conditional patch-added Def fields, and guarded runtime translation keys; it compares those inventories with every required locale, rejects malformed/duplicate/stale entries and placeholder or rich-text-tag drift, and records catalog hashes in candidate evidence. English Def source values may be canonical without redundant DefInjected copies, but every runtime key requires an English keyed value.

This gate validates completeness and structure, not linguistic quality by pretending that directory presence or string equality is sufficient. Product development records that the required translations were authored by an agent or human from gameplay context rather than sent through an automated translation service, and native-language smoke profiles render representative settings, Def, job, alert, and runtime text. Publication remains blocked when either structural coverage or the product's declared live language profile is incomplete.

### 16. Treat change notes and gameplay showcases as release inputs

Every update after first publication carries an authored change note that tells players what visibly changed. The note is stored with the mod's release metadata, displayed in the mutation-free dry-run, bound into the publication-plan hash, submitted through `SubmitItemUpdate`, and compared with the remote change-note entry after publication. It is not an automatic commit list, file inventory, test report, or generic phrase such as `Update`; if there is no player-facing change to describe, the release is not ready to publish.

Each distributable mod may declare an ordered showcase manifest under its Workshop sources. A showcase names its player-facing purpose, required active package chain, scene recipe, still-frame crop, motion beats, expected native actions and observable outcomes, whether it produces a screenshot, a GIF, or both, and its final carousel order. Captures come from a fresh isolated game process running the reviewed build. Setup code may construct a natural colony scene, but the visible outcome is driven by ordinary jobs, bills, orders, inspect tabs, memories, and optional-mod interactions rather than by drawing a synthetic end state.

Showcase scenes use believable names, materials, apparel, floors, lighting, decoration, storage, terrain, rooms, linked workbenches, paths, power/plumbing and incidental colony clutter. The frame contains no test banners, selection brackets, debug labels, active dev windows, learning helper, or disposable-test presentation. A presentation capture route may frame Things or a declared camera center and crop the rendered frame without selecting anything. It records and restores camera, selection and UI state. GIF capture samples a bounded fixed camera sequence and may use hard cuts between nearby story beats; it does not pan across dead travel time. A hard-cut assembly consumes only retained v1 segment frames from one exact held PID/start/run and package build, hashes every source record, requires the source beats to form the exact declared ordered sequence, and retains its relocatable source paths. It cannot join separate launches or add synthetic transition frames. Final GIFs are at most five seconds and every additional preview obeys Steam's under-1-MiB file limit.

Scene geometry is reference-grounded rather than improvised from a cleared test map. Before arrangement, the author inspects a varied player-built corpus covering whole-colony context, the specific room/workflow, the requested optional-mod ecosystem when available, and meaningful contrasting layouts. Recurrent or mechanically necessary patterns become constraints; one-off aesthetic choices remain candidates. A per-showcase design record retains the brief, eligible source IDs, classified rules, adjacency graph, exact placement rationale, rejected drafts and live visual observations. Real Ruins evidence uses a bounded but statistically useful bulk cohort: metadata for thousands of independently sampled maps, safely streamed blueprint bodies at a declared sample size, exact Def-aware placement measurements, and a stratified visual inspection subset. Tiny convenience samples cannot establish geometry rules. Raw player blueprints remain ignored research inputs and are never copied into a release.

Screenshots and GIFs are complementary release artifacts rather than substitutes: the still gives a readable high-detail view, while the short motion preview demonstrates the behavior. The release bundle records package order, scene/capture definition, source frame hashes, crop, timing, encoder/version/options, final dimensions/bytes/hash and personal visual-review observations. A screenshot or GIF whose scene looks like a synthetic test fixture, whose action is manufactured, whose crop clips the subject, or whose compression makes the behavior unreadable is rejected before dry-run.

## Risks / Trade-offs

- **[Steam revokes access to an old manifest]** → Fail target onboarding/release with the exact inaccessible manifest; never replace it silently. Preserve approved file hashes as evidence but do not redistribute the files.
- **[Steam account/session automation is interactive or rate-limited]** → Authenticate with QR/Steam client state, keep acquisition resumable, isolate one depot at a time, and never put credentials on a command line or in logs.
- **[Steam CDN URLs for additional previews are unsuitable for inline BBCode]** → Gate the production design on an unlisted-item experiment and require an explicit static-host decision or carousel-only presentation.
- **[Two-phase preview/description publication exposes a temporary inconsistent state]** → Surface the phases in dry-run and evidence, preserve the previous remote snapshot, and avoid claiming atomicity or rollback.
- **[Steam accepts an update but the client loses the callback]** → Keep an indeterminate terminal state, query remote fingerprint/content before retry, and refuse blind resubmission.
- **[Workshop updates cannot be rolled back transactionally]** → Retain the previous remote metadata snapshot and last verified local candidate so an operator can explicitly republish it; never call that an automatic rollback.
- **[Browser rendering differs across machines]** → Pin Chromium, fonts, viewport, scale, tool versions, and runner OS; compare a second render and record all provenance.
- **[Build output accidentally crosses targets]** → Give every target unique intermediate/output roots and verify referenced game-input hashes before merge.
- **[Version directives spread through feature code]** → Keep the development target unconditional, permit symbols only in narrow compatibility seams, and require an older-target compile failure or verified API divergence before adding a legacy branch.
- **[Legacy XML transforms drift from canonical XML]** → Use parsed typed operations with expected selector cardinality, validate every generated target, and hash both inputs and outputs.
- **[RimWorld, description, and Steam dependencies disagree]** → Derive all three from one typed manifest graph, include the graph in dry-run confirmation, and verify both packaged and remote projections.
- **[Steam dependency reconciliation partially succeeds]** → Treat dependency edges as separately observable asynchronous operations, query remote state before retry, and leave the release unverified until the exact graph matches.
- **[Full historical installations consume substantial disk]** → Separate compact compilation and full-run caches, use manifest-keyed reuse and explicit cache inventory/pruning, and never prune an active lease.
- **[The release orchestrator becomes a second test framework]** → Invoke existing guarded build/E2E scripts and add only target resolution, contracts, and evidence plumbing.
- **[A Gateway publisher weakens product isolation]** → Keep publisher code/package ownership in RimWorld Dev Gateway, validate product package exclusions, and perform at least one final product acceptance run without Gateway.
- **[A release ships English fallbacks or broken placeholders]** → Inventory canonical player text, require exact six-language structural parity for distributable mods, and bind catalog hashes plus live language evidence into the candidate receipt.

## Migration Plan

1. Add schemas, the initial target catalog, and one current-1.6 non-production example manifest containing development/supported targets plus explicit empty required/optional dependency collections. Onboard historical target manifests interactively and approve their file hashes in a separate review.
2. Implement acquisition/cache contracts with fake-downloader tests, then an opt-in real Steam acquisition smoke test. No existing local installation path changes.
3. Add clean target-isolated build, derived-symbol validation, parsed XML projection, and allowlisted merge phases; compare the current target's package with the existing single-version SDK output before making them the release path.
4. Add exact-target parameters to isolated runners while retaining their current default. Exercise a current build first, then one accessible historical build.
5. Add dependency-aware About generation and presentation compilation/validation, then require human review of required/optional sections and the local preview before any publication work.
6. Complete the unlisted Workshop image-hosting experiment and resolve the hosting open question in the spec/design before enabling inline-image production publishing.
7. Implement the Gateway adapter, content state machine, and required-item reconciliation under host tests, then run dry-run against a dedicated unlisted test item. With explicit user authorization, perform and remotely verify a test update and dependency graph.
8. Document the operational commands and switch one mod to the new pipeline. The previous manual upload remains available until one complete release and verification receipt succeeds.

Rollback of local tooling means selecting the previous committed release manifest/tool revision; all generated candidates and caches are external/ignored and can remain for diagnosis. Runner migration never changes the user's normal configuration: isolated `ModsConfig.xml` files are discarded with their savedata folder and the normal file's before/after hash must match. A Steam update has no transactional rollback; recovery is an explicit, reviewed republish of the previous verified candidate and metadata snapshot.

## Open Questions

- Can Steam additional-preview CDN URLs be treated as stable inline Workshop image URLs, and can their lifecycle be reconciled without breaking old descriptions?
- Which repository-owned static image host is acceptable if Steam preview URLs fail the experiment?
- Which exact historical 1.5/1.6 manifest set is both desired and still downloadable by the release account? Compatibility targets are not declared until this is answered and tested.
- Should acquisition use a dedicated release Steam account or the operator's normal owning account, given Steam Guard and license constraints?
- Which fixed Workshop visibility and tag set should the first production mod manifest declare for its unified item?
- Which first real RimWorld engine XML divergence, if any, is complex enough to require extending the initial typed add/replace/remove operation set?
