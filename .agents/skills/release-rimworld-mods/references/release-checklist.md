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
4. Native player-workflow acceptance on each claimed exact target with personally inspected screenshots.
5. Generated, validated, deterministic, and personally reviewed Workshop presentation.
6. Positive-allowlist package manifest and immutable candidate/presentation digests.
7. Independent review findings resolved and affected verification repeated.

## Publication gates

- Existing Workshop ID only; never create implicitly.
- Fresh isolated exact-PID RimWorld + Gateway process with initialized Steam.
- Mutation-free dry-run and explicit user confirmation bound to exact hashes.
- One update operation at a time; no blind retry after submission uncertainty.
- Check every SteamUGC setter, submit result, legal-agreement flag, and remote identity.
- Verify remote metadata/previews and reacquired package contents after propagation.
- Preserve a secret-free receipt and exact cleanup outcome.

## Current implementation status

The architecture is specified under `openspec/changes/automate-multiversion-mod-releases/`. Until its unchecked tasks implement and test a phase, this skill is planning/guardrail documentation for that phase—not evidence that a release CLI or Gateway publishing endpoint exists. Inspect `tasks.md`, repository scripts, and command help on every invocation.
