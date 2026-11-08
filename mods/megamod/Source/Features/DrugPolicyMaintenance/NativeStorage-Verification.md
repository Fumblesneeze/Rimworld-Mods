# S029 native data adapter verification

Owner: `fumblesneeze.megamod`, S029 / Drug Policy Fix. Status: standalone storage and catalog adapters
implemented and host-verified. Storage and catalog independent reviews are clear; native
integration/acceptance remain pending. This file records both 2026-11-08 tranches. It does not close
any gameplay task or authorize a native hook.

## Scope and exact native inputs

The adapter owns `DrugPolicy.entriesInt` membership and the seven mutable entry choices. It preserves
retained entry identity and `takeToInventoryTempBuffer`; it never calls `CopyFrom`, initializes a
policy through a getter, sorts, removes a Def, changes pawn dose history or saves automatically.
Capture detaches membership, drug handles and values. Restore reinstates the original nullable list,
original row order and original entry values, including null/duplicate/invalid rows on failure.
`DrugPolicyBatch` remains the sole batch/rollback authority. Future native callers must establish
the field shapes and valid game/catalog boundary before using this storage; this tranche installs
no patch and supplies no loaded-provider or current-catalog authority.

Research inputs retained under `artifacts/Megamod/P0-C/native-1.7.5000` are read-only decompilations
of installed 1.7.5000 rev1275, assembly SHA256
`D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`:

| Native type | Retained decompilation SHA256 |
| --- | --- |
| `RimWorld.DrugPolicy` | `828D0139C538C453205515C1F4851A4A32C1B8CBADA23E0E036EDB0FD7894B6E` |
| `RimWorld.DrugPolicyEntry` | `7543CD230EDA8981B0CF27FA723EFD49054A2751FA98AA1A2694F4F37A279B9A` |
| `RimWorld.DrugPolicyDatabase` | `944DF23088D5E4ABA3C3D7F7F93A975F71BBFBCF8CB51AE639D58C71DE9740D2` |

These hashes identify evidence, not runtime admission predicates. Existing owning design records
the early constructor/preset/post-load ordering gaps. No upstream implementation was copied.

## Focused host evidence

Typed operation: `test_run`, suite `Megamod.Defs`, configuration `Release`. Each RED used
`FullyQualifiedName=Megamod.Defs.Tests.DrugPolicyMaintenance.NativeDrugPolicyStorageTests.<method>`;
each GREEN selected that class using `FullyQualifiedName~`. The first GREEN selected its one exact
method. All tests ran, with no skipped cases. Each directory below is beneath `artifacts/TestResults`
and contains `Megamod.Defs.trx` and `Megamod.Defs.dotnet-test.log`. The tool returned a generic error
for expected failing runs; the retained logs/TRX establish their actual assertion/stub failures.

| Behavior / method | RED: one executed and failed | GREEN: executed/passed |
| --- | --- | --- |
| `CleanupRetainsTheFirstNativeEntryWithEveryChoiceAndItsEditBuffer` | `20261108T020543384Z-49536-bc54d379ca754958b476ab6bb1dbedb7`, `Capture` stub throws | `20261108T020633001Z-43084-fb70405e2fad495dbb5bc7c55fe7e971`, 1/1 |
| `NormalizationWritesOnlyTheInapplicablePermissionsOnTheRetainedEntry` | `20261108T020658471Z-52804-22902478a96b4a71bbbeda90f55d821b`, both permissions remained true | `20261108T020719098Z-37080-9b04ba443f2c436a97a84e5e913c7107`, 2/2 |
| `RestoreUsesDetachedChoicesAndOriginalListAndEntryIdentities` | `20261108T020748839Z-51492-6879ff004d7447318e9d50c5f127b362`, `Restore` stub throws | `20261108T020815856Z-50844-d09998d009994db18e13c61ecec33389`, 3/3 |
| `MissingRowsUseNativeDefaultsAndTheSingleCallerSuppliedNativeOrder` | `20261108T020906113Z-47456-109d012212734d7db763a2446e7a07dd`, absent new native entry causes null dereference | `20261108T020923875Z-46692-461299803ac94233b2efaa86ee5efc7e`, 4/4 |

The final run `20261108T021025843Z-22460-1c16eb175a254dd0a3914d62fc507334` passed **6/6**. Added
regressions `NullStorageMaterializesValidEmptyMembershipEvenWhenThePlanIsUnchanged` and
`BatchRestoresThePartiallyWrittenCurrentAndEarlierNativeStorage` passed against the existing
implementation, without claiming a new RED. The latter delegates to the real adapter, observes its
written current target, then deliberately throws at the storage boundary; the existing batch must
restore both native policies, including the current target's original null list.

All intended lightweight `ThingDef` fixtures are registered in an initially empty scoped database
and cleared in guaranteed cleanup. No XML, loaded Core/Workshop content, Harmony or active-mod
mutation is involved. Catalog facts are explicit inputs: these tests do not establish a native
catalog builder. The ordering case invokes actual `Verse.GenCollection.SortBy` with constructed
`CompProperties_Drug.listOrder`; the adapter consumes the resulting plan without sorting again.

Frozen source SHA256: `DCC407D9A21BA2E9A136C87F82D41B24348CC4FC57C4A000CE75E3DE569A293B`.
Frozen test SHA256: `911AF3448B7B727ACA4838DEA625776DC5FC51ECA3E5BE6659189AB465D0C96D`.

| Evidence | TRX SHA256 |
| --- | --- |
| Cleanup RED / GREEN | `80B35C18CA2C33B714668E38D69A3ECE92B822E1F2A2DCEDF3CC132638AE553F` / `E397E79FDCA57EFA4EDAB668825A0F113C65B2BF06B2A2BB7DEE45C69EFBEC71` |
| Normalization RED / GREEN | `20DD91E3E422BDC3AD48D28D57437BC2EFC279BC5CC9AAD2E4EBAD7C0B22F523` / `04E1FCE527F703D43B5A67694E9F3C7DA006E8C1FE7761E3F30CCB639DABB22F` |
| Restore RED / GREEN | `DCA64A03B8A5E18ADFFAEFE8D303A88E86AB0915E98AAD77ED47E563E70FE3EF` / `FD22A791432CBAEFE48BD2004513E5D82BCB9B50A7ED7C3637631E1619EA3EAD` |
| Additions RED / GREEN | `859FB508EFD3ED23D125A6E4028C4E93D40E9F25CE77CE5A34C62F2314D7D8BC` / `EE806E7EA36A5521A3CA2FB8CB0B15F73D30EEC11B789643E82615AB10D691D9` |
| Final 6/6 | `115EC3E2692E6C4F00467F208156A517396F4CDFFD23BB0B43DFBD5A572D9B9E` |

## Native catalog tranche

Scope: `Runtime/NativeDrugCatalog.cs` and
`tests/Megamod.Defs/DrugPolicyMaintenance/NativeDrugCatalogTests.cs`. Root owns the project source
link; the reviewed storage source remains unchanged at
`DCC407D9A21BA2E9A136C87F82D41B24348CC4FC57C4A000CE75E3DE569A293B`.

The installed assembly hash above was reconfirmed before implementation. Read-only ILSpy stdout
for `Verse.ThingDef` and `RimWorld.CompProperties_Drug` established these exact consumed facts:

- `IsDrug`: ingestible is nonnull and its drugCategory differs from None.
- `IsPleasureDrug`: IsDrug and ingestible.joy greater than zero.
- `IsAddictiveDrug`: first `GetCompProperties<CompProperties_Drug>()`, when present, has addictiveness
  greater than zero. GetCompProperties iterates comps.Count, so null comps would otherwise throw.
- `CompProperties_Drug.listOrder` is a float. The retained DrugPolicy decompilation above uses it in
  native SortBy; capture neither sorts nor invents a value range or tie-break.

`Capture` requires a caller-supplied finalized availability delegate and enumerates registered
ThingDefs only. Item/drug classification precedes that predicate; excluded candidates are skipped
before comp/addiction/joy access. An available Item/drug missing a usable native drug comp rejects
the entire capture with a diagnostic naming that ThingDef. There is no partial returned catalog,
silent removal, fallback availability, native field mutation, logging or provider/lifecycle lookup.
The future caller must establish the exact stable generation and report the unsupported maintenance
attempt through the protected bounded sink. This helper does not claim successful loading.

The same typed suite/configuration were used with exact method filters under
`Megamod.Defs.Tests.DrugPolicyMaintenance.NativeDrugCatalogTests`; GREEN runs selected only that
class, not the reviewed storage suite. Results are beneath `artifacts/TestResults` with the same
TRX/log filenames as above. The four successive behavior changes had actual RED before GREEN:

| Behavior | RED directory and executed failures | GREEN directory and passed/executed |
| --- | --- | --- |
| Registered Item/drug identity and native flags | `20261108T022047992Z-51016-321d18fa49e04deca35274ee0f740ff6`, 1/1, Capture stub | `20261108T022110074Z-37976-8d089d0aa9b54afdb66eea171c797739`, 1/1 |
| Finalized exclusion before comp access | `20261108T022132734Z-50080-23f8e51a754f43d79bbfa57aaa040e08`, 1/1, native null-comp access | `20261108T022150117Z-51892-624024b79dbe4581bfef1e2252286709`, 2/2 |
| Whole-capture rejection for available malformed comp | `20261108T022222685Z-19176-237332d25a614e8b9d3556d25f3ed46e`, 2/2, missing comp silently accepted / null comps generic exception | `20261108T022245831Z-52892-105cded62bf44918a092ed2ef2212d36`, 4/4 |
| Mandatory caller availability even with empty DB | `20261108T022259168Z-42948-91dedeb6806c46439adc6ce5f3132eb4`, 1/1, null callback accepted | `20261108T022314600Z-51940-f89b852175924ea0aa890b061a01edde`, 5/5 |

Final catalog-only run `20261108T070638068Z-32092-1bd0e060e57e4f8497189a72d29f6f87` passed **8/8**,
none skipped. Three additional regressions passed without production changes or a claimed new RED:
replacement registered Def with the same name retains its new reference identity, snapshots retain
native enumeration order and detached property flags, and caller failure propagates without a
partial catalog. The stale Def fixture was registered in an earlier exclusively owned DB scope;
it is retained only as an old policy handle after that scope closes. Every current intended fixture
is registered and every scope clears in guaranteed cleanup. No loaded-mod/stock/research fixture is
invented; this remains native-data host evidence, not XML loading or player acceptance.

Frozen catalog source SHA256: `D6969B213B0BD73F7A8D91B403CAE2FEB9F14C3DD30844365E882D3B549D0A34`.
Frozen catalog test SHA256: `BF2E45084CB1573089333496462B60E2FB2761A806B3461F86A0F63A5C7EBEFD`.

| Evidence | TRX SHA256 |
| --- | --- |
| Classification RED / GREEN | `5B7BE44EED8822B65D75D98E72730A379B49207CF7DD88061D27AF4A0221C845` / `1A24E355144F9F4519C25434498F1F9D2D30ACFCE0C34FBA96BF03C4AE0FB4ED` |
| Availability RED / GREEN | `C5CB0EE55E6825D4AC3AEE282AA17A104BCA2102C56DAF6B908B42F5D4BB4C53` / `CAAF47565A07587F1E2F14508D6141CEC44855878CDB9BE985E89D7CEFFC0417` |
| Unsupported comp RED / GREEN | `9415EB1B9DF92313DC8E0261330F60312BDD0AE01064DDBE89115CF0F5501FFA` / `0B96C52065A2A57F0200E892A2B3188B747E6261236A651B0E20ABCD83B6E6FF` |
| Mandatory caller RED / GREEN | `7A04A8B7655EDD863A1437EB6634F11A3653EEF1763DCD9EC2D0B83921AB603F` / `E61772102106744D3482644B4E24C28BEDC3C6AC3A4F68D9191B2881C3F54378` |
| Final 8/8 | `5D9450C972F47517D74157799882AAE2CDA72EB8CEEBC1FC1FCD25D56F44A94C` |

## Next native workflow — not yet executable or accepted

The next integration must supply finalized availability to the current native drug catalog, early
init/preset/post-load target admission and original-provider suppression. It must not infer success from these host tests or
substitute a late idle pump for recovery before native unsafe lookup/sort. Active canonical
`Mlie.DrugPolicyFix` suppresses owned maintenance and presentation only; root owns the active-package
snapshot. Never inspect installed folders as activation or unpatch the original. Original-present
acceptance requires a separate isolated run; there is no active-mod fixture in this Defs suite.

Planned narrow native scenario `megamod.drugs.native-load-repair`:

1. In a disposable exact-mod colony, open an ordinary policy through the native UI, capture its
   baseline, then close the dialog. Arrange bounded disposable preconditions: conflicting Beer rows,
   a null row, an invalid Steel row, and a missing eligible drug. Record policy/entry identities and
   choices diagnostically, then save through typed `SaveLoad`. Do not draw the malformed policy
   before the load repair has run.
2. Reload through the native save workflow. Open **Assign → Manage drug policies** through the
   actual native header button (`PawnColumnWorker_DrugPolicy.DoHeader`). Capture the displayed
   repaired rows and compare winner choices and new-row defaults. Supporting reads may correlate
   identity/order; they do not replace observing the native dialog and its controls.
3. Edit a valid row using the real checkbox/numeric control, capture the change, save and reload,
   reopen the same native window, and capture persistence. Retain every causal action/result and
   configuration/build identity. Separate later scenarios must exercise policy copy/default/assign
   and actual scheduled drug taking/refill, including native priority; this tranche proves none.
4. Restore disposable camera/selection/speed/dev state and request graceful process cleanup. Never
   touch normal Config or retain credentials. Root owns launch/build and visual acceptance.

Existing typed window capture/click and SaveLoad are suitable once the native entry point can be
opened. Read-only tool inspection found no typed main-tab selection action; the owning Gateway
extension must expose native Assign-tab opening (or an evidenced existing native input path) before
the executable scenario is authored. Directly constructing `Dialog_ManageDrugPolicies` is not an
acceptable substitute. No out-of-lease helper, Gateway, project or registration was changed here.
