# Need Bar Overflow

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin replacement, **S103/P2-33**. Source **AmCh.NeedBarOverflow**, author AmCh,
[Workshop2566316158](https://steamcommunity.com/sharedfiles/filedetails/?id=2566316158&l=english),
[author source](https://github.com/AmCh-Q/RimWorldMod_NeedBarOverflow). Inspected2026-11-05,
revision`0d368a91cf3dd1dadbc30d83363dd5711bd95e83`; installed source matches ignoring line endings.
About1.5.0 differs from selected1.7 DLL AssemblyVersion1.4.6.0. SHA256
`ACC89788237624EA39C8B4AA4D961931978DA4C9CEB5FB67EEE25FFC9D78B52E`, MVID
`6f288b1c-41e5-42f4-a93e-4da8e295795d`. GPL-3.0; independently implement, no upstream code/binary/art.

[Research](../refinement/2026-11-05-need-bar-overflow-research.md) separates public claims,
binary/native facts, source defects, optional-provider gaps and proposed corrections.
[Proposal/design/tasks](../../../../openspec/changes/specify-megamod-need-overflow/) and
[observable specification](../../../../openspec/changes/specify-megamod-need-overflow/specs/megamod-need-overflow/spec.md)
own all behavior below. **Nothing is implemented or natively verified.**

## Behavior inventory

| ID | Player action → result | Defaults/edges | Status |
| --- | --- | --- | --- |
| NBO-01 | Consume legitimate food/drug gain → reserve retained above ordinary full | Seven need families on at200%; food at least+1nutrition; current ordinary max unchanged | Specified, unimplemented |
| NBO-02 | Enable another supported need → existing real gains can overflow | Others off/dormant100%; no new needs, DLC or activities; kill-thirst qualifying gain+100points | Specified, unimplemented |
| NBO-03 | Continue ordinary play → native need-seeking and side effects remain | No reserve-chasing jobs; nonnegative food wanted; mood inspiration/child thought safeguards | Specified, unimplemented |
| NBO-04 | Change ownership/content/configuration → current eligibility | Colony/player groups on; other groups off; targeted exclusions win; unresolved entries retained | Specified, unimplemented |
| NBO-05 | Lower/disable a limit or save/load → existing finite reserve retained | No positive-gain clamp-down; native losses/resets/removal remain meaningful; own consequences cleaned | Specified, unimplemented |
| NBO-06 | Enable optional rest/joy/kill-thirst rates → current-excess scaling | Off by default; proposed dormant0.5; disabled/excluded need uses ordinary rate despite retained reserve; actual accepted recreation tolerance | Specified, unimplemented |
| NBO-07 | Enable food consequences → current scaled stage affects health | Five columns off; eight explicit dormant stages; reversible display, ordinary health consequences | Specified, unimplemented |
| NBO-08 | Order food or snack → full restriction respects actor/exceptions | On at100%; hungry/drug/hemogen exceptions; no blanket feeding-job block | Specified, unimplemented |
| NBO-09 | Inspect need/forecast → actual reserve in bounded native context | Ordinary-full marker, current/target distinction, piecewise rates, no global UI clamp | Specified, unimplemented |
| NBO-10 | Configure/debug → common scopes and top developer controls | Seven languages, inactive main-menu/save-inheritance editors, ±1/10/100points dev only | Specified, unimplemented |

## Interactions and composed result

S001 mood bars, S003 need forecasts, S007 compact health, S031 toddler skills, S085 partial ingestion,
S088 learning growth and future S127 RimHUD share owned need/presentation facts directly. No internal
Harmony stack or separate per-feature need index. S003's IN-Q2 amount/rate relationship is resolved:
actual reserve uses ordinary MaxLevel as reference; forecast across effective stages or report an
unavailable model. Optional learning overflow increases ordinary growth before S088's single factor;
it does not award toddler XP or historical points. Food/medical/drug policies retain permissions and
actual physical/dose costs, including future S124 Drug Response/S134 Priority Treatment.

## UI audit and relocation

| Source surface | Purpose | Owned destination / intentional change |
| --- | --- | --- |
| Native Needs bars and compact views | Contextual information | Same native surfaces; bounded actual/reserve/target presentation; no replacement bottom tab |
| Source ModSettings long form | Configuration | Ordered Needs → Overflow common Global default/Current save/Use global rows; validated atomic changes |
| Comma-separated disabling lists | Content exclusions | Structured searchable identities with unresolved-entry display; no translated-label identity ambiguity |
| Native bar ± controls / Character Editor modifier hooks | Unrestricted need editing | Owned actions exclusively top debug toolbar, developer-only; no copied ordinary row editors |
| FoodOverflow health condition | Consequence explanation | Owned contextual condition and food tooltip; reversible visibility, no dedicated menu |

All settings use shared load/store/resolve/render paths; no scope exemption needed. Need levels and
health state remain pawn state, not settings. No source bottom tab or simple-view toggle is required.

## Contracts, dependencies and co-installation

| Contract / evidence | Disposition |
| --- | --- |
| Need amounts use native saved curLevel | Retain through ordinary save/load, no second reserve ledger; no invented migration claim |
| Source type-named global settings, sign encodings, FoodOverflow health Def | No automatic import/rename/delete; a later migration requires its own backup/atomicity/restoration contract |
| Exported DisableNeedOverflowExtension list and modsOverflow dictionary | Own source XML consumes extension; no external binary caller established by bounded installed scan. No facade or spoofed package/assembly identity |
| Improved Need Indicator About recommendation / Modern Needs Tab legacy load hint | Metadata references, not proven callable compatibility contracts |
| Original AmCh.NeedBarOverflow active | Suppress owned overlapping reserve/effects/UI group with explanation whenever the original provider is loaded; never unpatch source |
| Optional exclusion providers | Five spec-listed mappings: Dust Bunny/Dead Stomach/Redundant Stomach→food, Mind Coalescence→mood, worn Techfriar Crown→rest/recreation. Scope by canonical package/content/semantics; absent safe, changed affected adapter fails closed; missing identities and1.7 contracts remain NBO-Q2 |
| VNPE dripper | Preserve its real threshold, cost and ingestion; do not copy nonexistent target or blanket reserve-trimming postfix |

Core required; DLC/providers optional only for their content. Harmony required for actual hooks.
First XML Extensions use requires `imranfish.xmlextensions`; provider exclusions must guard Def/node
shape and merge uniquely, preserving other entries. No Workshop assembly or Gateway dependency.

## Deviations, omissions and remaining work

Correct recreation's no-instant-level input, disabled negative coefficients, positive gain reducing
an existing reserve, global need-percentage AI override, generic rectangle clamps, stale hash-only
eligibility, wrong mutant DLC gate, mutable global health Def/configuration and sticky display.
Keep documented range/default differences from stale Steam/raw XML explicit. No hemogen/psyfocus,
invented activities, unsupported need catch-all, inactive source branches or universal compatibility
claim. Developer gesture/UI relocation is intentional. Character Editor's separate Shift fill-all
assignment from100% to1000% (subject to cap) is deliberately omitted; explicit ± actions are not
claimed as full gesture parity. Source health/settings migration is omitted.

All runtime adapters, target documents/executable predicates, tests, translations, performance
measurements and live verification remain open in NBO-Q1–5 and the unchecked task list. Final native
evidence must connect real ingestion/rest/joy/health/settings actions to observed changes on the
reviewed build, including exact optional-provider groups. Static documentation validation is not
gameplay, balance, compatibility or visual acceptance.
