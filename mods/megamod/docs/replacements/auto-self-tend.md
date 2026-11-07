# Auto Self-Tend

## Identity and provenance

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, **S042 / P1-33**. Source **Auto Self-Tend**, package `funstab.AutoSelfTend`,
Workshop **3300580124**, About author Funstab / Workshop Mr. Recoup. Installed About supports
1.5/1.6; assembly 1.0.0.0, SHA-256
`999B26031A547C64DC1ABD93B4D1DF39300F1AC0CCCF71CD92800D39A5D60D03`.
No source license established; no code, assemblies or assets copied into the product.

[Research](../refinement/2026-11-03-auto-self-tend-research.md) retains docs-first attempts, complete
three-type DLL inspection, native 1.7 contracts, exact identities and bounded consumer scans.
Owning [proposal](../../../../openspec/changes/specify-megamod-auto-self-tend/proposal.md),
[design](../../../../openspec/changes/specify-megamod-auto-self-tend/design.md),
[observable specification](../../../../openspec/changes/specify-megamod-auto-self-tend/specs/megamod-auto-self-tend/spec.md)
and [tasks](../../../../openspec/changes/specify-megamod-auto-self-tend/tasks.md).
**Draft only; nothing implemented or accepted.**

## Behavior inventory

Each row maps to the named requirement in the observable specification above; research records
source/native evidence and distinguishes proposed corrections.

| ID / owning requirement | Action → observable result | Retained / corrected / added | Status |
| --- | --- | --- | --- |
| ST-01 Automatic permission | First assessment or policy event → native checkbox follows effective Medical threshold | Source default 6, integer 0–20, disable-below true; enable-only alternative | Draft |
| ST-02 Manual choices | Change pawn checkbox → stable polling leaves it alone | Retain source crossing/policy exceptions and explain them accurately | Draft |
| ST-03 Eligible colonists | Maps/holders/caravans → consistent eligible population | Correct inaccessible baby/incapable and map/caravan slave discrepancy; preserve ordinary ability | Draft |
| ST-04 Timing | Join/cross threshold → update within 600 game ticks; commit setting → immediate active-game result | Retain bounded source cadence; explicit committed-value behavior | Draft |
| ST-05 Transfers/absence | Move or return → retain history, apply policy changes missed while away | Correct source global-last-policy gap | Draft |
| ST-06 Native medicine | Permission enabled → normal jobs can use it | No instant heal, priority/cost/quality/care bypass | Draft |
| ST-07 Common settings | Edit global/save policy → documented effective value and inactive inherited editor | Replace source global-only standalone settings path | Draft |
| ST-08 Contextual UI | Open Health → native pawn permission available | No new bottom tab/toggle; seven-language policy explanation | Draft |
| ST-09 Save lifecycle | Reload valid state → manual exceptions preserved | Versioned owned history; first use explicit, invalid history suspended, no source migration | Draft |
| ST-10 Compatibility | Source active or native contract unsupported → owned updates suppressed | One owner, bounded warning/status, native manual control remains | Draft |

## Interactions and composed result

S007 compact Health preserves the native permission control; S031 Toddlers retains developmental
restrictions. Future S124 Drug Response and S134 Priority Treatment govern their own medical choices;
research them in queue and compose directly with native permission/eligibility. No demonstrated
conflict or precedence is inferred from their names. No patches to our own feature implementations.

No new Thing index/cache: existing map/caravan pawn collections supply candidates; per-pawn assessment
history is gameplay lifecycle state rather than a duplicate search/hauling dataset. Ordinary costs,
skill/aptitude, medical quality and job availability remain native. No DLC required for baseline play.

## UI audit and relocation

| Source surface | Purpose | Owned destination / change |
| --- | --- | --- |
| Mod settings slider/checkbox | Configure policy | Health → Automatic self-tending, common global/save rows |
| Native Health self-tend checkbox | Pawn-specific gameplay permission | Retain native contextual control; not a mod setting |
| No source bottom tab, gizmo or debug action | None | None introduced |

Both real settings inherit global defaults unless a save overrides them. Use global retains dormant
overrides; per-save controls are inactive when inheriting and all save mutation is disabled at the
main menu. Effective committed edits reapply policy even while paused; equivalent values do not.
Settings explain manual-choice limits and first-assessment resets. No scope exception is necessary.

## Contracts consumed by other mods or saves

| Contract | Evidence / result | Owned handling |
| --- | --- | --- |
| Native `selfTend` boolean/save key | Health UI and caravan medicine consume native permission | Preserve authoritative native state and its save contract |
| Native Medical level | SkillRecord includes aptitude and disabled-skill semantics | Use supported effective skill; don't replace with raw level/tend-quality |
| Source public Settings/ApplyAllNow/component identities | No external binding found in bounded installed text/DLL scan | No facade or package/assembly spoof; unknown dynamic/uninstalled consumers not guaranteed |
| Source global settings and component keys | `MinimumSkillLevel`, `DisableBelowThreshold`, `appliedSelfTend`, `lastThreshold`, `lastDisableBelowThreshold` | No automatic import; first owned assessment may change native choices |
| Original package active | Both implementations could write the same native checkbox | Suppress owned automation before any write, explain in settings/startup; leave source untouched |

Existing native permission is retained when automation is suspended. Valid owned history survives
temporary suppression; reactivation compares current policy/classification. Missing history is
first assessment, whereas corrupt/unrecognized history suspends affected automation. This is not a
safe-removal or original-save migration claim. Any future migration needs separate backup, atomicity,
restoration and verified postconditions before implementation.

## Implementation and coverage

| Scope | Implemented | Deviations / potential conflict | Missing |
| --- | --- | --- | --- |
| ST-01–10 | Nothing | Native eligibility corrections, per-pawn policy history, shared configuration | All implementation, focused tests and live evidence |
| Native integration | Nothing | Proposed patch-free component; no blanket foreign-owner rejection | Exact current-build admission and actual medical workflow proof |
| Providers | Nothing | Source-provider suppression, no package facade | Exact absent/present and supported medical-feature composition runs |
| UI/persistence | Nothing | Clear first-use/policy-event limits, native pawn controls | Seven-language UI and save lifecycle evidence |

Core required. No source/DLC/Harmony/XML Extensions runtime dependency for this patch-free proposal.
Any later actual Harmony or XML use must add minimal target specs, admission and dependency metadata;
XML must use guarded merge-preserving content operations. Dev Gateway never ships or becomes a product
dependency. No balance changes beyond skill-based permission defaults; threshold and latency are explicit.

## Verification and acceptance

ST-Q1–4 retain exact eligibility/consumer, persistence, provider and native evidence work. Future
host tests cover assessment transitions, not pretend loaded-mod databases. Native acceptance must
use actual settings, Health, recruitment/travel/save and medical job paths on the reviewed build,
personally inspected with causal before/action/after evidence and exact optional-mod runs. Strict
document validation and static source analysis cannot close any implementation task.
