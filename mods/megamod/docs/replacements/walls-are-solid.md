# Walls are solid

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
S072, origin **replacement**. Source **Victor.WallsAreSolid**, Workshop **2896548513**, Victor/wilki24.
Inspected **2026-11-04**, installed assembly/file1.0.0.0 and all eight selected1.6 XML files plus root
binary. Source advertises1.4–1.6; native version-directory selection is static evidence, not 1.7 support.
[Research](../refinement/2026-11-04-walls-are-solid-research.md) records primary documentation, hashes,
read-only paths, current native evidence, source discrepancies and WAS-Q1–5. No license was found in
the inspected tree/repository listing. Independently implement behavior; no source assembly/code copying.

Owning [change](../../../../openspec/changes/specify-megamod-structural-damage/), with
[observable spec](../../../../openspec/changes/specify-megamod-structural-damage/specs/megamod-structural-damage/spec.md),
[design](../../../../openspec/changes/specify-megamod-structural-damage/design.md) and
[tasks](../../../../openspec/changes/specify-megamod-structural-damage/tasks.md).

## Behavior inventory

All rows are **specified, unimplemented, unverified**. The linked spec's named requirements own the results.

| ID / requirement | Native action → intended result | Defaults and boundaries | Evidence |
| --- | --- | --- | --- |
| WAS-01 Ordinary attacks | Attack an eligible building → relative structural damage | Sharp0.4/0.1, blunt0.5/0.3, projectile0.2/0.2, explosion0.4/0.25 passable/impassable; range0..3; exempt other buildings by default | Source Damage/Buildings patches, current damage pipeline and explicit corrections |
| WAS-02 Crushing heads | Supported blunt head hits a building → enhanced crushing | Passable2/impassable1.5; no ordinary reduction; retain native Blunt identity/resistance, handles, pawn/item damage and breaching | Source content/weapon patch; native primary Tool delivery, mace, Demolish and Thump |
| WAS-03 Mining | Existing mining hit on constructed nonmineable wall → half damage | Fixed0.5; preserve Mineable final removal/yields and safe roof handling | Source mining patch and current JobDriver_Mine |
| WAS-04 Rock resistance | Enable option and shoot native bullet/arrow at rock family → no HP loss | Initially off; includes ore/collapsed/smoothed rock, not loose chunks or explosions | Source RockBase list, native ancestry and explicit coverage |
| WAS-05 Escape | Already dig-enabled exit finds blocker → native attack alternative | Retain ranged building-destroyer branch, contention/wait and unrelated mining/exits | Full source patch and native ExitMap/DigUtility |
| WAS-06 Settings | Edit common scope → subsequent damage uses effective value | Four paired rows, two booleans, Use global/dormant override; no restart or HP reset | User foundation requirements; deliberate source UI/storage replacement |
| WAS-07 Optional content | Supported special attack hits building → documented family | Eight source groups; exact mappings in research; guards before activation | Full selected optional patch; partial actual provider inspection |
| WAS-08 Admission | Start with changed target/original source → coherent fallback | Source sole authority for all overlap; bounded skipped-patch warnings; preserve usable native attacks | User patch/merge policy and explicit coexistence design |

## Interactions and composed result

S037 blunt-social-fights retains actual attack/armor-category selection, S023 destruction remains
item-only, and S061 safe removal retains roof/job eligibility. Direct owned policies replace any
internal patch stack. Exit attacks are combat, outside S061: even if Other workers would protect the
escaping pawn's mining, the substituted attack can destroy roof support without preparation and cause
native collapse. Covered mining/removal elsewhere remains protected. Mace equipment does not become
part of native social-fight choices. S097 mining priority is queued; no conflict is inferred from its name.
Native modifier order and foreign list entries remain relevant, rather than being overwritten by
reciprocal exceptions. Exact custom workers and Blunt/owned-damage identity consumers need inspection.

## UI audit and relocation

| Source entry point | Purpose | Owned destination / change |
| --- | --- | --- |
| XML Extensions settings menu, eight sliders and two toggles | Global startup configuration | Common Combat / Structural damage, four atomic passable/impassable pairs and two options; global/save controls with next-hit application |
| No source bottom tab, debug entry or operational button | None | No new tab or toggle |

Use global, Current save and Global default are distinct. Main-menu save controls and callbacks are
inactive. Dormant overrides survive inheritance; reload/save switching resolves current values. All
seven baseline languages are required. Native attack/inspection menus remain the gameplay surfaces;
there is no per-building setting disguised as a global option.

## Contracts consumed by other mods or saves

| Consumer / evidence | Binding | Decision / remaining verification |
| --- | --- | --- |
| Installed CAI5000 continued About, `Krkr.rule56` | IncompatibleMods references source package | Do not spoof source ID or copy the blacklist to the whole megamod; inspect actual AI/damage overlap before support |
| Source external integrations | Eight display-name gates plus exact Def/worker expectations | Replace with verified canonical package/content/shape admission; both Oddities identities and selected provider worker chains remain open |
| Bounded installed XML/C#/JSON search | No additional callable or BluntCrushing/SmashCrushing consumer found | No facade justified yet; broader binary/reflection consumers remain part of WAS-Q3 |
| Installed FTC_BasePermanentStructure, Workshop2887532326 | Native damageMultipliers keyed to exact Blunt, value0.25 | Keep native Blunt identity and recognize the selected head through primary Tool context; no new DamageDef bypass |
| Source XML Extensions settings | Global load-time source keys | No automatic import or legacy identity alias; common scoped settings have independent owned keys |

No migration is selected. Future automatic migration requires backup, atomicity, failure restoration
and verified postconditions. Removing the feature on a fresh startup does not heal previously lost HP.

## Implementation and coverage

**Nothing implemented.** Every behavior awaits real target documents, executable admission, focused
RED/GREEN and reviewed-build native evidence. Major proposed deviations are explicit:

- Choose installed explosion defaults over conflicting description, normalize as additional factors,
  and preserve independent modifiers; arbitrary-provider numerical parity is not promised.
- Use semantic wall/door/mineable scope and skip exemptions, avoiding reciprocal zero division and
  raw-XML claimability/category mistakes. Keep other buildings unchanged by default.
- Prevent BluntBase inheritance from weakening dedicated Demolish; preserve native Blunt identity and
  exact resistance. Recognize the actual head strike without new damage/capacity/maneuver Defs or weapon
  conversion; the source's cosmetic smash(crushing) label is not retained. Preserve other tools/effects.
- Apply fixed half mining damage only to constructed nonmineable walls, not every explicit impassable
  Building node. Preserve ordinary rock/ore/smoothed mining and the native last-hit path.
- State rock immunity's actual broad ancestry and narrow native projectile coverage.
- Replace restart/global-only settings with common scoped live values. Do not mutate global Defs per save.

Harmony and XML Extensions become required with implementation; source, DLC and content providers are
optional. Original source suppresses all owned S072 contributions before XML/C# changes, retaining
preferences. Each unsupported contribution falls back independently where safe; there is no foreign
unpatching. XML keeps inherited/unrelated entries and requires atomic/idempotent guards. No private
Thing cache, new equipment recipe, runtime repository-mod dependency or Gateway dependency is added.

## Verification and acceptance

Independent document review, schema/link checks and semantic review are recorded in the sequential
ledger. Static comparative arithmetic is not finalized balance evidence. Native acceptance must include
actual attacks/HP changes, head versus handle and dedicated breaching, pawn/item behavior, real mining,
eligible exit jobs and settings save/menu/provider transitions. Inspect exact causal before/action/after
views on the reviewed build. Source execution, game launch, implementation tests/build/install and
Workshop/save/config writes have not occurred in this documentation slice.
