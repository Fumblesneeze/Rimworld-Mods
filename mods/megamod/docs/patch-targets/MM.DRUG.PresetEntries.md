# MM.DRUG.PresetEntries: retain valid choices after an unavailable preset entry

**36 focused host cases reviewed; initial preset and unchanged-catalog persistence workflows
natively verified.** The orchestrator integrated native baseline publication, registration and
exact native-body wiring. The reviewed two-scenario group passed 98/98 steps; root personally
inspected all 20 captures. This narrow acceptance does not establish content-change reconciliation,
normalization, dose history or save switching. Original-provider compatibility is outside scope;
the revised startup-warning/ordinary-settings-off path remains unverified. Unsupported
capture-result rewriting retains the explicit limit below; metadata rejection alone is not a
universal native-fallback guarantee.

Current F0 integration changes the initial publication source: `NativeDrugCatalog.CaptureInitial`
publishes one `DrugCatalogRead` with ordered identities/flags independently of the S029 startup
master; preset membership derives from it and adds this patch's availability. The shared definition
lifetime still owns invalidation, and no save-exclusion authority is inferred. Focused host evidence
is under `artifacts/Implementation/megamod-p0/s139-native-facts/catalog-publication.md`; review and
affected native preset acceptance for this changed startup path are pending. Historical live runs
above do not accept the revised runtime.

## Ownership and traceability

- Owner: `fumblesneeze.megamod` (`mods/megamod`), S029 / Drug Policy Fix.
- Kind: one narrowly scoped actual-body transpiler; no prefix replacement of the factory.
- Owning requirement: [Preset construction retains valid native choices](../../../../openspec/changes/specify-megamod-drug-policy-maintenance/specs/megamod-drug-policy-maintenance/spec.md), especially unavailable baseline entry followed by valid entries, malformed/unavailable baseline, construction before save choices, and bounded original-loaded warning/ordinary-settings-off handling.
- Technical scope and remaining work: [owning design](../../../../openspec/changes/specify-megamod-drug-policy-maintenance/design.md), tasks 1.2–1.3 and 2.5.1–2.5.4. This target alone does not complete full preset normalization, init/post-load recovery or S038 integration.
- First implementation: feature-local `Core/PresetEntriesRead.cs` and
  `Harmony/DrugPresetEntriesPatch.cs`; first fixture at
  `tests/Megamod.Harmony/Features/DrugPolicyMaintenance/DrugPresetEntriesPatchTests.cs`.
  Native baseline publication and identity wiring were subsequently integrated by root; the initial
  native preset and unchanged-catalog persistence workflows are recorded below.
- XML cannot express an invocation-local skip before a throwing native lookup without rewriting
  shared preset data. There is no inspected public per-entry callback. No owned megamod method is patched.

Evidence: installed Assembly-CSharp 1.7.5000 rev1275 SHA256
`D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`, reconfirmed 2026-11-08.
Retained `artifacts/Megamod/P0-C/native-1.7.5000/RimWorld.DrugPolicyDatabase/RimWorld.DrugPolicyDatabase.decompiled.cs`
SHA256 `944DF23088D5E4ABA3C3D7F7F93A975F71BBFBCF8CB51AE639D58C71DE9740D2`.
Current read-only ILSpy inspection confirms the source-entry/drug/lookup/CopyFrom chain at research
offsets 0x64–0x87 and loop increment at 0x8c. These offsets/hashes identify evidence, not admission.

## Target and minimal predicates

Assembly-CSharp, `RimWorld.DrugPolicyDatabase`, public instance nongeneric
`RimWorld.DrugPolicy NewDrugPolicyFromDef(RimWorld.DrugPolicyDef def)`.
Consumed native members include `DrugPolicyDef.entries : List<DrugPolicyEntry>`,
`DrugPolicyEntry.drug : ThingDef`, instance `DrugPolicy.get_Item(ThingDef) : DrugPolicyEntry`,
and instance `DrugPolicyEntry.CopyFrom(DrugPolicyEntry) : void`.

Metadata installation may be prepared before Defs; **active skipping requires finalized native
baseline publication before the supported factory invocation**. A native constructor is not proof
that the current Game or its saved content overrides exist. The initial-baseline hook is registered;
candidate save exclusions and general post-load reconciliation remain separate work.

The executable validator must establish these predicates on its actual incoming stream:

1. Bind the exact callable/member shapes above and identify one explicit-entry loop. The same
   effective `def.entries` and loop index produce the lookup drug and the CopyFrom source. Null-list
   handling and loop increment/test remain native. Missing or multiple eligible chains are unsupported.
2. Track the created policy from native MakeNewDrugPolicy through the lookup receiver; ensure the
   guarded source-drug load feeds that lookup. The lookup's result must be the CopyFrom receiver,
   and the same indexed source entry must feed CopyFrom's argument. Matching method names alone is insufficient.
3. Insert the guard after the existing source drug is evaluated and before lookup. Reuse that stack
   identity; do not add another entries[index] evaluation. True follows the untouched lookup/copy.
   False removes only the pending policy/drug operands and branches to the existing increment, then
   native loop test. Prove stack balance and that branches cannot enter the lookup around the guard.
4. The bypass region contains only the identified unavailable-entry lookup/copy operand preparation.
   Do not skip unrelated side effects. Reject ambiguous branch targets, unsupported exception-region
   crossings or additional control-flow into the protected interior; allow unrelated instructions elsewhere.
5. Preserve the native broad-pleasure loop, valid explicit copy order, policy object, label/sourceDef
   writes and return flow. The guard does not normalize, sort, copy values itself, replace the result,
   change database/default ordering or edit the source preset/list.
6. The injected capture and decision calls retain their immutable-baseline and Boolean-decision
   semantics in the effective composition. Relevant inner argument/result/receiver writes, bypasses
   or unproven selected effects make this target unavailable. Unrelated observers/owners do not.

The transpiler first validates a detached instruction description, then makes its own output changes;
failure returns the original supplied sequence unmodified, including labels/exception blocks. It
revalidates on every real composition. Local numbering, harmless aliases, offsets, nop placement
and unrelated compatible edits are not rejection reasons when the consumed data flow is proven.

Current supported stream binds the indexed List entry chain with an immutable direct argument or
an argument alias established in the straight-line entry, arbitrary compiler local numbering and
labeled nops. It proves that the created-policy store dominates entry to the explicit loop and
rejects branch/switch entry strictly after the receiver load through the matched creation store,
including intervening nops. Entry at the receiver or preceding nops executes the intact chain and
remains eligible. Dominating the loop alone does not prove the stored stack value
came from creation. It also rejects an address escape of that policy local and protects loop
increment/count as well as lookup/copy.
Exception-handler entries before the explicit loop and exception boundaries through the guarded
region are unsupported by this first validator. No pristine whole-method or offset/hash check is used.

## Native baseline and invocation lifetime

The first milestone uses **native registered baseline availability only**: current Item/drug Def
identity with a usable native drug comp. Main-thread finalized publication uses the reviewed
NativeDrugCatalog with explicit baseline allowance; stock, research, map reachability and requested
settings are absent. Missing/unregistered and no-longer-drug identities are absent from this baseline.
An available malformed drug rejects complete capture and cannot become an omitted row.

Proposed feature-local interface:

- `PresetEntriesRead`: immutable native identity membership plus a caller-owned validity receipt.
- Actual-body `Capture()`: capture one already-published baseline reference for this invocation, or
  no guard when readiness/admission is unavailable. It performs no worker DefDatabase/Store scan.
- `ShouldCopy(read, drugIdentity)`: native path for an absent guard; otherwise skip only an identity
  proven unavailable in the current admitted baseline. The identity is the native stack operand.

These are proposed contracts, not implemented APIs or a new shared lifecycle service. The read is
local to one invocation; recursive calls get their own local. There is no ambient scope, acquired
resource, IDisposable/finalizer cleanup, delayed callback or retained policy/pawn cache. A null
source entry remains an unsupported native content error; this target does not broadly catch it.
A nonnull source with an unresolved null drug has no baseline identity and can be omitted only
under the same complete-baseline proof as other unavailable references.

**Native initial phase inspected and centrally wired for the verified preset workflows.** An untouched dedicated
`[StaticConstructorOnStartup]` trigger queues the publisher through ExecuteWhenFinished during
PlayDataLoader's post-Defs CallAll. The existing main-thread drain runs that appended callback
after the synchronous constructors return, before dequeuing the native Quicktest/autostart/load
worker. The root idle pump is too late for this path. The callback requires the actual main thread
and loaded supported Defs, but must not require an empty long-event queue. Root instance state and
publisher must live outside the trigger type so ordinary early references cannot run its one-shot
constructor. [The owning design's native phase proof](../../../../openspec/changes/specify-megamod-drug-policy-maintenance/design.md#native-phase-proof-and-its-limits--2026-11-08)
records the exact methods, retained artifact paths/hashes, worker ordering and limits.

Root owns one existing `ContentSelection/Harmony/NativeContentInvalidationPatch` instance, admits
its HotReloadDefs/ClearAllPlayData entry coverage before the drug guard, and injects that instance
into both incident runtime and drug-baseline publication. Do not duplicate its owner/prefixes or
create another lifecycle authority. The same one-way atomic latch revokes the immutable baseline;
worker readers never scan DefDatabase or SettingsStore. Its unavailable admission withholds this
guard. Active original Drug Policy Fix detection requests the ordinary S029 settings disable path,
best effort, with a bounded startup warning and retained stored preferences; that request does not
disable this shared service when incidents still need it. Earlier package-suppression code and Unit
evidence do not establish acceptance of this revised ordinary settings path.
The separate root-owned invalidator reader migration and review must precede this early reuse:
sharing its instance does not prove an older public GetPatchInfo/attribute-reading path safe. The
preset bridge neither edits that reader nor installs a duplicate invalidator.

Native HotReload mutation is synchronous queued work and cannot start while the native queued
Quicktest/load worker is alive. Native main-thread factory work cannot overlap it; normal UI is
withheld during the worker. Language reload queues Clear+Load together, and initial-load recovery
clears before factories. This covers the inspected native paths only. Early external constructor
or deferred-callback factories, a provider's later deferred Def mutation, externally concurrent or
reentrant Clear/Reload, and modified queue/completion semantics need separate timing proof and are
not supported merely because a callback name or Loaded flag exists. ExecuteWhenFinished can run
synchronously in other contexts; it is not a general worker-to-main-thread dispatcher.

An invalid read before any omission leaves the native operation unchanged. Switching to native for
later iterations after an earlier omission does not prove a coherent completed repair; unsupported
mid-factory invalidation remains outside this route. The startup trigger is one-shot: after
HotReload/Clear, the latch and drug baseline remain unavailable. Successful Def-generation and
catalog reconciliation remain separate unfinished S038 work, not an implicit restart-only policy.
No universal error-log gate, factory rollback or successful hot-reload receipt is introduced.

S038 candidate save exclusions require a separate actual loading-save receipt and atomic publication
with the existing ContentApplication/batch boundary. Previous applied state, pending globals and an
incident-only applied policy are not baseline authority for those exclusions. Full init/post-load
repair and completed-preset normalization remain open and are not prerequisites silently supplied here.

## Composition, ordering, and invalidation

Actual-body capture occurs after outer prefixes. Earlier effective argument changes are naturally
observed; a prefix skipping the original body runs no guard and receives no claimed owned recovery.
The guard consumes no final return object. Compatible outer postfixes may deliberately transform
the result; preserving native identity means **our contribution** leaves native flow intact, not
that every external postfix must be absent/proven. No wider postfix export is required by this target.

Use the existing AdmissionCoordinator and OwnedHarmonyPatch receipt for one owned transpiler slot.
Validate the actual stream after relevant earlier transpilers and require a supported publication
position/effective proof that later ordinary or inner changes cannot bypass the inserted branch.
Use HarmonyPublication's published-generation/metadata mechanisms where they cover these consumed
operations. Its reporting and converter cleanup policies are not a blanket proof for this new target.
No synthetic foreign-transpiler replay, foreign callback execution, attribute construction or patch
factory resolution is an inspection strategy. Reject factory-shaped registrations before sorting
metadata through an API that may invoke them. Additional C01 work must follow a concrete unsupported
consumed predicate; this draft requests no generic metadata expansion.

Recomposition revokes publication until its new actual stream and required metadata are admitted.
An older invocation/read cannot be silently revived by a later successful generation. Refresh runs
at an explicit supported safe boundary; no per-row/per-frame metadata scan or automatic unchanged
generation retry. Arbitrary removal of an owned observer or a Def-generation transition has no
recovery claim without its explicit revalidation/publication path. Exact entry/freshness wiring is
still an integration prerequisite, not implemented by this document.

The implemented bridge exposes `Candidate`, `Enable`, `Disable`, `Available`,
`NeedsPublicationRefresh`, `RefreshPublication` and `Failure`. Its constructor takes the factory,
creation/list/drug/lookup/copy reflection bindings, `Func<PresetEntriesRead?>` and the protected
warning action. `PresetEntriesRead` detaches membership by reference identity and accepts the
caller-owned validity callback. Actual-body capture performs no metadata inspection; it checks
the published body generation both before and after invoking the baseline reader. Root invokes
explicit publication refresh outside provider locks; unchanged denied generations do not request
another automatic attempt. ExpectedVersion/inspection exceptions revoke authority and return the
unchanged supplied stream, so oversized metadata cannot block a foreign removal/rebuild.

When capture returns no read, an emitted null check branches directly to the native lookup before
calling ShouldCopy. This is tested against an actual inner decision-result writer: denied metadata
cannot make that callback omit valid copies while ordinary capture is inactive. Relevant native
lookup/copy/count changes are rejected; readonly no-argument observations remain admitted.

**Unsupported effective capture rewriting:** an inner argument/result writer on the injected
Capture operation is rejected by metadata admission. If a foreign patch nevertheless forges the
emitted nonnull read during an executing unsupported composition, that local can bypass the null
check; no claim of native fallback or safe continued preset construction is made for that case.
The focused test proves denial without inspection executing the callback, then recovery after
its removal. It deliberately does not relabel an availability getter as proof of native fallback.
There is no new invocation framework, speculative foreign execution or arbitrary detour removal.

## Admission, failure, and diagnostics

Root supplies its immutable canonical active-package snapshot. Active `Mlie.DrugPolicyFix` withholds
this owned overlap; mere installation does not. Preserve its patches and all unrelated features.
Optional external filtering requires its actual finalized timing to be admitted; its installed
version/metadata is not sufficient. A no-longer-drug classification can be a baseline fact without
claiming support for the provider's entire lifecycle.

For this milestone, only the preset-entry guard is advertised as available. It does not certify the
broader early-maintenance atomic group. Registration is idempotent; failed install/validation rolls
back only acquired owned work and disables the route. Missing/ambiguous stream, unavailable finalized
baseline, malformed available catalog or unproven relevant effective changes leave native/provider
behavior intact and withhold recovery. Do not catch all lookup errors: an available drug missing
from an unexpectedly changed target policy still follows its original native failure path.

Use the shared protected sink, bounded once per denied startup/Def or patch generation rather than
per entry. Example (values bounded):

`[fumblesneeze.megamod] Patch MM.DRUG.PresetEntries unavailable; target=RimWorld.DrugPolicyDatabase.NewDrugPolicyFromDef; targetSpec=MM.DRUG.PresetEntries; predicate=finalized-native-baseline; reason=no admitted baseline before this factory phase; unavailable=unavailable-drug preset omission; fallback=native/provider preset behavior.`

Do not claim native loading succeeds merely because owned recovery is inactive. An explicitly
disabled feature is not a failed candidate. This helper installs no sorting or policy write batch.

An active original's warning is separate from target-admission failure. Use the same effective-off
behavior and timing as manually disabling S029, best effort, without permanently rewriting stored
global/save preferences or modifying the original. An installed-but-inactive original does not
trigger that policy. Do not add original API/facade/parity checks or original-provider-present
compatibility runs, or promise sole-provider behavior or rollback of all earlier effects. Native
target semantics, genuine content integrations actually used and own-save continuity remain required.

## Verification matrix and final observation

| Case | Expected admission/result | Honest test tier | State |
| --- | --- | --- | --- |
| Supported unavailable entry before two valid entries | Skip only unavailable lookup/copy; following valid calls retain source order and policy identity | Harmony scoped factory fixture; Defs native classification; isolated native controls | Host GREEN; initial constructor-created preset and following valid choices observed natively |
| All entries available; broad pleasure and explicit overrides | Original calls and resulting native choices preserved | Harmony; isolated native preset workflow | Host GREEN; broad pleasure and explicit choices observed in unavailable-entry native fixture; separate all-available native case pending |
| Native edit/assignment and unchanged-catalog save/reload | Saved choices and assignment survive without preset reapplication; loaded identities are reacquired before native restoration | Isolated native player workflow and exact-process captures | Verified, task 2.5.4; no content repair or dose-history claim |
| No baseline; available missing/null comp; callback rejection | No incomplete-catalog omission; bounded unavailable reason; no claimed native recovery | Defs / host policy plus native diagnostic control | Scoped host GREEN; native pending |
| Different local numbers/nops; unrelated compatible prefix/transpiler/postfix | Same guarded data flow accepted; external owner preserved | Harmony | Alias/labeled-nop and native-shaped broad-loop locals GREEN |
| Prefix substitutes effective input or skips native body | Actual-body capture sees effective input; skipped body runs no owned callback | Harmony | GREEN |
| Changed source/receiver, ambiguous loop, bypass edge | Unchanged incoming stream and unavailable guard | Harmony | GREEN; unsupported EH remains a stated validator limit |
| Relevant inner changes to capture/decision/lookup/copy/count | Admit only preserved consumed behavior; capture rewriting has the explicit unsupported limit above | Actual owned Harmony | Scoped GREEN; no universal forged-capture fallback claim |
| Install rejection/repeated init/later repatch | Owned cleanup only, one registration, stale generation cannot regain availability | Harmony | GREEN; not proof of rollback after arbitrary physical detour failure |
| Def baseline absent/expired before first skip; potential mid-call expiry | Native fallback before omission; unsupported mid-call phase never advertised as coherent repair | Host receipt fixtures plus actual source/lifecycle proof | Pending |
| Original active / installed only | Bounded startup warning and best-effort ordinary S029 settings-off behavior; stored preferences retained; inactive installed source does not trigger it | Focused package/settings and warning-route checks; no original-provider compatibility run | Revised ordinary settings path pending; earlier package-suppression Unit evidence is historical |
| XML merge/additions | No preset/Def XML mutation by this target | Not applicable | No XML patch |

First executable behavior proposal: a shape fixture retaining the native source/drug/lookup/copy
structure contains an unavailable entry followed by two valid entries; the real owned transpiler
must omit just the unavailable lookup and preserve both valid copy calls/order and the original
return path. Capture an actual behavior RED before implementation, not a compile error. Def fixtures
register every needed constructed Def in an initially empty scoped database; no loaded-mod fakes.

The first implementation lease should be limited to feature-local `Core/PresetEntriesRead.cs`,
`Harmony/DrugPresetEntriesPatch.cs`, its matching Megamod.Harmony fixture and these owning docs.
Inject the immutable baseline capture function into the bridge; target/member bindings and actual
incoming-stream validation belong to the bridge. The fixture supplies opaque reference identities,
not a fabricated active mod. Central startup publication, single shared invalidation ownership,
incident constructor injection, project links and registration remain root-owned prerequisites.
Neither the first test nor those plumbing changes invoke full batch/init repair, add a sort, apply
save exclusions, normalize completed presets or claim native acceptance.

First host RED/GREEN on 2026-11-08 used the exact filter
`FullyQualifiedName=Megamod.HarmonyTests.Features.DrugPolicyMaintenance.DrugPresetEntriesPatchTests.UnavailableEntryBeforeTwoValidEntriesPreservesNativePolicyAndExplicitChoices`.
RED executed one test and failed with `ArgumentException: Unavailable drug` from the original-shaped
lookup while the owned no-op transpiler was installed. GREEN executed/passed one test with the
conditional skip. Both raw `.trx` and `.dotnet-test.log` are retained:

- RED: `artifacts/TestResults/20261108T080615576Z-50112-e28043d477d14383a2e8d84f58ca6afd/`, TRX SHA256
  `9B6114FC2B52C5876353A56BED6FC3C0B4DA02330F46EEA4605C761BE6DABEE6`.
- GREEN: `artifacts/TestResults/20261108T080936053Z-24244-98330d0502704f33933cff95c2710032/`, TRX SHA256
  `ABC02EAA2B559F9F36A70C14EC13C9C7753815EE0BFB206344B3B6BB5E436918`.
- Frozen first-slice source SHA256: PresetEntriesRead
  `5E7DC74F099D2188B3505911E005B099D168B3CB8C1DB8370C8E696707055CE5`;
  DrugPresetEntriesPatch `E86071276817390B62129C44398ECB30BB42626E24352CB4B0D9C0F0428F404F`;
  fixture `7B0192D495BF07EF6A8737E241C5B02242995FA420B0FEC2CC013D4347524C93`.

This temporary stable boundary returns the Harmony output lease for another owned correction.
It did not assert that all target predicates or composition cases had passed. At that checkpoint,
harmless aliases/control-flow variants, unchanged-stream rejection, inner effects, unavailable
callbacks and later repatching still required focused evidence, subsequently recorded below.
The earlier original-provider integration obligation is superseded by the 2026-11-08 bounded-warning
and best-effort ordinary-settings-off policy; no original compatibility gate remains.

### Completed host guard tranche — 2026-11-08

This supersedes the first-slice pending host cases above. The exact final filter was
`FullyQualifiedName~Megamod.HarmonyTests.Features.DrugPolicyMaintenance.DrugPresetEntriesPatchTests`.
Final run `artifacts/TestResults/20261108T084021615Z-34608-6c0eb02aecdd4946b2bd14ca74b3692f/`
executed/passed **26/26**, none skipped; TRX SHA256
`7CB15B4EC244BBD3CAA7279E28C322FE8FDDCF47EFDC9842D1C00F342444FDF8`.
Each following directory is under `artifacts/TestResults` and retains raw
`Megamod.Harmony.trx` and `Megamod.Harmony.dotnet-test.log`.

| Incremental behavior | Actual RED | Focused GREEN |
| --- | --- | --- |
| Absent/expired baseline reports once, native failure retained | `20261108T081638501Z-40796-ff5eb8b1f93d4ce1b70ca0495057c59a`, 2 failed: no warning | `20261108T081712335Z-23260-7570fe2c0a284d35bee5d8a473885ccf`, 2/2 |
| Immutable argument alias and labeled nops | `20261108T081747566Z-52532-00886e130f8c4ec6b9b02d2bbb74c1c8`, 1 failed: rejected benign stream | `20261108T081837103Z-51492-d3808e1d17b9442b91de6037652c438c`, 1/1 |
| Denied inner decision writer retains valid native copy | `20261108T082117915Z-42712-9caea5f05549484ea71a7a9ec54d7f0a`, 1 failed: quantity 0 instead of 12 | `20261108T082134950Z-47604-1be73625a6f64c30aa76fbb3d83a4031`, 1/1 |
| Inner skip of loop Count is unadmitted | `20261108T083054582Z-39892-2dae43c5fc9a4ddb8fd7fa4292104845`, 1 failed: admitted | `20261108T083114082Z-47616-cb10d89a906440e6a569f304c096f7a1`, 1/1 |
| Escaped created-policy local lacks receiver proof | `20261108T083316408Z-44784-9e7eabeb51234e2ea23a81d783bcce86`, 1 failed: admitted | `20261108T083334535Z-47360-1b4d84ab2f7d451ab84f6fc6e39218a2`, 1/1 |
| Branch bypassing policy creation returns unchanged supplied stream | `20261108T083805209Z-47528-d737ccb6fe884be3bccec4da84537d47`, 1 of 4 failed: transformed before later ordering rejection | `20261108T083839171Z-53120-a60d1a5f21fa4d8eba220821b649163b`, 4/4 |
| Repatch during capture cannot authorize an older body | `20261108T083909094Z-41928-5301d4fccd4244218193467de284a3cc`, 1 failed: omission instead of native failure | `20261108T083927861Z-54356-ff95b10e20ce4f598eecbb66d94d59cf`, 1/1 |

Additional regressions passed against existing behavior without an invented RED: six cases at
`20261108T083233008Z-51704-7e3064c6d9084b6290c4890ac13baa57` (callback/receipt exceptions,
failed warning sink, effective input/skip, later publication, owner collision); six at
`20261108T083455104Z-53588-6f3faa23fc664ae9935c89d1d2e5e772` (supplied-stream rejection and
inner skip/observation); remaining broad/all-seven-choice/postfix/factory/oversized-metadata and
capture-writer admission cases are included in the final 26. The native broad-permission fixture
initially used foreach; its separate nop transformer produced invalid EH IL in run
`20261108T083557567Z-33096-79dd4af504ec45eeb72d4455d34f71a4` (22 pass, 1 fail). Changing that
fixture to the native-shaped indexed loop fixed the test composition; this is a fixture correction,
not a claimed product RED. Final build output includes an unrelated CS8604 warning at
MessagePlacement/MessageDrawPatchTests.cs:71; no warning originates in the three frozen guard files.

Historical SHA256 submitted for the first review (superseded by the correction below):

- `Core/PresetEntriesRead.cs`: `5E7DC74F099D2188B3505911E005B099D168B3CB8C1DB8370C8E696707055CE5`.
- `Harmony/DrugPresetEntriesPatch.cs`: `EC8889E4A0FF95486C86FAE16E92512C06BDE7F761D5446A02D8EB85497E5640`.
- `tests/Megamod.Harmony/Features/DrugPolicyMaintenance/DrugPresetEntriesPatchTests.cs`:
  `B20A5C2BB4156DC93A68EDF726A876D6BB056958D3183A87AF3373052FF120AF`.

The reviewed reconciler, batch, native storage and native catalog remain unchanged. No central
registration, shared-helper edit, native baseline publication, game build/install/launch or commit
occurred in this guard tranche. Independent review and every native integration/acceptance step
remain open; no S029 gameplay task is closed.

### Review P2 correction: branch entry at the creation store — 2026-11-08

Independent review found that the earlier dominance check accepted a branch directly onto the
matched store. The regression first invokes a real foreign-transpiled factory which loads an
alternate policy and branches onto that store, preserving the matched creation instructions but
never executing them. It observes the alternate result and copied explicit choice before testing
owned admission. The prior implementation incorrectly admitted this incoming stream.

The first correction rejected branch/switch targets in the receiver/call/store chain and its nops,
then retained the existing loop-dominance proof. It neither rewrote the rejected incoming stream
nor removed the foreign owner. Eight actual owned-Harmony cases covered store substitution and
direct/switch entry at the receiver, call, store and labeled nops. They invoke the foreign path
before admission and again after rejection, checking its policy identity and native explicit copy.
Earlier alias/nop and compatible-composition cases still pass. No broader stack interpreter or
capture-rewriting guarantee was introduced. Re-review found that the receiver-entry cases wrongly
expected rejection despite executing the intact chain; the follow-up below supersedes that part.

- Exact RED filter:
  `FullyQualifiedName=Megamod.HarmonyTests.Features.DrugPolicyMaintenance.DrugPresetEntriesPatchTests.BranchOntoCreationStoreCannotSubstituteAnAlternatePolicy`.
- RED: `artifacts/TestResults/20261108T084933943Z-25936-79f1404b5db447acaf0899003ddce93b/`,
  **1 executed / 1 failed**, expected false admission but received true; TRX SHA256
  `D237BEA8A83C61CF0E45683CEB1A84C788F3912266DB14028CC99DB2AABEF634`.
- Minimum GREEN with the same filter:
  `artifacts/TestResults/20261108T085005176Z-43076-7e95be5e360247d6a2b59b94f5fc460d/`, **1/1**;
  TRX SHA256 `A499591713AA81327D464C4B8058B80668193D64086CDB63F43EEC14EC405782`.
- Affected fixture GREEN using the class filter above:
  `artifacts/TestResults/20261108T085125909Z-42740-fa7e8cbeb6ab4e1c94e91a2cb2b7b3a2/`,
  **34/34**, none skipped; TRX SHA256
  `8F34DC3C0C5FEFDC2C1E0093FB1C5DBBC194144B11C848B61CDEEEC70413B284`.

All three directories retain raw `Megamod.Harmony.trx` and `Megamod.Harmony.dotnet-test.log`.
The unrelated MessageDrawPatchTests.cs:71 CS8604 warning remains; no new warning originates here.
Historical SHA256 for the first P2 recheck (superseded below):

- `Core/PresetEntriesRead.cs`, unchanged:
  `5E7DC74F099D2188B3505911E005B099D168B3CB8C1DB8370C8E696707055CE5`.
- `Harmony/DrugPresetEntriesPatch.cs`:
  `3FA1CC54B82A4FA9EE250451132C6F536365422332E42057EC4038D69FF96979`.
- `tests/Megamod.Harmony/Features/DrugPolicyMaintenance/DrugPresetEntriesPatchTests.cs`:
  `A9459E2481ED8AE9CF8DE809C293BB3FBCDDCCD3EF1A31AFC4D11CED92AB5146`.

The first recheck closed the store bypass but found over-rejection at the receiver. This correction
changed no central integration or native baseline publication and supplied no native acceptance.

### P2 recheck correction: intact producer entry remains compatible — 2026-11-08

The protected interior now starts strictly after the receiver load and ends at the store. Branch
or switch entry at that load, or at a preceding nop, still executes the complete native creation
chain and is admitted. Entry at the call/store or nops between those instructions stays rejected.
The existing dominance proof remains unchanged. No additional metadata or lifecycle guard is added.

Four positive cases first execute the foreign branch/switch and observe native creation and the
explicit choice. After admission they insert an unavailable preset entry, invoke the real patched
factory, and observe its omission, the following native explicit choice and the original created
policy identity. This pairs the negative stack-substitution case with a compatible counterexample.

- Exact RED/GREEN filter:
  `FullyQualifiedName~Megamod.HarmonyTests.Features.DrugPolicyMaintenance.DrugPresetEntriesPatchTests.EntryAtReceiverOrPrecedingNopRetainsCompleteCreationAndGuard`.
- RED: `artifacts/TestResults/20261108T085805547Z-50852-5b8de32089194e1e89dc85050272e232/`,
  **4 executed / 4 failed**, expected true admission but received false; TRX SHA256
  `09822EB914BF405F9F8DCE5AC85B65906223FA2139683BDDD848CCB5044FA9D0`.
- Minimum GREEN: `artifacts/TestResults/20261108T085831367Z-53968-e1bc524ef6c54d179aaf62848c21cd96/`,
  **4/4**; TRX SHA256 `824D545DADAD59A67A4F8CBC5DF994D0B5F057C0027776D1ABEC4A485A6444DD`.
- Affected fixture GREEN with the class filter above:
  `artifacts/TestResults/20261108T085848118Z-47100-2e1d3d89380842e49cf65501f350a2f4/`,
  **36/36**, none skipped; TRX SHA256
  `88E29CC7E720C1394A43B8491FC15D0ED119ED901346B2835ABDEDF63109BE09`.

All directories retain raw TRX and dotnet-test logs. Current frozen SHA256 for recheck:

- `Core/PresetEntriesRead.cs`, unchanged:
  `5E7DC74F099D2188B3505911E005B099D168B3CB8C1DB8370C8E696707055CE5`.
- `Harmony/DrugPresetEntriesPatch.cs`:
  `28B59157EE31D18146B22016C484C4F7415B4249568DBCB1C43CD38CACF33CF8`.
- `tests/Megamod.Harmony/Features/DrugPolicyMaintenance/DrugPresetEntriesPatchTests.cs`:
  `40C2AC4FF08EC59F6A62508B80CE977796B175227A0909178362050C1172D508`.

Independent recheck is CLEAR: both producer-entry findings are closed. Root integrated the initial
native baseline publication with the one shared Def invalidator. Reviewed group
`20261108T104425075Z-09a7bfcbed474ee385e2073e8ef3f831` passed the 43-step initial preset and
55-step unchanged-catalog persistence workflows (98/98), with both cleanups passed. Root personally
inspected all 20 exact-process screenshots, including both native developer consoles. The validly
resolved Steel input is not a drug; following Beer/Smokeleaf choices and broad pleasure permission
survive. Native checkbox and pawn assignment edits/restoration operate on the constructor-created
policy. The persistence workflow saves the edited choice and assignment through the ordinary
command, reacquires policy/pawn identities in the replacement Game, visibly retains those values
after reopening native controls, and restores them through native controls. The source preset,
native underlying order and default policy remain unchanged; the catalog is unchanged throughout.

Independent audit matched all 145 deployed hashes, confirmed unchanged normal-configuration hashes,
unique-save removal and graceful exact-PID exit 0. The retained log contains no ERROR/exception and
25 classified warnings: 12 shutdown allocation, 10 inactive Workshop, two Gateway and one Vulkan.
The final shutdown allocation stack ends midline; a complete final log tail is not claimed. See
`artifacts/Implementation/megamod-p0/drug-native-preflight/persistence-verification.md` and S029
tasks 2.5.3–2.5.4 for the exact reviewed build, native actions and evidence.

Content-change reconciliation, post-preset normalization, dose history, save switching, successful
content reload and broader S029 lifecycle behavior remain open. Revised startup-warning/ordinary-
settings-off handling is also unverified; original-provider coexistence is not an acceptance gate. These native
workflows do not extend the target's documented foreign-rewrite limits or claim arbitrary
malformed-content recovery. Fixtures/getters remain supporting evidence; only the observed initial
preset and unchanged-catalog persistence workflows are accepted at this scope.
