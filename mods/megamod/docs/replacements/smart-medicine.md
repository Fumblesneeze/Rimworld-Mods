# Smart Medicine - Continued

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, target **1.7 beta**.
Origin replacement **S062/P1-56**. Source `Memegoddess.SmartMedicine`,
[Workshop3526251680](https://steamcommunity.com/sharedfiles/filedetails/?id=3526251680), original
`Uuugggg.SmartMedicine`/1309994319, Uuugggg (Alex Tearse-Doyle), continued by Meme Goddess.
Inspected2026-11-04. Public revision `0410071ef1ddc23bea7115da9787b7e817fe18b0`, MIT copyright2025
Alex Tearse-Doyle. Selected installed1.7 DLL/file1.7.0.0 SHA256
`3E28327108A663D60788B0A185D4DD17DADBB150280FBFE96281922EFEDD0315`.
All38 runtime source files and selected XML read; central compiled paths/native1.7.4998 compared.
Detailed hashes, artifacts, source discrepancies and actual consumers are in
[research](../refinement/2026-11-04-smart-medicine-research.md). No assembly/source/assets copied,
no game launched, no compatibility or performance claim accepted.

## Behavior inventory and coverage

Every row is **specified only; implementation/tests/live evidence absent**.

| ID | Observable input → result | Selected defaults / notable boundaries | Owning contract |
| --- | --- | --- | --- |
| SM01 | Choose individual/group care → only those conditions use it | All5 native categories plus Default; mixed state; group-wide reset; no general-policy write | [Medical care MC01](../../../../openspec/changes/specify-megamod-smart-medicine/specs/megamod-medical-care/spec.md) |
| SM02 | Exclude condition from rest → omit voluntary convalescence only | Urgent need, incapacity, sleep and actual healing unchanged | Same,MC02 |
| SM03 | Tend patient → count/fetch/treat one permitted batch | Urgent before care tier, then ordinary priority; native real costs | Same,MC03–04 |
| SM04 | Find medicine → eligible physical sources and sensible route | Four independent inventory roles on;6/12-cell detours;8-cell secondary radius; no extra capacity | Same,MC05–06 |
| SM05 | Enable Surgery uses best → operation relaxes routine care ceiling | New-pawn default off; bill filters/ingredients remain; doctor inventory only | Same,MC07 |
| SM06 | Configure preferences → shared inherited medical/supply policy | Common global/save, no duplicated Medical Defaults or cache switch | Same,MC08–09 |
| SM07 | Treat a field patient → ordinary care at their location | Emergency on; ordinary mode off; two-hour planning allowance; permissions remain | [Field care FC01–03](../../../../openspec/changes/specify-megamod-smart-medicine/specs/megamod-field-care/spec.md) |
| SM08 | Rest/tend/feed/cancel → temporary ground rest with recovery | Ordinary ground-spot effects; no wiping; cleanup within1game hour | Same,FC04–05 |
| SM09 | CE stabilization deferred | No initial adapter/job/control; ordinary field care remains independent | FC06 initial scope |
| SM10 | Edit/copy/drop personal targets → precise saved pawn orders | Counts0–9999, native tighter limits, snapshot paste, visible native/drug contributors | [Personal supplies PS01–02](../../../../openspec/changes/specify-megamod-smart-medicine/specs/megamod-personal-supplies/spec.md) |
| SM11 | Restock/return → real quantities under scarcity and capacity | Stocking on,150% threshold,scarcity return off; actual available care first | Same,PS03–05 |
| SM12 | Dispense/ingest/unload → native costs and exact retention | No query-time meals, target0 protects nothing, current usable alert counts | Same,PS06–07 |

## Deviations omissions and direct composition

- Correct grouped Default, saved general-care mutation/rollback, cross-query transient state,
  equal-unit distance comparisons, unsafe source drops/split references and stale desired counts.
- Preserve native1.7 holder-aware medicine and inventory stock instead of maintaining a competing
  source loop. Personal minimums use max, not sum; physical job allocations remain exact.
- Field estimates use the actual patient-to-bed path and earliest known risk; every supply source
  gets the same timing check. Ground rest is ordinary, temporary and safely cleaned up.
- Stocking uses real capacity and actual dispensers, copy-time snapshots and immediate edited
  targets. Care takes precedence only when the pawn can really perform the medical job.
- Source general cheap-equivalent-medicine claim is not implemented in inspected code and is not
  selected here. NiceHealthTab cache toggle, old internal WhileYou'reUp workaround and global
  nonsuspendable TendPatient XML are deliberately not copied. No source saves/settings import.
- Direct owned composition: S007 condition UI, S021/S126 physical cargo, S024 cleaning/ingredients,
  S042 self-tend, S049 storage limits, S055 food sharing and S058 eligible-patient order. Source-like
  hooks between these owned features disappear. S073 bedroll use, S080 stabilization, S123 meals,
  S124 Drug Response, S132 inventory UI and S134 Priority Treatment retain later detailed analysis.

## UI audit and configuration scope

| Source surface | Meaning | Owned placement |
| --- | --- | --- |
| Native/custom Health condition menu/icons | Individual condition care/rest orders | Same contextual controls in ordinary and owned compact views |
| Health overview/Assign/optional Medical column | Individual surgery preference | Existing appropriate pawn controls; no new main tab |
| Gear rows/stock dialog/optional RPG or CE inventory | Pawn supply orders, copy/drop/ingest | Contextual inventory with contributors and real status; CE inventory integration deferred |
| Medical Defaults extra checkbox | Default for new individual choices | Common mod settings, global default plus save override |
| Source ModSettings | Genuine medical/supply configuration | Ordered common Medical care, Field care, Personal supplies sections |
| Temporary-spot Cancel | Remove own transient rest place | Contextual native action |
| Stock ANYTHING | Request real haulable supplies | Ordinary extended catalogue preference, not developer-only cheat |

Source adds no bottom main tab. No extra view/behavior corner toggle is needed to duplicate these
orders or settings. All seven languages and readable unavailable/mixed states remain acceptance work.
Current-save controls are disabled while inheriting and at the main menu; entity orders are never
stored as global/save-inherited settings.

## Consumed contracts persistence and dependencies

| Consumer / source contract | Preserve / adapt / unsupported |
| --- | --- |
| Compiled Compact Hediffs package gate plus4 care delegates | Direct owned S007 composition; external adapter requires actual package/shape tests, no source-package spoof |
| Mod Medicine Patch1.6 SmartMedicine assembly, old LabelButton and PriorityCareComp dictionary | Stale/partial current-source binding; no fake assembly. Unsupported extended categories disable affected owned care pending explicit adapter |
| Reworked hauling Dreadarm.SmartMedicine V1 | Different provider; not an incoming Continued contract. Shared owned retention directly |
| Pharmacist, NiceHealthTab, RPG/bedroll/special-patient adapters | Optional; CE deferred; exact provider and semantic shape admission before support; current absent/provider gaps in SM-Q3 |
| Source condition/stock/surgery/default-care and temporary-bed save keys | No automatic migration or removal-safety promise; owned exact-entity state tested separately |

Core required. Harmony becomes required when used; XML Extensions required with guarded declarative
integration. Optional external content requires canonical package and actual content checks. All
additions merge without replacing other columns, think nodes or lists. Every actual patch needs a
target-spec document and executable admission check; failed dependent care paths stay inactive
together with bounded startup warnings. Compatible unrelated patches remain allowed.

Original or Continued source active → owned S062 overlap off, provider untouched, warning explains
fallback and saved owned orders remain visibly inactive. No upstream assembly/package identities or
Dev Gateway dependency enter the product. Source's optional compatibility/performance labels are not
our accepted claims.

## Verification status

[Tasks](../../../../openspec/changes/specify-megamod-smart-medicine/tasks.md) are all unchecked.
Typed specification validation and independent document review are recorded in the
[progress ledger](../refinement/specification-progress.md); neither accepts gameplay. Required final
native workflows include actual condition editing and treatment effects, inventory transfers and
consumption, surgery, field rest/feeding/cleanup, stocking/drop/copy/paste, dispenser production,
save switching, changed/absent providers and shared capacity. SM-Q1–4 remain for tuning, effective
targets, provider proof and composition/acceptance. No runtime implementation is claimed.
