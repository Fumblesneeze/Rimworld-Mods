## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S025 independently specifies the continued Prosthetic No Missing Body Parts behavior.
[Research](../../../mods/megamod/docs/refinement/2026-11-02-prosthetic-apparel-research.md) pins source,
installed/native assemblies, actual consumers and corrections. This is documentation, not a shipped
patch or a compatibility claim.

## Goals / Non-Goals

**Goals:** recognize real replacement coverage for clothing, preserve native union/side semantics,
retain safe current settings and support proven consumer outcomes through narrow adapters.

**Non-goals:** restoring anatomy, adding items/surgeries, changing armor/capacities, wholesale foreign
driver copies, identity spoofing, source settings migration, new tabs or a speculative caching library.

## Decisions

### Evaluate coverage without rewriting the body

Native HasPartsToWear(Pawn,ThingDef):bool checks any not-missing part in any apparel group. Preserve
positive native results and that union. Source scans only its first recognized group and generic
branches can accept an unrelated enabled arm; replace those shortcuts with actual matching group
members and the replacement's own attachment/subtree. Side-specific groups may be on fingers, so
the installed human body requires descendant traversal. Do not read translated/custom labels as
identifiers or assume two anatomical limbs are required by native apparel logic.

Resolve a supported installed added-part or explicit equivalent provider coverage for the missing
member. Confirm the attachment is actually present/usable as a replacement and is not itself lost.
A plain Hediff_Implant on a leg is not an added foot; an enabled legacy name alone cannot overrule
this. Respect custom modular missing-child semantics instead of automatically granting every declared
descendant. Alternate bodies need actual group topology and supported role mapping; the source's
mechanical name arrays are inspection leads, not a universal provider proof. Traversal must terminate
on malformed/cyclic body input and return unknown safely, without per-query exception logging.

Do not alter GetNotMissingParts, MissingBodyPart hediffs, BodyPartRecord groups, coverageAbs, capacities
or CanWearTogether. Such changes would affect surgery, damage, health and unrelated consumers.
One domain decision serves actual wear and retention through the admitted native query. S004 arrival
apparel and S024 unload/keep choices continue through their ordinary paths; neither gets a separate
definition of a restored hand. An explicit removal order is never undone by an auto-equip side effect.

### Current state and small derived data

Build a deduplicated finalized-definition catalog by (provider identity, HediffDef identity, attachment
role), using recipe applications and admitted conversion mappings. Do not rely on opening the UI.
The four settings sections share the foundation's definition/resolver/storage/renderer; no source
ModSettings static lists or manually flushed checkbox dictionaries. Stable explicit false values,
dormant save overrides and unknown content entries survive saves and catalog changes.

Use effective resolved membership sets rebuilt when the configuration/catalog changes, not four
new HashSets per failed query. Inspect current pawn health/attachment when asked; no persistent
boolean eligibility cache, no global pawn snapshot and no second map Thing cache. Shared finalized
recipe/body facts can reuse concrete existing catalog owners; no new framework before a consumer
needs one. Invalidate/rebuild derived data at load, settings changes and provider/catalog lifecycle;
never mutate Unity/Verse objects off-thread or persist live references.

The source's empty hands defaults are retained, and feet defaults intentionally become empty rather
than contain leg IDs. Arm/leg IDs are explicit in the observable spec. MuscleStimulatorLegs is retained
as a source-derived preference but grants nothing when it is the inspected ordinary implant; show
the unavailable coverage reason. Supported equivalents must be semantic, not same-label aliases.
Catalog entries represent candidate preferences; final action still validates actual coverage.

### Preserve native costs and actual lifecycle

Eligibility updates at the next query. Settings edits do not proactively scan and strip all pawns.
Native Wear and Notify_LostBodyPart consume the same result. Before implementation, prove successful
installation/child removal order, failed surgery, missing replacement, modular conversion and future
provider reconciliation. Do not substitute a synthetic direct Wear call for native user acceptance.
Preview refresh is read-only. Current queue/health revalidation must prevent stale permissions;
handling of already worn items remains native after a preference ceases to qualify.
The inspected beta JobDriver_Wear can call IApparelSource.RemoveApparel before Pawn_ApparelTracker.Wear;
Wear itself calls DeSpawnOrDeselect before HasPartsToWear can reject, and the job records forced
status without confirming actual wear. A negative final predicate alone therefore cannot satisfy
physical conservation. Admit a narrow pre-transfer/continuation guard for owned coverage loss, before
further TryUnequipSomething work and before source detachment/despawn. If a supported synchronous
provider transition invalidates coverage after transfer, preserve the actual object through the
existing safe holder/placement recovery path. Record new forced status only after actual wear.
Do not copy the whole driver, undo earlier physical drops or invent another apparel item.
Core replacement eligibility and its necessary pending-action safety form one declared admission
group; do not enable new owned-dependent wear opportunities if safe continuation cannot be supplied.
When supported revalidation withdraws that group, end already owned-dependent actions safely before
they reach an unsafe transfer. A newly available natural or other admitted replacement part still
qualifies normally; losing one contributor is not itself a reason to cancel a valid wear action.

No source save-state type is installed. Owned global/save preference records always round-trip,
including when admission is false; execution is gated separately. Removal of this exception may
make existing clothes unsupported at a later native check, so source 'safe removal' copy is not an
owned save-compatibility guarantee. Future migration requires its own backed-up atomic workflow.

### Provider outcomes without source impersonation

Integrated Implants (`lts.I`) ships a source-package-gated DLL with direct reference to the original
Mod type and a private init postfix. It injects LTS_ModularBionicArm/LTS_ModularArchotechArm and the
two Leg variants only with `ebsg.framework` active and `MSE2.Core` absent. Inspect finalized matching
Defs, EBSG Hediff_ModularAddedPart inheritance and conversion lifecycle before registration. The
inspected type derives from Hediff_AddedPart; that is useful shape evidence, not live acceptance.
Register equivalent candidates directly in the owned catalog, with true defaults only for these
four and no resetting explicit choices. Do not load its PNMBP DLL, pretend the source package is
active, or create an init method/private dictionaries merely to attract its patch. No external Def
mutation is needed for this registration; any later declarative mutation must use guarded XML.

Choose Your Outfit (`oels.chooseyouroutfit`) is a second real consumer. Its source adapter requires
the original package, reflected private mod/settings and public global List<string> fields. A facade
cannot satisfy that package gate honestly and flattened globals contradict per-save/per-role policy.
Preserve the consumer outcome through a narrowly admitted adapter at its GetExistPartsAndButtons /
selected-pawn refresh boundary, using the same actual replacement-coverage query and current setting
revision. Avoid reproducing its whole dialog or compiler-generated closure. Inspect a stable seam
before choosing the patch; if absent, leave only the display integration unavailable with explanation.
Its ordinary ListingApparelToShow already calls native HasPartsToWear. Layer/filter/preview mechanics
stay provider-owned; health/gizmo state is never mutated to make a preview appear supported.

No consumer was found for BodyPartUtils public recursion helpers or mutable HarmonyPatches body-name
lists in the bounded scan. Do not build an unsolicited source-shaped API. The two proven bindings and
their unsupported binary identity are documented, not silently described as drop-in compatible.

### Per-patch admission and dependencies

Harmony is the design-time fallback because this is a live pawn-health predicate, not a declarative
Def change. No XML patch is selected now. Harmony becomes required with the first implementation;
do not bundle it. Any later XML Extensions use declares its dependency and uses existence/shape,
canonical package and merge-preserving guards. Optional providers stay optional.

| Candidate boundary | Minimal required semantics | Fallback |
| --- | --- | --- |
| PA-T1 native body availability | Pawn/apparel signature plus actual any-group/not-missing predicate; replacement contribution changes only missing-coverage result, preserving independently meaningful vetoes | Native availability; owned exception unavailable |
| PA-T2 current settings/body refresh if a patch is needed | Actual callback/lifecycle consumed for new effective state; existence-only only for a genuine observer | Recompute from current state at use; no stale affirmative permission |
| PA-T3 optional outfit display | Exact provider package/types/selected-pawn/group mapping and refresh scope; no physical mutation | Provider's own view, explicit preview limitation; PA-T1 can remain active |
| PA-T4 optional modular coverage | Active lts.I and EBSG, MSE2 absence for this mapping, actual four Def/class/attachment/conversion semantics | Only unavailable modular registrations skipped; preferences retained |
| PA-T5 pending wear continuation/transfer | Actual ground or IApparelSource ownership, pre-unequip/pre-transfer eligibility, successful actual wear before forced status, safe interrupted physical recovery | Stop invalid owned-dependent action before mutation; preserve real target without forced/retry state |

These are design candidates, not individual completed patch target docs. Before adding any actual
Harmony/XML patch, give it a stable target document, observable owner/scenario, executable startup
admission and honest tests. A postfix that changes false to true is not an existence-only observer:
validate consumed effective semantics, relevant prefixes/skip-original and other result writers.
Do not reject all foreign patches; admit compatible unrelated changes. Unknown independent denials
must not be mistaken for missing body coverage. Prefer a narrower consumed predicate/provider seam
over pretending all false results mean the same thing. Do not rerun foreign transpilers as probes.

Every actual transpiler checks its incoming stream on every execution and returns it unchanged on
failure. Support later repatch invalidation/revalidation, idempotence, rollback of newly owned atomic
groups only, and a bounded startup warning per skipped enabled patch with target/spec/reason/lost
behavior/fallback. No assembly hash/version, local number, whole-method text or no-other-owner gate.
Source co-installation withholds overlap and dependent owned adapters without rewriting foreign
settings. Direct composition handles owned features; no internal patch stack against ourselves.

## Risks / Trade-offs

- Expanded clothing access changes which armor/jewelry a replaced limb can wear → keep actual item,
  native protection/cost/layers and explicit replacement defaults; test representative worn outcomes.
- A generic success postfix could erase another mod's veto → prove consumed effective semantics and
  isolate unknown paths; a signature-only compatibility claim is insufficient.
- Catalog writes can discard absent content → stable identities and round-trip unknown preferences;
  UI is a view, not the only initializer or source of persistence.
- Modular/race models differ → actual per-branch coverage and narrow admitted mappings, no name-only
  guarantee. Ordinary implants do not reconstruct missing digits.
- Settings-only or preview-only tests can miss native unwearing → test actual wear and successful,
  failed and removal surgery workflows on the reviewed build.

## Migration Plan

This change ships no runtime migration. Later settings use owned schemas; do not silently read or
overwrite original four-list files. Retain missing/unsupported owned entries and show admission
status, rebuilding current derived facts after load. No artificial health data is persisted. Source
removal and existing apparel retention need real proof; a future importer needs backup, atomic
conversion, rollback and verified postconditions before being offered.

## Open Questions

PA-Q1 retains corrective defaults/coverage and effects; PA-Q2 exact native health/wear/admission;
PA-Q3 actual provider/catalog/preview composition; PA-Q4 tests, UI, target docs and final native proof.
Questions remain in refinement without interrupting the sequential source pass. All implementation
and acceptance tasks stay unchecked.
