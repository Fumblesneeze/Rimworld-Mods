## Owner

Exactly one owner: repository tooling **RimWorldModding.Mcp**.

## ADDED Requirements

### Requirement: Exact megamod host-suite dispatch
The typed test operation SHALL accept `Megamod.Unit`, run only its .NET 10 host project, honor the
requested exact filter, and retain its real test counts and log/TRX evidence. It SHALL NOT require
the legacy game Managed layout or load a real mod to run this suite. The legacy All aggregate SHALL
not implicitly select this opted-in suite.

#### Scenario: Selected policy test on a 1.7 installation
- **WHEN** the caller selects Megamod.Unit and an existing exact test filter
- **THEN** only that suite/filter executes and the result reports its actual nonzero counts and .NET 10 framework.

#### Scenario: Product behavior fails
- **WHEN** a selected test assertion fails
- **THEN** dispatch retains the failed test evidence and returns failure rather than a successful command result.

#### Scenario: Filter matches nothing
- **WHEN** the selected filter executes zero tests
- **THEN** the operation returns failure with an explanation and retained diagnostic evidence.

### Requirement: Separate constructed-definition host dispatch

The typed test operation SHALL accept `Megamod.Defs` as a separate .NET 10 process for real native
data objects and explicitly registered lightweight Def fixtures. It SHALL honor exact filters,
retain actual nonzero counts and failure evidence, resolve the 1.7 game layout, and remain outside
the legacy All aggregate. It SHALL NOT load active mods, initialize Harmony or import Core/Workshop
Def databases. Fixtures SHALL own and clear an initially empty database, with assembly entry/exit
checks that reject leaked definitions, loaded mods or Harmony.

#### Scenario: Repair constructed native policy storage
- **WHEN** the caller selects a native drug-policy adapter test in Megamod.Defs
- **THEN** only that test runs against real policy objects and its declared constructed Defs
- **AND** its result identifies the .NET 10 environment and retains its actual assertion outcome
- **AND** no live game or loaded-content acceptance is claimed

#### Scenario: Constructed-definition filter selects nothing
- **WHEN** a Megamod.Defs filter executes no tests
- **THEN** dispatch fails with retained evidence instead of reporting successful verification
