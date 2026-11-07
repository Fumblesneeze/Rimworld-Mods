# [DN] Blunt Social Fights

## Identity and provenance

- Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
- Origin: **replacement**, S037 / P1-29. Source **[DN] Blunt Social Fights**, Danimineiro,
  `dani.BluntSocialFighting`, [Workshop 2886174990](https://steamcommunity.com/sharedfiles/filedetails/?id=2886174990).
- Inspected 2026-11-03: public commit **`e8e2291881d336d35673f603fb1acc885a193638`**;
  installed and public 1.6 DLLs are byte-identical, version 0.0.0.0, SHA-256
  **`7D2CD74E3D64015E6541F625824FB603D2FEF0927CFF78E5095ACA7ED53C2D04`**.
- MIT, copyright 2024 Dani. Preserve notice if any substantial code is reused; no source/DLL/art
  copied into the product. Latest reference advertises through 1.6, not accepted beta compatibility.
- [Research](../refinement/2026-11-03-blunt-social-fights-research.md) records read-only paths,
  hashes, the complete one-class/one-patch source, eleven native reference types and consumer scan.
- **Analyzed; specification drafted with gaps. Nothing implemented or verified in game.**

## Behavior inventory

All rows belong to [megamod-blunt-social-fights](../../../../openspec/changes/specify-megamod-blunt-social-fights/specs/megamod-blunt-social-fights/spec.md).
Named scenarios identify the observable owning contract; source evidence does not prove live results.

| ID | Input/action → result | Options/defaults and edge cases | Evidence / owning scenario | Status |
| --- | --- | --- | --- | --- |
| SF-01 | Non-Bloodlust social fighter chooses attack → primary-sharp attacks excluded | Active whenever admitted; every qualifying attacker, not colonist-only | Sole source postfix; A fighter has claws and a permitted blunt attack | Specified |
| SF-02 | Active Bloodlust fighter chooses attack → ordinary permission retained | Per attacker, not opponent; suppressed trait gives no exception; no forced hit | Source predicate/native TraitSet; One fighter has Bloodlust | Specified |
| SF-03 | Attack classified → primary damage decides restraint | Non-sharp/valid absent classification preserved; no name whitelist or damage conversion | Native GetDamageDef; Modded natural attacks use ordinary damage classifications | Specified |
| SF-04 | Sharp candidates removed → ordinary remaining weighted choice | Prior denial stays false; no new weapon pool or fallback punch | Native SocialInteractionUtility/job giver; Several permitted attacks remain | Specified |
| SF-05 | No permitted attack → no attack, normal state recovery | Does not preemptively block initiation or force early recovery; no busy retry | Native selection/no-job/state; Only sharp attacks remain | Specified |
| SF-06 | Combat outside social fighting → ordinary attack permission | Other mental states, drafted combat and non-pawn attacks unaffected | Exact source state test; A pawn is berserk | Specified |
| SF-07 | State/trait changes or save loads → current permission | Before choice/use, no stale exception or new saved record | Native availability checks; Resume a saved social fight | Specified |
| SF-08 | Permitted hit/fight ending → native consequences | Blunt lethality, secondary effects, wounds, XP, thoughts, recovery preserved | Native damage/state; A permitted blunt strike lands | Specified |
| SF-09 | Target/provider unavailable → ordinary behavior with warning | Compatible foreign restrictions allowed; original active suppresses owned duplicate | Guarded owned policy; The current game cannot support restraint | Specified |

## Interactions and composed result

The rule composes directly with owned social/trait/medical behavior and preserves the independent
contracts of finishing, hunting, item destruction and toddler eligibility. No internal Harmony stack
patches owned feature implementations. It does not treat every mental break as a social fight,
despite the source About text, and does not promise nonlethal fights.

Other attack/race/gene providers may supply compatible primary damage or bypass native permission.
Those paths need actual package/shape evidence and their own accepted workflows. Secondary damage
is not silently removed. Nothing here proves a generic conflict with a combat mod. The primary
classification is a concrete boundary, not a claim that wounds can never look sharp.

This decision needs only its attacker and attack. It adds no list/cache to consolidate with Ctrl F
or hauling, and does not cache pawn-specific eligibility in a shared Thing index. Required failure
warnings use the protected foundation path, including when owned logging controls suppress ordinary
messages.

## UI audit and relocation

| Source surface | Purpose | Owned destination / deviation |
| --- | --- | --- |
| Colored startup success line | Diagnostic | Guarded startup status/warnings; no unconditional applied claim |
| No settings, gizmos, overlay or bottom tab | No source control to port | No added control; native combat/Health/mental-state feedback remains |
| Compile-time debug classification log | Developer trace, absent in inspected release body | No per-attack runtime spam or new debug panel |

There are no adjustable values or new settings, so global/save inheritance requires no exception
or new rows. Pawn state and active traits remain native gameplay state, not a per-pawn setting.
If later customization is requested, it needs a new contract and the shared settings path rather
than an ad hoc global-only toggle. New shared diagnostics must use the baseline language catalogs.

## Contracts consumed by other mods or saves

| Consumer / evidence | Binding or saved contract | Preserve / unsupported / verification |
| --- | --- | --- |
| RimPy database installed text match | Workshop/package catalogue, older supported-version metadata | Proven catalogue identity only, not a callable consumer; no package spoof |
| Public source IsAvailablePatch method | Static Verb/ref-bool postfix entry; no external caller demonstrated by bounded scan | No speculative source-named facade; binary/reflection consumers remain SF-Q2 |
| Native permission/selection/cast path | Existing denial, primary damage, current social state and trait suppression | Preserve through admitted integration and real fight/action evidence |
| Native pawn injuries/mental state/partner | Existing game save data; source has no custom saved schema | No import/migration; feature removal does not heal native wounds |

No automatic source-settings import or binary/save drop-in claim is made. Original active package
suppresses the owned duplicate rule with a bounded warning; inactive downloaded files do not. Never
remove foreign patches or alter Workshop files. Optional providers remain absent-safe and narrowly
admitted, with separate exact-mod-list acceptance before a compatibility claim.

## Implementation and coverage

| Behavior | Implemented portion | Deviation / possible conflict | Missing / target evidence |
| --- | --- | --- | --- |
| SF-01–08 combat rule | Nothing | Preserve actual social-state/primary-only source semantics, not broader About wording; no new nonlethal promise | Effective native target docs, focused tests and native selection/hit/recovery/save evidence |
| SF-09 startup/provider behavior | Nothing | Replace unguarded patching/unconditional success log; suppress owned duplicate without foreign mutation | Semantic predicates, ordering/repatch/rollback tests and exact provider runs |
| UI/settings/persistence | Nothing new proposed | Intentionally no new feature controls or saved record | Confirm native feedback and package/state independence on reviewed build |

Harmony is required when eagerly used. No DLC, source binary, companion runtime mod or Gateway
dependency is introduced. No XML mutation is proposed; any later one must follow required XML
Extensions/package/content/merge rules. Source attack damage, cooldowns, probabilities and native
recovery values are preserved rather than imported as hardcoded balancing values.

See [proposal](../../../../openspec/changes/specify-megamod-blunt-social-fights/proposal.md),
[design](../../../../openspec/changes/specify-megamod-blunt-social-fights/design.md),
[tasks](../../../../openspec/changes/specify-megamod-blunt-social-fights/tasks.md) and
[progress/questions](../refinement/specification-progress.md). **SF-Q1–3 remain open**, and all
implementation/acceptance tasks remain unchecked.
