# QualityBuilder — targeted construction and ordinary retries

## Identity and provenance

- Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
- Origin: **replacement**, S013 / P1-10. Source **QualityBuilder Unofficial 1.6**,
  `hatti.qualitybuilder`, Workshop **3512466087**, authors Hatti / pkp24 (Steam maintainer Teemo).
- Read 2026-11-02: [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3512466087),
  [change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3512466087), then
  [author source](https://github.com/pkp24/QualityBuilder). Pinned commit
  **c063417d46b2252d15851bdd02a6712a5a971d0d**, 1.6.013, 2026-08-03.
- Installed read-only root `F:/Steam/steamapps/workshop/content/294100/3512466087`.
  Latest DLL `Versions/v1.6/Assemblies/QualityBuilder.dll`, assembly **1.0.17.0**, file/product
  **1.0.17**, SHA256 **A9759C6C3675644AB165FE256EB58FE50633DF5E3FC516F8B113AEE60E141247**.
  Its bytes match the pinned repository DLL; critical paths were checked in decompilation too.
- About version **1.6.013**, declared support **1.1–1.6** and required Harmony. Author's 1.7 testing
  comment does not establish installed support. No 1.7 load or gameplay was verified.
- Installed license declares **CC BY-SA 4.0** with attribution/share-alike/change notice and a
  Ludeon-specific exception. This records the notice, not a blanket permission for source/art reuse.
  Independently implement the contract; no upstream assets or binaries are copied into the product.
- Detailed [research journal](../refinement/2026-11-02-quality-builder-research.md) identifies source,
  native game and consumer inspection, settings/save keys and inferred defects. Code-derived risks
  are not live reproductions. Public documentation and implementation disagree in places.

## Behavior inventory

All rows are **specified, not implemented or verified**. Their owner is
[this change](../../../../openspec/changes/specify-megamod-quality-construction/); observable clauses
are in its [capability](../../../../openspec/changes/specify-megamod-quality-construction/specs/megamod-quality-construction/spec.md).

| ID | Native input/action → observable result | Defaults, edges and deliberate changes | Evidence / owning requirement |
| --- | --- | --- | --- |
| QB-01 | Place/mark/select a quality-bearing construction project → individual enabled state and minimum | New projects enabled, Awful; all seven qualities; mixed selections handled per eligible project | Source CompQualityBuilder/designators/Startup; Quality targets / Orders |
| QB-02 | Request ordinary finishing → suitable local constructor gets the job | Best minus 0 levels; absolute bypass 20; mechs included initially; native delivery and blocker work unaffected | QualityBuilder helpers / WorkGiver postfix; Ordinary finishing |
| QB-03 | Assign a colonist contextually → map-specific quality finishing waits for that colonist | Automatic initially; invalid assignment falls back visibly; move source pawn picker out of settings | Source override/map settings; Named builder |
| QB-04 | Force valid finishing or inspect work → deliberate override succeeds; pure query cannot kick another worker | Ordinary active worker checks its own policy before further work; remove source query-side interruption | Source WorkGiver patch; Quality policy |
| QB-05 | Finish below target → ordinary deconstruction and matching reconstruction | Native resources/work/XP/quality; same material/rotation/style; safe blocked-placement recovery | Construct/deconstruct hooks and native Frame; Below-target results |
| QB-06 | Spend replacement budget → retain last result with explicit exhausted status/restart | Default 3 initiated cycles after initial completion; 1–20 or Unlimited; native failures do not reset count | Source comp retry path; Retry budgets |
| QB-07 | Cancel quality removal, lower a met target or disable → stop owned pending work only | No later manual-order hijack; after destruction retain an existing ordinary plan; achieved-target retarget works | Source flag/cancel risks; Cancellation / Retry budgets |
| QB-08 | Reload/move/rebuild → intent and supported configuration survive | No load-time unforbidding; unknown custom configuration blocks destructive automation; ordinary auto-rebuild remains native | Source Scribe/transfer hooks and native Frame; Replacement preserves configuration |
| QB-09 | Configure behavior → common inherited global/save values apply | All seven rows/defaults/ranges/timing in spec; dormant override retained; main-menu save controls disabled | Source global/map settings; Defaults and preferences |
| QB-10 | Start with source or changed provider → one overlap owner or explicit unavailable behavior | Source co-install disables our overlap; optional absence normal; no automatic facade/migration/retexture support | Actual consumer inspection; Compatibility failures |

## Interactions and composed result

Selected **Replace Stuff - Continued** (S028, `Memegoddess.ReplaceStuff`) resolves upstream component
types and a designation while creating material-replacement frames. Its selected replacement must
compose quality target/run state directly with ours; no patching one megamod feature from another.
Material replacement alone must not add a free quality roll or duplicate a quality retry. Exact S028
semantics remain for its sequential deep dive, not guessed from this integration trace.

Whole-list workbench linking (S014) owns synchronized bill configuration; quality reconstruction
must retain supported building membership/configuration through that owner. Later construction or
best-material features cannot silently change this quality target, retry budget or native costs.
Shared candidate facts stay map-correct; no inherited-settings cache shared across maps is ported.

## UI audit and relocation

| Source entry point | Purpose | Megamod destination / change |
| --- | --- | --- |
| Orders Set/Unset designators | Mark blueprint/frame projects | Keep contextual Architect Orders tools, visible selected target and filled-rectangle workflow |
| Blueprint/frame and optional finished gizmo, right-click quality menu | Per-project gameplay intent | Contextual controls with discoverable target action and inspect status/cancel/restart; mixed selection no longer constrained by highest finished quality |
| Global and optional per-map settings page | Configuration | Common Construction / Quality construction global/save rows; no independent numeric map copy |
| Named builder selector inside map settings | Actual pawn assignment | Contextual map gameplay control, explicitly labeled current-map scope; not inherited configuration |
| Source bottom tabs | None found | No new bottom tab |

Defaults: automatic marking **true**, minimum **Awful**, allowed skill difference **0** (0–20),
absolute bypass **20** (0–20), mech exclusion **false**, replacement cycles **3** (1–20/Unlimited),
hide finished gizmos **false**. Defaults only initialize new project targets/enabled state. Effective
eligibility/presentation changes apply at the next relevant decision/display; changed retry limits
apply at the next cycle without canceling an initiated one or restarting exhaustion. The common
Use global row disables only the override editor during inheritance and both save controls at the
main menu; it preserves dormant overrides, save isolation and live inherited global changes.

## Contracts consumed by other mods or saves

| Consumer / evidence | Concrete binding and semantics | Decision / verification still required |
| --- | --- | --- |
| VEF 1.7, Workshop 2023507013 | Checks `hatti.qualitybuilder`; reflects `QualityBuilder._WorkGiver_ConstructFinishFrames.Postfix`; skips it for a specific extension-based construction giver | No automatic binding to megamod; investigate actual job metadata timing and preserve provider restrictions through a narrow adapter if justified. QB-Q3 |
| Replace Stuff - Continued 1.7, Workshop 3526354009 | Resolves `CompQualityBuilder`, `CompProperties_QualityBuilderr`, `SkilledBuilder`; marks/generated comps on replacement frames | Direct owned composition for selected feature; external reflected contract not yet supported. QB-Q3 and S028 |
| QualityBuilder's outgoing Replace Stuff patch | Old `Uuugggg.ReplaceStuff` / exact display name detector; copies enable/target on placement | Installed continued identity matches neither detector; do not replicate name guessing or promise it worked |
| Retexture `JGH.qualitybuilder`, Workshop 2638388641 | Hard source-package dependency and Skilled/Skilled1…7/UnSkilled paths | Unsupported automatic drop-in; no package spoofing or source art reuse |
| Existing QualityBuilder save/config | `Quality`, `desiredMinQuality`, `desiredMinQualityReaced`, `pendingQualityRebuild`, `qualityRebuildAttempts`, map `qbUseMapSettings`; source fields/defaults detailed in journal | No migration promised; preserve owned future state only. Any later import requires backup, atomicity, failure restoration and verified postconditions |

## Implementation and coverage

**Nothing is implemented.** Every QB row remains missing runtime code, executable patch admission,
focused tests and final native acceptance. The design owns technical candidates and QB-Q1–4 research;
the spec does not dictate a particular patch class or blanket exact-method match.

Intentional corrections include map-local best-skill decisions, pure work queries, stale achieved-flag
repair, canceled-order ownership, supported configuration preservation, explicit restart/exhaustion,
main-menu-safe shared settings and contextual pawn assignment. Potential conflict: source-bound
integrations do not automatically redirect to megamod, and unsupported custom building state prevents
automatic quality destruction. Ordinary native work remains available in those cases.

With active `hatti.qualitybuilder`, disable our overlapping feature with an explanation; do not edit
or uninstall the source. Rejected admission or source overlap must suspend saved owned removal before
it can destroy an intact building, retaining its target/budget and leaving manual/provider work alone.
Restored support requires explicit Resume without charging the same cycle twice. VEF/external Replace
Stuff are optional, shape-guarded cases; source provider
is not a dependency. Harmony becomes required when its types are used; XML Extensions becomes
required at its first operation. Prefer guarded additive static Def changes, preserving unrelated
components/designators, and justify any generated-Def runtime path. No whole-list replacement is
needed here. Hashes identify inspected inputs, never serve as the sole startup compatibility test.

## Verification and acceptance

This document is a source-backed draft. Remaining **QB-Q1–4** are tracked in the
[sequential ledger](../refinement/specification-progress.md); all implementation tasks remain open.
No game was launched and no upstream risk has been claimed as a live reproduction.
Independent document review found one persisted-removal fallback gap, now addressed in the observable
contract, load-path design and tasks. This review does not accept any runtime behavior.

Later acceptance requires focused RED/GREEN in honest environments, exact target-spec/guard failure
tests, package/dependency checks, independent code review and personally observed native player
workflows on the reviewed build in a fresh minimized isolated game. Include ordinary/forced finishing,
delivery, bounded costly replacements, active cancellation, manual teardown, achieved-target raising,
configured and unsupported buildings, reload, movement, multiple maps and supported provider runs.
Retain causal screenshots, exact build/mod list, logs, normal-configuration hashes and cleanup.

Semantic review: construction work, resource consumption, deconstruction return, experience and
quality probabilities stay native. Better selection/repetition changes outcomes through real labor
and cost. A finite default limits deliberate quality replacement cycles, not every possible native
construction failure. Unlimited remains opt-in; unreachable targets can exhaust normally. No balance,
performance or compatibility acceptance follows from this prose or static source inspection.
