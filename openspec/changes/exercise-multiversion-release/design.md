## Context and owner
Owner: repository tooling `RimWorldModding.Mcp`. The 2026-11-16 user request explicitly activates multi-version implementation; the old out-of-band design marker does not block this work.

## Decisions
Use DepotDownloader 3.4.0 from its official release, verified against the published winget SHA-256. Keep binaries, Steam account/session state and proprietary content in an external cache. QR authentication may open an interactive console; no password, code or token is accepted in operation arguments or stored in repository evidence.

Only major/minor RimWorld selectors are allowed, such as 1.6 and 1.7. Patch/revision numbers, Steam build IDs, depot manifest IDs and game-file hashes must never gate acquisition, builds, runs or publication. Strict catalog/profile/operation validation rejects those fields. Rolling branch downloads can refresh the same owned external cache. Full observed game details may be retained as evidence only. Tool/dependency versions and immutable mod package/source/image hashes remain separate concerns. Acquire complete external installations for native acceptance and compile against their reference directories; never substitute the normal Steam installation.

Use the existing VersionedGame props/targets to select .NET Framework 4.8 for 1.6 and .NET 10 for 1.7. Each target gets unique intermediate and output roots and its own version symbol. Package one product DLL per declared compatibility folder, shared source only, and generate supportedVersions from the completed matrix. Ordinary development chooses the declared development target; publication consumes the tested combined artifact without rebuilding it.

The initial matrix needs no speculative XML transform engine: canonical XML is copied with a recorded no-override projection unless an actual native divergence is found. Narrow engine differences belong in product Compatibility/RimWorld sources.

## Verification
Test invalid target and cache rejection, isolated build arguments and merged package policy. Independently review, then build and run native workflows on both major/minor installations before advertising either version together. Keep the split publication action evidence and normal-config/process cleanup evidence.
