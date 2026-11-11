## ADDED Requirements

Owner: repository tooling **RimWorld Dev Gateway**.

### Requirement: Retain completion evidence when verification fails

Smoke and E2E child runs SHALL attempt to retain a final credential-free evidence summary after
cleanup on both success and failure. The summary SHALL contain the actual captured process/start
and build identity, ordered mod list, normal configuration and preferences hashes from before and
after the run, and cleanup results/references. Uncollected identity values SHALL remain explicitly
unavailable; a failed run MUST NOT fabricate historical hashes or a successful verification result.

#### Scenario: A native scenario fails after launch
- **WHEN** the scenario fails and owned cleanup completes
- **THEN** the final summary records failed status, actual captured identities, both before/after hash pairs and cleanup evidence
- **AND** the launcher returns a nonzero exit code

#### Scenario: Failure occurs before a process is launched
- **WHEN** preparation fails before RimWorld starts
- **THEN** available run/configuration/cleanup evidence is retained with unavailable process identity
- **AND** the launcher returns failure without claiming a native run occurred

#### Scenario: Cleanup fails after a successful scenario
- **WHEN** a scenario passes but an owned cleanup step fails
- **THEN** the final summary is failed and retains the cleanup failure and completed scenario evidence
- **AND** success is not returned

### Requirement: Preserve failure reporting without credentials

New completion summaries MUST NOT retain bearer tokens, including tokens embedded in error text.
Failure and cleanup reporting SHALL remain available if summary persistence itself fails; the
launcher SHALL return nonzero and identify the attempted evidence location.

#### Scenario: Error text contains the live bearer token
- **WHEN** a failure message includes the run's bearer token
- **THEN** retained summary failure text redacts it and the remaining audit evidence is preserved

#### Scenario: The final summary cannot be persisted
- **WHEN** writing the completion summary fails
- **THEN** that error and earlier failures are reported with the evidence directory
- **AND** the command remains failed
