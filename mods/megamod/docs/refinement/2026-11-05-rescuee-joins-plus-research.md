# S108 — RescueeJoinsPlus research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement S108/P2-38. Documentation-first research on2026-11-05; no runtime acceptance.
[Proposal](../../../../openspec/changes/specify-megamod-rescuee-join-consent/proposal.md),
[design](../../../../openspec/changes/specify-megamod-rescuee-join-consent/design.md),
[spec](../../../../openspec/changes/specify-megamod-rescuee-join-consent/specs/megamod-rescuee-join-consent/spec.md),
[coverage](../replacements/rescuee-joins-plus.md).

## Identity and evidence

- Installed title **RescueeJoinsPlus**, package **Ogliss.RescueeJoinsPlus**, author **Ogliss**;
  [Workshop2177170403](https://steamcommunity.com/sharedfiles/filedetails/?id=2177170403) currently
  titles it **Rescuee Joiner Plus**. The public description promises permission before joining,
  a person summary and information-card access; it does not define timing/eligibility/quest effects.
- Installed root `F:/Steam/steamapps/workshop/content/294100/2177170403`. About supports1.1–1.6,
  not1.7. LoadFolders has root+version entries1.2–1.6; native1.7 chooses latest defined older entry
  1.6, with precedence1.6 then root. This resolves inspection inputs, not source beta compatibility.
- Selected `1.6/Assemblies/RescueeJoinPlus.dll`: Assembly/File1.0.0.0, informational1.0.0, Debug,
  MVID **bb2b81cd-9bdf-42db-b0ac-3f2ed4a325d8**, 28672bytes,
  SHA256 **FAC01E272076CEFE79353B671C935173BAA75A123E1C30AF5B8D9F31C5820169**.
  Read all four runtime decompiled classes and assembly metadata.
- Source bundles `1.6/Assemblies/0Harmony.dll` (2456064bytes,
  SHA256 **353DAAFEC180BB8E7BBE4DA78F2A7CDC78067392E3A4E79DC8E7AF295F2371E6**)
  and declares no external dependency in About. Our implementation requires external Harmony;
  neither source assembly is selected for copying.
- Author repository [AskJoinRescuee](https://github.com/Ogliss/AskJoinRescuee), inspected `main`
  commit **4acb2f12a32bed4a32bfd9d5c4c28265fc757708** dated2022-10-24. Its newest version is1.4;
  all four runtime `.cs` files under `1.4/Source/JoinsPlus/JoinsPlus/` were read and compared with
  the installed DLL. They corroborate the behaviors below but are **not its verified build revision**.
  Historical repository UpdateFeatureDefs/old build artifacts are not installed current content.
- No license/README found in the inspected author tree or installed package. Credit Ogliss, retain
  behavioral provenance and independently author code/strings; no copied-code/art permission inferred.
- Native **1.7.4998 rev1299**, Assembly-CSharp SHA256
  **FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B**.
  Offline decompilation only, no assembly execution/game launch. Relevant portions of17 successful
  native type extracts were inspected. Three wrong-namespace/missing-type attempts are empty files,
  not evidence of additional successful extracts.

Ignored local evidence: `artifacts/Refinement/megamod-spec-20261105/rescuee-joins-plus/` contains
public HTML, public tree/revision and four source files, selected binary/native decompilation,
eight installed-input hashes, two native-input hashes and four public-source snapshot hashes. Public
source snapshots contain decoded UTF-8 text without the remote BOM; their hashes identify those local
normalized files, not byte-identical GitHub downloads. The public manifest also records raw download
hashes/sizes and that normalization. Source package
and game installation remained read-only. No source Preview.png was selected as product artwork.

## Complete selected runtime inventory

| Source file | Inspected behavior and limitation |
| --- | --- |
| Main | Static startup, unguarded PatchAll with owner `com.ogliss.rimworld.mod.RescueeJoinPlus`; optional developer log counts owned patches; exact case-sensitive HAR package-facing string sets a summary boolean |
| GenPersonStatText | Full name, lowercase story title, biological age, optional race label only when HAR flag true, gender, relations; backstory/trait disabled-work mask, all traits, positive passions with Major suffix; size16 headings |
| RJP_Pawn_GuestTracker_Notify_PawnUndowned_RescueeJoin_Patch | Matches short unsigned comparison branches, replaces joining block until short branch; AskToJoin returns godMode OR A<B; Accept directly SetFaction(player) and sends letter; Reject emits neutral message |
| RJP_Pawn_MindState_JoinColonyBecauseRescuedBy_RescueeJoin_Patch | Replaces matching three-argument DoRecruit with dialog callback; Accept calls DoRecruit later; Reject only assigns unused static text; rest of original method continues immediately |

Both dialogs use native Dialog_NodeTree with delayed interactivity, radio ambience and forcePause;
More info opens Dialog_InfoCard, Accept/Reject resolve the tree. No ModSettings class, bottom tab,
key binding, XML patch, new content Def, custom saved object or source request persistence exists.
Native dialog supplies one-second real-time delay and no close-on-Enter/Escape.

Observed defects/limits are bounded static findings, not live reproductions:

- Both titles dereference `pawn.Map.Parent.Label`; native recovery admits a pawn held by a spawned
  carrier whose direct Map need not exist. Public carried-recovery comments are reports, not our
  runtime proof. Use held context and validate identity.
- Guest acceptance passes the full joining text as letter label and label plus details as body;
  retain correct title/body roles in the replacement. Guest Reject does not mark the native
  consumed-refusal flag, unlike the native nonjoining branch, so repetition needs explicit treatment.
- Promise code computes unused static label/text and changes only that text on rejection; it does
  not emit its rejection message. Delaying DoRecruit alone does not delay native following memories
  and rescue-finished letter. It cannot prove a successful quest join at dialog creation.
- Nonempty skills with zero passions produce an empty section; story/skills/name assumptions are
  unguarded. English hardcodes male pronouns. Three installed languages: English, ChineseSimplified,
  ChineseTraditional; both Chinese files omit the rejection-message key. No full seven-language UI.
- Branch-size matching has no semantic/cardinality/effective-patch check. No exact source API or
  lifecycle protection is inferred from a startup patch count.

## Current native routes and consequences

`Pawn_GuestTracker.Notify_PawnUndowned` checks humanlike, player host or wild man in player bed,
nonprisoner, spawned self/parent and !leftAfterRescue. It grants the flagged Rescued memory when mood
exists. Factionless or rescueesCanJoin factions get a seeded decision:0.5 if outdoor temperature is
safe and no toxic fallout/active Biotech noxious haze, otherwise1.0. Positive branch SetFaction plus
positive letter; negative branch neutral message and leftAfterRescue=true. Final cleanup clears
the rescue-thought flag. leftAfterRescue is saved and has no reset in the inspected guest class.
The owned feature preserves that opportunity policy; rejection consumes it without fabricated rerolls.

`Pawn_MindState.JoinColonyBecauseRescuedBy` clears WillJoinColonyIfRescued, checks current eligibility,
then DoRecruit(false), two rescue memories and a rescue-finished letter, with a caravan text addition
unless the map came from gravship landing. Its eligibility rejects already-player pawns, prisoners
of nonhostile hosts, and conscious nonprisoner hostiles. This is a distinct route from ordinary guest
recovery, including native rescue promises to prisoners of hostile captors.

`FloatMenuOptionProvider_OfferHelp` exposes Offer help or Free prisoner for a living flagged target,
requires a reachable touch path, and schedules JobDriver_OfferHelp. That job rechecks the flag, walks
to touch and calls the promise method. This is an actual player acceptance fixture; do not replace
it with raw invocation of the method. Ordinary physical rescue instead follows JobDriver_TakeToBed,
Toils_Bed and RestUtility.TuckIntoBed, with relations Notify_RescuedBy. The current tuck path does not
itself call JoinColonyBecauseRescuedBy. CarryDownedPawn and TakeToBed have different guest/child checks;
do not copy either as a generic eligibility override.

`InteractionWorker_RecruitAttempt.DoRecruit` calls RecruitUtility, with recruiter faction or player
fallback. RecruitUtility unlocks apparel, handles replaceOnRecruited royal titles, clears guest state,
sets faction and notifies recruitment. Humanlike recruitment can record tale/PrisonersRecruited,
RecruitedMe memory and Recruited quest signals; audiovisual false suppresses the ordinary success
letter but not those side effects. Our deferred acceptance must preserve actual actor/context rather
than substituting an absent actor and accidentally changing credit or faction. Apparel/title/guest
changes precede faction change: a failure while still unjoined is not proof that nothing happened.
The owned contract distinguishes pristine preflight failure from any entered partial continuation.

Native exit job generation respects downed state, map exit/portal possibilities and reachable routes.
Refusal does not guarantee an immediate exit. We do not change the think-tree's eligibility or force
an exit job. Source has no explicit refusal mood/goodwill penalty; independent quest/ideology rules
remain in force and require scenario-specific runtime evidence.

## Contracts, composition and selected deviations

Bounded `rg -a -l -i` scan of installed Workshop and local Mods `.xml/.cs/.dll` for source package
and assembly/namespace name found only its own About and six version binaries. No real external API
consumer was found; this cannot rule out constructed/UTF16 reflection, archives or remote callers.
Public GenPersonStatText methods, dialog helpers and Main.AlienRaces_Active do not justify speculative
facades. Do not spoof package/assembly identity. There is no source custom saved state to import.

Select both joining decisions and the summary/info card, correcting premature completion, letter
roles, refusal feedback/consumption, missing data, carried context and repeated calls. Add explicit
saved request identity and safe stale/failure handling; these are selected robustness requirements,
not claims about upstream parity. Preserve native willingness instead of source god-mode override.
Use current visible native restrictions and hide unrevealed information. Keep the native contextual
paused UI with no new settings/tab; share presentation directly with S109 only after its own research.

S058 changes rescue ordering, not eligibility; S031 child care stays authoritative. S109 wanderer
events are a separate source, not inferred here. Other ritual/visitor recruitment is outside this
feature's scope. Original provider suppresses owned overlap. HAR remains optional; data-driven
kind/race text replaces its boolean dependency. New patches require per-patch effective target specs,
coherent group admission, owned rollback/revalidation and bounded startup warnings. No XML changes.

## Balance and playability comparison

| Comparator | Acquisition/work/risk | Consequences preserved / intended change |
| --- | --- | --- |
| Native recovering guest | Actual rescue/care; seeded50% safe or100% hazardous willingness | Preserve chance/eligibility/gratitude; add player veto before automatic faction change |
| Native Offer help/Free prisoner | Reach target at rescue site; native promise/captor/faction conditions | Preserve rescue recruitment and quest outcomes on Accept; Reject grants no recruitment rewards |
| Native ordinary prisoner recruitment | Existing warden/resistance/interaction process | Outside this feature; no bypass, free recruitment or extra consent dialog |

No new cost, chance slider, item, research or crafting economy is introduced. One-second UI delay
is source/native behavior. Pending state pauses rather than providing timed labor or a retry loop.
Final playability review must observe real rescue risks/care, refusal with a blocked exit, actual
quest outcomes/credit and no repeat-reward exploit on the reviewed build. These static comparisons
are not finalized modded-context or gameplay acceptance claims.

## Open work

| ID | Remaining work |
| --- | --- |
| RJP-Q1 | Effective native recovery/promise continuation targets, caller/tail semantics and foreign patch admission |
| RJP-Q2 | Saved pending/resolved opportunity identity, duplicate routes, stale decisions and partial-callback reconciliation |
| RJP-Q3 | Optional race/quest information, visible data, actual consumers and source license scope |
| RJP-Q4 | Focused implementation/TDD, seven-language UI, finalized playability and native causal acceptance |

Independent behavior review's two P2 findings were resolved with explicit partial-effects and
competing-route/reload scenarios; focused re-review found no further issues. Provenance review
verified all14 retained local hash/size pairs, metadata, four runtime re-decompilations, five key
native re-decompilations and the bounded consumer/public-source claims. Its BOM clarification is
recorded above and in the public manifest. Strict validation passes130 items, zero failed; this
does not accept runtime behavior. The ledger retains review details and remaining work.

All implementation tasks remain open. No runtime changes/tests/build/install/game launch, normal
save/configuration writes, Workshop mutation, asset selection, commit or publication occurred.
