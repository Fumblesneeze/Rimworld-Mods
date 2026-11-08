## ADDED Requirements

Owner: repository tooling **RimWorld Dev Gateway**, `mods/RimWorldDevGateway`.

### Requirement: Explicitly expect native incident refusal

`IncidentActionStep` SHALL expose an explicit refusal expectation through additional overloads while
preserving the original three- and five-argument CLR constructors and their success-required behavior. The Gateway
MUST execute the existing exact native incident path and SHALL satisfy the expectation only when
that path reports `incident_rejected` from `IncidentWorker.TryExecute` returning false. The step
SHALL retain exact incident identity and expected/observed refusal codes in its outcome evidence.

#### Scenario: Native refusal is expected
- **WHEN** a scenario requests refusal for an exact incident and its real worker returns false
- **THEN** the step passes with `incident_rejected` identified as the observed expected result
- **AND** later observation steps can verify the physical game state and incident history

#### Scenario: Unexpected native success
- **WHEN** refusal is expected but the native incident executes successfully
- **THEN** the step fails with `incident_not_rejected`
- **AND** already committed native side effects are not represented as rolled back

#### Scenario: Default execution remains strict
- **WHEN** no refusal expectation is supplied and the real incident worker returns false
- **THEN** the step fails with the existing `incident_rejected` result
- **AND** successful native execution retains its existing result evidence

#### Scenario: Existing compiled scenarios keep their constructor targets
- **WHEN** a scenario resolves the original three- or five-argument incident constructor
- **THEN** that exact CLR signature remains available with unchanged argument meaning
- **AND** refusal remains unexpected unless the scenario chooses an explicit new boolean overload

### Requirement: Preserve infrastructure failures and honest acceptance

Refusal expectations MUST NOT accept missing maps, incident Defs, factions, raid-choice Defs,
unsupported operations, thrown errors or absent outcomes. Host outcome tests SHALL remain free of
active mods, Harmony initialization and populated Def databases. Acceptance SHALL require the
reviewed native workflow, personally observed before/after game results, process/build identity and
normal isolated-run cleanup evidence; a passing step alone SHALL NOT constitute acceptance.

#### Scenario: Incident prerequisites are unavailable
- **WHEN** refusal is expected but the native path cannot resolve its required map, Def or faction
- **THEN** the original prerequisite failure remains a failed step
- **AND** it is not recorded as a worker refusal

#### Scenario: Native execution or dispatch fails
- **WHEN** the backend reports unsupported execution, throws, or provides no outcome
- **THEN** the scenario still fails through its existing error boundary
- **AND** no successful refusal evidence is fabricated

#### Scenario: Observe the native refusal workflow
- **WHEN** the acting acceptance agent runs the reviewed scenario in a fresh minimized isolated game
- **THEN** the agent personally inspects causal action-to-result captures and the expected unchanged
  physical game state while retaining actual incident execution identity
- **AND** the scenario cleans only its own disposable state and preserves normal configuration hashes
