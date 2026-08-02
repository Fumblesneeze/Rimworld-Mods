## Context

`Zlepper.RimWorld.ModSdk.Testing/0.0.9` imports NUnit and references `Assembly-CSharp.dll` plus `UnityEngine.CoreModule.dll`. Its targets contain no mod loader, Def loader, Harmony initializer, or per-test reset facility. Project references make production types callable but do not construct the RimWorld `Mod` entry point.

RimWorld's Def databases and Harmony patch registry are process-global. Test order and `[NonParallelizable]` alone cannot undo arbitrary static initialization or external-mod side effects reliably, so incompatible environments need separate testhost processes.

The owning mod is Immersive Chefs (`fumblesneeze.immersivechefs`).

## Goals / Non-Goals

**Goals:**

- Prove the default suite starts without loaded mods, loaded Defs, or Harmony.
- Prove a dedicated suite can apply one explicit Harmony patch and remove it without leaking it beyond the test.
- Prove a dedicated suite can construct lightweight test-only Defs, take exclusive ownership of an initially empty `DefDatabase<T>` for one scope, and clear that whole database without ever mutating a pre-populated database.
- Make every environment independently selectable while preserving one command for all Immersive Chefs tests.

**Non-Goals:**

- Recreate `LoadedModManager.LoadAllActiveMods`, Unity native initialization, or RimWorld's complete play-data loader in an external NUnit process.
- Treat an assembly reference, package-ID fake, or manually applied patch set as proof that an optional mod was loaded normally.
- Validate real Core/Workshop XML inheritance, cross-references, conditional patches, or mod constructors outside RimWorld.

## Decisions

### Isolate incompatible global state by test project

`ImmersiveChefs.Tests` remains the default unpatched domain suite. `ImmersiveChefs.Harmony.Tests` references the exact installed `0Harmony.dll` and is the only suite allowed to patch methods. `ImmersiveChefs.Defs.Tests` owns all host-side `DefDatabase<T>` mutation. Each `dotnet test` project invocation receives a fresh testhost process.

Within the specialized processes, fixtures remain non-parallel and cleanup runs in `Dispose`/`finally`. Harmony acquisition rolls back its unique owner if patching fails, and disposal is marked complete only after unpatching succeeds. This limits ordinary test failures from contaminating later tests, while process exit remains the hard isolation boundary.

### Apply only the patch under test

The Harmony suite patches one explicit method with a unique owner ID and removes all patches for that ID in cleanup. It does not instantiate `ImmersiveChefsMod` or call broad `PatchAll` unless a future test explicitly owns and isolates that contract.

Tests of optional adapters should reference only the exact external assembly needed and apply only the owning Immersive Chefs patch. A test that requires another mod's constructor, static initialization, full patch set, or loaded Defs belongs in an isolated RimWorld acceptance run.

### Use three levels of Def test data

1. Pure calculations receive ordinary value objects/snapshots and need no `DefDatabase`.
2. Host Def lookup tests use a constructed minimal test-only `Def` subclass and a scoped database registration in a fresh, empty generic database. These fixtures are real `Def` instances, not mocks, but they do not claim XML loading. The scope refuses pre-populated state: RimWorld's `InitializeShortHashDictionary` calls a Unity-Mono `Dictionary.EnsureCapacity` API unavailable in the Microsoft .NET Framework testhost, so clearing and re-adding a live database would silently corrupt short-hash lookup or fail during reconstruction.
3. Source-XML shape tests may use ordinary `XmlDocument`/XPath without populating a Def database. Tests of actual `ThingDef`/`RecipeDef` loading, inheritance, cross-references, `PatchOperationFindMod`, or external Def presence run through RimWorld's real loader in game.

The host suite deliberately avoids constructing `ThingDef`: its constructor initializes Unity-backed `BaseContent`/shaders and cannot be treated as a faithful headless Def load. `DirectXmlToObject`/`DirectXmlLoader` are also not a supported shortcut: in the NUnit testhost their inheritance/type discovery reaches Unity internal calls. Uninitialized `ThingDef` fakes are permitted only for narrowly documented field-reading tests and should usually be replaced by a small product-owned snapshot.

### Group suites in the guarded wrapper

`-Suite ImmersiveChefs` selects the unpatched, Harmony, and Def projects. Each environment is also selectable by its exact suite name. `-Suite All` includes every project. Grouped runs continue after a project failure so every environment retains TRX/log evidence. A fake-dotnet contract fixture injects early-failure, all-pass, and zero-executed TRX results to keep that behavior regression-tested. Evidence directories use an exclusive timestamp/PID/GUID identity. The guard uses TRX `executed`, not merely `total`, and therefore rejects all-ignored results. A test filter is accepted only with one exact suite.

## Risks / Trade-offs

- **Static state leaks after a failed specialized test** -> use failure-safe scoped cleanup and a dedicated process; never mix specialized and ordinary tests in one project.
- **Installed Harmony path differs or a stale copy is used** -> derive it from the configured Workshop root, expose an explicit override, embed the resolved source path in the test assembly, compare source/runtime hashes, and retain identity evidence beside the TRX.
- **Constructed Def passes while game XML fails** -> state the tier in test names/evidence and require real in-game loading for XML/patch compatibility claims.
- **Too many optional-mod projects** -> add one only when an implemented adapter needs the real assembly shape; keep package-ID behavior in the ordinary domain suite.

## Migration Plan

1. Capture RED evidence for unavailable specialized suite selections and missing test helpers.
2. Add the isolated projects and focused capability probes.
3. Register grouped suite selection in `Invoke-Tests.ps1` and rerun all repository tests.
4. Document the decision table and validate OpenSpec plus the Release solution.
