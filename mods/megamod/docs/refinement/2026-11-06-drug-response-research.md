# S124 — Drug Response research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, target **1.7 beta**.
Read-only inspection on **2026-11-06**. This is evidence/refinement, not delivered behavior.
The [proposal](../../../../openspec/changes/specify-megamod-drug-response/proposal.md),
[observable draft](../../../../openspec/changes/specify-megamod-drug-response/specs/megamod-drug-response/spec.md)
and [coverage](../replacements/drug-response.md) distinguish selected changes from source behavior.

## Identity and primary evidence

1. [Steam description](https://steamcommunity.com/sharedfiles/filedetails/?id=2196401201), then
   [author repository](https://github.com/emipa606/DrugResponse), pinned at
   `bc24a72ff131946b687521a009c9cee89463b39b` (2025-07-20).
2. [Five-page author manual](https://github.com/emipa606/DrugResponse/blob/bc24a72ff131946b687521a009c9cee89463b39b/Source/DrugResponseNotes.pdf):
   all pages extracted and visually inspected, including the actual UI on page1. Pages2–4 explain
   controls; page5 documents the pain-drug extension. This is an older manual, not a 1.7 guarantee.
3. Installed `F:/Steam/steamapps/workshop/content/294100/2196401201`: About names pelador, package
   `Mlie.DrugResponse`, modVersion1.6.1, supported1.1–1.6, required Harmony. No LoadFolders.xml.
   Selected latest1.6 payload, root Defs/languages/README/license, and all12 versioned XML patches
   inspected. Do not infer that it loads or works on1.7.
4. Installed1.6 `MSPainless.dll`,56832bytes, SHA256
   `5577DB01936D5FF976059C63C9BFD1E9BF903C87CDD2DA0EC8948D6969DE10DE`.
   Its bytes equal the repository's1.6 DLL at the pinned commit. All12 production C# files read;
   installed assembly decompiled read-only and critical response branches cross-checked. Matching
   repository binary is provenance, not a claim that source was rebuilt reproducibly.
5. Exact native1.7.5000rev1275 `Assembly-CSharp.dll`, SHA256
   `D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`.
   Offline extracts: Recipe_AdministerIngestible, RecipeDefGenerator, DrugPolicy,
   Pawn_DrugPolicyTracker, JobGiver_TakeDrugsForDrugPolicy and Caravan_NeedsTracker.
6. Narrow S134 comparison: [Priority Treatment Steam](https://steamcommunity.com/sharedfiles/filedetails/?id=3813351382),
   [author guide](https://rmapi.dreadarm.net/mods/priority-treatment/guide/) and
   [API reference](https://rmapi.dreadarm.net/mods/priority-treatment/api/), then installed
   `Dreadarm.PriorityTreatment`0.1.22,1.7 DLL20992bytes, SHA256
   `AEC9D8DB18A1E080CE42F9BAAC584AECB654D65A2F083B084078C6ABB3570F1A`.
   DrugAdministration, WorkGiver_QueueAdministerDrug and QuillRemoval inspected; this is not the
   older `tk421storm.PriorityTreatmentRessurected` reference or S134's complete review.

Local evidence: `artifacts/Refinement/megamod-spec-20261106/drug-response/`, input/source manifests,
pinned author files/PDF, public guide/API, installed-dr/installed-pt decompilations and native extracts.
The web reader could not open the PDF/RMAPI; direct read-only retrieval succeeded. Poppler emitted
font-substitution notices; all five rendered pages remained legible and were personally inspected.

License metadata differs: installed LICENSE.md says MIT, copyright2020 Mlie; README/Steam retain
pelador's CC BY-NC-SA4.0 credit. Record both, do not assume the continuation grant resolves every
upstream asset/file. Independent behavior specification only; no source code, DLL, translation or
art copied into the product. Dreadarm reference states all rights reserved, copyright2026.

## Source behavior, not proposed corrections

| Area / source | Findings |
| --- | --- |
| Enrollment, MSPainlessData setup | Adds a comp at static startup to humanlike races containing OrganicStandard. Eligibility is colonist or separately enabled prisoner, alive, spawned or caravan member, not in mental state. No animal automation from this source. Runtime Def mutation is not an acceptable owned XML shortcut. |
| Polling | Pain every150ticks, condition response every200ticks; on-map paths require a current non-Ingest job. The comp saves last pain tick and per-condition/per-slot ticks. No proof that an unspawned comp is ticked in actual1.7 caravans. |
| Pain bands, MSPainUtility | Response requires pain strictly greater than1%; minor below15%, serious15–<40%, intense40–<80%, extreme>=80%. Exactly1% produces no response. A missing drug for the current band does not fall back to a lower band. |
| Pain selection, MSPainDrug | Requires MSPainless.MSPainDrug.ManagesPain and an ingestible GiveHediff stage with painFactor<=.8 OR painOffset<=-.2. Medical-category only unless UseNonMedical. It does not infer therapeutic purpose from the displayed name. |
| Research filter | UI-only filter; uses a finished direct recipeMaker prerequisite or a product recipe with no prerequisite/finished single prerequisite. Does not fully evaluate multiple prerequisites. Main-menu helper returns researched after a message. |
| Pain execution | Defers burning, mental state, drafting and sleeping non-prisoners. Uses a one-unit Ingest job, or bill if configured, intense/extreme override, prisoner, downed/unable to move, or medical rest with summaryhealth<75%. Searching is patient-based even for a doctor bill. |
| Condition execution | Visible non-added-part condition in explicit catalog; two independent slots, default medical bills. First slot may be self-use; second is bill-only in supported UI. Enabled on-map prisoners always use bills for both slots. Self-use checks mobility, medical rest and burning, but lacks the pain path's explicit sleep/draft checks. Source can forcibly end jobs and clear queued player work. |
| Drug availability | Own inventory first; otherwise map-wide closest reachable drug. Ordinary pawn: unforbidden/reservable/socially proper; prisoner path relaxes checks. No doctor-source admission before a bill is created. No stock means no response. |
| Policy, MSDrugUtility | Requires an entry if a policy exists; a positive-frequency scheduled drug suppresses responses regardless of current schedule eligibility. Does not check addiction or pleasure permission for general responses. No policy bypasses the entry check. Non-medical drugs rejected for teetotal player-faction pawns and non-player pawns. |
| Other dose guards | Optional full-immunity suppression. Anesthetic condition>=.8 suppresses its response. If an added drug effect already exceeds75% of the outcome's positive severity, source defers. This is not75% of max effect severity and not a general overdose guarantee. |
| History | Source records when an ingest job or bill is scheduled, not consumed; a failed job still starts cooldown. Pain starts at tick0 and therefore waits an initial24hours by default. Condition first-use has no equivalent delay. Pain expiry uses >=, conditions use >. |
| Bills, MSAddDrugBill | Guesses Administer_<drug>, deduplicates same recipe including suspended bills, appends native Bill_Medical. It emits native skill/bed/care/faction warnings but these warnings do not veto creation. Does not own cleanup when a condition disappears or a bill is canceled. |
| Caravans | Calls caravan.needs.IngestDrug using carried supply; ignores on-map bill mode. Pain skips night rest; conditions do not. Two condition slots can run in one pass. Actual1.7 dispatch and capability checks remain unverified. |
| UI | One minimized MainButtonDef DrugResponse, order100; fixed1100×764 MainTabWindow_DrugResponse. Ten boolean selectors, four pain selectors/info, wait slider, and two single-condition editors. Saved rules invisible unless individually selected; Set applies staged changes. Clear/clear-all operate separately per slot. No native pawn policy editor. |
| Persistence | Static DRSettings fields serialized through a WorldComponent; MSDrugMinor/Serious/Intense/Extreme, MSDRValues/MSDRValues2 and editor fields. Rules are semicolon records; pawn history comma records. Startup missing-def cleanup can discard rows or reset drug/wait/bill choices. No cross-save cache validity proof. |
| MultiplayerSupport | Conditionally registers private UI methods/static field watches and Harmony patches DoWindowContents under owner rimworld.pelador.drugresponse.multiplayersupport. This source-specific synchronization is not a ready-made owned multiplayer contract. |

### Complete source configuration inventory

| Setting | Installed default / range |
| --- | --- |
| Pain AI / condition AI | Off / off |
| Pain prisoners / condition prisoners | Off / off |
| Include non-medical pain drugs | Off |
| Only researched choices | On |
| Pain / condition notifications | On / on |
| High pain always bills | Off |
| Respond after full immunity | On |
| Pain method / wait | Self-use /24hours; slider6–48 whole hours |
| Four pain selections | None |
| First condition method / wait | Bill /24hours;6–120 whole hours |
| Second condition method / wait | Bill /24hours;6–120 whole hours; no self-use UI option |
| Initial condition rules | Empty |

The page1 screenshot shows an example configured save, not these initial values. `SetUseDB(bool,int)`
always writes UseDRBills, ignoring slot number; the second editor can affect the first editor's
staged mode. Preserve intended independent rules, not this mistake. UI text misstates the80%
boundary; use exact half-open bands in the owned draft.

### Source catalogs and declarative integration

Baseline conditions: Asthma, Burn, BloodLoss, Carcinoma, FibrousMechanites, Flu, FoodPoisoning,
GutWorms, Malaria, MuscleParasites, Plague, SensoryMechanites, SleepingSickness, WoundInfection,
Anesthetic. Also every ChemicalDef.addictionHediff except MSMental. Condition choices accept any
ingestible Medical drug; there is no proof that a selected drug treats the chosen condition.

Common Ailments/Continued additions: CA_CommonCold, CA_Conjunctivitis, CA_Earache, CA_Indigestion,
CA_Restless, CA_SoreThroat, CA_Hayfever, CA_SkinRash, CA_Sinusitis, CA_Minor_STD, CA_Fatigue,
CA_Headache, CA_Migraine, CA_PhantomPain. Diseases Overhauled: HepatitisK, StomachUlcer, Stupor,
Unease, PTSD, SuicidePreparation, Tuberculosis, KindredDickVirus, Sepsis, Toothache,
VoightBernsteinDisease, HansenKampffDisease, Necrosis, LymphaticMechanites, Psoriasis, CommonCold,
NewReschianFever, BloodCancer. Source checks active display names, not package IDs.

All12 XML files only add the pain marker. The following is a candidate inventory, not an accepted
optional-provider matrix; actual package identities/effective shapes must be bound before code.

| File / named provider | Exact candidate items |
| --- | --- |
| DR_Vanilla | SmokeleafJoint, GoJuice, PsychiteTea, Flake, Yayo, Luciferium |
| DR_AY / Apothecary Continued | AYElixirSoothing, AYTurmericTea, AYElixirInciting, AYHenbaneTea, AYHenbaneJoint |
| DR_DO / Diseases Overhauled | Mortracain |
| DR_DrugsPlus / Drugs+ | Hypomorphone |
| DR_MedicinePlus / Medicines+ | PoppyLatex, SSalts, ThornWeedNeedle, Upper, ThornWeedExtract, Morphine, Heroin |
| DR_MS / Medical Supplements Continued | MSMetasis, MSAspirin, MSRimCodamol, MSMorphine, MSOpiumPipe, MSRimcetamol |
| DR_RC2 / RimCuisine2 Smokes and Shrooms Continued | RC2_Dreamstick, RC2_Skag, RC2_Tango, RC2_Viko, RC2_Zope |
| DR_SS / Social Supplements Continued | SSVape_Regular, SSVape_Fruity, SSVape_Menthol |
| DR_VG / VGP Garden Medicine | VG_Ibuprofen |
| MedicineExpanded | Acetamol, Opimol, Vicomol, Oxymol |
| Remedies | FF_Painkiller |
| SI_Reborn / Smokeleaf Industry Reborn | SmokeleafCannagar, SmokeleafHash, SmokeleafLotion, SmokeleafPills, SmokeleafTincture, SmokeleafDistillate, CHO |

Several source patches use one sentinel item for a multi-item target; others use display names.
Owned adapters require package+individual-content+semantic shape checks, additive idempotent marker
merge, and no discarded unrelated extensions. A marker does not alone satisfy the actual pain-effect
test. Older RimCuisine1/SmokeleafIndustry support advertised publicly has no separate selected1.6
patch here; do not claim it from old documentation.

## Native and selected-source composition

Native RecipeDefGenerator generates one-unit administer recipes for actual drugs, with base ingest
work time, no anesthetic, humanlike restriction inherited from the drug and flesh recipe users.
Recipe_AdministerIngestible calls real Ingested, records thoughts/history and applies mutant-specific
availability. It does not simply remove a condition. Native dose tracking updates real drug history;
scheduled use applies frequency, mood/joy, willingness and overdose checks. The inspected native
scheduled overdose guard defers a capable-of-overdose drug while overdose severity>.5 and fewer
than1250ticks since such a drug. This is a limited automatic-use guard, not immunity to overdose.
Caravan IngestDrug consumes the actual item and updates owner/container state; its callers determine
eligibility. Calling it alone is not proof of safe response dispatch.

S032 already reconciles drug policies without changing valid choices or schedules. New addictive
drugs get native addiction permission on; general response configuration must not pretend that is
an independent response opt-in. S062 condition No-care/No-medicine and patient medical ceilings
remain authoritative for automatic treatment. Its MC07 operation boundary admits the surgeon's
eligible inventory alongside ordinary ingredients; MC05's patient/other-colonist/animal donor
extension is for tending, not automatic drug operations. Surgery uses best owns its stated medicine
exception. S021/S033 own shared quantities/candidate facts. S085's complete partial-dose contract
requires actual positive exposure on interruption to retain history and quantities; canceled work
with zero exposure is a different case. S086 Medical go-juice affects current classification but
does not impose one universal manual-use permission on all routes.

S134 actual overlap is narrow and proven: doctor-scanned bills for unconscious/deathresting in-bed
patients with unmet addiction or genetic dependency, one pending operation per chemical including
suspended operations. It selects a usable fixed-ingredient administer recipe from the patient's
recipes and a drug reachable/reservable/unforbidden by the doctor. Existing policy requires
allowedForAddiction; no drug-policy tracker permits a dose, while a tracker with missing policy
does not. Injured-surgeon veto blocks scheduling too. Bill cap15. Source selects first usable drug
in lister order; no optimal medicine comparison is implied. Its quill scheduling is separate,
visible quill + ordinary bed eligibility + available whole-pawn removesHediff recipe. No direct
DrugResponse assembly reference is present in these inspected files.

Proposed composition: one physical pending-dose record across owned producers; active/suspended
manual bills and current ingestion prevent duplicates. Positive-frequency schedule blocks general
S124 pain/condition supplementation; S134 may still satisfy actual unmet dependency in an
incapacitated patient with explicit addiction permission. It is not blocked forever by a schedule
the patient cannot execute. Neither producer alters policy or steals emergency medical work.
S134 quill removal and S137 contextual filtering remain separate from drug dosing.

## Contracts and deliberate changes

Installed text search of XML/C#/JSON outside the source found only replacement-catalog metadata
(`3396308787/replacements.json`) mapping pelador.drugresponse to Mlie.DrugResponse; a separate
database also lists the old package. This is bounded evidence, not proof of no binary/reflection
consumers. The author's documented external XML contract is MSPainless.MSPainDrug.ManagesPain.
The source's own12 patches consume it; no external installed text caller was found. Do not ship a
same-name type while the original is active or spoof the original package to satisfy find-mod gates.
Actual optional adapters/facade needs remain DR-Q5. No upstream serialized-world/component import
or removal-safety claim; unchanged native medical bills stay ordinary bills, not newly owned orders.

Proposed deviations are explicit in the observable draft: all configuration moves to common
settings; per-patient holds/status stay Health-contextual; valid row edits save immediately; two
responses share one visible condition row; successful consumption starts intervals; never erase
forced work; doctor eligibility determines bill supply; rules retain unavailable selections;
no automatic drug substitution; coherent caravan eligibility; shared deduplication/cancellation.
Source-like defaults are provisional, especially continuing after immunity and the75% effect guard.

## Open research and acceptance

DR-Q1's source/UI analysis is complete as a draft; final native usability remains. DR-Q2's observed
overlap is recorded, with the shared contract proposed; S134 still needs its full sequential change.
DR-Q3–7 track exact1.7 targets/callbacks, balance/dose edges, optional bindings/licenses, cancellation
and native UI/combined acceptance. No runtime tests, game launch, build, package, source edit,
normal configuration/save mutation or gameplay acceptance occurred.
