# Prosthetic No Missing Body Parts (Continued)

## Identity and provenance

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, **S025 / P1-19**. Source `Mlie.ProstheticNoMissingBodyParts`, Workshop **2739055353**,
original iamrespawn/RimworldFixes, continued by Mlie. Inspected **2026-11-02**.

[Steam documentation](https://steamcommunity.com/sharedfiles/filedetails/?id=2739055353) was read before
[public source](https://github.com/emipa606/ProstheticNoMissingBodyParts) and installed decompilation.
[Research](../refinement/2026-11-02-prosthetic-apparel-research.md) pins commit
`417551f191fa4f2d0576921437ba1c5f8a22b4ae`, installed 1.7.0 assembly SHA-256
`ED964FE29D382645BBE6BA32ADBA979721D2C5FB5D146BA1FA57944CC6A6EEFE`, matching committed DLL,
line-ending-only About/LICENSE differences, native beta inputs and actual consumers. A matching
binary does not prove a reproducible source build. MIT copyright 2020 RimworldFixes; retain notices
for any future substantial licensed reuse. No source code, assembly or artwork is copied here.

## Behavior inventory

All rows are **analyzed; specified with retained gaps; not implemented**. The
[observable capability](../../../../openspec/changes/specify-megamod-prosthetic-apparel/specs/megamod-prosthetic-apparel/spec.md)
owns these behaviors; research distinguishes source facts from proposed corrections.

| ID | Player input → result | Defaults, boundaries and scenarios |
| --- | --- | --- |
| PA-B1 | Wear ordinary gloves/boots/jewelry over a suitable installed replacement → corresponding body-availability exception | Source arm/leg choices; actual replacement coverage; no loose prosthetic or unrelated implant substitute; successful surgery retention |
| PA-B2 | Wear a multi-group or side-specific item → native any-covered-member rule with the correct actual branch | All groups considered, recursive descendants, no custom-label or group-order dependency, no invented pair requirement |
| PA-B3 | Wear permitted item → ordinary physical apparel and health remain authoritative | Preserve layers, biocoding, race/provider rules, work and cost; no healing, changed body groups or extra armor effect |
| PA-B4 | Search/configure replacements → distinct arm/hand/leg/foot preferences | Source-derived arm/leg defaults, empty hands/feet, unknown candidates off; deduplicated rows and admitted conversion candidates |
| PA-B5 | Switch save, change defaults or temporarily remove content → correct retained choices | Common per-entry inheritance, dormant overrides and main-menu disabled save callbacks; no silent source-file import |
| PA-B6 | Change installed coverage or settings → current eligibility and safe invalid-wear cancellation | No UI initialization requirement or permanent cached permission; retain ground/source garments, no false forced entry, proactive strip/re-equip or duplicated job |
| PA-B7 | Convert an admitted Integrated Implants arm/leg → equivalent configurable clothing coverage | Four modular variants default on only for the inspected EBSG/no-MSE2 route; actual current attachment and user choices retained |
| PA-B8 | Inspect a pawn in admitted Choose Your Outfit → consistent clothing regions and eligibility | Refresh on pawn/health/effective preference changes; preview does not change anatomy or equip items |
| PA-B9 | Enable original/unsupported provider → one admitted owner and clear fallback | Preserve owned preferences, skip only affected overlap/integration, bounded startup warnings; no blanket source save/binary compatibility |

## Interactions and UI audit

S004 clothing selection consumes ordinary clothing eligibility. S024 explicit unload/keep choices
remain authoritative; this feature never re-equips something the player removed. Optional prosthetic
and apparel mods remain independent content providers. No internal self-patches or runtime dependency
on another repository mod is introduced.

| Source surface | Purpose | Megamod destination / difference |
| --- | --- | --- |
| Four scrollable replacement sections with search | Configure coverage types, not a particular pawn | Structured mod settings, shared Global default / Current save / Use global controls, readable source/description and stable identity search |
| Native wear and lost-body-part handling | Use actual garments | Native controls and jobs; only the admitted missing-coverage decision changes |
| Choose Your Outfit optional body preview | Inspect/control ordinary outfit policy | Narrow existing-window integration; no new bottom tab or fake health parts |

Source adds no bottom tab or gameplay gizmo. Its installed-version footer is source metadata, not a
required separate gameplay feature. Seven-language text, absence explanations and native UI testing
remain implementation tasks. No per-pawn choice is reclassified as a setting.

## Consumed contracts and support limits

| Consumer / binding | Preserve / adapt / unsupported |
| --- | --- |
| Integrated Implants `lts.I`: original package-gated loader, direct Mod type, private init plus dictionaries/lists | Binary binding not emulated. Owned registration preserves four modular candidates with EBSG active/MSE2 absent, independent of first settings display and without resetting choices |
| Choose Your Outfit `oels.chooseyouroutfit`: original package gate, reflected mod/settings, four List<string> fields and copied union | Source globals cannot represent our per-save/per-role policy honestly. Preserve current display/eligibility outcome through an admitted optional adapter; native wearing can work even if its separate preview adapter is unavailable |
| Vanilla Bionics Expansion: load-before source/legacy identity | Metadata hint only; use finalized content discovery rather than impersonating that identity |
| Public BodyPartUtils / mutable source body-name lists | No actual consumer found in the bounded scan; no speculative source-shaped facade |

Source saves four global name lists and no custom pawn health data. Owned choices use stable provider/
replacement/attachment identities and survive unavailable content; no settings migration or safe
removal claim is implied. A future importer needs backup, atomic application/rollback and verified
postconditions. Existing worn clothing can become unsupported when the exception is removed.

## Implementation, deviations and missing work

**Implemented: nothing.** [Proposal](../../../../openspec/changes/specify-megamod-prosthetic-apparel/proposal.md),
[design](../../../../openspec/changes/specify-megamod-prosthetic-apparel/design.md) and
[unchecked tasks](../../../../openspec/changes/specify-megamod-prosthetic-apparel/tasks.md) distinguish
intent, technical boundaries and future delivery.

Intentional corrections: inspect all garment groups, use the actual replacement branch rather than
any same-kind limb, remove the custom-label prerequisite, require real replacement coverage, use
empty foot defaults instead of leg IDs, deduplicate candidate rows and preserve missing choices.
The source's MuscleStimulatorLegs preference is recorded but cannot manufacture coverage for a plain
implant. Modular discovery does not depend on UI initialization. Preview integration uses the current
save instead of a flattened source-global union.

Required later dependency: Harmony. No XML changes currently selected; future XmlExtensions use
requires its dependency and guarded additive composition. Optional alternate anatomy, modular and
preview providers need exact semantic admission; source advertised CE/Android/general compatibility
is not accepted here. Original active withholds owned overlap and dependent preview extensions.
Derived definition/settings data is shared where concretely useful; no separate Thing index or
persistent pawn permission cache. PA-T1–5 are design candidates, not implemented target checks.

## Verification and acceptance

[The progress ledger](../refinement/specification-progress.md) records strict artifact validation
and independent specification review. PA-Q1–4 retain corrective choices, exact beta lifecycle,
provider support and acceptance work. No host gameplay test, build or live run has accepted this feature.

Future evidence must include real wearing, surgery/conversion/removal and retention, unchanged health,
ordinary layers/restrictions, save/global/UI transitions and each claimed optional provider with its
exact mod list. Retain reviewed build identity, actual action/result screenshots, config hashes and
cleanup. Balance review must consider access to real apparel and its native protection without
changing prosthetic research, surgery costs or body capabilities.
