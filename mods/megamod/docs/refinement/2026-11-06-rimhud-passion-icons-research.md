# S128 — RimHUD Passion Icons research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Origin replacement. Documentation and offline inspection only, 2026-11-06; no game execution,
Workshop edits, gameplay acceptance or asset selection. [Coverage](../replacements/rimhud-passion-icons.md)
and [change](../../../../openspec/changes/specify-megamod-passion-display/) distinguish selected
observable behavior from the source's implementation and omitted historical repair.

## Public documentation first

Read the author's [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3829500081),
retrieved directly after the web opener could not serve it. Retained 86,997-character HTML in local
evidence. It promises replacing RimHUD stars with actual native passion icons; VSE's Apathy,
Natural and Critical and Alpha Skills use their own icons. VSE is now optional. The author reports
an older VSE/Prepare Moderately missing `GetRandomOfEnum` startup target that prevents later
Stranger Danger/RimHUD integration; the source supplies a repair. Alpha Skills testing and expected
Oracle's Skill Icon Retextures compatibility are **author claims**, not our acceptance results.

The author links [public source](https://github.com/DanZinagri/RimHUD-Passion-Icons), pinned at
[`3d32cc578f956b2ea5a1affcbaf54f900fa25df7`](https://github.com/DanZinagri/RimHUD-Passion-Icons/tree/3d32cc578f956b2ea5a1affcbaf54f900fa25df7).
The inspected 19-entry tree has no README or license file; API license is unset. Retained 11 text
inputs including the four implementation files. Independently engineer; do not copy source/assets
or distribute upstream assemblies. Mirror search hits were not used as technical evidence.

## Exact installed inputs

Root `F:/Steam/steamapps/workshop/content/294100/3829500081`, title **RimHUD Passion Icons**, author
**DanZinagri**, package **DanZinagri.RimHUDPassionIcons**, advertised1.6/1.7. About declares Harmony
and `Jaxe.RimHUD` required; `loadBefore vanillaexpanded.skills` is explained by the repair. For1.7,
LoadFolders selects root+1.7 and `1.7/Mods/VSE` only with `vanillaexpanded.skills`. No installed source
or separate mod-version declaration was found. These are identities, not proof of beta operation.

| Input | Bytes | SHA256 |
| --- | ---: | --- |
| About/About.xml | 1021 | `37d47ae260436792e89eafd862ef3268a2882984c423bc49f163a3b450539220` |
| LoadFolders.xml | 280 | `6311cfe464668e109697fe1b917d7bb28bafa82c554114bbc626de08efdd3453` |
| 1.7/Assemblies/RimHUDPassionIcons.dll | 8192 | `de7a34b42d3731319f859a8d7424c8cfe1fa444fa785e62a013f54106e94b6fb` |
| 1.7/Mods/VSE/Assemblies/RimHUDPassionIcons.VSE.dll | 8704 | `5dd93b7a7173a08c75eea4763439c3f50d3940d88b0387439838fb2051aed118` |
| VSE3400246558 1.7/Assemblies/VSE.dll | 117248 | `17d120dc3454de9ce283b58a0654f32fe82c68fc1b46db452e86d30847ecbf34` |
| Prepare Moderately2057362949 1.7/Assemblies/PrepareModerately.dll | 143872 | `38d3ddc07b6c3024aef40839fb44d16cdf84a177e4a8bba23a00e5b58281c1ca` |

Prepare Moderately is `Lakuna.PrepareModerately`, declared6.10.1.0, authorLakuna, supported through1.7.
Its current source link is [Lakuna/RimWorld-Prepare-Moderately](https://github.com/Lakuna/RimWorld-Prepare-Moderately).
Both selected icon assemblies were decompiled completely; six relevant provider types were freshly
extracted from the hashed binaries. An initial extraction using a guessed VSE filename failed with
file-not-found; the corrected manifest path `VSE.dll` succeeded. No assembly was executed.

## Source behavior inventory

| Source path / body | Inspected behavior | Selected disposition |
| --- | --- | --- |
| Core `RimHUDPassionIconsMod` | Queues patch setup after load and checks display name RimHUD; hard RimHUD references | Direct owned S127 composition; no display-name gate or original dependency |
| `PassionIconPatches.Patch` | Postfixes ValueModel.Build and ValueWidget.Draw; accesses private skill, label and text style fields | No patches to owned HUD; structured row data |
| Build / `VanillaIcon` | Only SkillValue→ValueWidget; skips null skill and None; minor/major use current native texture; missing icon leaves source label unchanged | Same identity/None semantics; stronger known-name or unknown fallback |
| `StripIndicator` | Removes provider indicator suffix, otherwise repeated localized star suffix; supports immediate trailing `</color>`; refuses other label forms | Never parse owned translated labels; preserve name/emphasis and avoid enum-sized strings |
| Draw postfix | Weak-table texture per widget; icon square min(rowheight,lineheight), measured label endpoint clamped to label edge; white ScaleToFit then restore color | Reserve label space; preserve aspect/tooltip/actions; guaranteed GUI cleanup |
| `VSEIntegrationMod` / `PassionDefIcon` | Optional assembly sets global resolver delegate; obtains PassionToDef, requires nonempty iconPath, returns Indicator and Icon | Narrow admitted registry adapter; no list of hardcoded added enum values |
| `PrepareModeratelyFix.Install` | Prefixes VSE Do, skips original, supplies its own Apply; missing method catches are dev-log only | Omit for inspected current provider; no foreign init replay |
| `Apply` | Patches MakeMenu&lt;Passion&gt;, two filter UI/Summary methods using VSE transpiler, two Randomize methods using own AllPassions postfix | Existing VSE already supplies current behavior; retain it |
| `PrepareModeratelyFix.Fallback` | If unhandled and VSE PM flag, Apply again, fills SD_passion and calls RimHUDPatches.Do, exceptions dev-logged | No reinitialization of foreign integrations to render owned icons |
| `TryPatch` | Skips null target and duplicate exact patch method among prefix/postfix/transpilers; catches install failures | Insufficient semantic/equivalent-patch/Prepatcher guard; not our admission policy |

No standalone settings window, bottom tab, operational toggle, save keys, XML Def changes, skill
stat changes or source textures were found in this selected code. The native/provider textures
are looked up from their suppliers. The public core and selected compiled core agree on the
inspected mapping, suffix and draw paths. Source control-flow findings are not reproduced live bugs.

## Current provider comparison and historical repair

Fresh `VSE.PrepareModeratelyPatches` targets `HasPassion` and `HasPassionsAtLevel` methods
`DoEditInterface`, `Summary`, `Randomize`. Its own `PickRandomPassionPostfix` chooses from
`PassionManager.AllPassions`. The current Do path does **not** call the obsolete `GetRandomOfEnum`.
`PatchPrepareModerately` is a Prepatcher hook on the generic FloatMenuUtility.MakeMenu, using
registered passion labels/choices. This is not discoverable merely as an equivalent Harmony patch
method. `ModCompat.Init` still initializes PM before Stranger Danger and queued RimHUD integration,
so the author's old cascading-failure explanation is plausible for the older described code.

Fresh Prepare Moderately filter bodies confirm both current Randomize methods, saved passion keys,
UI selectors, summary and Matches. HasPassion also chooses a skill and accounts for gene modifiers;
HasPassionsAtLevel counts matching skills within a range. Those behaviors are external and unchanged
by this display. We do not select arbitrary extra PM changes or alter its gene/range semantics.

**Inference:** copying the icon mod's repair onto this already-modern VSE could replace an
equivalent current implementation and, in fallback/repeated-owner cases, add a second Randomize
postfix or repeat later integration. Exact-method deduplication cannot prove semantic equivalence.
Therefore **deliberately omit the old repair for these current inputs** and retain the provider's
existing authority. This is not a claim that every VSE/PM version works. Older broken variants need
separate demonstrated scope and target research before any owned repair is promised.

Fresh `PassionManager` assigns None/Minor/Major indexes0/1/2 and registers other Defs after them;
`PassionToDef` directly indexes an array without safe range handling. `PassionDef.Icon` caches a
ContentFinder lookup; `Indicator` is an optional space-prefixed string; `FullDescription` describes
learning/forgetting and other-skill effects. The owned adapter must validate readiness/index/shape
and preserve identity on art failure. It must not change passions, multiply stats or read an enum
value as a strength/rating.

## Consumer, UI and composition audit

The public delegate `PassionIconResolver(SkillRecord,out string)` and mutable Resolver field are
consumed by the mod's own VSE assembly. A bounded `rg -a -l -g '*.dll' RimHUDPassionIcons` search of
installed Workshop content returned only its own1.6/1.7 core/optional assemblies. No external
consumer was demonstrated; this is not an assertion that none can exist. Preserve observable
provider data through direct owned calls; do not invent a binary facade or spoof source package IDs.

Bounded installed About searches did not locate Alpha Skills, Stranger Danger or Oracle's icon
retexture provider; School & Learning was a textual false positive. These advertised cases remain
fixture/identity research, not accepted compatibility. No assets were viewed, copied or selected.

The sole added surface is a skill-row icon inside S127 inspection, with its existing skill-detail
tooltip/action. No extra bottom tab/settings page/toggle. Layout preferences use S127's common
global/save/Use global model; actual passions are pawn gameplay state. All owned fallback text uses
the seven product languages. Original RimHUD co-installation preserves its display and suppresses
the owned overlap with warning, including our icons. No interaction with S137 medical filtering is
introduced by this feature. No map-wide cache or standalone passion save data is warranted.

## Open research, evidence and status

- **RPI-Q1:** source inventory complete; current repair divergence explicitly recorded.
- **RPI-Q2:** exact beta/provider/long-label/scale/icon fallback native proof; Alpha/retexture fixture
  identities and actual contracts before a specific compatibility claim.
- **RPI-Q3:** any concrete older broken VSE/PM combination needing an owned repair; not selected by
  default merely because the source once carried one.
- **RPI-Q4:** any demonstrated external Resolver/private RimHUD consumers requiring a narrow facade;
  no such third-party binding found in the bounded installed search.

Local ignored evidence `artifacts/Refinement/megamod-spec-20261106/rimhud-passion-icons/` contains
the Workshop HTML, installed/public/provider manifests, pinned text inputs, compiled extractions,
review disposition, document hashes and strict validation result. No tokens or game-session state.
The draft's seven requirements and fourteen scenarios are observable contracts only. Gameplay
implementation, focused TDD, reviewed build, visual/provider workflows and acceptance remain open.
