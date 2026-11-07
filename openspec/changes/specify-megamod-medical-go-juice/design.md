## Context

Sole owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S086 is a specification-only independent replacement. The original contains one XML operation:
replace `GoJuice/ingestible/drugCategory` with Medical. No C# assembly, settings, translation catalog,
save component or external runtime contract exists in its inspected distribution.
[Research](../../../mods/megamod/docs/refinement/2026-11-05-go-juice-medical-research.md) records hashes
and exact inspected native paths. The author also describes no binges and an inventory workaround;
those claims need separate scrutiny on1.7.

## Goals / Non-Goals

**Goals:** preserve the existing item and change its medical classification; retain all unrelated
effects/content; guard actual XML target semantics; explain unsupported content; compose directly
with the already-owned drug/medical/inventory features.

**Non-Goals:** chemical rebalance, free supplies, making a tending medicine, a new policy engine,
special Hussar logic, universal binge/joy exclusion, source package spoofing or save migration.

## Decisions

### Narrow declarative scalar change

Use XML Extensions Conditional for actual content/node predicates, FindMod with packageId=true for
original-provider selection or any future external target, and Log.warning for bounded unsupported
branches. Inspected operations exist in the installed reference; verify exact active1.7 resolution
before implementation. A narrow vanilla scalar replacement can perform the admitted mutation.
XML Extensions is required from first use with its Harmony dependency graph. No runtime Def rewrite,
custom Harmony patch or C# status layer solely to mirror this XML change.

This is an intentional replacement of one scalar classification: a drug cannot simultaneously have
Hard and Medical as its category. Merging is not meaningful for that field. Do not replace ingestible,
comps, recipe lists, item categories or parent definitions. Keep all unrelated children and provider
entries. Do not append a second drugCategory or add a missing field to a malformed target merely to
make the patch succeed. Already Medical is success without rewriting or duplicating nodes.

Before implementation create the stable target document/index entry for candidate
`megamod.go-juice.medical-category`, linked to the owning scenarios and executable XML checks.
Require one intended GoJuice ThingDef, one supported ingestible/category node, a recognized drug
classification and the expected go-juice chemical identity/compatible drug structure. Validate what
the operation consumes; do not demand exact whole-Def text, unchanged recipe/stats, exact game/DLL
hash, no other mods or specifically the original Hard value. Compatible Social-to-Medical and
already-Medical input need not be rejected simply because another provider ran first.

False/unknown/ambiguous predicates and operation failure must leave no partial mutation. Emit one
bounded startup warning for each skipped enabled patch with package/patch/spec/target, failed
predicate, lost Medical behavior and fallback. Preserve other providers' content and never remove
their patches. Original `oriom.gojuicemedical` owns overlap when selected; deliberate provider choice
is not a failed enabled patch. Check actual resulting category before claiming its behavior.

GJM-Q2 must settle finalized/load-order checks and diagnostics using supported XML Extensions and
the existing owning admission mechanism. A predicate seen before later XML changes cannot prove final
semantics. Do not add imperative category enforcement or a bespoke C# framework just to hide that
gap; inspect final Defs and real behavior with competing category edits before acceptance.

### Use existing native behavior, with explicit limits

Inspected native1.7.4998 `ThingDef.IsDrug` remains true for Medical, `IsNonMedicalDrug` becomes false,
and `IsPleasureDrug` depends on positive joy, not Medical versus Hard. `CompDrug.PostIngested`
continues generic drug history, ordinary chemistry and combat-drug timestamp while skipping the
recreational timestamp/history and hard-drug history conditions. MedicalOnly ideology listens to
the recreational event. Generic all-drugs events still exist; no thought deletion or broad CanTakeDrug
bypass is implemented beyond the native category consequences. Native scheduled/carry-policy methods
call CanTakeDrug only for IsNonMedicalDrug, so Medical can pass those routes despite generic drug
willingness refusing direct use. Current JobGiver_TakeDrugsForDrugPolicy and
JobGiver_MoveDrugsToInventory retain that distinction and do not add a general willingness veto.
Direct inventory use calls CanTakeDrug, whose active matching dependency exception precedes its
teetotaler/generic ideology checks. Keep these observable route differences, including generic
consequences after scheduled consumption; do not add an unintended universal prohibition.

Current `FoodUtility.WillIngestFromInventoryNow` accepts `CanTakeDrug` directly, including a nonfood
medical drug. Drafted inventory gizmos enumerate CompDrug items and call that native route; multiple
drugs get a native choice menu. This already covers the author's old Common Sense workaround on the
inspected beta. Retain S022's initiating-purpose checks and settings; do not duplicate the old prefix.

Do not infer no-binge from the enum name. Native CanBingeOnNow can use Any, ChemicalDef.canBinge
defaultstrue, GoJuice's chemical does not override it, and actual target IncludedIn compares enum
ordering (Medical is below Social/Hard/Any). Addiction-driven and fallback Any selection therefore
invalidate the blanket guarantee. No extra binge veto is introduced without GJM-Q1's separate
observable intent/scope. Likewise retain existing joy amount/eligibility instead of silently making
the drug non-recreational in every possible selection route.

### Direct composition and presentation

Keep original GoJuice/Chemical identities and policy entries; only category changes at load. S029
drug-policy maintenance, S022 Common Sense, S062 personal medical supplies, S085 partial ingestion,
S124 Drug Response, S132 RPG inventory and S134 Priority Treatment consume the final owned/native
category directly. No self-Harmony stack, new map scan/cache or dose/permission ownership here.
Medical classification cannot satisfy S085's unresolved risk/event gates. No runtime dependency on
other repository mods.

There is no actual configuration introduced: fixed selected behavior is not an exception to the
common setting model. If a future category preference is requested, specify its effect timing and
scope before adding it; do not mutate globally shared Def state differently for two saves. No source
bottom tab exists; ordinary injection remains contextual. Any added explanations follow all seven
baseline languages. Source preview art is not reused or modified.

## Risks / Trade-offs

- 'Medical' can imply harmlessness → explicitly preserve addiction, overdose and generic drug rules.
- Public no-binge claim differs from native code → document it, preserve actual category behavior,
  keep stronger exclusion open rather than silently changing mental breaks.
- Other providers change the same scalar → deterministic original-provider selection, semantic
  node guards and final outcome verification; no broad list overwrite or exact-text rejection.
- Existing saves contain old thoughts → preserve them; changing category affects new actions, not
  historical memory repair.

## Migration Plan

No source-specific save data or settings to import. Existing item/chemical identifiers stay intact;
the applicable category is resolved at load. No save rewriting or retroactive history edits. Any
later source migration request needs backup, atomicity, restoration and postcondition requirements.

## Open Questions

GJM-Q1: whether a separate explicit go-juice binge prohibition is wanted, and its precise scope;
current draft deliberately does not add one. GJM-Q2: exact1.7 XML Extensions/dependency selection,
effective target shape, competing edits and finalized diagnostics. GJM-Q3: native workflow and
composition evidence for medical-only/generic prohibitions, dependency, drafted inventory, partial
doses, localization and unchanged balance. No gameplay accepted by this draft.
