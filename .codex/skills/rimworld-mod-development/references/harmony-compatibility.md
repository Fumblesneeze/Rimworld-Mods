# Harmony and optional-mod compatibility

## Patch only when necessary

Prefer, in order: XML/Def composition, public extension points or comps, a narrowly isolated adapter, then Harmony. A Harmony patch should bridge one engine boundary and delegate immediately into a testable domain module.

Before coding, classify Harmony as either a design-time fallback because the inspected version has no stable XML/public seam, or a runtime variant for explicitly supported external-mod surfaces. Never let XML/public-API and Harmony paths mutate the same behavior concurrently. A runtime selector must choose exactly one strategy, expose it in diagnostics, and pass duplicate-initialization tests.

- Use the gameplay mod's stable package ID as the Harmony ID.
- Let the RimWorld ModSdk compile against Harmony; do not ship a private `0Harmony.dll`.
- Prefer postfixes for observation and additive behavior. Use prefixes only when the original must be skipped or arguments/results must change.
- Avoid transpilers unless no stable call boundary exists. If unavoidable, match structural landmarks, fail closed with a clear warning, and add a live compatibility probe for the exact installed game/mod version.
- Keep patches idempotent and side-effect-free during discovery. Avoid hot-path allocations and recursive logging.
- Never touch Unity or Verse live state from network, timer, threaded-log, or worker callbacks. Queue it to the game main thread.

If an eagerly loaded product assembly references Harmony types, declare `brrainz.harmony` as a required dependency and load it before the owning mod. The external compatibility mod remains optional and must not appear under hard dependencies. Default external-API integration to string/reflection lookup; permit a compile-time adapter only when a documented loader boundary and an absent-mod Unity smoke prove it is safe.

## Optional integrations

Detect a mod by canonical package ID using `LoadedModManager.RunningModsListForReading`. Do not put an optional mod's type in a field, signature, attribute, generic argument, or eagerly JIT-compiled core method: that can trigger type resolution even while the mod is absent.

Resolve optional Defs by exact Def name only after the package is active. For C# APIs, keep reflection or a separately guarded adapter at the edge and validate:

- assembly and type identity;
- target method signature, declaring type, and return type;
- required fields/properties and lifecycle timing;
- graceful no-op behavior when any probe fails.

Log one bounded compatibility warning, not one per tick/job. Expose adapter availability in diagnostics and write tests for absent, present-supported, and present-incompatible cases.

## Patch hygiene

- Patch in a single initialization path and guard duplicate initialization.
- Preserve the original exception behavior unless the spec explicitly changes it.
- Avoid capturing mutable Verse objects beyond the tick/job in which they are valid.
- Use stable IDs or `ThingID` handles across queued work, then resolve and revalidate on execution.
- For job-giver and reservation changes, test cancellation, save/load, destroyed targets, drafted/downed pawns, map changes, and competing pawns.
- For compatibility claims, inspect the locally downloaded 1.6 assembly and prove the patch point in an isolated in-game run.
