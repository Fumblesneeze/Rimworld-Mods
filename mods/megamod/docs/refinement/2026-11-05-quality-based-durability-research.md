# S106 — Quality Based Durability research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Researched2026-11-05; origin replacement **S106/P2-36**. Documentation, all bundled C# and selected
binary, native callers and bounded consumers inspected read-only. This is evidence/refinement,
not a runtime compatibility claim. [Proposal](../../../../openspec/changes/specify-megamod-quality-apparel-durability/proposal.md),
[design](../../../../openspec/changes/specify-megamod-quality-apparel-durability/design.md),
[observable spec](../../../../openspec/changes/specify-megamod-quality-apparel-durability/specs/megamod-quality-apparel-durability/spec.md),
[tasks](../../../../openspec/changes/specify-megamod-quality-apparel-durability/tasks.md),
[coverage](../replacements/quality-based-durability.md).

## Documentation first and exact identity

- [Steam Workshop3798015624](https://steamcommunity.com/sharedfiles/filedetails/?id=3798015624),
  title Quality Based Durability; displayed creator gabriel123gonzalez4. Initial web open failed;
  indexed Steam result and a successful direct English-page fetch confirmed the description.
  The page describes quality-based clothing/armor durability loss, emphasizes combat and says
  values are configurable. Its embedded example image markup is malformed; no values are inferred
  from that image. It links Ko-fi, not a public source repository. Settings/source supply the values.
- Installed root `F:/Steam/steamapps/workshop/content/294100/3798015624`; About author
  **gabrieel1482**, canonical package **gabrieel1482.QualityBasedDurability**; PublishedFileId matches.
  About supports **1.6 only**, requires/loads after `brrainz.harmony`; dependency URL points to the
  legacy1279018671 item. Future owning metadata uses the repository's current Harmony identity.
- No LoadFolders.xml,1.7 or Common directory. Native1.7 `ModContentPack.InitLoadFolders` selects
  nearest version1.6 and root. `Source/obj/Debug` contains build copies, not an additional active
  load folder. No source assembly was loaded/executed or copied into a package.
- Selected `1.6/Assemblies/QualityBasedDurability.dll`: Assembly/File **1.0.0.0**, informational
  **1.0.0**, Debug; MVID **5ccac812-5f82-49b6-a67d-fc02d7fda2f8**; SHA256
  **8EC59AB66171A0D7E1E8BD6F2135B09A01F9BCE02C90C6F9A6C9CF6AFD0E34D4**.
- Bundled Source has six C# files and a net48 project. All six were compared with the selected
  decompilation; behavior/defaults match. A decompiled `listing.Gap()` resolves the source's
  default12 spacing; no behavior discrepancy was found. Language files are English, Spanish,
  SpanishLatin. There are no Defs/Patches or source assets required to implement this behavior.
- No README/license or public source URL was found in the inspected package/page. Source availability
  is not a reuse license. Independently reimplement behavior, credit the author, copy no upstream
  code/binaries/text/art. Any later reusable-contract/license discovery is QBD-Q2.

Ignored local evidence: `artifacts/Refinement/megamod-spec-20261105/quality-based-durability/` contains
the public HTML, complete read-only selected decompilation, native extracts, **13-input source** and
**5-input native** SHA256 manifests. Native game **1.7.4998 rev1299**, Assembly-CSharp SHA256
**FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B**. Hashes identify inspection
inputs, never runtime admission predicates. No live source/provider compatibility was tested.

## Source behavior and public surface

| File / member | Inspected behavior |
| --- | --- |
| `ModInit` static constructor | `StaticConstructorOnStartup`; `Harmony("gabrieel1482.QualityBasedDurability").PatchAll()`; unconditional Spanish success log; no target/admission validation |
| `Patch_Thing_TakeDamage.Prefix` | Non-null, positive amount; requires `Apparel` and `TryGetQuality`; multiplies DamageInfo amount if multiplier differs from1; no damage-type/wearer/faction/useHitPoints check |
| `QualityDurabilityUtility.GetDurabilityLossMultiplier(QualityCategory)` | Reads static settings; null settings or unknown enum returns1; quality switch returns configured float; no validation/clamp |
| `Patch_Thing_SpecialDisplayStats.Postfix` | Yields every existing entry if enumeration nonnull, then adds quality-bearing Apparel entry in Apparel category, priority5000; shows percent and keyed explanation; no HP eligibility check |
| `QualityBasedDurabilitySettings` | Seven public floats; global Scribe keys `awfulMult`, `poorMult`, `normalMult`, `goodMult`, `excellentMult`, `masterworkMult`, `legendaryMult`; reset restores defaults |
| `QualityBasedDurabilityMod` | Public static settings field initialized with GetSettings; mod-settings page with seven sliders,0–3inclusive, percent plus two-decimal multiplier, reset button; no per-save data |

| Quality | Default loss | For20 incoming points before other handling |
| --- | --- | --- |
| Awful | 150% | 30 |
| Poor | 125% | 25 |
| Normal | 100% | 20 |
| Good | 85% | 17 |
| Excellent | 70% | 14 |
| Masterwork | 50% | 10 |
| Legendary | 25% | 5 |

These are loss multipliers, not health multipliers or protection bonuses.0% reaches native zero
damage,100% contributes no change,300% triples incoming damage before remaining rules. Independent
settings can invert the quality order. Source loaded values are not sanitized before damage use.

Source stat row label means durability loss rate; explanation covers damage. Public combat wording
understates the actual unconditional damage-type scope. No maximumHP change, quality upgrade,
repair, inventory transfer or custom-save lifecycle exists. No source bottom tab, gameplay gizmo,
bottom-right button or debug UI was found. Current source settings are global mod configuration.

## Native1.7 evidence and consequences

Eight successful type extracts: `Thing`, `Apparel`, `ArmorUtility`, `DamageWorker`,
`Pawn_ApparelTracker`, `SteadyEnvironmentEffects`, `CompQuality`, `ModContentPack`.
An initial incorrect RimWorld.ArmorUtility type lookup failed; the successful type is Verse.ArmorUtility.

1. **Thing.TakeDamage:** destroyed and zero fast exits, per-Def damage multipliers with integer
   rounding, `PreApplyDamage` absorption, `DamageDef.Worker.Apply`, ordinary signals/records/fire/
   postdamage notifications. Source prefix changes the amount consumed by all these later steps.
2. **ArmorUtility.ApplyArmor:** sends apparel a rounded quarter of incoming attack damage before
   its independent protection roll. Modifying that apparel DamageInfo does not directly modify
   the separate attack amount used to protect/injure the pawn. Longer-lived armor can of course
   influence subsequent survival. No claim of seed-identical RNG outcomes across configurations.
3. **DamageWorker.Apply:** only harms health for hit-point items and harmsHealth damage; applies
   remaining rules, randomly rounds fractional damage, clamps to currentHP, subtracts and kills at
   zero. A1-point event at25% can cause0 or1 loss; it is not rounded up or banked for a fourth hit.
4. **Daily wear:** Pawn_ApparelTracker's day check is60000ticks; world pawns are excluded by that
   caller. `TakeWearoutDamageForDay` randomly rounds `wearPerDay`, sends positive Deterioration
   damage, then handles ordinary worn-away notification. Source multiplier applies to those events.
5. **Outdoor and corpse deterioration:** `DoDeteriorationDamage` sends1 Deterioration damage;
   the ordinary opportunity/protection logic remains independent. Corpse iteration also tests worn
   apparel. Source changes amount, not chance, terrain protection or native DeteriorationRate stat.
6. **Violent wearer death:** `Notify_PawnKilled` sends15%–40% of each worn hit-point garment's currentHP
   as the death damage type before ordinary apparel notifications. Source scales that chosen amount.
7. **Current quality:** `CompQuality.SetQuality` stores current quality and notifies owned comps;
   quality is saved in native `quality`. Damage/inspection queries it afresh. MaxHP is separately
   supplied by native stat lookup; source neither changes it nor rescales existingHP.
8. **Transfers/load/removal:** source creates no item-specific fields. NativeHP and quality persist;
   no future source hook means no future scaling. Historical damage cannot be reconstructed safely.

These are static findings, not observed gameplay. Exact effective hook shapes, custom workers,
info-card refresh and later-patch behavior are QBD-Q1 before implementation. Direct HP setters or
custom workers bypassing the admitted semantics cannot be advertised as covered by a broad prefix.

## Consumer and composition audit

Bounded case-insensitive `rg -a -l` scan of installed Workshop and local game Mods, limited to XML,
C# and DLL, searched package ID, `QualityDurabilityUtility`, `QualityBasedDurabilitySettings` and
`QualityBasedDurabilityMod`. Matches occurred only inside this source package (including its
inactive obj build copy); no external exact-name consumer found. This does not prove absence of
UTF16 metadata, constructed reflection strings, remote consumers or unsearched archives. No
upstream-name assembly/type facade or package spoof is justified. Preserve nativeHP/quality and
behavioral consumers; investigate any concrete future binding separately.

- Original active package owns overlap; skip owned damage and information together, with bounded
  warnings/status. Do not uninstall or unpatch it; its own1.7 runtime status is not established.
- Modded quality-bearing ordinary apparel works through admitted native shape, not an item whitelist.
  No specific combat provider has been independently admitted in this slice.
- S075 UnLimitedArmor changes rating limits; this feature changes apparel loss only.
- S082 Easy Upgrades quality work owns its actual change/repair outcome; subsequent damage/inspection
  reads the resulting quality without extra repair. S054 repair rules remain independent.
- S023 custom on-site melee destruction receives one ordinary multiplier per hit; S096 Micro
  Designations owns recipe processing, whose smelting/recycling/direct consumption is unaffected.
  High quality can take more attacks to destroy. At0%, retain S023's ten completed ineffective
  cycles then persistent suspension; unchanged scans/restarts/load do not erase the allowance.
  Explicit Retry or genuine attack/susceptibility changes can permit another attempt. No direct
  deletion, forced damage or duplicate no-progress mechanism is added here.
- S104 No Burn Metal controls flammability contributions; this source scales actual fire damage
  reaching eligible apparel, not ignition probability. No override forces a fire event.
- S009 comparison and queued inventory/quality display keep their own contracts; no abstract
  lifetime or new armor-stat behavior is inferred. All owned features compose by direct calls.

## Selected specification and deliberate changes

Keep the seven defaults and0–300% range, broad damage scope, native fractional handling and one
current-item rate. Use common global/save inheritance, per-entry invalid-value fallback, scoped
resets and future-event timing. Explain0% accurately; do not guarantee survival from consumption.
No source settings import or silent HP migration. Only actual durable items get a meaningful rate.
Reject invalid interactive edits and invalid arithmetic instead of feeding NaN/overflow into native
damage. Unknown quality stays neutral. Native quality and shared settings suffice; no Thing cache.

Replace unguarded PatchAll with semantic admission for effective amount use and actual item-info
enumeration. Damage and truthful active display form one failure group; owned-only rollback,
per-skipped-enabled-patch startup warning and supported later revalidation are required. Keep source
priority5000 an incidental UI reference, not a player behavior promise. Seven-language item/info and
settings UI are mandatory. No source raster or new art is needed for this feature.

## Static balance review, not acceptance

Inspected Core XML supplies these nearby comparators; finalized inherited/material/provider values
still require an exact game run. These are source base values, not claimed finalized game exports.

| Comparator | Source baseHP / acquisition | Consequence under the same incoming damage |
| --- | --- | --- |
| Basic shirt | 100HP,40Stuff,1600work; ApparelMakeableBase | 20points at Normal loses20; Legendary loses5, no more maxHP |
| Parka | 180HP,80Stuff,8000work; same makeable base | Same event20→20/5; warmth/material stats and costs unchanged |
| Marine armor | Apparel_PowerArmor inherits ApparelArmorPowerBase:340HP,60000work,100plasteel/20uranium/4advancedcomponents; PoweredArmor, Crafting7 | Same event20→20/5; native protection, cost/research and coverage remain separate |

No recipes, inputs, work, research, rarity, market value or stats are added. Low-tier1.5 and
high-tier0.25 change condition expenditure substantially and apply to all factions, daily wear and
death-damaged loot. Simple equal-exposure life scales roughly as reciprocal multiplier (Normal1,
Legendary4); this is not an exact survival or market-value forecast. Ordinary quality armor bonuses
already coexist in Core. Final review must include low/high quality, repair economics and no-loss
settings, modded cheap/common/expensive Stuff where eligible, actual use and finalized comparators.

## Open work

| ID | Remaining work |
| --- | --- |
| QBD-Q1 | Exact effective1.7 damage/worker/info refresh/repatch contracts and grouped safe fallback |
| QBD-Q2 | Any real API/reflection consumers, admitted custom providers and public source/license discovery |
| QBD-Q3 | Finalized durability/armor/material comparisons, extremes, actual cost and seven-language UI |
| QBD-Q4 | Focused nonzero TDD, reviewed implementation/package and causal native action-to-condition evidence |

No runtime code/tests/build/install/game launch, normal configuration/save write, Workshop mutation
or publication occurred. Review and strict documentation validation are recorded in the ledger once
completed; every implementation/acceptance task remains open.
