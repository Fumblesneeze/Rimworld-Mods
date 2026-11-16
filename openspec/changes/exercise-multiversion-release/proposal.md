## Why
The user requested the first real combined Immersive Chefs 1.6/1.7 release on 2026-11-16. The early multi-version release design was never implemented beyond single-version publication.

## What Changes
- Add typed DepotDownloader onboarding/acquisition and exact external-cache validation.
- Add independently built version targets and a combined positive-allowlist package.
- Derive supported versions, platform reference checks and Workshop tags from the declared matrix.
- Keep acquisition, build, gameplay verification, package upload and page update separate.

## Owner
Repository tooling: `RimWorldModding.Mcp`.

## Capabilities
### New Capabilities
- `versioned-release-matrix`: Exact external Steam inputs and independent builds merged into one package.

## Impact
Implements the acquisition/build subset of `automate-multiversion-mod-releases` through the current MCP operation registry, superseding its unimplemented standalone CLI direction. Product compatibility changes and native acceptance belong to `release-immersive-chefs-multiversion`.
