# S056 — Cut It Off research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement P1-50. Inspected 2026-11-04. Source analysis and observable draft complete;
independent document review and strict validation passed. Runtime work remains open.
Nothing implemented or accepted in game.

## Documentation and exact inputs

Read [Workshop 3628405994](https://steamcommunity.com/sharedfiles/filedetails/?id=3628405994)
before source analysis. It describes desperate self-amputation by a conscious, non-incapacitated
colonist when no other player-faction pawn is in that condition on the map, an expendable part is
infected, medical work is possible and self-tending is enabled. The contextual command interrupts
other activity. The request in comments to remove the solitude restriction is not a user instruction.
The page supplies no source link or settings description; a targeted public source search produced
no useful repository. That is a bounded finding, not proof no source exists.

Installed root `F:/Steam/steamapps/workshop/content/294100/3628405994`, package
`LegendaryMinuteman.CutItOff`. About author LegendaryMinuteman; Workshop display author Shakecataz.
About supports 1.6/1.7; LoadFolders selects root plus `v1.7`. Assembly/file 1.0.0.0, copyright 2025.
No source or license found in the installed inventory. Independently engineer behavior; no permission
to reuse code/art is inferred from installation. No upstream code, icon or binary is shipped.

| Input | SHA256 |
| --- | --- |
| About.xml | `CABA120B288BB8F0C4D49F7F90F087B58B3A8525CA77CFCF3969E088A49D57AF` |
| Selected v1.7/Assemblies/CutItOff.dll | `32D54A0DE28246E9B2C9C012E532D87FB8CA7CF1BAEE6E44F07B2BB2100822C0` |
| Native Assembly-CSharp.dll, 1.7.4998 rev1299 | `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |

Ignored evidence: `artifacts/Refinement/megamod-spec-20261104/cut-it-off/{compiled,native}`.
All four selected behavior classes, AssemblyInfo and generated project were read. The decompiler's
net472 project is a reconstruction, not upstream build source. References include Assembly-CSharp,
0Harmony, System.Core and UnityEngine.CoreModule. About declares no dependencies despite eager
Harmony use. All selected Job/Thought/Tale XML and English keys, About and LoadFolders were read.

## Complete selected behavior

| Surface | Exact installed behavior |
| --- | --- |
| HarmonyBootstrap | Static startup, owner `com.legendaryminuteman.cutitoff`, blanket PatchAll inside try/catch; catches to a log message, no semantic admission |
| Pawn.GetGizmos postfix | Yields existing commands, then finds the amputation part before actor guards; appends one command when eligible |
| Actor guards | Player-controlled colonist, alive, not downed, CanBeAwake, Doctor work enabled and not disabled, selfTend true, Moving and Manipulation present, Caring not disabled, no current SelfAmputation job |
| Solitude | `pawn.Map.mapPawns.FreeColonistsCount == 1`; no consciousness test on other colonists and no spawned/map null guard before use |
| Infection | Exact WoundInfection; selects greatest severity first, then tests that one part, rather than selecting the most severe eligible infection |
| Part | Outside; manipulation/moving limb core/segment/digit tags, or exact Eye, or case-sensitive Ear/Nose substring in defName; no explicit null/core/vital/implant/ancestor safety checks |
| Command | Finds medicine, creates player-forced SelfAmputation with count1, targetA when found; StopAll, clears queued jobs, starts forced interrupt; no part choice/confirmation |
| Medicine discovery | First IsMedicine inventory item; else nearest reachable map medicine within9999 cells, not forbidden; no patient care policy or reservation check in discovery |
| Reservation | Reserves targetA with stackCount -1, even though intended use is one unit |
| Collection | If no inventory medicine, walks to map target and StartCarryThing; carried medicine is not the inventory medicine later read |
| Work | Reselects infection part when MakeNewToils is constructed, waits4800 ticks, progress TargetA, then completes; no explicit in-wait clinical revalidation |
| Chance | `0.65 * InventoryMedicine.MedicalPotency * (1 + 0.05 * MedicalLevel)`; no clamp and no null guard |
| Success roll | Cut damage equal to part max HP times1.01, armor penetration0, exact part, propagation disabled; then unconditionally success memory, tale and message |
| Failed roll | Cut damage15–29 inclusive, random native hit part; if still alive replaces success memory with failure memory; failure message also runs on death |
| Cost | At completion consumes one unit of whichever inventory medicine is first then, not the pinned map target or necessarily the medicine previously evaluated |
| Persistence | Base job persistence only; no custom saved part, medicine decision, work outcome or one-shot outcome marker |

The job may begin lying down and is player-interruptible. There is no bed/anesthetic/research/tool
requirement, surgery-speed stat, native surgery-success formula, explicit medical experience award,
automatic wound tending or general self-surgery feature. No bottom tab, settings class, view toggle
or source configuration persistence. Four English keys cover command/description/success/failure.

Selected own Defs: SelfAmputation job; SurvivedSelfAmputation memory, +30 mood for3 days, stack1;
SelfAmputationDespair memory, -18 for0.5 day, stack1; SelfAmputationSurvived single-pawn tale.
Failure stage description says "I lived." despite the failure narrative; do not copy that mismatch.

## Native cross-checks and static risks

Read current Recipe_RemoveBodyPart completely; relevant MapPawns collections, StatExtension.GetStatValue,
StatWorker.GetValue and StatRequest.For; Core MedicalPotency stat and medicine XML values. These are
original binaries/XML, not a loaded effective mod list or gameplay reproduction.

- FreeColonists delegates to FreeHumanlikesOfFaction over AllPawns, including map-held unspawned
  pawns. It tests faction/host/humanlike and Anomaly subhuman exclusion, not consciousness. A downed
  second colonist, or a held colonist, therefore blocks the source command. Slaves can be included.
- Medicine carried in hands is a different holder from inventory. Null stat requests log an error
  and create an empty request; stat caches can also reject null keys. Do not reduce this to a proven
  universal NullReferenceException. What is established is that no valid collected-medicine potency
  is supplied and that carried world medicine is not the consumed inventory unit.
- Native MedicalPotency has defaultBaseValue0 and minimum0.2; Core herbal/industrial/glitterworld
  source XML values are0.6/1.0/1.6. Finalized active-mod values remain unmeasured.
- Native body-part removal uses SurgicalCut99999 with penetration999, rather than ordinary Cut at
  maxHP1.01. Its full recipe also evaluates surgery failure and harvest/product consequences.
  Calling the entire recipe after our chance roll would double-test failure and add unintended
  harvesting semantics. The actual native removal path, missing descendants, bleeding and death
  consequences must be retained without those extra outcomes.
- Source ordinary Cut/zero penetration does not itself establish that the intended part becomes
  missing. Success text/tale/mood is emitted without checking the resulting pawn. This is a static
  false-success risk, not a reproduced failure rate.
- Job reconstruction can choose another part as infection ordering changes; completion can choose
  another medicine as inventory changes. There is no explicit interruption/reload at-most-once proof.
- Finding an ineligible worst infection can hide a lower-severity eligible limb infection. Name
  substrings alone cannot establish anatomical safety on a modded race.

## Proposed owned behavior and deliberate deviations

The draft follows the documented emergency niche, not the exact-one-total-colonist implementation.
Use one contextual command with a confirmation naming the infected part, current chance, medicine
or no-medicine cost, lasting loss and danger. No new tab or unrestricted health override. Default
part is the most severe eligible infection; tied choices use stable body order, and the player may
select another eligible infected part. Never silently switch the confirmed target.

Proposed solitude test is conservative and explicit: no other spawned player-controlled humanlike
colonist on this map is awake-capable, not downed and has movement/manipulation. Sleeping does not
make an otherwise capable colonist absent. Medical skill, work priority and path obstruction do not
remove that other colonist from the count. Dependent children can therefore block if they meet
those physical conditions; whether to narrow this to potential medical helpers is EA-Q1, not an
undocumented improvement. Held/unspawned pawns, other maps, prisoners, visitors and animals do not
block. Actor still needs native medical/Caring eligibility, active Doctor work and self-tend.

Natural outside limb segments/digits and eyes/ears/nose are eligible only when present, directly
infected and their removal is not intrinsically fatal for the current anatomy. No vital/core/internal
part, implant extraction, healthy harvesting or arbitrary name matching. Losing the last working
limb may down the pawn and later bleeding may kill; those ordinary consequences remain. Select the
most severe eligible infection even if an ineligible one is worse.

Pin one permitted medicine unit: prefer own inventory, otherwise nearest safe reachable unclaimed
map supply; within equal location/path priority prefer potency then stable identity. Respect patient
care permission, forbidden/access and shared cargo/capacity accounting. No Medicine/No Care policy
does not permit spending medicine; No Care blocks the command, while no-medicine care or unavailable
permitted stock allows an explicitly confirmed attempt without medicine. No-medicine potency0.2 is
a selected draft baseline grounded in the native stat floor, **not a demonstrated source outcome**.
Losing an agreed medicine aborts without substituting unmedicated surgery.

Retain4800 work ticks and source chance coefficients, clamped0–100%, medical level0–20. The chosen
unit's effective potency applies regardless of its holder. Pin chance when work starts and show it;
no automatic bedside/anesthetic/skill-learning addition. Cost and one attempt resolve only after
completed work, once. Cancellation before resolution consumes no medicine and causes no injury;
elapsed time is lost unless resuming the same saved active job. No automatic retry. A success roll
must actually remove the selected part before living-success feedback. Native physiology remains
authoritative; failure uses the source15–29 ordinary random-part Cut injury and can kill. Do not
invent loot. Retain source nonstacking mood/tale outcomes only for a matching living outcome; a
death or blocked/unknown health outcome must not report survival or refund a completed attempt.

All values above are explicit named constants for this draft, not new per-port configuration. No
new settings interface is selected; future tuning controls must use the common global/save model.
Self-tend, medical-care category, part selection and an active job are pawn gameplay state. S042
owns self-tend automation; this feature reads that result, never toggles it to make itself eligible.

## Balance comparison, not acceptance

| Source-XML medicine | Potency | Medical0 chance | Medical10 chance | Medical20 chance |
| --- | --- | --- | --- | --- |
| Proposed no medicine |0.2|13%|19.5%|26%|
| Core herbal |0.6|39%|58.5%|78%|
| Core industrial |1.0|65%|97.5%|100%|
| Core glitterworld |1.6|100%|100%|100%|

Numbers follow the source formula with the proposed clamp. Source raw industrial Medical20 is130%
and glitterworld Medical0 is104%; these already always pass its random comparison. One medicine
unit and4800ticks (1.92 game hours) plus permanent loss are real costs. A100% roll is not immunity
to bleeding/death. +30 for3 days is strong; keep as source-derived provisional balance, no repeat
stacking or healthy-part farming. Finalized statistics, actual removal physiology, costs and mood
still require an isolated player workflow. No package/game launch is justified by this prose slice.

## Contracts and composition

Bounded installed Workshop/local Mods scans of DLL ASCII/UTF16 names and XML/C#/JSON references found
only source1.6/1.7 own files for CutItOff/SelfAmputation. No external executable consumer found in
that scan; not universal absence. Public SelfSurgeryUtility functions, postfix and job-driver types
are potential bindings; no facade is justified without an actual consumer. Job/Thought/Tale Def
names and driver type are save bindings even without a third-party consumer.

Use owned identifiers; no package spoof or automatic foreign-save/settings migration. An explicit
later migration needs backups, atomicity, rollback and proven postconditions. Source present disables
our overlapping group with a warning, leaves its own commands and saves untouched. Invalid owned
saved work must cancel safely, release claims and preserve actual medicine and health.

S021/S024 own cargo interruption/quantities; S042 self-tend; S007 health presentation must reflect
actual health. S046 consciousness and later medical entries affect eligibility through current pawn
state, not self-patches. Priority Treatment/Drug Response composition remains a research item, not
permission to ignore forced-care or treatment claims. Reuse native listers/shared facts only where
needed; no per-pawn whole-map medicine cache or off-thread health access.

## Open work retained for later

| ID | Remaining question / proof | Current draft stance |
| --- | --- | --- |
| EA-Q1 | Solitude scope and modded anatomy | Documented physical-capability rule; dependent children can block; no speculative race support. Narrowing to medical helpers is a later product decision, not assumed |
| EA-Q2 | Effective native removal, medicine and saved-job contracts | Guard complete command/job/consumption/removal path; no part/medicine substitution, duplicate resolution, unknown success or source co-install duplication |
| EA-Q3 | Finalized medical balance and future treatment composition | Source4800ticks/formula/injury/mood, explicit0.2 no-medicine proposal; common settings only if tuning controls are later selected; check S046/S042/S124/S134 interactions |
| EA-Q4 | Implementation and final acceptance | Honest RED/GREEN, language/confirmation/health workflows, independent review, live success/failure/cancel/reload proof on reviewed build; no implementation yet |
