# Consciousness Patch replacement

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, **S046 / P1-38**. Status: **analyzed; observable draft independently reviewed/validated**.
No runtime implementation, beta compatibility acceptance or source cap-defect reproduction.

## Source and intent

**Consciousness Patch**, Vex, `Vex.Consciousness`, Workshop
[2913127696](https://steamcommunity.com/sharedfiles/filedetails/?id=2913127696), Manifest1.2,
supports1.4–1.6, requires Harmony. Latest installed 1.6 binary is an analysis reference, not a proven
beta-loaded assembly. Matching [published source](https://github.com/Vexthecollector/Consciousness-Patch)
revision `a445e47cd55c6e386dfc228cf54d7b28b3ae832a` has MIT copyright2023Vexthecollector.
[Research](../refinement/2026-11-03-physiological-consciousness-research.md) records documentation,
complete source/decompilation, hashes, native IL, settings, consumers and static balance examples.
No source code, assembly, localization or art is copied into the product.

Enhanced pumping/breathing/filtration should improve consciousness while real health limits continue
to matter. [Proposal](../../../../openspec/changes/specify-megamod-physiological-consciousness/proposal.md),
[design](../../../../openspec/changes/specify-megamod-physiological-consciousness/design.md),
[observable contract](../../../../openspec/changes/specify-megamod-physiological-consciousness/specs/megamod-physiological-consciousness/spec.md)
and [tasks](../../../../openspec/changes/specify-megamod-physiological-consciousness/tasks.md) own the work.

## Coverage and deliberate changes

| Behavior | Specified result | Implemented / missing |
| --- | --- | --- |
| Three physiological contributions | CO-01: relative weights20/20/10%, multiplicative benefits, retained impairment | Nothing; actual native/provider integration and acceptance open |
| Optional ceiling | CO-02: cap each input, off by default, stored300%, range100–1000% | Nothing; correct source cap-on transformation instead of reproducing malformed IL |
| Health composition | CO-03: native brain/pain/health rules, all supported pawn kinds, final S045/other maxima | Nothing; direct composition, no internal self-patch or promised immunity |
| Five settings | CO-04: common globals/save overrides, weights10–100%, dormant values, main-menu guards | Nothing; replaces global-only source page and absent installed translations |
| Application timing | CO-05: coherent next-unpaused-update application with pending status and actual native health refresh | Nothing; replaces startup-inlined settings and per-draw writes |
| Compatibility | CO-06: atomic supported behavior, retained foreign contributions, original suppression and warnings | Nothing; semantic incoming checks and owned-only rollback remain required |

Source supplies no bottom tab, debug action or operational toggle. All configuration stays in the
structured mod settings menu. Native Health details remain the gameplay information surface; no
parallel value/cache. Pawn injuries, organs and conditions are gameplay state, not settings.

## External contracts and dependencies

Native Consciousness/capacity/impactor information, health limits and save identity remain native.
The three inputs retain ordinary provider semantics and actual acquisition costs; no required organ
pack or new organ Def. S045 malnutrition ceilings are applied after the single physiological result.
S007 Health UI and later HUD work must display that same native value.

No outside executable consumer of the source namespace/package/Harmony owner was found in bounded
installed scans. No speculative facade, source settings import or package spoof. Stored source keys
and the defective cap transformation are not promised as drop-in contracts. Future imports need a
separate backup, atomicity, rollback and verified postcondition contract.

Core and Harmony (`brrainz.harmony`) required when implemented; no DLC/provider is required. No XML
operation is needed for S046; S045 owns its XML Extensions requirement independently. Do not bundle
source/private Harmony/Gateway assemblies. Active `Vex.Consciousness` suppresses the owned overlap
with a bounded startup warning; never unpatch the original. Other mods are admitted by needed
semantics, not rejected for having patched a method.

## Gaps, risks and acceptance

CO-Q1–4 track exact incoming/effective targets, native policy/health lifecycle, finite provider
arithmetic/finalized balance, translations and reviewed-build native evidence. Multiplicative boosts
can materially change play; source defaults/ranges are retained provisionally and have not been
certified for all organ packs. Ordinary native death/downing consequences can change and are not
automatically undone by reverting a policy. Safe arbitrary mid-save provider removal is not claimed.

All implementation tasks remain unchecked. Source/IL inspection and document validation do not prove
gameplay. Final evidence must show native surgery/health and real settings/resume/save workflows in
fresh minimized isolated games, exact provider/S045 combinations, personally inspected causal views,
reviewed package identity, normal-configuration protection and cleanup.
