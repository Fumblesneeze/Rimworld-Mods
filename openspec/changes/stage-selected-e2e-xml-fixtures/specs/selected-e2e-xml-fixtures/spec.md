Owner: repository tooling, `RimWorldDevGateway.EndToEndHost` at `tools/RimWorldDevGateway.EndToEndHost`.

## ADDED Requirements

### Requirement: Selected fixtures remain separate from installed inventory

The tooling SHALL plan explicitly declared XML-only repository fixtures only for selected exact active-package groups. It SHALL report planned fixtures separately from discovered installed packages and ordinary product deployment, preserve declared active-package order and keep unrelated groups independent of fixture payload availability.

#### Scenario: Diagnostic fixture selected
- **WHEN** a selected group declares the fixture after its owning product and a valid repository manifest exists
- **THEN** its plan contains the fixture identity and exact file hashes without modifying the installed-package inventory or writing a package

#### Scenario: Another selected group does not declare the fixture
- **WHEN** a selected group omits the fixture package
- **THEN** the fixture is not staged and its payload files are not required for that group

#### Scenario: Fixture declared by the owning test project
- **WHEN** a selected test's explicitly marked project declares an unconditional literal XML fixture item with the same owner
- **THEN** only that declaration can supply planned fixture availability to its selected exact group, while complete metadata validation and undeclared dependency failures remain effective

#### Scenario: Adjacent performance selectors remain unambiguous
- **WHEN** an internal performance command accepts repeated exact group or project selectors
- **THEN** each option has one parser binding and preserves all supplied values, including after common E2E selector options are extended

### Requirement: Explicit bounded XML fixture payload

The tooling SHALL accept only repository-contained positive file inventories belonging to the fixture manifest, with matching About package/version and owner dependency. It SHALL reject traversal, reparse paths, unsupported payloads, ambiguous identities and malformed metadata. Diagnostic Def values and type names SHALL remain inputs to the real loader rather than being rewritten or semantically approved by the host.

#### Scenario: Exact XML inventory
- **WHEN** the fixture manifest declares its About file and diagnostic Def XML
- **THEN** only those XML bytes are planned and published, with no fixture assembly, product project or release dependency

#### Scenario: Unsafe or mismatched input
- **WHEN** a selected fixture path escapes its source, crosses a reparse point, contains an unsupported payload or mismatches its declared identity
- **THEN** planning or publication fails before the fixture becomes discoverable

### Requirement: Atomic owned fixture publication preserves existing packages

The tooling SHALL persist a recoverable ownership lease before fixture mutation, prepare a complete package outside scanned Mods on the destination volume, and atomically publish it at the canonical fixture-package directory. It SHALL reject an existing destination or discovered duplicate fixture package and SHALL never replace, back up, remove or edit a preexisting product, fixture or Workshop package. Publication SHALL verify its payload against the planned inventory.

#### Scenario: New fixture becomes discoverable
- **WHEN** publication succeeds for an absent fixture destination
- **THEN** the complete About and Def payload appears together and the retained lease identifies only that transaction

#### Scenario: Trailing separator in the configured Mods path
- **WHEN** the configured Mods directory ends with a primary or alternate directory separator
- **THEN** planning normalizes the directory and temporary publication remains outside scanned Mods

#### Scenario: Existing package is preserved
- **WHEN** the canonical fixture destination or an installed package with that identity already exists
- **THEN** staging fails and its existing bytes remain unchanged

#### Scenario: Payload changes after planning
- **WHEN** source bytes differ from the planned file identity
- **THEN** publication fails before exposing the fixture and removes only its own temporary data

### Requirement: Failure and completion clean only the exact owned fixture

The typed E2E lifecycle SHALL clean each fixture after its exact isolated process has stopped, including action, launch and publication failure. Cleanup SHALL verify lease-derived containment, transaction ownership and absence of reparse paths before deleting anything. A changed or unowned path SHALL be preserved, and an unresolved cleanup failure and recovery lease SHALL remain visible. Missing leased paths SHALL be treated as already clean. Dry-run SHALL perform no staging or cleanup mutation.

#### Scenario: Selected child completes or fails
- **WHEN** the selected isolated child stops after success, failure or cancellation
- **THEN** the exact owned fixture is removed and the result includes a verified cleanup receipt

#### Scenario: Cleanup ownership has changed
- **WHEN** the destination marker no longer matches the retained lease or a leased tree crosses a reparse point
- **THEN** cleanup preserves it and reports the failure without claiming the run fully cleaned

#### Scenario: Recovery root is not canonical
- **WHEN** a retained lease contains a noncanonical Mods root, including a trailing directory separator
- **THEN** recovery rejects the lease before mutation even if a matching marker exists at its derived path

#### Scenario: Retained lease recovery
- **WHEN** publication was interrupted after its lease was persisted
- **THEN** recovery can remove only that transaction's marked temporary or published directory and preserves unrelated paths

#### Scenario: Invalid lease appears after a valid lease
- **WHEN** any record in a fixture recovery journal has an invalid identity or canonical path
- **THEN** cleanup rejects the journal before deleting any fixture and retains the journal for diagnosis

#### Scenario: Outer cancellation interrupts runner cleanup
- **WHEN** cancellation or failure prevents the grouped runner from completing its cleanup
- **THEN** the typed operation recovers only fixture leases registered beneath its exact pre-owned artifact directory, after confirming the exact runner and launched child have stopped or positively proving no child launch was attempted
- **AND** missing, malformed, still-running or reused process identity preserves fixture data and the lease and reports incomplete cleanup together with the primary failure

#### Scenario: Cancellation precedes fixture publication
- **WHEN** the typed operation is cancelled before any fixture is registered for publication
- **THEN** recovery reports no registered fixture to clean only after proving that the exact grouped runner has stopped, and never searches other run directories
- **AND** an absent or empty catalog with missing, live, reused or unknown runner identity remains incomplete because that runner could still publish the catalog

#### Scenario: Publishing child survives the grouped runner
- **WHEN** fixture publication was attempted but the exact publishing process is still alive or its PID/start receipt is unavailable
- **THEN** recovery retains the registered obligation and reports incomplete cleanup even if no fixture lease has appeared yet
- **AND** normal timeout cleanup likewise requires proof that the publishing child has stopped before treating absent fixture paths as clean

#### Scenario: Failed native runner with successful cleanup
- **WHEN** the native runner returns a nonzero exit code and fixture cleanup succeeds
- **THEN** recovery retains that exact primary exit code and marks the primary outcome failed independently of completed cleanup

#### Scenario: Failed launch result followed by failed cleanup
- **WHEN** the child returns a failed result and fixture cleanup subsequently fails
- **THEN** the group evidence retains the original stdout, stderr, numeric exit and parseable child result alongside the cleanup error

#### Scenario: Recovery journal reaches its size bound
- **WHEN** appending the next prepared fixture lease would exceed the journal's bounded recovery read size
- **THEN** the next fixture is not published and rollback retains a valid bounded journal for any paths that could not be cleaned

### Requirement: Intentional fixture errors require exact selected accounting

The tooling SHALL allow optional fixture-owned `expectedErrors` declarations with an exact original first-line message and positive occurrence count. Only selected validated fixtures SHALL supply expectations. Manifest bytes, payload inventory and expectations SHALL be bound to immutable plan and internal expectation-file hashes. There SHALL be no caller-supplied regex ignore or general error bypass. Native timestamps and severity prefixes may be normalized; message text, context and traces SHALL remain intact in evidence.

#### Scenario: Declared malformed Def is loaded once
- **WHEN** a selected fixture declares one exact malformed-Def error and the native log emits it once
- **THEN** startup and final scans classify that diagnostic consistently, retain its exact full block and observed count, and do not reject it as unexpected

#### Scenario: Missing or repeated diagnostic
- **WHEN** the final flushed log has fewer or more occurrences than declared
- **THEN** final validation fails with the actual and required counts
- **AND** earlier incomplete scans permit missing occurrences but reject excess occurrences

#### Scenario: Live stdout writer remains open during intermediate scan
- **WHEN** the running isolated process still has its append-only Player.log writer open
- **THEN** the intermediate scan permits that writer, reads only the byte length captured at open, and classifies every complete line in that snapshot
- **AND** it records the snapshot byte length and any deferred unterminated tail without treating a partial message as an unexpected diagnostic
- **AND** final validation after exact-process shutdown denies concurrent writing, scans the complete file including an unterminated final line, and still fails on missing, excess or unexpected errors

#### Scenario: Other error accompanies an expected diagnostic
- **WHEN** an unexpected error appears, including an error with changed context or an exception adjacent to a matched diagnostic
- **THEN** validation fails and retains both the expected diagnostic and unexpected error evidence

#### Scenario: Expectations absent or unselected
- **WHEN** a v1 fixture omits optional expectations or a group does not select the declaring fixture
- **THEN** it grants no diagnostic allowance and existing error rejection remains active

#### Scenario: Fixture group projected into a native Gateway process
- **WHEN** the selected fixture receipt carries the exact metadata group, which excludes the implicit Gateway
- **THEN** smoke accepts only that ordered group with exactly one Gateway appended, matching its real launch projection
- **AND** missing, reordered or foreign packages remain invalid

#### Scenario: Informational continuation repeats expected text
- **WHEN** an INFO or WARN record contains an unprefixed continuation equal to an expected error message
- **THEN** that continuation grants no expected ERROR occurrence, including when native header severity uses padding
- **AND** a later unrelated native record and its trace are not retained as part of a preceding expected diagnostic
- **AND** when the log has timestamped native headers, an untimestamped continuation beginning with `ERROR` does not become a new native error record or satisfy an expectation
- **AND** severity-looking continuation lines remain in their original diagnostic block unless independently classified as unexpected errors; only timestamped headers delimit records in that stream

#### Scenario: Expectation inputs drift or are ambiguous
- **WHEN** expectation metadata is malformed, duplicated, outside its declared bounds, changes after planning, or its internal file differs from the launcher hash
- **THEN** publication or smoke validation fails before game launch

### Requirement: Fixture evidence does not replace native acceptance

The tooling SHALL retain planned/published file identities and cleanup evidence while keeping fixture payloads outside product and release packages. Successful host staging tests and XML diagnostic assertions SHALL remain supporting evidence; they SHALL NOT close a product's native player-workflow acceptance task.

#### Scenario: Real loader diagnostic verification
- **WHEN** the selected diagnostic group runs with the staged fixture
- **THEN** the normal XML loader consumes its original XML, while product acceptance still requires the separately specified native action and observed result
