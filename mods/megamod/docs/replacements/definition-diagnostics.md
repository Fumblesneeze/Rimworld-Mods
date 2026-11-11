# Log After Def Error (Continued) — replacement coverage

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S094/P2-23; canonical `mlie.logafterdeferror`, Workshop 3550944748, selected file 1.7.1/assembly
1.7.0.0. **Status: initial runtime composition implemented-unverified; no feature acceptance.**

## Specified coverage and corrections

Attach current definition name/kind, known source mod/package and XML path to ordinary accepted
definition-conversion warnings and errors. Preserve original text, severity, available trace,
native failure outcomes and truthful unknown fields. Context describes what was being converted,
not an unproven culprit. Inspect/copy it through the ordinary log and file; no new bottom tab or
upload workflow. One enabled-by-default global-only common setting explains its process scope.

[Proposal](../../../../openspec/changes/specify-megamod-definition-diagnostics/proposal.md),
[observable contract](../../../../openspec/changes/specify-megamod-definition-diagnostics/specs/megamod-definition-diagnostics/spec.md),
[design](../../../../openspec/changes/specify-megamod-definition-diagnostics/design.md),
[tasks](../../../../openspec/changes/specify-megamod-definition-diagnostics/tasks.md),
[source/binary/native research](../refinement/2026-11-05-definition-diagnostics-research.md).

The installed continuation only adds an extra informational Message from a Warning prefix and
does not include defName. Owned Error coverage, actual definition identity and one-event enrichment
are deliberate improvements, not claims about the source. Direct [S035 composition](better-log.md)
keeps mute/limit/once/announcements tied to the original severity, avoids a second cap charge and
does not let white context escape a muted warning. Nested unknown sources remain unknown; root
paths are checked structurally; exceptions/reloads cannot leave stale context.

## Contracts and exclusions

The first P0-A host slice implements exact active-provider decisions and initial pure reporting-group
admission orchestration. Four provider cases and sixteen coordinator cases are green. Independent
review's candidate-list mutation and hidden cleanup-failure defects each have focused regressions;
the cleanup-warning fix passed reviewer recheck. Four real scoped Harmony receipt tests and their
independent review also pass. A further six-test tranche supplies the common global option,
immutable worker snapshot and nested/concurrent/exception-safe context scopes; its review passed.
Eleven formatter tests cover original-text preservation, truthful paths and reversible escaped
metadata; independent review and the escaping-collision recheck passed. The actual native-output
adapter has eighteen scoped Harmony cases. Its accepted lock-inversion and suppression-bypass review
findings passed recheck. Logging reads published state; after a rebuild, explicit safe
refresh must verify final composition outside logging locks before context can resume.
The actual-body converter bridge has eight passing scoped Harmony cases and source capture has six
passing Unit cases. `DefinitionDiagnosticsRuntime` composes actual Verse converter/log types.
Subsequent reporting/ownership tranches compose S035 warning capture independently of
context availability. Root has now registered both common global definitions before settings load,
constructed their runtime owners before XML conversion, connected their existing settings UI and
scheduled safe reporting refresh before content-related early returns. This central integration
passed independent review after an actual isolated XML-loader RED showed missing definition context.
The following real loader run exposed an unrelated Error overload falsely counted as ambiguity;
its focused correction passed39 host cases and independent review. The reviewed native selection/Copy
tracer then passed all66 steps, with all12 captures personally inspected: distinct source contexts,
native selection/scrolling and full copied output were observed. A separate intermediate log-file
sharing defect then received focused open-writer/tail tests and independent review. Fresh run
`20261107T221824752Z-f0469e9c06ba45738ebb7a1c4022b2cd` passes the complete runner and the same66-step
native workflow; root inspected its own12 screenshots, complete file diagnostics and cleanup.
Normal configuration is unchanged. This does not accept the complete feature. The target documents and
[native scenario authoring notes](definition-diagnostics-native-scenarios.md) remain contracts;
executable cases now exist in the root `tests/Megamod.EndToEndTests/Diagnostics` project folder:
`megamod.diagnostics.initial-xml-preflight` and `megamod.diagnostics.definition-initial-default`.
Intentional fixture errors use exact selected-fixture accounting in the separately owned
verification tooling; they are not silently ignored. Exact native evidence is recorded in
`artifacts/Implementation/megamod-p0/early-diagnostics-composition.md`. Local RED/GREEN receipts live under
`artifacts/Implementation/megamod-p0-a/tdd-ledger.md`.

Original `ordpus.logafterdeferror` or continued `mlie.logafterdeferror` presence disables owned S094
overlap while leaving independently admitted log features available. No source settings, API facade,
package identity or VersionFromManifest dependency is imported. Bounded installed scans found only
source assemblies and recommendation metadata, not a concrete external callable consumer.

No XML-read/configuration/cross-reference/assembly/gameplay attribution outside conversion, causal
patch history, automatic repair, altered parsing values, retrospective annotation, extra history
database or arbitrary external-logger guarantee. Native top diagnostic access remains developer UI;
ordinary automatic startup reporting and file capture remain available with developer mode off.

DDI-Q1–3 retain exact semantic admission, scoped/deferred logging composition, provider evidence,
localization and reviewed-build native acceptance. The initial default-loader workflow now has
personally observed evidence; broader tasks remain unchecked for their unrun reload, option,
legacy/provider and composition obligations. [Progress ledger](../refinement/specification-progress.md).
