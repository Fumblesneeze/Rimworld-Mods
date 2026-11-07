## Why

The user wants competent slaughterers to recover more useful material, with Cooking contributing
to slaughtering effectiveness as well as speed. Make that an earned result of deliberate slaughter
while keeping the game's base yields and the normal later butchering process intact.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. Origin **original**, S118/P2-46/P2-47.

## What Changes

- Add a Cooking contribution to S117's existing slaughter-speed model, neutral at level10 by
  default. Capture skill/settings when handling starts; do not change an attempt halfway through.
- For successful deliberate slaughter, preserve the slaughterer's skill-derived recovery factor
  on that particular corpse. Apply it to its ordinary meat/leather output when actually butchered.
- Preserve current carcass condition, native difficulty and the butcher's own efficiency; do not
  globally raise animal yields, spawn extra products at death or multiply horns/organs/trophies.
- Specify bounded, editable draft balance defaults with the common global/save/Use-global UI;
  keep earned corpse results stable when settings or the slaughterer's skills later change.
- Explain current slaughter proficiency and recorded corpse recovery in existing contextual UI;
  preserve native bills, product accounting, stack/hauling rules, save/transfer and resurrection.
- Compose directly with S117 and the native production pipeline, with per-patch target checks and
  safe fallback. No patch stack against our own implementation and no separate corpse index.

**Specification only.** User-confirmed direction is distinguished from proposed formula/product/
scope defaults in [refinement](../../../mods/megamod/docs/refinement/2026-11-06-slaughter-yield-refinement.md).
The earlier doubled-base/skill16 suggestion is superseded. The user accepted approximately60% extra
slaughter recovery at mastered skills, retaining the selected160.5406%/100% endpoints at all20/all10.
No runtime behavior or playability is accepted.

## Capabilities

### New Capabilities

- `megamod-slaughter-yield`: Cooking contribution, deliberate-slaughter provenance, meat/leather
  recovery, shared tuning, contextual information, corpse lifecycle and guarded processing.

### Modified Capabilities

None. The existing S117 draft already reserves direct S118 Cooking/yield composition while keeping
its source-derived contribution and native timing safeguards. Its design/coverage links are updated
to this concrete contract. S010 recipe inspection still labels input-dependent results honestly.

## Impact

- Requires admitted **owned S117 capability in the same product**, not another runtime mod.
  Harmony and XML Extensions remain required with the planned S117 patch/guarded definition use;
  no source mod or DLC is required. Optional slaughter/butchery providers are absent-safe and guarded.
- Uses ordinary physical corpses, the butcher-creature bill path, common settings and native/shared
  production facts. Source-selected or unproved alternate processing does not receive a guessed bonus.
- No copied upstream code/art, new content family/tab, general pawn override, automatic legacy-corpse
  inference or save conversion, runtime dependency on another repository product or the Dev Gateway.
