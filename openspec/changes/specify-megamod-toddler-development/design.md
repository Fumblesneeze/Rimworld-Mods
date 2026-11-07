## Context

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
This is a specification draft for S031, not implementation. Read the proposal and six capabilities.
The original 1.6 provider's full author manual and selected current source/XML were analyzed.
Pinned revision, installed hashes, compile exclusions, actual consumers and limits live in:

- [Research index](../../../mods/megamod/docs/refinement/2026-11-03-toddlers-research.md).
- [Feeding/play/care](../../../mods/megamod/docs/refinement/2026-11-03-toddlers-feeding-play-care.md).
- [Apparel/mood](../../../mods/megamod/docs/refinement/2026-11-03-toddlers-apparel-mood.md).
- [Travel/rendering](../../../mods/megamod/docs/refinement/2026-11-03-toddlers-travel-rendering.md).
- [Providers/consumers](../../../mods/megamod/docs/refinement/2026-11-03-toddlers-integrations.md).

The source package's 38 selected inputs match its pinned checkout, not a reproduced build. Native
beta semantics, other provider internals and exhaustive inbound binary consumers are not proven.
No license grant was established. Reimplement observable contracts; do not copy code, DLLs or art.

## Goals / Non-Goals

**Goals:** coherent dependent childhood, gradual self-care, actual adult/child interaction, safe
care and travel, provider-preserving optional support, common settings, honest source deviations.
**Non-goals:** unrelated child content, new bottom tabs, a general childcare plugin framework,
an internal Harmony patch stack, source-ID spoofing, automatic foreign-save conversion, automatic
provider removal, redistributing upstream assets, or declaring beta compatibility from static reads.

## Decisions

### 1. Own the child lifecycle and compose policies directly

Keep this behavior together under a toddler feature. Use native age/life-stage, health, apparel,
needs and job mechanisms where their exact semantics satisfy the contract. Add declarative life
stages, learning states, thoughts, garments, recipes and curves through guarded XML, rather than
recreating all of native childhood in C#. Neither a human age guess nor a translated Crib label
is a complete capability test. Resolve an admitted stage and actual mobility/crib/door capabilities.

S030 owns prevention, escape, evaluation/retry bounds and safe outside-area recovery. Call it from
owned idle/play, crib, care, undressing and arrival selection; do not patch our own methods. S024
needs and S021/S125/S126 hauling share relevant facts but do not schedule duplicate jobs. S133/S135
transport/caravan work owns its loading additions; toddler passenger identity and safe care handoff
must be shared. No independent full-map candidate cache: use native listers first and the existing
shared Thing-data contract when clothing/food selection genuinely needs derived candidates.
Pawn-specific reservations, permissions, provider support and reachability are rechecked at use.

### 2. Turn source discrepancies into explicit owned choices

| Area | Chosen contract and reason |
| --- | --- |
| Loneliness time | Source adds 0.0008 at a 200-tick hash check. Adopt 0.000004 per elapsed ordinary tick at toddler start (0.24/day), declining with age by factor 1−0.6×stage fraction. This is a provisional normalization, not a measured engine rate. |
| Other repeated effects | Per-tick gains use elapsed simulation time. Mess needs an elapsed-time event-count process preserving the per-tick distribution, e.g. binomial counts for a fixed integer interval and unchanged probability. The probability of at least one event is insufficient: retain multiple events and their hygiene effects. Partition at relevant rate/activity changes; avoid an unbounded per-missed-tick loop. |
| Solo attention cap | Clamp gain and finish at the cap; do not repeatedly select a job that cannot help. Adult play can still relieve loneliness at full Play. |
| Food mood | Follow the manual's actual consumption-mood exemption, not just MoodFromIngesting's menu estimate. Preserve physiological/diet consequences. Verify actual native memory creation first. |
| Activity discrepancies | Permit indoor bugwatching and clumsy floor drawing. Require ongoing fire/TV/roof/access safety. Fix decoration lookup and safe unpowered TV support rather than assuming all televisions have a power comp. |
| Adult/child pairing | Track the exact counterpart and actual interaction phase. Recompute bedside adjacency after placement, release paired waiting on every exit, grant only earned effects. |
| Care admission | Retain ordinary loneliness-driven adult-play triggers independently of low mood; retain sleep/rest precedence for crib exit and allow hunger to override sleep only for an actual self-feeder. |
| Medical temperature | Follow S030 safety instead of preserving the source's serious/minor injury thresholds for unsafe medical beds or unchecked carry-drop fallback. Explicit warned player placement remains distinct. |
| Apparel | Keep the five choices but scope owned acquisition changes to owned garments; preserve providers' original eligibility/progression. Explicit cloth-only tuque, native tailoring benches and source numerical costs are draft choices pending finalized comparators. |
| Undressing | Protect forced/locked items and evaluate actual post-removal S030 safety reliably; retain its base comfort±10°C rule or admitted provider interpretation. Remove only the targeted item. |
| Mental states | Exempt only Crying, Giggling and owned RemoveClothes from specified control/travel/group/ritual exclusions while preserving area obedience and other eligibility; not every Baby mental state. Retain source Go frenzy minimum age 1 with native prerequisites. |
| Refugee loitering | Retain arrival/parent story and 12/3-cell intent, but S030 and genuine needs outrank proximity. Native adoption details still need pinning. |
| DBH | Per-pawn eligibility; no label-substring command deletion; atomically admit replacement care; consume a bottle unit on first actual wash, retain earned partial gains, never reward travel/fill failure. |
| HAR | Use the actual infant-child interval, ordered ages and existing stage reuse. Atomic per-race application, independent nested state and cycle-safe graphics traversal replace partial mutation. |

Source-only numeric priority internals and animation waveforms are evidence, not extra player
requirements. Preserve the specified visible outcomes with the simplest supported native mechanism.
Keep incidental animation constants in named code values and verify actual appearance. The complete
source tables remain available for faithful tuning; no source text being cataloged implies acceptance.

### 3. Admission is semantic, transactional and scoped

Before implementing each external hook, add its stable target document under mod patch-targets,
linked to the specific scenario, tests and eventual live workflow. Check minimal required target
semantics, incoming/effective IL where needed, cardinality and control/data flow. Permit compatible
foreign owners and unrelated body edits. No PatchAll, hard-coded instruction offsets, speculative
foreign-transpiler execution or copied bed-finder plus foreign postfix.

Declare dependent atomic groups for lifecycle/required care and each provider adapter. Stage checks
before mutation; rollback only newly applied owned changes on failure. A runtime load that cannot
support already saved owned content needs an explicit load-safety outcome before implementation;
do not equate skipping a patch with safe removal of saved jobs/Defs. Transpilers validate every
incoming stream and return it unchanged if not admitted. Enabled skipped patches warn once at
startup with patch/spec/target, failed predicate, unavailable player behavior and fallback.

Use XML Extensions presence/shape checks and package-ID FindMod for optional content. Merge list
membership idempotently; preserve unrelated categories, stages, stats, jobs and sound entries.
The only intentional whole-list replacement currently specified is the crying/giggling curve:
its behavior requires one coherent curve, so validate and record what is replaced. A clothing
availability setting must not globally rewrite every provider's recipe users/research/stage flags.

### 4. Persistence and settings are distinct

Common settings definitions/resolver/storage/UI own all configuration. Apply effective values per
active save and pawn where needed, without leaking global Def mutations between saves. Keep original
immutable baselines separate from current values. Save per-pawn learning progress, attention and
real paired activity/arrival ownership; do not serialize derived candidate caches or transient
provider reflection handles. Reconcile loaded jobs against current objects and release only owned
reservations/waiting/fixture state.

Changing duration affects future learning speed, not earned stages; initializing generated/native
eligible children uses current age once. Existing foreign learning/job types are a separate migration
problem. Child-specific policies remain native gameplay state. Debug reset is an explicit override,
not the path for normal settings changes. All new labels/reports/settings follow seven baseline
languages; old unused source keys and typos are not compatibility contracts.

### 5. Optional adapters have narrow boundaries

| Provider | Intended surface | Remaining pre-implementation evidence |
| --- | --- | --- |
| `Dubwise.DubsBadHygiene` | Actual child's need eligibility, clean-water bottle, baths/buckets, fill/drain/contamination and occupant ownership | Pin native provider bodies and actual unit consumption; support/no-support per concrete fixture; cancellation and reload accounting |
| `Dubwise.DubsBadHygiene.Lite` | Source-recognized alternative hygiene provider, intended same applicable caregiver outcomes where its concrete content supports them | Separate variant research pending; do not assume the full provider's APIs, fixtures, water system or feature availability; no support claim yet |
| `erdelf.HumanoidAlienRaces` | Stage intervals and existing toddler shapes, body/graphics conditions | Current provider input, supported races, independent copying and finite traversal; Moonjelly/TTancon/Mincho entries require exact content-specific evidence |
| `ferny.progressionattire` | Babywear category and optional tribal research equivalence | Current beta folder selection, concrete category/research targets and compatible activation without cyanobot package gate |
| Hospital / Altered Carbon | Hospital/guest bed eligibility, empty sleeves/caskets where actually relevant | Actual consumed APIs and semantics; source reflection is only a lead, no promised adapter yet |
| Celsius / GeneSeed / Realistic Human Sounds | Relevant effective temperature, stage/content or sounds only if still needed | Verify live callers, exact canonical identities/content and native name matching; avoid porting dead startup flags |

Resolve optional types only inside admitted adapters; no eager third-party types in load paths.
Keeping HumanlikeToddler or garment Def names may help concrete consumers but does not activate an
external LoadFolders gate for cyanobot.toddlers, nor preserve an assembly ABI. Attire is the first
observed real XML consumer; bounded text search is not exhaustive binary/reflected analysis.
Never claim drop-in replacement until each actual consumed contract is separately evidenced.

### 6. Verification follows behavior slices

Use Unit for deterministic progression, need time, mode resolution and decision rules; Harmony for
one owned admitted patch; Defs only for lightweight constructed fixtures. Native inheritance,
real providers, jobs, loaded content and rendering require exact fresh game processes. Keep RED/
GREEN evidence, use focused groups and build/package checks; do not run the whole repository suite.

Final acceptance must exercise actual feeding, play, dressing, crib exit/rescue, growth, carrying,
caravan departure, adoption and admitted provider washing/bathing through native player workflows
on the reviewed build. Setup can arrange age/needs/fixtures, not manufacture outcomes. Inspect exact
process before/action/after views personally, including failed/blocked/interrupted paths and reload.
Use minimized isolated Gateway runs and preserve build/mod order/action/screenshots/log/config hashes
and cleanup. Raster provenance and live visual gates remain required before accepting clothing/art.

## Risks / Trade-offs

- **Large source surface** → six observable capability groups with vertical tasks; source coverage
  and a drafted contract are not implementation completeness.
- **Source claims differ from native 1.7** → exact native methods and provider bodies remain gates,
  especially adoption, Strip, consumption moods, growth vats and transport; revise specs openly if
  evidence requires a different observable choice.
- **Tuning may be too harsh or cheap** → provisional values retain measured source provenance;
  compare finalized Core/closest installed equivalents, then native care burden/crafting workflows.
  No balanced-cost or support claim before that pass.
- **Asset rights/appearance** → native reference use or independently authored assets with provenance;
  no source texture copying into packages or writes into installed mods.
- **Dependent feature failure on a saved colony** → define safe-load/recovery boundaries before
  enabling saved content; warn honestly rather than silently leaving a half toddler system.

## Migration Plan

Documentation only now. No normal configuration, save or Workshop modification. Future rollout is
one reviewed vertical behavior at a time. Preserve owned state across ordinary upgrades. Source-save
conversion/removal is not offered by this change; any later tool must specify backup, staged atomic
conversion, rollback/restoration and verified postconditions. Co-installed original providers keep
the overlapping owned feature inactive. Unknown ports require identity research before any support.

## Open Questions

TD-Q1's current-source inventory is complete at the documented coverage; this draft resolves intended
behavior choices rather than requiring more user explanation. TD-Q2/4/6/7/8/9 now have owned decisions
above and implementation evidence still pending. Retain TD-Q3/5 and the following research gates:

1. Pin exact beta ingestion memories, Strip/ForceTargetWear/scoring, growth-vat accumulation, native
   adoption constraints, departure/carry routes and renderer ownership. Broader DLL consumer audit.
2. Resolve finalized garment inheritance and three meaningful Core/installed comparators; validate
   declared recipes, cost/research/material paths, armor/insulation and no profitable craft loops.
3. Pin each optional provider's public/private consumed boundaries and lifecycle. No further optional
   compatibility is promised from names; document supported/unsupported versions by evidence.
4. Define safe load when a dependent admission group fails with saved owned entities/jobs. This is
   required before runtime delivery, not an invitation to silently mutate or lose a save.
5. Validate normalized rates, actual childcare burden, stage duration and visuals in game, then amend
   provisional values with rationale if necessary. No launch is needed to validate this prose draft.
