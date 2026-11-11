## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, RimWorld 1.7 beta.
This capability specifies observable delivery records and acceptance states, not new gameplay.

## ADDED Requirements

### Requirement: Bounded assignments preserve feature coverage
The implementation plan SHALL assign every selected feature to a primary chunk, record its
dependencies and shared-component owner, and preserve its currently authorized observable
specification and unfulfilled tasks. Explicit later user scope changes SHALL supersede earlier
obligations with a recorded disposition. An implementer MUST receive a bounded assignment with
explicit edit ownership.

#### Scenario: Parallel consumers need the same shared data
- **WHEN** two chunks need Thing data or another common capability
- **THEN** their assignments identify one shared owner and the contract milestone each consumes
- **AND** neither handoff claims readiness against a fabricated substitute for the real provider.

#### Scenario: An implementation task also requires live verification
- **WHEN** its code and host tests are ready but native verification has not run
- **THEN** the task remains unchecked or is split into traceable implementation and acceptance tasks
- **AND** the feature remains implemented-unverified rather than verified.

### Requirement: Reference research does not replace the owned contract
Implementers SHALL inspect the assigned referenced mods and existing owned helpers before designing
equivalent functionality. They MUST independently implement the owned specs and design, MUST NOT copy
upstream implementation code, and MUST record relevant differences and unresolved contract problems.

#### Scenario: Referenced code behaves differently from the selected design
- **WHEN** inspection finds an upstream shortcut or default that contradicts our specification
- **THEN** the implementer retains the owned behavior and records the finding
- **AND** any required change beyond the assignment is returned to the orchestrator with evidence.

### Requirement: The orchestrator can revise the plan on evidence
The orchestrator SHALL be authorized to revise chunk allocation, sequencing, dependencies, shared
designs and part boundaries within accepted user intent. Every revision MUST record the reason,
affected contracts and assignments, invalidated evidence and remaining work. A revision MUST NOT
silently remove requested functionality or weaken acceptance and safety requirements.

#### Scenario: A shared dependency invalidates two active assignments
- **WHEN** new evidence shows that their shared contract must change
- **THEN** the orchestrator records a revised contract and plan, supersedes the affected assignments
  and coordinates acknowledgements before dependent work resumes
- **AND** obsolete handoffs no longer count as ready for the revised part.

#### Scenario: Work moves to a later part
- **WHEN** an unforeseen blocker makes a chunk unsuitable for the current part
- **THEN** the revised roadmap retains its feature coverage and outstanding verification obligations
- **AND** the current part's manifest explicitly identifies its reduced scope and deferred dependency.

#### Scenario: The user excludes original-mod compatibility work
- **WHEN** the user removes original API/drop-in compatibility work while retaining startup warning and best-effort ordinary feature-off behavior
- **THEN** the plan and current assignment scope remove the superseded consumer/facade/coexistence tasks rather than treating them as pending release blockers
- **AND** independent source-behavior research, game-target admission, genuine external integrations and owned save continuity remain required.

#### Scenario: The user adds drug-policy references during implementation
- **WHEN** tolerance conditions and safe policy generation are added to the selected feature list
- **THEN** each has a stable queue identity, a primary chunk and its own observable specification before implementation
- **AND** the generator remains an independent implementation of the idea, with its safety assumptions and unresolved research visible rather than inherited from the source code.

### Requirement: Implementation handoff includes reviewable native scenarios
Each ready chunk SHALL provide focused nonzero host-test evidence, independent review dispositions,
dependency checks and executable in-game verification scenarios. A scenario MUST distinguish setup,
native player actions, observable outcomes and supporting diagnostics. Authoring or registering a
scenario MUST NOT be reported as running or passing it.

#### Scenario: Scenario tooling cannot perform the required player action
- **WHEN** an implementer cannot author a faithful action through the available verification surface
- **THEN** the handoff records the exact capability gap and its separately owned tooling dependency
- **AND** the corresponding gameplay acceptance remains incomplete.

### Requirement: Integrated acceptance is personally performed by the orchestrator
After all selected chunks of a part are ready, the orchestrator SHALL verify the reviewed integrated
build in isolated running RimWorld processes and personally inspect native action-to-result behavior.
Acceptance MUST retain exact build, action, observation and cleanup evidence. Host tests, startup,
synthetic state assertions or another agent's claim MUST NOT substitute for that observation.

#### Scenario: Individual host tests pass but combined gameplay fails
- **WHEN** the orchestrator observes a failed combined workflow
- **THEN** the part remains unaccepted and the issue is returned to the responsible implementation,
  scenario or tooling owner with evidence
- **AND** the orchestrator reruns affected workflows after the reviewed fix is integrated.

#### Scenario: A later change affects an accepted behavior
- **WHEN** runtime-affecting edits invalidate the build or assumptions supporting its acceptance
- **THEN** affected acceptance tasks reopen and require evidence on the new reviewed build
- **AND** unrelated completed scenarios are not automatically rerun as a full repository suite.
