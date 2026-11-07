# S101 — Monument Highlight research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Replacement S101/P2-31; inspected2026-11-05. Documentation/source-decompilation/native analysis only;
no gameplay, asset selection, runtime implementation or compatibility acceptance.

## Public documentation first

[Author Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3018715974)
describes monument quest names and remaining build/keep/protect time beside the structure. It credits
Falconne's Labels on Floor routines while saying that mod is not required. Author replies describe
fixes for world/other-map leakage and font display problems; these are historical reports, not
reproduced defects or accepted1.7 compatibility. About/Workshop advertise1.4–1.6.
Targeted public searches did not establish an author source repository or reuse license. No license
file or C# source is shipped in the inspected package. Do not infer asset/code rights from attribution.

## Identity and inspection evidence

| Item | Identity |
| --- | --- |
| Source | Monument Highlight, Espio Idsavant, `espio.monhighlight`, Workshop3018715974 |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/3018715974` |
| Selected assembly | `1.6/Assemblies/monhighlight.dll`, assembly0.0.0.0; no file version attribute in generated assembly metadata |
| DLL SHA256 | `412EF2942FBCF9A8C37E98BD69AB9D89B3ABB0370C04719D4CC393842CE33B2E` |
| About SHA256 | `C86EFD9F7BF67BFA6F6A2A6017B5C38BD45045F410F97F0E130137540E2EE33A` |
| English keys SHA256 | `F9DFA7A4A691D36A8918C64D2A6433E3EC5B9C4EAD9E1B0725ED24877A11E6AA` |
| Native | RimWorld1.7.4998 rev1299; Assembly-CSharp SHA256 `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |
| Native BuildMonument worker XML SHA256 | `FCDB33B0256A94176765CBCA17880BC1646783BAFF658ECAAAD1AF455C824983` |
| Native ChangeRoyalHeir XML SHA256 | `F8ACBD1496F7B95E9EACF575E6408BB6DB073DDA28F28F1F12B657DA636B7E41` |
| Native decree XML SHA256 | `C32572C7F0EC33CF8E1B5DCA96FEA77BF0E11EA164A3E4927D9F20FF6C482A12` |
| Native decree utility XML SHA256 | `4F5F66FCB2F1E93B57389CCBE6B8C407678FD1E0A4F2679D718B9B4664040C69` |
| Ignored local evidence | `artifacts/Refinement/megamod-spec-20261105/monument-highlight/{binary,native}` |

There is no LoadFolders.xml or1.7 directory. Inspected native ModContentPack fallback selects1.6
plus root, including root textures/language. The folder includes bundled0Harmony but selected
monhighlight.dll references Assembly-CSharp and UnityEngine.CoreModule, not Harmony. No Harmony
patches, ModSettings subclass, Mod entry class, Def/XML patch set or source save fields were found
in its ten type files. This does not make that bundle appropriate for redistribution.

Read-only ILSpy inspection covered GameComponent_MonHighlight, empty MapComponent_MonHighlight,
LabelHolder, LabelDrawer, MeshHandler, FontHandler, Resources, Label, PlacementData and
CharBoundsInTexture, plus selected English keys. No external binary was built or executed.

## Reconstructed source behavior

| Area | Selected binary behavior |
| --- | --- |
| Refresh | Static font/holder/mesh/drawer fields on GameComponent. FinalizeInit schedules buildLabels; GameComponentTick rebuilds only on ticks divisible by2000. |
| Candidates | Clear labels, visit every Find.Maps entry and listerThings.ThingsOfDef(MonumentMarker). Cast each marker, read questTags[0], regex first digits, int.Parse as quest ID, then scan all quests. No guards for empty/malformed tags, sketch or missing construction delay. |
| Quest name | Match quest.id, take quest.name, uppercase it. No ongoing/hidden/accepted/history filter. No association through actual look/select targets. |
| Delay selection | FindLast QuestPart_Delay whose inSignalEnable contains .Initiate for Build, .MonumentCompleted for Protect, .OuterNodeComplete for Keep. Marker.complete chooses phase; Protect wins over Keep if any matching part exists. Does not inspect State or objective target. |
| Time | EnableTick + delayTicks minus TicksGame, ToStringTicksToPeriod(allowSeconds:false,shortForm:false,canUseDecimals:false,allowYears:false). English Build/Protect/Keep wrappers, then uppercase. No due/unknown boundary handling. |
| Position | Quest title starts at marker position + sketch minX/maxZ+1; time at minX/minZ-1. AltitudeLayer.Item is cast to int in IntVec3; Draw adds x+.2 and uses identity rotation. Reads a LowPlant altitude but never uses it. |
| Scale | (maxX-minX-.4)/string.Length, clamp.5–1.2 uniformly. Off-by-one footprint width and minimum-scale overflow can matter; zero-length text has null mesh. Logs every scale calculation. |
| Drawing | GameComponentUpdate skips WorldRendererUtility.WorldRendered and missing camera. LabelDrawer checks current map equality and viewport containing startAt only. Label.IsValid is not called. No fog, complete text-bound, collision or zoom gate. |
| Text resources | Resources loads MonHighlight/Consolas.png. FontHandler assumes35-pixel-wide cells, maps ASCII-like characters, maps unsupported glyphs to index0; white MaterialPool transparent material alpha.5. No Unicode fallback. |
| Mesh resources | MeshHandler caches by entire text forever in a static-reachable dictionary, no eviction/disposal or game/map reset. Clearing label list does not clear meshes. Every distinct countdown can add a mesh. |
| User controls/state | No settings, hotkey, toggle, editor, bottom tab, saved label records or custom quest state. Three English keyed strings only. Unused PlacementData and LabelHolder._dirty do not establish extra behavior. |

Potential failure paths and rendering defects above are statically visible risks, not reproduced
in-game claims. Owned corrections are specified explicitly rather than assumed already fixed.

## Native association and timing evidence

Inspected native classes: MonumentMarker, Quest, QuestPartActivable, QuestPart_Delay,
QuestPart_DropMonumentMarkerCopy, QuestNode_GenerateMonumentMarker, QuestNode_Delay, QuestUtility,
QuestNode_SituationalThought and QuestPart_SituationalThought. The native GenDate period formatter
was also inspected read-only: with source flags it rounds whole hours/days; the explicit under-hour
cue is an owned correction to potential zero-hour presentation.
Metadata/folder selection also uses the native ModMetaData/ModContentPack inspection retained under
the preceding Snap Out research with the same native binary identity.

- MonumentMarker.CustomRectForSelector uses sketch.OccupiedRect.MovedBy(Position). SpawnSetup rotates
  the sketch unless respawningAfterLoad, and destroys the marker if CheckRoyalty fails. Completion
  checks every177ticks; complete/destroyed changes emit exact target signals from questTags.
- GetInspectString obtains a quest through QuestLookTargets and appends native quest-part inspect
  strings. QuestUtility's inspect helper filters QuestState.Ongoing and enabled activable parts.
  Its related-quest gizmo filters hidden/historical/dismissed quests and looks at look/select targets.
- QuestPartActivable.EnableTick is-1 unless enabled. QuestPart_Delay.TicksLeft is0 unless enabled,
  otherwise enableTick+delayTicks-currentTick. waitUntilPlayerHasHomeMap can postpone actual completion;
  it does not automatically prove a simple suspended-time counter. Quest ticks only enabled parts.
- Copy-marker quest parts deep-copy the sketch and **copy all original quest tags** into the new
  marker, but existing timer inspectStringTargets can still refer to the original marker. Copy
  itself is a quest look target once created. A raw object-reference-only join is insufficient.
- QuestNode_Delay uses a supplied hardcoded enable signal or the current slate signal, explaining
  nested sequence enable names. A mere signal substring does not establish obligation meaning.

Inspected native Royalty quest scripts:

| Quest path | Actual obligation / static comparator |
| --- | --- |
| BuildMonument/Script_BuildMonument_Worker.xml | Construction timeout10–16days before points scaling (curve800→1,2000→2,4000→3); disabled by marker.MonumentCompleted, has no inspectStringTargets. Source comment8–14days is stale; expression is10–16. |
| BuildMonument_Root_Basic.xml + worker | Reward on construction; ordinary non-permanent-enemy asker can require Keep; nested delay inherits its enable signal, explicitly targets marker for inspect text. Some branches end on construction with no retention. |
| BuildMonument_Root_TimeProtect.xml + worker | Protect after MonumentCompleted, target marker,20–40/41–60/61–80days by challenge tier; violation triggers threat/failure. Reward on construction is separate from final quest end. |
| Decree/Scripts_Decree.xml, Decree_BuildMonument |15day construction deadline disabled by DecreeCompleted; completion enables DecreeUnmet rather than ending the quest. After actual construction, a Keep delay explicitly enables on MonumentCompleted. Source substring logic misclassifies this Keep as Protect. |
| Decree/Scripts_Decree_Utility.xml, DecreeSetup |A separate80day timer ends an unfulfilled decree as forgotten/failure; also disabled by DecreeCompleted. The shared signal alone cannot distinguish this expiry from the15day penalty deadline. |
| Script_ChangeRoyalHeir.xml |20day construction timeout targeting marker and disabled on MonumentCompleted; post-completion Keep is a nested delay with marker inspect target. |
| Utility/Scripts_Utility.xml, Util_Constants_Monuments |keepMonumentTicks=15days and goodwillChangeIfMonumentDestroyed=-10; these are native comparators, not owned balance edits. |

No generated live quest instances were inspected. Exact runtime signal/part association and
third-party timing subclasses remain MHL-Q1 before implementation acceptance. The supported four
workflows cannot be reduced to the easiest case merely because fallback is safer.

Independent provenance review identified the omitted overdue-decree state. The draft now shows
Build: overdue after the15day penalty deadline with a separately identified active80day forgotten
countdown. Late construction switches to Keep; actual forgetting removes the ended quest label.
Completed construction-delay state is not used as a running timer, and the80day timer is not
substituted as a renewed Build deadline. This requires matching the generated effect chain, not
hardcoding15/80 or relying only on DecreeCompleted. Native thought penalties remain untouched.

## Composition, consumer and rights audit

Bounded installed ordinary-string scan of `.dll`, `.cs`, `.xml` under Workshop and local Mods for
espio.monhighlight/monhighlight/MonHigh_Build/MonHigh_Protect found no matches outside the source
package. This is not a guarantee against UTF-16/reflection encodings, assets, uninstalled mods or
future consumers. Public classes alone justify no compatibility facade.

Source credits old Labels on Floor routines; our selected S093 continued provider has a separately
specified label/resource design. Share owned text metrics, Unicode fallback, material/mesh lifetime
and map presentation infrastructure directly. Do not copy source caches or make either feature a
runtime library dependency. Monument visibility is independent of floor-label mode; it needs only
native quest/marker facts, not a private hauling/search index.

No code or atlas reuse license was established. Preserve attribution in research and independently
implement; choose licensed/native/shared resources only during later implementation under the
applicable asset process. Source texture existence is not visual acceptance or a reuse grant.

## Intentional owned changes and semantic review

Actual ongoing associations and enabled timers replace first-tag digits and last-substring guessing.
Names preserve case; due/less-than-hour/unknown/untimed cues prevent false obligations. History and
map teardown remove labels, paused state changes refresh immediately, and malformed one-marker
content cannot break all labels. In-bounds measured placement and readable glyph fallback replace
source ASCII/minimum-scale overflow, with bounded resource lifetime and no log-per-scale spam.
Source title/time north/south preference, white50% appearance and.5–1.2scale are retained where safe.

The added bottom-right view toggle follows the user's view-control rule. Its sole actual setting
uses common global/save/Use global handling; no per-monument editor or extra appearance menu is
invented. Original active suppresses owned overlap. Royalty is required for this native feature but
optional for the whole megamod; no bundled Harmony is retained for an unused reference.

Static balance comparison covers basic construction, threat protection, decree and heir retention;
none of their duration, resource, work, reward, goodwill or destruction consequences is changed.
Reading/hiding labels is not permission to demolish. Finalized active-mod and causal native action
evidence, actual glyph/zoom rendering and measured combined label costs remain unperformed.

## Open questions and status

- **MHL-Q1:** exact generated native/objective/tag/copy/timer contracts and executable semantic admission.
- **MHL-Q2:** shared renderer/resources, measured bounds/language/fog/edge/collision/zoom behavior and cost.
- **MHL-Q3:** common settings/provider lifecycle, focused RED/GREEN and final reviewed native evidence.

[Change](../../../../openspec/changes/specify-megamod-monument-labels/),
[coverage](../replacements/monument-highlight.md), [ledger](specification-progress.md).
Independent review/strict validation recorded in the ledger when complete. All runtime tasks remain open.
