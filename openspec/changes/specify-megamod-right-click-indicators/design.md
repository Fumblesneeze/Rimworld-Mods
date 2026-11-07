## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S110 is specification only. [Research](../../../mods/megamod/docs/refinement/2026-11-05-right-click-gizmo-indicator-research.md)
binds the complete three-class source and selected1.7 binary to author commit
557a05c5717a996aaf9bf1b65b5b649a1d92f90a. The public1.7 DLL is byte-identical to the installed one.
Native inspection began on1.7.4998 rev1299; the installation changed to1.7.5000 rev1275 during the
pass. A separately pinned update produced identical decompilations for all eighteen inspected
types and the same eight menu-getter declarations. Research retains both identities; this scoped
static comparison does not claim whole-game or effective patched equivalence.
See [proposal](proposal.md) and [contract](specs/megamod-context-menu-indicators/spec.md).

The source draws a native marker in Command.GizmoOnGUIInt's postfix. It caches both override
presence and the first enumerated menu answer by Type, special-cases allowed-area designators,
and suppresses Messages.Message while probing. Native1.7 and optional providers demonstrate
instance/context-dependent menus and getter side effects. The intended convenience is retained;
the type-wide answer and speculative suppressed probe are not.

## Goals / Non-Goals

**Goals:** First-display discoverability on ordinary native/owned/admitted command surfaces;
current per-command truth; passive drawing; unchanged native input, grouping and real messages.

**Non-goals:** A new menu system, new gameplay actions, custom mechanitor/foreign renderer coverage,
configuration/blacklist UI, persistent save state, private world/Thing index, broad message suppression,
copied raster art, runtime implementation or an unmeasured zero-performance-cost claim.

## Decisions

### 1. Separate capability knowledge from executing a menu provider

Use a small internal menu-capability result tied to the actual command and current context:
known menu, known no menu, or unsupported/unknown. Do not infer no menu solely from a getter's
declaring type: I Aint Building That can add options through a patch to an inherited getter.
Do not discover capability by calling ProcessInput, menu actions, arbitrary property getters or
enumerators on every repaint, even inside a message-suppression scope. No Messages.Message patch
is selected. Catching an exception cannot undo a getter that already mutated game or UI state.

For owned commands, compute capability alongside their actual options from the same pure
eligibility facts and expose it directly. S107 quick-storage creation/exclusion and other selected
order/storage features use that path, with no self-patches or duplicated caches. If a provider has
an admitted side-effect-free current capability query, consume it narrowly. Otherwise inspect its
exact behavior and provide a package-gated pure adapter; unknown behavior remains explicitly
unsupported until that contract exists. A previously clicked menu is not enough to promise accurate
first-display or later-state indication. Do not quietly replace the intended feature with a marker
that appears only after the player has already discovered the menu.

Native coverage must include the actual routes found in this beta, not a few sample subclasses:

| Native path | Capability facts / preservation |
| --- | --- |
| Ordinary Designator options | Effective hasDesignateAllFloatMenuOption or Designation-derived removal choices; a disabled None row still makes a menu. Do not enumerate the getter just to count things: it scans listers and mutates shared temporary designation data |
| Plan add / zone add | Respect CanRightClickToggleVisibility and ShowRightClickHideOptions; use actual effective subtype semantics, not one cached answer for all instances |
| Command_Hide concrete families | Current hide options for plans, stockpiles, growing and fishing; existing helpers yield menus even with no affected objects; do not perform their hide actions |
| Allowed-area Expand/Clear | Native ProcessInput opens a picker with Manage areas; admit its current interaction/tutorial preconditions without invoking the input action |
| Build materials / dropdowns | Already draw native indicators and use ProcessInput; recognize existing marker ownership and do not add another, including when their native marker rules differ from owned menu-availability rules |
| Non-menu Command fallback | No added marker unless a proven effective provider adds a menu; a command with an inherited base getter can still acquire foreign options |
| MechanitorControlGroupGizmo | Custom Gizmo rendering, outside the source's ordinary Command hook; retain explicit omission |

This is a bounded route inventory, not permission to equate every subclass with its base behavior.
At implementation time establish exact callable/field shapes and effective semantics for the
registered native routes. Changed subtype behavior requires an admitted capability or a scoped
unknown result. Preserve the native routing: GizmoGridDrawer first tests the displayed command's
options; if empty it routes to ProcessInput, otherwise it builds the combined group menu. A peer's
menu alone does not make the representative open a float menu. Do not replace this with any-member
aggregation or modify native menu merging/action composition.

### 2. Cache stable metadata, not mutable availability

Type-level resolution metadata can be cached only while its effective patch generation is valid.
Do not cache an instance's positive/negative answer by Type, and do not serialize indicator data.
Cheap current facts are preferable to a cache. If an admitted provider genuinely needs a cached
derived fact, name its exact selection/map/command identity and all invalidation inputs first,
including disabled state, modifier keys, option eligibility and later patch changes; verify next
display-refresh freshness. Recreated gizmos must not force expensive menu generation each frame.

Use existing native listers/shared owned candidate data if any provider requires it; this indicator
has no reason to maintain a parallel Thing index or scan the map. Clear transient references on
selection/context teardown and game changes. No worker-thread Unity/Verse access. Measure cold
discovery, warm rendering, command recreation, grouped selections and provider updates before
claiming optimization; the author's no-cost wording is not measurement evidence.

### 3. Add presentation at a verified boundary without changing input

Command.GizmoOnGUIInt(Rect,GizmoRenderParms) is the inspected normal/compact shared draw boundary;
normal Command size is75 and compact36 in this revision. Its return value and event handling remain
native. Add only repaint drawing for a known-menu enabled command that lacks an existing indicator.
Native Designator_Dropdown.DrawExtraOptionsIcon uses a16px marker with1px top/right inset. These
are measured implementation reference values, not a requirement to cover a counter or ignore UI scale.

Before implementation selects or accepts raster presentation, follow the asset skill and native
visual gate. The current resolved native helper/texture is the candidate; no copied texture or
new asset is selected in this documentation change. Preserve compatible retextures through native
resolution. When top-right content occupies the anchor, choose a clear in-button corner deterministically
and test ordinary/compact/scaled layouts; if no clear fit can be established for a custom layout,
its presentation adapter is unavailable rather than covering information. Do not add a hitbox.
Recognize existing native/provider marker ownership through admitted draw semantics rather than
pixel matching. Avoid double drawing even when a provider calls the native helper after the base
Command draw. Owned commands use one direct shared render path.

Restore any temporary GUI color/material/text state in guaranteed cleanup, including when native
or optional drawing throws. Leave tooltips, input events, GizmoResult, sounds, grouping and actual
menu construction unchanged. No general input or message interception is justified by this feature.

### 4. Optional provider and source overlap policy

`Taggerung.IAintBuildingThat` (`3118060751`) is a demonstrated optional boundary, not an adopted
feature or required dependency. Its selected1.6 code can add Hide/Restore via Designator options,
an inherited Gizmo getter for abilities and ProcessInput fallback; menuCombo.ShouldShowMenu makes
availability modifier-dependent. Its dropdown option enumeration also mutates bookkeeping. Do not
execute that getter as a probe. A pure admitted adapter must mirror the provider's current gates
and preserve native marker ownership, without caching the first modifier state. Show the indicator
for genuine Hide/Restore menus, including many build buttons; the source comment requesting a
blacklist is not a user requirement. No blanket ignore-by-package or English-label filtering.

The installed provider's 1.6 metadata and decoded Harmony targets are not beta acceptance.
Resolve effective routes and required settings/member shapes before claiming
support. Unknown or absent optional routes do not alter their menus; absent means no candidate,
active incompatible support produces a bounded skipped-patch warning. The original
CheaterEater.RightClickGizmoIndicator owns overlap if active; disable our indicator with warning,
without unpatching it or claiming that its type cache/suppression has been corrected by co-installation.

No external consumer of the source API was found in the bounded installed scan. Public initializer
type and private patch names are not sufficient reason to emulate its assembly/package. Its public
Analyzer.xml has a stale HasMenuOnce entry and is not present in the installed package; it is a
profiling reference, not a proved third-party API. No source settings or save contract needs migration.

### 5. Executable target specs before patch registration

Create indexed target documents and tests for each actual patch; the following scopes reserve
requirements but are not implemented admission checks:

| Scope | Minimal predicates | Failure boundary |
| --- | --- | --- |
| RCGI.command-marker | Exact callable draw shape, valid current rectangle/repaint state, normal/compact coverage, existing marker ownership, no skipped-original assumption or destructive event/result modification | Added marker unavailable on affected surface; native input/rendering remains |
| RCGI.native-capability | Current effective menu and input routes, disabled/tutorial gates, actual subtype flags, representative/group semantics and side-effect-free capability facts | Affected capability/marker group inactive; do not guess or enumerate a stateful getter |
| RCGI.optional-menu-capability | Canonical active package, exact consumed members/fields and current modifier/eligibility conditions; foreign addition to inherited getters accounted for | Optional adapter inactive; original provider menus remain |
| Any future invalidation hook | Exact lifecycle callback/semantic change consumed by concrete derived data | Dependent cached result and marker inactive until safely current |

Use observational postfix/public seams where sufficient; no transpiler is selected. If one becomes
necessary, first document the required snippet, operands/data flow/cardinality and actual incoming
stream checks. Original IL alone cannot prove behavior removed by a foreign prefix/transpiler.
Account for effective patch order, skip-original paths and supported later repatching; do not
speculatively run foreign transpilers or require an exact whole body/hash/version or empty patch list.

Register once, isolate atomic groups, treat false/unknown predicates, validator exceptions and
installation failures as skipped enabled patches. Remove only newly added owned patches on group
rollback. Warn once per skipped enabled patch with package/patch/spec/target/reason/unavailable
behavior/fallback; no per-frame logging. Later invalidation withdraws only affected owned indication
until revalidation. Runtime unknown instances/failures get bounded diagnostics without freezing other
commands or changing foreign state. No blanket unpatching.

There is no Def or XML mutation in this design. Any future XML Extensions operation first requires
the exact guarded mutation contract and hard dependency, preserving unrelated content. Harmony
becomes required with its first use; no bundled upstream assembly, Harmony or developer Gateway.

## Risks / Trade-offs

- Arbitrary getters can have side effects → pure capability facts/verified adapters, no suppress-and-probe
  workaround. Unsupported providers are explicit; admitting all foreign code without evidence is not parity.
- Dynamic/grouped menus → current per-button route and full modifier/eligibility lifecycle, not per-type truth.
- Existing native markers can outlive usable options → preserve native-owned cues; the owned policy
  controls only newly added markers and does not redefine native build UI.
- Occupied corners/custom renderers → actual layout/asset review and explicit unsupported custom
  surfaces; no false universal claim or loss of original labels.
- Presentation-only intent can still change play → native actions/messages/selection remain unchanged;
  test before/action/after behavior, not a screenshot of a static icon alone.

## Migration Plan

No deployment in this change. Future focused implementation adds guards and marker capability
slices, then review/package checks and final native workflows. No saved-state migration is required
for the source feature; owned caches remain transient. Enabling/disabling this indicator must not
rewrite saves, global options or other features' state. This does not claim arbitrary removal of
the entire megamod is safe. No source texture, binary or settings are copied.

## Open Questions

- **RCGI-Q1:** Exact effective render/menu/input/group/marker-ownership target contracts and later
  repatch detection on the implementation revision; full ordinary native route admission.
- **RCGI-Q2:** Safe current optional-provider capability queries, actual I Aint Building That beta
  target shapes and modifier conditions, real consumers and shared owned-command composition.
- **RCGI-Q3:** Final scaled/counter/compact native appearance, bounded cold/warm costs, seven-language
  workflows, honest TDD and reviewed-build causal native/visual acceptance.
