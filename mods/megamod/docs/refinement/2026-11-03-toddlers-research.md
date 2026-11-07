# Toddlers — sequential source research

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Queue **S031**, original notes P1-24/P2-49. Started **2026-11-03** after S021 reconciliation.
**Selected-source research cataloged; six-capability owned draft reviewed and validated.**
See [replacement coverage](../replacements/toddlers.md) and
[OpenSpec change](../../../../openspec/changes/specify-megamod-toddler-development/proposal.md).
Implementation, native compatibility and final playability remain unverified.
The existing S030 temperature change owns the user-confirmed proactive avoidance/escape correction;
this deep dive must cover the rest of Toddlers and compose directly with that behavior.

## Documentation first and exact inputs

Read the [author's Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=2903359152)
and its complete linked [manual](https://docs.google.com/document/d/1QGBg9GYoz4XfWlBkH2_kgV1imqMIKABsfny30A-zRwo).
The manual export was unavailable through the web reader but public HTTP export succeeded; all
991 text lines were read. It labels itself last updated 05/09/2025 and describes 1.6. Treat its
compatibility claims, intended behavior and defects as leads for source verification, not native proof.
Ignored local evidence: `artifacts/Refinement/megamod-spec-20261103/toddlers-manual.txt`.

Installed reference: `F:/Steam/steamapps/workshop/content/294100/2903359152`, **Toddlers** by
**cyanobot**, package **cyanobot.toddlers**, About support **1.4/1.5/1.6**, requiring Harmony and
Biotech. LoadFolders has no 1.7 entry; 1.6 loads root plus 1.6 and conditionally 1.6/HAR for
`erdelf.HumanoidAlienRaces`. Installed presence is not activation or beta compatibility.

Public repository [cyanobot/Toddlers](https://github.com/cyanobot/Toddlers) was cloned read-only at
**ea885dffc6ed65937b3fbf0daa62598cf11a61fc**, 2025-12-09T15:26:16Z, commit
`realistic human sounds compat`. Local checkout:
`artifacts/Refinement/megamod-spec-20261103/toddlers/upstream`.
All **38 compared inputs**—About, LoadFolders, every published 1.6 file including both assemblies,
and English keyed files—match installed bytes. This connects the package to the checkout; it does
not assert that unbuilt source exactly reproduces its assemblies or works on beta.

| Exact input | SHA256 |
| --- | --- |
| Installed About/About.xml | `A356F0EF0BFE51CA2C4C039B26FF5E6B232AFDFC40F94321461199E0188E2368` |
| Installed LoadFolders.xml | `700C34EA3772840A0ED0CD9C650592ADEC93B51ADDBE3AEF96008336D22F5D10` |
| Installed 1.6/Assemblies/Toddlers.dll | `8CDBA87787B069B1CBBB44F6C78DF5527723CBC253E4404A548DB112F1FC08B5` |
| Installed 1.6/HAR/Assemblies/ToddlersHARCompat.dll | `C319A64D8D92CAB3F8EC2ECA48D49D88375F35DAC28B838F45260DB37427AE6D` |
| Public manual text export | `C9D6C27780EF98DD7124A028316DE5D7EEA9C4BD199B0FFC7DC21DE5E087E714` |

No README/LICENSE-named file was found in the pinned checkout inventory; no license grant is
assumed. Independently specify behavior; do not copy its code, assemblies or graphics into product.
The manual says some graphics derive from the game and describes copying third-party clothing
textures locally. That is source provenance, not authorization to redistribute them.

The Workshop comments link an unofficial 1.7 port, **3832132406**. Its directory is not installed.
It has not been analyzed or selected as a replacement source. Comments are not evidence of its
correctness; keep the exact original reference and inspect a port only where useful for beta gaps.

## Coverage ledger — do not treat the inventory as an audit

The checkout contains **188 C# files** under Source. The main project explicitly includes 162 paths,
some version-conditioned; its separate HAR project includes seven. Old, testing and excluded source
must not be treated as active merely because present. Existing read-only main decompilation from
S030 has 153 C# files at
`artifacts/Refinement/megamod-spec-20261102/brrr-and-phew/toddlers-decompiled`.
Its input DLL still matches the hash above. No downloaded project was built or executed.

Fully read in this S031 pass:

- `Toddlers_Settings.cs`, `Toddlers_Mod.cs`, `Toddlers_Init.cs`, `ToddlerUtility.cs`,
  `LifeStageWorker_HumanlikeToddler.cs`.
- `Learning/ToddlerLearningUtility.cs`, the three `Hediff_*` learning classes, and learning
  `Harmony/{GeneratePawn_Patch,VatLearning_Patch,Door_Patch}.cs`.
- Four current `UI/` classes and two `Work/` classes.
- `1.6/Defs/{LifeStages,Hediffs}.xml` and `1.6/Patches/{LifeStages,Races_Humanlike}.xml`.
- Selected installed-body comparison: settings declarations/persistence, complete base learning
  class, and walking progress/cache/animation dispatch. The remaining walking body is source-read,
  not a completed whole-assembly comparison.
- All 62 files across Feeding (10), Play (35), Crib (11) and Safety (6), plus the corresponding
  complete ThinkTrees/play/jobs/lists/filth/thoughts XML and ThinkTrees/Jobs patches. Detailed
  findings, exact coverage and discrepancies are in [feeding, play and care](2026-11-03-toddlers-feeding-play-care.md).
- All 16 non-legacy Apparel files and seven MentalStates files, including explicit excluded/
  commented-code identification, plus apparel/category/mental-state/adult-job Defs and
  mental-fit/expectations/inspiration patches. See [apparel and mood](2026-11-03-toddlers-apparel-mood.md).
- Current caravan/carrying/kidnap/combat/refugee, MiscHarmony and animation groups, root debug/
  Def/log bindings and all four English keyed files, with legacy/excluded files separated. See
  [travel and rendering](2026-11-03-toddlers-travel-rendering.md) for exact coverage and limits.
- All 12 DBH source files, the HAR bridge and seven current separate HAR-project files, matching
  XML, and a bounded installed-consumer search. [Integration research](2026-11-03-toddlers-integrations.md)
  records the concrete Progression: Attire package/XML consumer and unverified provider boundaries.

S030's [narrow earlier inspection](2026-11-02-brrr-and-phew-research.md#narrow-toddlers-composition)
read ThinkTrees/patches, identification, crawling, crib/door and initial safety methods. It is useful
evidence but does not complete S031's care, food, play, graphics, apparel, compatibility or lifecycle audit.

The selected current source groups and 1.6 XML/keyed descriptions are now cataloged. Remaining
research is exact native-beta behavior, selected published-body equivalence, provider internals
and inbound binary/reflected contracts. The owned draft now reconciles documented intent/source
discrepancies into observable scenarios. An inventory, source read or draft is not runtime acceptance.

## Documentation scope to verify

The manual describes an early-childhood lifecycle, learning-dependent independence, adult attention
and care, temperature protection through clothing, and continued dependence during illness. Its
chapters also cover travel, kidnapping, adoption and refugee arrival. These all belong in the
behavior inventory; limiting replacement to the already specified temperature fix would omit most
of the selected source. Source debug tools and compatibility mechanisms remain separate from ordinary
player mechanics. Defaults, restrictions, timings and discrepancies require code confirmation.

## Confirmed initial source findings

### Lifecycle, learning and capabilities

Human XML inserts `HumanlikeToddler` at biological age 1; the helper uses end age 3 and delegates
race-specific ages to HAR when detected. Toddler identity is an actual life-stage Def or HAR worker
contract, not every pawn within a guessed age range. Developmental stage remains Baby. Its Def
permits movement, voluntary sleep and sleeping while held, with MoveSpeed factor 0.4. No ordinary
work capability is granted: current work patches return all WorkTypeDefs as disabled and add
AllWork/Violent flags. The later travel/rendering audit records the broader combat/command patches.

On a normal transition from a baby stage while playing, the worker reconciles body/apparel when
needed, resets learning to age, sends an appropriate notification/effect, assigns a toddler
backstory and refreshes disabled work. A generated toddler also receives age-adjusted learning.
These paths alone do not prove safe mid-save installation or repair of every already-existing pawn.

Learning initialization uses elapsed fraction of the toddler span divided by each configured
learning fraction, capped at 1. A human's default span is two years; each learning fraction is 0.8.
Learning progress is applied on a 2500-tick hash interval using biological aging speed. Completion
or leaving the toddler stage removes the learning condition. The vat hook adds learning using
60% of accelerated vat aging; exact combined native/vat ticking still needs a beta check.

| Learned progression | Source thresholds and observable consequences to specify |
| --- | --- |
| Walking | Crawling below 0.5, wobbly from 0.5, complete at 1; Moving offsets -0.65/-0.45 while learning; crawler cannot open doors |
| Manipulation | Inept below 0.3, clumsy from 0.3, confident from 0.6, complete at 1; offsets -0.85/-0.55/-0.25 |
| Self-feeding | Helper permits clumsy or later, or absent learning condition; source food/job checks are in the feeding research, exact beta behavior remains open |
| Self-dressing | Helper permits confident or later, or absent learning condition; apparel research records native delegated jobs still needing beta inspection |
| Eating location | Clumsy helper chooses floor eating; later source leaves the ordinary native eating-location route in place |

Walking progress chooses crawl/wobble animations. Wobble magnitude/period are cached every 60 ticks;
the walking class exposes an interpolated penalty, but the source's former stage-mutation code is
commented out and XML retains the stated fixed offsets. Do not describe the exposed property as
an active gradual capacity improvement without finding a consumer. First-steps notification occurs
on entering the wobbly stage outside a growth vat. The later travel/rendering audit covers the full animation group.

`IsBabyBusy` presently treats drafting, any Lord assignment, urgent eating and being a current
target of a player-faction pawn's job as busy. Full callers/reservations/emergency precedence remain
to inspect; this does not prove all eating is protected against every caregiver interruption.

### Settings and existing UI

Source preferences are static/global ModSettings. Settings UI also calls ApplySettings after
drawing; it mutates shared baby/toddler temperature offsets and no-expectations mood directly.
Apparel selection invokes separate global availability changes. Our common defaults/save model
and entity-state separation must replace this architecture; source global storage does not by
itself justify an exemption from per-save overrides.

| Setting | Source built-in value | Source UI range / choices |
| --- | --- | --- |
| Respect scheduled bedtime / floor sleeping | Each on | Boolean |
| Feed toddlers capable of self-feeding | On | Boolean; successful native best-food search gates the off-state suppression, see feeding research |
| Feeding mess | On | Boolean |
| Draft toddlers / toddler baby talk | Each off | Boolean |
| Infant / toddler play decay | 5 / 3 | Each 0–50; toddler tooltip incorrectly says default 5 |
| Loneliness rate | 1 | 0–10 |
| No-expectations mood | 20 | 0–100 |
| Infant temperature minimum / maximum | 20 / 30 Celsius | Minimum -30–25; maximum 26–50 |
| Toddler temperature minimum / maximum | 18 / 28 Celsius | Same ranges; source subtracts fixed adult 16/26 baselines |
| Walking / manipulation learning fraction | Each 0.8 | Each 0.01–1 of toddler stage |
| Apparel availability | BabyTribalwear | NoBabyApparel, NoTribal, AllTribal, BabyTribalwear, AnyChildApparel; source effect recorded in apparel research |
| Move feedback | DangerAndUnknown | None, Danger, DangerAndUnknown, Unknown, All |
| Baby-safety debug logging | Off | Boolean; our debug placement takes precedence |

The source stores present static values as Scribe defaults rather than independently resetting
immutable defaults. No save-specific inheritance is supplied. Temperature values above are source
baseline-setting intentions, not measured final tolerances for every equipped or modded pawn.
The extra `tribalBabyClothes` boolean is declared but not persisted/rendered in this class; the
subsequent apparel caller check finds only an excluded Scrapyard UI use. It is not the active
similarly named apparel list.

Existing Assign/Schedule pawn getters are replaced to include babies; these are existing bottom
windows, not new tabs. Per-pawn schedule, area, apparel and feeding choices remain gameplay state.
The draft-button patch hides it for an undrafted toddler when the option is off; it preserves an
already drafted pawn's control. The baby-talk transpiler consumes fixed flow/offset assumptions
without a completed no-partial-change guard. Do not copy it; future hooks need target specs.

Startup detects optional mods by display name, invokes optional DBH/HAR/Hospital hooks, runs
PatchAll and applies global settings/apparel changes. This is not our required canonical-package,
semantic-admission architecture. Hospital reuse even applies a foreign postfix to a source-owned
cloned bed finder; actual contracts and direct owned composition require separate analysis.

## Open research and proposal boundaries

- **TD-Q1 — resolved for specification:** selected current groups/XML and functional scope cataloged;
  six owned capability drafts retain apparel/protection and lifecycle alongside intentional source
  corrections. Remaining exact native/provider and acceptance evidence is tracked separately.
- **TD-Q2 — direct safety/needs composition:** S030 controls safe wandering, escape and safe area
  return; reconcile caregiver urgency, safe beds, starvation/eating, crib capability and S024 needs
  work. No allowed-area return may undo the user-confirmed temperature correction.
- **TD-Q3 — external contracts and beta lifecycle:** inspect actual consumers, HAR/DBH/provider
  bindings, child-stage age transitions and 1.7 native work/rendering/UI. Distinguish no-Biotech
  feature availability from making every megamod feature require Biotech. No package impersonation.
- **TD-Q4 — settings, saved development and assets:** define settings effect timing/validation,
  existing saves/learned progress, common globals/overrides, capability-aware clothing, native
  visual behavior and independent asset provenance; record any justified scope exemption.
- **TD-Q5 — future acceptance:** focused tests, semantic patch/XML guards, native care/feeding/
  play/growth/travel workflows, visuals and playability remain unimplemented. Research is not proof.
- **TD-Q6 — feeding/play discrepancies:** reconcile actual food memories/policies, mess/hygiene
  toggle scope, simulation-time rates, solo-Play cap/interruptions, crying priority and the manual's
  indoor-bugwatching/clumsy-drawing claims against the inspected source. See the detailed group
  research; no accidental source behavior has been adopted as an owned requirement yet.
- **TD-Q7 — apparel/mood contracts:** finalize native dressing/stripping/scoring and XML inheritance,
  safe policy/mode transitions, provider preservation, assets, clothes-removal and mental-state
  scope. Source global rewrites and broad state suppression are not automatic owned requirements.
- **TD-Q8 — travel, arrivals and presentation:** pin native adoption/carry/departure, passenger-list
  lifecycle, kidnapping intent, refugee cleanup/age-up and animation ownership. Preserve S030
  safety and ordinary threat/damage rules without claiming every provider follows native hooks.
- **TD-Q9 — integration corrections and consumers:** atomic DBH admission, per-pawn needs, real
  water accounting/bath recovery, HAR ordered ages/graphics/idempotence, and the concrete Attire
  package/XML contract. Remaining provider/binary checks stay explicit; no source-ID spoofing.

The completed source catalog supports the owned proposal, six observable capability drafts,
design, 49 unchecked implementation/acceptance tasks and replacement coverage. No normal
configuration, save, Workshop file, runtime source or package was modified. Earlier checkpoints
below describe their historical coverage; the current disposition is the draft linked above.

Independent read-only review of the initial checkpoint found no issues. It confirmed the pinned
commit, all five hashes, all 38 package comparisons, file counts, initial settings/learning/UI
statements and truthful incomplete status. Local links/whitespace and queue/question uniqueness
checks passed; the repository still has **31 drafted entries out of 136**. Strict typed OpenSpec
validation passed **52 items, 0 failed**. This validates documentation at the stated coverage level,
not the unread behavior groups or future native implementation.

The subsequent feeding/play/crib/safety and apparel/mood checkpoint added **85 fully read source
files**, including explicitly identified inactive files, and the listed XML. Separate independent
reviews checked those groups against source and manual. Corrections record stale crib-play
adjacency, a missing safety-menu explanation, precise non-downed play eligibility, special-apparel
availability in the default modes and the explicit slave branch in player control. Both focused
follow-up reviews found the corrections resolved. Links/whitespace, 136 unique queue entries and
unique question IDs passed; **31 drafts / 136 entries** and **52 strict validation passes** remain.
At that historical checkpoint TD-Q1–7 were open and travel/rendering/provider groups remained.
Those groups are now cataloged above and their intended outcomes are reconciled in the owned draft.

## Owned draft and review checkpoint

The current change contains **6 capabilities, 33 requirements, 62 scenarios and 49 unchecked
implementation/acceptance tasks**. Two independent research-group reviews checked travel/rendering
and integrations; corrections distinguish GeneSeed's age-one start from its unknown duration and
remove stale unread claims. Three independent specification reviews and focused follow-ups resolved
all nine findings: time-consistent mess counts, loneliness-driven initiation, sleeping crib exit,
the existing S030 temperature margin, harmless episode/group/area outcomes, Go frenzy age, baby-talk
thought display, explicit hygiene-loss units and the separate pending DBH Lite variant.

Typed strict OpenSpec validation passes **53 items, 0 failed**. Local link/whitespace checks passed
for 17 scoped Markdown files; the ledger has 136 unique entries, 32 drafts and 144 unique question
IDs. TD-Q1 catalog/reconciliation is complete at this source coverage level. TD-Q2–9 retain exact
native/provider evidence, tuning, save failure handling, visuals and runtime work. No implementation,
test/build/package execution, game launch, source removal or compatibility acceptance occurred.
