# Everybody Gets One - Continued: source research and draft decisions

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Queue **S018**, intake **P1-14**, origin **replacement**. Started **2026-11-02** after completing
S017's draft and independent review. [S018 draft](../../../../openspec/changes/specify-megamod-adaptive-production/)
and [coverage report](../replacements/everybody-gets-one.md) now exist. **No implementation or live acceptance.**

## Documentation first

Read the [continued Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3530806680),
[change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3530806680) and
[maintained source repository](https://github.com/MemeGoddess/RimWorld-EverybodyGetsOne).
The documentation describes population plus offset, quantity per person and a surplus-ingredient
trigger restricted to single-ingredient recipes. Who counts is editable through TD Find Lib.
The description warns that negative offsets entered in the overview can be reset by Details; this
needs exact installed UI tracing, not acceptance as desirable behavior. Recent notes include 1.7,
a compiler-optimization fix, one-unit adjustment for custom modes, and earlier map/filter preservation.
Comments alone do not establish a reproducible bug; a reported baby-filter issue was later retracted.

Also read the [required TD Find Lib description](https://steamcommunity.com/workshop/filedetails/?id=3529443295)
to identify the consumed query/editor/search-transfer contract. It supplies configurable predicates,
map scope and saved-search exchange shared with other clients. Its full selected-feature analysis
remains its own later queue turn; S018 must inspect enough of the dependency to specify person counting
accurately, without silently adopting every library feature or its UI placement.

## Exact inspected inputs

- Read-only installed root: `F:/Steam/steamapps/workshop/content/294100/3530806680`.
  About name **Everybody Gets One - Continued**, author **Uuugggg**, continued by **Meme Goddess**
  according to Workshop; package **Memegoddess.EverybodyGetsOne**, supported **1.1–1.7**.
  Required **brrainz.harmony** and **Memegoddess.TDFindLib**; loadAfter TD Find Lib and falconne.BWM.
  No About modVersion field. About SHA-256
  **B2A60479E5F4CF2C5A8931E41415253EC21BA2278A27523AEF026C5D948867D7**.
- Installed `1.7/Assemblies/Everybody_Gets_One.dll`, assembly/file **1.0.0.0**, SHA-256
  **51259BBC102B2566066CFC956A9B7C6E7F74CB686F0433B33E2FF4A927C8C433**.
  Full decompilation already obtained for S017 consumer inspection is reused at
  `artifacts/Refinement/megamod-spec-20261102/better-workbench-management/consumers/everybody-17`.
  Obtaining the full project does not mean every file has been read yet.
- Public checkout: `artifacts/Refinement/megamod-spec-20261102/everybody-gets-one/upstream`, pinned
  **7475bc5d16398ed8a71eca5c257cfa8a21899c86**, **2026-10-22T14:42:54+11:00**,
  commit message about the compiler-optimization issue. All **13** compared inputs match installed
  text after normalizing line endings/BOM: eleven Source C# files, LoadFolders and 1.4 repeat Def XML.
  The public 1.7 DLL SHA-256 is **2942AAFC5BBB865DED6B7E0355E6C7DE839538FCC2065266873FB9CE6F92B8BA**,
  **different** from installed. Do not claim byte-identical builds; installed decompilation governs.
- Installed LICENSE.txt is **MIT**, copyright **2025 Alex Tearse-Doyle**. No code/art reuse or assembly
  packaging is authorized by merely researching behavior; retain attribution/license if reuse occurs.
- LoadFolders 1.7 includes root and 1.1 through 1.7, plus conditional
  `1.7/ImprovedWorkbenches` for falconne.BWM. That bridge directory is absent from the installed 1.7
  inventory; the main DLL has its separate BWM UI adapter. Earlier bridge DLLs exist for older versions.
- TD Find Lib inspected metadata: Workshop **3529443295**, package **Memegoddess.TDFindLib**, supports
  **1.4–1.7**, requires **Memegoddess.TDSBugFixes**, Workshop **3529433984**. Exact dependency inspection
  is recorded in the follow-up below. These are source dependencies; owned core query behavior does
  not require them, and external exchange is optional.

## Completed reads: repeat modes and controls

Read installed source `Everybody_Gets_One.cs`, `Settings.cs`, `BillRepeatTypes.cs`,
`ColonistCounter.cs`, `CountEquippedCountsPersonCounter.cs`, `CounterClipboard.cs`,
`BillOrphaner.cs`, `InsertFindLibEditorButton.cs`, root/1.3/1.4 BillRepeatModeDef XML and English
root/1.4 keyed strings. Read installed decompiled `Extensions`, `ShouldDoNow_Patch`, `Mod`,
`JobDriver_DoBill_Patch`, `PersonCountWorldComp`, `CountEquippedCountsPersonCounter`,
`CounterClipboardPaste`, `ImprovedWorkbenches_Patch`, `OrPersonCount_Transpiler`, AssemblyInfo.
Other compiled classes and native call boundaries still need comparison.

- Current source DefOf references **TD_PersonCount**, **TD_XPerPerson**, **TD_WithSurplusIng**.
  The 1.4 repeat Def file defines those modes with listOrder **15/17/25**. Older root/1.3 files
  contain colonist/slave predecessors; versioned same-path resolution and any migration must be
  checked before promising old Def/save identity compatibility.
- Let P be the query's result count. Offset mode uses **P + targetCount**, multiplier mode
  **P × targetCount**. Resume threshold is transformed the same way using unpauseWhenYouHave.
  There is no clamp in these helpers. Switching mode sets repeatMode only: it retains the existing
  numeric fields rather than establishing a new offset/multiplier default. Native fresh bills have
  targetCount **10** and unpauseWhenYouHave **5**; do not infer initial offset zero from the mod title.
- Population-mode ShouldDoNow checks suspension, sets paused when products reach derived target,
  clears paused at/below derived resume or when hysteresis disabled, then requires not-paused and
  products strictly below target. CanUnpause uses derived target. Current product count still comes
  through the recipe counter, making S017 composition relevant.
- Surplus mode only offers success when recipe.ingredients.Count is **one**. It checks suspension,
  lets a bound unfinished product continue, otherwise requires ingredient stock **strictly greater
  than targetCount**. It is a start trigger, not a reservation or promised minimum remaining after a
  batch. It uses the map ResourceCounter: fixed ingredient directly, otherwise allowed Defs in the
  intersection of recipe fixed filter and bill ingredient filter. It sums physical resource counts,
  not recipe nutritional/work units. Zero/non-resource/multi-ingredient edges need native comparison.
- Overview displays products/(P+offset), products/(P×multiplier) with multiplier, or ingredients >
  threshold. Custom mode +/- adjustments are **one**, independent of recipe batch size. Offset
  overview replaces native minimum clamp; Details target/resume/count substitutions and negative
  input behavior need exact compiled/native confirmation before designing a correction.
- Person editor button appears in the ingredient pane only for the two population modes. It shifts
  the pane by 36 pixels in source. Source settings constructor/UI integration is commented out;
  Settings.cs contains an unused sample checkbox, **not a real user setting to port**. Startup uses
  Harmony owner **Uuugggg.rimworld.Everybody_Gets_One.main** and PatchAll.
- JobDriver_DoBill_Patch contains a static constructor that searches compiler-generated methods by
  a sealed type's two-field shape and applies the mode transpiler. Its startup invocation/discovery
  is not yet established; do not call it active merely because its method exists.

## Completed reads: population query, state and copying

- WorldComponent maps Bill_Production references to deeply saved QuerySearch under
  **billPersonCounters**. Missing search lazily creates a search tied to bill.Map; default listing
  **Everyone**, include **IsColonist**, exclude **IsSlaveOfColony** and default **quest lodger**.
  Exact listing/map/quest semantics live in TD Find Lib and remain to be traced.
- CountFor calls RemakeList then returns **result.allThingsCount**, not an explicit Pawn count.
  The editor exposes inherited query controls, so non-pawn selections/count multiplicity cannot be
  assumed away without inspecting the dependency. Empty ChosenMaps is automatically repopulated
  with bill.Map. OrphanBills removes a deleted map from every chosen-map list. Migration ingests the
  old PersonCountMapComp into world state, preserving existing entries and removing the old component.
- PersonCounterEditor inherits SearchEditorRevertableWindow. It pauses, closes on outside click,
  permits one editor, is not draggable/resizable, and defaults to **750×320** at the top center.
  Transfer identity is **TD.EGO**, receiver label Use as person counter, CloneArgs.use; it registers
  on PostOpen and deregisters on PreClose. Actual revert/import/map behavior needs the base class trace.
- Count equipped in population modes replaces native FreeColonistsSpawned candidates with the
  query result's pawns **only when all three equipment/apparel/inventory trackers are non-null**.
  It does not restrict those results back to bill.Map. Other repeat modes retain native colonists.
  This changes the counted carrier set, not only the target number; compose directly with S017's
  primary/additional products, away stock and dedup rather than stacking another count postfix.
- Native bill copy stores an inactive query clone in a static CounterClipboard; copying a bill
  without a person counter clears it. Paste intercepts AddBill in ITab_Bills.FillTab, then attaches
  CloneForUseSingle(destination map). Exact method/closure coverage, source edits, stale clipboard
  lifecycle, default searches and cross-map references need comparison to S017 snapshots.
- Deleting a bill through Building_WorkTable.Notify_BillDeleted removes its query; Building.Destroy
  removes all worktable queries. Minification/movement and other bill givers need explicit ownership
  treatment. No automatic foreign migration is promised by reading these source routines.
- S017 already established the real **MirrorBills** older bridge and current **DrawFilters** adapter.
  The latter enables BWM count controls in the two person modes, excluding surplus. Preserve those
  intentions directly after completing S018's own analysis, not as patches to megamod itself.

## Completed dependency, native and consumer follow-up

Installed TD Find Lib **1.7/Assemblies/TD_Find_Lib.dll**, assembly/file **1.0.0.0**, SHA-256
**1A9218A2FF0908E35A82AFD5F1DE12980F7431CC5D064F9979AFFB7A2168A818**, was decompiled read-only to
`artifacts/Refinement/megamod-spec-20261102/everybody-gets-one/dependency-tdfind17`.
Read source QuerySearch and SearchEditorWindow, then compiled query rebuilding/cloning, QueryHolder,
HeldQueries, ThingQuery enable/matching, ThingQueryWithOption resolution, LinqEx.AnyX, revertable
editor, transfer interfaces and the basic-property/quest/age/skill/developmental-stage/trait queries.
The wider library catalogue belongs to selected S121; this is the consumed EGO path, not a claim to
have specified every library predicate.

- Everyone means spawned map pawns. The default three predicates select player humanlike colonists,
  excluding colony slaves and quest lodgers; there is no age, health or downed exclusion. Children
  and babies can count. Caravans and unspawned container occupants do not join this default listing.
- Listing alternatives include selectable things, items, buildings, plants, natural objects, junk,
  all ordinary things and known contents. Result total is **sum of stackCount**, not Pawn count.
  Non-pawn searches are therefore real source functionality. Ordinary All/Selectable also traverse
  known holder contents. Source includes debug listings and bypasses fog in god mode; these are not
  evidence that ordinary production should change when a developer toggle changes.
- Conditions support inclusion/exclusion, enabled/disabled clauses, all/any and nested groups.
  AnyX with zero means ordinary Any; positive N requires at least N successes. All with no enabled
  clauses passes; Any with none fails. Missing Def/reference resolution records an error and disables
  the clause; filtering skips disabled clauses, potentially broadening an intended search.
- Chosen maps default to the bill map. Explicit Current map uses Find.CurrentMap; any CurMapOnly
  child forces that choice even in other scopes. All maps uses loaded Find.Maps, not world caravans.
  CloneInactive drops map references. CloneForUseSingle(destination) retargets the copy; an empty
  chosen-map list otherwise falls back to the viewed map. EGO also repopulates empty chosen maps.
- Rebuilds are normally once per game tick, unless a query edit marks them dirty. Paused world changes
  are not thereby guaranteed fresh. A catch around the map loop logs and then sums partial results;
  a failed query is not distinguished from a valid partial/empty population by EGO's caller.
- The editor edits live query state, stores an inactive original and asks whether to keep changes
  after closing. Import/revert copies listing and children/all-any settings, retaining destination
  name and map scope. Full cancellation of map/name edits is not established by that code.
- Public ISearchReceiver exposes Source, ReceiveName, CloneArgs, CanReceive and Receive(QuerySearch).
  SearchTransfer.Register/Deregister maintain receiver/provider/library lists. EGO registers only
  while its editor is open, using Source **TD.EGO** and CloneArgs.use. This is a real optional exchange
  contract, unlike a saved mod-list entry. Unsupported foreign query clauses cannot safely be omitted
  during an independently owned replacement's import.

Read additional compiled EGO menu/config/repeat-text/unpause/editor/clipboard/map-comp/deletion
classes and source compiler-generated-method helper. The menu checks CanCountProducts for population
modes and exactly one ingredient entry for surplus. Switching mode changes only repeatMode. The
installed JobDriver static constructor has neither a startup attribute nor a discovered caller; its
presence does not prove that legacy generated-method patch executes.

Native inputs: **1.7.4995 rev309**, Assembly-CSharp SHA-256
**56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21**.
New reads/decompilation under the same artifact root cover ModContentPack, Listing_Standard and Pawn;
reuse the retained `workbench-linking/game` bill dialog, Bill_Production, WorkGiver_DoBill,
JobDriver_DoBill, Bill_ProductionWithUft and Building_WorkTableAutonomous inputs.

- LoadFolders traverses configured folders in reverse; relative-path de-duplication prefers the
  higher-priority file. The current 1.4 repeat-mode XML therefore supersedes its root/1.3 predecessors
  on this 1.7 folder list. This does not prove a foreign-save migration path.
- Native Details IntEntry defaults to a zero minimum, whereas EGO removes overview clamps for signed
  offsets. Its sequential field-load/by-reference rewrites depend on dialog layout that changed;
  live correctness is unverified. Own signed controls must show operands and derived totals honestly.
  Native target display uses an Infinity label from 999999; do not apply that label to a finite
  computed population total. Source +/- uses base step one, with native keyboard multipliers.
- Native WorkGiver checks ShouldDoNow before selecting bound unfinished work, autonomous collection
  or new ingredients. EGO's surplus bypass is only for a bound unfinished product. Native autonomous
  forming ticks independently after start; a low surplus can otherwise block later worker collection.
  Preserve committed work/collection explicitly in the owned composition, not by assuming this source
  special case covers all bill families.
- Native 1.7 JobDriver_DoBill.MakeNewToils has no legacy TargetCount-specific post-hauling recount
  branch. Do not port a compiler closure two-field heuristic to a target that no longer exists.
- Native ResourceCounter.UpdateResourceCounts traverses actual SlotGroup HeldThings, unwraps minified
  things and sums CountAsResource stack units that pass freshness/fog checks. Its periodic tick is
  204 ticks, with additional CheckUpdateResource updates. No generic pawn inventory or loose-map
  enumeration exists there. Any supported resource container in the draft must participate in this
  storage-resource contract or have an explicitly verified provider extension; no blanket container
  parity is inferred. Own paused freshness is a proposed improvement.
- Review follow-up: native Bill.IsFixedOrAllowedIngredient explicitly admits a matching fixed entry
  before recipe/bill filters, WorkGiver uses that distinction, and Details hides filters for all-fixed
  recipes. The initial draft's fixed-ingredient intersection was wrong; it is corrected to native
  eligibility. Only editable alternatives intersect current bill restrictions.
- Review follow-up: ThingQuery.AppliesTo tests the outer thing, then GetInnerThing (minified InnerThing
  or corpse InnerPawn), and compares the combined match with include. The initial draft omitted this
  visible contract; dead-colonist corpse matching, post-match exclusion and one represented counting
  identity now have an explicit scenario. This is part of the initial predicates, not deferred S121.
- Host cross-check: native Pawn.IsColonist checks player faction/humanlike/non-subhuman and slave
  security, not prisoner status. Pawn_GuestTracker.SetGuestStatus does not change an ordinary arrested
  colonist's faction. The default therefore includes arrested player colonists, while foreign
  prisoners fail the faction test. Corrected the draft's blanket prisoner exclusion.
- Host cross-check: FloatRangeUB/IntRangeUB include values beyond absolute end stops when those ends
  are selected. Skills initialize Animals, range 0–20 with both bounds effectively open, passion
  Ignore; age initializes biological 0–50 with open lower end (display maxima 200 biological / 1000
  chronological). DevelopmentalStage defaults enum 8 = Adult in inspected native code. Trait starts
  Pyromaniac and first degree. The draft makes open bounds/defaults explicit and distinguishes a
  deliberately finite upper limit from No upper limit.

Bounded installed text and DLL literal scans are retained as `consumer-text-paths.txt` and
`consumer-dll-paths.txt` under the EGO artifact root. No external DLL literal consumer was found;
own assemblies/older bridge assemblies were excluded when interpreting this result. External text
hits were RimPy/community order metadata, **Mlie.UseThisInstead** replacement metadata mapping the old
Uuugggg package to the continued package, and LayoutHarness saved mod lists. This does not establish
universal absence of dynamic or uninstalled consumers. The concrete BWM adapters and TD Find Lib live
exchange remain actual integration surfaces.

## Draft decisions and remaining questions

The draft below is an independently owned proposal, not a report of observed gameplay. It preserves
three modes and editable query selection, composes directly with S014/S017, uses shared map data,
and deliberately corrects signed input, camera-dependent scope, partial/unknown counts, cancellation,
paused freshness and committed-result collection. It adds no new global setting. Own mode identities
avoid silently interpreting source repeat Defs without their saved queries; automatic source migration
and an assembly/package impersonation facade are not promised.

- **EG-Q1 — numeric/UI review:** proposed zero floor for effective targets, signed offset controls,
  explicit operand reuse on mode switch, complete cancel and finite large-number display need future
  UX/live validation. Source behavior and proposed corrections are separated in the draft.
- **EG-Q2 — shared query breadth:** default population, ordinary listings/group logic and inspected
  predicate families are concrete here. S121 must enumerate the rest of the shared predicate/library
  catalogue and its external codecs; S033 later uses the same engine/index. Record unsupported clauses
  visibly and reject whole imports until their behavior is specified. No full library parity claim.
- **EG-Q3 — exact adapters/lifecycle:** current beta special counters, provider shapes, native modifier
  keys, holder freshness, lost-map/cross-map references and opt-in foreign-save migration require
  implementation-time proof. TD exchange is optional and absent-safe; original EGO coexistence
  withholds our overlap. No current compatibility acceptance is claimed.
- **EG-Q4 — admission and acceptance:** design candidate groups need real per-patch target documents,
  executable validators, focused RED/GREEN, seven-language UI, independent implementation review and
  final native action-to-result acceptance. All corresponding tasks remain unchecked.

No game was launched and no installed mod was modified. The earlier read lists describe staged
research; their remaining-work wording is superseded by the completed follow-up and EG-Q1–4 above.
The next source is not started until this draft's validation and independent review are complete.
