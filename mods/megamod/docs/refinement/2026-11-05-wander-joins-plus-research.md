# S109 — Wander Joiner Plus research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin **replacement**. Inspected **2026-11-05**. Documentation/source analysis only; no runtime,
compatibility or migration acceptance. [Change](../../../../openspec/changes/specify-megamod-wanderer-offer-details/)
and [coverage](../replacements/wander-joins-plus.md). S137 health-condition filtering is separately
queued; this investigation does not reorder that intake.

## Documentation first and exact identity

- [Workshop 2177016016 — Wander Joiner Plus](https://steamcommunity.com/sharedfiles/filedetails/?id=2177016016),
  author Ogliss. Installed About calls it **WanderJoinsPlus**, package **Ogliss.WanderJoinsPlus**,
  supported versions 1.1–1.6. The public description promises consent, a pawn summary and info card,
  including VEE's traitor wanderer without revealing the betrayal. It credits JamieWx's TI-Joins+
  as inspiration, Rabbitt's request, Chinese translation by 蛤皮的呱呱 and Russian by Tkhakiro.
- [Author repository](https://github.com/Ogliss/AskJoinWanderer), inspected main commit
  **804127b9dafe142090a183d595396ca0cc6f716b**, dated **2022-10-24T10:20:03Z**.
  Newest public source is 1.4, not the selected installed 1.6 build. Six runtime source files were
  read in full. The tree contains no license/readme; no license was found in the installed content.
  These are inspection inputs, not permission to copy source/assets. Independently implement and
  retain attribution; resolve any reuse permission separately.
- Installed root `F:/Steam/steamapps/workshop/content/294100/2177016016`. LoadFolders defines
  versions only through 1.6; native latest-older-version selection on 1.7 selects the 1.6 content
  with root content. This selection is not a claim of beta support.
- Selected `1.6/Assemblies/WandererJoinPlus.dll`: **32,768 bytes**, SHA-256
  `C6E9EC5DCFD4A92151794EDCC80B9CF483AE8895ED4CB2B1A8131B5DF8FBEA7A`,
  assembly/file version **1.0.0.0**, informational **1.0.0**, Debug configuration,
  MVID **a9369f89-229e-4fb2-9422-ea0c3a0e30bd**. All five runtime classes were decompiled/read.
  Bundled 0Harmony is **2,456,064 bytes**, SHA-256
  `353DAAFEC180BB8E7BBE4DA78F2A7CDC78067392E3A4E79DC8E7AF295F2371E6`.
  Do not bundle it in the replacement.

Ignored evidence: `artifacts/Refinement/megamod-spec-20261105/wander-joins-plus/` contains the
Workshop HTML, public tree, six raw public files (UTF-8 BOM retained), per-file public-source
manifest, selected decompilation, native/provider type excerpts and `input-manifest.json`.
The latter binds **24 inputs**: ten source-package files, eight native files and six VEE files.
Hashes identify research bytes, never runtime compatibility conditions. Original source trees and
game files were not modified, loaded as executable assemblies or copied into a product package.

## Complete selected runtime and data inventory

| Input | Code-derived behavior | Adopted intent / correction |
| --- | --- | --- |
| Main | Static Harmony PatchAll, owner com.ogliss.rimworld.mod.WandererJoinPlus; DevMode patch-count log; exact package-string HAR detection; unused VEE/AlphaAnimals flags | Replace unguarded startup with semantic admission; no diagnostic UI feature |
| GenPersonStatText | Appends backstory/trait disabled-work mask, traits and passionate skills with major marker; optional race label defaults false; assumes pawn trackers exist | Current ordinary visible facts; explicit None/unavailable; safe absent trackers; no hidden data or forced translated casing |
| ChoiceLetter_AcceptJoinerPlus | Native subclass with public joiner reference, Scribe key joiner; injects info card before the third base choice when joiner exists | Retain native letter/choices; identify insertion semantics, not ordinal; source archive only has Close so source adds no card there |
| WJP_ChoiceLetter_AcceptJoiner_Choices_Patch | Postfix helper with English about concatenation, but no HarmonyPatch attribute/registration in selected DLL | Dormant helper is not an active feature; do not port it as a second action |
| WJP_QuestNode_SendLetter_Patch | Finds all wanderer-root subclasses; resolves SendLetter_NewTemp; fallback SendLetter lookup discards the result; inherited methods can repeat | Identify/deduplicate actual routes; no broad class-name discovery or unsupported fallback claims |
| Same transpiler | Looks four instructions ahead for ReceiveLetter(Letter,string,int,bool), injects letter replacement with assumed args/casts | Semantic data-flow/cardinality checks on effective incoming code; preserve native letter rather than rebuild |
| ReplacementLetter | Generic walk-in title/text/charity, relationships, juvenile age and best skill, then stats; creates source subclass; copies accept/reject signals, quest and map; resets timeout to 60000 | Preserve original illness/context, look targets, provider fields, deadline and native identity; do not copy this replacement |
| 1.6/Patches/LetterJoinerPatch.xml | Unconditional Replace of AcceptJoiner/letterClass with WandererJoinPlus.ChoiceLetter_AcceptJoinerPlus | Do not replace a global letter class; proposed design has no XML edit |
| Four root Keyed/Incidents.xml catalogs | English, Russian, Simplified/Traditional Chinese; legacy dialog keys, some English text in Russian; no current settings | Seven owned languages, no hardcoded English suffix or unused settings port |
| About/LoadFolders/PublishedFileId; version folders | Metadata, six version DLL/Harmony pairs and version-specific XML; loadBefore Vanya.Tools.IncidentPersonStat | No source package impersonation, bundled Harmony, or inferred TI integration |

No source ModSettings, bottom tab, new Thing/Recipe, custom runtime image or dedicated cache was
found in the selected feature content. About preview is presentation metadata, not selected art.
Current ReplacementLetter does not pass raceInfo=true, so the HAR flag does not cause a current
race-summary line. Empty traits print None, while a nonempty skills list without passions leaves
an empty passion section; the owned contract corrects that presentation.

The source's reconstructed letter changes abasia into walk-in wording and omits original
lookTargets/hyperlinks/other state. This is a static risk/observable deviation supported by the
field-copy inventory, not a claim that a particular live beta combination was tested.

## Historical public code is a different generation

All six public 1.4 runtime files were inspected: Class1.cs, GenPersonStatText.cs,
WandererJoinPlus.cs, WJP_ChoiceLetter_AcceptJoiner_Choices_Patch.cs,
WJP_QuestNode_Root_WandererJoin_WalkIn_SendLetter_Patch.cs and
WJP_IncidentWorker_WandererJoin_Patch.cs. The first inserts information first, unlike the selected
DLL's third-position insertion. The letter prefix replaces the older WalkIn SendLetter, not the
current general transpiler; the Choices postfix is dormant there too.

The historical incident interceptor discovers types whose names contain WandererJoin, patches
TryExecuteWorker, excludes ManInBlack/StrangerInBlack and passes through with no colonists. It
generates its own player-faction pawn, shows a paused one-second-delayed choice dialog, spawns on
Accept, and retains rejected pawns with KeepForever. It hardcodes AA_MimeHediff for names containing
Mime and handles a pawnHediff illness. It can continue after failed entry-cell discovery and replace
provider-specific setup. None of that direct-incident class is in the selected 1.6 assembly.
Do not claim it is the current runtime or adopt its retention, full worker replacement or hidden
state fabrication. Alpha Animals was not found as an installed provider in the bounded About scan;
historical mime support is documented but not an admitted current integration.

## Native beta observations

Native **1.7.4998 rev1299**, Assembly-CSharp **15,633,920 bytes**, SHA-256
`FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B`.
Thirteen types were inspected read-only: base/WalkIn/Abasia quest roots; ChoiceLetter_AcceptJoiner;
IncidentWorker_WandererJoin; QuestPart_JoinPlayer/PawnJoinOffer/SetFaction/PawnsArrive/
GiveDiedOrDownedThoughts/DropPods; ChoiceLetter; LetterWithTimeout. Relevant Core and Royalty
quest/incident/letter XML is separately hashed. These are static facts, not finalized-Def proof.

| Native route / surface | Inspected behavior | Specification implication |
| --- | --- | --- |
| Core WandererJoin IncidentDef | IncidentWorker_GiveQuest → WandererJoins; baseChance 0.4; population IncreaseEasy | Consent already native; do not patch all direct wanderer workers or alter frequency |
| WalkIn quest | Hidden auto-accepted quest; offered nonplayer villager, adult plus difficulty-allowed children, relation weight 20; saved world pawn; 60000-tick failure delay | Preserve actual pawn, generation and one-day event |
| WalkIn acceptance/refusal | Accept → SetFaction(player), PawnsArrive, success; Reject → DeniedJoining thoughts, failure; optional Ideology charity history | Not S108's distinct rescuee outcome; no added own reward/penalty or second recruit |
| Map recovery | PawnsArrive can select another suitable player map if original map fails; letter acceptance respects map/space constraints | Preserve native recovery and avoid obsolete guaranteed-destination claims |
| Royalty WandererJoinAbasia | Royalty Def → Core assembly abasia root; baseChance 0.4; pawn gets Abasia; DropPods joinPlayer=false before decision; Accept sets faction | Preserve pod/care chronology, illness text, look target and membership-only acceptance |
| Abasia charity/context | Afflicted description and native care allowance from base AllowKilledBeforeTicks=15000; letters timeout60000 | Preserve six-hour allowance context and one-day deadline; no new sliders |
| ChoiceLetter_AcceptJoiner | Accept/Reject send their signals then remove letter; map eligibility; jump/postpone; archived Close; saves signals/overrideMap | Delegate exact choices, preserve navigation and archives |
| LetterWithTimeout / ChoiceLetter | Saves disappearAtTick; final tick auto-opens and disallows postponement; passed timeout hides letter; event may separately fail | Do not restart time on refresh/load or equate expiry with an extra Reject |
| ManInBlack | Direct IncidentWorker_WandererJoin, emergency StrangerInBlack male violent pawn | Exclude from enhancement; it is not the ordinary timed offer |
| QuestPart_PawnJoinOffer | Visitor joining uses ChoiceLetter_AcceptVisitors and its own willingness checks | Not this feature or a reason to globally replace AcceptJoiner-like UI |
| Other incidents | RefugeePodCrash is a separate quest; GameEndedWanderersJoin has a different worker | Do not conflate them with Royalty abasia or ordinary walk-in |

## Advertised VEE traitor integration: exact current reference

[Vanilla Events Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=1938420742),
package **VanillaExpanded.VEE**, installed `.../1938420742`, supports 1.5–1.7 and requires Harmony
and Vanilla Expanded Framework (`OskarPotocki.VanillaFactionsExpanded.Core`, `2023507013`).
loadFolders.xml selects root, 1.7, asset bundles and conditional NoOdyssey/Biotech folders.
Selected `1.7/Assemblies/VEE.dll`: **127,488 bytes**, SHA-256
`ACD959EF2F79176FD1E40A2D2AFAA7F4F000E2F31A8A000599F1520E09F1FBB5`.
The Workshop page states CC BY-NC-ND 4.0; this work uses it only as an inspection reference.

Four relevant classes were inspected, not the entire VEE feature inventory:

1. QuestNode_Root_WandererJoinTraitor_WalkIn overrides old GeneratePawn(), creates an adult
   violence-capable pawn, adds Traitor and passes it to world ownership.
2. RegularEvents.WandererJoinTraitor is an older direct-spawn worker retained in the DLL; it is
   **not** the selected 1.7 IncidentDef worker.
3. HediffComp_Traitor tracks saved ticksToDisappear/isGroup/forcedFaction. While spawned, a
   250-tick check decrements its timer and eventually changes allegiance/assault behavior or
   notifies the group event. Its debug action is not an adopted UI feature.
4. HediffCompPropreties_Traitor supplies the timer range and comp class.

Selected 1.7 Defs: VEE_WandererJoinsTraitor uses IncidentWorker_GiveQuest and its corresponding
hidden, auto-accepted quest. Static incident values are baseChance0.1, earliestDay15,
minGreatestPopulation3 and minRefireDays60; these are preserved provider values, not copied owned
balance. The Traitor Hediff has becomeVisible=false and a 180000–1800000 tick range (3–30 game
days of eligible ticking). Do not expose its label/description, timer or special state in summaries.

**Unresolved dispatch, WJP-Q2:** Native base RunInt calls GeneratePawn_NewTemp(Map); native WalkIn
overrides that method directly. The inspected VEE subclass overrides only GeneratePawn(). In the
unpatched static call path, its traitor setup is bypassed. This is evidence of a required effective
dispatch check, not a live compatibility verdict: a foreign bridge has not been established.
Do not add hidden conditions to repair the external provider or claim its betrayal works merely
because the classes/Defs exist. Preserve the original event untouched if this route cannot be admitted.
The owned fallback must be visually uniform across comparable Core and concealed walk-in offers:
missing added controls only on a traitor offer would reveal it. If active VEE support fails admission
or later becomes invalid, both use native presentation, including already-pending/reopened/reloaded
offers; distinct abasia enhancement remains independent. This is an intentional owned privacy
correction, with a comparison scenario and task, not an observed source behavior.

## Contracts, composition and exclusions

A bounded raw scan of installed Workshop and local Mods `*.xml`, `*.cs`, `*.dll` for
Ogliss.WanderJoinsPlus / WandererJoinPlus found only its own About, six DLLs and four XML patches.
No external consumer was found; this does not exclude constructed reflection, UTF-16 strings,
archives, remote code or future consumers. The loadBefore TI package is metadata, not demonstrated
consumer support. No speculative compatibility facade is justified.

The source's assembly/type **WandererJoinPlus.ChoiceLetter_AcceptJoinerPlus**, saved **joiner**
reference and inherited native fields remain a real persistence contract even without another
mod consuming them. Archived letters matter. Migration is not specified: provider replacement in
such saves is unsupported; resolving active offers alone does not remove archived type bindings.
No automatic conversion or arbitrary uninstall-safety claim.

S108 can share visible formatting only; consent timing and native penalty semantics stay separate.
No private cache/index, regeneration or permanent rejected-pawn retention is needed. The active
original source owns overlap; disable owned enhancement with a startup explanation. Optional
providers keep their own generation/quest rules. The source global XML class replacement is omitted;
any future XML mutation must first satisfy XML Extensions dependency/content/shape/merge rules.

## Draft balance and playability review

Comparators: Core ordinary walk-in, Royalty abasia and VEE traitor; emergency ManInBlack is an
explicit exclusion. Preserve arrival frequencies, offered-pawn generation, native map recovery,
one-day deadlines, medical work, refusal/charity consequences and hidden risk. Information improves
the player's choice intentionally; it must not detect betrayal, cure abasia, increase joins or erase
costs. No new research, item cost, recipe, setting or progression unlock. Finalized Def and actual
player-workflow evidence remain pending; this static review cannot accept gameplay or compatibility.

## Open work

| ID | Topic | Remaining work |
| --- | --- | --- |
| WJP-Q1 | Effective targets | Exact letter binding/presentation/persistence, semantic guards, independent groups and later repatch behavior |
| WJP-Q2 | Optional/external contracts | VEE effective generator/letter route; current source/license and actual consumer evidence; historical mime remains unadmitted |
| WJP-Q3 | Lifecycle and presentation | Minimal saved association, archives/stale identity, long translations and native card return; no legacy migration selected |
| WJP-Q4 | Implementation and acceptance | Honest focused TDD, reviewed guards, finalized semantic review and native Core/Royalty/admitted-provider actions/results |

These are retained research and implementation questions. No user interview or new runtime work
is implied. Independent document review and strict validation are tracked in the sequential ledger.
