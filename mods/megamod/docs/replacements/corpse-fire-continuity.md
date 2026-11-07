# Keep Burning, Corpse!

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement. S091/P2-20. **Specified draft; nothing implemented or live-verified.**

## Identity and provenance

Tixiv, `tixiv.keepburningcorpse`, [Workshop 3717835691](https://steamcommunity.com/sharedfiles/filedetails/?id=3717835691).
Selected 1.6 DLL 1.0.0.0, Harmony dependency. Complete source/binary and native causal-chain findings,
hashes/revision, reuse limits and KBC-Q1–3: [research](../refinement/2026-11-05-corpse-fire-continuity-research.md).
[Change](../../../../openspec/changes/specify-megamod-corpse-fire-continuity/proposal.md),
[design](../../../../openspec/changes/specify-megamod-corpse-fire-continuity/design.md),
[spec](../../../../openspec/changes/specify-megamod-corpse-fire-continuity/specs/megamod-corpse-fire-continuity/spec.md),
[tasks](../../../../openspec/changes/specify-megamod-corpse-fire-continuity/tasks.md).

## Behavior inventory

| ID | Action -> observable result | Defaults / boundaries / evidence | Owning requirement / state |
| --- | --- | --- | --- |
| KBC-01 | Burning pawn dies -> fire at actual placed corpse cell | Prior strength/instigator; no faction/species limit beyond native support; source/native placement chain | Continue an existing flame at the placed corpse / specified |
| KBC-02 | No remaining flame or no eligible placed corpse -> no owned new fire | Existing fire unchanged, normal refusal and contained/off-map outcomes; native FireUtility/death branches | Respect ordinary fire placement and death outcomes / specified |
| KBC-03 | Extinguish/move/resurrect/save -> ordinary fire lifecycle | Static cell flame, no permanent/trailing/restarted fire; native Fire persistence and rules | Continue through ordinary fire lifecycle / specified |
| KBC-04 | Nested/skipped/changed death paths -> independent or inactive handoff | Scoped scalar state, one-use result, bounded admission diagnostics; source static-state risks corrected | Keep death handoffs independent and admitted / specified |

## Interactions and consumed contracts

S079 fire response/ignition changes only the fire that actually exists before death. S091 never
recreates a successfully extinguished fire or repeats apparel ignition resistance for a corpse.
Compose owned rules directly. Anomaly adds optional corpse/resurrection cases, not a special
anti-resurrection rule. Other fire/death changes need minimal effective semantic admission.

Native death/corpse placement, holder auto-extinguishing and Fire size/instigator/lifecycle are the
consumed contracts. No executable source-API consumer found in the bounded installed text search;
do not manufacture a facade for public fire_tmp/MyGetAttachment methods. Source co-installation
suppresses owned overlap, as does an already-correct native/provider handoff.

## UI and settings audit

Source has no UI, settings, bottom tab, operational toggle or save schema. Add none for this
continuity correction; no new number needs global/save inheritance. Native fire effects and ordinary
firefighting remain the player surface. Any common support/status presentation uses seven languages.

## Implementation, deviations and omissions

Nothing implemented. Preserve intended native handoff but replace source global Fire storage,
entry-time capture, broad attachment-call rewrite and warn-after-partial-transform behavior with
invocation-local current data, minimal admission and atomic failure handling. Keep existing fire
and actual environmental/placement limits. Non-finite/non-positive size is rejected with diagnostics.

No corpse-attached/permanent fire, guaranteed burning-to-destruction, duplicate intensity, resurrection
override, new corpse/stat/terrain definitions, map scanner or persistent transfer record. Native
timers/save ownership remain; no original save/settings migration. Core/Harmony required on use;
DLC/provider mods optional; XML Extensions required if later used. No foreign/Gateway assembly or
repository-mod runtime dependency.

## Verification and acceptance

[Ledger](../refinement/specification-progress.md) records document review/validation. KBC-Q1–3 keep
effective patch/native-fix/provider proof, focused TDD and final native workflows open. Personally
observe actual player fire/combat -> burning death -> corpse/cell fire and subsequent extinguishing,
including non-burning controls, relocated/no/held corpse, distinct victims/origins, save/load and
exact optional providers on the reviewed build. Retain causal screenshots, build/mod identities,
config hashes and cleanup. Static explanation or a pre-spawned burning corpse is not acceptance.
