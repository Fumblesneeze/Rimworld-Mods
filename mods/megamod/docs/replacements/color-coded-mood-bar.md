# CM Color Coded Mood Bar — research and replacement coverage

Owner: Make Rimworld Great Again (`fumblesneeze.megamod`). Intake: **P1-01**.
Research date: **2026-11-02**. Status: **analyzed; specification drafted with listed gaps**.
Implemented: **nothing**. Live compatibility/acceptance: **not performed**.

Change: [specify-megamod-color-coded-mood-bar](../../../../openspec/changes/specify-megamod-color-coded-mood-bar/).

## Identity and evidence

| Item | Evidence |
| --- | --- |
| Installed name / author | CM Color Coded Mood Bar [1.1+] / CrashM |
| Package / Workshop | `CrashM.ColorCodedMoodBar.11` / `2006605356` |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/2006605356` |
| 1.7 selection | `LoadFolders.xml` selects root and `1.7`; assembly `1.7/Assemblies/ColoredMoodBar17.dll` |
| Assembly version | `1.7.4976.1` |
| Assembly SHA256 | `CA0F194299ADC11343906761E01BF998C3A08C85B913F4426F8A6DF9A87EDEAB` |
| SettingHelper17 SHA256 | `5A6B08DD2067BB99867E9277DA2067FDEF07F61CB6E427ECBEE53CECDE1C058E` |
| Runtime requirement in original | Harmony; the helper is included by the original, not a megamod dependency |

Research order:

1. [Author's Steam description and changelog](https://steamcommunity.com/sharedfiles/filedetails/?id=2006605356).
   Useful intended features, defaults and historical integration notes; July 2026 adds age, October
   2026 updates 1.7. Old prose is not authoritative for every current default or threshold.
2. [Author's download page](https://www.crashm.co.uk/Home/CCMB) and
   [published source archive](https://www.crashm.co.uk/Content/Downloads/ColoredMoodBarSrc.zip).
   Archive SHA256 `9662ABC061B4593AA2A83081F9BD3A61BCC0EC918DA9BB4C54C99CD92FED5653`.
   Contains older `ColoredMoodBar`/`ColoredMoodBar13` sources, not an established exact 1.7 source match.
   Root license is MIT, copyright 2017 Brandon. This is archive evidence, not a blanket license claim
   for every later binary, texture or helper.
3. Read installed metadata/load selection and decompiled the selected 1.7 DLL with ILSpy 11.2.0.10050
   to resolve current behavior. Namespace remains `ColoredMoodBar13`. Decompiled methods may contain
   missing-reference annotations; conclusions here come from readable control flow and constants,
   not a successful rebuild. No downloaded files were changed.

Local research only: `artifacts/Refinement/megamod-spec-20261102/color-coded-mood-bar/` contains the
archive and `decompiled/`. These are ignored evidence, never package contents. Hashes identify the
inspection input; they are not proposed runtime compatibility predicates.

## Behavior inventory and traceability

All rows below are **specified, not implemented**. Requirement names refer to the owning capability.

| Source behavior | Evidence in installed namespace | Observable requirement / treatment |
| --- | --- | --- |
| Six mood bands and pawn-specific break thresholds | `MoodCache.DoMood`; `MoodPatchSettings` | Mood is readable in each eligible portrait |
| Risk outline, layering, neutral outline; schedule color | `MoodPatch.DrawColonist`; settings | Risk and schedule outlines are independently configurable |
| Illness immunity race, greatest warning | `MoodCache.DoIllness` | Illness warnings distinguish the worst current immunity race |
| Visible human pregnancy progress | Same method, `PregnantHuman` | Pregnancy progress has a distinct indicator |
| Four bleeding presentations | `MoodCache.DoBleeding` | Bleeding warning reflects estimated time remaining |
| Heat/cold stages; suspended state | `DoIllness`; `MoodCache.Update` | Temperature and suspension are visible without inspecting the pawn |
| Player-faction love relationships, pair colors, gender, biological age | `DoRelations`; `TruncateLabel.TruncateWithAge`; label generation | Optional relationship and identity information uses current pawn state |
| Drafted-only/always weapons, size and vertical placement | `MoodCache.Update`; `MoodPatchMod` | Equipped weapon presentation is optional and adjustable |
| Six portrait filters and four fallback presentations | `PortraitMode`; `HidingMode`; `MoodCache.Update`; draw path | Portrait filters have explicit reduced presentations; world/caravan edge still CM-Q1 |
| Activity icons, role/guest markers, labels and selection feedback | `DoIconVars`; `MoodPatch.DrawColonist`; `GenMapUI2` | Native colonist-bar actions and information remain usable |
| Red health strip beneath name below 99.9% summary health; drafted underline and native name color | `GenMapUI2.DrawPawnLabel`; `MoodCache.DoLabelVars` | Explicitly retain proportional health presentation, including reduced modes with visible labels |
| Settings, refresh interval, reset/apply | `MoodPatchSettings.ExposeData`; `MoodPatchMod` | Configuration follows shared settings; see deviations |
| Alternate bar/badges/jobs/disabled markers | `Main` reflection and patch setup | Research candidates only; CM-Q2 blocks support claims |

## UI and settings audit

The source draws on the **existing top colonist bar**, with configuration in mod settings. No
source-added bottom tab was found in the inspected assembly. Preserve that contextual placement.
Configuration moves into megamod's ordered searchable colonist-bar section with common global/save
inheritance; no separate source-style configuration window or private settings serializer.

Fresh source defaults: mood colors at 44% opacity; break outline on/behind, neutral outline off;
schedule off; illness+pregnancy, bleeding, thermal and suspension on; relationships, gender, age,
forced draft portrait refresh and weapons off; all portraits shown; hidden fallback completely hidden;
weapon drafted-only/scale 1/offset -10; update interval 150 ms (15–2000 ms adjustable).
Source alternate label renderer is on. The specification retains its visible capabilities without a
technical renderer switch. Mood palette and technical value tuples are in design.

## Deviations and possible conflicts

- **Common settings:** source settings are global-only. Megamod adds save inheritance and scope-aware
  reset; no source configuration migration is implied.
- **Reset consistency:** installed reset enables relationships despite their fresh default being off,
  swaps male/female default colors, and leaves some state such as hiding mode untouched. Our reset
  returns the same complete defaults as a new installation.
- **Fractional weapon scale:** source UI casts the 0.1–5 slider result to integer. Our scale stays
  continuous; choosing 0.5 does not hide the icon by turning it into zero.
- **Filtering/state cleanup:** source glow/weapons can be drawn outside the portrait-filter condition,
  and several cached condition flags are set without a corresponding visible reset in their method.
  Our intended behavior is no stale or leaking indicators. These are static-code findings, not
  reproduced bug reports.
- **Color/default documentation:** source neutral color is pale green, not the older documented gray;
  installed alternative labels default on. Installed bleeding cutoffs are 3/8/24 hours. The spec
  uses installed values, keeping historical prose out of the contract.
- **Original co-installation:** proposed initial policy suppresses our CM feature while the original
  provider is active, with a startup explanation. Other megamod features remain independent.
- **Other bar mods:** their order/drawing/input contracts require separate researched adapters and
  exact installed-provider verification; absence of other Harmony patches is not a prerequisite.

## External interface and persistence audit

The installed DLL has public implementation classes under `ColoredMoodBar13` and depends on a
SettingHelper assembly. No stable published API or proven incoming consumer has yet been established.
Do not promise assembly/type-name substitution or source settings-key migration.

Outgoing probes include Tactical Groups' colonist drawers/bar/settings, Pawn Badge's draw postfix,
Job In Bar's draw postfix, and `DisabledPawns.Disabled.IsDisabled`. They are found by assembly/type
scanning in the original; our adapters must instead start with canonical active package identity
and validate consumed shapes. Source author compatibility claims are leads, not megamod acceptance.

No source save-game component was found in the inspected feature logic. Transient display state
must rebuild after loads and map/game changes. New save overrides belong to the shared settings
contract; exact source settings import is deliberately not included.

## Deferred and unresolved

- CM-Q1: world/caravan filtering semantics.
- CM-Q2: actual consumers and canonical optional-provider contract audit.
- CM-Q3: precise layout, overlapping icons, tooltips and accessibility at different bar scales.
- CM-Q4: provenance if any later upstream asset/helper reuse is proposed.

These have matching design questions and unchecked research tasks. All gameplay implementation,
patch-target admission, host TDD, review, localization, native workflows and performance evidence
remain outstanding. No color-bar functionality is yet replaced in the shipped scaffold.
