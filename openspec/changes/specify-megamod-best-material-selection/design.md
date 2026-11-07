## Context

Sole owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
This is S026 specification only. [Research](../../../mods/megamod/docs/refinement/2026-11-02-use-best-materials-research.md)
pins author documentation, the older public source, installed 1.6 binary and current 1.7 native input.
Use the installed evidence for source behavior; no claim that the 2022 checkout builds the 2025 DLL.

The source sorts a non-mixing ingredient helper, has per-bill activation but globally cached mutable
comparers, and injects controls via ambient Window context and generic button interception. S009–S012
already own the integrated browser; S014 owns logical bill groups/started iterations; S017 owns
clipboard transactions; S021/S024 own physical collection and perishable priority. Compose these
owned functions directly, without retaining source-on-source Harmony patches internally.

## Goals / Non-Goals

**Goals:** independently persistent bill choices; useful explained comparisons; separate view sorting;
safe physical selection and copy/link behavior; minimum semantic patch admission; shared immutable
comparison data; preserved costs, quality and native search scope.

**Non-goals:** game implementation now, global optimum search, custom weight/ordered-rule editor,
mixing/surgery redesign, source binary identity emulation, automatic save import, content Def changes,
new bottom tabs, new global settings framework or a separate item cache.

## Decisions

### 1. Own immutable comparison descriptions and independent bill state

Store enabled flag and stable criterion identity on the logical bill's owned extension. Built-in
weighted recipes have stable IDs and fixed documented weights; individual stats use resolved
provider/Def identity, never translated text. Keep inactive and unresolved identities round-trippable.
The source's ConditionalWeakTable alone is not persistent ownership, and its product-keyed mutable
objects must not be reused. Copy immutable values, not a mutable comparator with origin/UI state.
The three named presets are source-derived constants, not new global settings. This remains entity
state under the user's rule. No exemption from the common settings model is needed.

Cache only immutable product/Stuff/stat applicability and values where actual calculation semantics
permit it, shared with the browser's Normal-reference comparison service. Key by finalized product,
material, stat/criterion and reference quality plus relevant content epoch. Rebuild after load/content
or provider changes; no saved live references, cross-game mutable origin or UI selection. Query-local
origin and stable native sequence break ties. Stateful/provider-specific stats require fresh supported
calculation or unavailable status, not an unjustified permanent cache. No off-thread Verse/Unity access.

### 2. One selection policy composes material and spoilage rules

Discover through native listers/ingredient search and existing shared candidate facts. Retain native
search radius, reachable candidate discovery and early stopping, with S024's explicit bounded
extension: when enabled perishable preference discovers an eligible perishable candidate in linked
storage, include eligible contents throughout that group before selection, even if the initially
found stock is sufficient. Apply the iteration's radius and actual access to every added candidate;
do not expand unrelated groups. Do not force a full map scan just
to claim a global optimum. Recheck current pawn access/reservation/provider permissions at use time;
shared Ctrl+F/hauling facts are candidates, never cached eligibility. A product comparison cache is
not a second map Thing inventory. No performance claim before combined-consumer/update measurements.

Resolve the recipe's actual output-material-bearing ingredient role first. Admit only a single
unambiguous Stuff output kind (count may be >1), supported no-mix semantics, and safe fixed/secondary
allocation. Avoid the source's all-ingredients sort: a secondary ingredient can itself be Stuff but
must not become dominant merely because of a score. Existing recipe checks continue governing
fixed ingredients, per-material counts, whole-stack recipes and physical allocation. Probe ambiguous
roles and overlapping ingredient filters explicitly; do not overclaim that every MadeFromStuff
ProducedThingDef is supported. The original missingIngredients/count bookkeeping is not rewritten
without a separately specified observable need.

Policy order for output material: hard recipe/filter/explicit-lock constraints and the S014 committed
iteration; active criterion; S059 explicit ingredient criterion; S024 perishable/whole-hour key for
remaining ties; horizontal distance; native discovery sequence. S059 Default adds no criterion;
Nearest/Random remain below the output-material choice. Other slots skip the output-material
criterion but keep explicit S059 before ordinary/S024 ties. Native medical choices and
surgery remain unchanged. Score once per eligible material where supported, then select actual stack
quantities through the admitted native path. Never sort a shared list in place outside its owned
selection context. Missing/NaN/infinite/throwing stat results are unavailable, not zero; preserve saved
intent and stop new active automatic work rather than quietly select an invented winner.

The narrow S024 clarification is made in its existing draft so there is one consistent composition.
S059's [ingredient-preference draft](../specify-megamod-ingredient-preferences/design.md) now owns
that explicit ingredient contribution and its attempt/quantity lifecycle, without another sort.

### 3. Preserve committed material through UI and hauling

Browser list sort, browser material preview/lock, bill crafting preference and ingredient-view sort
are four distinct intents. The browser's Add keeps its selected material lock and new automatic
preference starts off. Later enabling the preference only ranks inside allowed choices. Do not
propagate weighted presets into the browser's prohibited aggregate-score menu. UI help states native
search scope and reference-quality limits.

Use the existing bill-context view composition for activation, criterion, unavailable explanation,
display sort and tooltips. A per-window view model starts Default and is discarded on close. Sort
copies of displayed category rows stably; never rewrite ThingCategoryDef lists or ingredient filters.
Preserve descriptions and distinguish property identity/units instead of merging translated labels.
Avoid global interception of the next two Widgets.ButtonText calls or a leak-prone static curGUI.
Any optional external-window context is scoped and restored on exceptional exit. Seven languages
must cover every label, direction, weighted explanation and failure reason in the runtime slice.

S014 extension snapshots and S017 clipboard transactions own copying/validation. Off/absent source
must explicitly clear destination activation; clone/paste retain dormant criteria without aliasing.
Unsupported consequential state rejects the entire transaction. New policy governs new iterations;
the existing iteration token/snapshot keeps original material through batch pickup, housekeeping,
unfinished product and reload. It retains current physical permission checks and cancellation rules.
Maintain native dominantIngredient independently of traversal order so GenRecipe creates the
intended Stuff/color and normal quality/output. Never fabricate a result to repair a selection.

### 4. Patch admission and precise fallback

Before implementation, instantiate separate stable-ID documents under
`mods/megamod/docs/patch-targets/` and index them; the candidates below are a design inventory, not
already-existing executable validators. Reconfirm exact 1.7 signatures. Prefer owned extension points
over additional Harmony patches; remove candidates made unnecessary by owned code composition.

| Candidate ID | Target / minimum semantic predicate | Failure and atomic boundary |
| --- | --- | --- |
| UB-T1 | WorkGiver_DoBill no-mix selection and its caller: alreadySorted actually controls only preliminary sorting; supplied physical sequence feeds counted permitted ingredient choice; fixed/variable roles, quantity and dominant material remain provable | Whole automatic-selection admission group; no prefix argument mutation on signature alone. Unsupported active preferences wait, off bills remain native |
| UB-T2 | Native bill persistence/copy lifecycle where an owned bill extension cannot supply it: correct bill/context, load/save stages, destination and successful clone; original-skipping prefixes considered | State read/write must remain independent of selection admission. No activation without durable storage; preserve unknown records. Failed copying rejects operation atomically |
| UB-T3 | Contextual bill/filter UI only where owned UI composition is unavailable: correct bill/filter/view lifetime, safe layout and descriptions, exceptional exit restoration | Disable only affected enhancement; retain recoverable activation/disable controls in owned context, otherwise new active work remains held |
| UB-T4 | Optional BWM `ImprovedWorkbenches.ExtendedBillDataStorage.MirrorBills` source/destination and preserved-product semantics after a successful mirror | Optional adapter only; validate full state transfer or reject unsupported paste before mutation. Owned S017 uses direct calls |
| UB-T5 | Optional Math! recognized dialog, current bill field and successful rendering lifecycle | Optional view adapter only; native owned bill view remains usable; package absent is inactive, not a failure |
| UB-T6 | Any required data-invalidation or started-iteration hook: actual notification/identity and causal completion/transfer path consumed by shared service | Own only required hooks, preserve shared data fallback; no source-style duplicate map index |

Check original/effective semantic flow including skip-original prefixes and transformations. Another
owner is neither automatic incompatibility nor proof of compatibility. Every transpiler validates
its actual incoming stream on every invocation, leaves it unchanged on false/unknown, and never
speculatively reruns foreign transpilers. Tolerate harmless instructions/locals/offsets. Support later
repatch invalidation and group revalidation; roll back only newly installed owned group members.
Log one bounded startup warning per skipped enabled patch with package, patch/spec, target, failed
predicate, unavailable behavior and fallback. Validator exceptions/install failures fail closed.

Selection admission must be paired with an independently admitted way to hold affected new work
and expose Disable preference. If that safety path cannot be established, do not advertise this
feature as supported or silently run a persisted active bill natively: retain its state and mark
that bill suspended before normal scheduling; preserve its prior suspension so explicit recovery
does not resume a bill the player had already paused. This recovery is bill-state handling, not
unpatching a foreign provider. Existing committed work follows S014/physical recovery; if it cannot
continue safely, hold it without deleting cargo/work or creating replacement ingredients.

With original `kathanon.usebestmaterials` active (canonical case-insensitive comparison), withhold all
owned overlap; show original-provider control and retain owned choices without implying the source
reads them. Do not mutate the original's settings or force it off. For other providers, require
actual semantic compatibility rather than refusing every foreign patch. The author's Performance
Fish warning motivates UB-T1 effective-flow tests, not a global provider blacklist or forced config edit.

### 5. External identities and migration boundaries

The bounded source-identity scan found no external type consumer. No facade assembly/package spoof
is planned. BWM/Math are outbound integrations; their package, consumed type/member and lifecycle
must all resolve before a narrow adapter activates. Exact current Math/Fish are absent locally;
their live compatibility remains open. BWM's installed four-argument MirrorBills is recorded in
research, and its selected replacement uses direct owned copying rather than self-patching.

The source's per-bill node and deep comparator type names are not automatically compatible with
owned persistence. A later importer must preview scope, back up original inputs, validate every
criterion, apply atomically, restore on failure and verify bill postconditions before writing. No
such importer or automatic source-file rewrite is part of this change. Unknown owned criteria remain
losslessly stored during temporary provider absence even if the feature cannot activate.

## Risks / Trade-offs

- Native early success can miss a better distant material outside the enabled S024 group addition → explain bounded scope; broader search is
  a later decision with cost/reachability tests, not an optimization claimed now.
- Weights combine unlike raw units → retain source values visibly, no effectiveness promise or
  unrecorded rebalance; custom weights remain outside scope.
- Fixed/secondary material roles and whole-stack providers differ → admit only demonstrable roles,
  retain native quantities and explicitly test actual product material with batch route changes.
- Stable data caches can outlive content/state → share immutable facts with correct epochs, discard
  view state, recalculate sensitive values and keep current permissions out of caches.
- Source co-installation uses different preference state → explicit owner indication and retained
  owned data, no claim of synchronized or automatically migrated source choices.

## Migration Plan

No runtime deployment in this documentation change. Implement owned state first, then vertical
selection/UI slices and optional adapters with target specs and focused RED/GREEN. Retain unknown
data through disabled admission. Rollback disables owned enhancements and conserves physical work;
active preferences require explicit recovery before native fallback. Do not publish safe mod removal
or source-save migration until separately verified. No user normal save/config is edited now.

## Open Questions

UB-Q1–4 in research/progress retain broader search and criterion choices, exact beta material role/
quantity/recovery proof, external adapters/migration, and implementation/native acceptance. No open
question authorizes guessing a missing provider API. All implementation tasks remain unchecked.

## Verification and semantic review

Use host Unit rules for score direction, weighted results, stable ties and snapshot independence;
Defs only for honest initially empty lightweight fixtures; Harmony only explicitly owned targets
with cleanup. Real recipes/active providers/loaded Defs require isolated exact-mod game runs.
Final reviewed-build workflows must show choosing a bill criterion, actual worker pickup/crafting
and resulting Stuff/quality/cost; separate display sorting; two same-product bills; off/absent copy;
linked edit mid-collection; reload; lost stat; compatible changed targets and false admission.
Retain native action → observed result evidence, screenshots inspected personally, exact hashes,
mod order, normal-config hashes and cleanup through the Gateway. Logs and synthetic state queries
are supporting evidence only. No game launch is needed to accept this prose draft.

Balance/playability pass: no new research/content/quantities/free work or forced quality; a preference
can spend scarce higher-stat stock and must remain explicit/off by default. Normal comparison,
source weight units, insufficient-best fallback, fixed components, source-present behavior and
recoverable held work are the important boundaries. Finalized values and real crafting verification
remain implementation evidence, not claims made by this static review.
