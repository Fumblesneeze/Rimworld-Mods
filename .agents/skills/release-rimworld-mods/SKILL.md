---
name: release-rimworld-mods
description: Prepare, verify, and publish reproducible RimWorld mod releases across exact game versions. Use when planning or operating release manifests, Steam depot acquisition and caching, version-isolated builds, generated Workshop descriptions or graphics, exact-version regression runs, staged packages, or guarded Steam Workshop updates in this repository.
---

# Release RimWorld Mods

## Overview

Build a release from pinned Steam inputs without committing game-version source trees or compiled assemblies. Keep acquisition, compilation, player-workflow verification, presentation rendering, and Workshop publication connected by one immutable evidence chain.

Read [references/release-checklist.md](references/release-checklist.md) before changing release contracts or operating a release. Also read the repository's `rimworld-mod-development` skill and its routed references because its build, package, live-run, and acceptance gates remain mandatory.

## Establish scope and readiness

1. Inspect root and local `AGENTS.md`, the selected mod, its package ID, its release manifest, and the global target catalog.
2. Identify the owning OpenSpec change and read its proposal, design, capability spec, and tasks. For the planned pipeline, use `openspec/changes/automate-multiversion-mod-releases/`.
3. Check task state and the actual CLI help/scripts before naming commands. While the implementation tasks are incomplete, do not invent a working release or publication command; limit work to the implemented phases and report the missing phase precisely.
4. Keep out-of-band release work isolated from active gameplay work. Never stage, rewrite, or commit another agent's files.
5. Refuse publication from an uncommitted or dirty candidate. A dirty override may create a non-publishable local candidate only when the implemented tool explicitly supports it.

## Resolve exact game targets

1. Accept only target IDs declared by both the mod manifest and the committed target catalog.
2. Acquire the catalog's exact Steam depot manifests. Use the pinned selective downloader for compilation inputs and the full-install path for regression; never substitute the normal installation or a nearby version.
3. Put proprietary files and downloader session state in the configured external ignored cache. Verify the immutable cache identity and every approved file hash before use.
4. Treat an inaccessible historical manifest as a blocked target. Do not redistribute cached game files, approve a different manifest implicitly, or place them in a mod artifact.

## Build and stage

1. Run a clean SDK build separately for every declared target, with unique managed-input, intermediate, and output roots.
2. Confirm that generated RimWorld-version folders exist only under ignored release output. Never create version-named source folders or commit generated DLLs.
3. Merge only successful verified targets. Generate the root About metadata from those targets and stage the product package from a positive allowlist.
4. Reject source, symbols, caches, credentials, tests, undeclared content, and Dev Gateway assemblies in product packages.
5. Record a sorted file manifest and candidate digest. Keep the candidate immutable from review through remote verification.

## Prove compatibility

1. Run focused and owning tests, guarded repository tests, Release builds, strict OpenSpec validation, and package inspection.
2. Build a release impact map from runtime code, XML/Defs, assets, dependencies, engine targets, workflows, observation contracts, and test-harness changes. Run every impacted target scenario in a fresh exact-version RimWorld process with unique savedata and the complete ordered mod list; unchanged scenarios may rely on current deterministic regression results when the map proves their relevant staged bytes and inputs are identical.
3. Make each live scenario assert the admitted native player action, the causally related observable outcome, and exact-process cleanup. Personally inspect before/action/after screenshots for new or materially changed player-visible scenarios. Do not manually review every unchanged scenario merely because a release is being prepared. Compilation, startup, logs, uncorrelated Gateway JSON, direct state mutation, and static end-state screenshots are supporting evidence only.
4. Use the Dev Gateway as the default controller, including subscribed-copy verification. Run a Gateway-free duplicate only when explicitly requested or when an impacted behavior cannot be faithfully controlled or observed through the Gateway. Preserve the normal `ModsConfig.xml` before/after hash and exact-PID cleanup evidence.
5. Use the `code-review` skill on the scoped diff, resolve findings, and repeat any verification invalidated by review fixes.

## Generate and review Workshop presentation

1. Keep `About/About.xml` and Workshop copy as separate outputs, even when they share factual structured data.
2. Render description BBCode, banners, primary preview, and feature/mechanics graphics only from versioned templates, local fonts, authored content, and repository-owned sprites.
3. Require offline deterministic rerendering, text-overflow checks, link/BBCode policy, image dimension/file-size validation, provenance hashes, and a local preview.
4. Personally inspect copy, layout, sprites, cropping, readability, and image order. Publication consumes only the exact reviewed presentation digest.
   Each feature card must visually communicate the behavior named by its headline and bullets;
   decorative but unrelated assets are a release defect. For directional game sprites, require the
   reviewed player-facing orientation instead of trusting a cardinal suffix.
5. Write for players browsing a mod page, not developers reviewing a patch. Lead with the fantasy and visible behavior; use concrete verbs such as cooks gather, diners fetch, dishes return dirty, and waiters clear. Keep terms such as adapter, package-gated, absent-safe, ownership, provenance, seam, reflection, and state machine in technical docs. Compatibility entries must say what the player will notice and any exact load-chain requirement in plain language.
6. Use wide visual headers/feature cards to break up dense inventories. For the proven Immersive Chefs layout, author 1164×655 cards and keep every Steam additional preview below 1 MiB. Use a checked-in card manifest, reusable template, repository-local licensed font, and actual shipped sprites; review one contact sheet plus every important full-resolution card. Avoid unexplained status affordances such as checkmarks, warning badges, toggles, or progress marks: feature art should showcase behavior, not look like a task tracker. Verify alpha on the actual Workshop page; Steam may matte transparent rounded corners white, so use an explicitly measured page-background matte (Immersive Chefs currently pins `#1b2838`) and test the final corner pixels.
7. Do not assume Steam's primary preview also occupies gallery slot zero. When the mod identity must open the gallery, upload the reviewed title image again as additional preview zero, bind both roles to the same local hash, and keep that title URL out of BBCode unless the user explicitly requests an inline repeat. Decode and validate the title's real dimensions separately from feature-card dimensions.
8. Treat image hosting as a reviewed identity problem. Synchronize ordered additional previews first, query the resulting Steam URLs/original names, resolve only the intended inline URLs into the final BBCode, then publish metadata/content. Bind every local hash, remote index, and URL in evidence, including gallery-only images. Never put unresolved tokens on the live page or silently use an anonymous/ad-supported image host.
9. Query public Workshop change history with bounded ordinary browser request headers. Steam may throttle an obvious custom verifier user agent while serving the same page to a normal browser-shaped request. Keep the request read-only, unauthenticated, capped in bytes/time, and retain the explicit-429-only retry ceiling; never skip or fabricate pre/post change-note verification because one client fingerprint was throttled.
8. End distributable-mod copy with the user-reviewed `Author's Note` when requested. It may disclose AI-supervised development, frameworks, testing, playtesting limits, and feedback expectations in the author's voice; nothing follows it. State whether the shipped mod performs live AI generation.
9. When a mod distinguishes especially valuable optional integrations, put a concise `Recommended Mods` section immediately before the full optional catalog. Link exact Workshop pages when the description budget permits, state any framework chain in player language, and remove those entries from the optional list instead of repeating them.
10. Declare gameplay showcases per mod instead of improvising them at capture time. Each declaration names exact presentation-only package groups, natural scene dressing, the native player workflow, visible beats, requested still/GIF formats, and carousel order. Presentation-only apparel or scenery mods are not product dependencies.
11. Before drafting showcase geometry, use the repo-local `rimworld-realistic-base-generation` skill and create `Release/workshop/designs/<showcase-id>.md`. Record the brief, eligible visual/mechanical reference IDs, classified rules, adjacency graph and placement rationale that justify the colony form, room grammar, and optional-mod placement. Build scenes like believable colonies: use named pawns, sensible materials, finished rooms, floors, lighting, decor, power/plumbing, stocked storage, linked workbenches, short paths, and incidental clutter. Do not show test labels, debug windows, selection brackets, learning-helper overlays, sterile empty boxes, or fixture names. Use the Gateway's camera-centered crop so framing does not select the subjects.
12. Capture both a lossless full-detail still and raw PNG frames for a GIF candidate when requested. Keep GIFs at most five seconds; prefer a fixed camera and hard cuts between useful beats over panning or showing dead walking time. For hard cuts, retain every v1 segment under `<showcase>/segments/`, capture all segments from the same exact held PID/start/run and package build, and assemble with `scripts/Invoke-RimWorldShowcaseAssembly.ps1`; the exact ordered segment-beat union must equal the showcase declaration. Never splice different launches, invented transition frames, or direct-state outcomes. Encode below Steam's 1 MiB preview ceiling, retain source-frame timing/crop hashes, and compare the still and GIF at actual Workshop display size. A GIF that becomes illegible under the limit should not replace the clearer screenshot.

### Human-deferred showcases

When a mod's showcase manifest says `human-deferred`, agents must stop at the authored brief. Do not
arrange scenes, capture frames, repair partial showcase media, promote earlier agent captures, or include
showcase slots in a publication plan. The release plan must carry the deferral state, empty showcase
design/capture evidence arrays, and only the independently reviewed non-showcase presentation inventory.
Human-produced media can enter a later release only after the user explicitly lifts the deferral and the
normal review/provenance gates are satisfied. Immersive Chefs is currently human-deferred.

## Publish deliberately

1. Use the implemented Gateway publisher as the primary path only from a fresh isolated RimWorld process with Steam initialized. SteamCMD may be a documented recovery path, but it is not equivalent Gateway evidence.
2. Address an existing declared Workshop item by default. A first publication is allowed only when the manifest explicitly opts in, the mutation-free dry-run has no item ID, the user confirms that exact creation, no prior receipt or local identity exists, and one bounded native query proves the owning account has no exact-title item. Regardless of an eventual Public declaration, force that first submission to Private so the user can inspect it; promotion is a separate reviewed update of the retained ID. Persist the returned nonzero ID before continuing; immediately after a successful first release, write and commit the same ID in both the release descriptor and `About/PublishedFileId.txt`, disable first publication, and make later staging fail closed if those tracked identities differ. The uploaded/subscribed package must carry that same About identity so RimWorld and Steam treat local and Workshop copies as one mod. Never delete ignored release state as a substitute for that tracked transition.
3. Run a mutation-free dry-run and show the remote item, meaningful metadata/dependency diff, player-facing content summary, visibility, and change note to the user. Keep exact content and presentation digests in the evidence; do not make the user validate, repeat, or copy hashes or a tool confirmation phrase.
   Every update after initial publication needs a short authored change note describing the visible player-facing change. Reject empty, generic (`update`, `fixes`, `various changes`), automatically synthesized, or unchanged notes. Bind the exact note to the immutable dry-run and pass it to Steam's `SubmitItemUpdate`; record it in the receipt and verify the corresponding Workshop change-note entry after propagation.
4. Obtain one explicit natural-language user authorization for that release intent. Treat it as covering the existing item's required preview synchronization, byte-verified Steam URL resolution, deterministic provenance update, final metadata/content submission, dependency reconciliation, and subscribed-copy verification. Pass any exact CLI confirmation phrase internally. Do not ask again merely because Steam assigned new image URLs, the provenance/plan hash changed during that required finalization, or a clean bookkeeping commit was needed. Require the tool's authorization-intent lineage to prove that every committed input except the generated resolved-description/provenance pair is unchanged. Reconfirm only when the Workshop item, visibility, dependencies, change note, user-authored copy, local image bytes/order, product code/XML/assets/packaging inputs, or requested scope changes—or when another Steam submission would be required.
5. Observe progress and the terminal Steam callback. Report legal-agreement, authentication, quota, connectivity, and indeterminate-callback states without blind retries or false rollback claims.
6. Query the resulting remote metadata/previews and reacquire the published item into an ignored verification directory. Compare it with the staged file manifest before calling publication verified.

For the current Immersive Chefs 1.6 bootstrap, stage the clean committed candidate with
`.\scripts\Build-ImmersiveChefsRelease.ps1 -Output json`. After personally reviewing the emitted
`publication-plan.json`, invoke `.\scripts\Invoke-ImmersiveChefsWorkshopRelease.ps1` only with that
file's exact SHA-256 and pass the exact confirmation phrase printed by its help internally; never ask
the user to recite that implementation detail. When preview synchronization is required, verify that
the final description resolves only the same reviewed local image hashes/order to Steam-returned URLs,
then continue under the existing authorization without another user prompt. The publisher revalidates
the source and presentation, proves exact-title absence on the owning account before first creation, uploads through RimWorld's initialized Steamworks session, queries the
remote title/description/tags/preview/owner/dependency graph, subscribes the exact item, and compares
the downloaded package. It then removes the repository-local product from RimWorld's discovery path
under a recoverable exact-path move, launches the subscribed Workshop copy, invokes the checked-in
native Prioritize and Consume float-menu workflow, captures before/cooking/plated/dining/dirty-ware evidence, restores the local
mod, and only then retains a token-free receipt. The publisher durably records creation/submission
admission and reconciles an indeterminate submit by querying Steam; never delete that ignored state
to force a second CreateItem. After successful first-publication verification, update
`mods/ImmersiveChefs/Release/release.json` with the returned `publishedFileId`, set
`allowFirstPublication` to `false`, rerun its focused descriptor checks, and commit that tracked
identity before declaring release administration complete.

## Preserve evidence and clean up

Keep source revision/dirty policy, manifest and tool hashes, depot/file identities, per-target builds, the scenario impact map, assertion results, required manual screenshot reviews, package and presentation digests, operator confirmation, Gateway process identity, Steam result, remote verification, and cleanup together. Exclude passwords, Steam Guard codes, downloader sessions, bearer tokens, and live Gateway discovery files.

Request graceful process shutdown first, use exact-PID fallback only when required, release cache/stage leases, and confirm the user's normal configuration hash is unchanged. Never claim an accepted Workshop update was rolled back automatically; recovery is an explicit reviewed republish of a prior verified candidate.

If the user stops or forbids a verification profile, record the directive, the graceful/exact-PID shutdown result, and the final zero-process check. Keep that profile disabled for the remainder of the release unless the user later gives fresh explicit authorization; do not treat an authorized minimized Gateway profile as permission to revive a stopped product-only profile.

## Pause conditions

- Pause before publishing only when the user has not authorized the current meaningful release intent, or when a material item/content/dependency/visibility/scope change falls outside that authorization. Do not pause for deterministic preview URL/provenance finalization or ask the user to repeat hashes or magic phrases.
- Leave the target unsupported if its exact Steam manifest cannot be acquired and verified.
- Leave gameplay compatibility incomplete when an impacted native player workflow lacks its assertions or a new/materially changed player-visible scenario lacks required manual review.
- Leave presentation incomplete when rendered assets or inline hosting have not been reviewed and proven.
- Preserve an indeterminate post-submit operation for diagnosis; do not start another update until remote state resolves it.
- Stop if required implementation tasks or commands do not exist. Report the exact OpenSpec task/blocker instead of bypassing the contract manually.
