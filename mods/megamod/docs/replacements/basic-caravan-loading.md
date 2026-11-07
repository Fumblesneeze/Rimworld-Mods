# Basic Caravan Loading

## Identity and provenance

Owner Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`),1.7 beta. Replacement S135.
Source `LM420.BCL`, [Workshop3370434966](https://steamcommunity.com/sharedfiles/filedetails/?id=3370434966),
author LM / Workshop Shakecataz, unversioned flat XML-only package. Complete460-byte patch inspected
2026-11-07; SHA256 `2ee506cd1d979da1a637f7f8a7c8e6eff7f08e7ebfb235555ec3cb88991c45e4`.
[Research](../refinement/2026-11-07-basic-caravan-loading-research.md) contains public-first provenance,
all source hashes, native1.7 evidence and limits. No reuse license found; independent authorship.

## Behavior inventory

All behaviors reference the [owned spec](../../../../openspec/changes/specify-megamod-basic-caravan-loading/specs/megamod-basic-caravan-loading/spec.md).

| ID | Input and result | Boundaries | Status |
| --- | --- | --- | --- |
| BCL01 | Enable Basic → eligible helper gathering and unloading | Player priorities unchanged; unload before help; native higher-priority work and orders remain | Specified, unimplemented |
| BCL02 | Form caravan/request unload → native eligible physical work | Actual carrier/items/stage/capacity/access; no forced whole-colony recruitment | Specified, unimplemented |
| BCL03 | Enable S133 batch unload → same Basic authority | No extra Haul requirement; shared cargo/recovery; normal gathering and other work unchanged | Specified, unimplemented |
| BCL04–05 | Native controls/save priorities and guarded startup | No extra settings or tab; equivalent no-op, atomic group fallback | Specified, unimplemented |

## Interactions and UI audit

Direct S133 unloading composition with S021 quantities/protection, S126 visible stacks and S132
inventory presentation; S125 remains separately eligible. No internal patches or extra item cache.
Source and owned entry points are native Work/caravan/unload controls. No main tab, debug control,
configuration row or bottom-right switch. Pawn assignments are gameplay state, not global/save
settings. No new default or inheritance exemption is needed because no setting is introduced.
Basic worker eligibility is intentional; preserving mech flags does not preserve all Haul-only mech
eligibility. Pod/shuttle/portal work is not reassigned, and portal bulk loading remains excluded.

## External contracts and persistence

No source assembly/API/serialized settings/types; native WorkGiver identities and jobs are retained.
No installed external consumer found by bounded package/Workshop text search; universal compatibility
not inferred. No migration or source package impersonation. Save/removal/native job references
remain subject to runtime verification, with pawn priorities never rewritten.

## Implementation and coverage

**Nothing implemented.** [Proposal/design/tasks](../../../../openspec/changes/specify-megamod-basic-caravan-loading/)
cover both source scalar changes. Deviations: guarded XML, atomic/effective admission and rejection
of conflicting work assignments instead of loading last to overwrite them. Equivalent original
source assignments can coexist as no-op. Core/XML Extensions required at runtime; no new Harmony
requirement, other repository mod or Gateway dependency. Preserve all unrelated fields/lists.
No new batching algorithm, work category manager, source preview art or configurable taxonomy.

## Verification and acceptance

BCL-Q1 inventory resolved; BCL-Q2–4 retain effective guards, eligibility and reviewed native proof.
Progress/review tracked in [refinement](../refinement/specification-progress.md). Future tests use
honest XML/game tiers; independent review and build/package checks precede final fresh minimized
Gateway player workflows. Personally observe Basic-only pickup/delivery/unloading, competing work,
S133 enabled/disabled, exact source/provider cases and save priorities. Record causal screenshots,
build/mod/config identity and cleanup. Balance review checks job reachability and actual cargo;
Def inspection and clean logs alone do not accept the feature.
