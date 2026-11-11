# Cherry Picker

## Identity and provenance

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Origin: **replacement**, S038/P1-30. Source: Cherry Picker, `Owlchemist.CherryPicker`, installed
Workshop **3521312241**, publisher ajperson1927; About credits Owlchemist and supports through 1.6.
Inspected 2026-11-03, installed assembly **1.0.62.0**, informational
`1.6+7e77b7fdba2daa472a5b7c2b35ac17c149dffa99`, SHA-256
`4F4AB849B5A3F5BF9FDC497607DE05B03035ED920E2C955FE6660C89BDAF73E1`.

[Research](../refinement/2026-11-03-cherry-picker-research.md) records public documentation first,
all fourteen runtime files, native parser paths, consumer XML, exact hashes and source limitations.
Legacy public code is MIT copyright 2020 jptrrs; installed-fork provenance is not assumed identical.
No source binary/code/assets are copied into the product. Source-derived facts, proposed corrections
and remaining native research are distinguished there.

Owning [change](../../../../openspec/changes/specify-megamod-content-selection/proposal.md),
[design](../../../../openspec/changes/specify-megamod-content-selection/design.md),
[selection contract](../../../../openspec/changes/specify-megamod-content-selection/specs/megamod-content-selection/spec.md),
[availability contract](../../../../openspec/changes/specify-megamod-content-selection/specs/megamod-content-availability/spec.md)
and [tasks](../../../../openspec/changes/specify-megamod-content-selection/tasks.md).
**Specification drafted; nothing implemented or accepted in game.**

## Behavior inventory

| ID | Player input/action → observable result | Scope/default/edge cases | Contract |
| --- | --- | --- | --- |
| CK-01 | Browse/filter catalogue → exact identifiable entries | All source families; protected generated entries; excluded/missing rows retained; S020 selectors | Selection: catalogue and filters |
| CK-02 | Set global/save choices → inherited or overridden policy | All content allowed/presets inactive initially; common Use global; main-menu save actions disabled | Selection: scope and application |
| CK-03 | Activate/deactivate removal lists → reason-aware exclusions | Nested/overlapping/cyclic/missing members; no erasure of independent settings | Selection: presets and authored lists |
| CK-04 | Load requested choices → new acquisition constrained | Fixed starting supplies and butchering/destruction drops also filtered; existing objects/value/nutrition/components retained; no silent import | Both contracts |
| CK-05 | Browse/execute bills or construction → legal choices or explained block | Every required ingredient group, products and fixed costs; no free production | Availability: physical/production |
| CK-06 | Exclude research → bypass selected gate, retain surviving ancestors | Completed progress, independent prerequisites, invalid chains | Availability: research |
| CK-07 | Browse Architect/sample terrain → only allowed new orders | Categories/designations, unchanged existing ground and work | Availability: terrain/Architect |
| CK-08 | Generate world/pawns/map → omit selected candidates | Factions, kinds/races, biomes, steps, scatter; bounded impossible request | Availability: world/pawn generation |
| CK-09 | Generate pawn appearance/background/weapon/name → allowed choices | Traits, backstories, bodies, hair/beard/tattoos, weapon traits, naming groups; existing entities retained | Availability: appearance/background |
| CK-10 | Select events/quests/raids → allowed choices | Incidents, independent quests, traders, gatherings, breaks, inspirations, weather, raid strategy/arrival/age; ongoing state retained | Availability: events |
| CK-11 | Apply new condition/thought/wear or reconcile drug policy → respect selection | Existing hediffs/memories/worn gear retained; current drug policy uses S029; drug exclusions withheld for original DPF or unavailable maintenance | Availability: health/apparel/drugs |
| CK-12 | Select ideology/styles/genes/abilities → allowed choices | Active DLC; existing state; valid remaining requirements | Availability: ideology and genes/abilities |
| CK-13 | Select optional powers/psycasts → allowed paths and prerequisites | Ancients/VEF/VPE package/shape adapters; unavailable integration explained | Availability: optional powers |
| CK-14 | Browse UI/random interactions → excluded choices hidden | Work columns do not disable jobs; stored special filters do not broaden; existing storyteller continues | Availability: interface |
| CK-15 | Use owned consumers → consistent availability and physical stock | Bill browser/Architect/drug policies direct composition; search/hauling retain existing goods | Availability: shared consumers |

All rows are **drafted, unimplemented**. Detailed source behavior and proposed deviations are in
the complete per-type research table; this report does not substitute source names for requirements.

## Interactions and composed result

S020 owns selector navigation; S038 owns actual content policy. S029 reconciles usable drug rows
as part of policy publication, including physical-stock-independent removal and fresh restoration.
S012 bill browsing and S036 Architect obey availability without recreating their own removal lists.
S014 whole-list workbench linking retains shared orders, recoverable work and quotas when blocked.
Original Drug Policy Fix suppresses owned S029 maintenance, so S038 withholds dependent drug-item
exclusions in that combination. Successful owned drug application publishes consistent exclusions
and policy rows together; failure restores prior rows, explains unavailable drug exclusion and
leaves other supported families active. No compatibility with original DPF is assumed.
Future S136 Floor Pipette respects terrain eligibility. Search/hauling share real Thing candidates
and still see existing excluded goods; current native eligibility is rechecked at action time.
No source hook survives as a patch against owned code.

Original Cherry Picker active: select original and leave owned application/adaptation inactive with
bounded startup warnings and visible status; preserve both configurations. No foreign unpatching,
automatic deactivation or duplicate content processor. The original scrollbar add-on still targets
its original host; its presence alone does not disable owned selector UI.

## UI audit and relocation

| Source surface | Purpose | Owned destination / deviation |
| --- | --- | --- |
| DoSettingsWindowContents with type/source/search and checkboxes | Configuration of available game content | Structured mod settings → Content selection; S020 selectors, common global/save controls and requested/applied/reason state |
| Reload/restart messages | Explain partial restoration/application | Consistent pending-until-save/reload explanation; no misleading enabled state |
| Debug-spawn filtering | Developer-only content override selector | Existing top debug workflow; no new ordinary tab or shortcut |
| No source bottom tab | None | None added; hiding another tab is itself a content setting, not a new mechanic window |

Each entry/preset is actual configuration, not pawn-specific state. No global-only exception.
Global defaults, overrides and inheritance persist through the common path; main-menu save
controls/callbacks are disabled. Search/scroll/menu state is transient. Seven languages, exact
input ownership and save-switch workflows remain implementation acceptance tasks.

## Consumed contracts and persistence

| Consumer/evidence | Binding | Decision / verification |
| --- | --- | --- |
| Too Many Mods, `Wara.toomanymods`, actual installed XML | CherryPicker.DefList element, defName/label/defs strings, original MayRequire, provider XPath appends | Guarded owned Class adapter on compatible native parser, retain element/member/source identity; absent/present/provider-addition workflows required |
| S020 `lucius.cherryscroll`, compiled target inspection | Original Mod_CherryPicker settings method/FloatMenus | Direct owned selector composition; no pretend original assembly or self-patch |
| Smart Minify About | Original loadAfter package only | Document actual binding; no callable facade inferred |
| Source global config | `keys`, TypeName/defName[/Namespace] | Document syntax; no silent migration or foreign-file write; future opt-in migration needs backup/atomicity/recovery/postconditions |
| Original-package/hard assembly checks | Not satisfied by an authored XML data adapter | Explicitly unsupported unless a later narrow contract is proven; no package identity spoof |

The default new 1.7 parser honors Class; its legacy parser does not at initial type lookup.
Composite provider/DLC gates, provider callbacks, required-original metadata and legacy-mode support
remain CK-Q2. Proposed preset compatibility is **not yet a verified drop-in claim**.

## Implementation and coverage

| Scope | Implemented | Deviation / possible conflict | Missing or omitted |
| --- | --- | --- | --- |
| CK-01–15 | Reviewed pure stable key/catalog slices; common requested-settings connection in independent review; unregistered native catalog reader | Source global mutable definitions replaced by per-save availability policy | Applied publication, availability families, native wiring, per-patch admission and native evidence |
| Restoration and costs | Nothing | Preserve existing goods and costs; block invalid work instead of source zeroing/removal | Exact saved-job/native refusal paths |
| Application timing | Immutable requested snapshot uses real common detached save/global reads; no applied publication | Load-boundary application; visible pending requests rather than partial immediate restore | Lifecycle wiring, publication and reviewed UI |
| Presets | Independent cached common activation definitions and captured requested values; no graph application | Independent reasons; deactivate only own contribution, including TMM_All rather than unconditional revive/reset | Reason graph, guarded authored-data adapter and real provider validation |
| Dependencies | Nothing changed in scaffold | Harmony/XML Extensions required when implementation uses them; other providers/DLC optional | Correct actual package and exact active-mod verification |
| Import/arbitrary Def editor | None | No automatic foreign settings import, no identity spoof | Deliberately outside this change; possible future migration CK-Q5 |

XML adaptation must preserve provider entries and unrelated gates, use package-ID/content guards,
and avoid partial mutations. Runtime patches require minimal semantic admission, compatible-owner
tolerance, actual incoming-stream checks, repatch handling and bounded skipped-patch warnings.

## Verification and acceptance

Implementation checkpoint 2026-11-08: the first incident-only native save-override workflow passed
38 steps. The orchestrator personally inspected all8 captures: inherited exclusion, enabling the
save editor, continued refusal while the edit was pending, then a physical ship chunk after native
save/load applied the override. Globals remained unchanged; exact build, logs and cleanup evidence
is retained in `artifacts/Implementation/megamod-p0/native-incident-save-override-reviewed.md`.
This supports that component on its tested build. Other content families, full selector/preset
workflows, provider combinations, successful Def-reload reconciliation and the integrated P0 gate
remain incomplete; Cherry Picker replacement is not accepted.

Document/schema review does not establish runtime compatibility. CK-Q1–6 track exact game/provider
seams, adapter proof, proposed timing/dependency decisions, migration boundaries and final native
workflows. All implementation tasks stay unchecked. Later work must retain focused RED/GREEN,
reviewed package identity, causal player actions and personally inspected results in fresh minimized
Gateway runs, including save transitions and real provider presence/absence/shape failure.

Semantic review preserves costs, work, value and nutrition; makes research bypass and threat removal
explicit; and rejects impossible generation without retries or fabricated resources. It does not
claim in-game balance or optimization. Pure host slices do not close any native acceptance task.

Independent source review corrected the missing PawnKindDef inventory and the trigger for style
database removal. Contract review added original-DPF/unavailable-maintenance fallback, explicit
fixed starting supplies and incidental products, and corrected S012/S014 composition references.
Focused re-reviews resolved the substantive findings; the remaining publication-timing wording was
aligned to the atomic contract. None of this closes an implementation or live acceptance task.
Typed strict OpenSpec validation passed **60 items, 0 failed**. The two capability drafts contain
**23 requirements and 54 scenarios** at that checkpoint; all **32 implementation tasks remain unchecked**.

S068 composition refinement (2026-11-04): applied thought exclusions prevent new acquisition while
retained memories continue under the current [memory-fading policy](all-memories-fade.md). Their
types remain visible in fading previews/counts with a distinct acquisition status; pending choices
remain pending until S038 application. Neither exclusion nor fading deletes existing memories.

Refused captured/fallback counselling awards must not be announced as granted. Use the actual
native acquisition outcome, or refuse the affected action before cost/cooldown when truthful
reporting cannot be admitted. If safe refusal also cannot be supported, leave the coupled exclusion
group unavailable and retain its requested choice. This remains S038's exclusion/reporting obligation
even when owned S068 fading is unavailable or the original fading mod supplies the calculation.
The added scenarios/design/tasks remain documentation only; no runtime acceptance is implied.
