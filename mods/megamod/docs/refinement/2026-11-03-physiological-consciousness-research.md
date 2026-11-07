# S046 — Consciousness Patch research

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Inspected **2026-11-03**, origin replacement, handwritten **P1-38**. Source/native analysis for an
observable draft; no implementation, live defect reproduction or compatibility acceptance.

## Documentation first, exact source and installed reference

The author's [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=2913127696)
says breathing, blood pumping and blood filtration above 100% can improve consciousness, with a mod
menu. It links [published source](https://github.com/Vexthecollector/Consciousness-Patch); comments
describe past settings-menu API failures, but these do not prove a defect in this inspected revision.
Cached generic Workshop status banners are not beta compatibility evidence.

Installed root `F:/Steam/steamapps/workshop/content/294100/2913127696`: About/Manifest/PublishedFileId/
preview plus 1.5 and 1.6 DLL/PDB pairs. Package **`Vex.Consciousness`**, author **Vex**, Manifest **1.2**,
supports **1.4–1.6**, Harmony required. No 1.7 declaration/explicit folder, LoadFolders, XML patches,
Defs, installed source or installed language catalog. The latest available **1.6 binary is the analysis
reference**; its actual beta folder selection/operation is not assumed. Assembly/file version 1.0.0.0,
Debug, informational `1.0.0+a445e47cd55c6e386dfc228cf54d7b28b3ae832a`.

Published source was shallow-cloned read-only into ignored evidence
`artifacts/Refinement/megamod-spec-20261103/consciousness-patch/source`, HEAD
**`a445e47cd55c6e386dfc228cf54d7b28b3ae832a`**, matching the installed informational revision. Complete
three source files/four classes and English Keys.xml were read. Selected binary was independently
decompiled with ILSpy 11.2.0.10050 into sibling `compiled`; its four classes agree with source behavior.
Published source has an **MIT license, copyright 2023 Vexthecollector**; preserve notices if reused.
No upstream code, DLL, localization or art has been copied into the product.

| File | SHA-256 |
| --- | --- |
| Installed 1.6 DLL | `D85E8B794EF7A7A77178B0B5D8F12473EA16A27E15CDCA5F88A1FAC60BCF641E` |
| Installed About | `4837941D54402B2DF01DB9E2D1DD9A51D27804CC866D425189F02FC66171C368` |
| Source TranspilerConsciousnessPatch.cs | `71B55D4B19CB5444E13F3CA4F4FCAD9A6A2714EEFC3C5A537F6A67350370C60B` |
| Source ConsciousnessSettings.cs | `91B871F4111F4724C46F4C3FF66D1B3C2E7691302C791D165945C4AB2894C603` |
| Published English Keys.xml | `78EAA53261EA38E529B4ABBCCBAEB3E589E78ECE3FFFDB3B139A5685660005ED` |
| Inspected native assembly | `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |

## Complete source behavior and settings

One unguarded static-startup PatchAll owner, **`com.consciousness.patch`**, installs one transpiler on
`PawnCapacityWorker_Consciousness.CalculateCapacityLevel`. It changes the three physiological input
limits and interpolation weights; it does not replace brain efficiency, pain calculation, health
conditions, the outer capacity calculation or native death/downing rules. Scope is every pawn using
that native worker, with no faction/race filter or creation of new capacities/organs.

| Persisted setting | Source default | UI / effective meaning |
| --- | --- | --- |
| `setCap` | false | Checkbox controlling a common maximum for each of the three **input capacities** |
| `maxCap` | .3 | Slider .1–1 displayed ×1000: 100–1000%, default 300%; intended input limit = raw×10 |
| `percentageModifierBP` | .2 | Blood pumping interpolation weight, slider .1–1 displayed 10–100% |
| `percentageModifierBR` | .2 | Breathing interpolation weight, slider .1–1 displayed 10–100% |
| `percentageModifierBF` | .1 | Blood filtration interpolation weight, slider .1–1 displayed 10–100% |

`mCMultiplier=1000` and `pMMultiplier=100` are display/conversion fields, not serialized settings.
The cap value remains editable when the checkbox is off. Global ModSettings only; no save override,
own pawn state or per-pawn controls. The settings page writes configuration during drawing. The
transpiler embeds current numbers at patch construction, so ordinary setting edits do **not** change
already generated method code; no own repatch/invalidation hook exists. Normal startup reapplies them.

Published English help explicitly describes each input cap and 150% input at 20% weight producing a
10% relative increase. However, installed files contain none of those ConPat_* translations. The
code calls translation keys; actual missing-key presentation is not live-tested. The owned UI needs
all seven baseline languages and accurate explanations rather than copying missing keys or per-frame
settings writes. No bottom tab, debug action, operational toggle or gameplay window is supplied.

### Exact transformation and static failure risks

The scanner finds three occurrences of `ldc.r4 1` followed by **any Call**, assuming subsequent fixed
positions contain Min and weight. It never verifies capacity field, call operand, interpolation/data
flow, uniqueness or bounds; missing matches retain index zero. It Nops each presumed cap constant,
replaces presumed weight instructions, and edits the three paths in reverse order.

- Default **cap off** removes the following Min call, leaving the actual input uncapped.
- **Cap on** Nops the old constant and replaces the **Min call** with the new cap constant. It does
  not restore the old constant's position or preserve the Min operation. For the inspected native
  sequence this leaves an extra value on the evaluation stack and changes the arithmetic shape;
  valid cap behavior is not implemented by that path. This is a source/IL finding, not a reproduced
  game exception or an assertion about every old runtime/compiler shape.
- Replacement CodeInstructions do not preserve removed labels/exception metadata. The scanner can
  select unrelated instructions or run beyond the stream. Startup uses no atomic semantic checks,
  logs a success message if the transpiler reaches its end, and provides no safe original fallback
  when discovery or emission fails. That message is not compatibility evidence.

Correcting the cap path is required to implement the **documented intended input ceiling**. The owned
spec must not preserve malformed IL as a contract, nor mislabel the ceiling as final consciousness.

## Exact native 1.7 math and composition

Native **1.7.4995 rev309** C# evidence is already in sibling S045
`artifacts/Refinement/megamod-spec-20261103/consciously-malnourished/native`. Exact worker IL was also
read under `consciousness-patch/native-il/RimWorld.PawnCapacityWorker_Consciousness.il`.

The worker obtains brain/ConsciousnessSource efficiency and subtracts native pain loss. Then it
successively interpolates current C toward C×min(input,1): BloodPumping with .2, Breathing with .2,
BloodFiltration with .1. The corresponding input field, CalculateCapacityAndRecord call, scalar
constant, Mathf.Min, multiplication and Mathf.Lerp are visible together in native IL. The source's
default removes only each input's 100% upper limit and preserves native weights.

For each supported finite input X and weight w, the intended contribution multiplies current C by
**1 + w×(X−1)**, using min(X,K) when the input limit is enabled. The three factors multiply; they are
not three additive percentage-point bonuses. Weights also affect losses below 100%. At ordinary
weights, inputs below/equal 100% behave as vanilla; adjusting weights changes both directions.

After a **positive worker result**, **PawnCapacityUtility** adds ordinary hediff/gene offsets,
multiplies their factors and applies the lowest condition/gene maximum. A zero/negative worker result
skips those modifier steps; positive offsets cannot rescue it through that branch. The capacity
minimum and rounding to hundredths follow regardless.
Thus S045 Malnutrition ceilings occur **after** physiological contributions. They cap boosted capacity
at .95/.90/.80/.70 in the early stages and retain native .10 extreme. Other native maxima, anesthesia,
brain/pain penalties and death/downing rules remain effective. The feature doesn't upgrade organs,
change item/research costs or make missing vital functions safe.

### Static comparative examples

Assume brain/pain baseline C=100%, no later offsets/factors/ceilings and default weights unless stated:

| Inputs / policy | Native worker | Intended owned worker before final rounding | Meaning |
| --- | --- | --- | --- |
| All three 100% | 100% | 100% | Ordinary baseline unchanged |
| Pumping150%, others100% | 100% | 110% | A 50% pumping surplus contributes a 10% relative gain |
| Filtration150%, others100% | 100% | 105% | Filtration's default weight is half pumping/breathing |
| All three200% | 100% | 158.4% | 1.2×1.2×1.1; final native display rounds to 158% |
| Pumping50%, others100% | 90% | 90% | Native impairment remains with default weight |
| Pumping50% with weight40%, others100% | 90% | 80% | Higher weight also strengthens impairment |
| Pumping400%, others100%, input limit300% | 100% | 140% | Intended input-limit result; source cap-on path is defective |
| All three200% plus S045 trivial malnutrition | Native worker100%, ordinary offset gives95% | Worker158.4%, S045 final ceiling gives95% | Benefits never bypass malnutrition maximum |

No example is a finalized-Def measurement or live acceptance. Three-input multiplication can be
powerful with modded organs; source ranges/defaults are retained as draft balance, not certified
for every enhancement pack. Zero/invalid/unrepresentable provider outputs and native lifecycle/cache
invalidation require exact contract work; do not silently invent a new arbitrary gameplay cap.

## Consumers, selected-source composition and draft decisions

Bounded installed binary/text searches for namespace `Consciousness_Patch`, package `Vex.Consciousness`
and Harmony owner `com.consciousness.patch` found only the original's two DLLs/About/Manifest. No
outside executable consumer was found; public scanner/settings types are not a reason to spoof
identity or create a compatibility facade. No native save object is added by the source. Source
global settings are not imported silently.

S045 is a genuine composition point: keep its directly owned condition ceilings after the single
owned physiological worker result. External original Consciousness Patch suppresses our equivalent
worker integration; leave foreign patches intact. Other capacity/name-independent source packages
are not conflicts by mere presence. Organ mods are optional and need current native capacity semantics,
not hardcoded item lists. The earlier S045 source/package suppression remains independent.

Proposed corrections for the draft: functional optional per-input cap; semantic incoming-stream
admission; no unconditional PatchAll; common global/save configuration; accurate seven-language UI;
commit-time policy/capacity refresh rather than stale startup-inlined numbers; no per-frame disk write,
duplicated capacity calculation, second health cache or patches to our own S045 implementation.
Keep native impactor/Health information and genuine organ acquisition costs. Concrete application
timing and native health/state invalidation must be specified together before implementation.

| ID | Remaining work |
| --- | --- |
| CO-Q1 | Exact effective worker/impactor/dataflow admission, stable target document, compatible foreign contributions, safe incoming emission and original-provider suppression |
| CO-Q2 | Common five-setting policy, precise committed application and native capacity/dependent-health invalidation across maps/world/save switch, including paused menu edits |
| CO-Q3 | Finite arithmetic, real modded-organ/health constraints, source intended-cap correction and directly composed S045 result; measure finalized healthy/impaired/boosted comparators |
| CO-Q4 | Focused honest tests, seven-language UI, independent implementation review, package checks and reviewed-build native surgery/health/settings/save workflows |

This research does not implement the mod, establish beta loadability of its DLL, execute its patches
or accept any physiological/medical gameplay. Source code, original Workshop installation and normal
saves/configuration remain unchanged.
