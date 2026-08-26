## Context

The repository has good individual safety mechanisms, but they are spread across PowerShell entry points, .NET hosts, raw Gateway HTTP, and mod-specific release scripts. Agents repeatedly rediscover argument shapes, output parsing, exact-PID rules, evidence paths, package identities, and Steam confirmation gates. The first concrete symptom is that a second distributable mod would otherwise receive a second publisher.

The owner is repository tooling, specifically `RimWorldModding.Mcp` at `tools/RimWorldModding.Mcp`. Product and developer mods never reference, depend on, or ship it. It must work in local trusted Codex sessions on Windows, while retaining deterministic JSON contracts that can later support other hosts.

## Goals / Non-Goals

**Goals:**

- Make one typed, discoverable MCP/CLI the agent-facing surface for routine RimWorld repository operations.
- Share one operation implementation between stdio MCP and command-line invocation.
- Make publishing universal across validated per-mod profiles, with stronger guards than the existing bespoke path.
- Preserve process isolation, exact-PID ownership, evidence, cancellation, normal-config restoration, and Steam identity safety.
- Turn repeated custom automation into specified, tested operations instead of accumulating scripts and prompt recipes.

**Non-Goals:**

- Replacing MSBuild, NUnit, OpenSpec, Steam, RimWorld, Harmony, or the in-game Gateway as underlying engines.
- Exposing arbitrary shell execution as an MCP tool.
- Making the Gateway or MCP a product-mod dependency.
- Treating MCP success, tests, startup, or raw mutation as gameplay acceptance.
- Deleting proven scripts before their behavior is represented and regression-tested behind the new surface.

## Decisions

### 1. One .NET 8 single-file executable serves MCP and CLI

Create `tools/RimWorldModding.Mcp` targeting the repository-pinned .NET 8 SDK, with single-file publication, the official `ModelContextProtocol` 2.2.0 package, and `System.CommandLine`. `serve` starts a local stdio server; `tool list` and `tool call <name> --arguments <json>` invoke the same operation registry without MCP. Standard output is reserved for MCP frames or the selected `json|table` result, and diagnostics go to standard error.

This follows the repository's existing .NET host stack and avoids a second runtime. A remote HTTP server is rejected because these tools act on a local worktree, local game processes, and local Steam client state.

### 2. A single operation registry owns names, schemas, risk, and dispatch

Every operation has a stable snake-case name, description, typed input/output, risk classification (`read`, `workspace-write`, `external-write`, or `destructive-local`), default timeout, cancellation behavior, and implementation. MCP discovery and CLI help are projections of that registry. The initial catalog covers:

- repository/mod/profile discovery and status;
- focused OpenSpec validation, builds, tests, package validation, isolated launches, grouped E2E, and evidence reads;
- live Gateway health, allowlisted diagnostics, semantic actions, registered automations, and explicit bounded raw C# mutations;
- normal `ModsConfig.xml` inspection, enablement, backup, restore, and validation;
- universal release candidate preparation, dry-run admission verification, Steam publication, identity persistence, reacquisition, and subscribed-copy verification.

Long-running operations receive a run ID and durable artifact directory. They retain child-process identity, stream bounded progress to evidence, honor cancellation, and expose status/stop operations. Read-only Gateway diagnostics and raw mutations remain separately named so callers cannot confuse observation with acceptance.

### 3. Repository configuration starts a concurrency-safe source build

Commit `.codex/config.toml` with a required local stdio server whose command invokes one internal bootstrap for `RimWorldModding.Mcp`. Omit an MCP `cwd` override so Codex uses the task's logical workspace-root fallback; a relative `.` or `..` would instead depend on the desktop/CLI host process directory. Codex project configuration remains authoritative because Codex automatically loads trusted `.codex/config.toml` files. Also commit a root `.mcp.json` projection of the same command with its conventional project-root working directory for MCP clients which choose that JSON convention; MCP itself does not standardize configuration discovery, so the JSON file is a portability aid rather than a replacement for Codex configuration.

The bootstrap is not another public automation surface. It exposes no repository operation and only acquires a bounded cross-process build lease, fingerprints the exact C# project/SDK/build inputs, builds into an isolated staging directory, atomically publishes a verified immutable cache entry, and then executes that C# assembly with `serve --repository-root <exact-root>`. Build diagnostics go to stderr and stdout remains untouched before the MCP process owns it. A cancelled or failed build cannot publish a success stamp, and a later start repairs or retries it. The server independently validates its root and refuses to search upward into another repository.

This is reproducible in a fresh clone without committing build output, while avoiding `dotnet run` races when Codex initializes multiple tasks concurrently. Release builds still publish one single-file executable into ignored artifacts for manual or CI use.

### 4. Per-mod JSON profiles make release behavior universal

Each distributable mod owns `mods/<ModName>/Release/release.json`. A strict schema and typed validator require stable mod/project/package identity, distribution class, supported game target, package/build inputs, presentation files, Workshop title/description/tags/visibility, dependency relationships, Steam required-item IDs, owner Steam ID, current published item ID or explicit first-publication opt-in, explicit hard runtime AssemblyRefs, and verification profiles. Steam required-item edges are deliberately separate from RimWorld's `modDependencies`: the latter must exactly match project-generated hard dependencies and be a subset of the former, while optional compatibility mods may remain load-after-only in `About.xml`. Unknown properties fail. Package traversal refuses reparse points, and any non-platform/non-game product AssemblyRef fails unless the profile explicitly classifies it as hard; optional integrations cannot use that escape hatch.

The universal release pipeline accepts a profile path or package ID; it never contains a mod name in code. Profile build/presentation steps are selected from registered operation IDs and declared inputs, not free-form command strings. The initial migration may call proven internal runner scripts behind adapters, but those scripts are not the public contract and may not contain product-specific publication policy.

### 5. Publication remains a two-turn immutable transaction

`release_prepare` requires a clean committed source revision, builds and stages from a positive allowlist, hashes every content/presentation input, validates metadata and dependencies, scans the owner account for an exact-title collision on first publication, and writes an immutable candidate plus canonical dry-run plan. It returns a candidate digest, remote diff, expiry, and random nonce.

Preparation pins and validates the exact Steam depot build, RimWorld runtime identity, and managed-assembly hash. Package validation admits exactly the declared product assembly and rejects RimWorld, Unity, Harmony, compiler, Gateway, optional-mod, host, and test assemblies or AssemblyRefs. Steam preflight is authenticated: first publication proves exact-title absence through Steamworks, while an update captures title, description, tags, metadata, visibility, preview bytes, owner/app identity, content size/update identity, dependencies, and additional-preview identities. The admitted plan binds that baseline and one specific player-facing change note; an update also proves its pinned previous note against accessible Workshop history.

`release_publish` requires the exact digest and nonce as an internal admission proof. A prior explicit publish order authorizes the subsequently prepared plan when its title, target item, visibility, dependency graph, and other material mutation match that order; the workflow must pass the proof directly and must not ask for a second confirmation. After its bounded Steam startup/preflight delay it repeats source revision, worktree cleanliness, candidate bytes, Steam user, exact remote baseline, profile, frozen subscriber inputs, and expiry validation immediately before the irreversible submission. First publication is always Private. The public call launches the same executable as a detached, exact-plan worker and records an atomic request/lease/result protocol. Before Steam dispatch the worker reserves and records the exact Gateway run. A retry reattaches to that callback; if the process was lost after an item ID became durable, it authenticated-queries and polls only that same item for the submitted plan. It never repeats CreateItem or SubmitItemUpdate, and an identity-less lost callback remains explicitly incomplete. As soon as Steam reports success, and before any remote/subscriber verification, it persists the item ID in the profile and `About/PublishedFileId.txt`, disables first-publication opt-in, and commits that identity-only change. It then verifies remote metadata, required items, preview, and package, reacquires the subscribed item, and runs the declared native verification. Subscriber isolation has its own pre-move durable record containing the reserved exact run, backup path, and normal-config hash, so worker-loss recovery restores local state before replay. The repository MCP timeout exceeds its declared release bound, while `release_status` and the validated retained worker result remain usable after plan expiry or later local profile/candidate changes.

Subscriber automation success remains supporting evidence. Publication returns `awaiting-personal-review` with exact fresh screenshots and an expected observation; it MUST NOT claim gameplay acceptance. `release_accept_subscriber_evidence` completes the local receipt only after the caller supplies a concrete personal observation for the exact retained receipt/screenshots, which the tool hashes into a review record.

### 6. Normal mod-list writes are transactional and recoverable

`modlist_enable` resolves the normal RimWorld `ModsConfig.xml`, refuses to write while a non-owned RimWorld process is running, hashes and copies the exact file to a timestamped backup, parses XML, inserts one canonical package ID at the requested anchor without changing other entries, writes atomically, reparses, and reports before/after hashes and backup path. Any validation/write failure restores the backup. `modlist_restore` requires that exact recorded backup and refuses a mismatched target unless explicitly confirmed.

Local package synchronization stages in a temporary directory outside RimWorld's scanned `Mods`
directory, replaces only the selected package, and retains no install backup. This keeps the normal
build/deploy loop predictable: the repository package remains the recovery source and a successful
build is immediately what the game will discover. Subscriber verification remains separately
recoverable because it temporarily removes the local package while testing the subscribed Workshop copy.

### 7. Repetition becomes a process trigger, not technical debt

`AGENTS.md` will require agents to search the MCP operation catalog before adding RimWorld automation. A workflow becomes a candidate when it is repeated, copied across mods, requires structured parsing, owns a long-lived process, crosses a safety boundary, or produces acceptance/release evidence. The owning OpenSpec change must add observable tool scenarios and TDD before another public script is accepted. One-off diagnostic exploration may still use raw C# or shell, but reusable behavior graduates to the MCP/CLI.

## Risks / Trade-offs

- **[MCP server context grows with too many tools]** → Keep tools high-level and typed, group narrow variants behind bounded arguments, and expose concise descriptions.
- **[Concurrent Codex tasks race a source build or corrupt stdio]** → Serialize cache validation and the cold build/publication transaction, use isolated staging and immutable fingerprinted output, and reserve stdout for MCP frames from the executed C# server.
- **[A wrapper preserves script fragmentation internally]** → Treat scripts as versioned migration adapters, record which operation owns each one, and remove adapters only after parity tests.
- **[Long tests exceed client timeouts]** → Use run leases and status/cancel operations; the initiating call returns identity promptly where execution cannot finish within the configured bound.
- **[Generic release configuration becomes an unsafe command language]** → Permit registered operation IDs and typed data only; reject arbitrary commands, paths outside the repository/candidate, unknown fields, and mismatched identities.
- **[MCP writes bypass Codex shell approval expectations]** → Mark write tools with MCP risk metadata and retain exact operation-level plan admission regardless of client policy. Risk metadata may surface the client’s own approval UI, but the repository workflow must not add a second confirmation after a matching explicit user order.
- **[Steam accepts an update after client timeout]** → Persist admitted operation identity and poll/recover the same operation; never retry by creating another item.
- **[Normal config corruption or user-state loss]** → Exact backup, atomic replacement, post-parse verification, and explicit restore operation; isolated test runners remain forbidden from using normal config.

## Migration Plan

1. Add specs, tool project, operation-registry contract tests, and a non-destructive repository-status vertical slice.
2. Add `.codex/config.toml`, publish the single-file CLI, and verify MCP initialize/list/call plus CLI JSON/table/error behavior.
3. Move build/test/OpenSpec/package/E2E/Gateway/config operations behind typed adapters while retaining current engines.
4. Replace the Immersive Chefs-specific public release entry point with the universal profile and migrate Guest Bed Gizmo as the second profile.
5. Exercise Guest Bed's clean candidate and private first publication through MCP, persist identity, reacquire, and verify.
6. Deprecate agent-facing documentation for superseded scripts. Remove an internal adapter only after equivalent focused and live tests pass.

Rollback disables the `.codex/config.toml` server entry and restores prior documented commands. Product packages are unchanged. A failed normal-config write restores its exact backup. A Steam operation cannot be cancelled after submission, so rollback is a verified corrective update to the same item, never deletion or creation of another identity.

## Open Questions

- Exact-version depot acquisition remains governed by the active multi-version release change and can be added to this operation registry when its pinned target catalog is implemented.
- Cross-platform local operation is desirable, but Windows is the first verified host because the current RimWorld and Steam installation are Windows-only.
