# Add Deconstruct Button To Floor Category — replacement coverage

## Identity and provenance

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Origin: replacement, S067/P1-61. Source: GeodesicDragon's **Add Deconstruct Button To Floor Category**,
`geodesicdragon.deconstructfloorcategory`, [Workshop 2905837332](https://steamcommunity.com/sharedfiles/filedetails/?id=2905837332),
installed 1.1.4, inspected 2026-11-04. Complete two-file XML implementation and author-installed
documentation inspected; Steam page access failed. No source license file found or source copied.
[Research and hashes](../refinement/2026-11-04-floor-order-shortcuts-research.md).

Status: **observable draft independently reviewed and validated. Nothing implemented.**
[Proposal](../../../../openspec/changes/specify-megamod-floor-order-shortcuts/proposal.md),
[design](../../../../openspec/changes/specify-megamod-floor-order-shortcuts/design.md),
[spec](../../../../openspec/changes/specify-megamod-floor-order-shortcuts/specs/megamod-floor-order-shortcuts/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-floor-order-shortcuts/tasks.md).

## Behavior inventory and implementation coverage

All rows are specified only; no code/test/live evidence exists. FOS IDs refer to the linked spec.

| Behavior | Native input → result | Selected boundary / deviation | Status |
| --- | --- | --- | --- |
| FOS01 | Open Floors → Deconstruct; Orders → Remove Floor | Keep originals and unrelated entries; no additional equivalent membership | Specified, unimplemented |
| FOS02 | Click/drag/cancel → native designation and real removal work | Current support/fog/ownership, refunds and native instant exceptions retained; floor is not foundation | Specified, unimplemented |
| FOS03 | Orders mouse selection; Floors keyboard selection | Proposed Orders mouse-only correction avoids shared-key display-order dependence; native fallback if correction unavailable | Specified, unimplemented |
| FOS04 | Startup with altered/optional content → independent supported contributions | Guarded additive XML; original supplies placement when active; no foreign list replacement | Specified, unimplemented |
| FOS05 | Search/page/reload/use current action policies → ordinary native commands | S036/S008/S061 direct composition, no settings/save migration; S136 sampling remains separate | Specified, unimplemented |

## Interactions and UI audit

The source only adds commands in existing Architect categories. Both are ordinary contextual
gameplay actions; they stay there. No bottom tab, setting, bottom-right toggle or extra debug UI is
selected. Native labels, descriptions and icons remain. Any new player-facing explanation needs
all seven languages. Pending orders are gameplay state, not inherited configuration.

S036 Architect presentation must preserve both commands' identities/current eligibility and the
Orders-specific binding policy even with optimization off. S008/S061 action policies remain;
no self-patching layer. S136 Floor Pipette remains separately queued and cannot activate removal
merely by sampling. No new shared-data demand, private cache or optimization claim.

## Contracts, deviations and provider policy

Source has no DLL/API, custom saved state, options or translations. Scoped installed XML/CS/JSON
consumer search found only the RimPy database's package catalog entry, not a runtime binding;
this does not prove absence of every possible consumer. No source package identity is spoofed.
Keep native command types and action semantics instead of a copied/subclassed removal tool.

Original active: select it as placement provider, skip owned additions regardless of relative load
order, and independently admit the binding correction against actual native Orders instances.
A changed original missing a shortcut is reported as a provider gap, not silently promised parity.
Other already-present native entries are preserved without an extra append. No universal promise
to deduplicate later foreign changes or label-similar custom tools.

The original XML appends unconditionally. Ours checks required content and preserves lists,
including one-step creation of a valid missing container. Each patch has its own target spec and
startup warning; compatible foreign patches are allowed. XML and binding correction are independent:
if only the latter fails, commands keep native/provider key allocation with an explicit warning.
This can make shared-key ownership display-order dependent; no corrected-keyboard claim in fallback.

XML Extensions is required at first XML use; its inspected 1.6 folder does not prove 1.7 support.
Harmony required on eager use. Original source optional; no other repository mod, DLC or Gateway
dependency. No assemblies/artwork copied and no source-specific migration or user mod-list edit.

Both upstream additions are selected. Deliberately changed: unconditional appends and proposed Orders
keyboard claim. Not selected: any new removal mechanics, global key reassignment or automatic foreign
duplicate removal. All actual implementation remains missing; the source is not yet replaced.

## Verification and remaining work

FOS-Q1–3 retain proposed key-policy review, exact dependency/effective-target/lifecycle proof and
focused tests plus native acceptance. Actual click/key/drag/cancel → designation → pawn-work,
S036/S061 composition, source-present/absent/load-order runs, localization and pending-order reload
remain unchecked. No live compatibility accepted from XML inspection or a clean startup.

Semantic balance pass: extra navigation does not unlock targets/research, shorten work, change
refunds or grant roof immunity. Native direct-removal exceptions stay explicit. No runtime build,
installation, tests, game launch or normal save/configuration mutation occurred.

Independent source/native/composition review found no actionable P1/P2 issues. The draft has
**5 requirements, 16 scenarios and 18 unchecked tasks**; strict typed validation passed **89 items,
zero failures**. All eight scoped Markdown documents passed local-link/whitespace checks. The
separate CLI telemetry transport error does not change that validation result.
