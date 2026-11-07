# S044 — Auto Name Babies research

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Inspected **2026-11-03**, origin replacement, handwritten **P1-35**. Source/native research and
observable draft independently reviewed and validated; not a live reproduction or implementation.
Adopted behavior belongs to the separate
[change](../../../../openspec/changes/specify-megamod-auto-name-babies/proposal.md).

## Documentation first and selected build

The [Workshop item](https://steamcommunity.com/sharedfiles/filedetails/?id=3782676886) could not be
opened through the web reader, including English/German variants. Primary Steam collection search
results expose the author's short automatic-birth-naming description; the complete installed
About description supplies the same intent: retain the birth letter, skip the naming dialog, use
the native Randomize generator and choose parental or optional Ideology surnames. Third-party
automatically summarized descriptions were not used as behavior evidence.

Installed root: `F:/Steam/steamapps/workshop/content/294100/3782676886`.

| Item | Evidence |
| --- | --- |
| Identity | Auto Name Babies, Cruesoe, `cruesoe.autonamebabies`, Workshop 3782676886 |
| About | modVersion 261016; supports 1.6 and 1.7; Biotech and Harmony required, Ideology optional |
| Selection | `1.7/Assemblies/AutoNameBabies.dll`; assembly version 0.0.0.0; no LoadFolders override found |
| Content | Separate 1.6/1.7 DLLs, About/preview, one English keyed catalog; no included C# source, Defs or XML patches |
| License | Included MIT license, copyright 2026 Cruesoe; preserve notice if code is ever reused; no code/assets/DLL copied |
| Source UI | One global mod-settings radio group, no bottom tab, pawn gizmo, debug action or key binding |

| File | SHA-256 |
| --- | --- |
| Selected 1.7 DLL | `27EC375F26BA8E315F8AED6556DF81FE21E184C494EADD905071659216F63CFA` |
| About | `376254D8BF58CEF5DBAE7BB33F668A23F0F34C4C4638825E6F26B85440B60509` |
| Native Assembly-CSharp | `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |

ILSpy 11.2.0.10050 decompiled the complete selected DLL to ignored local evidence
`artifacts/Refinement/megamod-spec-20261103/auto-name-babies/compiled`. All eleven types were read:
mod, settings, enum, startup, BabyNamer and six patches. Native evidence in sibling `native` is
from **1.7.4995 rev309**, not the older repository decompilation used only to locate symbols.

## Complete installed behavior

`SurnameMode` is Father=0, Mother=1, Random=2, Ideology=3. The only source setting is global
`surnameMode`, default Random. Stored Ideology remains stored when the DLC is absent but resolves
to Random; its radio button is disabled. Settings take effect for subsequent naming, not a sweep
of previously named babies. No per-save settings or own saved pawn history exists.

`IsTemporarilyNamed` tests only `NameTriple.First == "Baby".Translate().CapitalizeFirst()` in the
**current** language. `TryName` additionally needs Biotech, a humanlike pawn, Baby developmental
stage and that placeholder; it does not itself check faction or whether the baby is dead.
It chooses a surname, falling back to the old surname, then calls native
`PawnBioAndNameGenerator.GeneratePawnName(Full, chosenLast, false, pawn.genes?.Xenotype)`.
A returned NameTriple contributes First and an explicitly set Nick; the source forces the chosen
Last even if the generator returned a different one. A NameSingle contributes its Name as First
with no nickname and the chosen Last. Unsupported/empty generator results return failure; exceptions
are logged with the intended fallback to vanilla naming controls. Native naming deadline is not reset.

### Surname rules and discrepancies

| Mode / path | Actual selected DLL behavior |
| --- | --- |
| Father | First nonempty surname: father, genetic mother, birth parent |
| Mother | First nonempty surname: genetic mother, birth parent, father |
| Random at new birth | Prefix allows native `RandomLastName`; native randomly chooses a parent entry, including a distinct birth parent; equal surname strings are **not** deduplicated |
| Random/Ideology at final naming or load | Preserve an existing nonempty baby surname instead of rerolling it |
| Random fallback when no prepared surname | Uniform choice among distinct nonempty surname strings of genetic mother/father/distinct birth parent |
| No usable parental surname | Preserve existing baby surname; if also absent, source still forces that empty/null surname after generating a name |

`PawnNamingUtility.GetLastName` handles NameTriple and NameSingle; unsupported parent name forms are
treated as unavailable. Mother tooltip incorrectly says father before birth parent. Ideology tooltip
overstates deterministic usually-man/woman behavior; the implementation uses native marriage rolls.

Ideology chooses the genetic mother's ideology (birth parent if no genetic mother), or the father's.
If those two candidates both have different ideologies, each has **50%** chance. Otherwise use the
primary candidate's ideology, then father's, then birth parent's; missing ideology falls back to
random distinct parental surnames. Read the first MarriageName precept:

- Always man's/woman's selects that native marriage-role parent's surname, then the other role,
  then birth parent. With no usable pair, man's falls back father→mother→birth; woman's mother→birth→father.
- Keep names selects random distinct parental surnames.
- Usually man's, usually woman's and random use native `Roll_NameChangeOnMarriage`; no-change results
  select random distinct parental surnames. Unknown/missing precept also selects random distinct surnames.
- The pair is genetic mother (or birth parent) and father; a missing/identical father may be replaced
  by a distinct birth parent. Native `DetermineManAndWomanSpouses` uses actual male/female roles for
  different genders and lower pawn ID as the “man” for equal genders. It is not invariably father/mother.

Native marriage rolls choose uniformly among willingness-approved man-name, woman-name and no-change
options, defaulting to no change when none are allowed. The inspected Ideology XML includes AlwaysMans,
Random, KeepNames, UsuallyWomans and AlwaysWomans; no UsuallyMans Def was found there. UsuallyWomans
rejects man's-name with chance .95 and keep-name with chance .75 before native selection. These are
**not** fixed final surname probabilities. The owned contract follows supported native marriage
rules rather than inventing a new hardcoded distribution or assuming every precept exists.

### All six patch paths

Startup calls unguarded Harmony PatchAll with owner `cruesoe.autonamebabies`.

| Patch | Source action and boundary |
| --- | --- |
| PregnancyUtility.RandomLastName prefix | Random lets native run; other modes return a chosen nonempty surname and skip original. No player-faction gate, so foreign births can be affected. Exceptions fall back with ErrorOnce |
| PregnancyUtility.ApplyBirthOutcome postfix | Extract Pawn or Corpse.InnerPawn; if player faction, TryName, including stillbirth and letter-suppressed births |
| ChoiceLetter_BabyBirth.Start postfix | Reflect private pawn; for player pawn, if already non-placeholder or successful TryName, replace known naming paragraphs with the automatic-name sentence |
| ChoiceLetter_BabyBirth.Choices postfix | If player and not literal placeholder, remove options by translated NameBaby/Postpone captions, replace Jump by plain native Jump, retain others and append Close if none |
| ChoiceLetter_BabyBirth.ShouldAutomaticallyOpenLetter postfix | Same player/non-placeholder condition forces false |
| Game.FinalizeInit postfix | Snapshot AllMapsWorldAndTemporary_AliveOrDead, name player Baby-stage literal placeholders; no live-map-only restriction, includes dead and temporary pawns |

Letter text handling resolves the whole text, temporarily restores the previous pawn name in a
try/finally to construct expected native translated fragments, then does exact newline/string
replacement for live birth, stillbirth and adoption paragraphs. Unknown paragraphs remain unchanged.
No successful-assignment marker protects button suppression or already-named adoption notices.

## Exact native lifecycle and observable implications

- `GiveAppropriateBioAndNameTo` first generates a normal name, then replaces the First with translated
  Baby and clears Nick for player-faction Baby-stage NameTriple pawns. Foreign babies keep generated
  first names. This is a stronger new-baby provenance seam than later spelling checks.
- `PawnGenerator.GenerateRandomAge` gives Newborn stage a **60,000-tick** naming deadline. Ordinary
  native naming controls permit First/Nick/Last until that deadline (or debug mode); later controls
  permit Nick/Title only. Accepting a native naming dialog sets deadline to **-1**, including when
  the chosen first name deliberately is Baby. Source ignores this useful distinction on load.
- `ApplyBirthOutcome` takes a pawn or growth vat as birther, uses its faction, chooses surname and
  generates the newborn; parent/genetic details are populated along the birth path. Stillbirth sets
  the deadline to current tick+1 and returns a corpse. Birth health, parent/surrogacy, maternal death,
  inbreeding and naming paragraphs are assembled before `Start`/ReceiveLetter. Letter Start therefore
  runs before the source's ApplyBirthOutcome postfix. Some callers suppress the letter entirely.
- Native `RandomLastName` includes genetic mother, father and a birth parent distinct by pawn identity;
  two equal surnames from different parents retain two chances. It returns null with no usable names.
- `Designator_Adopt.DesignateThing` transfers faction, performs ordinary bed assignment, builds an
  adoption letter with a naming paragraph while the deadline is still open, calls Start and receives
  it. An already genuinely named adoptee is not an unnamed newborn; the source nevertheless changes
  its letter because it checks only “not Baby.” No new adoption eligibility is supplied.
- `ChoiceLetter_BabyBirth` displays in the letter stack only for a player pawn with deadline ≥0.
  Setting deadline=-1 merely to “complete” automatic naming would therefore hide the birth notice.
  Keep the native deadline for source-parity voluntary renaming, while separately knowing automatic
  naming succeeded. Native timeout/control lifecycle does not justify renaming on expiry.
- Native Choices distinguish archived, dead, expired and pending letters. Jump, Close and Postpone
  have different actions; text labels alone are not stable action identity. The native archive retains
  a received letter after dismissal. Do not delete or duplicate the birth/adoption history entry.
- Native manual Rename prefill temporarily seeds RNG with pawn ID and restores RNG state; source
  TryName simply calls the generator. No promise of identical future game-wide random sequences is made.

These facts came from PregnancyUtility, ChoiceLetter_BabyBirth, PawnBioAndNameGenerator,
PawnNamingUtility, SpouseRelationUtility, Designator_Adopt, PawnGenerator, Dialog_NamePawn, Letter,
ChoiceLetter and LetterStack. Pawn's saved deadline was inspected in the identical-build S042 evidence.
Ideology `Defs/PreceptDefs/Precepts_MarriageName.xml` supplied current precept facts. No running-game
test has yet established patches, notification presentation, races or source interoperability.

## Adopted rules, deliberate corrections and limits

The draft retains source generation, parental modes/fallback orders, default Random, optional Ideology,
stillbirth/letter-suppressed coverage and load catch-up for **confirmed pending** player baby names.
It preserves prepared stochastic surnames and normal voluntary renaming windows; changing settings
never renames a completed baby. All birth outcomes, relationships, care and development remain native.

Corrections: require reliable pending-name evidence, not a localized word alone; preserve intentional
Baby and ambiguous legacy names; restrict surname changes to the owned player birth; use a usable
generated surname if neither parents nor existing name supply one; tie notification changes to a
confirmed successful owned assignment; preserve all non-naming notification content/actions and
fallback atomically if safe handling cannot be established. Existing genuinely named adoptees retain
their native letter. New and persisted owned provenance is language-independent. Ambiguous old saves
cannot be safely “fixed” by guessing all translations of Baby and are deliberately left native.

No extra entity UI, permanent first/last rename privilege, auto-naming of foreign babies, source-config
import, general race/name-system rewrite or whole-mod drop-in claim. Biotech is a feature prerequisite,
not a mandatory DLC for unrelated megamod functionality. Settings use common global/save controls;
individual names/history are gameplay state. No second pawn/Thing index, periodic world scan or
per-port cache is justified by one birth event plus one bounded initialization reconciliation.

## External contracts and selected-source composition

Bounded installed text and binary searches for the source package, namespace and BabyNamer found
source files/assemblies and inventory references, but no executable outside consumer. Public helper,
patch and settings types exist; absence of a found consumer is not proof none exists. Do not spoof
the package or promise these assembly/type identities. Native Pawn.Name, parent links, naming deadline,
letter identity/history and ordinary manual controls are the meaningful contracts to preserve.
Source global `surnameMode` is not silently imported. Original active source suppresses the overlapping
owned group with a clear diagnostic; never unpatch its owner. Other name/letter providers need exact,
absent-safe semantic adapters. S031 Toddlers retains `DevelopmentalStage.Baby`, as recorded in its
[lifecycle research](2026-11-03-toddlers-research.md). Source catch-up therefore also reaches placeholder
toddlers. The owned contract preserves confirmed pending-name eligibility for those toddlers while
protecting chosen names. Supported alien stages follow actual stage/naming contracts, not human age
guesses. Do not patch our own development code.

## Deferred research and verification

| ID | Remaining work, not a question asking the user to explain the source |
| --- | --- |
| AN-Q1 | Resolve exact current-build birth/generation/adoption/pre-publication and load seams, pending-name provenance, atomic name/notification commit, effective foreign patch interactions and stable target documents |
| AN-Q2 | Verify native/name-provider generators, parent-role and Ideology willingness semantics, prepared-surname provenance, optional DLC absence and all seven localized notification/manual flows |
| AN-Q3 | Implement common policy and minimal saved success/pending state, conservative legacy catch-up, repeated initialization/save switching and original-provider suppression; any migration needs a separate contract |
| AN-Q4 | Focused honest RED/GREEN, independent implementation review, package checks and native birth/adoption/stillbirth/load/manual workflows on the reviewed build, including ordinary development and notification regressions |

Semantic pass: no change to fertility, birth success, parentage, mood, development, combat or work.
The 50/50 ideology selection and surname fallbacks are identity policy, not a stat bonus. Native
renaming deadlines are preserved, not extended as a cheat. Biotech absence is a visible feature
availability condition. Actual UI usability and successful native integration remain unverified.

## Documentation checkpoint

Independent contract review requested removing transaction/patch mechanics from the observable spec;
those remain in design while visible consistency, fallback and startup warnings stay specified.
Independent source review caught the S031 toddler-stage composition error; the draft now preserves
confirmed pending toddler catch-up and adds its scenario, without changing chosen names. Both focused
re-reviews found **no remaining issues**. Source/About/native hashes were independently verified.
All eight scoped documents passed local-link/whitespace checks. Typed strict OpenSpec validation:
**66 items passed, 0 failed**. This capability has **9 requirements and 33 scenarios**; all **21**
implementation tasks remain unchecked. No runtime implementation, game launch or source/save mutation.
