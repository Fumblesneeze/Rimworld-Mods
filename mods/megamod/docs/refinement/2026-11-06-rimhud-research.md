# S127 — RimHUD research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Research **2026-11-06**. Source behavior is inspected, not executed or accepted. This note supplies
evidence for the [owned change](../../../../openspec/changes/specify-megamod-pawn-hud/); observable
requirements belong in its two specs, implementation mechanisms in its design.

## Public documentation before source

Read the [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=1508850027),
[author README](https://github.com/Jaxe-Dev/RimHUD/blob/6f1dbfc3ba1196ab081ee4d382101ddd6b23badb/README.md)
and [author widget/preset wiki](https://github.com/Jaxe-Dev/RimHUD/wiki) before code analysis.
The author describes selected-pawn docked/floating inspection, dangerous-health/mental warnings,
configurable layout, modded needs/skills and provider extensions. Configuration is reached through
a hover gear. The stated design resolution is at least 1600×900; smaller displays need font
adjustment. Add/remove safety, multiplayer and broad compatibility are author claims, not our
acceptance. The source maintains English only; owned work still requires all seven baseline languages.

The wiki documents provider preset discovery and five version-one widget forms. These are concrete
contract candidates rather than proof that any installed provider binds. All descriptions below
are independently paraphrased; upstream implementation/assets are not selected for copying.

## Exact inspected inputs

| Input | Identity |
| --- | --- |
| Installed mod | **RimHUD**, Jaxe, `Jaxe.RimHUD`, Workshop **1508850027** |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/1508850027` |
| Advertised versions | 1.1–1.6; root folder selected for 1.6, older versions have Legacy overlays; no advertised 1.7 |
| Mod version | **1.17.5**, minimum configuration version **1.17.0** |
| Root `Assemblies/RimHUD.dll` | 221696 bytes, SHA256 **D24BB116949DA4F2EEFCF350CB73E20B014A9DB28D922DDAE256D11D59DB206F** |
| About.xml SHA256 | `9424327E6EC107E64708DE07B64BCD4EC2598237514D290EA84693F2DFE0BA9C` |
| LoadFolders.xml SHA256 | `65EF54CF845750FBDC9909F16733DBA79D633A9525E2D6E6E0F54987723C8B70` |
| Public source | Jaxe-Dev/RimHUD **6f1dbfc3ba1196ab081ee4d382101ddd6b23badb**, same declared version; 170 retained text files |
| License evidence | No LICENSE/COPYING/NOTICE path found in the inspected public tree; no redistribution permission inferred |
| Required source dependency | Harmony; source uses unguarded PatchAll, not an acceptable owned admission policy |

Local evidence: `artifacts/Refinement/megamod-spec-20261106/rimhud/` contains input/public manifests,
pinned source, wiki text, complete read-only root-assembly decompilation, selected consumer/native
decompilations and preset inventory. The installed compilation was used to check key defect paths
and source identity; the whole source tree is not claimed byte-equivalent to the DLL. Hashes identify
evidence, never a runtime compatibility predicate. No Workshop file was modified or assembly loaded
into the game. No game/config/save was launched or changed.

## Observable source inventory

| Surface | Inspected source behavior | Owned treatment |
| --- | --- | --- |
| Subject/lifecycle | `State`: single selected Pawn, playing, map/background-world view. Activated is static true, no persisted toggle. Four humanlike/creature and player-managed/other targets; hosted pawns count managed. | Preserve selection semantics, persist per-save operational view choice, respect actual control/hidden-information rules. |
| Docked/floating | Root embedded default layouts; docked replaces summary/geometry/tab layout; floating uses nine anchors. `LayoutLayer` prepares on changed pawn or wall-time interval, default 500 ms. | Preserve both presentations and refresh; native extension paths, screen fit, no stale action context. |
| Identity | `NameHeader`, `GenderRaceAndAge`, `RelationKindAndFaction`, Bio/Animal tooltips. Biological years/day remainder/birthday, mutant/xenotype, title/faction relations, native traits/backstory/stats. | Native knowledge and tab visibility remain authoritative; unsupported specialized pawns retain native inspection. |
| Health | `HealthBar`, `HealthConditionValue`, `HealthTooltip`; visible hediffs, bleeding death <1 day, untended, life-threatening, active sickness, downed, summary bands <.3/<.6/>.9. | Preserve order and thresholds in observable spec; no concealed-condition leakage; S007 inside normal Health, S137 separate. |
| Mental | Stun/trance, stagger, mental state, imminent extreme/major/minor break, inspiration, mood >.9/>.65. Thought tooltip multiplies first offset by count. | Preserve priority but use real native grouped effect when stacking is non-linear. |
| Need bars | Mood/food/rest/recreation/suppression/mech-energy; generic NeedDef, live percent; mood break markers. Tooltips diet/speeds, energy fall, comfort/beauty/outdoors, terror/suppression. | Truthful overflow values, no change to needs, direct owned formatting. |
| Skills | 12 ordinary plus generic SkillDef. Disabled dash; saturation outranks active skill, then passion. Level plus rounded/clamped 00–99 XP remainder; native description and specialized stats. | Preserve meanings; direct S128 icons later, no large string repetition for unknown passion values. |
| Animals | Generic/trainable steps, wanted/learned colors, master relation. Ordinary and newly available trained abilities in defaults. | Respect actual trainability/availability; no training commands from summary clicks. |
| Work/status | Job/lord/attack report with work-type/relevant-skill tooltip, first queued + remaining count, primary equipment/restraints, native hand stack, prisoner resistance/will, component/quest info. Traits/stats/records optional; standalone Traits hover includes full native trait descriptions. | Cargo composes S021/S126; real condition checks; truthful skill/policy numbers and retained explanations. |
| Selectors | Area hover/selection/manage; configurable food including hosted pawns; player-faction apparel/current-hour schedule; native management dialogs. | Capture subject/map/hour; revalidate, preserve native authority. |
| Header | Native select-next/info, genes, faction/ideo, hostility, medical care, self-tend, certain animal/mech renames. Self-tend tooltip hardcodes .7. | Preserve native extension buttons and actual self-tend efficiency; entity controls never common settings. |
| Floating log | Up to 300 native log entries, cached by pawn/play/battle ticks, reset on map switch. Iteration draws only LogLineDisplayableLog subtype. | All chosen native entries remain accessible; no stale log or omission of supported entry kinds. |
| View/config | Bottom-right toggle; Shift-toggle opens separate config dialog, hover gear; ordinary mod-settings screen mostly tutorial presentation. | Toggle stays bottom-right; all config routes to common mod settings, no extra bottom tab. |
| Debug/tutorial | Shift-click config footer toggles timing overlay under developer mode; source suppresses native Tutor while floating. | Diagnostic top debug toolbar only, temporary off by default; native tutorials retained. |

### Default layouts and preferences

Embedded docked defaults place identity/policy controls above two columns (health/needs and
skills/training), followed by work and equipment/carrying. Floating is vertical with a name header.
H/h denote player-managed/other humanlikes; C/c their creature counterparts. Defaults restrict
policy/work and certain needs to appropriate groups, with absent actual models collapsing. Catalog
supports Blank/Separator, common values, Need/Skill/Training/Stat/Record and custom forms. Source
editor inserts at the start, supports rows/sibling rows, movement/removal, target masks and fill
height. A copied `LayoutElement` reuses `Args`, an aliasing risk; owned copies must be independent.

Theme defaults/ranges are enumerated in the customization spec. Source: docked true, pane modify/log
true, height232 [165,800], min7 [6,12] tabs of width85 [72,150]; floating top-right, offsets0,
320×416 [100,1000]; letter compress true/padding4 [0,12]; fonts13 [+9/-1] with size ranges7–20,
0–24/-5–0 and line height100–250%; labels95/values40 [30,150]; decimals true. Source colors include
white text, .5/.65 gray disabled/neutral, native readable red, yellow warning, cyan-green good,
green excellent; 25 customizable semantic roles (including line, skill and faction colors).
Owned UI uses the common scope rows and documents the global preset-library exception. A single
three-choice presentation replaces the source's coupled docked/modify flags so inheritance cannot
produce an invalid pair. Global resets restore section built-ins; save resets restore inheritance
while preserving dormant overrides. These are owned scope decisions, not source behavior.

Installed presets: **Classic**, **Compact**, **Preview**, **Dubs Bad Hygiene**, **Dubs Bad Hygiene +
Thirst**. Compact sets regular font12 and docked height220; hygiene variants use252/268 and need
IDs `Bladder`, `Hygiene`, optionally `DBHThirst`. Their `Requires` alternatives are
`dubwise.dubsbadhygiene|dubwise.dubsbadhygiene.lite`. Preview includes Traits. Full retained XML
inventory records each input hash and widget selection. These prove inputs, not rendered fit.

### Configuration/preset persistence

Source globals live under `Config/RimHUD/Config.xml`, with `Docked.xml`, `Floating.xml` and
`Presets/*.xml`. Version <1.17.0/absent resets configuration; malformed XML may be replaced during
load. Source saves a preset by name without stable origin. Library scans own Presets and external
`RimHUD/Presets`; `Requires` is any active listed package. Layout XML carries group/row/widget IDs,
target masks, fill-height, optional dimensions/tab counts/text-size tuple. Save permits one or both
layouts and optional sizing. User deletion asks confirmation; same-name save overwrites without a
separate confirmation. Source presets can reset dimensions/text sizes omitted by the file.

Owned choices: common global/save persistence, explicit destination, atomic/import rollback,
preserve omitted fields, unambiguous origin and read-only provider files. No automatic foreign
configuration migration and no reset solely because the source version changed. Unavailable
entries remain recoverable. Existing pawn policies are native gameplay state, not HUD settings.

## External contracts and concrete consumer evidence

The [design](../../../../openspec/changes/specify-megamod-pawn-hud/design.md) records exact callback
tuple signatures and consumer bindings. The five published Def forms are CustomNeed, CustomBar,
CustomValue, CustomSelector and CustomWidget; v1 callbacks receive a Pawn, raw drawing receives a
Rect and reports whether it used space. Display labels, style defaults, optional tooltips,
thresholds and action identities matter. `apiVersion=0` is treated as v1 with warning in source.
The wiki's custom-bar Regular default differs from the uninitialized compiled field: its
`DefaultValue(Regular)` attribute does not initialize it, and enum zero is Small. The owned spec
explicitly selects the published Regular default; values/selectors retain Small.
Need callbacks are optional; other forms require exact GetParameters or OnDraw/GetMaxHeight shapes.

Read-only installed text/path search found no separate custom-v1 Def or external preset-folder
consumer. This is a bounded installed-set result, not an assertion that none exist publicly.
Binary-name hits prompted targeted decompilation: Useful Marks' **1.7** package/assembly-gated header
postfix, Vehicle Framework and bundled SmashTools **1.6** package-gated bypass/header hooks.
Retained earlier VSE and Readable Numbers research provides their actual private constructor/color
and styled-number consumers. GUI Performance Patch's earlier retained source reflects private
text-measurement methods. Orassans' installed **1.3-only** source targets an obsolete PawnModel;
it is not a present 1.7 compatibility claim. Passion Icons' metadata hard-depends on Jaxe.RimHUD;
its full behavior is next, S128. A same-looking owned HUD does not satisfy those package gates.

Optional source integration machinery identifies mods by display name. Pawn Rules calls
`PawnRules.Integration.RimHUD` and writes ReplaceFoodSelector/HideGizmo. Its base `Enabled` flag
cannot become true through the inspected `if (IsActive)` constructor branch, which depends on
that flag already being true. Multiplayer directly attempts registration of five setters after
finding its API; that attempt is distinct from the broken `IsActive` flag. Neither advertised
integration is accepted here, and native action coverage must exceed just those five setters.

## Inspected defects and intentional deviations

These are control-flow findings, not live reproductions:

1. Food/apparel/area menus invoke `Active.Pawn` later; changing selection can retarget a pending
   action. Schedule captures hour but still resolves pawn later. Owned actions capture/revalidate.
2. `CompInfoValue` tests whether the nullable shutdown boolean **has a value**, so ordinary mechs
   can be described as requiring transport. Owned display requires true.
3. `HudContent.ExternalElements`, `IsExternalType`, and HudArgs' Def lookup omit CustomSelector
   despite a builder existing. CustomSelectorDef passes hover/click in reversed constructor slots,
   and SelectorWidget does not render its supplied tooltip. Preserve the documented API semantics.
4. `LetterStackPlus.Draw` returns without letters when excess≤0 while the prefix suppresses native
   drawing. Compression is active even with no selected pawn; cached lists can outlive a game.
   Owned logic acts only on actual overlap and preserves native letter access.
5. `Presets.Load` returns during the first external-mod iteration even if no preset was found;
   same-name origin is ambiguous. Import/apply must identify the chosen provider and be transactional.
6. Layout target serialization maps an empty target set back to absent/all. UI tries to prevent
   that state; owned validation must prevent hide-all becoming show-all on reload/import.
7. Shared layout arguments and source resets can affect more entries/preferences than edited.
   Owned settings retain independent copies and explicit mutation scope.
8. `Report.HandleError` deactivates everything and offers reset-oriented recovery; constructor
   initialization can call `UnpatchAll()` without a scoped owner. Owned rollback never touches
   another owner, and widget failure never erases configuration.
9. Source gene transpiler replaces every approximately165 float; pane width is actually a prefix
   in source/compiled listing despite README calling it a postfix. Owned guards inspect semantic
   consumers and cardinality, not constant/name-only assumptions.

## Playability and acceptance limits

No costs, production, research, stats, training or medicine yields change. Additional visibility
must remain within native knowledge; shortcuts must retain faction, work, age, policy and DLC
restrictions. Lack of optional providers must never make basic inspection unplayable. HUD numbers
must agree with authoritative owned/native values, especially mood aggregation, overflow and cargo.
That is the documentation-stage balance/playability assessment; native usability, exact finalized
content, performance and compatibility remain future reviewed-build workflows.

RH-Q2–RH-Q6 retain remaining native/facade/provider/localization/composition research. No questions
are sent to the user to explain source behavior. Two independent document reviews and focused
resolution passes found no remaining issues after source/default and settings-scope corrections.
Strict typed validation passed148 items with0 failures; the ledger records the checkpoint and
local evidence. All27 implementation tasks remain unchecked.
