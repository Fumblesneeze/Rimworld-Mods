# Toddlers

## Identity and provenance

- Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; origin: replacement.
- S031, **Toddlers** by cyanobot, `cyanobot.toddlers`, Workshop **2903359152**; original inspected
  installed 1.6 package, target owned behavior **1.7 beta**. Inspected 2026-11-03.
- [Exact manual, source revision, hashes and compile coverage](../refinement/2026-11-03-toddlers-research.md).
  Public revision `ea885dffc6ed65937b3fbf0daa62598cf11a61fc`; 38 selected installed/package inputs
  match. No native beta acceptance or source-build equivalence follows from that comparison.
- No license grant was established. Independently implement the behavior; do not copy code,
  binaries or art. Workshop inputs remain read-only. An unofficial 1.7 port is a research lead,
  not the selected provider or proof of support.
- [Owned change](../../../../openspec/changes/specify-megamod-toddler-development/proposal.md):
  **six capability drafts, nothing implemented**. Questions and technical gaps remain explicit.

## Behavior inventory

| ID | Input/action → observable result | Options, values and edge cases | Evidence / owned scenario | Status |
| --- | --- | --- | --- | --- |
| TD-01 | Age/growth → dependent mobile toddler, then child | Human 1–3, learning stages, actual capacities, vat/normal time, saved progress and notice uniqueness | [Development](../../../../openspec/changes/specify-megamod-toddler-development/specs/megamod-toddler-development/spec.md); research index | Drafted |
| TD-02 | Eating/caregiver feeding → nutrition, slower eating and actual mess | Learned ability, diet/access, no consumption mood, tenfold chew duration, real cleanup | [Care](../../../../openspec/changes/specify-megamod-toddler-development/specs/megamod-toddler-care/spec.md); feeding/play research | Drafted |
| TD-03 | Solo/adult play → entertainment and distinct attention | Seven activities, attention cap, loneliness, real paired/bedside interaction and interruptions | Care; feeding/play research | Drafted |
| TD-04 | Crib exit or adult safety order → actual safe placement | Crawler/medical/door limits, single-cell failure, S030 temperature policy and safe carry fallback | Care; feeding/play research | Drafted |
| TD-05 | Craft/assign/dress → functional clothing and protection | Four garments, five modes, exact-item changes, locks/forced clothes and native contextual policies | [Apparel/mood](../../../../openspec/changes/specify-megamod-toddler-development/specs/megamod-toddler-apparel-mood/spec.md); apparel research | Drafted |
| TD-06 | Learning/mood → limited episodes and appropriate control | Clothing removal safety, source fit curves, no expectations, narrow harmless-state exemptions | Apparel/mood; apparel research | Drafted |
| TD-07 | Carry/caravan/kidnap → real dependent passenger or vulnerable victim | No free capacity/work, unique passenger identity, safe drops, ordinary damage and exit restrictions | [Travel](../../../../openspec/changes/specify-megamod-toddler-development/specs/megamod-toddler-travel/spec.md); travel research | Drafted |
| TD-08 | Refugee crash/adoption → child with history and safe arrival lifecycle | Parent chance/relations, trauma, safe waiting, native adoption, abandoned-child age-up and cleanup | Travel; travel research | Drafted |
| TD-09 | Supported provider care/race/clothing → integrated outcomes | DBH real water/bath recovery, HAR ordered ages/appearance, Attire categories/research | [Integrations](../../../../openspec/changes/specify-megamod-toddler-development/specs/megamod-toddler-integrations/spec.md); integrations research | Drafted, provider evidence pending |
| TD-10 | Configure/load → scoped effective behavior and truthful unavailability | Common globals/save overrides; entity state, debug actions, Biotech/source co-installation and migration boundary | [Configuration](../../../../openspec/changes/specify-megamod-toddler-development/specs/megamod-toddler-configuration/spec.md) | Drafted |

## Interactions and composed result

S030 owns mandatory toddler thermal prevention, escape and safe area return. All S031 play,
undressing, bedtime, crib, bath and arrival work calls that owned policy directly. S024 needs and
S021/S125/S126 hauling share concrete facts, not duplicate job schedulers or independent map scans.
S133/S135 transport/loading must preserve dependent-passenger identity and safe care handoffs.
No self-patching stack or inferred conflict between unrelated selected mods is introduced.

## UI audit and relocation

| Source surface | Purpose | Owned destination / difference |
| --- | --- | --- |
| Mod settings | Care, rates, comfort, learning, apparel and feedback preferences | Ordered common Childcare settings; global/default/current-save/Use global; no source static global leakage |
| Existing Assign/Schedule/Gear | Per-child policies and status | Retain those entity controls; no new bottom tab |
| Child/caregiver context and gizmos | Feed, dress, rescue, let out, drop, adopt, wash | Native contextual gameplay actions with actual failure reasons |
| Play tooltip, notices and reports | Attention, development and current actions | Clear localized native displays; do not describe a selected move as already successful |
| Spawn/reset/safety diagnostics | Unrestricted developer actions | Top developer controls only; hidden when developer mode is off |

Every ordinary setting follows common inheritance; none currently requires a global-only exemption.
The configuration capability records exact defaults/ranges and timing. Main-menu save controls are
disabled. No proposed rendering switch is inferred from an unused source translation key.

## External and saved contracts

| Consumer / evidence | Contract | Intended treatment / limit |
| --- | --- | --- |
| Progression: Attire installed XML | cyanobot package-gated folder, garment/category names, optional tribal research | Owned admitted equivalent without package impersonation; preserve unrelated categories; beta folder activation remains to verify |
| Full DBH / DBH Lite / HAR | Source's reflected needs/fixtures and stage/graphics operations; Lite explicitly source-recognized | Optional narrow atomic adapters; actual provider bodies, Lite variant differences and native workflows remain unverified |
| Hospital / Altered Carbon / Celsius / GeneSeed / sounds | Source references and conditional XML | Remaining contract research; no inferred supported adapter |
| Original saves / reflected API consumers | Source type/assembly/Def/job/settings identities | No automatic conversion, API parity or safe source-removal claim; complete actual consumer audit before promising facades |
| Owned saves | Development, loneliness, paired care/fixture and arrival ownership | Preserve across reload; exact failure/recovery schema must precede implementation |

Biotech is required for this feature, not unrelated megamod behavior. Harmony/XML Extensions become
required when the planned runtime/XML integrations ship. All listed content providers are optional.
The original provider disables overlapping owned behavior with a warning; no automatic mod removal.
Enabled incompatible targets fail closed with scoped diagnostics and intact native/provider fallback.

## Deviations, omissions and implementation coverage

**Nothing is implemented or accepted.** The draft intentionally corrects unsafe/stale transfers,
time-dependent callback errors, attention-cap overshoot, incomplete food-mood suppression, conflicting
drawing/bugwatch eligibility, destructive foreign-apparel rewrites, forced clothing removal, broad
foreign mental-state suppression, bath cancellation rewards and malformed alien stage insertion.
It keeps the source's functional scope and records all tuning assumptions in observable contracts.
The [design decision table](../../../../openspec/changes/specify-megamod-toddler-development/design.md)
explains each discrepancy and remaining native/provider evidence requirement.

Deprecated/excluded source paths, unused UI keys, package-writing texture copies and wholesale
native-function clones are not behavior to preserve. Source API/save migration and unproven optional
providers remain explicit gaps. No unconditional support for all races, crib names, TVs or transports.
XML additions must merge; whole crying/giggling curves are intentionally replaced and need guards.

## Verification and acceptance

Research-group reviews checked current source/manual statements; they do not validate gameplay.
All [implementation and acceptance tasks](../../../../openspec/changes/specify-megamod-toddler-development/tasks.md)
remain unchecked. Native beta methods, finalized apparel comparators, assets/localization, honest
focused RED/GREEN, reviewed builds and personally observed game workflows remain future work.
No game launch, product build, normal config/save change or Workshop modification occurred here.
