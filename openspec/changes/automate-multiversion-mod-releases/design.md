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
- Generate and review Workshop copy and graphics independently from in-game About metadata.
- Update exactly one existing Workshop item through a guarded dry-run/confirm operation, then verify the remote result.
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

The per-mod manifest records package/project paths, existing Workshop item ID, publication language/title/tags/visibility, supported compile target IDs, optional additional regression target IDs, shared and target-specific package allowlists, verification profiles, presentation sources/templates, and upload policy. Each uploaded compatibility folder maps to exactly one compile target. Additional exact patch builds may be regression-only and exercise that folder's compiled product, but cannot create a second payload for the same folder. A JSON Schema (or equivalently strict typed validation) rejects unknown properties, duplicate compatibility folders, and package IDs that disagree with About/project metadata before side effects.

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

For each compile target, materialize a short-lived projection containing the approved `Version.txt` and managed DLLs in the directory shape expected by `Zlepper.RimWorld.ModSdk`. Invoke MSBuild once per compatibility folder with explicit `RimWorldVersion` set to that folder (not the full patch/build string), plus explicit `RimWorldPath`, `RimWorldManagedPath`, output root, and intermediate root. Do not share `obj`, incremental state, or generated package output between target invocations.

The SDK produces that target's ignored `<RimWorldVersion>/Assemblies` and content. The release tool verifies assembly references and target evidence before merging generated target trees into a final staged package and generating one root About file whose supported-version list comes only from successful, verified compatibility folders. Multiple exact regression builds for one compatibility line run against this one compiled folder rather than producing colliding upload payloads.

The alternative—multi-targeting one MSBuild invocation into one output tree—risks stale references and cross-target reuse. Reimplementing the SDK package layout would duplicate behavior already present in the repository's build dependency.

### 5. Stage from a positive allowlist and make the candidate immutable

Create candidates under `artifacts/Releases/<package-id>/<run-id>/` with separate `targets`, `package`, `presentation`, and `evidence` directories. Staging copies only declared paths and then rejects source, symbols, caches, credentials, test assemblies, Gateway assemblies in a product package, and undeclared files. It emits a sorted path/size/SHA-256 manifest plus one candidate digest.

After dry-run, publishing reads from an immutable leased candidate and rehashes it before admission and immediately before Steam submission. A changed byte invalidates the dry-run nonce. Steam receives the uncompressed package directory because its Workshop pipeline computes file deltas; zip files may be produced separately for non-Steam distribution but are not the Workshop content root.

### 6. Extend existing runners with exact target selection

The release CLI resolves a full-install cache entry and passes its exact executable, data path, matching compatibility-folder output, and build identity into the current scenario/grouped-E2E runners. This permits several exact regression builds to exercise the one binary selected for their compatibility line. Each group still declares its entire ordered mod list. The runner launches a fresh exact PID with a unique savedata folder, records the depot/file identities, and retains all repository acceptance requirements, including native player action and personal inspection of before/action/after screenshots.

The user's normal `ModsConfig.xml` is never selected. Its hash is recorded before and after as an additional invariant. Any temporary configuration lives under the isolated savedata folder and is removed with the exact process lease. Graceful shutdown precedes bounded exact-PID fallback; camera, selection, speed, developer/god state, and disposable world state are restored or discarded with the isolated run.

### 7. Generate presentation from structured content and deterministic web templates

Keep reusable templates under `release/templates/` and mod-owned presentation sources under `mods/<ModName>/Release/workshop/`. Structured YAML/JSON content names copy, source sprites, templates, ordering, and accessibility/asset metadata. A BBCode template produces `description.bbcode`; HTML/CSS templates produce the primary preview, text banners, and feature/mechanics cards through a pinned Playwright Chromium plus pinned repository-licensed fonts.

HTML/CSS is chosen over manually compositing pixels because it supports reusable layouts, typography, sprite placement, masks, and a local review page. The renderer disables network access, time/random input, animations, and device-dependent fonts; it uses a fixed browser/tool version, viewport, device scale, and OS runner. It checks declared text regions using measured scroll/client bounds and fails on overflow, missing sprites, unexpected network loads, nondeterministic rerendering, wrong dimensions, or file-size limits.

The Workshop compiler and About generator may read common factual data, but their authored copy and templates remain distinct. Generated output never overwrites source sprites or presentation definitions.

### 8. Treat inline Workshop-image hosting as a gated prototype

SteamUGC supports a primary preview and additional preview files. Workshop BBCode supports `[img]` URLs, but Steam does not document an API for attaching arbitrary inline-description images. Many established mods use third-party image hosts; that creates link-rot and ownership risk.

Before production publication, use a dedicated unlisted test item to determine whether uploaded additional preview files yield stable Steam CDN URLs that can be queried and embedded back into the description. If proven, publication becomes an explicit two-phase update: upload/reconcile preview assets, query their remote URLs, compile the final description, then submit the content/metadata update. Evidence records that this is not atomic. If the experiment fails, select and document an immutable repository-owned static host, or restrict those graphics to the Steam preview carousel until one exists. Do not silently upload to an ad-supported third-party host.

This question is deliberately not hidden inside the renderer because it affects the remote publication contract and recovery semantics.

### 9. Publish through a typed, update-only Gateway state machine

Launch a fresh isolated current supported RimWorld process with Core and the Dev Gateway only (plus the minimum publisher fixture if needed), and bind all discovery, HTTP calls, screenshots, diagnostics, and cleanup to its PID/start identity. The Gateway remains authenticated and loopback-only.

The dry-run endpoint accepts a validated candidate receipt, queries the exact declared PublishedFileId and current account ownership/remote metadata, computes a bounded diff, and returns a short-lived nonce bound to the candidate, presentation, remote-item, and preflight hashes. It does not call `StartItemUpdate`.

Confirmation must present that nonce and the same hashes. The Gateway rejects ID zero/invalid and never calls `CreateItem`. On the Unity main thread it calls `StartItemUpdate`, verifies every SteamUGC setter result, sets update language/title/description/visibility/tags/content/primary and additional previews plus a metadata fingerprint, and calls `SubmitItemUpdate` with the authored change note. The HTTP request returns an accepted operation ID rather than blocking the Unity thread. Bounded status endpoints expose progress and terminal result while the Steam callback-owned operation remains unique and retry-safe.

The state machine distinguishes pre-submit cancellation from post-submit uncertainty. After submission it keeps the stage lease until a terminal callback, checks both `EResult` and the legal-agreement flag, and refuses a second publication while an earlier callback may still arrive. Host polling timeouts do not destroy Gateway state or imply failure. No reusable Steam or Gateway credentials enter receipts.

Calling RimWorld's existing `Workshop.Upload` was rejected because it can create an item implicitly, ignores setter results, generates the change note, and cannot update the description on the inspected 1.6 update path. SteamCMD publication remains a manual recovery option when the in-game Steam session cannot initialize, but must consume the same immutable stage and cannot be reported as Gateway-verified.

### 10. Verify the remote outcome, not only the submit callback

On success, query the item through SteamUGC and compare item identity, title, description, tags, metadata fingerprint, and preview inventory with the reviewed bundle. After CDN propagation, reacquire the user's own item into a separate ignored verification directory and compare its file manifest with the staged package. Record discrepancies as a failed verification even when `SubmitItemUpdate` returned success.

Implementation tests use a narrow fake SteamUGC adapter to exercise identities, stale nonces, setter failures, timeouts, late callbacks, and legal-agreement states. Final acceptance of publication requires explicit user authorization and a dedicated unlisted/private test Workshop item; it cannot use a production item or synthetic mutation. Every claimed target needs its native player-workflow acceptance, and at least one separate product-mod acceptance run must omit the Gateway entirely.

## Risks / Trade-offs

- **[Steam revokes access to an old manifest]** → Fail target onboarding/release with the exact inaccessible manifest; never replace it silently. Preserve approved file hashes as evidence but do not redistribute the files.
- **[Steam account/session automation is interactive or rate-limited]** → Authenticate with QR/Steam client state, keep acquisition resumable, isolate one depot at a time, and never put credentials on a command line or in logs.
- **[Steam CDN URLs for additional previews are unsuitable for inline BBCode]** → Gate the production design on an unlisted-item experiment and require an explicit static-host decision or carousel-only presentation.
- **[Two-phase preview/description publication exposes a temporary inconsistent state]** → Surface the phases in dry-run and evidence, preserve the previous remote snapshot, and avoid claiming atomicity or rollback.
- **[Steam accepts an update but the client loses the callback]** → Keep an indeterminate terminal state, query remote fingerprint/content before retry, and refuse blind resubmission.
- **[Workshop updates cannot be rolled back transactionally]** → Retain the previous remote metadata snapshot and last verified local candidate so an operator can explicitly republish it; never call that an automatic rollback.
- **[Browser rendering differs across machines]** → Pin Chromium, fonts, viewport, scale, tool versions, and runner OS; compare a second render and record all provenance.
- **[Build output accidentally crosses targets]** → Give every target unique intermediate/output roots and verify referenced game-input hashes before merge.
- **[Full historical installations consume substantial disk]** → Separate compact compilation and full-run caches, use manifest-keyed reuse and explicit cache inventory/pruning, and never prune an active lease.
- **[The release orchestrator becomes a second test framework]** → Invoke existing guarded build/E2E scripts and add only target resolution, contracts, and evidence plumbing.
- **[A Gateway publisher weakens product isolation]** → Keep publisher code/package ownership in RimWorld Dev Gateway, validate product package exclusions, and perform at least one final product acceptance run without Gateway.

## Migration Plan

1. Add schemas, the initial target catalog, and one non-production example manifest. Onboard historical target manifests interactively and approve their file hashes in a separate review.
2. Implement acquisition/cache contracts with fake-downloader tests, then an opt-in real Steam acquisition smoke test. No existing local installation path changes.
3. Add clean target-isolated build and allowlisted merge phases; compare their package with the current single-version SDK output before making them the release path.
4. Add exact-target parameters to isolated runners while retaining their current default. Exercise a current build first, then one accessible historical build.
5. Add presentation compilation/validation and require human review of its local preview before any publication work.
6. Complete the unlisted Workshop image-hosting experiment and resolve the hosting open question in the spec/design before enabling inline-image production publishing.
7. Implement the Gateway adapter and state machine under host tests, then run dry-run against a dedicated unlisted test item. With explicit user authorization, perform and remotely verify a test update.
8. Document the operational commands and switch one mod to the new pipeline. The previous manual upload remains available until one complete release and verification receipt succeeds.

Rollback of local tooling means selecting the previous committed release manifest/tool revision; all generated candidates and caches are external/ignored and can remain for diagnosis. Runner migration never changes the user's normal configuration: isolated `ModsConfig.xml` files are discarded with their savedata folder and the normal file's before/after hash must match. A Steam update has no transactional rollback; recovery is an explicit, reviewed republish of the previous verified candidate and metadata snapshot.

## Open Questions

- Can Steam additional-preview CDN URLs be treated as stable inline Workshop image URLs, and can their lifecycle be reconciled without breaking old descriptions?
- Which repository-owned static image host is acceptable if Steam preview URLs fail the experiment?
- Which exact historical 1.5/1.6 manifest set is both desired and still downloadable by the release account? Compatibility targets are not declared until this is answered and tested.
- Should acquisition use a dedicated release Steam account or the operator's normal owning account, given Steam Guard and license constraints?
- Which fixed Workshop visibility and tag set should the first production mod manifest declare for its unified item?
