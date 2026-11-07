# S102 — More power info research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Read-only research on 2026-11-05. No source mod was executed and no live compatibility is claimed.

## Documentation and identity

Attempted the [Steam page](https://steamcommunity.com/sharedfiles/filedetails/?id=2906529763) first;
the initial web fetch returned HTTP429 and its language variant was inaccessible. The
[author repository](https://github.com/angelolocritani/Rimworld-MorePowerInfo) README was readable
and describes total usage and current/storage-only autonomy. Source inspection followed that public
documentation. A later Steam retry succeeded: its description confirms those three functions,
and the author's December 2022 replies describe the addition of self-discharge and the absence of
historical battery-state tracking. The [change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/2906529763)
record the self-discharge addition on 2022-12-27, 1.5 update on 2024-03-14, 1.6 update/fix on
2025-06-11 and an auto-generated update on 2026-10-25. The last note alone does not explain its
changes; installed 1.7 routing and binary inspection establish the selected implementation.

- Title **More power info**, author **arl85**, package **arl85.MorePowerInfo**, Workshop **2906529763**.
- Installed root `F:/Steam/steamapps/workshop/content/294100/2906529763`.
- About declares 1.4, 1.5, 1.6 and 1.7, required Harmony and loadAfter Harmony. No LoadFolders.xml;
  native version selection uses **1.7 plus root**. No gameplay Defs, XML patches or settings files.
- Selected `1.7/Assemblies/MorePowerInfo.dll`, assembly/file version **1.3.0.0**, MVID
  `e3fde58c-3b7b-43df-b12b-52c2965c5bb7` (read with PEReader without loading the assembly).
- Public source revision **7a837a8b869bf72694e6b2e3745795e1df0565e5**. Its committed 1.7 DLL is
  byte-identical to the installed DLL. Inspected the complete source and decompiled two runtime types,
  assembly metadata and references. The public source matches the selected binary's behavior.
- No LICENSE/COPYING file or explicit reuse grant found in the inspected tree; assembly copyright
  is 2022 with no named holder. Retain attribution and independently implement; public visibility
  is not a reuse grant. No preview/image was selected for product use.

| Evidence | SHA-256 |
| --- | --- |
| Installed/committed 1.7 DLL | `7FE7C4AEAE3DC74693BB8C46FC4FB9887F1A01922129152303F37FB5B0623236` |
| Installed About.xml | `A33168AFA3EF20F7A1003F2901608A9F7F8C8D84473CBE7A7DA3D0E0D9F41C3B` |
| Installed 1.7 English keys | `B202CFDDAEA7561A86DF9AF1B97EF4960193BF3567643E46C83152F5FCCEC581` |
| Native 1.7.4998 rev1299 Assembly-CSharp.dll | `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |
| Core Buildings_Power.xml | `F9213D01681E0D9082447C75A3D31113BA8E6B52F7595EF951D29CA624D0AABA` |

Local ignored evidence: `artifacts/Refinement/megamod-spec-20261105/more-power-info/`, with
`upstream`, `binary` and `native` subdirectories. The operation registry exposes live
`gateway_decompile`, not offline decompilation; read-only ILSpy inspection was used without a game
launch or execution of foreign startup/patch code.

## Complete selected-source behavior

`ExtraPowerInfo.ExtraPowerInfoMod` is a StaticConstructorOnStartup class creating Harmony owner
`arl85.MorePowerInfo` and invoking unguarded PatchAll. Its single nested patch postfixes
`CompPower.CompInspectStringExtra`. A null PowerNet preserves the incoming result. Otherwise:

1. Read `CurrentStoredEnergy()`. Scan `powerComps`; only PowerOn entries contribute. Negative
   EnergyOutputPerTick becomes positive usage; positive output becomes production. Balance is their
   difference. Magnitudes below **0.00001 Wd/tick**, roughly **0.6 W**, are forced to zero.
2. If stored energy is positive, scan all batteries with StoredEnergy>0, adding
   `min(5 * WattsToWattDaysPerTick, StoredEnergy)` as self-discharge, including EMP-stunned ones.
3. Current battery flow is balance for a deficit, **balance/2** for a surplus, minus those losses.
   Another 0.00001 threshold applies. Nonnegative flow becomes “Forever”; otherwise stored energy
   divided by loss becomes an integer tick count. Zero stored energy always leaves zero ticks.
4. Storage-only ticks equal stored energy / (current usage + self-discharge). No explicit finite,
   overflow or zero-denominator guards precede integer conversion.
5. Production/usage/balance and stored energy are rounded F0. Tick counts use native period text.
   Three translated lines **replace the whole incoming base result**: production/usage/balance,
   storage/current autonomy, storage-only autonomy. Device subclasses can still prepend their own
   lines, but other contributions at the patched base boundary can be lost by ordering.

All four keys are English-only. No custom bottom tab, window, toggle, settings, data persistence,
Def type, public consumer protocol or cache exists. Internal `Utility.LogEveryNTicks` has an empty
body in the selected DLL; call arguments still construct strings. DEBUG source logs at
ticks divisible by 60, not a proper once-per-tick deduplication. Neither is desired product behavior.

## Native contracts and limitations

Inspected `CompPower`, `CompPowerTrader`, `CompPowerBattery`, `CompProperties_Battery` and `PowerNet`:

- `CompPower.PowerNet` resolves transNet or connectParent.transNet. Its inspection returns not
  connected or network balance/stored text. **1 W = 1.6666667e-5 Wd/tick** (60000 ticks/day);
  Wd is game watt-days, not real-world watt-hours.
- Trader EnergyOutputPerTick is actual PowerOutput times that conversion. EMP returns zero output.
  PowerNet sums only PowerOn traders. Off/browned-out installed demand is therefore not current
  consumption. A separate future-demand forecast would be a different feature.
- Battery CurrentStoredEnergy excludes EMP-stunned energy. Native self-discharge is **5 W per
  charged battery**, capped to remaining energy per tick, not “5 Wd per battery” as a source comment
  suggests. CompTick performs this even on a disconnected battery. Inspection also shows the
  selected battery's capacity, efficiency and loss; those lines must remain.
- AddEnergy applies each battery's actual efficiency. AmountCanAccept is remaining capacity divided
  by efficiency and is zero when broken/EMP-stunned. Distribution shares input among batteries and
  redistributes around capacity limits; a universal 0.5 multiplier is unjustified for modded Defs.
- After a redistribution pass, native DistributeEnergyAmongBatteries stops if remaining input is
  below **0.0005 Wd/tick**, about **30 W**. Removing a full battery from the candidate list can
  therefore discard a small surplus before a partially filled battery receives it. This differs
  from a small surplus directly shared when no capacity-limited pass is needed. Admit the actual
  branch, cutoff and observation ordering; do not credit input the native routine will discard.
- Native discharge has an important asymmetry: CurrentStoredEnergy excludes EMP batteries, while
  ChangeStoredEnergy's giving list checks StoredEnergy>1e-7 without that same exclusion. A mixed EMP
  network is not automatically an admitted precise forecast; do not repair its simulation here.
- PowerNetTick handles electricity-disabled conditions, restart reserve/intervals and load shedding.
  A depletion estimate is not a promised exact outage time. Debug unlimitedPower overrides net
  gain; the source's component sum misses that override. Both need explicit presentation states.
- A full battery can reject charging for one tick and accept again after self-discharge. Extrapolating
  that single clipped tick into a long-term drain gives a misleading forecast. The owned full-state
  presentation below avoids that assertion; partial-capacity estimates remain qualified snapshots.

Static comparators, not finalized active-mod values:

| Comparator | Inspected behavior/value | Consequence |
| --- | --- | --- |
| Core Battery | 600 Wd, 50% charge efficiency; native 5 W self-discharge | Standard goldens use these units; no balance change |
| Core wood/chemfuel generator | XML base output 1000 W; fuel-dependent operation | Use actual output, never promise fuel lasts as long as reserve estimate |
| Core solar generator | Source description says sunlight-dependent | Recalculate as actual output changes; no day/night forecast inferred |
| S052 Lights Out / S053 RePowered | Owned activity/rate policy, minimum standby and separate work-start forecast | Read resulting current demand once; do not add hypothetical pending work or reapply discounts |

## Proposed observable corrections

Retain contextual totals and both estimate meanings, but qualify them as **at current rates**.
Use actual native membership, units, efficiency and loss where admitted. Preserve native and foreign
inspection text additively. Show no usable reserve separately from sufficient current generation;
report a full reserve as **Full — at current output** rather than extrapolating saturation loss.
Expose unknown estimates as unavailable, not zero or infinite. Known totals remain available when
only the forecast is unsupported. A disabled electrical condition is not a healthy zero-load network.

No own settings or save state are necessary: selecting a device is contextual UI state. No tab,
gizmo, global toggle, new graph, future weather/load simulation or automatic power control is added.
Use common numeric/localization presentation and native lists; any shared projection belongs with
the existing owned power services, not a source-specific map scan/cache. Display does not reserve,
charge, drain, connect or switch anything.

## Consumers and co-installation

Bounded read-only `rg -a -i -l` scan of installed Workshop `.dll`, `.cs`, `.xml` for
`arl85.MorePowerInfo|ExtraPowerInfoMod|MorePowerInfo.Production` returned only this source's own
versions/About/keys. This ordinary string scan is not a complete UTF16/reflection/dependency proof;
no external consumer or required facade was established. Preserve no invented source API, package
identity or settings/save migration contract. Original active package suppresses owned overlap,
leaving the provider installed and untouched. Unknown power models require narrow admission;
native-compatible content is not rejected solely because another mod supplies it.

## Open questions / future evidence

- **MPI-Q1:** Finalize effective native inspection and numerical contract admission, including
  skipped base calls, compatible postfix/transpiler changes, optional losses/charging, EMP asymmetry,
  complete fallback and native same-tick update ordering. No whole-method hash admission.
- **MPI-Q2:** Finalize the bounded pure snapshot allocation/formatting, full/empty/zero/near-zero/
  overflow states, heterogeneous capacity/efficiency and common numeric service; measure shared
  network reads and UI cost before claiming an optimization.
- **MPI-Q3:** Focused TDD, seven-language inspection, owned power composition, original-present and
  changed-provider runs, independent review and real selection/switch/work/charge/discharge/load
  workflows on the reviewed build. All gameplay tasks remain open.

[Owned change](../../../../openspec/changes/specify-megamod-power-inspection/),
[coverage](../replacements/more-power-info.md), [sequential ledger](specification-progress.md).
