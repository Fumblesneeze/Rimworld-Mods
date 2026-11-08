## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). The user selected
RimWorld 1.7 beta. The requested deliverable is an initial scaffold and enforceable development
process. At the initial scaffold checkpoint no source inventory or gameplay slice had been supplied.
The subsequent 137 feature drafts now have a [shared architecture](../plan-megamod-parallel-implementation/design.md),
[parallel roadmap](../plan-megamod-parallel-implementation/roadmap.md) and
[governing tasks](../plan-megamod-parallel-implementation/tasks.md). Their user-authorized process
exception makes the orchestrator responsible for integrated native acceptance after each part's
chunks are ready; all foundation behavior and safety obligations below remain in force.

## Goals / Non-Goals

**Goals:** one independently packaged mod, traceable feature contracts, tolerant but meaningful patch
guards, independent source-behavior research, composed behavior across source mods, and
shared data ownership where consumers actually share a dataset.

**Non-goals:** porting named examples now, claiming compatibility from their names, designing a
general plugin platform, original-mod API/drop-in compatibility or consumer/facade work,
implementing empty cache/patch registries, adding placeholder tests, or
publishing a Workshop item. Runtime requirements below are specified for subsequent vertical slices.

## Decisions

### Initial support and original-provider handling — 2026-11-08 clarification

Initial support is new-colony single-player without CE. Ordinary saves subsequently created with
megamod must still reload; this is not a requirement to start a new colony on every load or update.
Do not build original-mod save import or multiplayer/CE adapters in the initial feature slices.
Existing source research about those integrations remains reference material, not supported scope.

Resolve canonical active package IDs against the original-provider identities recorded for each
replacement. An active original triggers a bounded startup warning naming it and the corresponding
owned features, plus best-effort suppression through those features' ordinary settings/effective-off
path. Apply the same behavior and timing as a manual off choice, using the common settings resolution
path without overwriting stored global values, save overrides or inheritance choices. This does not
guarantee suppression before every C#/XML/Def mutation or rollback of effects that an ordinary off
choice would leave in place. The warning states best-effort disablement, not coexistence compatibility.

Retain independent features and the shared services they need. Do not unpatch foreign owners, spoof
their IDs, edit their data or change the user's mod list. Installed-but-inactive packages do not
trigger this policy; normal preference resolution resumes on a later startup without the original.
Provider identification and the corresponding feature-setting mapping come from source research,
not a similar title. Do not add an exhaustive original-mod compatibility matrix, consumer audit or
facade. This clarification supersedes the earlier all-phase suppression guarantee.

Ordinary game-target semantic checks continue to admit compatible unrelated patches. Genuine
external integrations actually used retain the root package/Def resolution, absent-safety and shape
guards. Those requirements and normal persistence of our own saves are not waived by this policy.

The [round-one decision record](../../../mods/megamod/docs/refinement/2026-11-07-decisions-round-1.md)
tracks the accepted feature refinements and remaining draft-reconciliation work. Neither this
scope decision nor that record claims implementation or compatibility acceptance.

### 1. An inert, buildable product first

Use Zlepper ModSdk 0.0.9 and the repository's versioned build imports, selecting net10.0 and refusing
non-1.7 installations. Generate the assembly metadata and About.xml from `megamod.csproj`; add no
`Mod` subclass, startup initializer, Harmony dependency, or gameplay Def. This keeps the scaffold
honest and avoids implementing behavior without a first acceptance scenario. The seven language
catalogs have exactly zero keys because there is no runtime UI. Add contextual text with its feature.
Register the project in the solution for navigation but omit default `Build.0` selection so the
documented 1.6 aggregate remains usable. Build megamod through its owning operation/project. Reuse
the existing versioned-build tooling; those imports must be delivered with or before this scaffold.

The release profile identifies the inspected 1.7 installation and exact allowlist. Its private,
unpublished state is not publication approval. A labeled SVG development placeholder satisfies the
profile's required preview path; it is excluded from the package and must be replaced with reviewed
release presentation before publishing. No raster art or generated illustration is accepted here.

### 2. Organize by behavior, with small shared seams

Future source goes under `Source/Features/<Feature>/`; external adapters stay beside their feature in
`Compatibility/<ExternalMod>/`. Only genuinely shared Thing data belongs in `Source/ThingData/`,
and only the concrete admission mechanism belongs in `Source/Patching/`. These are documented
destinations, not empty frameworks. Tests live under repository `tests/` in the honest Unit,
Harmony, Defs, isolated integration, and E2E tiers as actual requirements demand.

### 3. Analyze upstream evidence before implementing a replacement

Maintain `docs/replacements/README.md` as the coverage index. Each researched source gets a report
from the supplied template recording exact identity, source/license provenance, player behavior,
options/defaults, observable persistence, interactions, deviations, omissions, and evidence.
Convert findings into observable OpenSpec scenarios owned by megamod. An original feature uses the
same scenario and acceptance discipline with `origin: original`; it does not invent an upstream mod.

For an upstream mod that modifies another listed mod, record the dependency/behavior graph and
resolve their effective behavior, load-order effects, options, and conflicts before coding. Express
the chosen composition in one owned feature contract and ordinary calls/policies. Do not preserve
upstream internal patches or patch megamod with Harmony. Original-mod API parity, external consumer
checks, compatibility facades, original saved-schema import and drop-in/coexistence work are excluded.
Research observable behavior without copying implementation. Separately specify our own save schema
and the contracts of external integrations actually used; those remain subject to the root safeguards.

### 4. Every patch has a minimal target spec

Keep a stable per-patch document under `docs/patch-targets/`, linked bidirectionally to the owning
feature spec, implementation, validator, tests, and live evidence. The target spec describes the
external seam, exact callable shape, phase, minimal semantic predicates, allowed variation, failure
scope, and observation needed to prove the behavior. Documentation alone is not a runtime guard.

An observer postfix may need only a resolvable method and the signature needed to bind its own
parameters. If it must observe an executed body, it must also account for prefixes that skip it.
An argument-writing prefix must establish that the relevant argument still reaches the documented
operation on the relevant control-flow path. Structural IL/data-flow checks identify operands,
calls/fields, relationships, and required cardinality; compiler-specific offsets/local numbers,
full-body hashes, exact versions, and unrelated instructions are not compatibility gates.

Distinguish original IL from the effective patched method. The validator must observe the surface
that its assumption concerns, including relevant earlier transpiler effects, skip behavior, and
argument-changing prefixes. A body-dependent patch cannot claim safety from original IL alone when
the active chain changes that body. Document initialization/order guarantees and how subsequent
repatching invalidates admission. A transpiler must also recheck its actual input on every execution
and preserve it unchanged on failure. Do not run arbitrary upstream transpilers repeatedly just to
probe them. Select a supported inspection/revalidation seam during the first runtime slice.

Evaluate enabled candidates at startup, before installing them; delay only to a documented startup
phase when the required assemblies/Defs exist. Use one idempotent registration path, with no
unguarded `PatchAll`. Missing/ambiguous/unsupported evidence, predicate failure, or validator errors
skip only the affected patch or its declared atomic feature group. Clean up only newly installed
owned patches if that group fails. Emit one bounded warning per skipped enabled patch, including
patch/spec ID, target, predicate/reason, and unavailable player behavior. Never unpatch other owners.
Compatible patches by other mods and unrelated method edits are allowed. Missing optional providers
are candidates only when the feature explicitly requests them; ordinary disabled features are not
warnings. There is no blanket ban on other Harmony owners.

For XML-only modifications, use XML Extensions to guard required Def/node existence and shape before
mutation. For external content, also gate by canonical package ID with `XmlExtensions.FindMod`
(`packageId=true`); package presence alone does not prove the target content exists. This makes XML
Extensions a hard dependency when the first such feature is implemented. Preserve the same spec,
no-partial-application, and startup warning contract; implement any missing diagnostic seam in the
first real XML feature. Do not use runtime Def mutation as a shortcut around declarative XML.

Prefer the narrowest additive/merge-preserving operation that keeps existing entries and unrelated
fields. Add only missing membership entries and test that already-present entries do not duplicate.
Never replace a woody category or fuel-filter list just to add one Thing. Whole-list replacement is
permitted only when the feature explicitly intends to replace the list: record the reason merging
is unsuitable, expected final contents, ordering, discarded entries, and external compatibility
impact. Inspect the installed XML Extensions operation contract rather than inventing merge syntax.

### 5. Share authoritative data, preserve each consumer's rules

First inspect native map listers/indexes. Reuse them when they provide the required dataset; maintain
one additional map-owned index only for proven missing needs. Search and hauling can share candidate
Things, not pawn-specific eligibility, reservations, reachability, visibility, or job outcomes.
Derived views declare their keys, freshness, invalidation, and memory cost. Rebuild once per invalid
generation on the main thread, or update incrementally; do not independently scan/cache the same
source list per feature. Revalidate native job/access rules immediately before an action commits.

Data is transient, map/game scoped, cleared on teardown and rebuilt after load. Handle spawn,
despawn, destruction, movement, holder/map transfer, stack split/merge, and metadata changes for any
field actually indexed. Invalidation hooks are patches too and require target specs. Unknown
freshness uses the specified native-query fallback or marks the feature unavailable with a bounded
warning. Measure allocations, maintenance cost, memory, and query time with multiple consumers;
do not infer a performance win from fewer class names.

### 6. Classify UI by purpose, not upstream placement

Audit every source's bottom tabs and other entry points, including dynamically injected controls,
before porting UI. Megamod's placement rules supersede upstream placement; record relocations as
intentional differences. First separate mixed-purpose windows into commands/configuration/mechanics
so a settings page or cheat cannot acquire a bottom tab by sharing a container with ordinary play.

Debugging and unrestricted cheats/overrides belong in the top developer toolbar alongside native
debug controls, visible only with developer mode. Character-editor-style changes outside sensible
game mechanics are overrides even if they only change appearance. Ordinary configuration belongs
in the mod settings menu, including save-specific configuration. Organize settings in a stable,
documented feature/category order with clearly labeled Global default and Current save controls.
Save controls are disabled, with a reason, when opened at the main menu; they must not create
or edit an implicit game or stale last-loaded save. A loaded game in world view still has save scope.

A control that directly toggles a current-play view/behavior belongs among the small bottom-right
controls, like vanilla automatic rebuilding. This is distinct from configuring that behavior's
defaults/parameters in settings: merely persisting a toggle's state does not turn it into a settings
window. Cheats still route to debug controls first. Only a window that exposes an ordinary gameplay
mechanic to control qualifies for a bottom tab; a window wrapper around a toggle does not qualify.
Prefer existing native contextual controls for functions outside these categories instead of
inventing a new tab. Document deliberately changed navigation and its player impact; this UI audit
does not require preserving the source mod's callable API or checking its consumers.

The first UI slice must specify exact ordering, labels, state scope, and enable/disable conditions,
and verify entry point → action → observed result, including developer mode on/off and main-menu
versus loaded-save settings. This scaffold creates no menus, tabs, controls, or persisted settings.

### 7. Share settings resolution, persistence, and row rendering

Distinguish configuration from gameplay state before applying this model. Choices attached to a
particular pawn, area/zone, bill, building, or other gameplay entity are gameplay state, not mod
settings, even though saved. Put them in native contextual controls or an appropriate mechanic's
management window. Unrestricted pawn/area overrides still follow the debug classification.

Ordinary mod configuration defaults to an editable global value and a per-save Use global flag
(true when missing/new), with an optional saved override. A common resolution function returns the
current global default while inheriting, otherwise the current save's override. The global default
falls back to the definition's built-in value when absent. Global edits intentionally flow to saves
that inherit, while overridden saves retain their value; feature specs state application timing.

One common path loads/stores global defaults and per-save inheritance/override data and renders all
feature rows. Definitions supply stable identity, type/default/validation, feature/order, labels,
and documented scope exceptions. Each normal row shows **Use global**, **Current save**, and
**Global default**. The save editor is inactive while inheriting, showing the effective value; both
it and Use global are inactive at the main menu. Global controls remain active. Callbacks enforce
the same policy, so disabled appearance is not the sole protection.

Enabling inheritance preserves any dormant override; disabling it restores that value or initializes
a first override from the current effective global value, avoiding a surprise jump. Save/load and
save switching preserve flag/value ownership without copying inherited globals into stale save
snapshots. If inheritance is impossible or nonsensical, the feature spec and definition record why,
the actual scope/persistence, and renderer behavior for unavailable controls. Such settings still use
the common path. This is a requirement for the first real settings slice, not an empty framework to
implement in the scaffold. Test resolution, serialization, state switching, row behavior, and exceptions.

Original-provider detection uses the same effective-off path and effect timing as an ordinary manual
feature-off choice. Keep this startup policy separate from the stored preference: neither global
defaults, saved overrides nor inheritance flags are rewritten. A later startup with that provider
inactive resolves the retained preferences normally, subject to target admission. This is best-effort
avoidance of duplicated behavior, not a guarantee that the original and megamod are compatible.

## Risks / Trade-offs

- Beta engine changes → pin build identity for evidence; semantic guards and new live verification
  determine support, not the version label alone.
- Overly strict probes reject safe mods; weak probes corrupt behavior → document allowed variants,
  test both positive and negative structural fixtures, and verify genuine external integrations
  actually used. Original-provider warning/effective-off checks do not establish coexistence support.
- Startup checks cannot predict arbitrary later runtime patches → declare ordering/lifecycle limits,
  revalidate when the supported seam changes, and do not claim unobserved combinations as supported.
- An original remains loaded → warn and use the ordinary feature-off path on a best-effort basis;
  do not claim API parity, original-save import, complete suppression or coexistence compatibility.
- Shared caches amplify stale-state bugs → one owner, explicit invalidation and freshness, native
  action revalidation, teardown tests, and a safe fallback.

## Migration Plan

The scaffold has no persisted state and introduces no migration. Build/profile/package verification
is sufficient for this inert scaffolding scope; it does not close any gameplay requirement. Every
future migration of our own save/settings schema needs an explicit source schema, backup, atomicity,
rollback/failure restoration, and verified postconditions. Original-mod state import is outside
initial support. Installing remains the canonical local package-ID sync;
do not enable the mod in the user's normal configuration automatically.

## Open Questions

The initial feature order, exact upstream identities/versions, composed behavior, and intentional
deviations will be resolved during intake. No behavior, timing, balance, default setting, or
compatibility assumption is silently chosen from the example mod names.
