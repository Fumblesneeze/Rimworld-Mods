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
2. Run every target's declared scenario in a fresh exact-version RimWorld process with unique savedata and the complete ordered mod list.
3. Exercise the native player workflow and personally inspect before/action/after screenshots from that exact process. Compilation, startup, logs, Gateway JSON, direct state mutation, and static end-state screenshots are supporting evidence only.
4. Retain at least one product acceptance run without the Gateway. Preserve the normal `ModsConfig.xml` before/after hash and exact-PID cleanup evidence.
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
6. Use wide visual headers/feature cards to break up dense inventories. For the proven Immersive Chefs layout, author 1164×655 cards and keep every Steam additional preview below 1 MiB. Use a checked-in card manifest, reusable template, repository-local licensed font, and actual shipped sprites; review one contact sheet plus every important full-resolution card. Avoid unexplained status affordances such as checkmarks, warning badges, toggles, or progress marks: feature art should showcase behavior, not look like a task tracker.
7. Treat image hosting as a reviewed identity problem. Synchronize ordered additional previews first, query the resulting Steam URLs/original names, resolve only those URLs into the final BBCode, then publish metadata/content. Bind local hashes, remote indices, and URLs in evidence. Never put unresolved tokens on the live page or silently use an anonymous/ad-supported image host.
8. End distributable-mod copy with the user-reviewed `Author's Note` when requested. It may disclose AI-supervised development, frameworks, testing, playtesting limits, and feedback expectations in the author's voice; nothing follows it. State whether the shipped mod performs live AI generation.
9. When a mod distinguishes especially valuable optional integrations, put a concise `Recommended Mods` section immediately before the full optional catalog. Link exact Workshop pages when the description budget permits, state any framework chain in player language, and remove those entries from the optional list instead of repeating them.
10. Declare gameplay showcases per mod instead of improvising them at capture time. Each declaration names exact presentation-only package groups, natural scene dressing, the native player workflow, visible beats, requested still/GIF formats, and carousel order. Presentation-only apparel or scenery mods are not product dependencies.
11. Build showcase scenes like believable colonies: use named pawns, sensible materials, finished rooms, floors, lighting, decor, power/plumbing, stocked storage, linked workbenches, short paths, and incidental clutter. Do not show test labels, debug windows, selection brackets, learning-helper overlays, sterile empty boxes, or fixture names. Use the Gateway's camera-centered crop so framing does not select the subjects.
12. Capture both a lossless full-detail still and raw PNG frames for a GIF candidate when requested. Keep GIFs at most five seconds; prefer a fixed camera and hard cuts between useful beats over panning or showing dead walking time. Encode below Steam's 1 MiB preview ceiling, retain source-frame timing/crop hashes, and compare the still and GIF at actual Workshop display size. A GIF that becomes illegible under the limit should not replace the clearer screenshot.

## Publish deliberately

1. Use the implemented Gateway publisher as the primary path only from a fresh isolated RimWorld process with Steam initialized. SteamCMD may be a documented recovery path, but it is not equivalent Gateway evidence.
2. Address an existing declared Workshop item by default. A first publication is allowed only when the manifest explicitly opts in, the mutation-free dry-run has no item ID, the user confirms that exact creation, no prior receipt or local identity exists, and one bounded native query proves the owning account has no exact-title item. Regardless of an eventual Public declaration, force that first submission to Private so the user can inspect it; promotion is a separate reviewed update of the retained ID. Persist the returned nonzero ID before continuing; immediately after a successful first release, commit it to the release descriptor and disable first publication so every clone and later release is update-only. Never delete ignored release state as a substitute for that tracked transition.
3. Run a mutation-free dry-run and show the exact remote item, metadata diff, content/presentation digests, and change note to the user.
   Every update after initial publication needs a short authored change note describing the visible player-facing change. Reject empty, generic (`update`, `fixes`, `various changes`), automatically synthesized, or unchanged notes. Bind the exact note to the immutable dry-run and pass it to Steam's `SubmitItemUpdate`; record it in the receipt and verify the corresponding Workshop change-note entry after propagation.
4. Obtain explicit user confirmation for that dry-run. Confirm using its bound nonce/digests; changed files, presentation, or remote state require a new dry-run and review.
5. Observe progress and the terminal Steam callback. Report legal-agreement, authentication, quota, connectivity, and indeterminate-callback states without blind retries or false rollback claims.
6. Query the resulting remote metadata/previews and reacquire the published item into an ignored verification directory. Compare it with the staged file manifest before calling publication verified.

For the current Immersive Chefs 1.6 bootstrap, stage the clean committed candidate with
`.\scripts\Build-ImmersiveChefsRelease.ps1 -Output json`. After personally reviewing the emitted
`publication-plan.json`, invoke `.\scripts\Invoke-ImmersiveChefsWorkshopRelease.ps1` only with that
file's exact SHA-256 and the exact confirmation phrase printed by its help. The publisher revalidates
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

Keep source revision/dirty policy, manifest and tool hashes, depot/file identities, per-target builds, tests, player actions/screenshots, package and presentation digests, operator confirmation, Gateway process identity, Steam result, remote verification, and cleanup together. Exclude passwords, Steam Guard codes, downloader sessions, bearer tokens, and live Gateway discovery files.

Request graceful process shutdown first, use exact-PID fallback only when required, release cache/stage leases, and confirm the user's normal configuration hash is unchanged. Never claim an accepted Workshop update was rolled back automatically; recovery is an explicit reviewed republish of a prior verified candidate.

## Pause conditions

- Pause before publishing if the user has not explicitly confirmed the exact dry-run.
- Leave the target unsupported if its exact Steam manifest cannot be acquired and verified.
- Leave gameplay compatibility incomplete when a native player workflow or personally inspected live evidence is missing.
- Leave presentation incomplete when rendered assets or inline hosting have not been reviewed and proven.
- Preserve an indeterminate post-submit operation for diagnosis; do not start another update until remote state resolves it.
- Stop if required implementation tasks or commands do not exist. Report the exact OpenSpec task/blocker instead of bypassing the contract manually.
