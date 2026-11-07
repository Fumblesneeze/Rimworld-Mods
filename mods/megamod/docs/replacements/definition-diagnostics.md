# Log After Def Error (Continued) — replacement coverage

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S094/P2-23; canonical `mlie.logafterdeferror`, Workshop 3550944748, selected file 1.7.1/assembly
1.7.0.0. **Status: independently reviewed and strictly validated observable draft; nothing implemented or accepted in game.**

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

Original `ordpus.logafterdeferror` or continued `mlie.logafterdeferror` presence disables owned S094
overlap while leaving independently admitted log features available. No source settings, API facade,
package identity or VersionFromManifest dependency is imported. Bounded installed scans found only
source assemblies and recommendation metadata, not a concrete external callable consumer.

No XML-read/configuration/cross-reference/assembly/gameplay attribution outside conversion, causal
patch history, automatic repair, altered parsing values, retrospective annotation, extra history
database or arbitrary external-logger guarantee. Native top diagnostic access remains developer UI;
ordinary automatic startup reporting and file capture remain available with developer mode off.

DDI-Q1–3 retain exact semantic admission, scoped/deferred logging composition, provider evidence,
localization and reviewed-build native acceptance. All implementation tasks stay unchecked until
real definition loading followed by native log inspection/copy and correlated file evidence is
personally observed. [Progress ledger](../refinement/specification-progress.md).
