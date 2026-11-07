# S071 — Better Message Placement research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Read-only inspection **2026-11-04**. Origin replacement. **Observable draft independently reviewed
and validated. No implementation or acceptance.**

Owning [proposal](../../../../openspec/changes/specify-megamod-message-placement/proposal.md),
[design](../../../../openspec/changes/specify-megamod-message-placement/design.md),
[spec](../../../../openspec/changes/specify-megamod-message-placement/specs/megamod-message-placement/spec.md),
[tasks](../../../../openspec/changes/specify-megamod-message-placement/tasks.md) and
[replacement coverage](../replacements/better-message-placement.md).

## Primary documentation and exact identity

[Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2259516339), installed README and
[author repository](https://github.com/emipa606/BetterMessagePlacement) were inspected before binary
analysis. The stated purpose is moving short messages away from the colonist bar. Documentation
explicitly redirects Colony Groups users to Dynamic Message Positioning. Installed About declares
`DerekBickley.LTOColonyGroupsFinal` incompatible. This is source metadata/documentation, not a proved
conflict in an owned implementation or permission to mark the entire megamod incompatible.

Source **Better Message Placement (Continued)**, package **Mlie.BetterMessagePlacement**, Workshop
**2259516339**, maintainer Mlie, About author Dyrewulfe. Manifest **1.7.0**, selected
`1.7/Assemblies/Better_Message_Placement.dll`, assembly **1.7.0.0**, file **1.7.0**,
informational version `1.0.0+436f2da6518041728c9a09c72d25d9d872cfd9ae`.
Installed path `F:/Steam/steamapps/workshop/content/294100/2259516339`. No LoadFolders.xml;
the selected 1.7 folder and shared root content follow the ordinary versioned layout.

MIT LICENSE.md credits D. R. Gunter (2019). No source code/art/binary copied into product or executed.
Remote repository Source folder was visible but its page fetch failed; no installed source C# was
present, so the selected binary was decompiled read-only instead. Its four classes were read fully.
The adjacent VersionFromManifest.dll supplies a version footer; do not introduce it as a product
dependency. Legacy 1.0 includes 0Harmony.dll; never bundle it. Required current source dependency is Harmony.

| Input | SHA-256 |
| --- | --- |
| About/About.xml | `FE85F6A525978AE8E237E4ABC5DC3B5A728E7A3BF3D5E559D6B93296AA254B48` |
| About/Manifest.xml | `F9E2FF2EF5DA8500F44E2D79E657F0205853A7F256CEADF15AEBA1E2A9D7CDDE` |
| Selected 1.7 DLL | `91B07FC549323BA0EB794999539E9C88F16DC89B4B87A2F4B85ABC70BCC862B1` |
| Native 1.7.4998 rev1299 Assembly-CSharp.dll | `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |

Local artifacts: `artifacts/Refinement/megamod-spec-20261104/better-message-placement/{upstream,native}/`
and consumer-paths.txt. Static findings are not live layout/interaction acceptance.

## Complete selected-binary behavior

| Surface | Inspected behavior |
| --- | --- |
| Startup | Unguarded PatchAll owner rimworld.dyrewulfe.bettermessageplacement |
| Messages_MessagesDoGUI | Transpiler inspects Vector2 x/y field loads and injects offset-helper call plus add after the following instruction; source assumes the expected integer conversion/context |
| Horizontal helper | Returns native standard x plus integer Prefs.ScreenWidth × xOffsetPercent when CurrentMap exists; standard x alone otherwise |
| Vertical helper | Returns native standard y plus int(ColonistBar.Size.y + 24 × scale) × estimated row count and integer Prefs.ScreenHeight × yOffsetPercent when CurrentMap exists; standard y alone otherwise |
| Estimated rows | 1 for scale >0.58, 2 for >0.42 through 0.58, 3 otherwise |
| Settings | Two global floats, xOffsetPercent/yOffsetPercent, both default 0, each slider −1 through +1 (−100% through +100%) |
| Slider feedback | With Current.Game, emits a SilentInput test message when old x+y differs from new x+y |
| Reset | Both floats set to zero after the preview-change check; reset itself has no separate preview emission |
| Version footer | Shows manifest-derived source version when available |

The two source helpers include the native standard value even though the transpiler adds their
results to that value. Against the inspected native method this derives x=280 before manual offsets,
and y=32 before row/lesson/manual additions, because native standard is (140,16). This is a static
double-origin finding, not a desired contract or observed screenshot. No-map helper return still
doubles the origin and omits the configured percentage adjustment. CurrentMap absence must not be
equated with every world-view state without inspecting actual view lifecycle.

Matching is not anchored to the actual MessagesTopLeftStandard data flow; it uses preceding x/y
field loads and stops after both flags are true. It can leave an already modified instruction stream
when one side fails, merely logging an error. Empty streams and changed/effective foreign code are
not safely admitted. Owned implementation needs one atomic position contribution and validation of
the actual incoming stream/required anchor use, not this permissive first-field match.

Source sliders use stored physical-resolution preferences rather than current scaled UI dimensions.
Values can move text out of the view; no clamp, overflow recovery or visibility check is present.
The x+y preview test is not independent field-change detection; equal-and-opposite changes can cancel.
Preview uses normal Messages.Message with historical default true, so it can enter the game's archive;
native duplicate-message handling limits repeated identical live messages rather than creating one
new entry for every slider frame. This should not be described as a custom harmless preview system.

## Native message behavior inspected

Complete small Messages and Message classes were read. Native messages are distinct from letters,
alerts, developer logs, combat motes and every other UI notification surface.

- MessagesDoGUI uses Small font, standard position **(140,16)**, adds an active lesson's y offset,
  draws newest messages first and advances y by **26 UI units** per live message.
- The native live list caps at **12**; default lifetime **13 real-time seconds**, fade **0.6 seconds**.
  Normal message posting can archive the message and play its type sound. Repeated text/targets can
  refresh/flash an existing live message. Relocation should not become message-generation/lifetime policy.
- Message.CalculateRect measures text, expands its rect by **6 horizontal / 2 vertical UI units**
  and records lastDrawRect. Draw uses that position for its immediate window, label, highlights,
  hover target feedback and click hitbox. A text-only visual shift would leave interactions wrong.
- Message.ShouldDrawBackground depends on native window conditions; ordinary play can draw text
  without a background. Clearance therefore uses padded display bounds in both states and does not
  make hidden backgrounds visible.
- In play, right-click expires the message; left-click can jump/select the primary look target or
  open/select its visible quest. Hover calls Messages.Notify_Mouseover, feeding target highlight.
- ColonistBar.GetEntryRectAlpha calls Messages.CollidesWithAnyMessage using lastDrawRect and reduces
  alpha on overlap. Preserve accurate native geometry, not just an apparently moved string.

Native UI.ApplyUIScale computes UI.screenWidth/Height from actual Screen dimensions divided by
Prefs.UIScale and installs that GUI transform. Prefs.ScreenWidth/Height return stored resolution
preferences. Their coordinate meanings differ when scaling/window dimensions differ. A sensible owned
percentage policy must specify the visible UI viewport rather than silently copying this mismatch.

## Native colonist-bar geometry and draw lifecycle

ColonistBar, ColonistBarColonistDrawer and selected DrawLocsFinder paths were read. Entries recaches
on access; DrawLocs/Scale alone return cached values. Base portrait size is 48×48, top margin21,
vertical row spacing uses (48+32)×scale and horizontal spacing uses (48+24)×scale. Source's thresholds
match native allowed-row limits, not proof of the actual occupied row count.

Portrait rectangles alone do not bound all visible content: labels are drawn near the lower edge,
optional weapons below the portrait, group frames extend by 12×scale, and selection overlays can
extend further. Own S001 mood outlines and admitted providers also need consistent bounds. Do not
invent a universal screen-avoidance service or scan all pawns to reconstruct what native layout knows.

ColonistBar.Visible checks minimum logical width800/height500 and active tile picker. Native
CheckRecacheEntries populates entries only with showColonistBar enabled; an empty map-group entry can
still participate in a visible group frame, so zero pawns is not by itself proof of zero bar content.
ColonistBarOnGUI also skips Layout events. Actual UI routing is relevant in addition to that getter.

UIRoot_Play.UIRootOnGUI calls base UIRootOnGUI first. The base draws Messages when screenshot-mode
event filtering permits, before the later MapInterface/WorldInterface bar draw. MapInterface draws
the bar when CurrentMap exists and DrawingMap, subject to screenshot filtering. WorldInterface draws
it in WorldSelected with active world camera, Playing state and permitted screenshot event. A
previous-frame-only bounds capture would not prove correct initial/changed-row placement. Use current
prepared native layout and actual visibility; no independent map/pawn census is needed.

GenMapUI's label background has actual height12/16 depending on DisableTinyText, with horizontal
padding4/6. The 100-high label text container is not a 100-high visible label reserve. Group frames
extend12×scale, weapon rectangles use portrait-relative scaling/offsets, and S001 mood outlines need
direct composed bounds. Exact final visible edges and Unity rounding remain live verification work.

Lesson.MessagesYOffset is virtual (base zero); preserve the actual active lesson contribution once
rather than replacing the lesson system. WindowStack.ImmediateWindow updates the supplied rectangle
on repaint; ImmediateWindow/Window then round and pass it to GUI.Window. No explicit screen-fit clamp
was found on this managed path. This supports specifying owned fitting, but is not a claim that
Unity's clipping/input has been verified in game.

## Consumers and UI

A completed bounded installed XML/CS/JSON search included source identifiers and Colony Groups ID.
Matches were own About, RimPy, LayoutHarness sample mod lists, and unrelated mods' LTO integration
metadata/XML: S001 Color Coded Mood Bar, Hide Cryptosleep Pawn and TDFindLib. Those LTO references
are not consumers of Better Message Placement's runtime API. No actual source API consumer was
found; this is not an all-binary/dynamic binding audit. Actual Colony Groups implementation was not
located by that search and no adapter has been analyzed. Dynamic Message Positioning is a documented
alternative reference, not a newly selected replacement entry.

No source bottom tab or operational button. Its offset configuration belongs in the common mod
settings model; source global-only values should receive ordinary global/save inheritance unless a
documented exception is selected. Native message target actions remain on the relocated messages.
Preview/reset controls belong with the settings they explain, not a new gameplay management tab.
Installed English, German, French and ChineseSimplified strings exist; owned UI needs all seven baseline
languages. No new raster art is indicated.

## Selected draft choices and remaining questions

These are owned proposals, not a claim that source behavior already provides them:

- Preserve native baseline/lesson once; raise only to clear actual visible bar content by four logical
  UI units. Four is a provisional named layout constant, not a measured balance value. No bar means
  no reserved bar space; world view uses the actual visible bar.
- Use a common scoped 0/0 pair, finite −100%..+100% per axis, applied once using current logical UI
  dimensions after automatic placement. Manual overlap is intentional and remains possible.
- Fit the native stack as one translation on each axis where it fits. Too wide starts at the left
  edge; too tall prioritizes the newest message vertically, then permits ordinary overflow. Viewport
  fitting can overlap the bar. No text/spacing/lifetime rewrite or replacement notification manager.
- Use a labeled scaled viewport preview inside settings, showing effective save values or menu-global
  values. No posting/archive/sound/target path. Pair validation, independent change detection, scoped
  reset and dormant override behavior correct source limitations without a separate settings system.
- Original Mlie.BetterMessagePlacement is sole provider if active. Unknown bar geometry loses only
  automatic clearance where the native position path remains supported; unsupported position loses
  the entire owned contribution. Preserve preferences and explain each fallback.
- Keep native posting and interactions, direct S001 effective-presentation composition (hidden/reduced
  entries, visible labels/indicators/outlines and adjustable weapons), no new UI tab/toggle, no
  private Thing/pawn cache and no runtime/XML dependencies beyond the future required Harmony use.

BMP-Q1–4 retain exact effective targets/geometry/Unity proof, these provisional layout/preview choices,
actual optional-provider/consumer bindings and implementation/language/cost/native acceptance. Patch
documents/executable admission, focused RED/GREEN and reviewed-build real player workflows remain
open. Independent native/source review identified ambiguous clearance wording for native messages
without a visible background. The spec now measures padded display bounds in both states and
explicitly preserves native background visibility. Composition review identified that exporting only
S001 mood-outline bounds misses its filtered/reduced entries and adjustable weapons. The draft now
consumes its full prepared visible presentation without duplicating decisions or changing update
timing, with an observable scenario and dedicated native acceptance task. Focused re-review confirmed
the correction with no remaining P1/P2 findings. Final typed strict validation passed **93 items,
zero failures**, exit0/no stderr. The draft has **7 requirements, 24 scenarios and 24 unchecked tasks**.

Semantic pass: readability/navigation changes only. Native message generation, importance, sound,
history, expiry and quest/game outcomes remain. No progression, resource, work-priority or gameplay
balance change is selected. Clipping/overlap on constrained screens remains an explicit limit, and
combined cost/native rendering/input have not been measured or accepted.

No runtime code/Defs/assets, tests, build/install, game launch, upstream execution, Workshop edits or
normal save/configuration mutation occurred.
