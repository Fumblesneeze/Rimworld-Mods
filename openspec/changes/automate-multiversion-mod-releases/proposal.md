> **OUT-OF-BAND COORDINATION MARKER:** This change is reserved for release research and design outside the active Immersive Chefs implementation stream. Do not implement its tasks concurrently.

## Why

The repository needs reproducible multi-version releases without committing RimWorld-version source trees or compiled assemblies. Release presentation and Steam Workshop publication also need one reviewed, repeatable path instead of hand-edited About text, banners, descriptions, and uploads.

## What Changes

- Add a per-mod release manifest that declares the authoritative supported RimWorld versions, current development target, required and optional mod relationships, exact game build inputs, package layout, presentation inputs, Workshop identity, and required verification profiles.
- Acquire exact RimWorld managed assemblies from Steam into an ignored content-addressed cache and compile every supported target independently against its own assembly set.
- Generate versioned folders only inside ignored release artifacts; keep repository source and compiled DLLs version-neutral and uncommitted.
- Keep new feature code on the unguarded current-version path while compiling narrow legacy C# compatibility seams with generated symbols such as `RIMWORLD1_6`; generate target-specific XML projections from canonical XML plus declared legacy overrides when engine data shapes diverge.
- Allow isolated scenario/E2E runs to select and launch an exact cached RimWorld build for regression testing.
- Render the Steam Workshop description separately from the in-game `About/About.xml`, including required/optional mod sections, deterministic text banners, and content/mechanics graphics composed from templates, mod sprites, and authored copy.
- Require an authored player-facing Steam change note for every incremental publication, and let each mod declare a reviewed ordered set of additional-preview showcases made from real in-game screenshots and short GIFs.
- Generate required-mod entries in packaged RimWorld metadata and add a typed, authenticated Dev Gateway publication operation that uses RimWorld's initialized Steam integration to upload the reviewed staged package, presentation assets, and exact Steam required-item relationships without depending on the incomplete in-game upload UI.
- Record immutable build, dependency, verification, presentation, and publication evidence while keeping Steam credentials and downloaded proprietary game content out of the repository and release package.
- Classify each mod as distributable or development-only and reject a distributable candidate unless its complete player-facing string inventory has valid English, German, Spanish, French, Simplified Chinese, and Russian catalogs with matching placeholders and tags; development-only Gateway text is exempt.
- Specify developer/release infrastructure only; this change does not implement or alter shipping gameplay behavior.

## Affected Mods

- **RimWorld Dev Gateway** — package ID `fumblesneeze.rimworlddevgateway`; repository path `mods/RimWorldDevGateway`; owning mod.

## Capabilities

### New Capabilities

- `dev-gateway-mod-release-automation`: Exact-version dependency acquisition, build/test matrices, generated Workshop presentation, staged release validation, and guarded Steam publication.

### Modified Capabilities

None.

## Impact

This will add repository-local release manifests, ignored caches/artifacts, generated compile symbols and XML projections, dependency-aware About/Workshop metadata, host tooling and tests, presentation templates/assets, version-selectable isolated runners, Gateway API/runtime publication support, documentation, and a dedicated release skill. Product mods remain independent of `fumblesneeze.rimworlddevgateway`; neither the Gateway nor cached RimWorld binaries may enter a product release.
