# Quality Based Durability replacement coverage

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement **S106/P2-36**. Source `gabrieel1482.QualityBasedDurability`,
[Workshop3798015624](https://steamcommunity.com/sharedfiles/filedetails/?id=3798015624), author
gabrieel1482. Installed source/selected1.6 DLL1.0.0.0 inspected; no original1.7 support claim.
**Status: researched; specification draft independently reviewed/validated; nothing implemented or verified in game.**

[Research and exact identities](../refinement/2026-11-05-quality-based-durability-research.md),
[proposal](../../../../openspec/changes/specify-megamod-quality-apparel-durability/proposal.md),
[design](../../../../openspec/changes/specify-megamod-quality-apparel-durability/design.md),
[observable spec](../../../../openspec/changes/specify-megamod-quality-apparel-durability/specs/megamod-quality-apparel-durability/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-quality-apparel-durability/tasks.md).

## Behavior inventory

| ID | Input/action/result | Boundaries and selected result | Evidence/status |
| --- | --- | --- | --- |
| QBD01 | Quality-bearing apparel takes damage → scale incoming loss once | Defaults Awful150/Poor125/Normal100/Good85/Excellent70/Masterwork50/Legendary25%; native later handling | All six source files + selected DLL/native callers; specified |
| QBD02 | Daily wear, outdoor/corpse deterioration or violent wearer death → same multiplier | Actual source scope exceeds public combat wording; no new event opportunities | Native apparel tracker/environment/damage; specified |
| QBD03 | Inspect actual eligible item → current percentage and explanation | Existing info retained; lower-is-better, no maxHP/armor/lifetime claim; no guessed abstract/non-durable rate | Source iterator and selected correction; specified |
| QBD04 | Edit quality settings → subsequent events use current effective value | Seven common global/save rows,0–300%, scoped reset, validation and main-menu guards | Source globals/defaults plus owned settings rules; specified |
| QBD05 | Load/rework/transfer/remove → no retroactive condition rewrite | Current quality read on use; nativeHP/quality persist; no silent source settings import | Source has no item state; native lifecycle; specified |
| QBD06 | Original/unsupported integration → inactive owned feature with reason | Coherent damage/display group; ordinary fallback; no foreign unpatch or duplicated multiplier | Required target admission; not implemented |

## Interactions and composed result

S075 armor ratings/protection remain separate from item durability. S082 quality work owns its
quality/repair outcome; later damage and inspection use that quality without another heal. S054
repair authorization stays intact. S023 on-site melee destruction scales loss once per hit and
retains its persistent ten-ineffective-cycle suspension at0%; unchanged work scans cannot loop.
S096 Micro Designations recipe consumption does not become damage. S104 flammability controls whether its fire
effects occur; actual damage reaching apparel still scales here. Shared data is native current
item quality plus common settings; no private Thing index or internal patch stack is introduced.
No specific custom combat provider has a verified compatibility claim from this draft.

## UI audit and relocation

| Source surface | Purpose | Owned destination / difference |
| --- | --- | --- |
| Seven global sliders and reset in mod settings | Balance configuration | Equipment / Apparel durability; common ordered global/save/Use global rows, explicit reset scope, inactive save controls at main menu |
| Apparel information extra entry | Explain actual current item's loss | Existing native item info; clearer broad scope and current state, preserve other entries |
| Automatic Spanish startup message | Initialization diagnostic | Ordinary owned bounded status/warnings; no unconditional success claim from installing patches |
| Bottom tab, bottom-right toggle, gizmo/debug tool | None found | None introduced |

Current itemHP/quality is entity state, not a separate inherited setting. Only multipliers are
configuration. Defaults, scopes, persistence, reload/save-switch behavior, invalid-value recovery,
effect timing and resets are specified. All seven baseline languages are required; no raster art.

## Contracts consumed by other mods or saves

| Contract / evidence | Selected disposition |
| --- | --- |
| Public utility, settings type/fields, patch classes and source package/assembly names | Bounded installed XML/C#/DLL exact-name scan found no external match; no invented facade or identity spoof; reflection/remote/archive limits recorded |
| Seven source global Scribe keys | No automatic read/write/import; future migration needs independent backup/atomicity/restoration/postconditions |
| Native item condition and quality | Preserve; current values remain on removal, no historical damage reconstruction or new item-save schema |
| Native damage and information consumers | Compose only through admitted effective behavior; custom replacement paths need evidence |

No license or public repository was found in inspected material. Credit the source author and
independently implement behavior without copying code, binaries, text or artwork.

## Implementation and coverage

**Nothing implemented.** All rows above are future behavior. Preserve source defaults/range and
broad damage scope; deliberately add common save settings, safe arithmetic, real-durability display
eligibility, localization and semantic admission. Do not reproduce unguarded PatchAll or misleading
combat-only wording. No setting makes repairs cheaper, increases maxHP or adds a protection bonus.
0% suppresses supported damage loss, not direct consumption/removal.

Original active package owns the overlap; skip owned damage/display with bounded warnings. Require
Core/Harmony when implementation ships; no XML operation/new XML dependency is planned. Optional
apparel is absent-safe through real eligibility, optional custom damage paths require narrow guards.
Never bundle source assemblies or the Gateway. Preserve runtime/save/provider gaps as QBD-Q1–4.

## Verification and acceptance

Static source/default/native checks and source base shirt/parka/marine-armor comparisons are
recorded in research. Independent documentation review/strict validation belongs in the ledger.
No gameplay acceptance follows from artifact completeness. Future proof requires focused honest-tier
TDD, reviewed package and personally observed native equip/damage/wear/death/info/settings/rework/
repair/consumption/transfer/save workflows, original/changed-provider cases, seven-language UI and
finalized balance comparisons. Retain causal evidence and exact build/mod list, normal-config
hashes and isolated-process cleanup; check no runtime task until that evidence exists.
