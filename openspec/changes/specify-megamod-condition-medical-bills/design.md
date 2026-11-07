## Context

Sole owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), 1.7 beta.
S137 is original, with Compact Hediffs and Dubs Mint Menus as UI references. See the
[research record](../../../mods/megamod/docs/refinement/2026-11-07-condition-medical-bills-research.md).
No code, package change or live acceptance is included in this draft.

## Goals / Non-Goals

**Goals:** contextual discovery; per-instance and per-target correctness; transitive anatomy;
internal match provenance with uniform UI; native ordering; direct S007/S062/S124/S134 composition; bounded optional
exceptions; concrete user examples with the ancestor-availability discrepancy exposed.

**Non-goals:** an automatic doctor, optimum-treatment ranking, universal arbitrary-code analysis,
trial surgery, whole Mint Menus replacement, package spoofing, medical save migration, or health
changes on selection. The proposed ancestor menu extension is the only selected availability change.

## Decisions

### One owned health surface and medical catalog

Native `DrawPawnHealthCard` keeps conditions visible beside the overview/operation summary.
Mint's `DrawPawnHealthCard` postfix adds its searchable pane only on the Operations subtab and its
`InspectTabBase.TabRect` postfix widens that tab. Build the owned presentation around the same
patient/`thingForMedBills` context, with a current candidate list and source actions. Prefer reuse of
native candidate/action generation to copying its logic; if that requires extracting a native seam,
document its semantic target and retained conditions first. `GenerateSurgeryOption` preserves
confirmations, xenogerm choices, custom worker confirmations and `CreateSurgeryBill` warnings.
Do not use Mint's separate legacy `GenerateListing` direct-bill helper: the inspected live listing
uses `DrawSurgeryOption` and its native cached action instead.

The proposed pane stays reachable beside conditions and may show the Operations summary when a
condition is selected. Queued bills remain the existing stack. Compact view off must still allow
native visible-row selection; this is not a second compact renderer. At narrow sizes use reachable
scrolling/reflow within the contextual view, not another main tab. S007 supplies explicit row
instance sets and folded replacement identity. Primary click filters; a labelled log control
dispatches native event navigation. Keep provider care controls and info controls distinct.

### Match a recipe option, not only a recipe

Represent the option as current patient/bill giver, recipe and exact `BodyPartRecord` (or null for
whole body), current disabled reason and native action. Match against selected `Hediff` instances.
Deduplicate identical recipe/part pairs and union explanations across selected instances. Resolve
whole-body and descendant effects explicitly; never compare translated labels or just BodyPartDef.

Use small effect descriptors with a current-patient pure evaluator. These describe inspected worker
semantics, not speculative interfaces for every future mod. Initial known families:

| Family | Required semantic evidence | Actual match boundary |
| --- | --- | --- |
| `Recipe_RemoveHediff` | Successful effect removes matching visible Def on exact part, or all visible matching instances for whole body | Per-part mode removes the first matching instance only; whole-body mode can match all selected quills |
| `Recipe_RemoveImplant` | Inspected successful removal selects the first matching Def globally, despite part-specific candidate enumeration | Match the actual first selected instance only; do not infer exact-part removal from the menu label |
| `Recipe_ChangeImplantLevel` | First operable matching Hediff_Level on the exact part and nonzero offset | Report level increase/decrease, not universal cure; preserve actual bounds and instance selection |
| `Recipe_RemoveBodyPart` | Surgical damage creates missing part; `Hediff_MissingPart.PostAdd` restores/clears its subtree before missing descendants | Match affected conditions on target/descendants, respecting `keepOnBodyPartRestoration`; explain bodily loss |
| `Recipe_InstallNaturalBodyPart`, `Recipe_InstallArtificialBodyPart` | Successful path calls `RestorePartAndSpawnAllPreviousParts`, then replacement where applicable | Respect recursive restoration exceptions; do not generalize every addsHediff recipe to clearing injuries |

Known worker type alone is insufficient when an override or effective patch changes the consumed
effect. Admit exact effective semantics or a researched adapter; do not trust arbitrary subclasses.
Treat probabilistic successful effects as such. Do not call `ApplyOnPawn`, ingestion, surgery outcome
selection or RNG to discover relevance. No assumed generic `IngestionOutcomeDoer_OffsetHediff` exists
in the inspected build: drug effects need inspected concrete outcomes/adapters. S124 classifiers
may provide validated shared effect data, but their policy recommendations are not automatically
proof that a recipe changes this exact condition. Uncovered ingestion/custom workers remain
eligible through the bounded heuristic or excluded; the full catalog remains accessible.

### Native eligibility first, with one explicit proposed exception

Native catalog gates `AvailableNow`, worker report, ingredient hiding, current body-part enumeration
and `AvailableOnNow`; keep accepted options and visible disabled reasons. A missing drug or tech
hediff ingredient hides its recipe in the inspected game, while some other missing ingredients
produce disabled rows. The UI filter must not change these rules. No-doctor/no-bed warnings occur
after native order creation and are not generic browsing eligibility failures.

**HBF-Q2:** vanilla `Recipe_RemoveBodyPart.GetPartsToApplyOn` checks `d.Part == part` for the harmful
condition branch. A healthy Core Leg is not automatically removable for an infection solely on its
Foot. To fulfill the example, propose generalizing only this branch to descendants for present,
non-core, outside parts with `canSuggestAmputation`, with a bad visible non-injury or permanent
injury on a descendant. Keep the native recipe on the patient's repertoire, all other availability
and incoming provider restrictions. Do not union in ancestors after an external provider veto.
This is a selection-independent shared catalog extension; it must not depend on a transient UI
filter during bill execution. Recheck qualifying condition and target before queuing; later loss
of relevance follows normal existing-bill validity/player cancellation rules, not automatic deletion.

Use a separate admission group for this target-enumeration extension so ordinary filtering can
survive its rejection. Inspect effective eligibility and native bill/job validity before implementation;
if provider vetoes cannot be distinguished safely, withhold the extension. No change to surgical
damage, recipe cost, success, anesthesia, harvested products or faction/thought consequences. A
full isolated test must compare ordinary foot removal, proposed containing-leg removal and an
available leg replacement, including actual descendants lost and those consequences. This proposal
implements the user's explicit foot-infection/leg-amputation example; it is an owned behavior
extension, not source parity. The decision is retained; exact target verification remains before code.

### Bounded inference and explicit corrections

Order: validated specific correction/exclusion, admitted known family, then bounded metadata
heuristic. Initial heuristic: recipe `removesHediff` or `changesHediffLevel` names the selected Def,
with compatible exact target/whole-body scope, but the executing family lacks proven semantics.
Label similarity, `addsHediff` alone, shared part, arbitrary ingredients and source-mod name are
insufficient. Match provenance stays internal for diagnostics and later corrections. General,
heuristic and exception matches use one list/style/order with no confidence labels or toggle.

Corrections are owned descriptors beside the effect resolver. Each has stable ID, researched
provenance, canonical optional package ID where relevant, exact resolved recipe/condition and
target-scope predicates, required semantic shapes, and inclusion/exclusion plus visible explanation.
No wildcard whole-mod trust or public settings editor is needed. Exact recipe+condition corrections
precede family rules; equal specificity disagreement produces unresolved relevance and a bounded
diagnostic. A correction cannot grant eligibility. Initially no arbitrary third-party registration
API or migration contract is promised; add a narrow adapter only for an actual consumer.
Declarative owned descriptors may use new owned Defs. Any later external XML alteration must use
XML Extensions existence/package checks and merge-preserving operations with required metadata.

### Freshness and shared ownership

Keep selected instance handles and query only in the active view; clear on view/patient/game change.
Snapshot grouped membership at selection, then intersect with current visible instances; new wounds
do not become selected merely because the renderer groups them. Build static effect descriptors once
per finalized content lifetime. Reuse the owned medical catalog and S007 current-patient projection;
do not copy Mint's global `cachedPawn`, 500-draw refresh or static recipe/ingredient caches. Reevaluate
patient/ingredient/permission facts on relevant invalidation and immediately before action, including
paused changes. Do not scan all maps or add a second Thing index; use native listers/shared supply
facts only where needed. Clear all references on teardown; keep Unity/Verse access on its thread.

S062 care clicks remain care; S089 serum targeting remains its separate item workflow; S124/S134
handle order reason/ownership, pending dose and player cancellation reconciliation. A row click
does not request treatment, reset a hold or import policy. Invoking a native player order enters
the same shared medical-order path as the ordinary menu. No internal Harmony patches between these
owned features. No dose, bill-stack or condition save schema is added by the filter.

The specific quill duplicate case has a navigation branch: when a matching whole-body bill already
exists, including suspended, show Already queued and focus its ordinary queue controls without
calling the add action. Merely navigating does not transfer ownership or resume it. Editing/reordering
then uses S134's existing takeover rule, retaining automatic origin and later cancellation/suspension
holds. With no matching bill, dispatch the current native action once; its new bill is manual, with
no invented automatic origin. This explicit presentation rule avoids trying to deduplicate two
already-created bills or cancelling a player's operation afterward. If multiple pre-existing matching
bills exist, show the matching queue entries; do not merge/delete them. Recheck before either branch.
Native RemoveBodyPart inherits a null confirmation, so its ordinary choice can queue immediately;
only operations whose native workflow requires confirmation receive one here.

### Semantic admission and external providers

Prepare indexed per-patch target specs for patient/bill-giver context, visible row identity, click
dispatch, pane sizing/rendering, candidate and action generation, and the separate ancestor rule.
Specify original/effective behavior, original-skip prefixes and any relevant transpiler effects.
Transpilers validate their actual incoming stream every time and return it unchanged on failure;
do not rerun foreign transpilers as probes. Admit pane+selection+action routing atomically, roll back
only newly installed owned patches, and emit bounded startup warnings for enabled skipped patches.
Compatible unrelated modifications and benign edits are allowed; exact hashes are research identity,
not compatibility predicates. Later supported repatching invalidates/revalidates affected groups.

Detect `PeteTimesSix.CompactHediffs` and `Dubwise.DubsMintMenus` by package ID and actual active overlap.
Default to withholding the owned overlapping view/filter if no exact adapter is admitted; never
disable/unpatch either source. An unrelated Mint menu alone is not a reason to suppress a safe
medical view. Preserve native operation access and S007 fallback log clicks. Optional effect mappings
fail locally. Odyssey recipe resolution is absent-safe and requires actual recipe/condition shapes.
No upstream assembly/name facade or save import is evidenced or promised.

## Risks / Trade-offs

- Effect prediction is incomplete for arbitrary code → internal provenance, ordinary effect descriptions, exact-instance matching,
  narrow inspected mappings and a permanent route to all ordinary operations.
- Proposed ancestor target changes medical choice → HBF-Q2 decision and explicit constraints before
  implementation, then compare real costs, risk, anatomy loss and social consequences.
- Dynamic providers change eligibility/effects → semantic admission, current action recheck and
  local fallback; never guess from a familiar method name.
- Compact groups and extra controls can collide → one measured S007 layout and native pointer tests
  at narrow widths and all seven languages. Existing context menus remain independent.

## Migration Plan

No import or persistent filter state. Native bills/health remain their owners' saves. Disabling this
feature removes its view and proposed target extension, but does not delete existing bills or rewrite
health. Any future settings use the foundation common inheritance path; none are introduced here.

## Open Questions

- **HBF-Q1 resolved for drafting:** public UI references and focused source/native effects inspected;
  concrete selection/matching/ordering scenarios now exist.
- **HBF-Q2:** retain the proposed ancestor eligibility extension or change the example's availability
  expectation before implementation; confirm native and provider veto/bill validity behavior.
- **HBF-Q3:** exact effective-worker/provider and incoming consumer contracts, concrete exception
  fixtures and unsupported custom/ingestion effects; no universal compatibility claim.
- **HBF-Q4:** final pane width/reflow, keyboard access and labelled event controls through localized
  native workflows; no raster asset has been selected or accepted.
- **HBF-Q5:** focused RED/GREEN, package/review, real surgery UI/jobs, dose composition, shared-data
  cost and finalized balance/playability acceptance remain pending.
