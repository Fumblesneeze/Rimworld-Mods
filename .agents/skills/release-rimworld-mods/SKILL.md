---
name: release-rimworld-mods
description: Prepare, verify, and publish reproducible RimWorld mod releases across major/minor game versions. Use when planning or operating release manifests, Steam depot acquisition and caching, version-isolated builds, generated Workshop descriptions or graphics, major/minor regression runs, staged packages, or guarded Steam Workshop updates in this repository.
---

# Release RimWorld Mods

## Overview

Build a release for declared major/minor Steam branches without committing game-version source trees or compiled assemblies. Keep acquisition, compilation, player-workflow verification, presentation rendering, and Workshop publication connected by one immutable evidence chain.

Read [references/release-checklist.md](references/release-checklist.md) before changing release contracts or operating a release. Also read the repository's `rimworld-mod-development` skill and its routed references because its build, package, live-run, and acceptance gates remain mandatory.

## Establish scope and readiness

1. Inspect root and local `AGENTS.md`, the selected mod, its package ID, its release manifest, and the global target catalog.
2. Identify the owning OpenSpec change and read its proposal, design, capability spec, and tasks. For the planned pipeline, use `openspec/changes/automate-multiversion-mod-releases/`.
3. Check task state and the actual CLI help/scripts before naming commands. While the implementation tasks are incomplete, do not invent a working release or publication command; limit work to the implemented phases and report the missing phase precisely.
4. Keep out-of-band release work isolated from active gameplay work. Never stage, rewrite, or commit another agent's files.
5. Refuse publication from an uncommitted or dirty candidate. A dirty override may create a non-publishable local candidate only when the implemented tool explicitly supports it.

## Resolve major/minor game targets

release/rimworld-targets.json declares only major/minor IDs, owned external cache paths and rolling Steam branches. Game content stays in F:\RimWorldVersions (override with RIMWORLD_RELEASE_CACHE or cacheRoot).

Only major/minor RimWorld selectors are allowed, such as 1.6 and 1.7. Patch/revision numbers, Steam build IDs, depot manifest IDs and game-file hashes must never gate acquisition, builds, runs or publication. Strict catalog/profile/operation validation rejects those fields. Rolling branch downloads can refresh the same owned external cache. Full observed game details may be retained as evidence only. Tool/dependency versions and immutable mod package/source/image hashes remain separate concerns.

Use game_depot_download with downloaderPath, targetDirectory, targetId (1.6 or 1.7), and accountName. It resolves the rolling version-1.6 or unstable branch and can refresh the same owned cache. For initial authentication use interactiveLogin:true and enter password/Steam Guard directly in the console. The -remember-password option stores the token in Windows per-user isolated storage outside the repo; subsequent downloads use interactiveLogin:false. A revoked or expired token requires signing in again. game_target_validate checks ownership, required layout and major/minor compatibility. Never use Steam-managed or Workshop directories.

Use game_target_validate, then release_matrix_build (or matrix-aware mod_build), then release_target_test for each target. No historical manifest lookup or snapshot approval is required. Prepared native verification binds the project owner and active groups to the requested product and compiles tests against its frozen target DLL; it must not rebuild that product through a ProjectReference.

## Build and stage

1. Run a clean SDK build separately for every declared target, with unique managed-input, intermediate, and output roots.
2. Confirm that generated RimWorld-version folders exist only under ignored release output. Never create version-named source folders or commit generated DLLs.
3. Merge only successful verified targets. Generate the root About metadata from those targets and stage the product package from a positive allowlist.
4. Reject source, symbols, caches, credentials, tests, undeclared content, and Dev Gateway assemblies in product packages.
5. Record a sorted file manifest and candidate digest. Keep the candidate immutable from review through remote verification.

## Prove compatibility

1. Run focused and owning tests, guarded repository tests, Release builds, strict OpenSpec validation, and package inspection.
2. Run every target's declared scenario in a fresh RimWorld process for that major/minor with unique savedata and the complete ordered mod list.
3. Exercise the native player workflow and personally inspect before/action/after screenshots from that exact process. Compilation, startup, logs, Gateway JSON, direct state mutation, and static end-state screenshots are supporting evidence only.
4. Gateway-based native player workflows satisfy live release acceptance; a separate run without the Gateway is not required. Preserve the normal `ModsConfig.xml` before/after hash and exact-PID cleanup evidence. Verify product independence through dependency metadata, package contents, and assembly references.
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
8. Keep inline image hosting independent of the Steam slideshow. Declare inline files in
   `description-images.json` with repository paths, full-commit `raw.githubusercontent.com` URLs
   and SHA-256 hashes. The agent must commit and push images, verify the GitHub mirror, and commit
   the resolved description before page publication. Release tools must never commit or push image
   changes. `release_page_update` verifies committed and remote bytes, then submits page metadata,
   primary preview and slideshow together; no preliminary Steam image upload is needed. The
   slideshow inventory may differ from the inline inventory. Keep human-deferred media excluded.

9. Query public Workshop change history with bounded ordinary browser request headers. Steam may throttle an obvious custom verifier user agent while serving the same page to a normal browser-shaped request. Keep the request read-only, unauthenticated, capped in bytes/time, and retain the explicit-429-only retry ceiling; never skip or fabricate pre/post change-note verification because one client fingerprint was throttled.
8. End distributable-mod copy with the user-reviewed `Author's Note` when requested. It may disclose AI-supervised development, frameworks, testing, playtesting limits, and feedback expectations in the author's voice; nothing follows it. State whether the shipped mod performs live AI generation.
9. When a mod distinguishes especially valuable optional integrations, put a concise `Recommended Mods` section immediately before the full optional catalog. Link exact Workshop pages when the description budget permits, state any framework chain in player language, and remove those entries from the optional list instead of repeating them.
10. Use Steam's native Links metadata only for fields its player-facing Workshop editor actually exposes: `facebook`, `twitter`, `youtube`, `polycount`, `reddit`, and `sketchfab`. Steam has no GitHub/custom Links field; do not pretend generic SteamUGC key/value tags are visible links, do not put a GitHub URL into a misleading social field, and do not fall back to the BBCode description without explicit author approval. Keep `workshopLinks` empty when none of the six fields applies. Release preparation must reject unsupported keys before mutation, preserve unrelated supported remote fields, and verify each declared supported field after publication. An already-mutated immutable plan may recover without resubmission when an unsupported key is absent or retained with its exact URL as generic metadata, but the receipt must explicitly say it is not player-facing; conflicts and duplicates remain fatal.
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

Use `release_package_upload` for the exact already-built and tested canonical package, and
`release_page_update` for the page. The latter requires an existing Workshop ID, never uploads
content, and never consumes the package change note. A first package upload creates a Private
draft and backfills the ID; promotion is a separate page update. Both actions retain immutable
plans and shared recovery protection. For an explicit dry-run, `release_prepare` accepts
`action: package|page`, followed by `release_publish`. Publishing does not rebuild the package.
The isolated Gateway publisher runtime can differ from the target of an already accepted package;
this does not claim that the product supports that publisher runtime.

Keep local installation and Workshop publication semantically distinct. An instruction to install,
deploy, or copy a mod means synchronizing the reviewed package to RimWorld's canonical local
`Mods/<package-id>` folder; it never authorizes `SubscribeItem`. The release pipeline itself never
subscribes to a repository-owned item, moves the local package, or launches a gameplay smoke. If an
explicit standalone diagnostic ever needs a subscribed copy, treat that as a separate operation with
guaranteed unsubscribe/restore cleanup, never as a publication gate.

1. Use the implemented Gateway publisher as the primary path only from a fresh isolated RimWorld process with Steam initialized. SteamCMD may be a documented recovery path, but it is not equivalent Gateway evidence.
2. Address an existing declared Workshop item by default. A first publication is allowed only when the manifest explicitly opts in, the mutation-free dry-run has no item ID, the user has explicitly ordered publication and the prepared title/item/visibility/dependency scope matches that order, no prior receipt or local identity exists, and one bounded native query proves the owning account has no exact-title item. Do not ask for a second confirmation merely because preparation produced a digest or nonce. Regardless of an eventual Public declaration, force that first submission to Private so the user can inspect it; promotion is a separate reviewed update of the retained ID. Persist the returned nonzero ID before continuing; immediately after a successful first release, write and commit the same ID in both the release descriptor and `About/PublishedFileId.txt`, disable first publication, and make later staging fail closed if those tracked identities differ. The uploaded package must carry that same About identity so RimWorld and Steam treat local and Workshop copies as one mod. Never delete ignored release state as a substitute for that tracked transition.
3. Run a mutation-free dry-run and retain the exact remote item, metadata diff, content/presentation digests, and change note. Report them to the user without pausing an already authorized publish workflow.
   Every package update after initial publication needs a short authored change note describing the visible player-facing change. Reject empty, generic (`update`, `fixes`, `various changes`), automatically synthesized, or unchanged notes. Bind the exact note to the immutable dry-run and pass it to Steam's `SubmitItemUpdate`; record it in the receipt and verify the corresponding Workshop change-note entry after propagation. Page-only updates do not consume a package change note.
4. Feed the dry-run's bound nonce and digests directly into publication when an explicit publish order is already in scope. Ask only when no publish order exists or the prepared title, target item, visibility, dependency scope, or other material mutation differs from that order. Changed files, presentation, or remote state require a new dry-run and revalidation against the standing order, not a redundant prompt.
5. Observe progress and the terminal Steam callback. Report legal-agreement, authentication, quota, connectivity, and indeterminate-callback states without blind retries or false rollback claims.
6. Query the resulting remote metadata/previews and the exact item's Steam `time_updated`. Publication
   is verified only when the remote graph matches the admitted plan. Package uploads require a nonzero
   modified time strictly newer than the frozen baseline. Steam can retain the content timestamp for
   page-only updates: accept equality only after the exact new plan metadata, description, images and
   relationships match and content identity/size remain unchanged. Reject zero or regressed timestamps.
   Derive the action from the hash-verified plan, including when migrating previous package history;
   never let a receipt choose its own weaker timestamp rule. Page receipts never become package history.
7. Treat a release trigger as an assertion that the candidate was already tested. Keep mod-specific
   cooking, dining, UI, compatibility, and other native acceptance workflows in their owning
   pre-release test operations; never encode them in `release.json` or dispatch them from
   `release_publish`.

## Preserve evidence and clean up

Keep source revision/dirty policy, manifest and tool hashes, observed game details, per-target builds, tests, player actions/screenshots, package and presentation digests, publication authorization, Gateway process identity, Steam result, remote verification, and cleanup together. Exclude passwords, Steam Guard codes, downloader sessions, bearer tokens, and live Gateway discovery files.

Request graceful process shutdown first, use exact-PID fallback only when required, release cache/stage leases, and confirm the user's normal configuration hash is unchanged. Never claim an accepted Workshop update was rolled back automatically; recovery is an explicit reviewed republish of a prior verified candidate.

## Pause conditions

- Pause before publishing only when the user has not explicitly ordered publication or the prepared material mutation differs from that order. Never require a second digest/nonce confirmation for an already authorized matching plan.
- Leave the target unsupported if its major/minor branch cannot be acquired or verified.
- Leave gameplay compatibility incomplete when a native player workflow or personally inspected live evidence is missing.
- Leave presentation incomplete when rendered assets or inline hosting have not been reviewed and proven.
- Preserve an indeterminate post-submit operation for diagnosis; do not start another update until remote state resolves it.
- Stop if required implementation tasks or commands do not exist. Report the exact OpenSpec task/blocker instead of bypassing the contract manually.
