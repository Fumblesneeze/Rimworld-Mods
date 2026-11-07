## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
S071 replaces Better Message Placement (Continued), `Mlie.BetterMessagePlacement`, Workshop
2259516339. This design is unimplemented. The [research](../../../mods/megamod/docs/refinement/2026-11-04-better-message-placement-research.md)
records documentation-first inspection, selected binary/hash/license and native 1.7.4998 rev1299 evidence.

Source injects additions after loosely matched Vector2 x/y loads in Messages.MessagesDoGUI. Its
helpers include MessagesTopLeftStandard again, use Prefs.ScreenWidth/Height, and infer rows from
colonist-bar scale. An incomplete match can leave a partially changed stream. Source sliders post
ordinary historical messages. These are implementation findings, not desired parity.

Native MessagesDoGUI applies the standard (140,16) and lesson offset, then calls Message.Draw in
newest-first order with 26-unit spacing. Message.CalculateRect, lastDrawRect and its immediate window
carry drawing and interactions together. WindowStack.ImmediateWindow stores the supplied rectangle;
Window.WindowOnGUI rounds it and passes it to GUI.Window. No explicit viewport clamp was found on
that inspected managed path; Unity rendering/input still require live verification.

UIRoot_Play calls base UIRootOnGUI (messages) before MapInterface/WorldInterface draw the bar. A
previous-frame-only rectangle capture can lag at initial display, changed rows or resolution. Native
ColonistBar.Entries prepares dirty layout; DrawLocs/Scale alone do not. Native visibility also depends
on view routing, showColonistBar, UI dimensions, tile picker and screenshot filtering. Empty groups
can still have visible frames. These facts rule out a portrait-count heuristic.

## Goals / Non-Goals

**Goals:** reliable short-message placement, current native geometry, unchanged message behavior,
common scoped offsets, inert preview and atomic compatible integration with explicit fallback.

**Non-Goals:** a notification manager, new archive, wrapped/reordered messages, collision avoidance
for every window, private pawn/Thing index, source-identity emulation, wholesale settings migration,
or unsupported Colony Groups compatibility. No new tab, toggle, debug function or raster asset.

## Decisions

### 1. Translate the native message stack once

Use one common translation at the point where native message coordinates, including the active
lesson contribution, feed the draw loop. Retain native Message.Draw and all posting/lifetime/input
paths. Do not replace the loop with a second renderer or move only the text. Required semantics are
the consumed coordinates, their complete lesson contribution, one coherent translation and the
unchanged native relative arrangement. Exact implementation targets remain BMP-Q1, not approved patches.

Measure the native stack's padded rectangles with the correct font/UI scale without mutating
lastDrawRect during speculative sizing. Pass only final positions into the native drawing path so
backgrounds, hitboxes, hover and colonist-bar collision/fade queries agree. Native cached text sizing
can be reused where admitted; any needed extra measurements are bounded by the live message list.
Preserve/restore GUI font/color/matrix state. Do not add a separate message collection or lifetime cache.

### 2. Resolve current visible bar geometry directly

Read native prepared layout at the draw boundary. Bound visible portrait, label-background, weapon,
group-frame and selection/mood-outline extensions, rather than using the 0.58/0.42 allowed-row
thresholds. GenMapUI's 100-unit label container is not its visible label height; inspected backgrounds
are 12/16 units depending on tiny-text preference. Derive only consumed visible geometry and verify it.

At zero offsets the top of the first padded message rectangle is at least **4 logical UI units** below
the lowest visible bar content, unless viewport fitting forces overlap. Four is a provisional named
layout constant, recorded in the spec; the existing vertical setting provides adjustment. Keep the
native baseline if it is already lower. Do not add native origin or lesson offsets again. No visible
bar means no bar reserve, including main menu and genuinely hidden/suppressed bar states; world-view
bar content is handled by the same visibility contract.

S001 contributes its **effective visible presentation bounds** through a direct owned call: completely
hidden entries contribute nothing; reduced entries contribute only their visible name/frame/portrait
parts; labels/health strips, indicators, outlines and weapons contribute their actual rendered extent,
including configured weapon scale and vertical offset. Do not add hidden native portrait rectangles
back into that result. Separately visible native group frames still count. Consume the presentation
prepared for that draw rather than independently reimplementing S001's filtering or refreshing its
gameplay state on a different schedule. This preserves S001's configured update timing and avoids
duplicated decisions. No Harmony patch against megamod. A GUI-pass-local geometry value is sufficient;
invalidate/recompute when the native view/layout or composed presentation changes. Do not introduce
a second colonist census, map scan, persistent per-port cache or universal screen-layout service.
The shared Thing index has no role here.

### 3. Manual adjustments and viewport fitting have a fixed order

Treat horizontal/vertical percentages as a common scoped position pair, default **0/0**, each finite
and within **−100%..+100%**. Convert using current logical UI.screenWidth/Height, not stored physical
Prefs resolution. Apply once after the automatic anchor, allowing deliberate negative/manual overlap.

Then constrain one translation for the whole stack: if its native bounding extent fits on an axis,
clamp that extent into the viewport on that axis. If too wide, align its left edge with the viewport;
if too tall, constrain the newest message vertically when it fits, otherwise align its top edge.
Keep the other messages' original relative positions, including native multiline limitations. Never
clamp individual older messages onto each other. Overflow can clip; do not rewrite text, change
spacing, remove/archive messages early or manufacture a scrollable replacement window. Viewport
fitting can override bar clearance. This is deterministic and smaller than a collision packer.

Valid stored offsets are not rewritten by fitting, screen changes or hidden-bar fallback. Invalid
loaded values fall back for the complete pair to 0/0 in their existing scope with an explanation;
do not partially accept one field or modify global settings as a side effect of loading a save.
UI edits validate both values atomically; reject invalid edits without changing the previous pair.

### 4. Use common settings and an inert preview

One **Interface / Messages / Position offset** compound row shows the two controls in each scope
with Use global. The pair inherits together, retaining dormant overrides. Save reset sets the pair
to 0/0 only when its override editor is enabled, without changing Use global. Global reset only
resets the global pair. Main-menu callbacks cannot mutate Use global, a save pair or a save reset.
Changes affect the next message draw; no reload, repost or timer restart.

Show a labeled, scaled viewport preview inside settings with a sample stack and current bar reserve.
It uses effective current-save values during play and global values at the main menu, with that
scope named. Changing a global value under a save override therefore need not move the effective
preview. The preview uses the same placement policy and does not enter Messages, Archive, look-target
selection or sound paths. Refresh on independent coordinate changes, reset, inheritance, view/scale
and save changes; compare the pair, never x+y. Dispose transient preview state when settings close.
This preview is not native behavior acceptance and does not promise avoidance of the settings window.

Translate row labels, percentage/range help, preview scope, intentional overlap/overflow, reset,
fallback and provider explanations in all seven baseline languages. No source VersionFromManifest
footer dependency; the megamod's ordinary version presentation remains authoritative.

### 5. Admit only supported contributions and preserve external authority

Before coding, create and index each actual target spec with stable ID, minimal consumed semantics,
exact incoming/effective assumptions, cardinality, native fallback, tests and live workflow. Candidate
boundaries below are design responsibilities, not assertions that these exact patches exist:

| Boundary | Required evidence | Failure result |
| --- | --- | --- |
| Native position contribution | Coordinates after native lesson handling reach the normal draw loop exactly as consumed; translation preserves message geometry/actions | Entire owned position contribution inactive; native/source placement remains |
| Native bar geometry | Current layout preparation, actual draw visibility and consumed bound calculations agree with the supported bar | Omit automatic bar reserve; admitted native anchor plus manual offsets/fitting still work, with status explanation |
| Optional foreign bar adapter | Canonical active package plus exact callable/layout/visibility shape, effective semantics and proven consumed contract | Adapter inactive; same explained no-automatic-clearance fallback |
| Common settings preview | Shares the admitted effective policy; no mutation of message/history/targets | Show unavailable state rather than pretend the real game uses an unsupported placement |

Every actual transpiler validates its actual incoming stream on every execution and returns that
stream unchanged on false/unknown/ambiguous predicates. Materialize/validate before rewriting; no
partial X-only/Y-only result. Original IL alone cannot prove semantics changed by another transpiler.
Account for relevant skip-original prefixes and later repatching without speculatively reexecuting
foreign transpilers. Installation/validation exceptions roll back only newly added owned patches in
the declared group. Do not reject whole hashes/versions or merely another Harmony owner. One bounded
startup warning per skipped enabled patch names package/patch/spec, target, reason, lost behavior and
fallback. Runtime invalidation transitions likewise remain bounded, not per-GUI log spam.

When **Mlie.BetterMessagePlacement** is active, it is the sole position provider: owned placement
stays dormant, settings retained, status explains source differences and owned preview unavailable.
Never unpatch the original or silently import its config. Source metadata marks
**DerekBickley.LTOColonyGroupsFinal** incompatible; do not copy that whole-product flag. That known
replacement needs a proved geometry adapter before automatic clearance is claimed. Other foreign
bar changes are assessed by consumed semantics; package presence alone neither grants support nor
forbids an otherwise compatible patch. Dynamic Message Positioning is a reference, not selected scope.

The bounded installed XML/CS/JSON audit found no actual source API consumer, but did not inspect every
assembly/reflection string. Preserve native message interfaces; do not fabricate public source facades.
No XML contribution is selected. Harmony will be required; optional bar/source mods and DLC are not
requirements. No source binary/helper, other repository mod or Gateway reference in the product.

## Risks / Trade-offs

- **Bar draws after messages** → prepare current native layout; verify first frame/resize/group change,
  not merely steady-state screenshots or a previous-frame rectangle.
- **Visible extensions differ from portrait bounds** → direct S001 effective-presentation composition,
  including hidden/reduced entries and adjustable weapons, plus exact label/weapon/frame
  evidence and native screenshots; unsupported providers lose only their unproved clearance contribution.
- **Manual placement or small viewport overlaps the bar** → explicit priority/range/help and native hitboxes;
  no claim of universal non-overlap or all-message visibility.
- **Native multiline/window behavior** → retain spacing and test actual clipping/input on reviewed build;
  static IL does not prove Unity's rendered result.
- **Source coexistence or unavailable integration** → retain preferences, name the provider/fallback and
  disable misleading owned preview; do not claim source bugs are fixed while source is authoritative.
- **Cost of geometry/text measurement** → use native prepared data and bounded live-list work, measure
  combined S001 costs before optimization claims; no private per-feature world scan.

## Migration Plan

Add the admitted implementation only after focused RED/GREEN and current target documents. New/missing
settings use global; source config remains untouched. No import, save conversion or source-class alias
is selected. Any later migration requires backup, atomic commit, failure restoration and postcondition
verification in its own contract. Disabling/removing owned placement leaves ordinary message data and
native actions intact; do not clear messages, archive records or user preferences on admission failure.

## Open Questions

**BMP-Q1–4** in the [progress ledger](../../../mods/megamod/docs/refinement/specification-progress.md)
retain current target/geometry proof, proposed layout/preview choices, provider/consumer boundaries and
tests/localization/cost/native acceptance. They do not ask the user to re-explain upstream behavior.

Semantic pass: only message readability/navigation changes. Native events, warning eligibility,
quest outcomes, expiry/sound/archive policy and game progression remain native. Preview creates no
game event. No cost/reward, research, material, hauling or work-priority change is proposed.
