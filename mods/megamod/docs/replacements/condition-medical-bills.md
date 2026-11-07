# Condition-driven medical bill filtering

## Identity and provenance

Sole owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin **original**, S137, user request2026-11-05; no upstream feature identity to impersonate.
[Intake](../refinement/2026-11-05-condition-medical-bill-filter-intake.md),
[exact research/provenance](../refinement/2026-11-07-condition-medical-bills-research.md), and
[owning change](../../../../openspec/changes/specify-megamod-condition-medical-bills/).
UI references are Dubs Mint Menus (`Dubwise.DubsMintMenus`, Workshop1446523594,1.3.1704) and Compact
Hediffs (`PeteTimesSix.CompactHediffs`, Workshop2031734067, pinnedv1.6). Research records the exact
hashes, inspected current native1.7 behavior, public sources, licenses and limits. Independent code
is planned; no source DLL or art is included. Mint's unrelated menus are outside this request.

## Behavior inventory and coverage

Every row is **specified only; nothing implemented or live verified**. The
[observable specification](../../../../openspec/changes/specify-megamod-condition-medical-bills/specs/megamod-condition-medical-bills/spec.md)
owns all rows below; future tests, target specs and native evidence remain unchecked tasks.

| ID | Input/action → observable result | Defaults, deviations and boundaries |
| --- | --- | --- |
| HBF01 | Click condition → adjacent recipe filter and selection identity | No bill, health, policy or work changes; existing queue intact |
| HBF02 | Click grouped/folded condition → exact instances and deduplicated union | Snapshot visible members, exact left/right parts; separate log/info/care controls |
| HBF03 | Inspect matches → relevant actual removal/change/restoration/descendant effects | Explain destructive effects and retained conditions; no optimal-cure claim |
| HBF04 | Select foot infection → containing-leg removal when admitted | Selected narrow ancestor eligibility extension for the user example; exact target/provider verification remains |
| HBF05 | Encounter unfamiliar effect → bounded heuristic in the same match list | Specific researched corrections/exclusions; no arbitrary label inference or eligibility bypass |
| HBF06 | Search/change/clear → predictable intersecting catalog | Uniform label/target ordering without confidence UI, text and condition clearing separate, no-match is not incurable |
| HBF07 | Health/patient/availability changes → current selection/options | Paused revalidation, no same-name retarget, transient view state only |
| HBF08 | Choose operation → native confirmation where applicable and player bill | Existing quill bill shows Already queued and opens its controls without adding/resuming; ordinary edit takeover retains automatic origin/holds; native work/cost/risk |
| HBF09 | Incompatible seam/provider → usable fallback and bounded warning | No partial click capture or blanket other-patch veto; optional content absent-safe |

## UI audit and composition

| Reference surface | Purpose | Owned destination and intentional change |
| --- | --- | --- |
| Mint Health Operations recipe sidebar | Search/choose surgery | Health contextual pane using current native actions; no full Mint menu replacement |
| Compact grouped/folded health row | Read condition details; native event click | Primary click filters when S137 active; separate labelled Combat log action retains navigation |
| Info/care/provider controls | Inspect condition or change care for this patient | Preserve independent hit targets and exact selected instance sets |
| Search/filter marker | Temporary current-patient browsing | Pane-local state, cleared on view/patient/game change; no settings menu or save override |

S007's unimplemented display draft is amended directly. S062 care and S089 serum targeting keep
their distinct semantics. S124/S134 receive ordinary player orders through their common authority;
selection never orders drugs, resets holds or changes policy. One shared medical catalog/current
patient projection replaces per-provider static caches. No internal Harmony patch stack.

## External contracts, dependencies and persistence

Core required; Harmony required when used. Odyssey/other recipe providers optional and resolved by
actual content. Original Compact/Mint are references, not dependencies; an unverified active overlap
keeps the external provider and withholds the owned overlapping filter/view with a warning. No
verified incoming consumers of a new S137 API, no package/assembly spoof, no blanket drop-in claim.
Optional corrections require canonical package/Def/effect contracts; unsupported corrections fail
locally. No XML alteration planned; any later external XML patch must use guarded merge-preserving
XML Extensions operations and declare the dependency. Every actual native patch needs semantic
startup admission and effective/incoming-code handling, atomic owned rollback and bounded warnings.

No saved health/bill type or filter schema is added. Native bill and medical ownership persistence
remain with their established owners; removing the filter must not erase orders or conditions.
No original settings import. Any future actual settings must use the common global/save system;
current patient selection is not such a setting.

## Omissions, risks and acceptance

No arbitrary-code effect proof, automatic treatment, universal cure discovery, whole Mint feature
set, native duplicate-implant-worker repair or source API facade is included. Unknown operations
remain available without the filter. The ancestor exception is explicitly proposed because source
matching alone cannot produce the user's example; its retained decision precedes implementation.

All runtime tasks remain open: semantic target specs, focused RED/GREEN, seven-language UI,
scoped build/package checks, independent implementation review, current-provider tests and personally
observed real player workflows on the reviewed build. Required cases include the three examples,
actual surgery results, grouped/left-right/folded conditions, uniform match presentation, clear/search, stale paused
options, confirmation cancellation, log/info/care actions and shared drug/quill ownership. Finalized
balance compares foot removal, containing-leg removal and replacement without free treatment or
changed surgical risk. HBF-Q2–5 track remaining work; no game was launched for this draft.
