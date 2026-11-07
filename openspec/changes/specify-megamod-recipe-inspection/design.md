## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
The [proposal](proposal.md) selects inspection behavior from Nice Bill Tab as an Add bill UI reference,
not every function in its replacement Bills tab. The [research journal](../../../mods/megamod/docs/refinement/2026-11-02-nice-bill-tab-research.md)
records public documentation, installed 1.7 hashes, read-only source paths, defaults and consumers.
This change is documentation only. There are no implemented targets, accepted tests or live claims.

The proposed [Quartermaster-derived capability](../specify-megamod-quartermaster-comparison/)
owns recipe/material identity, Normal-quality comparison, finite ingredient allocation, skill status
and native bill creation. S011 will analyze the Expansion separately. S012 owns the composed browser's
search, category pills, stat menu, final gestures and enablement. Resolve these composition dependencies
before implementation; artifact completeness alone is not permission to guess their behavior.

## Goals / Non-Goals

**Goals:** readable recipe facts and requirements; explicitly chosen two-variant comparisons; ordinary
info cards and bounded ingredient navigation; no hidden production/edit side effects; safe native
fallback; shared data and preserved external restrictions.

**Non-Goals:** replacing the entire bill queue; ingredient subtask planning; global item browser;
cross-workbench pinned comparisons; upstream package/assembly impersonation; arbitrary combat scores;
temporary pawn generation for previews; automatic migration; copying source icons or assemblies.
The current contract does not promise every source stat panel, CE ammunition picker or medical effect
summary. Unknown contextual facts stay unknown or remain available in native information cards.

## Decisions

### 1. Compose one owned Add bill flow

Keep small inspection state next to the owned browser, keyed by the actual bench, recipe, style and
explicit preview material. Reference and comparison partner are distinct choices; two is the
source-derived maximum adopted by the observable spec. A third choice replaces the partner. Avoid
recreating the source's static Selections/LockedSelection lifetime across benches. Info-card closure
returns to the same state; explicit map navigation can leave the view under ordinary selection rules.

Use direct calls into the material-aware evaluator and bill-creation action. Inspection must not
receive an existing bill as an editable selection. Do not reproduce RecipeSelection.SelectBetterMaterial,
which calls SetMaterialToBill on a selected bill, nor Utils.QueryCraft, which chooses another recipe
and bench and adds a bill. The source's double-click-to-create gesture is not adopted automatically;
S012 must settle the final creation gestures with an unambiguous active variant and one action per click.

Native queue UI stays outside this slice. BWM, whole-list linking and Everybody Gets One have later
owners; their composed behavior will call owned policies directly rather than patch this UI. This
avoids importing filtered-list drag indices, queue-tail mutation and unmanaged clipboard behavior
that are unrelated to the user's chosen Add bill workflow.

### 2. Reuse one recipe and availability model

The displayed ingredients and highlight must consume the same evaluation result as S009. Do not
recalculate counts from a rotating first ingredient icon or sum all map Things without eligibility.
Native IngredientCount, ingredient-value getter, mixing and fixed-role semantics determine the
requirements for one execution. Selected Stuff applies only to the output-material role; independent
non-fixed requirements must not disappear as in the source's material-specific cache branch.

Read outputs independently of RecipeDef.ProducedThingDef, which is null for multi-output cases.
Keep declared quantities, special/input-dependent output descriptions and unknowns distinct. Do not
use the source's hardcoded smelting 25% heuristic to predict actual returns. WorkAmountTotal may
require context; unsupported evaluation is unknown rather than a guessed duration. Display all skill
requirements, not the source's mech-bandwidth-versus-skill either/or. Native special prerequisites
remain independent; the current capability does not invent automatic job readiness.

S009's shared map Thing data and native listers supply availability and navigation candidates. No
second per-mod inventory cache or unbounded repaint scan is justified. Any additional immutable
recipe/category index is shared with the browser and keyed by finalized recipe/content identity.
Invalidate display choices at the specified context boundaries, react to stock/skill/filter changes
while paused and revalidate every add/navigation action. Latency and workload evidence remain NB-Q4.

### 3. Treat statistics as facts with explicit identity and context

Use native stat applicability, Normal-quality requests and formatting where those requests have
well-defined, side-effect-free semantics; coordinate with QM-Q1. Values carry semantic identity,
unit, basis, direction and known/not-applicable/unknown state. Compare that identity, not localized
label text or a broad display enum. Nice Bill Tab gives MedicalPotency and MedicalQualityMax the
same comparison category, and pairs bars by category; our spec explicitly forbids this mispairing.

The initial supported facts are native product facts available without fabricating an actor: general
mass/HP/value where applicable, armor/insulation, declared apparel layers/coverage, declared equipped
offsets, nutrition, medicine values and ordinary weapon stats with defined abstract meaning. Do not
claim a universal complete panel. Compound, nonhuman or provider-dependent details use native info
cards or unknown until their exact semantics are specified. Preserve valid zeroes and negative values.
Coverage names come from the content's declared groups, not a hardcoded human-body silhouette.

Do not copy the source's first-verb DPS/accuracy assumptions, 12 fire-DPS addition, chance-free CE
secondary-damage sum, drug safety prediction for a fixed body size, hardcoded quality-price factors,
or generated temporary mech pawn. No single combat-effectiveness number is needed by this slice.
If later selected, each derived statistic needs its own observable meaning and scenarios first.

The source retries failed stat construction using default Stuff; this can make the displayed material
false. Isolate failures by fact and choice, retaining the requested material. A native info-card action
is preferable to making an unsupported prediction. Per-item market value is informational; omit the
source's materials-plus-work/profit-like row and show batch quantities separately.

### 4. Information and navigation use existing native surfaces

Open the recipe/item Dialog_InfoCard for the exact chosen identity; closing it must not be treated as
closing the browser. Find on map is an explicit read/navigation action, not an ingredient-eligibility
promise. Select a still-spawned, unfogged, selectable instance on the active map; forbidden visible
items may be inspected without unforbidding them. Contents that cannot be selected natively are not
secretly extracted. Revalidate before the native camera/selection jump. Absence leaves camera and map
unchanged and gives a readable message. Candidate ordering is incidental unless visible cycling is
later added; no nearest-path guarantee is specified.

All labels/actions must be localized in the seven baseline languages. Prefer native icons and text;
new raster assets require the separate repository asset workflow. S010 introduces no additional
setting: temporary selected recipes are browsing state. Any S012 persistent configuration uses the
foundation's global-default/Use global/current-save controls and main-menu guards.

### 5. Document semantic targets before writing hooks

No final hook or target ID is selected by this source study. Fill the target template and registry
before implementation, using the exact installed 1.7 game and reviewed effective patch path.
The prior native inspection used 1.7.4995 rev309; its identity is in the S009 journal, not an admission
rule. The source's whole FillTab replacement, static WinSize reflection and unconditional PatchAll
are not necessary implementation commitments.

| Intended boundary | Minimum proof to establish | Failure isolation and fallback |
| --- | --- | --- |
| Add bill entry and selected bench | Correct callable shape, live bench/recipe context and required native action path; account for skip-original prefixes and effective transpilers | Treat the enhanced entry and its add action as an atomic feature group; preserve native or active foreign entry if unsafe |
| Recipe/stat/ingredient reads | Requested identity, quantity semantics, applicable stat context and safe provider call; do not require unrelated method bodies to match | Unknown fact or unsupported choice, bounded diagnostic; native info/bill access if independently safe |
| Info-card return and navigation | Required callback existence/shape and actual selected-object/context lifetime; body semantics only where relied upon | Skip the affected navigation action, explain failure, retain inspection when safe |
| Optional provider actions | Canonical package present, consumed member/Def shapes and action semantics | Absent provider is normal; changed adapter fails closed with one diagnostic; do not omit provider actions silently |

Existing unrelated patches are allowed. No exact hash/version/pristine-body/no-other-owner test is
valid. Prefix argument changes would require semantic use/data-flow proof, not signature existence.
Group partial admission so no enhanced click creates two bills or strands the native Add bill path.
No XML edits are selected; any future content edit needs package/Def/shape guards and merge-preserving
XML Extensions operations, with XML Extensions declared required on first use.

### 6. Preserve consumer semantics without pretending to be Nice Bill Tab

The [coverage report](../../../mods/megamod/docs/replacements/nice-bill-tab.md) names concrete bindings:

- Expansion reflects and patches NiceBillTab internals and source patch methods. Its selected behavior
  will be direct owned composition after S011, not a companion patch stack against megamod.
- Colony Manager Redux conditionally loads a compiled NiceBillTab integration and uses
  ManagerJob_Production.FindManagingJob to protect managed bills. Leaving native queue controls
  intact and making previews read-only preserves the boundary; any future queue replacement must
  independently preserve this restriction. The original conditional integration will not load merely
  because megamod is present. Do not claim binary drop-in compatibility.
- Show Possible Materials is not installed in the checked Workshop path. Its public source gates on
  Andromeda.NiceBillTab and prefixes DrawBillPreview. It also has a native Bill_Production path; its
  operation with our unchanged queue still requires a present-provider workflow test. Its Ctrl/map
  indicators are not silently adopted as new megamod functionality.
- Nice Inventory/Plants/Research load-folder references concern shared/bundled dependency choices,
  not demonstrated direct calls into the new inspection capability. Respect their own loading rules;
  do not bundle FloatSubMenu.dll or spoof the source package to influence them.

Optional outgoing CE, VEF, Research Reinvented and BWM integrations need their own exact provider
research before support. Preserve native/provider-only recipe actions through ordinary fallback if
the enhanced browser cannot represent them. Merely detecting a package is not enough to claim support.
Original Nice Bill Tab co-installation is allowed as an installed source, but a conflicting active
replacement retains its UI and disables our affected enhancement with a warning. A compatible
effective path need not be rejected simply because that package or another Harmony owner exists.

## Risks / Trade-offs

- **Reference scope can expand accidentally:** source queue/subtask/stat extras are explicit omissions;
  S011/S012 must record any deliberate additions before code.
- **Misleading information affects player decisions:** unit/basis/unknown labels, exact per-execution
  quantities and native info-card fallback take precedence over attractive bars.
- **Third-party stat code may have side effects:** require safe contexts; do not create sample pawns or
  mutate live bills just to obtain a preview. Keep unsupported facts explicit.
- **Suppressed native/provider entries can remove gameplay:** test the actual original action and fall
  back to the owning UI when its context cannot be preserved. No blanket compatibility statement.
- **UI caches can be stale or costly:** shared candidate ownership, paused updates, context reset,
  action-time validation and measured combined consumers; no performance improvement claim yet.

## Migration Plan

No runtime deployment or migration occurs in this draft. Later implementation adds ephemeral
inspection state, not new bill types or imports. Ordinary bills remain native when the enhancement
is removed. Do not read/write Nice Bill Tab settings or saved state automatically. A future requested
migration requires a separate contract with backup, atomicity, failure restoration and postconditions.

## Open Questions

- **NB-Q1:** exact 1.7 selected-bench entry, supported stat/ingredient/work/info-card semantics and
  minimal target predicates; no new heuristics or silent special-recipe omission to close gaps.
- **NB-Q2:** S011/S012 composition, final comparison/alternate-material gestures, responsive layout,
  prioritized stat catalog and enablement. The two-choice/read-only proposal is explicit and can be
  revised by the composed observable spec, not by an implementation shortcut.
- **NB-Q3:** provider/consumer present-run evidence, unresolved Nice Bill Tab/FloatSubMenu reuse
  provenance and any genuinely required facade. No identity spoofing or copied code/assets.
- **NB-Q4:** shared-data costs/freshness, seven-language accessibility and actual native UI acceptance
  including save/context changes, protected bills and optional provider failures.
