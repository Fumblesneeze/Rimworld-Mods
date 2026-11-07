# Dress for the Weather — research and coverage

Owner: Make Rimworld Great Again (`fumblesneeze.megamod`). Intake **P1-05, P1-39**. Date **2026-11-02**.
Status: **analyzed; specified; WF-Q1 policy resolved, WF-Q2–3 research/acceptance open**. Nothing implemented or accepted in game.
Change: [weather-ready arrivals](../../../../openspec/changes/specify-megamod-weather-ready-arrivals/).

## Identity and evidence

- Selected continuation: `Mlie.DressForTheWeather`, Workshop `3607724833`, author credit Daniel,
  maintained by Mlie. Original Workshop item `3077924542` is provenance, not a second selected provider.
- Read [Steam description](https://steamcommunity.com/sharedfiles/filedetails/?id=3607724833) and
  installed README first, then [author repository](https://github.com/emipa606/DressForTheWeather).
- Repository commit `d33656d4b8d341572423b8c958a38978de634a9a`; selected installed 1.7 DLL matches
  the repository's packaged 1.7 DLL SHA256 `BAC9800E1EFC3844F4668966939FAA97CEDD5A9BD67971FCEF2172C2E6B43243`.
  Assembly version `1.7.0.0`, informational build suffix `23ad8a66f9a1e40628d5c2e210ebf65baaa47450`.
- Root: `F:/Steam/steamapps/workshop/content/294100/3607724833`; selected file
  `1.7/Assemblies/DressForTheWeather.dll`. Also ships VersionFromManifest helper, not needed by our feature.
- Installed LICENSE.md: MIT, copyright Mlie 2020; assembly credits Daniel 2026. Retain attribution
  if any permitted material is reused; current plan independently implements the behavior.
- Read all five source classes, then inspected installed decompilation to confirm constants, filters,
  trigger hooks and loop behavior. Evidence: `artifacts/Refinement/megamod-spec-20261102/dress-for-the-weather/`.
  These are static sources, not live acceptance or source-to-binary reproducibility proof.

## Coverage and observable contracts

| Source surface | Evidence | Spec treatment |
| --- | --- | --- |
| Neutral groups receive gear | `IncidentWorker_NeutralGroup_SpawnPawns` | Ordinary temperature scope; vacuum scope separately covers all humanlike arrivals |
| Raiders optionally prepared | `IncidentWorker_Raid_TryGenerateRaidInfo`; default false | Prospective ordinary temperature/pollution setting, off initially; vacuum independent |
| Hot/cold apparel attempts | `PawnsArriveUtilities.DressPawnAppropriately` | Improve current comfort, preserve stronger/protected gear, bounded fallback |
| Faction tech/Stuff/specific material requirements | `canUseStuff`, `apparelRequirementCanUseStuff`, `getNewApparel` | Legal wearer/faction/material selection |
| Candidate and replacement filters | `DressForTheWeatherSettings` | Two separately explained inherited filters |
| >50% pollution, toxic fallout, PoisonForestSpores | `DressPawnAppropriately` | Conditional masks/veils; exact equality excluded |
| Spacer+ vacuum suits/helmets, child suit variant | Same method | All humanlike vacuum arrivals regardless of tech/role; guarded Odyssey/SOS2; age/body-appropriate protection and no free gear for existing player pawns |
| Missing apparel cleanup in serialized filters | `CleanFilterXmlOfUnknownDefs` | Load valid remaining choices without broadening selection |
| Settings filter UI/reset | `DressForTheWeatherMod` | Shared searchable mod settings, no new tab |

Source thermal pool excludes belts, mechanitor apparel and things not generatable for warmth.
Initial candidates exclude armor, armor headgear and utility. Replacement defaults exclude most
armor/headgear but allow simple/advanced helmets and flak vest/pants/jacket; masks are protected.
The source's utility exclusion is accidentally applied to the candidate filter again. Our proposed
replacement default explicitly protects utility too; this deviation is recorded, not hidden.

## Deviations, interactions and limits

- Source min/max comfortable temperatures are read once before loops, so becoming comfortable does
  not stop subsequent attempts. Our contract recomputes actual comfort and stops unnecessary changes.
- Source outer budget is 100 tries, but inner candidate sampling repeats until wearable with no
  bound. Our search must finish even for zero eligible apparel. No performance improvement is claimed yet.
- Special masks/vacsuits bypass the candidate filter; veils are selected from the candidate-filtered
  `allApparelPairs` pool (`InitializeApparelPairs` and the lower-tech toxic-protection branch).
  The spec preserves the veil restriction. All three bypass the replacement filter in the source
  and can displace otherwise protected gear. WF-Q1 now selects survival-gear precedence over protected
  armor, preferring all-hazard protection and vacuum first when incompatible. Ordinary temperature adjustments
  preserve necessary hazard protection and do not equip duplicate or impossible combinations.
- Source gas-mask check uses installed Biotech and assumes some Def lookups succeed; vacuum uses
  installed Odyssey and requires Spacer+. Ours requires active content and guards actual Defs/shapes.
- Source supplies lower-tech war veils as well as the documented industrial gas masks. Include the
  observed behavior while explicitly avoiding a claim that a veil guarantees toxin safety.
- Child vacsuit protection differs between source cold/heat paths. Our intended protection applies
  consistently. Already suitable gear is not repeatedly replaced.
- Settings move from global-only to shared global/save inheritance; global filter editing works
  from the main menu, where source filters are hidden. No automatic original settings migration.
- Generated apparel has ordinary gameplay/loot consequences and remains after disabling the feature.
  Exact disposal of displaced generated gear needs native verification; no duplication is intended.
- Initial original-provider co-install policy suppresses our feature with a warning. Other sources
  modifying generated apparel need researched composition. Future staged generation calls this
  owned preparation before publishing its group, not a patch against another megamod module.

## Interfaces and dependencies

Source Harmony owner is `DanielWedemeyer.DressForTheWeather`; public utility/settings types are
under `DressForTheWeather`. Publicness alone is not a consumed API. Incoming consumers have not
been proven; no assembly/type/settings-key replacement claim is made. Outgoing contracts include
native incident pawn lists, apparel wearability/Stuff/gear generation and optional hazard Defs.
CE support is an author statement requiring separate exact-provider inspection and live verification.

Harmony is required for the proposed hooks; Biotech, Odyssey and external spore-weather content are
optional feature adapters. No dependency on the original or its VersionFromManifest helper is needed.
No bottom tab was found; settings are configuration, generated pawn gear is ordinary game state.

## Questions and remaining work

WF-Q1 policy resolved: survival precedence, vacuum-first unresolved combinations. WF-Q2: full native incident coverage,
factionless and external wearer rules. WF-Q3: exact spore provider and actual incoming consumers/CE.
Implementation, semantic target checks, TDD, translations, independent review, balance measurements
and actual native arrival/save workflows remain unchecked.
