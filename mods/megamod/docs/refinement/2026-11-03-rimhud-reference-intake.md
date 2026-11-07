# RimHUD and Passion Icons intake — 2026-11-03

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Status: selected sources with installed identities and initial documentation checked; full behavior
research and individual OpenSpec changes remain queued. This is refinement evidence, not a feature spec.

**2026-11-06 follow-up:** S127's full [research](2026-11-06-rimhud-research.md) and
[owned draft](../../../../openspec/changes/specify-megamod-pawn-hud/) now exist. The initial findings
below remain dated intake evidence; S128 retains its separate sequential deep dive.

## User direction and ownership

The user explicitly selected **RimHUD** and **RimHUD Passion Icons**. Track them separately as
**S127** and **S128**, with at least one megamod-owned change per source. Their eventual observable
behavior composes directly in the owned HUD; do not implement Passion Icons as patches against
our own RimHUD replacement. Neither original mod becomes a required megamod runtime dependency.
Do not silently add other mods discovered through compatibility references.

## Installed identities

Paths below are relative to `F:/Steam/steamapps/workshop/content/294100/`. Presence on disk is not
activation, verified beta support, complete source analysis or permission to copy code/assets.

| Queue | Title / author | Package ID | Workshop | Installed version evidence |
| --- | --- | --- | --- | --- |
| S127 | RimHUD / Jaxe | `Jaxe.RimHUD` | [1508850027](https://steamcommunity.com/sharedfiles/filedetails/?id=1508850027) | About description and README say 1.17.5; advertised game support ends at 1.6 |
| S128 | RimHUD Passion Icons / DanZinagri | `DanZinagri.RimHUDPassionIcons` | [3829500081](https://steamcommunity.com/sharedfiles/filedetails/?id=3829500081) | No modVersion in About; advertises 1.6 and 1.7 |

| Evidence file | SHA256 |
| --- | --- |
| `1508850027/About/About.xml` | `9424327E6EC107E64708DE07B64BCD4EC2598237514D290EA84693F2DFE0BA9C` |
| `1508850027/Assemblies/RimHUD.dll` | `D24BB116949DA4F2EEFCF350CB73E20B014A9DB28D922DDAE256D11D59DB206F` |
| `3829500081/About/About.xml` | `37D47AE260436792E89EAFD862EF3268A2882984C423BC49F163A3B450539220` |
| `3829500081/1.7/Assemblies/RimHUDPassionIcons.dll` | `DE7A34B42D3731319F859A8D7424C8CFE1FA444FA785E62A013F54106E94B6FB` |
| `3829500081/1.7/Mods/VSE/Assemblies/RimHUDPassionIcons.VSE.dll` | `5DD93B7A7173A08C75EEA4763439C3F50D3940D88B0387439838FB2051AED118` |

RimHUD requires Harmony; its installed LoadFolders names versions through 1.6. Passion Icons
requires Harmony and RimHUD; its 1.7 load list includes `/`, `1.7`, and a conditional VSE assembly
for `vanillaexpanded.skills`. Its About also orders it before that provider. This establishes an
optional integration to inspect, not a request to include Vanilla Skills Expanded or proof of
working compatibility. The differing version declarations must be checked during source analysis;
they do not establish whether RimHUD works or fails on beta.

## Initial documentation and research leads

Read RimHUD's installed About, README and LoadFolders, Passion Icons' About and LoadFolders,
and the public [RimHUD repository description](https://github.com/Jaxe-Dev/RimHUD) and
[integration wiki](https://github.com/Jaxe-Dev/RimHUD/wiki). Steam retrieval returned 429 for
RimHUD and an inaccessible-page result for Passion Icons; no Workshop description beyond the
installed metadata was inferred. Retry public documentation during the owning deep dives.

RimHUD describes selected-pawn information in a resizable inspect pane or a floating dockable
window, with warnings for dangerous health conditions, wounds requiring tending and mental-break
risk. Its README lists affected native UI boundaries and notes some prefixes suppress original
drawing. These are research leads for semantic patch guards, not adopted patch targets or evidence
of a UI conflict. The README's English-only policy does not override our seven-language requirement.

The wiki documents presets supplied by other mods and five custom widget Def families: needs,
bars, values, selectors and fully custom drawing, with an API-version field and callbacks. Inspect
actual consumers, persistence and failure behavior before proposing a drop-in contract. Preserve
supported external behavior where practical without spoofing the original package or claiming
private/binary parity from documentation alone.

Passion Icons' installed description says it displays passion icons instead of stars. Its optional
VSE assembly and About comment about a Prepare Moderately fix require source/decompilation to
establish their exact behavior and scope. Do not infer the icon mappings, additional passion types,
settings or compatibility fix from those names alone. No source files were found in the bounded
installed C# inventory; no decompilation of these two mods has been performed in this intake.

## Remaining work

- **RH-Q1:** research RimHUD's complete observable selection, display, warnings, clicks/tooltips,
  layout/configuration, defaults, persistence and external contracts. Review overlapping selected
  mood/need/health displays and compose their results. Apply our UI placement and shared settings
  rules; HUD configuration belongs in settings and pawn-specific actions remain gameplay state.
- **RPI-Q1:** research exact passion icon behavior and optional provider handling, then specify it
  as direct composition with S127. Audit upstream fixes separately from the requested icon feature;
  record proposed adoption/deviation and any unresolved choice instead of silently broadening scope.
- For both: documentation first, then pinned public source or read-only decompilation; inspect
  licensing, beta target semantics and actual consumers. Record open questions for later. No
  asset selection/copying or gameplay acceptance is implied by this identity check.

The existing sequential hauling reference comparison remains next. These additions do not mark
any draft, implementation, external contract or compatibility test complete.

## S128 deep-dive follow-up — 2026-11-06

The initial Passion Icons research above is superseded by the [complete S128 source inventory](2026-11-06-rimhud-passion-icons-research.md)
and [owned draft](../../../../openspec/changes/specify-megamod-passion-display/). The author page,
pinned public source, selected1.7 core/optional assemblies and exact current VSE/Prepare Moderately
types were inspected. Native/provider icons compose directly with S127; the old startup repair is
deliberately omitted for current providers already targeting the modern filter methods. RPI-Q1 is
resolved as source research; RPI-Q2–Q4 retain native/provider proof and concrete compatibility gaps.
No runtime implementation or acceptance follows from this documentation checkpoint.
