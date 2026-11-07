# Growth Speed Makes Sense Now

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S088/P2-17; `jdalt.gsmsn`, Jdalt / Workshop Jdalt40,
[Workshop2969141187](https://steamcommunity.com/sharedfiles/filedetails/?id=2969141187).
[Research](../refinement/2026-11-05-child-growth-learning-research.md) pins complete source/latest
shipped1.5 binary, native beta paths and actual consumer evidence.

[Proposal](../../../../openspec/changes/specify-megamod-child-growth-learning/proposal.md),
[observable spec](../../../../openspec/changes/specify-megamod-child-growth-learning/specs/megamod-child-growth-learning/spec.md),
[design](../../../../openspec/changes/specify-megamod-child-growth-learning/design.md),
[tasks](../../../../openspec/changes/specify-megamod-child-growth-learning/tasks.md).

**Implemented: nothing.** The draft multiplies future child growth-point rate by the pawn's effective
Global Learning Factor once, including native growth-vat rates. Native eligibility, learning needs,
aging, skill XP, toddler development, tier thresholds, birthday choices and historical points remain
under their existing rules. It adds no plant-growth behavior; earlier plant references are corrected.

The ordinary contextual growth display and supported native rate consumers share the same result.
No new settings, tab, per-pawn editor or private cache. Any new availability text uses all seven
languages. Core/Harmony required when implemented; Biotech required only for this feature, optional
for the overall package. Original GSMSN and other providers remain optional.

The full one-postfix source behavior is drafted. Deliberate deviations are finite-rate safeguards,
semantic admission, bounded diagnostics, source overlap suppression and correct Harmony dependency
metadata. Source/API/package impersonation, reuploads, save migration and retrospective scaling are
not promised. Native getter identity is retained for actual readers such as School & Learning;
that included-source finding is not full current-binary compatibility proof.

CLG-Q1–4 retain finalized balance, exact effective/native/numeric/cache contracts, S031/S103 and
optional-provider proof, implementation/localization and reviewed-build native player workflows.
Every runtime task remains unchecked. No foreign source/DLL/art or Gateway dependency is shipped.
