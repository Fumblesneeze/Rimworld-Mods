# Enemies Use Apparel Too - EUAT

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Replacement **S083/P2-12**, `Argotis.EnemiesUseApparelToo`, author Argotis,
[Workshop3746069759](https://steamcommunity.com/sharedfiles/filedetails/?id=3746069759).
Installed support1.6; assembly0.1.0.0; DLL SHA256
`B417D07FC9269023684F925EFAD844DC6735F3B84F7330A03946155DD6B2816B`, matching the committed DLL in
[source19c1401](https://github.com/Argotis-dev/Enemies-Use-Apparel-Too-EUAT/tree/19c1401d7533307639388100040a230340954045).
No license found in the bounded scan; no source binary/code/art copied into product. Identity does
not prove beta compatibility. [Research](../refinement/2026-11-04-enemy-apparel-research.md) distinguishes
public claims, active source behavior, unwired prototypes and current-native gaps.

[Proposal](../../../../openspec/changes/specify-megamod-enemy-apparel/proposal.md),
[design](../../../../openspec/changes/specify-megamod-enemy-apparel/design.md),
[use spec](../../../../openspec/changes/specify-megamod-enemy-apparel/specs/megamod-enemy-apparel-use/spec.md),
[loadout spec](../../../../openspec/changes/specify-megamod-enemy-apparel/specs/megamod-enemy-loadout-variation/spec.md),
[talent spec](../../../../openspec/changes/specify-megamod-enemy-apparel/specs/megamod-enemy-combat-talents/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-enemy-apparel/tasks.md).

## Observable coverage

All rows **specified only; nothing implemented or accepted**.

| Behavior | Player action / situation → proposed result | Boundary |
| --- | --- | --- |
| Equipped combat | Encounter equipped non-player fighters → actual offensive item use | Real range/target restrictions, warmup/scatter/effects, stable valid candidate and native fallback |
| Charges/loot | Observe cast/loot/reload → exact physical item balance | One use/charge owner, ordinary failure costs, no duplicate command or free reset |
| Tactical jumps | Engage equipped melee/ranged/injured enemies → legal approach/reposition/retreat | Current item range, source-derived thresholds, useful landing before movement replacement |
| Deployment | Fight hunter/turret/deadlife users → actual supported projectile effects | Deadlife marking follows real release; Anomaly content verification pending |
| Loadouts | Trigger ordinary eligible generation → additional supported choices | Additive pools, existing restrictions/chances/required apparel, no rerolls or retroactive equipment |
| Optional talents | Enable policy before generation/combat → real possessed commands and tribal talent chance | Off by default; direct35% tribal grant; defeat removes owned cast but retains recruitment exclusion |
| Integration | Changed/source provider → one owner or local fallback | Semantic guards, bounded warnings, safe resolution of existing owned state |

## Deviations and direct composition

Use actual item targeting/cost/effect semantics instead of copied ability wrappers that omit frag
scatter or impose stale lance values. Explicitly exclude all player-controlled pawns, validate casting
positions and current jump range, bind costs to one worn item and avoid null/invalid-jump loops.
Fallback retreat explicitly requires a nearby threat; the source's zero-count check does not.
Deadlife's source premarking at intended destination becomes actual-release ownership. No separately
added emergency explosion jump or turret-position prototype: neither is actively wired in the source.

Generation preserves provider lists/locks and unrelated required apparel, with explicit native torso
substitutions for cataphract/champion/guild elite armor and ordinary fallback when not selected.
Odyssey salvager elite/scrapper inherit the source-targeted guild families and are included.
Per-candidate50%/10% and missing-only15%
are not promises about whole-raid equipment shares. Source tribal implant-pool behavior becomes an
explicit independent35% talent chance, preserving unrelated implants/money and adding no physical
implant or surgery content. Commander/marksman new grants remain unresolved rather than invented.
Persistent unrecruitability follows the source's actual grant/removal behavior and remains explicit.

S004 final hazard protection, S051 player specialist-work autocast, S078 prepared-group generation,
S081 burst handling and S082 current quality/range compose through owned calls. No internal Harmony
stack or private Thing cache. Native listers/current action eligibility supply the required facts.

## UI and settings audit

No source bottom tab. Source global/restart implementation switches become four common behavior rows
under Combat / Non-player equipment: offensive use, jumps, expanded loadouts On; optional Ideology
talents Off. Each uses Global default, Current save and Use global, dynamic inheritance/dormant
overrides, protected main-menu callbacks and explicit next-decision/new-generation timing. No new
right-side toggle. Equipment/charge/grant state stays contextual pawn/item state. Seven-language text
remains due; hardcoded source English and empty LanguageData are not coverage.

## Contracts, omissions and implementation state

No actual incoming consumer was found in bounded installed text/binary searches. Public helpers,
AbilityDefs, ThinkTrees and settings/save names are inventoried, not automatically reproduced as
facades. No source package spoofing, jobs/abilities/settings migration or safe mid-save assembly
removal is promised. Original EUAT owns new overlapping use/generation/talents when active; existing
owned casts/state retain safe completion or recovery.

**Implemented: nothing.** Core/Harmony and XML Extensions with first actual guarded XML use must be
declared truthfully. Royalty/Odyssey/Anomaly/Ideology remain optional and content-gated. Every actual
patch still needs executable minimal-semantic admission, owned-only rollback and later revalidation.
EUAT-Q1–6 retain tactical/balance, exact lifecycle, missing Anomaly/provider data, optional talents,
consumers/migration and reviewed native acceptance. All runtime tasks remain unchecked.
