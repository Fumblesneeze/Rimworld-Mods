# S042 — Auto Self-Tend research

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Inspected **2026-11-03**; origin replacement, handwritten **P1-33**. Static source/native evidence,
not a live run. No implementation, installed-mod edit or acceptance claim.

## Documentation and identity

Read public documentation first: the initial bare
[Steam URL](https://steamcommunity.com/sharedfiles/filedetails/?id=3300580124) returned HTTP 429.
Search then the [English Steam page](https://steamcommunity.com/sharedfiles/filedetails/?id=3300580124&l=english)
provided the author's description: automatic self-tend permission using a configurable Medical
threshold, editable during play. The retrieved page is a cached older view; its update date and
comments do not identify the installed binary. The linked
[GitHub repository](https://github.com/Funstab/AutoSelfTend) returned 404. Installed metadata and the
complete decompiled binary therefore provide the detailed contract, not assumed current GitHub code.

Installed root: `F:/Steam/steamapps/workshop/content/294100/3300580124`.

| Field | Evidence |
| --- | --- |
| Title / package | Auto Self-Tend / `funstab.AutoSelfTend` |
| Author | About: Funstab; public Workshop display: Mr. Recoup |
| Declared game versions | About: 1.5, 1.6; no installed 1.7 declaration |
| Files | About XML, PublishedFileId, preview and one unversioned Assemblies DLL |
| Assembly/file version | `AutoSelfTend`, 1.0.0.0; copyright 2024 |
| DLL SHA-256 | `999B26031A547C64DC1ABD93B4D1DF39300F1AC0CCCF71CD92800D39A5D60D03` |
| About SHA-256 | `973345FE2A3291AB1D1B8013D71B5479337550CB6D90ABCAF9AB57D7E965B884` |
| Dependencies / patches | None declared; complete DLL contains no Harmony/XML patch |
| License | No installed license/source license established; no upstream code, DLL or art reused |

ILSpy 11.2.0.10050 output is local ignored evidence at
`artifacts/Refinement/megamod-spec-20261103/auto-self-tend/{compiled,native}`. All three source
types were inspected: `AutoSelfTendMod`, `Settings`, `AutoSelfTendGameComp`, plus AssemblyInfo.

## Complete installed behavior

- `Settings.MinimumSkillLevel`: default **6**, integer slider **0–20**. `DisableBelowThreshold`:
  default **true**, checkbox; false means only switch on, never automatically off. Both are global
  ModSettings using same-named save keys. UI text is hardcoded English, with no language assets.
- `AutoSelfTendMod.WriteSettings` saves configuration then invokes `ApplyAllNow`. Settings fields
  change during UI drawing; a periodic assessment can see them before window close. The replacement
  instead defines explicit committed-value timing, so mere rendering is not a policy event.
- Game component applies at `FinalizeInit`, every **600 game ticks**, and `ApplyAllNow` with an
  active game. It walks every map's `FreeColonists`, then colonists in player caravans. It does not
  scan all world pawns or transporters in flight. Static list capacity 64 is initial allocation,
  not a colonist limit. No custom Thing index exists.
- A pawn needs non-null settings and skills. Source compares native Medical `Level >= threshold`;
  a missing Medical record is treated as zero. It does not check ability to doctor or baby status.
- `_applied[thingIDNumber]` records the last threshold classification, **not** the last checkbox
  value and not a permanent manual-override flag. No write if known classification and both options
  are unchanged. Otherwise it writes the classification, except a false classification is never
  written in enable-only mode. Existing equal checkbox values need no write.
- Therefore a manual choice survives ordinary polls and same-side skill changes. A genuine crossing
  or either effective option changing can override it, even if a threshold edit leaves that pawn on
  the same side. Opening/saving unchanged options does not reset it. Source explanatory text omits
  the policy-change exception and can be read as stronger than the implementation.
- The component saves `appliedSelfTend`, `lastThreshold`, `lastDisableBelowThreshold`; ordinary native
  pawn state saves the checkbox. Unchanged loaded state preserves manual choices. Missing component
  state treats eligible pawns as new and can replace existing native choices. Source does not prune
  assessment IDs. A policy changed while a known pawn is outside enumerated populations can be
  missed on return because source stores only one last-policy pair for the entire game.
- No new job, medical quality, medicine, work priority, notification, bottom tab, gizmo or debug
  action. It only changes permission. This is not an instant healing feature.

## Exact native 1.7 inspection

Inspected `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll`, **1.7.4995 rev309**, SHA-256
`56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21`.

| Native path | Finding and implication |
| --- | --- |
| `Pawn_PlayerSettings` | Public boolean `selfTend`, saved as `selfTend`, default false; preserve this ordinary contract |
| `HealthCardUtility` | Checkbox shown for living colonists other than babies, with player settings; enabling is rejected for disabled Doctor work and cautions for Doctor priority zero |
| `SkillRecord.GetLevel` | Includes aptitude, clamps 0–20, returns zero if totally disabled; do not substitute raw learned level or tend-quality stat |
| `MapPawns.FreeColonists` | Uses `FreeHumanlikesOfFaction`, includes contained/unspawned map pawns and slaves, not merely spawned free adults |
| `Pawn.IsColonist` | Player humanlike, not subhuman; slaves qualify when secure. Apply native visible-permission eligibility consistently across map/caravan, rather than source's map/caravan discrepancy |
| `CaravanTendUtility.IsValidDoctorFor` | Self-treatment requires colonist permission plus ordinary doctor/patient restrictions; permission does not force treatment |
| `HealthAIUtility`, `WorkGiver_Tend` | Existing patient suitability, medicine selection, reservation and work paths remain native |
| `JobGiver_SelfTend` and Core Humanlike think tree | This job giver appears in an NPC self-tend branch; do not mistake it for the sole player permission consumer |

Proposed correction: assess only living native colonists with the ordinary self-tend control,
usable Medical information and ability to do Doctor work. Doctor priority zero is not inability;
permission may be on, without changing priorities or guaranteeing work. Babies/toddlers still using
the Baby stage, animals, mechs, subhumans, prisoners and non-player guests are not granted hidden
permission. Temporary injury, bed rest or lack of medicine does not become a new automation filter.
Retain a pawn's classification/policy history while absent; compare the policy last assessed for
that pawn on return. These corrections are proposed observable rules, not source parity claims.

## Consumers, interactions and boundaries

Ignore-disabled bounded text scan of installed Workshop XML/C#/JSON and repository mods/shared/tests
for package/type/save identifiers found source About and intake metadata only. Ignore-disabled binary
scan of installed DLLs for `AutoSelfTend`/`appliedSelfTend` found only the original assembly. No external
API consumer established; dynamic, uninstalled or renamed consumers remain unknown. Public types,
static `ApplyAllNow`, `Settings` and saved component names are not automatically promised facades.

Original active: suppress owned policy automation with one bounded startup explanation, without
editing/unpatching the original. Existing native checkbox remains useful. No package spoof, upstream
config/component import or safe-removal guarantee. First owned assessment is explicit; do not label
it a migration that preserves every prior manual choice.

S031 Toddlers supplies developmental restrictions; S007 compact health presentation must preserve
the native permission control. Future S124 Drug Response and S134 Priority Treatment consume the
same native medical eligibility/permission and compose directly with owned policies. No demonstrated
conflict or precedence is invented before their research. No runtime dependency on another repo mod.

## UI, balance and remaining work

Source policy window becomes **Mod settings → Health → Automatic self-tending** with common global
default, Use global and Current save rows. Native pawn self-tend remains entity gameplay state.
No new bottom tab or operational toggle. Threshold 6 and disable-below true retain source balance;
0–20 follows native Medical range. The 600-tick bound retains source responsiveness without real-time
timers. No changed medical speed, costs, quality penalty, medicine eligibility or automatic healing.
Global/save controls and explanatory text require all seven baseline languages and native UI proof.

| ID | Open work retained for implementation |
| --- | --- |
| ST-Q1 | Prove exact 1.7 eligibility, native permission consumer paths, aptitude/capability transitions and map/caravan/holder population |
| ST-Q2 | Implement stable per-pawn policy history, save-switch/absence recovery and shared settings without source migration or lost manual choices |
| ST-Q3 | Validate no-patch admission, original-provider suppression and any future medical-provider composition; no compatibility claim from identity alone |
| ST-Q4 | Focused RED/GREEN, seven-language UI, measured assessment cost and final real Health/work/treatment/save/caravan evidence |

No unanswered custom preference blocks this draft. All runtime work remains unchecked in the owning
[change](../../../../openspec/changes/specify-megamod-auto-self-tend/proposal.md).
