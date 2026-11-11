# Selected E2E diagnostic expectations

Owner: repository tooling, `RimWorldDevGateway.EndToEndHost` at
`tools/RimWorldDevGateway.EndToEndHost`.

## ADDED Requirements

### Requirement: Only selected compiled tests grant diagnostic expectations

The existing typed E2E lifecycle SHALL derive runtime expectations from bounded declarations on the
exact selected compiled test. It SHALL retain test, owner, assembly identity/MVID/hash and ordered
active-mod provenance. It SHALL NOT accept a caller error-ignore option. A launch containing a test
with runtime diagnostic declarations SHALL select exactly one test. Gateway admission SHALL reject
manifest/compiled declaration drift. Unselected or undeclared tests SHALL grant no allowance.

#### Scenario: Selected fault test declares one exact diagnostic
- **WHEN** one selected compiled test declares one ERROR first line with count one
- **THEN** dry-run shows that declaration and provenance without mutation
- **AND** the real run derives a hash-bound internal receipt for that exact test and active group

#### Scenario: Selection or declaration provenance changes
- **WHEN** the selected test, ordered package list, assembly identity/hash, manifest declaration or receipt differs
- **THEN** planning or admission fails without granting a diagnostic allowance

#### Scenario: Other tests share the active group
- **WHEN** a selected runtime-expectation test would run with another test in the same process
- **THEN** planning refuses that selection and requires an exact single-test run
- **AND** selecting an ordinary sibling test alone grants none of the fault test's allowances

### Requirement: Declarations bind exact bounded severity and messages

Declarations SHALL name a distinct stable ID, explicit ERROR or WARN severity, an exact first-line
message and required count 1–16. At most 16 declarations per test and 4096 characters per message
SHALL be accepted. Malformed, duplicated, ambiguous or out-of-bound declarations SHALL fail.
FATAL, regular expressions, substrings and general wildcard matching SHALL NOT be supported.
The complete serialized UTF-8 selected-test receipt, including JSON escaping and provenance,
SHALL fit within 64 KiB. Planning and dry-run SHALL reject a larger receipt before deployment or
staging even when every individual declaration satisfies its local bounds. The writer and reader
SHALL enforce the same aggregate limit.

#### Scenario: Individually valid declarations exceed the aggregate receipt bound
- **WHEN** valid messages or inner causes would serialize with their provenance to more than 64 KiB
- **THEN** planning fails before deployment, staging or receipt creation
- **AND** JSON escaping and UTF-8 serialization contribute to the measured size

#### Scenario: Exact severity and count match
- **WHEN** the selected test emits the exact declared first line at its declared severity and count
- **THEN** accounting retains that full diagnostic block and exact observed count as expected evidence
- **AND** WARN and ERROR remain distinct declarations

#### Scenario: Missing, extra or different diagnostics
- **WHEN** final evidence has a missing/extra occurrence, changed first line, wrong severity or unrelated error
- **THEN** final validation fails and retains actual and required evidence
- **AND** intermediate scans allow future missing occurrences but reject excess or unexpected diagnostics

### Requirement: Declared inner causes belong only to their exact parent

The tooling SHALL accept at most four optional distinct exact ordered inner-cause lines, each at most 4096
characters with the native ` ---> ` prefix and no control characters. Each cause SHALL be required
once immediately after each matching parent first line and before other trace content or another
native header. No arbitrary continuation or separate diagnostic SHALL receive an allowance.

#### Scenario: Native save wrapper carries its known inner exception
- **WHEN** an expected WARN or ERROR is immediately followed by its exact declared inner-cause line
- **THEN** accounting retains and counts that cause within the matching parent diagnostic
- **AND** the same text beneath an undeclared parent grants no allowance

#### Scenario: Inner cause changes or adjacent error occurs
- **WHEN** the cause is missing, changed, reordered, repeated or preceded by other trace content
- **THEN** accounting fails with parent and cause evidence
- **AND** a separate adjacent ERROR or undeclared risky continuation remains unexpected

### Requirement: Variable save paths use only exact owned expansion

The only message token SHALL be `{savedata}`, expanded before launch from the exact isolated run's
savedata directory. It SHALL occur at most once in a quoted path with a literal bounded basename
under `Saves`, ending in `.rws` or `.rws.new`. No other token, traversal, rooted suffix, wildcard or
out-of-root path SHALL be accepted. The template and resolved exact message/path SHALL be retained.

#### Scenario: Native save warning contains the isolated path
- **WHEN** a selected test declares WARN with the closed token and its literal owned save basename
- **THEN** only the exact first line containing that run's resolved path can match
- **AND** another run's path or another filename remains unexpected

#### Scenario: A binding escapes or broadens the path
- **WHEN** a declaration adds another token, traversal, basename separator or wildcard
- **THEN** validation fails before launch

### Requirement: Existing log safety and XML fixture behavior are preserved

The tooling SHALL reuse existing intermediate/final snapshot accounting and preserve XML fixtures'
exact ERROR-only declarations. Full logs and traces SHALL remain intact. Matching a diagnostic SHALL
NOT exempt adjacent independent risky text or later native records. Final accounting SHALL occur
after exact-process shutdown and require complete stable evidence; scenario/cleanup failures SHALL
remain failures even when diagnostic counts match.

#### Scenario: Expected diagnostic is adjacent to an unrelated exception
- **WHEN** an expected record is followed by an unrelated ERROR or independently risky continuation
- **THEN** accounting retains both and fails for the unexpected diagnostic

#### Scenario: Existing selected XML fixture runs unchanged
- **WHEN** a selected XML fixture emits its declared exact ERROR once
- **THEN** existing matching, count, provenance and cleanup behavior remains unchanged
- **AND** INFO/WARN lookalikes and unselected fixture declarations grant no allowance

#### Scenario: Native workflow produces the declared fault
- **WHEN** root runs a reviewed selected fault scenario through actual native actions
- **THEN** exact log accounting supplements the personally observed player-visible failure behavior
- **AND** passing accounting alone does not accept product behavior or cleanup
