## Context

Owner: repository tooling **RimWorld Dev Gateway**, `mods/RimWorldDevGateway`.
`GatewayEndToEndGatewayBackend.ApplyIncident` resolves the exact native incident and parameters,
then calls `IncidentWorker.TryExecute`. A false result is already distinguished as
`incident_rejected`; missing prerequisites and thrown execution have other failure codes.

## Goals / Non-Goals

Goals: permit native refusal expectations, retain explicit evidence, preserve ordinary incident
execution and fail closed for all other errors. Non-goals: product callbacks, Def database fixtures,
storyteller simulation, generic expected-error handling or acceptance from a returned boolean alone.

## Decisions

- Preserve the original three- and five-argument CLR constructors, including the original optional
  faction parameter. Both retain success-required behavior. Add explicit `expectRejected` overloads:
  a three-argument `(name, incident, bool)` convenience constructor, four arguments with a faction,
  and six arguments with faction/raid choices. The new boolean is required in these overloads, so
  ordinary calls retain their original overload resolution. Use the existing `ExpectRejected`
  vocabulary. New scenarios rebuild against the extended contract; previously compiled callers keep
  their constructor targets.
- Interpret the returned outcome in `GatewayEndToEndNativeActions.Apply`, after the unchanged native
  backend executes once. Match only ordinal `incident_rejected`; expected refusal records exact
  incident identity and expected/observed failure codes. Native success with the flag fails as
  `incident_not_rejected`. Preserve other failure outcomes, including null/throwing backend failures.
  This uses the existing action boundary rather than creating an incident abstraction for host tests.
- Host tests use a recording backend through the real native action dispatcher; they do not load
  incident Defs or assert actual engine behavior. Source-link the existing focused fixture into
  `RimWorldDevGateway.Waylith.Unit`, whose mod/Harmony/Def guards remain unchanged.
- Live acceptance uses a freshly reviewed isolated build and actual incident path, with before/after
  screenshots and a later observation of physical results/history. Product scenarios provide their
  own meaningful expected-refusal preconditions. Successful refusal evidence does not prove why an
  incident was refused; the scenario must establish that causal condition independently.

## Risks / Trade-offs

- An arbitrary infrastructure error could mask an incident defect → exact failure-code match and
  explicit tests for unsupported, missing prerequisites, exception and unexpected-success outcomes.
- Native success may already have side effects before an expectation fails → report failure without
  fabricated rollback; retain normal isolated-scenario cleanup.
- Host tests can prove only outcome interpretation → leave live acceptance unchecked until the
  acting orchestrator personally verifies the reviewed build. No balance or gameplay value changes.
