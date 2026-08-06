> **OUT-OF-BAND COORDINATION MARKER:** This change is reserved for release research and design outside the active Immersive Chefs implementation stream. Do not implement its tasks concurrently.

## Why

The repository needs reproducible multi-version releases without committing RimWorld-version source trees or compiled assemblies. Release presentation and Steam Workshop publication also need one reviewed, repeatable path instead of hand-edited About text, banners, descriptions, and uploads.

## What Changes

- Add a per-mod release manifest that declares supported RimWorld versions, exact game build inputs, package layout, presentation inputs, Workshop identity, and required verification profiles.
- Acquire exact RimWorld managed assemblies from Steam into an ignored content-addressed cache and compile every supported target independently against its own assembly set.
- Generate versioned folders only inside ignored release artifacts; keep repository source and compiled DLLs version-neutral and uncommitted.
- Allow isolated scenario/E2E runs to select and launch an exact cached RimWorld build for regression testing.
- Render the Steam Workshop description separately from the in-game `About/About.xml`, including deterministic text banners and content/mechanics graphics composed from templates, mod sprites, and authored copy.
- Add a typed, authenticated Dev Gateway publication operation that uses RimWorld's initialized Steam integration to upload the reviewed staged package and presentation assets without depending on the incomplete in-game upload UI.
- Record immutable build, dependency, verification, presentation, and publication evidence while keeping Steam credentials and downloaded proprietary game content out of the repository and release package.
- Specify developer/release infrastructure only; this change does not implement or alter shipping gameplay behavior.

## Affected Mods

- **RimWorld Dev Gateway** — package ID `fumblesneeze.rimworlddevgateway`; repository path `mods/RimWorldDevGateway`; owning mod.

## Capabilities

### New Capabilities

- `dev-gateway-mod-release-automation`: Exact-version dependency acquisition, build/test matrices, generated Workshop presentation, staged release validation, and guarded Steam publication.

### Modified Capabilities

None.

## Impact

This will add repository-local release manifests, ignored caches/artifacts, host tooling and tests, presentation templates/assets, version-selectable isolated runners, Gateway API/runtime publication support, documentation, and a dedicated release skill. Product mods remain independent of `fumblesneeze.rimworlddevgateway`; neither the Gateway nor cached RimWorld binaries may enter a product release.
