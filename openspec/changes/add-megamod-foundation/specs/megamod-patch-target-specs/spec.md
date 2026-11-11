## Owner and delivery

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). These are mandatory
contracts for every owned patch. The initial scaffold contained zero patches; current P0 guard
components are implemented-unverified and do not constitute complete compatibility acceptance.

## ADDED Requirements

### Requirement: Document executable target specs for every patch
Every Harmony prefix/postfix/transpiler/finalizer and XML mutation SHALL have a stable patch ID and
documented target spec linked to its feature, actual startup validator, tests, and acceptance
evidence. State the target identity/shape, lifecycle, minimal predicates, permitted variation,
affected behavior, and safe failure scope. Do not patch megamod's own implementation.

#### Scenario: Review a new patch
- **WHEN** a contributor proposes a patch without a documented target spec or executable predicate check
- **THEN** it is not eligible for registration or feature acceptance

### Requirement: Guard only the assumptions the patch needs
Target specs SHALL admit semantically compatible targets without requiring a full-body match,
specific file hash/version, or absence of other Harmony patches. A simple observer SHALL require
only target existence and the shape/lifecycle it actually consumes; it MUST NOT invent body predicates.

#### Scenario: An observer has no body assumptions
- **WHEN** the exact method remains callable with the needed signature and an unrelated method block or compatible other patch has changed
- **THEN** the observer is admitted without comparing the entire method or rejecting other owners

### Requirement: Verify parameter use and structural modifications semantically
An argument-changing prefix SHALL prove that the targeted argument is still used in the documented
operation and control-flow context. A transpiler SHALL prove all required structural landmarks,
operands, relationships, and match cardinality before changing instructions. Missing, ambiguous,
unreadable, or changed required semantics SHALL fail closed without partial modification.

#### Scenario: Parameter use changes
- **WHEN** a method still exists but the argument no longer feeds the operation required by an enabled prefix
- **THEN** that prefix is not installed and startup logs identify the failed predicate and unavailable feature

#### Scenario: Harmless compilation changes
- **WHEN** offsets, local indices, or unrelated instructions change but the required data/control-flow predicates remain true
- **THEN** the patch is admitted

#### Scenario: Ambiguous transpiler matches
- **WHEN** the required unique structural region has zero or multiple admissible matches
- **THEN** the transpiler leaves its incoming instruction sequence unchanged and reports the rejected patch

### Requirement: Evaluate the relevant patched surface
Each target spec SHALL declare whether it relies on original metadata, original code, or effective
code and execution after other patches. Body-dependent guards MUST account for relevant earlier
transpilers and prefix skip/argument effects; original IL alone MUST NOT stand in for a changed
effective body. Specify startup ordering and invalidation/revalidation for supported later patch
changes. Transpilers SHALL validate their actual input on every invocation. Unknown required
semantics fail closed; the mere presence of another patch does not.

Required relationships SHALL remain valid after relevant transformations that occur later than the
owned structural check. A valid earlier check does not certify a later changed operation, including
changes to an owned observer. Revalidation must refer to the newly active result, not a stale patch
inventory. Unrelated or demonstrably compatible transformations remain eligible.

#### Scenario: Another mod removes the required argument-use region
- **WHEN** original IL passes but the relevant effective instruction stream no longer contains the required region
- **THEN** the dependent patch is skipped with a warning instead of relying on the original-only result

#### Scenario: Compatible patch coexistence
- **WHEN** another owner's patch preserves the documented target predicates and execution guarantees
- **THEN** both owners remain installed and the native workflow follows the specified combined behavior

#### Scenario: A later transformation changes a required operation
- **WHEN** a loaded modification changes a required operation after the owned structural check has passed
- **THEN** the dependent behavior remains unavailable until its required relationships are established for the active result
- **AND** a warning identifies the affected behavior and fallback without disabling unrelated compatible features

#### Scenario: A later supported repatch invalidates a predicate
- **WHEN** the supported patch lifecycle rebuilds or changes the observed surface
- **THEN** admission is revalidated before the dependent behavior runs, or that feature fails closed with a warning
- **AND** unsupported late-patching combinations remain documented limitations, not compatibility claims

### Requirement: Startup admission is idempotent and failures are isolated
Before applying any enabled patch at its documented startup phase, the mod SHALL evaluate its target
spec. It SHALL apply passing patches exactly once and skip failing ones. A declared atomic feature
group SHALL be fully admitted or left inactive; cleanup MUST remove only that group's newly added
owned patches. No unguarded global PatchAll, mutation during probing, or removal of another owner's
patches is permitted. Optional executable APIs SHALL first be gated by active canonical package ID.

#### Scenario: Validator failure and repeated initialization
- **WHEN** one enabled patch's validator throws or cannot resolve its target and initialization is repeated
- **THEN** that patch remains absent, independent passing patches appear once, and no duplicate side effects occur

#### Scenario: A dependent patch group cannot finish registration
- **WHEN** a prerequisite fails or installation fails after some patches in an atomic group were added
- **THEN** the affected feature is inactive, its newly installed owned patches are removed, and other features/owners remain intact

### Requirement: Every skipped enabled patch has an actionable bounded warning
Startup logs SHALL emit one bounded warning per skipped enabled patch with package/patch/spec ID,
target, failed predicate or exception reason, unavailable player behavior, and safe fallback if any.
Do not emit repeated per-tick/job warnings. User-disabled features are not failed candidates.

#### Scenario: Read a startup rejection
- **WHEN** an enabled target spec is false or cannot be evaluated
- **THEN** the log explicitly says that the identified patch was not applied and why
- **AND** a reader can identify the unavailable feature without interpreting a stack trace alone

### Requirement: XML mutations meet the same admission contract
Each XML patch SHALL specify required target nodes/shapes, conditional package dependencies, startup
phase, and warning/failure behavior. Check preconditions before mutation and prevent partial feature
application. XML-only modifications SHALL use XML Extensions guards to check required Defs/content
and target shapes before applying the narrowest suitable declarative operation. External content
SHALL also be gated by canonical package ID with `XmlExtensions.FindMod` and `packageId=true`;
package presence alone is insufficient. Declare XML Extensions as required when used. Do not
substitute runtime Def mutation for declarative changes.

#### Scenario: An XML target disappears
- **WHEN** an enabled XML feature's required target node is absent or incompatible during loading
- **THEN** its mutation is skipped without partial feature state and a startup warning identifies that patch and predicate

#### Scenario: An active external mod no longer supplies the target Def
- **WHEN** its package is active but the required external Def/content is missing or has changed incompatibly
- **THEN** the XML Extensions content guard rejects the mutation and logs the affected enabled patch
- **AND** unrelated features remain available

### Requirement: Preserve existing XML content through merge-safe edits
XML modifications SHALL use additive or narrowly targeted merge-preserving edits wherever possible
and sensible. Preserve unrelated fields and existing vanilla/third-party list entries; prevent
duplicate membership. Whole-list replacement SHALL require an explicit intended replacement in the
feature spec, why merging is unsuitable, exact resulting semantics, and documented discarded-content
and compatibility effects. Convenience alone is not a reason to replace a list.

#### Scenario: Extend a woody category or fuel filter
- **WHEN** a feature adds its Thing to a category/filter that already contains vanilla and third-party entries
- **THEN** all existing entries remain and the new membership is added exactly once
- **AND** an already-present membership is not duplicated

#### Scenario: Deliberately replace an entire list
- **WHEN** the specified feature intentionally removes prior choices and replaces the list
- **THEN** the target spec names the required preconditions and intended resulting list semantics
- **AND** tests cover retained/removed choices and the ledger explains conflicts with mods that extend the old list
