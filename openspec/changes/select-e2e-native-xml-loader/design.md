## Context

Owner: repository tooling, `RimWorldDevGateway.EndToEndHost` at
`tools/RimWorldDevGateway.EndToEndHost`. This is a startup-selection extension to the existing typed
E2E workflow, not a new launcher or product feature.

Installed RimWorld 1.7.5000 rev1275 reads
`GenCommandLine.CommandLineArgPassed("legacy-xml-deserializer")` in
`LoadedModManager.ParseAndProcessXML`, before its node loop. Absence selects
`DirectXmlToObjectNew.DefFromNodeNew`; presence selects `DirectXmlLoader.DefFromNode`. This applies
to initial loading and later native reloads. The native predicate accepts a complete argument,
case-insensitively, with an optional single leading hyphen; a value assignment does not match.
The launcher emits the single exact argument `-legacy-xml-deserializer`.

Before this change, discovery and XML-fixture lookup derived groups from the ordered package sequence alone.
The runtime bundle catalog also selected by that sequence. The typed operation already selects an
exact test or group and supports isolated prelaunch mod-settings XML, but neither that XML nor an
Arrange callback can select the initial native loader. Native source inspection is retained in
`artifacts/Implementation/megamod-p0-a/native-1.7.5000/`; the earlier bounded planning note is
`artifacts/Implementation/megamod-p0-a/native-toggle-reload/legacy-loader-plan.md`.

## Goals / Non-Goals

**Goals:** carry one declared startup choice from compiled metadata to a real minimized process,
admit only matching scenarios, preserve exact fixture/process ownership and expose mode in evidence.
Keep default-only declarations, package group IDs and native arguments unchanged.

**Non-Goals:** arbitrary process arguments, general startup-profile dictionaries, direct loader calls,
in-process mode mutation, product runtime changes, fixture payload changes, performance variants,
new public scripts, replacement fixture staging or acceptance of S094 from tooling tests.

## Decisions

### Fixed declaration and guarded schema transition

Add `bool LegacyXmlDeserializer { get; set; }` to `RimWorldEndToEndTestAttribute`, default false.
Propagate it through the existing host and runtime descriptors and manifest test records. PE metadata
decoding validates the named argument's Boolean shape without executing test code. Missing fields
mean default; malformed values fail. Limit explicit legacy selection to supported 1.7 E2E runs.

Default-only bundles retain `RimWorldDevGateway.EndToEndTests/v1` and omit the default Boolean field.
A bundle containing any explicit legacy declaration uses v2 for its entire complete manifest, with
the per-test mode preserved. The new catalog accepts v1 only as default; a v1 record claiming legacy
fails. V2 validates mode against each compiled declaration, including default entries in a mixed
bundle. An older catalog's existing exact-schema check therefore rejects mode-bearing bundles rather
than silently ignoring the new field. Unknown schemas fail. Assembly/manifest/owner/dependency/hash
checks remain complete before selected tests can execute. Hashes prove identity, not loader semantics.

### Group identity includes the declared mode

Use one host-owned group-key function for discovery, selected fixture planning and availability
lookup. Default remains the exact existing pipe-delimited ordered package sequence. Legacy adds
the fixed suffix `::legacy-xml`; it is not a fictitious package. Extend the typed operation's group
grammar only for that terminal suffix; reject unknown, repeated or misplaced suffixes and validate
the preceding package sequence unchanged. Exact test-ID selection requires no new public parameter.

Two modes with the same packages produce separate process plans and fixture leases. Planned fixture
availability remains distinct from discovered installed packages. Fixture validation, immutable file
identities, selected-only publication and exact-child cleanup/recovery keep their existing contracts.
Every group passes its selected test IDs to the child, including group-ID invocations, so another
mode in the staged assembly cannot join execution accidentally. Full metadata validation remains
independent from selected-only package/fixture availability.

### Existing launch adapters carry one fixed switch

The grouped runner projects `xmlDeserializer: "default" | "legacy"` in discovery/dry-run and final
group results. Its existing `New-RimWorldEndToEndSmokeLaunch` projection passes the internal
`-LegacyXmlDeserializer` switch only for a legacy group. The smoke adapter checks that it is an
isolated 1.7 E2E invocation and appends exactly one native flag to the same argument array consumed by
its dry-run and actual child start. Reject incompatible uses before launching or publishing fixtures.
There is no public raw-arguments API and no special case keyed to megamod or one scenario ID.

Retain language, audio, timeout, exact selected packages, prelaunch config hashes, fixture error
expectations, stdout/stderr, stop identity and cleanup outcomes. Existing default arguments remain
unchanged apart from always carrying the selected group test IDs when necessary for mode isolation.
Minimized launch and savedata/log/config isolation remain the runner's responsibility.

### Observe the actual native mode before admission

The Gateway bootstrap reads the real native command-line predicate once and supplies the immutable
Boolean to the existing E2E coordinator/catalog. Record this observed mode in the persisted E2E
snapshot; a host-requested value cannot stand in for it. The catalog retains complete manifest versus
compiled metadata checks and selects only matching package order plus mode. An explicitly selected
wrong-mode test produces a clear requested/observed mismatch failure before construction/Arrange.
Unselected opposite-mode declarations are validated but do not fail a compatible selected run.
Zero admitted tests remain failure; no default fallback or silent empty success is permitted.

This needs only optional constructor/CreateEnabled parameters on existing owners, not a new service,
route or dispatcher. The selected IDs already flow through CreateEnabled and can scope mismatch
reporting. Keep diagnostics bounded and retain no credentials or unrelated environment arguments.

### Focused host evidence and real native gate

Work vertically: first compiled metadata and opposite-mode grouping RED/GREEN; then fixture lookup
and manifest schemas; then typed group grammar and actual launch projection; then runtime mismatch
before Arrange. Use existing honest host environments and scoped tests, never active-mod emulation.
Repository instructions supersede the generic CLI skill's suggestion of another public test script:
use retained typed `test_run` plus existing host PowerShell invocation fixtures.

The concrete initial-legacy player case belongs to S094 and is separately leased. It can reuse the
initial fixture and native selection/details/Copy workflow without changing the fixture. Its exact
prelaunch flag, observed native mode and real initial XML records provide supporting loader evidence;
native row selection, inspected details/copy/screenshots provide the player workflow. A fresh default
case is also required after shared workflow/runner changes. Legacy reload is not implied by initial
coverage. No native acceptance task closes from a host pass, dry-run or startup marker.

## Coordinated source and test lease

Root approved these exact implementation paths; central project files remain root-owned:

| Boundary | Existing files |
| --- | --- |
| Shared declaration/manifest | `shared/RimWorldDevGateway.EndToEndTesting/EndToEndTestContract.cs`; `shared/RimWorldDevGateway.Contracts/EndToEndBundleManifest.cs` |
| Host metadata/group/plan | `tools/RimWorldDevGateway.EndToEndHost/EndToEndAssemblyMetadataReader.cs`; `EndToEndDiscoveryModels.cs`; `EndToEndDiscoveryValidator.cs`; `EndToEndBundlePlanner.cs`; `EndToEndProjectXmlFixtures.cs`; `EndToEndHostCli.cs` in that same directory |
| Typed selector validation | `tools/RimWorldModding.Mcp/RepositoryOperationPlanner.cs` (group suffix validation only) |
| Internal launch/evidence | `scripts/Invoke-RimWorldEndToEndTests.ps1`; `scripts/RimWorldEndToEndRunner.Support.psm1`; `scripts/Invoke-GatewaySmoke.ps1` |
| Gateway admission/evidence | `mods/RimWorldDevGateway/Source/EndToEndTesting/GatewayEndToEndBundleCatalog.cs`; `GatewayEndToEndBundleModels.cs`; `GatewayEndToEndCoordinator.cs`; `GatewayEndToEndSnapshot.cs` in that same directory |
| Native mode injection | `mods/RimWorldDevGateway/Source/Runtime/GatewayRuntimeBootstrap.cs` (only supplying the native predicate to the existing E2E owner) |

Requested tests: new `tests/RimWorldDevGateway.EndToEndHost.Tests/EndToEndNativeXmlLoaderTests.cs`,
new `tests/RimWorldModding.Mcp.Tests/NativeXmlLoaderOperationTests.cs`, new
`tests/RimWorldDevGateway.Waylith.Tests/EndToEndNativeXmlLoaderCatalogTests.cs`, and new compiled
metadata declarations in `tests/Fixtures/EndToEndHost.ValidFixtures/NativeXmlLoaderFixtures.cs`.
Narrow affected fixture-inventory expectations may require
`tests/RimWorldDevGateway.EndToEndHost.Tests/EndToEndMetadataDiscoveryTests.cs`,
`tests/RimWorldDevGateway.EndToEndHost.Tests/EndToEndHostCliTests.cs`, and
`tests/RimWorldDevGateway.Tests/RimWorldEndToEndRunnerCliTests.cs`.

Root additionally approved only the manifest fixture builder in
`tests/RimWorldDevGateway.Tests/GatewayEndToEndBundleCatalogTests.cs`. The newly compiled legacy
declaration makes that existing helper's old all-default v1 manifest incomplete; the correction
copies declared mode and selects v2 when needed, preserving all existing default/performance
assertions. Its legacy performance environment is not imported into Waylith. Root added only the
existing `GatewayEndToEndDiscoveryCoordinatorTests.cs` source link there for affected default-owner
regressions; the new mode-specific catalog tests use the existing ValidFixtures output.

Root retains central project/build ownership: request one Compile link of the new host test into
`tests/RimWorldModding.Mcp.Tests/RimWorldModding.Mcp.Tests.csproj`; the new Waylith test is compiled
by that project's normal glob. If the catalog test uses compiled host fixture bytes, build only the
existing `EndToEndHost.ValidFixtures.csproj` before Waylith tests via a root-owned narrow target;
do not import the legacy test project's unrelated full fixture build. No new test project is needed.
Required output leases are `RimWorldModding.Mcp`/HostTools/EndToEndHost and, separately,
`RimWorldDevGateway.Waylith.Unit`. Neither needs Megamod.Harmony output.

## Risks / Trade-offs

- A late scenario flag cannot affect initial XML loading: require actual prelaunch mode plus native
  observation; do not manipulate command-line state or call a loader to manufacture proof.
- Old Gateway code could ignore an additive field: use v2 for mode-bearing bundles and fail old
  schemas claiming legacy; preserve v1 output for default-only bundles.
- Independently rebuilt group keys could lose fixture availability: centralize host identity and
  test exact default/legacy selection with the same planned fixture, including empty fixture groups.
- Broad settings/config copying could alter evidence: use existing bounded prelaunch config only
  when explicitly requested. The initial S094 case needs fresh defaults, not an extra setting file.
- Real legacy loading may reveal additional native errors or layout differences: retain exact logs
  and stop on unexpected diagnostics; do not loosen error counts or synthetic-state assertions.
- Manifest-wide versioning means a mixed bundle uses v2 even when a default member is selected:
  this is intentional fail-closed compatibility, with unchanged default behavior and group identity.

## Migration Plan

Implement only after coordinated leases, with one behavior's focused RED/GREEN at a time. Run affected
host checks, typed OpenSpec validation and independent review before a root-owned compile/deploy.
Dry-run exact default and legacy selections and compare ordered packages, fixture identities, mode
and native flag projection. Then run the reviewed native workflows in separate fresh processes.
Any failure retains the existing exact process/fixture recovery obligations. Rollback uses the
previous reviewed build and v1/default declarations; no user config or product payload migration exists.

## Implemented candidate and typed usage

The implementation candidate passes focused host checks and awaits independent review and native
verification. Retained before files, exact source delta, TDD receipts and final hashes are under
`artifacts/Implementation/e2e-native-xml-loader/`. These local receipts are not native acceptance.

Scenario authors set `LegacyXmlDeserializer = true` on the existing `RimWorldEndToEndTest` attribute.
Callers continue using `e2e_run_start` with its exact `projectPath` plus `testId`, or with the discovered
legacy `groupId` ending in `::legacy-xml`; there is no new public mode/arguments parameter. First use
`dryRun: true` and inspect each group's `XmlDeserializer`, selected test IDs and full fixture inventory.
The internal smoke adapter converts the fixed selection to exactly `-legacy-xml-deserializer`.

The runner's selected-group projection preserves the validated mode from the host plan. Independent
review caught that the first candidate dropped this field while copying packages/tests/fixtures,
causing legacy selection to fall back to default. The focused correction exercises the actual JSON
plan-to-candidate-groups-to-selected-groups-to-launch-plans path for both selector types and both
modes, with and without a declared fixture. Tests no longer inject an already selected group at this
boundary. The implementation correction is one projected field using the existing mode validator.

The Gateway observes the native predicate once when it creates the enabled E2E owner. Persisted
snapshots expose `ObservedXmlDeserializer`; admitted test snapshots expose `RequestedXmlDeserializer`.
Selected mismatches return `xml_loader_mode_mismatch` with both values before execution is created.
Default-only bundle serialization remains v1 with the false field omitted. A mixed v2 bundle still
validates every compiled declaration before admitting its compatible subset. Performance declarations
continue using their existing default mode; no performance variant has been added or natively tested.

## Open Questions

None about intended behavior. Root must independently review this candidate and separately authorize
the feature-local scenario. Native legacy behavior remains unverified until that workflow.
