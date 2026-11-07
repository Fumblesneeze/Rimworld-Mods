# RimHUD Passion Icons

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement, **S128**. Source **RimHUD Passion Icons**, DanZinagri,
`DanZinagri.RimHUDPassionIcons`, Workshop[3829500081](https://steamcommunity.com/sharedfiles/filedetails/?id=3829500081).
Selected1.7 core SHA256 `de7a34b42d3731319f859a8d7424c8cfe1fa444fa785e62a013f54106e94b6fb`;
optional VSE SHA256 `5dd93b7a7173a08c75eea4763439c3f50d3940d88b0387439838fb2051aed118`.
Public source commit `3d32cc578f956b2ea5a1affcbaf54f900fa25df7`; no license file found in its tree.
No source/DLL/asset redistribution. [Research](../refinement/2026-11-06-rimhud-passion-icons-research.md)
records exact provider inputs and distinguishes author's compatibility claims from inspected facts.

## Behavior inventory

| ID | Input → observable result | Scope / evidence | Status |
| --- | --- | --- | --- |
| RPI-01 | Inspect native passions → minor/major game icons, no None icon or duplicate marker | Core mapping; [contract](../../../../openspec/changes/specify-megamod-passion-display/specs/megamod-passion-display/spec.md) | Specified only |
| RPI-02 | Inspect admitted extended passion → its actual icon/identity | VSE resolver and current registry; Alpha content not yet verified | Specified only |
| RPI-03 | Constrain row or hover/click → readable name/icon/value, full details and existing action | Source drawing correction; direct S127 composition | Specified only |
| RPI-04 | Missing art or unknown identity → truthful text fallback, unaffected HUD | Intentional robustness improvement over source star fallback | Specified only |
| RPI-05 | Switch pawn/save or update passion → current subject on next HUD refresh | S127 lifecycle, no duplicate saved passion/cache | Specified only |
| RPI-06 | Use existing extended-passion filters → unchanged choices, matching and randomization | Current VSE/PM code already handles modern filters | Preservation specified; historical repair omitted |
| RPI-07 | Incompatible integration or original HUD → bounded warning and supported display fallback | Foundation target admission and S127 co-installation policy | Specified only |

## Interactions and composed result

This source modifies RimHUD; its selected behavior becomes a direct part of our owned S127 skill
row. No internal Harmony stack or reflection against owned fields. Native/provider skill data stay
authoritative. No new shared map index is needed. Existing layout/theme/refresh settings continue
through S127's common load/store/render path; no independent settings implementation or new scope
exception. No learning, forgetting, generation, assignment or medical behavior changes.

The source's older VSE/Prepare Moderately repair is **deliberately omitted for the inspected current
versions**, which already use the modern filter methods. Replaying it can duplicate equivalent
patches or foreign startup. Older broken combinations remain unsupported by this feature unless
separately demonstrated and specified. Current compatibility still requires exact live proof.

## UI audit and relocation

| Source surface | Purpose | Owned destination / contract |
| --- | --- | --- |
| Icon inside skill label | Read passion identity | S127 pawn inspection row, preserve tooltip/action and reserve space |
| No source settings/tab/toggle | No independent configuration | Reuse S127 presentation controls; add no bottom tab or separate settings |
| Source debug-only patch failures | Diagnose compatibility | Bounded startup/runtime diagnostics with useful fallback, not repetitive messages |

No new saved settings, entity data or migration. Pawn passions remain existing pawn state. S127
global/save settings and main-menu restrictions apply unchanged; owned text supports seven languages.

## Contracts consumed by other mods or saves

| Evidence / binding | Disposition | Remaining proof |
| --- | --- | --- |
| Own VSE assembly assigns public PassionIconPatches.Resolver delegate | Direct owned provider adapter; no extra runtime mod dependency | Registered identity, absent/invalid data and lifecycle |
| Native SkillUI passion textures / VSE PassionDef.Icon | Use active supplied texture, preserve aspect and identity | Native and exact provider/retexture visuals |
| Source hard RimHUD dependency/private fields and widgets | Not a binary facade; original required package not impersonated | Any future concrete external consumer needs its own binding proof |
| Current VSE/Prepare Moderately filters | Leave provider authoritative; omit redundant historical repair | Exact native selectors/randomization and absence of display side effects |
| Author-advertised Alpha Skills / Oracle retextures / Stranger Danger repair | Not accepted from description | Resolve actual identities/fixtures before compatibility claim |

Original RimHUD remains the sole renderer when co-installed: withhold the owned HUD/icons with a
startup warning, never unpatch the original or change its configuration. Provider integrations are
optional and package/shape admitted; original icons mod is not required. No new XML edits selected.

## Implementation, verification and coverage

[Proposal/design/tasks](../../../../openspec/changes/specify-megamod-passion-display/) contain the
future work. **Nothing implemented or accepted.** All seven groups await focused TDD, executable
target checks where hooks are needed, localization, independent runtime review, reviewed package,
and fresh minimized native before/action/after workflows. Visual asset rules apply to future art
selection/change; no image or source claim substitutes for in-game visual proof. Final playability
review must confirm no skill/progression changes and document exact optional-provider limits.
