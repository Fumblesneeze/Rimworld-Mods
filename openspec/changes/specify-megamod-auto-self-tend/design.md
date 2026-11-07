## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S042 replaces Auto Self-Tend. [Research](../../../mods/megamod/docs/refinement/2026-11-03-auto-self-tend-research.md)
records complete source binary inspection, exact native identity, documentation limitations and
consumer scans. This is a design for later implementation; no game acceptance exists.

## Goals / Non-Goals

**Goals:** skill-based permission defaults, source manual-choice semantics with explicit policy-change
exceptions, current-save/global configuration, correct native eligibility, durable per-pawn history.

**Non-goals:** new medical jobs, guaranteed treatment, changed medicine/quality/costs, permanent
manual-lock UI, an upstream API/package impersonation, upstream save/settings conversion, another
Thing cache, medical-priority/drug-policy implementations or general migration infrastructure.

## Decisions

### Use the native checkbox and an ordinary game component

`Pawn_PlayerSettings.selfTend` remains authoritative and native-saved. Read the existing Medical
`SkillRecord.Level` (including aptitude), not `levelInt` or MedicalTendQuality. Owned state records
assessment history, not another permission value. A GameComponent performs initialization and
bounded tick reconciliation; settings commit requests a synchronous reconciliation. XML cannot
express this per-pawn temporal behavior. There is no reason to add Harmony for polling or to patch
our own component. No new Defs, patch operations, raster art or dependency declarations in this slice.

Use native map and player-caravan pawn collections, deduplicate by pawn identity in a transient
collection, clear it after each pass, and retain no static cross-game references. Map collections
include held/unspawned pawns. Do not scan every Thing, create another map index or search all world
pawns. Retain assessment records for temporarily absent pawns; prune only identities conclusively
removed from the game, not merely missing from a map. A missing record means first assessment.

### Eligibility is permission eligibility, not readiness for a job

Consistently require living native `IsColonist`, a non-Baby developmental stage, player settings,
Medical information and `!WorkTypeIsDisabled(Doctor)`. Native secure slaves count as colonists;
insecure slaves do not. Source map collection can include more than this; this is an intentional
correction. Map/caravan scope plus native eligibility excludes ordinary prisoners, non-player
visitors, animals, mechs and subhumans. Children with an enabled native control remain eligible;
Toddlers in Baby stage do not gain a hidden permission.

Doctor priority zero does not exclude permission eligibility. Never change work priorities to make
self-tend happen. Downed, resting, reserved or medicine-starved pawns need not have their permission
cleared; readiness, care category, reservation, medicine choice and self-treatment quality remain
native. Exact player work selection and provider paths still need ST-Q1 proof; the inspected
`JobGiver_SelfTend` is an NPC think-tree path and is not a sufficient player-path acceptance test.

### Store the policy last assessed for each pawn

Minimum state: stable pawn identity, last assessed threshold-side boolean, and the effective
threshold/disable-below pair used for that assessment. Keep this save-owned and versioned. A shared
small immutable pair is enough; no generic rules engine, event bus or independent cache is needed.

| Assessment condition | Action |
| --- | --- |
| No admitted feature, no active game, absent population or ineligible pawn | No permission write; preserve valid history |
| First eligible assessment | Evaluate current policy once |
| Previous classification differs, or that pawn's last effective policy differs | Evaluate current policy once |
| Classification and effective policy unchanged | Preserve current native checkbox, including manual/external edits |
| Evaluation says above/equal threshold | Enable native checkbox if currently off |
| Evaluation says below, disable-below enabled | Disable native checkbox if currently on |
| Evaluation says below, enable-only mode | Keep native checkbox unchanged |

After a successful eligible assessment record classification/policy even when no checkbox write
was needed. Do not mark an unsuccessful/unknown eligibility read as assessed. Changing an option
from inherited to overridden with the same effective value is a no-op. A genuinely changed option
can reset a manual choice for a pawn whose classification did not change; document that in UI.

Per-pawn policy history fixes source's single `_lastThreshold`/`_lastDisableBelowThreshold` pair:
a pawn returning after an effective change must see the new policy even if other pawns already did.
Simple map-to-caravan transfer with unchanged policy/classification must not behave like recruitment.
Ineligible intervals preserve history; resuming eligibility compares current values with that history,
not an invented permanent override. Missing old history or first installation is a fresh assessment,
which can replace preexisting native choices; source migration is not implied.

### Timing and configuration

Shared settings IDs proposed: `health.selfTend.minimumMedical` (integer 0–20, default 6) and
`health.selfTend.disableBelow` (boolean, default true), in that order under Health / Automatic
self-tending. Validate committed input; out-of-range/corrupt persisted integer uses bounded 0–20
normalization with a diagnostic, missing value uses 6. No per-feature ModSettings class/window.

Use the common global/save definitions, persistence, effective-value resolution and row renderer.
Current-save rows inherit by default; dormant override retention and main-menu mutation guards are
mandatory. Global edits affect inherited values, not override values or unrelated saves. On loading
a save compare its recorded per-pawn pair with that save's current effective pair before play.

Run initial reconciliation after pawn/settings deserialization and before first playable tick.
Retain source **600-tick** maximum ordinary reconciliation latency (10 seconds at normal 60 ticks/s,
not real time when paused); join, skill/aptitude changes and return from absence use that bound.
Successful committed settings changes reconcile current eligible pawns before control returns to
play, even while paused. Merely drawing or closing unchanged settings does nothing. Interval is a
named implementation constant with observable bound in the spec, not another user preference.

### Admission without speculative patches

Before enabling the component, verify the minimal native contracts actually used: boolean native
permission and serialization, usable Medical/Doctor Defs, native eligibility and collections, and
the ordinary lifecycle/settings commit entry points. Hashes above identify evidence only. Do not
require an unchanged game hash, complete method body or absence of foreign Harmony owners.
Unsupported consumed behavior disables only this automation, retains native checkbox values and
logs one bounded startup warning identifying feature/target/failed requirement/fallback. Repeated
polls do not log repeatedly. Corrupt/missing individual pawn data is skipped without breaking other
eligible pawns; diagnose once per distinct affected condition, not every poll.

No patch target document is fabricated for a patch-free component. If exact implementation needs
Harmony/XML hooks, add stable per-patch target documents and executable startup admission first:
minimal consumed semantic/data-flow predicates, effective-code interactions, skip-original handling
when relevant, incoming transpiler validation each execution, owned-only atomic rollback, bounded
warnings and supported repatch revalidation. Every enabled skipped patch requires its own warning.
If XML becomes necessary, use XML Extensions content/package/shape guards and merge-preserving
changes, declare its runtime dependency and verify finalized Defs. Do not mutate Defs in C#.

### Providers and composition

Active package `funstab.autoselftend` (canonical case-insensitive comparison) suppresses owned
automation before any write, including `FinalizeInit`. Do not load the source DLL to detect it,
edit its settings, unpatch its owners or spoof its package/assembly. The megamod settings section
must explain that the original currently owns automatic updates; dormant values remain editable
with their ordinary scope, but must not trigger duplicate writes.

No actual external caller of the source's types/ApplyAllNow established by bounded scans; omit a
facade. Preserve native checkbox/save contracts for normal consumers. Compatible external checkbox
changes survive stable assessments; future incompatible writers require a narrow, evidence-based
adapter or explicit suppression, not a broad ban on modified methods. Toddlers developmental rules
and Compact Hediffs display compose directly. Drug Response/Priority Treatment remain queued; this
feature must not bypass their ordinary medical eligibility or manufacture an unverified priority
rule. Any optional provider adapter is package-gated, absent-safe and shape-guarded.

## Risks / Trade-offs

- Periodic reconciliation has bounded delay → retain source 600-tick bound; test paused settings and
  ordinary advancement separately. Do not claim an optimization before measuring multi-map cost.
- Policy changes can reset manual permission → explicit UI explanation and tests; avoid a new
  permanent override feature that the source does not offer.
- Source absence/version and cached public docs → installed DLL is detailed evidence, not a 1.7
  compatibility certificate. All exact runtime paths remain unaccepted.
- Provider edits or interrupted saved state → native permission stays authoritative; never guess
  source history, silently migrate, or claim missing records preserve old manual choices.
- Native eligibility includes secure slaves → document the native definition, test it with DLC
  present and keep DLC optional; do not mistake map `FreeColonists` for only free adults.

## Migration Plan

No source import or on-disk save rewrite. Adding the feature initializes missing owned policy/history
on the loaded game and applies first-assessment rules; tell the player this can reset permissions.
The original source config/component remains untouched. Normal owned saves persist native permission
and owned history together through native save lifecycle. Repeated load must not reapply unchanged
policy. An invalid or unsupported owned history schema must be reported and leave affected automation
suspended rather than treating questionable history as a new pawn and overwriting a manual choice.
Missing history in an otherwise recognized schema is first assessment, distinct from corrupt data.

Disabling/suppressing owned automation leaves current native permission values and retains dormant
valid history. Re-enabling compares current classification/policy with the last successful assessment.
No safe removal claim until live round-trip proof. A future migration would require its own preview,
backup, atomic save publication, failure restoration and verified postconditions before adoption.

## Open Questions

ST-Q1–4 in research/ledger retain exact native/provider seams, history and lifecycle proof, shared UI,
test coverage and final in-game evidence. No additional user interview is needed for this source.
