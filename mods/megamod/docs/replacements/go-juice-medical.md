# Go-juice is medical

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Replacement S086/P2-15; `oriom.gojuicemedical`, author orioM / Workshop display PYAS,
[Workshop2971408015](https://steamcommunity.com/sharedfiles/filedetails/?id=2971408015).
Installed1.4–1.6 distribution is a single XML scalar change, with no DLL or settings.
[Research](../refinement/2026-11-05-go-juice-medical-research.md) retains hashes and native evidence.

[Proposal](../../../../openspec/changes/specify-megamod-medical-go-juice/proposal.md),
[design](../../../../openspec/changes/specify-megamod-medical-go-juice/design.md),
[tasks](../../../../openspec/changes/specify-megamod-medical-go-juice/tasks.md).

**Implemented: nothing.** The draft changes existing go-juice to Medical for all pawns, retaining
chemical risks/effects, dependency, dose/cost, policy entries and identity. Category-specific
recreational/hard-drug consequences cease; generic drug rules and old memories remain. It creates
neither a safe drug nor a tending medicine and never supplies or administers doses for free.
Native policy-scheduled use/carrying can permit Medical go-juice when manual inventory willingness
would refuse it; manual use retains its dependency exception and ordinary checks. Generic ingestion
consequences still apply. No new universal permission or prohibition is introduced.

Intentional safeguards: guarded/idempotent single-field XML, unchanged unrelated nodes, original
provider owns overlap, and bounded warnings for unsupported target content. XML Extensions and its
Harmony dependency are required with implementation; no foreign assets or code are bundled.

Public no-binge wording is not adopted as a guarantee: the inspected native Any-category and actual
binge target rules can still admit Medical go-juice. GJM-Q1 retains a stronger optional future policy
decision. The inspected beta already offers permitted medical drugs through native drafted inventory
controls, so no duplicate Common Sense workaround is added. No new tab, setting or private cache.

GJM-Q2/Q3 retain exact effective XML/dependency/provider checks, native workflows, localization,
balance and composition with drug policy/response, treatment, supplies and S085. Source API/save
impersonation or migration is unnecessary and not promised. Every runtime task remains unchecked.
