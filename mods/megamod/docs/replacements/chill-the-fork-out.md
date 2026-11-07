# Chill the Fork Out — research and replacement coverage

Owner: Make Rimworld Great Again (`fumblesneeze.megamod`). Intake: **P1-02**.
Researched **2026-11-02**. Status: **analyzed; specification drafted with gaps**.
Implemented/accepted: **nothing**. Original's current operation was not tested in a live game.

Change: [specify-megamod-directed-recreation](../../../../openspec/changes/specify-megamod-directed-recreation/).

## Provenance and research sequence

- Installed title/package: **Chill the Fork Out**, `KT.ChillOut`, Workshop `3523235444`.
  Authors credit Captain Muscles (original), Khobs, Kat and Mr. Wolf (translation).
- [Steam page](https://steamcommunity.com/sharedfiles/filedetails/?id=3523235444) attempted first;
  HTTP 429 prevented retrieval in this pass. No review/comment contents were inferred.
- Read installed `About/About.xml`, `README.md`, `Changelog.txt`, manifest and English strings.
  Description promises ordered building recreation; changelog adds swimming, ordered reading and
  work-hour continuation, and says pawns can leave after sufficient recreation or boredom.
- Read the author's [linked repository](https://github.com/Maxhd1234/CM_Chill_Out), commit
  `fd4b40c9ca27247fbad970f2b51ed427b3670e5a`, then its two `Source/Scripts/*.cs` files.
  Independently decompiled the installed 1.7 DLL to verify relevant control flow and defaults.
- Installed `F:/Steam/steamapps/workshop/content/294100/3523235444/1.7/Assemblies/KT_Chill_Out.dll`
  SHA256: `033B9879FFCA0E9FA1400D0833AE9332D425A098ADDD9AA99055A5FA231A4485`.
  Repository's packaged 1.7 DLL has the same hash. Assembly version is `1.0.0.0`; manifest says
  `1.0.54`, while latest installed changelog says `1.0.64` (2026-10-21). Retain all identities;
  do not flatten this into one supposedly exact release version.
- No LICENSE/COPYING file was found in the inspected repository. Independent implementation is
  planned; downloaded source and bundled reference assemblies are research inputs only.
- Local evidence: `artifacts/Refinement/megamod-spec-20261102/chill-the-fork-out/` contains the
  read-only checkout and installed-binary decompilation. None is included in the product.
- To close the schedule/termination gap, also inspected installed game `RimWorld.JoyUtility`:
  version `1.7.4995 rev309`, root `Assembly-CSharp.dll` SHA256
  `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21`.
  `JoyTickCheckEnd` applies normal joy/skill gain, checks timetable allowance unless the job ignores
  it, and handles fullness above 99.99% according to the activity's completion mode. It does not
  itself establish every activity's boredom/duration rules; CH-Q3 retains those caller-specific gaps.
- Traced the same game's `Mod.GetSettings` and `LoadedModManager.ReadModSettings` to close CH-Q1:
  no loaded object returns a new settings instance (60%); an existing settings record lacking
  `fckICE` takes its serialized fallback (50%). Both paths explain the apparent default conflict.

## Behavior inventory

| Behavior | Evidence | Specification treatment |
| --- | --- | --- |
| Single undrafted pawn with joy, building context menu | `FloatMenuOptionProvider_ChillOut` | Order recreation at a supported building |
| Discover building joy providers, obtain a valid native job | `GetSingleOptionFor`, `TryGivePlayJob` | Preserve activity eligibility; actual provider inventory remains CH-Q2 |
| Above-threshold refusal; equality allowed | `CurLevel > fckICE` | Admission threshold limits building orders, not duration |
| No-path and reservation handling | `CanReach`, prioritized-task decoration | Eligibility/failure explanations and revalidation |
| Clear old queue, issue player-forced job | Accepted menu action | Immediate order replaces queued tasks |
| Work/Anything treated as recreation during marked job, ordered reading/swimming | `Patch_CurrentAssignment.Postfix` | Temporary accommodation without saved schedule edits |
| Ordinary jobs retain own completion and interruption | No custom job driver; author changelog | Ordinary lifecycle; exact activity edges remain CH-Q3 |
| Odyssey-only swimming on water with valid route | `FloatMenuOptionProvider_Swim` | Optional contextual swimming |
| Outdoor suitability; cold explanation at at most 10°C | `NextDestIsOutdoorsAndNotEnjoyable` | Conjunctive rejection, not a universal 10°C cutoff |
| Global slider 0–100% | `ChillOutMod.DoSettingsWindowContents` | Common inherited setting, proposed default 60% |

## UI, defaults and persistence

Actual source entry points are native context menus and a mod-settings slider. No added bottom tab.
The orders are pawn/target gameplay state, not settings; only the admission threshold belongs in
the shared settings menu. The original global field initializes to 60% but missing-key loading
uses 50%, under save key `fckICE`. **60% is an explicit proposed megamod default**, not an assertion
that every original installation starts there. The proposed setting is inherited per save.

No separate source save component was found. The original marks the normal current job using its
count field, which is not a stable public compatibility contract. Native job persistence and cleanup
need CH-Q3 verification; no original-settings import or promised save migration is included.

## Contracts, dependencies and deviations

- Harmony is required by the original. Odyssey (`ludeon.rimworld.odyssey`) is optional for swimming;
  non-swimming functionality remains available without it. Other building activities are content
  providers, not unconditional dependencies.
- Source consumes `JoyGiver_InteractBuilding`, reflected `TryGivePlayJob`, ordinary job ordering,
  timetable state and Odyssey swimming paths. No proven incoming consumer of the source's public
  classes/settings has been established. Do not claim drop-in assembly compatibility.
- Replace the source's unguarded PatchAll and numeric job-count sentinel with documented, guarded
  owned integration; hashes above identify evidence, not compatibility gates.
- Preserve meaningful normal constraints and recheck when accepting an order. Do not retain stale
  menu-created jobs, null-ordering errors or failures that disable unrelated activities.
- Common settings replace the source slider/serializer. No imported source settings, global
  schedule mutation, extra bottom tab or limitless recreation is intended.
- With the original active, initial proposed policy defers this feature to it with a warning;
  never show duplicate actions or remove foreign patches.
- The timetable getter is also relevant to the earlier mood-bar schedule outline. This is a real
  source interaction to investigate (CH-Q4), not a reason to invent a priority conflict.

## Open work

CH-Q1 is resolved by the source trace above. Remaining: CH-Q2 actual providers and eligibility/contracts;
CH-Q3 detailed activity completion and save/load; CH-Q4 timetable presentation composition.
All are retained in the design, refinement ledger and unchecked tasks. Runtime implementation,
target checks, translations, test-first work, independent review, optional-content verification and
final native player workflows remain outstanding.
