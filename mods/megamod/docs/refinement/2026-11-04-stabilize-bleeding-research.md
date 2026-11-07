# S080 — Stabilize Bleeding research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Origin replacement, P2-09/S080. Inspected2026-11-04, read-only documentation then binary/XML/native
inspection. No mod code executed, no game launched, no build/install/normal-config/Workshop mutation.
This is refinement evidence, not gameplay acceptance.

## Identity and public documentation

[Workshop3521178066](https://steamcommunity.com/sharedfiles/filedetails/?id=3521178066), title
**Stabilize Bleeding**, installed package **Defi.StabilizeBleeding**, author **Defi**; public author
display Captain Shitface. Public page labels1.6, posted9July2025, updated20July2025. It describes
in-place emergency tending focused on bleeding, greatest bleeding first, with no medicine fetching.
An author clarification says drafted and undrafted use and queuing distinguish it from ordinary
drafted tending. It identifies the earlier Stabilize mod as inspiration, not the same implementation.
Generic scraped removed/incompatible banners do not establish actual availability. Comments about
speed, bed departure and dumb-labor restriction are leads, not reproduced defects.

Installed root `F:/Steam/steamapps/workshop/content/294100/3521178066/` contains About, published ID,
preview, one DLL, one JobDef and English keyed XML. No source, README, license, LoadFolders or manifest
in that bounded file inventory. About advertises1.6 only, no declared dependency or DLC. No claim
that this assembly is runnable on1.7. No reuse permission inferred; independent engineering only.

| Input | SHA256 |
| --- | --- |
| `Assemblies/Stabilize.dll` | `1A32A748F7873638B68BE115D44229395FB79CD5F6C238CC4A8AEC9119439616` |
| `About/About.xml` | `ED6CE7F08B0829FF47BB61603BCA8F144EE80896FE04092CAECF80260E4B33BE` |
| `Defs/JobsDef/Jobs_StabilizeHere.xml` | `BC99C645B740741684CBFA49AB78BA325D224E64A04D5949B9BE3678ED60F03D` |
| Current `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll` | `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |

Assembly/file version1.17.0.0, net48, assembly nameStabilize. Title/product ClosableVents and
Linkolas2017 copyright are inherited-looking assembly metadata, not proof of provenance/license.
All four behavior classes were read from ILSpy project output at
`artifacts/Refinement/megamod-spec-20261104/stabilize-bleeding/source/`.
The registry offered live `gateway_decompile`, not offline inspection; local ILSpy did not load or
execute foreign code. Decompiling against current references is not running a1.6 compatibility test.

## Complete selected source inventory

| Source | Inspected facts |
| --- | --- |
| `Stabilize.Base` | Empty Mod constructor; no settings or startup patches |
| `FloatMenuOptionProvider_Stabilize` | Native provider, drafted+undrafted, single selection, self-target; rejects animal doctor; permitted mutant canTend bypasses skill check, otherwise Medical>0 |
| Provider target/menu | Requires a bleeding `Hediff_Injury`; downed any-faction target, otherwise non-hostile colonist/lodger/prisoner/slave/playeranimal/medically-entitled colony subhuman; Doctor-incapacity, no-path, aggression exceptScaria, selfTend disabled explanations |
| Provider action | Creates `StabilizeHere`, TargetApatient, count1, draftedTendtrue for both drafted states, native TryTakeOrderedJob and prioritized-task decoration; no medicine target |
| `JobDriver_StabilizeHere` | Reserve patient; path mode selfOnCell/bedInteractionCell/otherClosestTouch set in Notify_Starting; no ExposeData override |
| Driver loop | Fail despawn/null/forbidden/aggression; finish when no bleeding hediff; upright hostile fails; go→wait→native FinalizeTend→go |
| Driver wait | integer600/MedicalTendSpeed; self Wait, other draftedTend WaitWith with posture retained and sleep not retained; progress and tend sound; activeMedicine skill; immediate reach check; upright mental/hostile failure |
| Wait cleanup | Ends current patient Wait or Wait_MaintainPosture regardless of which action created it; cannot establish ownership of a replacement order |
| `StabilizeDefOf` | Public static field `StabilizeHere` and native DefOf initialization |
| Job XML | Unprefixed defNameStabilizeHere, internal driver type, report stabilizingTargetA, casualInterruptiblefalse; no explicit suspendability override |
| English keys | `StabilizeHere` and `CannotStabilize`; no source options, sliders, tab, gizmo, custom cache or save component |

The driver predicate accepts any `hediff.Bleeding` after its injury test; the menu accepts only an
injury. Neither tests `TendableNow`, NoCare or zero-progress when deciding whether bleeding remains.
No source list filtering is passed into native finalization. The property MedicineUsed exists but
the created job has no TargetB. No private patched medical helper exists in this assembly.

## Current native1.7 evidence and implications

Fresh native extracts under `artifacts/Refinement/megamod-spec-20261104/stabilize-bleeding/native/`:
`RimWorld.Toils_Tend`, `TendUtility`, `FloatMenuMakerMap`, `FloatMenuOptionProvider`,
`FloatMenuOptionProvider_DraftedTend`, `FloatMenuUtility`, `Verse.Hediff`, `Hediff_Injury`,
`Hediff_MissingPart`, `JobDef`, `Verse.AI.JobDriver`, `Toils_General`.
Same-binary earlier extract: `artifacts/Refinement/megamod-spec-20261104/smart-medicine/native/RimWorld.JobDriver_TendPatient.decompiled.cs`.
Core `Data/Core/Defs/JobDefs/Jobs_Work.xml` contains nativeTendPatient; `Jobs_Misc.xml` was not the
right file. A guessed provider typeTend was absent; ILSpy identified actualDraftedTend, which was read.

- Native provider defaults require no manipulation and exclude mechanoids; DraftedTend explicitly
  requires manipulation and permits mechanoids. Source inherits the weaker manipulation default.
  Provider base filters fog, despawn and mutant provider whitelist. `FloatMenuMakerMap.Init` discovers
  concrete `FloatMenuOptionProvider` subclasses and constructs them through Activator. Its menu path
  dispatches provider options to actual clicked Things/pawns; common actor gating rejects wrong map,
  downed/deathresting actors and lord-disallowed control. Effective startup/whitelist/shape admission
  remains work; a registration Harmony patch is not required by the inspected static path.
- Native DraftedTend uses the same basic patient populations, Medical work restriction, selfTend and
  aggression/Scaria menu conditions, then inventory-only medicine selection or explicit no-medicine.
  Its job checks source-independent native eligibility; a source-like arbitrary copied check is
  not complete proof. No source holding-platform/entity tending path exists; not selected here.
- Native FinalizeTend awards Medical XP before DoTend: animal175 or other500, times medicine factor
  or0.5 unmedicated. DoTend can early-return if no tendable conditions, so a caller must avoid empty
  finalization to avoid unearned credit. Native skill learning modifiers still apply.
- DoTend selects native optimal candidates using allTendableNow conditions and normal priority,
  then applies each selectedTended and once-only counters/notices/bond chance/recruitment, medicine
  consumption when present, Ideology role effects and quest signal. This source passes no filter.
- Native priority max(lifeThreatening1, BleedRate×1.5, certain severity-reducing treatments0.025),
  then descendingseverity. Thus current native all-condition tending can treat a life-threatening
  non-bleeding condition before a bleeding wound with rate0.1 (priority0.15). This proves a gap
  against a direct source-like1.7 port, not a claimed observed1.6 playthrough.
- Native candidate helper permits caller-supplied order, same-def tendAllAtOnce, and otherwise
  multi-injury severity≤20 batching only when medicine is used. Source's no-medicine finalizer
  does not turn the all-condition pool into bleeding-only candidates.
- Native no-medicine potency0.3,maxquality0.7; doctorquality×potency + currentbedoffset, self×0.7,
  clamp0..max. Native work600/speed with truncation. No source speed buff, instant cure or surgery.
- Injury bleed rate becomes0 after tending/permanence/age or inability to bleed; missing-part wounds
  are another hediff class, tendable while fresh non-solid extremity. Missing-partTended sets
  IsFreshfalse, not limb regeneration. Menu's injury-only gate misses a lone severed-part wound.
- Toils_General.WaitWith invokes ForceWait. A generic current-job-name check cannot prove ownership
  at cleanup. Source path mode is not serialized; nativeTendPatient scribes it, baseJobDriver does not.
  Save-position/lifecycle and unrelated-wait hazards need actual native regression workflows.
- Native JobDef defaults suspendabletrue and casualInterruptibletrue; source sets only the latter
  false. Native TendPatient sets bothfalse. This is a source difference, not copied save behavior.
  No dumb-labor filter was found in the source/provider/JobDef paths read; comment alone is not proof
  of its cause. Owned medical capability and a non-dumb-labor regression are explicit.

## Selected deviations and direct composition

1. Implement actual bleeding-only selection by current bleed contribution, not an all-condition
   native call guarded only by whether some bleeding exists. Include fresh missing parts and
   supported mod conditions; no untendable or ineffective-repeat loop.
2. Retain source actual wider patient scope and Medical1 floor as explicit draft defaults. Correct
   manipulation and current care/self permission checks; no Scaria exception that the driver rejects.
3. S062 individual NoCare remains authoritative, including for this explicit command. This differs
   from source and native player-forced treatment that can bypass ordinary care advice. The label
   explicitly says withoutmedicine, other care tiers are ceilings and are not rewritten.
4. Preserve ordinary unmedicated work/quality/effects and genuine health progression. Do not convert
   this into CE temporary stabilization or add a bonus survival guarantee.
5. Own temporary waits and serialized work; do not blindly cancel whatever wait a patient now has,
   interrupt bed recovery as cleanup, replay completed XP or migrate foreign jobs.
6. DirectS007/S024/S042/S058/S062 composition; no additional patient/Thing cache or medicine query.
   S124/S134 later research remains; no asserted conflict or guessed precedence between their drugs.

## UI, settings and external contracts

Native right-click order and job progress/report only; these are ordinary patient gameplay actions.
No bottom tab/debug/corner toggle/settings exist to copy. No new options proposed, hence no exception
to the common settings machinery is needed. If later policy becomes adjustable, amend the spec and
use the common global/save/Useglobal path. All seven languages required; logs/internal IDs invariant.

Source shared identifiers are not a drop-in contract by themselves. Bounded installed `.cs/.xml/
.json/.md` search for packageID/provider/driver/DefOf/JobDef names found only selected source and
the distinct older Stabilize item2023407836 (`Linkolas.Stabilize`, advertised1.1–1.5).
Completed ASCII `.dll` symbol search returned the selected DLL, older Stabilize versions1.1–1.5,
and `3642977893/1.6/CombatExtended/Assemblies/CombatExtended.dll`. That latter path is nested in
Bill Doors' Framework - 1.6 Temporary, `3HSTltd.Framework`, not proof of an active CE installation.
Its extracted `CombatExtended.FloatMenuOptionProvider_Stabilize` is its own same-named provider,
calling `Stabilization_Utility.Stabilize`, not an incoming `Stabilize.*` binding in that class.
Read-only extract: `artifacts/Refinement/megamod-spec-20261104/stabilize-bleeding/consumer-ce/`.
Neither text nor symbol scans prove absence of computed/reflected consumers. No public helper calls,
source settings or custom serialization keys were found in the selected four classes. Persisted
native jobs reference its unprefixed JobDef/driver; do not alias them automatically.

Source active => owned overlap inactive, bounded startup explanation, provider untouched. Older
Stabilize names collide in provenance but are not proof it consumes this implementation; any claimed
co-installation/migration needs separate actual provider evidence. No copied assembly/namespace/package
identity and no inherited broad license assertion. New owned static JobDef uses its own prefix;
no XML list replacement is proposed. Actual patches require executable documented target checks.

## Deferred work and status

**SB-Q1** explicit policy/balance decisions above; **SB-Q2** real selected-set effect seam, provider
discovery, effective-chain admission, wait ownership, no-progress and save lifecycle; **SB-Q3** actual
compiled/reflected consumers and supported providers/S124/S134 composition; **SB-Q4** honest tests,
seven-language package/UI and final native acceptance. These are research/implementation gates,
not requests that the user explain existing mods.

All observable scenarios are draft only. Compare ordinary unmedicated Tend, self-tend and medicated
Tend for final work/quality/blood-loss playability; no finalized/live balance measurement has occurred.
See [coverage](../replacements/stabilize-bleeding.md) and
[change](../../../../openspec/changes/specify-megamod-stabilize-bleeding/).
