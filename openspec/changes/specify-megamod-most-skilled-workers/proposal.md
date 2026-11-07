## Why

Players should be able to reserve a production bill for the highest-skilled suitable workers without
manually changing a named assignment whenever the colony changes. The rule should explain when it
waits for an absent or downed expert and preserve ordinary recipe eligibility and physical work.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Origin: replacement, queue **S019**, **Most Skilled Bill Restriction**, `Matheus.MostSkilledBill`.
**Specification only.** No implementation or native acceptance is delivered.

## What Changes

- Specify a contextual Most skilled worker choice with Include away and Ignore if downed, both off
  initially, tied winners and current skill-level ranking rather than a guaranteed quality result.
- Make the retained allowed skill range visible. Propose a recipe-relevant benchmark so workers
  assigned only to an unrelated work type cannot block production merely by sharing its skill.
- Share map facts and home associations with existing owned consumers, with fresh visible decisions
  instead of duplicating the source's timed maximum cache.
- Compose complete worker choices directly with S014 linked lists and S017 bench policy/snapshots;
  preserve in-progress work and reject unsafe saved-policy fallback.
- Keep controls scoped to the actual bill. Do not reproduce global dropdown/range interception,
  unconditional pawn-comp injection or incomplete source clipboard propagation.

## Capabilities

### New Capabilities

- `megamod-most-skilled-workers`: highest-skill production restrictions, benchmark scope, contextual
  controls, away/downed choices, lifecycle and composition with shared production plans.

### Modified Capabilities

None in archived capabilities. S017's worker-policy draft gains a cross-reference to this composed
choice; its existing capability remains owned by that change under the same mod owner.

## Impact

Future implementation affects production worker selection, bill UI, saved worker choices and shared
map/home data. It introduces no new bottom tab, mod setting, skill bonus or recipe/balance change.
Required future runtime dependency: **Harmony**. No required source mod, BWM runtime dependency or
source assembly; their selected behavior is implemented within megamod. No optional provider adapter
is currently promised. DLC-specific workers appear only when their content and capability exist.

No XML patch is selected; use owned saved home associations rather than editing every pawn Def.
If a later XML mutation is needed, XML Extensions (`imranfish.xmlextensions`, Workshop 2574315206)
becomes required and every operation needs semantic existence/content guards and merge preservation.
Source-active coexistence withholds our overlap. Foreign saves/APIs are recorded without automatic
migration or package impersonation. [Research and open questions](../../../mods/megamod/docs/refinement/2026-11-02-most-skilled-bill-research.md)
separate installed behavior, proposed fixes and remaining proof.
