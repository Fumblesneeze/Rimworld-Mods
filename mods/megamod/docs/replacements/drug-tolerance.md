# Only If 'Tolerance' Below in Drug Policy — S138

## Identity and provenance

- Owner: Make Rimworld Great Again (`fumblesneeze.megamod`), target 1.7 beta.
- Origin: replacement, independently implemented with accepted owned Any/zero semantics.
- Source: SeoHyeon, [Workshop 3088210509](https://steamcommunity.com/sharedfiles/filedetails/?id=3088210509),
  package `seohyeon.onlyiftolerancebelow` (About uses mixed case).
- Inspected 2026-11-08: installed `F:/Steam/steamapps/workshop/content/294100/3088210509`,
  selected `1.7/Assemblies/DrugPolicyEnhanced.dll`, SHA256
  `6C48ACE04DECFEDD65549489046CA5E23B7B40B2DD033331C9BB6E68AA7F6F29`.
- MIT source, copyright 2023 SeoHyeon; no source, binary, art or translation copied.
  See [research](../refinement/2026-11-08-drug-tolerance-research.md) for source hashes and native seams.
- Status: native runtime, persistence, common setting and seven catalogs are implemented.
  Reviewed exact-build observations cover the native slider, map/caravan routes, boundaries,
  save contexts and owned language rendering. Full S138 acceptance remains open.

No source API compatibility, original saved-state import, identity spoofing or drop-in claim is made.
All scenarios belong to the [owned capability](../../../../openspec/changes/specify-megamod-drug-tolerance/specs/megamod-drug-tolerance/spec.md).
The [tasks](../../../../openspec/changes/specify-megamod-drug-tolerance/tasks.md) retain full obligations
and exact-build evidence references; a scoped observation does not close the whole feature.

## Behavior and coverage

| ID | Owned behavior | Implemented and observed scope | Remaining scope |
| --- | --- | --- | --- |
| DT-01 | Native row chooses Any or a strict numeric limit; inactive choice retained | Same native slider as neighboring controls, ordinary press versus drag, malformed repair, layout recovery and seven owned language catalogs/tooltips | Wider settings/world/inherited UI combinations |
| DT-02 | Zero blocks governed routine use; equality rejects; missing hediff is zero; Any differs from numeric 100% | Map zero/Any, seeded positive/cap equality, natural-decay eligibility and actual Tea-to-Yayo shared tolerance | Other chemicals and full accumulation/clearance combinations |
| DT-03 | Scheduled permission preserves native mood, interval, quantity and history; S024 adds potential mood | Map/caravan scheduled use and direct S024 potential-mood conjunction; independent controls, layout failure/recovery, ordinary physical doses and native interval | Remaining drug/policy combinations and later S029 corrections |
| DT-04 | Direct joy keeps its native exceptions; social drinking lacks those exceptions; patient treats use the patient | Direct joy, positive DrugDesire exception, social drinking and drinkless social recreation | Party lifecycle and S024 patient-treat producer |
| DT-05 | Medical/manual/refill retain their separate authority | Explicit manual Drink and independent refill; scheduled attached pickup remains coupled to its accepted dose | S124/S134 medical/dependency producers and remaining exclusions |
| DT-06 | Copy/save/maintenance retain immutable owned choices by row identity | Native copy/edit/save/reload, failed overwrite, late-load rejection, save/inheritance contexts and in-flight dose save/resume | Full S029 maintenance/catalog combinations |
| DT-07 | Malformed retained data is visible and conservatively blocks governed use; missing field means Any | Lossless codec, retained malformed bytes and actual native slider repair | Remaining malformed/route/save combinations |
| DT-08 | Common default-on setting and honest unavailable/effective-off behavior preserve stored choices | Global timing, dormant save override, inheritance, two-save switching, main-menu guards and layout-unavailable recovery | Native original-provider suppression and remaining failure/composition contexts |

The table summarizes historical reviewed builds. Later affecting runtime changes require new
focused native evidence; unrelated test additions do not replay completed workflows.

## Composition and ownership

S029 owns policy/catalog identity and maintenance transactions. S138 contributes its version-1
Any/numeric/invalid state, weakly attached to the actual current row. Copy and rollback retain raw
unknown bytes; there is no hidden archive for deleted rows and no import of the original mod's field.
A later owned-schema migration requires its own backup, atomicity and restoration contract.

S024 scheduled potential mood and tolerance now compose directly through one admitted native hook,
with independent settings and failure handling verified in the focused English batch. Its future
patient-treat selector must check the actual recipient without granting new self-recreation exceptions;
that producer remains unimplemented. S124/S134 likewise remain future producers with separate
medical eligibility, dose history and shared pending-chemical ownership. S139 may later propose
choices through owned state. No internal Harmony patch stack or universal ingestion veto is used.

Core and Harmony are required. Prepatcher and the original mod are not dependencies; no XML
Extensions or product Gateway dependency is introduced. Optional drug content uses resolved native
capabilities; unsupported pharmacology is not guessed. No external provider adapter is required.

## UI and configuration

The row choice is save-local gameplay state in the existing native drug-policy manager. Following
the user's 2026-11-12 correction, it uses the same native horizontal slider as the neighboring mood
and recreation controls. There is no extra tolerance editor dialog or bottom tab. Numeric 100% and
Any remain distinct. Actual adjustment uses native dragging; painting or an ordinary press preserves
retained precision. Older modal-editor captures are historical to their exact builds.

`drugTolerance.enabled` is default-on in Drug policies, through the common Global default,
Use global and Current save model. Saves resolve the current inherited global value and retain
dormant overrides. Save controls and callbacks are disabled at the main menu. Changes affect the next
eligible selection/editor refresh and do not cancel or reverse an executing dose.

An active original `seohyeon.onlyiftolerancebelow` triggers the specified bounded startup warning
and best-effort ordinary effective-off behavior. Stored preferences remain intact; merely installed
but inactive does not trigger suppression. The original mod is never edited or unpatched.
This policy's native workflow remains unaccepted; it is not a coexistence promise.

## Native evidence and limits

The owning tasks link the earlier persistence, map routine/social, strict-boundary/shared-chemical,
global/save/in-flight, two-save/main-menu and independent-refill gates. Those records include exact
reviewed builds, causal player actions and personally inspected physical-item/Health outcomes.

Recent local ledgers under `artifacts/Implementation/megamod-p0/`:

| Checkpoint | Scoped result | Ledger |
| --- | --- | --- |
| `812ae49` | Native slider editor/layout: 131 steps, all 15 captures inspected | `s138-native-slider/native-verification.md` |
| `4d4d57e` | Caravan scheduled tolerance: 148 steps, all 13 captures inspected; zero retains two Beer/no effects, Any consumes one and keeps the native interval | `s138-caravan-consumer/native-verification.md` |
| `90f9245` | Seven fresh language runs: 392 steps, all 49 captures inspected; native drag, inactive retention and both tooltips | `s138-language-layout/slider-language-native-verification.md` |
| Runtime `43f0b71`, reviewed fixtures through `91cb62c` | S024 scheduled composition: four English cases,679steps/58 inspected captures; independent restrictions, native map/caravan doses, layout recovery and settings reload | `s024-resume/native-verification.md` |

The beta lacks Core translations. The launcher temporarily supplies language metadata so the real
owned catalogs render alongside Core's accented-English fallback; cleanup removes that metadata.
These observations do not accept future fully translated Core geometry. Numeric culture remained
en-US in all seven runs; there is no removed-text-editor or alternate decimal-parser claim.

Every accepted slice retains exact package/process/mod/config identity, complete classified logs,
native console inspection and graceful cleanup. No broad compatibility, addiction safety or optimal
schedule follows. The exercised routes preserve native costs, cadence, supplies and health effects.
Full feature acceptance and the remaining composed playability review stay open.
