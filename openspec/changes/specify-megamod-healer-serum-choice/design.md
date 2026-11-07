## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S089/P2-18. Source `syrus.hmschoice`, Workshop2714095848. Included1.6 source and selected DLL
agree. [Research](../../../mods/megamod/docs/refinement/2026-11-05-healer-serum-choice-research.md)
pins complete runtime analysis, native contracts and actual external consumers. Documentation only.

## Goals / Non-Goals

**Goals:** explicit condition choice after ordinary preparation; one paid cure; safe cancellation,
staleness and duplicate handling; retain native administration and external automatic-healing contracts.

**Non-goals:** new healing items or powers, early pre-job selection, balancing item prices, Anomaly
abilities, mutagenic/resurrector serums, multiplayer support by implication, source-package spoofing,
arbitrary callback rollback, broad HealthUtility interception or another cache/settings framework.

## Decisions

### Keep the native action, separate actor and patient

Inspected1.7 direct use is `JobDriver_UseItem` -> `CompUsable.UsedBy`; medical administration is
`Recipe_AdministerUsableItem.ApplyOnPawn` -> the ingredient's CompUsable with the patient. The
native recipe's ingredient-consumption override is empty: the item's destroy effect owns the dose.
Core explicitly supplies AdministerMechSerumHealer to both Animal and Human races; do not copy an
obsolete animal recipe patch or assume RecipeDefGenerator creates this usable-item recipe.

Use narrowly owned usable/effect adapters and an admitted recipe worker where necessary. Share one
eligibility policy between command availability, bill availability, chooser and final validation.
Retain native flesh/mutant/planet-layer, required-user-condition, power and actor/job constraints;
evaluate physician movement/manipulation and item access on the administering actor, not the patient.
The source's `CanBeUsedBy(patient, true)` recheck differs from native final use's
`ignoreReserveAndReachable:true`; it is not a safe prescription for the doctor's path.

The native recipe currently uses TryGetWorstHealthCondition for availability, which is not identical
to the chooser's eligible set. Any recipe adjustment must be scoped to these admitted item recipes,
preserve base medical restrictions, and never broaden every usable/ingestible medical recipe.

### One selected native cure with explicit transaction stages

Keep the source's after-preparation timing. Use a small feature-local pending-treatment record tying
game/map identity, patient, exact physical dose, selected Hediff/part and completion state together.
No second pending choice for the same patient or dose; separate patients/doses remain independent.
Hold custody/reservation through the choice, release on every terminal path, and never rely on a
captured stack reference remaining usable after the native job has ended. Split/merge/transfer and
medical ingredient ownership need actual native tracing before choosing the custody representation.

Refresh/revalidate eligibility at confirmation on the game thread; reject changed patient, item,
part or condition, and never redirect to the next/worst condition. No choice replay across saves.
On save or teardown cancel before serialization and return/retain the exact unused physical unit
through ordinary holder/placement paths. Do not construct a replacement copy or persist raw UI refs.
Cancelled once-only medical bills must require a fresh order rather than reopen in a retry loop.

Stage the supported treatment as validated -> begun -> completed/failed. Before begun, all rejection
and cancellation paths preserve the dose and health state. Once begun, reserve/commit the one physical
cost exactly once and apply the selected cure once; native cosmetic feedback occurs once after the
corresponding action. An unexpected exception after health side effects is not safely reversible:
record/report the known dose outcome, suppress automatic repeat/refund, and do not claim success.
Do not infer that a thrown provider callback means nothing happened. Exact ordering and safe item
custody are HMSC-Q1 implementation obligations, with fault-injection tests, not accepted functionality.

Use ordinary HealthUtility cure semantics: a non-missing Hediff goes through native Cure; an eligible
missing part uses native restoration. Cure can remove all same-Def instances when
`cureAllAtOnceIfCuredByItem`; RestorePart traverses descendants and retains
`keepOnBodyPartRestoration` conditions. Show that scope before Confirm. Do not reimplement a
general health engine, remove every Hediff by hand, or assume one row implies one removed object.
Source eligibility is Visible && isBad && everCurableByItem && !FullyImmune(), plus no directly
added part on the missing part/ancestors and no missing parent. No new severity threshold.

### Preserve a proven native component contract

Current public More Faction Interaction source creates a temporary MechSerumHealer and calls
`TryGetComp<CompUseEffect_FixWorstHealthCondition>().DoEffect(pawn)`. Its event chooses the sickest
caravan member and does not use CompUsable or a carried serum. Source HMSChoice replaces that
component with an unrelated type, then compensates using an unsafe shaman transpiler.

Prefer an owned effect that remains assignable to native CompUseEffect_FixWorstHealthCondition.
Keep ordinary `DoEffect(Pawn)` as native automatic worst-condition healing; expose a separate owned
selected-treatment operation called only by the owned usable flow. Do not store a mutable
HediffToHeal field which changes the next external call. Preserve native ordinary CanBeUsedBy for
external callers; the owned workflow evaluates its selected-condition availability separately,
including remaining effect restrictions. Verify actual generic component lookup in the native game.
This is a consumed native subtype contract, not a fabricated HMSChoice facade. If it proves unsafe,
revise the design and add a narrowly guarded provider adapter before enabling the dependent Core
mutation; do not knowingly leave MFI broken while calling its adapter independently optional.

The current MFI provider identity is `mlie.morefactioninteraction`; other forks are not implicitly
supported. Preserve its payment/event/pawn selection and final world-object removal. The proposed
native subtype strategy needs no MFI Harmony patch when its consumed contract is retained.

Multiplayer Compatibility is an actual HMSChoice consumer: package match, reflected
HMSChoice.Dialog_HediffSelection, SelectedHediff field, Close(Hediff), DoWindowContents and first-window
lookup. It will not activate for our identity. Do not impersonate that package or inherit its ambiguous
first-window selection. Owned chooser is inactive in multiplayer until explicit synchronized commands,
patient/dose identities and multi-client acceptance exist; native automatic behavior remains available.

### XML admission, effect ordering and optional content

Core source mutations target MechSerumHealer's usable compClass and worst-healing effect compClass;
the same shapes are conditionally changed for ArchotechishMedicine and HealingPotion. The source's
unguarded add can duplicate an existing usable compClass and its optional checks are Def-only.
Use XML Extensions guards for exact Def/node cardinality, expected assignability/semantics and current
component values; set/merge only the intended scalar nodes, preserve every unrelated comp/field/list,
and treat already-owned values idempotently. Any recipe worker change is its own guarded scalar.
Atomic admission must cover the usable, selected effect, dose ownership and recipe paths together.
Preflight before any edits; no half-converted item. Do not use runtime Def mutation to hide XML failure.

Optional item providers require canonical package ID via XmlExtensions.FindMod packageId=true AND
the required item/content shape. Archotech/medieval forks are not proven by a shared Def name;
HMSC-Q3 retains exact fork identity/source/effect and installed-binary work. No optional item adapter
is claimed ready to activate. Their absence is expected; unsupported present item keeps its provider's
behavior and gets one bounded explanation. Independent Core use stays available.

Enumerate admitted effect order before dispatch. Core has selected healing, play sound and
DestroySelf; source sorts OrderPriority descending but catches around the entire loop, unlike native
per-effect handling. An unknown mutation before/after selected cure cannot be silently dropped,
rerun or described as transactional. Preserve understood additional effects; otherwise withhold the
affected item conversion and explain the unsupported contract. Never run both automatic worst cure
and selected cure, or both native ingredient consumption and owned dose expenditure.

Each actual XML/Harmony hook needs a stable target document, executable startup predicate, focused
tests and eventual native evidence. Validate effective method/comp semantics, relevant skip-original
and transpiler effects, resource flow and cardinality, not exact hashes or absence of other owners.
Accept benign edits; any transpiler validates incoming instructions on every invocation and returns
them unchanged on failure. Do not reexecute foreign transpilers speculatively. Repeated init and
supported later repatches revalidate; failures roll back only newly added owned hooks in the atomic
group. Per skipped enabled patch log package/patch/spec ID, target, failed predicate, lost behavior
and fallback once at startup. No per-job flood or unpatching foreign owners.

Original `syrus.hmschoice` suppresses owned overlap before XML mutation. XML Extensions becomes a
required dependency with first use; Harmony is required only if eagerly used by actual hooks. No DLC
required. No foreign DLL/private Harmony/Gateway shipment or dependency on another repository mod.

### UI, shared state and composition

Use a contextual translated window from native use/Health operations. Source columns are part,
condition and severity; retain native health priority/colors and scrolling, add actual item name and
cure-scope explanation. No bottom tab, debug placement or mod setting. A chosen patient condition
and pending physical dose are gameplay state; global/save inheritance is inapplicable. No source
settings or persisted selection require migration. All new strings use seven baseline languages.

Derive the short current list from the patient's live health state, not a persistent per-source
cache. Native holders/listers and the common Thing index can locate candidates, but actual custody,
reservations and patient eligibility must be checked through native paths at action time. No extra
map-wide scanner or per-tick treatment loop. No optimization claim without measurement.

Smart Medicine, Drug Response and Priority Treatment may affect the ordinary medical scheduling
path only through their separately specified policies. Compose owned policy directly, never patch
our own feature. S085 partial ingestion is not applicable to this usable one-dose item. Do not add
automatic rare-item authorization or apply selected cures through the general HealthUtility entry.

## Risks / Trade-offs

- Deferred selection spans native job/ingredient cleanup -> verify native holder and bill lifecycle;
  preserve after-work timing, but do not promise reliable cancellation from the source snapshot alone.
- Third-party removal callbacks can fail after side effects -> preflight supported semantics, retain
  one completion record and report interruptions; do not claim arbitrary rollback or free retries.
- Shared component consumers -> preserve native assignability/direct-call meaning, test MFI present
  with the exact provider, and withhold mutations if the consumed contract cannot be retained.
- Optional source pages and forks drift -> identity/source pins are research evidence, not runtime
  equality guards; unsupported forks remain off without disabling independent content.

## Migration Plan

No automatic source settings/save import, health-point rewrite or pending-dialog migration. Existing
health and item identities remain native. Future enable/disable handles pending choices before
teardown and never edits the user's normal save/configuration. Source co-install keeps its authority.

## Open Questions

HMSC-Q1–5 in the ledger retain custody/failure and scope UX, exact native/effective admission,
optional providers and external consumers, seven-language/native acceptance, and finalized balance.
These are implementation/research obligations, not requests for the user to explain source behavior.
