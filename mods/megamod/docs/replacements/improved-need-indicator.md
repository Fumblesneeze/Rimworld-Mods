# Improved Need Indicator — research and replacement coverage

Owner: Make Rimworld Great Again (`fumblesneeze.megamod`). Intake: **P1-03**.
Researched **2026-11-02**. Status: **analyzed; specification drafted with 1.7 research gaps**.
Implemented: **nothing**. No live source/replacement verification or 1.7 compatibility claim.

Change: [specify-megamod-need-forecasts](../../../../openspec/changes/specify-megamod-need-forecasts/).

## Identity and documentation-first evidence

- Package `AmCh.ImprovedNeedIndicator`, Workshop `3211635205`; authors AmCh and Missionz3r0.
- [Steam description](https://steamcommunity.com/sharedfiles/filedetails/?id=3211635205) attempted
  first; retrieval returned HTTP 429. Read the installed author description and README instead.
  The description lists Food, Indoors, Outdoors, Recreation and Rest timer additions; README is
  primarily build documentation, not an exhaustive behavior contract.
- [Author's repository](https://github.com/AmCh-Q/RimWorldMod_ImprovedNeedIndicator) was inspected.
  The installed Workshop package includes source, keybinding Defs and translations, so its exact
  shipped source was read locally, then compared with the decompiled installed 1.6 DLL for active
  registration and rate lifecycle gaps.
- Installed root: `F:/Steam/steamapps/workshop/content/294100/3211635205`.
  Metadata version `1.6.4503.20250630Z`, supported versions **1.2–1.6**, no 1.7 declaration/folder.
  Reference assembly `1.6/Assemblies/Improved Need Indicator.dll`, assembly version `1.0.0.0`,
  SHA256 `5E784568D3F4994F0A3F2822574A0D4B474C18C7FFB6298D56EC0A3A8B4FF23B`.
- Installed LICENSE is GPL version 3. Independent observable-behavior implementation is planned;
  no source/binary/asset is copied into the product. Local decompilation evidence is under
  `artifacts/Refinement/megamod-spec-20261102/improved-need-indicator/`.

## Active behavior and evidence map

| Behavior | Shipped source / confirmed registration | Intended observable contract |
| --- | --- | --- |
| Append to existing need tooltips | `Mod.cs`: base Need plus separate Food postfix | Preserve descriptions and other compatible text |
| Five selected need types only | `Need_Postfix` / `Need_Food_Postfix` | Food, Rest, Recreation, Indoors, Outdoors |
| Food future categories | `AddendumManager_Need_Rate_Food` | Remaining hunger boundaries using actual pawn rates/capacity |
| Rest recovery plus future depletion | `AddendumManager_Need_Rate_Rest` | Current/last-observed sleeping conditions explicitly distinguished |
| Recreation depletion and stage-dependent rates | `AddendumManager_Need_Rate_Joy` | Conditional decline projection, including caravan preparation |
| Roof/enclosure transitions | `AddendumManager_Need_RoofEnclosure*` | Correct live environment/direction and reachable category order |
| Basic versus detail tooltip | `AddendumManager_Need*`; keybinding Def | Hold to compare current and full-level timing, plus rest recovery |
| Keybinding default | `Defs/KeyBindingDefs/KeyBindings.xml` | Left/Right Control; global native rebinding |
| Mood/Comfort/Beauty classes and strings | Files exist but are not selected by `Mod.cs` or installed DLL dispatch | Deliberately not counted as delivered functionality or included scope |

## Calculation evidence and limitations

Food obtains category-specific depletion rates and iterates thresholds. Rest reconstructs stage
rates with 0.7/0.3/0.559 factors, uses the pawn's rest-rate stats and the last observed rest
effectiveness for recovery. Recreation reads a private fall value, applies category factors,
current stats and caravan preparation. These are **source-version facts**, not established 1.7 math.

Detailed rate tooltips compare current time with time from maximum. Rest adds the amount of
recovery needed from each displayed boundary. Indoors/Outdoors detail equals the basic view and
does not advertise extra detail. Estimates assume current conditions continue; no schedule or
future meal simulation exists. Need values are observed, not modified by the feature.

The exact 1.7 audit must check rate factors already included by game APIs, threshold units for
nonstandard capacity, discrete update timing, frozen states and overflow. Merely resolving a
private field or matching a method name cannot establish mathematical compatibility.

## Explicit corrections and deviations

- **Roof need rate freshness:** source/decompiled base roof manager defines `UpdateRates` but its
  tooltip path does not call it. Outdoors starts with zero rate; Indoors snapshots its initial rate.
  Our intended contract updates actual rates after environmental changes. This is a static finding,
  not a claim that the bug was reproduced live.
- **Recovery ordering:** source walks the same descending category array when recovering. The
  replacement lists reachable transitions in temporal order and avoids misleading simultaneous times.
- **Units:** source uses `ticks / 60` labeled seconds below 600 ticks, then `ticks / 2500` labeled
  hours. The replacement explicitly uses consistent in-game minutes/hours/days and states assumptions.
- **Awake rest estimate:** source uses last rest effectiveness without explaining that this need not
  describe the next bed. Our tooltip names that assumption or reports no reliable recovery estimate.
- **No stale/invalid timer:** a frozen/zero/unknown rate cannot preserve an old countdown or emit an
  invalid number. Above-normal amounts must not silently be treated as ordinary maximum.
- **Binding hint:** source English text always says Ctrl. Our hint follows the actual current binding.
- **Settings scope:** the held key remains native global input preference, a documented exception
  to save inheritance. No source mod-settings menu was found; no extra menu is invented for parity.

## UI, contracts and composition

Entry point is the existing Needs tooltip with a held details key; no added bottom tab. Keybinding
Def names are `ShowDetails` and category `ImprovedNeedIndicatorKeys`. They are public Def identities;
actual consumers have not yet been established. Public source manager classes similarly do not
prove a stable supported API. IN-Q3 retains the consumer audit before facades/migration claims.

Original requires Harmony and has no separate gameplay save component/settings store in the
inspected code. New transient forecasts must rebuild after pawn/save switching. With the original
provider active, proposed initial policy suppresses our feature with a warning rather than duplicate
tooltips. Other text additions remain intact where compatible.

S103 [Need Bar Overflow](need-bar-overflow.md) establishes actual reserves against unchanged ordinary
full and effective piecewise hunger/rest/recreation rates. Compose these directly once in owned
forecasts; unsupported rate models remain unavailable. The source author's recommendation alone is
not proof of external compatibility; no internal stack of patches or duplicated need index.

## Remaining work

IN-Q1: exact 1.7 mathematical/need-state contracts. IN-Q2: owned overflow composition resolved by
S103; exact effective/external shapes remain NBO-Q1–2 research gates.
IN-Q3: actual consumers/keybinding identity. IN-Q4: localized tooltip presentation/assumptions.
All gameplay code, semantic patch targets, tests, review, native tooltip/action acceptance and
optional-provider evidence remain open. The five-need specification is a draft, not a port claim.
