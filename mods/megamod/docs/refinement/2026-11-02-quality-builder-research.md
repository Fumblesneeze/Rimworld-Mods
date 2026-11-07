# QualityBuilder — source research and specification decisions

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
S013 / P1-10. Documentation-first source/native/consumer inspection supports the
[quality-construction draft](../../../../openspec/changes/specify-megamod-quality-construction/).
No runtime implementation or compatibility acceptance. This follows the reviewed S012 browser draft.
Record unresolved work here; do not ask the user to explain existing mod behavior.

## Primary documentation first

Read on 2026-11-02: [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3512466087),
[change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3512466087) and the linked
[author repository README](https://github.com/pkp24/QualityBuilder). Changelog page two failed through
the web reader. Generic Steam removed/incompatible banners are not compatibility evidence.

The documented central workflow sets a minimum building quality and repeats deconstruction/rebuilding
at the same position until satisfied. The author describes a default three-rebuild-attempt limit,
retaining the last building with a message on exhaustion, and builder eligibility relative to the
best constructor's skill. Exact counter boundaries and settings defaults still require source
inspection. These are documentation claims, not verified native outcomes.

The README describes repairs to save-load forbidden state, unintended reconstruction after manual
deconstruction, map-setting enablement and overlapping-building/job handling. It says bundled Harmony
was removed. The Workshop description also mentions an ideology-furniture fix. About adds same-Stuff
replacement. Each must be traced in the installed code rather than assumed correct from release notes.

Current public metadata still advertises through 1.6. An author October 18 comment describes testing
1.7 with changes not yet uploaded; this is not proof that the installed release supports 1.7. The
readable changelog's latest entry is August 3, 2026. Author comments also discuss map settings overriding
defaults and best-builder selection. Treat those as research leads and source/version context.

## Installed identity and provenance

- **QualityBuilder Unofficial 1.6**, package **hatti.qualitybuilder**, Workshop **3512466087**.
  About authors **Hatti / pkp24**; Workshop maintainer display name **Teemo**. Original upstream item
  linked by the maintainer: **754637870**. The package ID is shared with the original; megamod must
  not impersonate that package just to trigger consumers.
- Read-only root `F:/Steam/steamapps/workshop/content/294100/3512466087`.
- About modVersion **1.6.013**, supported versions **1.1–1.6**, required Harmony and load-after Harmony.
  LoadFolders has version entries only through **v1.6**; no 1.7 declaration was found in these files.
  Actual 1.7 fallback/load behavior remains native research, not inferred here.
- Latest installed path `Versions/v1.6/Assemblies/QualityBuilder.dll`, assembly **1.0.17.0**,
  file/product **1.0.17**, SHA256
  **A9759C6C3675644AB165FE256EB58FE50633DF5E3FC516F8B113AEE60E141247**.
  Metadata and assembly version strings differ; keep both, not a guessed unified release version.
- File inventory includes PDB, earlier version assemblies, DesignationDefs, keyed languages and
  textures. No installed C# source was seen in the bounded inventory. The linked repository has
  `_PROJECT`, versions, documentation and source metadata; pin and compare its actual revision to
  this installed binary before treating current source as the installed behavior.
- Installed `About/License.txt` declares **CC BY-SA 4.0**, attribution/change notice/share-alike terms,
  and a separate Ludeon-specific exception. This records the supplied notice, not a new legal
  determination or permission to copy arbitrary later contributions. Independent implementation is
  intended; do not copy source assemblies/assets into the product. Confirm provenance before any reuse.

## Pinned source and exact installed cross-check

Read-only inspection checkout under ignored
`artifacts/Refinement/megamod-spec-20261102/quality-builder/upstream`, commit
**c063417d46b2252d15851bdd02a6712a5a971d0d**, timestamp **2026-08-03T22:08:19-04:00**, subject
**1.6.013**. Its `Versions/v1.6/Assemblies/QualityBuilder.dll` hash matches the installed hash exactly.
Relevant C# is under `_PROJECT/QualityBuilder`; installed DLL decompilation is sibling
`installed-decompiled`. Read the source settings/mod/map component, component/UI, core helpers,
startup injection/designators, construction/deconstruction hooks and optional Replace Stuff hook.
Critical defaults, retry flags and consumer bindings were also checked in the installed decompilation;
a matching committed DLL alone does not prove every C# file was compiled into it.

The operation registry exposes live Gateway method decompilation but no offline whole-mod source
inspection operation. Used existing ILSpy locally without loading the Workshop mod or launching a
game. Workshop content was not changed, and no upstream binary enters a product package.

## Source settings and scope

| Source field / saved key | Initial and missing-field value | UI / behavior |
| --- | --- | --- |
| defaultUseQualityBuilder / defaultUseQBuilder | true | Automatically mark new eligible build blueprints |
| skillDifferenceFromBestBuilder / qBuilderSkillDiff | 0 | Allowed levels below best; global page 0–20 difference; map page displays a computed minimum when best > 0 |
| ignoreQualityBuilderAtSkill / qBuilderIgnoAtSkill | 20 | Skill at/above which relative threshold is bypassed, slider 0–20 |
| defaultMinQualitySetting / desiredMinQuality | Awful | Awful through Legendary; default therefore restricts finisher without requiring a quality redo |
| maxQualityRebuildAttempts / qBuilderMaxRebuildAttempts | 3 | UI 1–20 cycles or infinity (int.MaxValue at slider position 21) |
| excludeMechsFromSkilledBuilds | false | Exclude mechs from best-skill pool and ordinary gated finishing; player-forced path bypasses |
| gizmoOnBlueprintsOnly | false | Hide finished-building toggle/menu when true; mechanics keep running |
| bestConstructorOverride | null | Pawn reference; picker only on current-map settings page |

Fresh map components clone globals but initially `useMapSettings=false`; saved key **qbUseMapSettings**
defaults **true** when absent in a loaded record. The source explicitly preserves older map settings,
while fresh maps follow live globals. `getSettings(map)` selects the map copy only when opted in,
otherwise global then fallback. Source settings UI defaults to globals; a status light/text explains
whether those globals affect the current map. It can edit a dormant map copy while opted out.
The pawn picker lists colonists with active Construction work, without the full runtime availability
filter. This is entity assignment incorrectly mixed into source settings for our conventions.

Best skill is cached on the **settings object**, using a Stopwatch with **10000 ms** expiry. With
multiple maps inheriting the same globals, a recent map A result can be reused for map B; the cache
has no map key. This is a code-derived cross-map risk, not an observed live incident. It also defers
eligibility changes until wall-time expiry. Megamod must keep candidate data map-owned and revalidate
native work decisions, not port this cache or clone-per-map settings architecture.

## Orders, gizmos, defaults and saved entity state

Startup scans finalized ThingDefs with a building and CompQuality, adding CompQualityBuilder properties
to the building, its build blueprint and frame if missing. It does not explicitly cover install
blueprints. It mutates Def comps at runtime, and patches Orders.ResolveDesignators to inject two tools
after every resolve. No independent bottom main tab is added.

- Orders Set/Unset tools use filled-rectangle designation on in-bounds unfogged cells. Cell lookup
  takes the first comp-bearing blueprint/frame; direct thing acceptance is less restrictive and
  does not independently establish faction/comp validity. Set skips an already-marked identical
  target. The selected quality is an instance field with default fallback, not saved game policy.
- Seven DesignationDefs **SkilledBuilder, SkilledBuilder2 … SkilledBuilder7** correspond to
  Awful … Legendary. Icons use Skilled1 … Skilled7, with general Skilled/UnSkilled controls.
- Blueprint/frame gizmo toggles skilled mode; right-click selects a quality and enables it on the
  selected compatible Things. Finished buildings offer it below Legendary unless blueprint-only
  display is enabled. For mixed finished selections, the menu only offers targets above the highest
  current finished quality. Finished selections hidden by that preference neither constrain nor
  receive the action. This can omit otherwise useful lower targets on a co-selected blueprint.
- New build blueprints auto-enable from current defaults, unless already designated or respawning
  after load. Changing defaults does not bulk-rewrite existing targets.
- `setSkilled` removes/replaces the quality designation, preserves CompForbiddable's current value,
  updates comp fields and immediately checks a finished building for a quality retry when enabled.
- Entity keys: **Quality** (false), **desiredMinQuality** (Awful), historical spelling
  **desiredMinQualityReaced** (true on missing field), **pendingQualityRebuild** (false),
  **qualityRebuildAttempts** (0). Load repair on blueprint/frame trusts a saved quality designation;
  otherwise a skilled comp restores its missing designation. Constant Scribe defaults avoid the old
  save-map-versus-load-null-map default bug. No automatic megamod migration is implied.

## Builder eligibility and current-job intervention

Source `_WorkGiver_ConstructFinishFrames.Postfix` only gates a nonnull **FinishFrame** job with a
quality designation; blocker handling and resource delivery are not skill-gated here. `forced=true`
bypasses quality policy but not the original game's returned-job restrictions.

Ordinary policy first requires pawnCanConstruct (humanlike with skills, active Construction and
Moving/Manipulation capacities, or colony mech with overseer and enabled Construction). If enabled,
mech exclusion takes precedence. A spawned, same-map, alive, construct-capable explicit override
permits only that pawn, ahead of threshold rules. Its availability test does **not** explicitly
exclude downed, prisoner or mental-state override pawns, unlike the best-skill pool below.

Without a usable override, a pawn at/above the ignore-at skill is allowed. Otherwise require
skill >= max(0, bestSkill - allowedDifference); if no positive best is found, allow. Pawn skill is
colonist Construction or colony-mech fixed skill, otherwise zero. Best candidates are player-faction
pawns on this map, spawned, not downed/prisoner/in a mental state, and construct-capable; drafted
pawns remain included. Mechs are skipped when excluded. No reachability/awake/busy test is part of
the pool; native job checks still govern execution.

A qualified unforced scanning pawn, same map and neither downed nor drafted, can interrupt an already
working unqualified pawn on that exact frame. Forced current jobs are protected. Source cooldown
is **600 game ticks** per frame; its static dictionary clears past 256 entries, without an explicit
map/save key. The intervention happens in JobOnThing/HasJobOnThing scanning, not after the replacement
has actually reserved/started work. Consequently a read/query can cause job interruption despite
the source's plausibility checks. This is a behavior/side-effect risk to correct explicitly, not a
reason to reject all foreign patches or assume a demonstrated mod conflict.

## Construction and quality retry lifecycle

- Blueprint-to-frame hook wraps the native toil init action and copies target, enabled state and
  retry count to the job's resulting target. It deliberately permits ordinary hauling/frame placement.
- Construction hook attaches a finish action to the last native construction toil. It identifies
  a completed building at the cell by the frame's built Def, with a single-candidate fallback only
  when that Def is unknown. Failed construction falls back to the first eligible blueprint/frame
  at the cell and copies quality/counter/style; that fallback still lacks equivalent identity proof.
  A toil finish can also run on interruption, so exact native completion/failure identity matters.
- Completed quality at/above target, or disabled skilled mode, sets reached=true, counter=0 and
  pending=false. Below target with no existing deconstruct designation/pending retry checks the
  cap, then increments the counter and adds an ordinary deconstruct designation with pending=true.
- Counter measures **initiated quality replacement cycles**, not failed initial constructions:
  default 3 permits the initial completion plus at most three deliberate replacements. Construction
  failure during a cycle carries the count without resetting/incrementing it. Native failure can
  still consume resources; the quality cap is not a universal bound on all construction failures.
- At the cap, keep the building and issue a NegativeEvent message. The source does not store a
  once-only exhausted-message marker; repeatedly invoking checks/retargeting can message again.
  New target/toggle changes do not generally reset an exhausted counter.
- Deconstruct FinishedRemoving prefix captures map in a static field; postfix rebuilds only if the
  comp is skilled, not reached and pending. It clears pending, releases reservations on that exact
  removed Thing, places a player blueprint with same Def/cell/rotation/Stuff, transfers counter and
  target, and restores precept/style through CompStyleable or native style APIs. It does not copy
  other building-specific settings/ownership/group state. No reservation on another cell occupant
  is deliberately released. Static handoff reentrancy is assumed by the source, not guaranteed for ours.
- Disabling quality mode removes a pending quality deconstruction designation and clears its flag;
  a purely manual designation is left alone. Ordinary canceled deconstruction has no observed hook
  clearing the pending flag, so a later manual deconstruction can potentially be mistaken for the
  old quality cycle. An already-running job also needs cancellation-boundary verification.
- **Retarget defect:** once reached=true, raising the target below which that building now falls
  schedules deconstruction but never resets reached=false. FinishedRemoving then refuses to recreate
  it. Source and installed decompiled branches agree; a complete search finds no false assignment.
  This is a source-derived loss path, not a live reproduction. Our intended retry must not inherit it.
- Native auto-rebuild propagation and optional Replace Stuff propagation copy target/enabled state
  but not the quality retry counter. Do not confuse ordinary destruction replacement with a quality
  retry or promise universal configuration retention from these two calls.

## Native 1.7 evidence

Read-only decompiled `RimWorld.Frame`, `JobDriver_ConstructFinishFrame`, `JobDriver_Deconstruct` and
`WorkGiver_ConstructFinishFrames` under sibling `game`. Initial `Verse.Frame` lookup failed and was
corrected to **RimWorld.Frame**; no game was launched. Input is the same **1.7.4995 rev309** game DLL
SHA256 **56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21** used by prior slices.

JobOnThing keeps faction/completed-frame/touch/blocker/native construction checks. A construction
toil ends after **5000 ticks** as well as success/failure; completion invokes Frame.CompleteConstruction
and failure invokes Frame.FailConstruction. Thus “last toil finished” alone is insufficient proof
of a particular resulting building. CompleteConstruction consumes the actual resource container,
creates the actual building, rolls quality through QualityUtility using the worker, preserves style,
and transfers storage group/settings when relevant. FailConstruction creates the corresponding
blueprint with Stuff/style/storage details through native behavior. Deconstruction calls native
Destroy(Deconstruct); ordinary resource return and job work remain native, not a fixed 50% formula
in our spec. The source's retry blueprint path does not automatically inherit all of Frame's state.

## Actual consumers and optional integration

Completed bounded package/type/Def-name and general **QualityBuilder** binary/source scans across
installed Workshop and local Mods. A full-qualified string alone misses normal assembly metadata,
so the general name scan was also performed. Findings establish concrete candidates, not acceptance:

- **Vanilla Expanded Framework**, `OskarPotocki.VanillaFactionsExpanded.Core`, Workshop **2023507013**.
  1.7 VEF.dll SHA256 **295E000ECE9F5C65F93A5BA1C461E80221A1F03E273DD7ABDF1E5B63879551CB**.
  Decompiled `VEF.Pawns.VanillaExpandedFramework_QualityBuilder_WorkGiver_ConstructFinishFrames_Patch`:
  active package check for hatti.qualitybuilder, then type/member
  **QualityBuilder._WorkGiver_ConstructFinishFrames.Postfix**. Its prefix skips that postfix when
  the Job's workGiverDef.giverClass derives from **WorkGiver_ConstructionSkill_FinishFrames**.
  That custom WorkGiver requires a ThingDefExtension.constructionSkillRequirement and delegates
  to native frame work, with an optional report override. The exact timing of job.workGiverDef
  assignment versus this patch needs research; no robot-specific behavior should be inferred.
  Without upstream identity this integration will not automatically apply to megamod.
- **Replace Stuff - Continued**, `Memegoddess.ReplaceStuff`, **3526354009** (selected S028).
  1.7 DLL SHA256 **BFAA9C9DC90CDB231F271CCA1BE090CAF696F3A57C49F71ED2044227A7C0C6BC**.
  Installed Source/Replace/GenReplace.cs and decompiled **Replace_Stuff.QBTypes** resolve unqualified
  **CompQualityBuilder**, **CompProperties_QualityBuilderr**, and Def **SkilledBuilder**. Frame
  generation can instantiate the reflected comp properties; placement can mark its generated frame.
  QualityBuilder's own outgoing detection instead checks old **Uuugggg.ReplaceStuff** or exact
  display name **Replace Stuff**, neither matching this installed continued metadata. Its intended
  PlaceReplaceFrame postfix copies target/enabled state. Selected replacements must compose directly
  under S028, not recreate these two independent internal patch/detection schemes.
- **[sbz] QualityBuilder Retexture**, `JGH.qualitybuilder`, **2638388641**, hard-depends/load-after
  hatti.qualitybuilder and supplies matching Skilled/Skilled1…7/UnSkilled texture paths. It is an
  actual asset/package consumer, not a selected full replacement. No automatic compatibility or
  asset reuse is promised for megamod; do not spoof the source identity to satisfy its dependency.
- Remaining hits were own source versions, mod-manager database/replacement rules and LayoutHarness
  sample mod lists; no behavioral consumer inferred from those. A VEF DLL inside another mod's
  source/bin directory is a bundled development artifact hit, not proof it loads at runtime.

## Proposed decisions and remaining research

The draft preserves the seven source defaults/ranges, all quality levels, ordinary construction
economy, relative/absolute eligibility and forced override. It deliberately changes per-map numeric
settings to the common global/save model and moves named builder assignment to contextual map state.
The following are explicit proposed corrections rather than claims about upstream behavior:

- Map-local candidate facts and work-time rechecking; no cross-map settings cache or query-side kick.
- Mixed selection targets apply independently; inspect recovery remains when finished gizmos hide.
- Count initiated replacement cycles, retain exhausted buildings, notify once and require explicit
  Restart for an exhausted run. Native failure/interruption does not replenish the budget.
- Reset the achieved state properly when raising a completed target; distinguish owned removal from
  pre-existing or later manual deconstruction, including active cancellation.
- Recover blocked placement without targeting another cell occupant or charging a second cycle.
- Preserve supported native configuration; retain a custom building intact when consequential
  configuration cannot safely survive. No free quality roll for minification/reinstallation.
- Keep ordinary native auto-rebuild separate from quality-cycle recovery. No original save migration,
  retexture support or package spoofing; source co-installation disables our overlapping feature.
- Independent review identified saved native removal surviving rejected startup admission/source
  overlap. The corrected contract suspends owned removal before it commits, preserves intact building
  and run state, leaves manual/provider work alone and requires explicit Resume after support returns.

| Question | Research / acceptance still required | Boundary until resolved |
| --- | --- | --- |
| QB-Q1 | Exact 1.7 transition/result identity, active-work/forced metadata, generated Defs and semantic admission | Candidate seams are not approved patches or a port |
| QB-Q2 | Active cancel commit, blocked placement, supported configuration, load-time suspension under rejected admission/overlap, minify/reinstall and ordinary auto-rebuild | No destructive implementation before safe ownership/transfer and protective loading are established |
| QB-Q3 | VEF giver metadata timing, external continued Replace Stuff and S028 direct composition | No inferred facade or provider compatibility; keep concrete unsatisfied bindings documented |
| QB-Q4 | Native UI/localization, costly retry playability, multiple maps and measured candidate costs | No performance/balance/gameplay acceptance from static research |

These are future research tasks, not a request for the user to explain source behavior. Review and
validation results are recorded in the sequential ledger; runtime tasks remain unchecked.
