## Context

Owner: repository tooling RimWorld Dev Gateway. The first minimized checkbox verification failed
under `artifacts/EndToEndRuns/Grouped/20261107T163846806Z/smoke-001/20261107T163917578Z`.
Its failure diagnostics and cleanup-status artifact exist, but no final summary exists because the
adapter exits before calling completion metadata and summary persistence. The success result is
also null when failure occurs before its construction.

## Decisions

Keep the existing typed smoke/E2E operation and internal PowerShell adapter. Construct a minimal
failure result from already captured values when no success result exists. Add final hashes and
cleanup references before the common summary writer. Set failed status whenever aggregate failures
exist, even if the scenario originally produced a passed result. Persist before returning nonzero.
Do not reread or invent historical before-hashes, process identity or build evidence.

Failure messages are untrusted diagnostics and must not copy the live bearer token into the new
summary. Redact that token when copying messages; retain the existing writer's final token check.
Use null identity values for a process that never launched. Summary failure remains nonzero and
reports the evidence directory; it must not erase prior failure/cleanup reporting.

Host tests execute the actual final control-flow tail in isolated PowerShell with supplied recorded
inputs and temporary output files, exercising the real summary writer. They launch no RimWorld,
read no user configuration and fake no active game. Source-link the existing pure launcher fixture
into the current net8 MCP test environment. Root owns final review and real failed-run acceptance.

## Risks / Trade-offs

Filesystem failure can prevent any summary from being written; report that failure explicitly and
keep nonzero status. A before-hash absent in an old run cannot be reconstructed after the fact.
The existing failure diagnostics/cleanup artifacts remain separate supporting records.
