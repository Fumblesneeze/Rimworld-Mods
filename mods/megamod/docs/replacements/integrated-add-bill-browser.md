# Integrated Add bill browser

## Identity and provenance

- Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
- **Origin: original**, S012 / P1-09. Provenance is the confirmed
  [interview](../refinement/2026-11-01-handwritten-notes.md), not an invented fourth source mod.
- Upstream package/Workshop ID, assembly version/hash and reuse license: **not applicable** to this
  original composition. Reference slices retain their own exact source identities and research.
- [Refinement](../refinement/2026-11-02-integrated-add-bill-browser-refinement.md) records the confirmed
  requirements and read-only native category/stat/entry observations on 2026-11-02. These are source
  facts and proposed design choices, not observed game behavior.
- Owning change: [specify-megamod-integrated-add-bill-browser](../../../../openspec/changes/specify-megamod-integrated-add-bill-browser/).
  Observable [capability](../../../../openspec/changes/specify-megamod-integrated-add-bill-browser/specs/megamod-integrated-add-bill-browser/spec.md).

The original request fixes contextual Add bill, search, product-category pills, relevant common-first
stat sorting, Normal quality, the available/all-material checkbox polarity, selected material in the
bill and ingredients plus one colonist meeting all skills. Matching fields, pill OR/ancestry, the
initial property priority, no-stat unrestricted planning and exact gestures are explicit proposed
details. No further interview is required to continue drafting; remaining questions are AB-Q1–4.

## Behavior inventory

| ID | Input/action → observable result | Defaults and edges | Owning requirement / provenance | State |
| --- | --- | --- | --- | --- |
| AB-01 | Bench Add bill → one rich contextual browser | Same native choices/styles; no global window or new main tab | “Open one rich browser from the bench's Add bill action”; confirmed interview/native entry | Specified only |
| AB-02 | Enter text → matching recipe/style/all-output names | All terms, case-insensitive; no guessed ingredient/ID search; clear empty results | “Search matches visible recipe and output names predictably”; proposed matching detail | Specified only |
| AB-03 | Select product pills → category/ancestor matches | Multiple selected categories OR; search AND; same-name disambiguation; Other recipes | “Category pills follow actual products and their hierarchy”; native categories plus custom policy | Specified only |
| AB-04 | Choose relevant stat → ranked Normal product/material facts | Recipe order initially; explicit common-property priority; unknowns/multiple distinct outputs unranked | “Stat choices follow the filtered products with common properties first”; S009 comparison | Specified only |
| AB-05 | Toggle material mode or inspect without a sort → explicit material meaning | Following previews track direct control changes; explicit previews/pinned comparisons stay fixed; Use list choice resets an override; no-stat Any allowed material | “Material mode and no-stat planning remain explicit”; interview plus proposed no-stat policy | Specified only |
| AB-06 | Click/Compare → read-only detail/reference/partner | Double-click does not add; two compared choices maximum; partner replacement | “Inspection and comparison gestures never queue implicitly”; S010 | Specified only |
| AB-07 | Main/row Add → one identified native bill | Same shared action; browser remains; native limits/warnings; successful repeat deliberate | “Every add action identifies one variant and commits once”; S009/S011 | Specified only |
| AB-08 | Visible alternate action or right-click → material list → explicit bill | Cancel harmless; unavailable planning allowed; no global preference or preview change | “Alternate-material addition is explicit and discoverable”; tentative interview gesture made explicit | Specified only |
| AB-09 | Filter/hide/update → consistent state and shared results | Removed choice/stat clears with explanation; paused refresh; no silent material change | “Compose visibility, availability and refresh without losing intent”; S009–S011 | Specified only |
| AB-10 | Set enablement or change context → correct feature/lifetime | Browser on; common inherited settings; fresh state resets; card/popup return preserves context | “Context and configuration have distinct lifetimes”; foundation | Specified only |
| AB-11 | Narrow view or unsupported integration → reachable controls/safe ordinary route | Seven languages, bounded startup warning; compatible foreign patches permitted | “Responsive access and safe fallback preserve gameplay”; repository/UI rules | Specified only |

## Interactions and composed result

| Referenced owner | Owned semantics used here | Internal composition |
| --- | --- | --- |
| [Quartermaster-derived comparison](quartermaster.md) | Recipe/style/material identity, Normal facts, finite one-execution supply, one-colonist skills, safe native creation | One evaluator and one add action; no separate recommendation window/cache |
| [Nice Bill Tab inspection](nice-bill-tab.md) | Read-only requirements/products/stats, comparison and native info/navigation | One context and explicit active variant; no queue/subtask overhaul |
| [Expansion list controls](nice-bill-tab-expansion.md) | Hiding/recovery, optional row Add, shortage decoration and common settings | Direct policies; no Harmony against owned UI |
| Later whole-list linking/BWM/Best Mix and bill owners | Separately selected queue/configuration/propagation rules | Preserve native boundary; no duplicate linked-bench add loop in this browser |

No source mod becomes a required dependency. No unspecified source extras are smuggled in by naming
the UI references: global loadout sets, equipment assignments, generated subtasks, clone import/export,
tag-generated bills and approximate combat scores remain outside this original browser.

## UI audit and relocation

| Entry/control | Classification | Destination and reason |
| --- | --- | --- |
| Native workbench Add bill | Contextual game mechanic | Rich browser at that bench, no new bottom tab |
| Search, pills, sort and Use all materials | Temporary local view choices | Top of contextual browser; checkbox beside sort, no mod settings or global toolbar |
| Recipe preview, Compare, preview material | Temporary inspection choices | Contextual detail/comparison controls; no existing bill edits |
| Add and alternate material | Explicit production planning | Main action, optional row action and discoverable popup; right-click is a shortcut |
| Hiding scopes/records | Entity/gameplay state | S011 contextual controls and save scope, not inherited configuration |
| Browser/comparison/decoration enablement | Actual configuration | Common Crafting / Add bill settings, ordered browser, comparisons, row Add, shortage cues |

Browser-enable default is on and resolves next opening. Comparison enablement controls stat sorting,
best-material optimization and two-choice comparison, leaving single inspection and explicit planning
usable. Row Add/shortage settings retain S011 defaults/effect timing. Global default, save override,
Use global, dormant value and main-menu guards all use the common path; no scope exemption. Closing
or changing bench/map/save resets browsing state, while ordinary info-card/popup return preserves it.

An inspected implicit list choice follows deliberate stat/material-mode changes so main Add cannot
quietly retain an older unrestricted or material choice. Explicit preview overrides and pinned
comparisons are visibly distinct and remain fixed; Use list choice returns a single preview to the
current automatic choice, or Any allowed material under Recipe order. Background freshness changes
still require review before substituting the inspected material.

## Contracts consumed by other mods or saves

This is a new original UI with **no existing incoming megamod API or saved browser schema**. No binary
drop-in claim applies. Native recipe/style availability, MakeNewBill behavior, ordinary bill limits,
warnings, tutorial notifications and protected/provider queue actions are boundaries to preserve.
The inspected native path and source consumers are recorded in the three source reports; they do
not become automatic compatibility claims for this composition.

| Contract | Preservation / uncertainty |
| --- | --- |
| Existing ordinary bills and their provider restrictions | Browser never edits them; only one explicit native add action creates a new bill |
| Source package/assembly/reflection identities | No impersonation or copied assemblies; actual consumer support needs a narrow proven adapter |
| Optional special recipe/stat provider | Canonical package and shape/semantics checks, working ordinary route on unsupported enhancement; present-provider live evidence required |
| Saved settings | One new owned enablement value through common defaults/inheritance; no source settings import |
| Browser state | Temporary, no migration or persistent Thing references |
| Owned hiding and created bills | S011 saved scopes and native bill persistence remain authoritative |

There is no automatic migration, clone import or bulk user-state conversion. If later requested,
that work needs backup, atomicity, restoration on failure and verified postconditions in its own
contract. Disabling the browser preserves existing native bills and keeps owned hiding records
dormant. Removing a mod and saving without it does not imply a guarantee for its extra data.

## Implementation and coverage

**Nothing implemented.** All AB-01–11 are proposed observable requirements with unchecked tasks.
Technical boundaries and alternative choices are in the design. In particular, no-stat Any allowed
material planning is intentionally distinct from promising a best material when no attribute exists.
The common-property list is an initial explicit menu priority, not a comprehensive safe stat catalog
or empirical usage ranking; further supported properties follow with distinct labels/units.

No hook or target admission validator exists. Future hooks require individual minimal semantic
target specs, effective-code handling, isolated startup failure and bounded warnings, accepting
compatible foreign patches. An incompatible active original UI retains its working entry. No XML
mutation is currently selected; any later XML work must use content/package/shape guards and merge
preservation with declared XML Extensions dependency. Shared map candidate data/evaluation replaces
per-port inventory caches; no performance gain is claimed without measurement.

## Verification and acceptance

Document validation and independent review are tracked in the
[progress ledger](../refinement/specification-progress.md). AB-Q1–4 record exact native/provider
boundaries, layout/text/category edges, supported stat expansion and measured/native acceptance.

Later work requires focused nonzero RED/GREEN in honest environments, typed owner build/package
checks, independent review and final native workflows on the reviewed build in a fresh minimized
Gateway process. Observe search/filter/material/compare/hide/add/cancel and actual production, including
correct consumed material, special bills, repeated deliberate versus duplicate accidental adds,
paused updates, settings/main-menu/save lifetimes and supported providers. Retain causal views,
build/mod identities, normal-config hashes and cleanup. Source/schema assertions are not acceptance.

Semantic intent: faster access to information and plans, with unchanged costs, research, skill
requirements, quality rolls and physical production. No global inventory reservation, free material,
automated ingredient production or hidden cross-bench propagation is introduced by this browser.
