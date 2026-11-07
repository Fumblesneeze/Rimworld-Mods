# Use Best Materials

## Identity and provenance

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, **S026 / P1-20**, Kathanon's `kathanon.UseBestMaterials`, Workshop **2901561962**.
Inspected **2026-11-02**. [Research](../refinement/2026-11-02-use-best-materials-research.md) records
the documentation-first sequence, installed 1.6 DLL SHA-256
`384B7C94E1B0B890BF52C63CE343AFF5979860D5E46EA083500396C217B1626A`, older public commit
`6e6e4d1e5f3d0ff92a03c414c9774e2823150d24`, complete class inventory and native beta comparison.
Installed source does not declare 1.7 support. BSD-3-Clause, copyright 2022 kathanon; applicable
notices/conditions/disclaimer must accompany future licensed reuse. No upstream assembly/source/art
is copied into the product. Source-code deductions are distinguished from live observations.

[Steam](https://steamcommunity.com/sharedfiles/filedetails/?id=2901561962) describes per-bill selection,
ingredient information and display sorting, and documents a source limitation with Performance Fish.
That warning does not establish a universal incompatibility for the owned feature.

## Behavior inventory

All rows: **analyzed; specified with retained gaps; not implemented**. The
[capability](../../../../openspec/changes/specify-megamod-best-material-selection/specs/megamod-best-material-selection/spec.md)
owns these observable scenarios.

| ID | Player input → result | Defaults and limits |
| --- | --- | --- |
| UB-B1 | Enable Use best by on one bill → choose supported output material by its criterion | Initially off, independent same-product bills, supported non-mixing/single output kind only |
| UB-B2 | Choose a stat or named combination → explained material order | Normal reference, lower Work to make/Flammability, other single stats higher; source armor/insulation/attack weights |
| UB-B3 | Start work with insufficient preferred stock → sufficient allowed alternative | Native search scope with enabled S024 bounded linked-group addition, physical quantities, split stacks, fixed/secondary rules and no double consumption |
| UB-B4 | Combine lock, output/ingredient preferences and spoilage → deterministic intended material | Lock, active output criterion, explicit S059 ingredient criterion, S024 spoilage among remaining ties, then distance; other slots skip output comparison |
| UB-B5 | Sort ingredient display → view changes without changing consumption | Default initially/reopened; category-local stable sort, unavailable rows retained, no checkbox/activation changes |
| UB-B6 | Hover material → relevant readable modifiers alongside description | Distinct raw/factor/offset/product meanings, no stale other-window context |
| UB-B7 | Copy/link/edit a bill → full preference follows explicit scope | Off/dormant choices included; independent copies, shared logical linked bill, atomic incompatible rejection |
| UB-B8 | Edit during collection or reorder hauling route → committed product material preserved | S014/S021/S024 physical lifecycle, current access, no discarded/duplicated cargo or work |
| UB-B9 | Lose a stat or encounter compatibility failure → retained state and explicit recovery | Active unavailable owned choice waits; Disable restores ordinary filtered work; source-present owner is explicit |

## Interactions and UI audit

S009–S012 material comparisons share facts, but their explicit Add material remains a lock; bill
automation starts off and never silently widens that lock. Their browser single-stat menu does not
gain weighted combat scores. S014 whole-list grouping and S017 complete snapshots carry independent
activation/criterion state. S021 route optimization and S024 cleaning/perishable selection preserve
committed material and quantity. The narrow S024 composition clarification is in its own draft.
S059 [Best Mix](best-mix.md) now specifies its ingredient criterion after output-material preference
and before spoilage, with Default adding no criterion. One direct selection policy preserves its
explicit Nearest/Random choices and S024's candidate addition; no internal self-patch stack.

| Source entry point | Purpose | Owned destination and difference |
| --- | --- | --- |
| Use best by checkbox/menu in bill ingredient area | Per-entity production preference | Existing contextual bill configuration, independent state, explicit availability and scope |
| Ingredient-list Sort control | View ordering | Same ingredient view; local stable sorting, reset on reopening, no gameplay mutation |
| Ingredient description tooltip | Explain material modifiers | Same native contextual surface, clear identities/units and retained ordinary description |
| Math! alternate dialog / BWM copying | Same bill workflow through optional provider | Narrow admitted adapter where supported; owned selected replacements use direct calls |

No source bottom tab, mod-settings menu or gameplay gizmo exists. Bill state and transient view
choices do not use global/save inheritance because they are not mod configuration. No new global
settings are invented for the fixed named source presets. Seven-language UI work remains unchecked.

## Contracts consumed by other mods or saves

| Binding / evidence | Preserve, adapt or unsupported |
| --- | --- |
| Installed Workshop/local scan for source namespace/package | No external consumer found in bounded literal scan; no speculative original facade |
| Source → BWM `falconne.bwm`, ExtendedBillDataStorage.MirrorBills | Outbound source adapter, not a source-type consumer. Own S017 direct copying preserves the complete choice; optional external integration must validate source/destination lifecycle |
| Source → Math! `crunchyduck.math` alternate dialog/private bill | Outbound adapter; current provider absent locally, optional exact-shape work remains open |
| Public State/comparer/interface types | Public is not proof of a consumed API. Source binary identity unsupported without demonstrated need |
| Per-bill `kathanon.UseBestMaterials`, active/deep compare and original comparator type names | Documented legacy save schema; no automatic import or binary save facade; owned identities retained independently |

Source display sorting is unsaved; its crafting comparer is saved, but installed mutable product
cache risks cross-bill/load leakage. Owned save/copy isolation deliberately corrects this. Any later
legacy importer needs explicit preview/backup, atomic application, failure restoration and verified
postconditions. No safe source-removal claim is made.

## Implementation, deviations and missing work

**Implemented: nothing.** [Proposal](../../../../openspec/changes/specify-megamod-best-material-selection/proposal.md),
[design](../../../../openspec/changes/specify-megamod-best-material-selection/design.md) and
[tasks](../../../../openspec/changes/specify-megamod-best-material-selection/tasks.md) are future work.

Deliberate corrections: independent mutable choices; stable local display sorting and UI cleanup;
clear property identity and direction; no misleading activation for unsupported recipes; output-role
ranking instead of indiscriminate secondary-ingredient sorting; complete off/absent copy semantics;
missing-stat retention and recoverable held work; explicit perishable/lock/started-iteration precedence.
Native bounded search and source weights remain proposed defaults. A custom ordered-comparison UI,
custom weight editor, whole-map optimum, mixed recipes and automatic source migration are omitted.

Original provider active: withhold owned overlap with a warning, retain owned preferences and identify
source control; do not claim source honors owned state. Other mods require semantic admission, not
an owner blacklist. Future Harmony is required when eagerly referenced; external providers optional.
No XML patch or XML Extensions dependency added. Candidate UB-T1–6 target families are in design;
individual executable guards/tests/live evidence do not yet exist. Shared comparison data and native/
shared Thing candidates replace duplicate scans; no optimization is claimed.

## Verification and acceptance

UB-Q1–4 remain in [research](../refinement/2026-11-02-use-best-materials-research.md) and
[progress](../refinement/specification-progress.md). Documentation validation/review is recorded
there. Every implementation task is unchecked. Native crafting, UI, saved-state, provider and
performance acceptance require the reviewed build and the repository's real player-workflow gate.
Static semantic review preserves research, actual cost/work/output and quality; selection can spend
scarce stock and is therefore explicit/off by default. No gameplay was launched or accepted here.
