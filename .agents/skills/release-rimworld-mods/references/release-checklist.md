# Release invariants and artifact model

Use this reference with the repository root instructions and the active OpenSpec release contract. It records stable decisions; the OpenSpec tasks and actual command help determine what is implemented today.

## Ownership and source boundaries

- Owning infrastructure mod: RimWorld Dev Gateway (`fumblesneeze.rimworlddevgateway`) at `mods/RimWorldDevGateway`.
- Product mods never reference or package the Gateway.
- Shared source stays version-neutral. RimWorld-version folders and compiled assemblies are generated only in ignored artifacts and the uploaded package.
- RimWorld binaries and Steam session material remain external, ignored, and undistributed.

## Planned repository inputs

| Input | Purpose |
| --- | --- |
| `release/rimworld-targets.yaml` | Stable target IDs, exact depot/manifest set, selected compile files, approved hashes |
| `mods/<ModName>/Release/release.yaml` | Package/Workshop identity, supported targets, file allowlists, verification and presentation policy |
| `release/templates/` | Reusable BBCode and graphic layouts plus licensed pinned fonts where applicable |
| `mods/<ModName>/Release/workshop/` | Mod-owned copy, sprite mappings, image ordering, and presentation definitions |

Do not create these from guesswork. Follow the schemas and onboarding process after their OpenSpec tasks are implemented.

## Planned generated layout

```text
artifacts/Releases/<package-id>/<run-id>/
  targets/<target-id>/
  package/<package-id>/<rimworld-version>/Assemblies/
  presentation/
    description.bbcode
    preview.html
    preview.png
    previews/
    provenance.json
  evidence/
    dependencies.json
    builds.json
    package-files.json
    verification.json
    publication.json
```

The exact schema may evolve through the OpenSpec change. All paths above are ignored generated output, not source directories.

## Dependency identities

- Compilation cache identity: app ID + depot ID + exact manifest ID + platform/architecture + normalized selected-file-set hash.
- Full game cache identity: ordered base/DLC depot and exact-manifest set + platform/architecture.
- Local SHA-256 indexes detect corruption and tie evidence to the files actually used.
- External RimSort/version catalogs are discovery inputs only. A target becomes releasable only after its IDs, `Version.txt`, accessibility, and managed-file hashes are pinned and reviewed here.

## Candidate gates

A publishable candidate has all of the following:

1. Clean committed source revision and validated release/catalog manifests.
2. Fresh isolated build for every supported target.
3. Focused, owning, guarded, Release-build, OpenSpec, and package checks.
4. Native player-workflow assertions on each impacted exact target, plus personally inspected screenshots only for new or materially changed player-visible scenarios; retain the impact map for unchanged regressions.
5. Generated, validated, deterministic, and personally reviewed Workshop presentation.
6. Positive-allowlist package manifest and immutable candidate/presentation digests.
7. Independent review findings resolved and affected verification repeated.

## Publication gates

- Existing Workshop ID by default; never create implicitly. First publication requires an explicit manifest flag, an ID-less reviewed dry-run, exact user confirmation, no prior identity/receipt, a complete native owner scan proving exact-title absence, Private visibility for the first submission, and durable persistence of Steam's returned ID before upload continuation. Public/Friends/Unlisted promotion is a later reviewed update.
- Workshop copy is a player-facing showcase, never a pasted technical design. Keep complete mechanics/things/buildings/compatibility coverage, but describe visible behavior and benefits in plain language.
- A `Recommended Mods` tier is still optional metadata: put it immediately above the broader optional list, link exact Workshop items when bytes permit, retain prerequisite guidance, and do not duplicate its entries below.
- Wide inline cards use versioned templates, local licensed fonts, shipped sprites, fixed dimensions, sub-1-MiB files, and a personally inspected contact sheet. Reconcile them as additional previews, query Steam URLs, then resolve the final BBCode. Retain local hash ↔ remote index/URL evidence.
- Per-mod gameplay showcase declarations identify exact optional presentation packages, natural scene art direction, ordinary player workflows, visible beats, formats, and order. Before arranging geometry, use `rimworld-realistic-base-generation` and create a per-showcase design record containing the brief, eligible reference IDs, classified rules, adjacency graph and placement rationale. Capture without selecting subjects; retain one high-detail still plus raw frame/timing/crop evidence for each requested GIF. Keep GIFs at most five seconds and under 1 MiB, use fixed views and hard cuts, and reject synthetic test-fixture presentation.
- A `human-deferred` showcase inventory is a brief only. Agents must not arrange, capture, repair,
  promote, or publish its media. Its publication plan carries no showcase evidence or preview slots;
  independently reviewed feature cards remain eligible presentation inputs.
- Put any author-requested AI/process disclosure in the final `Author's Note`; no content follows it. Be candid about playtesting limits and live-AI behavior.
- Fresh isolated exact-PID RimWorld + Gateway process with initialized Steam.
- Mutation-free dry-run and one explicit natural-language user authorization for the meaningful release intent. Keep exact hashes and the CLI confirmation phrase internal. The same authorization covers byte-verified preview synchronization, Steam URL/provenance resolution, final submission, dependency reconciliation, and remote verification when item, visibility, dependency graph, change note, authored copy, local image bytes/order, product code/XML/assets/packaging inputs, and requested scope do not change. Enforce that claim with the authorization-intent lineage; exclude only the generated resolved-description/provenance pair from its committed source-tree hash.
- A non-initial update has one specific player-facing change note bound to the plan, submitted through Steam's change-note parameter, retained in the receipt, and checked on the remote change history. Blank, generic, synthesized, or recycled notes are rejected.
- One update operation at a time; no blind retry after submission uncertainty.
- A first-publication ID is durable across candidates, every existing-item update passes an exact
  Steam ID/owner/app/title query first, and an admitted-but-indeterminate submit is query-reconciled
  before any further mutation.
- Check every SteamUGC setter, submit result, legal-agreement flag, and remote identity.
- Verify remote identity/owner/app/visibility, metadata, description, tags, dependency graph, preview inventory/bytes, and change history after propagation.
- Do not subscribe, reacquire, or launch the published mod after an ordinary publication. Use the immutable staged candidate and impact-selected pre-publication gameplay evidence instead.
- Run subscribed-copy package comparison and native gameplay smoke only as explicit validation when the release publisher, Steam subscription/install verification, or related Gateway tooling itself changed. Never make it a routine product-release gate.
- Use the Dev Gateway as the default native-workflow controller. A Gateway-free duplicate is optional
  and requires an explicit request or a recorded Gateway capability gap.
- Treat a user-stopped verification profile as disabled for that release. Record exact-process shutdown
  and the zero-process check; never relaunch it without fresh explicit authorization.
- Preserve a secret-free receipt and exact cleanup outcome.
- For the exceptional release-tooling-change subscriber check, keep evidence under a short repository-local ignored root and preflight the projected Gateway session/temp path before launching RimWorld.
- After a verified first publication, commit the returned ID to the mod's release descriptor and disable first publication. Ignored artifacts are recovery evidence, never the cross-clone identity authority.

## Current implementation status

The architecture is specified under `openspec/changes/automate-multiversion-mod-releases/`. The
Immersive Chefs 1.6 bootstrap currently implements clean positive-allowlist staging with
`scripts/Build-ImmersiveChefsRelease.ps1` and a guarded Steamworks publish/query/remote-verification flow with
`scripts/Invoke-ImmersiveChefsWorkshopRelease.ps1`. Broader target-catalog, historical-build,
presentation-compiler, and reusable multi-mod release tasks remain unchecked; do not imply that this
one-mod bootstrap implements them. Inspect `tasks.md`, repository scripts, and command help on every
invocation.
