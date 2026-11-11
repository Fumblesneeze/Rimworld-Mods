## Context

Owner: repository tooling, `RimWorldDevGateway.EndToEndHost` at
`tools/RimWorldDevGateway.EndToEndHost`.

The initial audit of the operation registry and typed `e2e_run_start` found no caller expectation
argument. At that point `RimWorldEndToEndTestAttribute`, metadata discovery and `EndToEndBundleTest`
had no diagnostic declarations. Existing XML fixtures produce a create-new, hash-bound internal
receipt from selected package plans. `RimWorldFixtureDiagnostics.Support.psm1` counts exact ERROR
first lines, rejects excess immediately and missing final counts, retains traces, and independently
rejects adjacent risky lines. Reuse that accounting rather than adding a second log sweep.

Retained native `SafeSaver.DoSave` source also emits a WARN whose first line contains the exact
save path and exception. The current risk classifier rejects that line, and its ERROR-only matcher
cannot allow it. Save/load fault scenarios therefore need explicit severity as well as message.
Exact current native shapes/counts must be supplied by root's fault-run evidence; this design does
not infer allowances from the fact that a scenario failed.

Evidence-driven revision: root's corrected save-fault run
`artifacts/EndToEndRuns/Grouped/20261111T190355846Z-36b163b8946042b7af8bbdd0baa2b2e2/`
smoke `20261111T190438353Z` retained one row ERROR, one SafeSaver WARN and one SaveGame ERROR.
Both latter records immediately continue with the exact line
` ---> System.InvalidOperationException: Expected S138 native-save fault fixture`.
The existing classifier correctly rejects those risky continuations independently; first-line
allowances alone are insufficient. The scenario stopped before retry, so final repeated-attempt
counts remain root's pending evidence, not inferred from this partial run.

## Decisions

### Compiled declaration and selection

Use repeatable development-test attributes with a stable declaration ID, severity (`ERROR` or
`WARN` only), exact original first-line template and required count 1–16. Bound declarations to 16
per test and first-line text to 4096 characters; reject blank/trimmed/control text, duplicate IDs,
ambiguous identical messages, unsupported severities and malformed token syntax. Ordinary tests
without declarations grant no allowance. Reflection/metadata readers inspect the real shared
attribute identity and never execute test code during host discovery.

The complete UTF-8 serialized receipt, including JSON escaping and all provenance, is limited to
64 KiB. Plan measures the exact serialization used by Write and rejects oversized plans before
the grouped runner deploys or stages anything. Local per-line/count bounds do not promise that
every combination fits this aggregate cap. Writer and reader retain the same 64 KiB limit.

A declaration may additionally name up to four distinct exact ordered inner-cause lines, each
bounded to 4096 characters, with the native ` ---> ` prefix and no control characters. They must
occur immediately after each matching parent first line, before other trace content or a new native
header. Each listed line is required once per parent occurrence. Only these exact continuations are
classified with their parent; a changed, missing, reordered or extra cause fails. No declaration can
exempt an independent adjacent ERROR, a cause under another parent, or arbitrary trace text.

Carry declarations through the test descriptor and versioned bundle manifest. Gateway admission
must compare compiled declarations with the manifest, just as it compares IDs, owners and limits.
Legacy manifests have no runtime diagnostic allowance. Selected runtime-expectation launches are
restricted to one exact test so a failure fixture cannot contaminate unrelated tests sharing its
active-mod group. Group selection remains supported when it resolves to that one test. This narrow
first version deliberately avoids inventing cross-test temporal attribution.

### Exact path binding

The only template token is `{savedata}`. The launcher expands it to its exact absolute isolated
savedata directory, never the normal user directory or a caller override. A token-bearing message
must use a quoted save path with a literal `Saves/<test-specific-basename>.rws` or `.rws.new` suffix.
The separator is the native platform separator; the basename is a bounded literal, not a glob.
Reject additional/repeated tokens, rooted suffixes, traversal, separators inside the basename,
invalid filename characters, or any resolved path outside the exact `Saves` directory. Declarations
without a token are literal messages. Root's fixtures use stable per-test basenames and verify
absence before taking ownership; uniqueness comes from the fresh isolated run directory.

Expansion happens before the game launches and is retained alongside its original declaration.
No test callback can register an expectation after observing the error. No prefix/substring match,
regex, GUID wildcard, arbitrary environment substitution or unconstrained path is accepted.

### Provenance and private handoff

The existing typed operation and CLI selection derive expectations from selected validated test
metadata, not new public parameters. Dry-run exposes declarations and their owner/test/assembly
provenance without creating a run or writing a receipt. The real run writes a create-new bounded
internal receipt with exact ordered active package IDs, test ID, assembly identity/MVID/hash,
manifest hash, declarations and a content hash. Smoke verifies the staged artifacts and the exact
selected test/active group including the appended Gateway before launch, then retains templates
and resolved paths/messages in `selected-test-expectations-resolved.json`. XML and test declarations
retain distinct provenance; duplicate/ambiguous matching rules across them are rejected.
Owner membership uses the full launched group, so Gateway-owned test receipts work while Gateway
remains forbidden in the declaration's pre-Gateway package sequence and appended exactly once.

### Existing accounting and fail-closed behavior

Reuse the existing stable-snapshot intermediate/final scans, timestamp/severity parsing and full
trace evidence. XML fixtures retain ERROR-only exact first-line matching and their current schema.
New selected-test rules match the declared native severity and exact resolved first line only.
FATAL is never expected. A matching record cannot exempt another ERROR, a changed exception, or an
independently risky adjacent continuation. WARN declarations are explicit and countable, not a global
warning suppression. Intermediate scans permit not-yet-emitted declared occurrences but reject
excess/unexpected records; the final post-shutdown scan requires exact counts and no unexpected
diagnostics. Missing/changed evidence or failed cleanup remains a failure independently.

## Scope and verification

### Terminal E2E completion

The selected late-load failure run on 2026-11-11 reached a passing, cleaned E2E terminal snapshot,
then the adapter continued generic smoke probes and waited for Playing. A rejected partial game
need not return to Playing; prolonging that process allowed later native update errors. The host
must proceed from validated terminal evidence to its existing shutdown/accounting/cleanup path.
E2E owns its initial readiness and player workflow. Its runtime snapshot supplies the actual ordered
active package list, checked against the launch plan. Generic Def export, REPL/UI/action probes,
post-workflow playable waits and generic screenshots belong only to ordinary smoke mode. Skipped
probe artifacts are null in the result. Intermediate/final log accounting and cleanup remain shared;
no new allowance or caller flag is introduced. Root owns the fresh native proof.

Reuse `RimWorldModding.Mcp` host tests, including existing source-linked metadata/CLI/XML fixture
tests. No new host tier, loaded mods or game launch is needed for RED/GREEN accounting tests. Exercise
real metadata discovery and private runner invocation shapes, including empty and singleton lists.
Keep selected-test metadata and matcher changes in small RED/GREEN slices; record durable evidence.
Root owns all native fixture edits, installation and isolated native acceptance on the reviewed build.

The source lease includes shared E2E contracts/manifest; EndToEndHost metadata, discovery, planner,
CLI and expectation projection; Gateway E2E compiled-metadata admission; the existing grouped/smoke
scripts and support modules; existing host tests and relevant documentation. Product source and
native fixture implementation are excluded. Root build freezes suspend edits to compile/runner
inputs. Independent review and final live tooling proof remain mandatory after implementation.
