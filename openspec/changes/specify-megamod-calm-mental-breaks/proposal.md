## Why

Colonists need a social way to help someone through a mental break, with time, relationship and
negotiation ability affecting the outcome. S100 captures Snap Out!'s counselling loop while making
eligibility, failure, recovery and its existing external reward contract reliable on 1.7 beta.

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin replacement **S100/P2-29, Snap Out!** (`weilbyte.snapout`). **Specification only:** no gameplay,
compatibility or save migration is implemented or accepted by this change.

## What Changes

- Add automatic Warden counselling and the ordinary contextual order for eligible mental breaks,
  including colonists, configured prisoners and non-hostile visitors. Preserve real work restrictions.
- Specify conversation time, negotiation/opinion chances, optional aggressive-target risk, recipient
  cooldown, failure retreat, recovery time, social practice and the helper mood memory.
- Correct stale-target outcomes, unrelated-interaction cooldown coupling, destructive catharsis
  removal and recovery that depends on an inaccessible bed. Distinguish calming from full recovery.
- Use common inherited settings; put guaranteed-success and diagnostics in the top debug surface.
- Preserve the demonstrated helper-memory contract and specify a narrow optional Character
  Development want adapter, with single-provider selection and guarded compatibility.

## Capabilities

### New Capabilities

- `megamod-calm-mental-breaks`: Eligibility, counselling, outcomes, recovery, feedback, scoped
  configuration and compatible external helper rewards.

### Modified Capabilities

None. Owned toddler care, health/capacities, social-fight and work-priority contracts compose directly.

## Impact

Future owned jobs, interaction/memory Defs, contextual orders and common settings are affected.
Core is required. Character Development (`ferny.characterdevelopment`, Workshop `3770112044`) and
Vanilla Races Expanded - Android (`vanillaracesexpanded.android`) and Multiplayer are optional,
none a prerequisite for ordinary play. Preserve Android's reformatting/solar-flare exclusions.
Multiplayer synchronization
remains explicitly unverified. Source presence suppresses owned overlapping behavior and Defs.

The proposed optional XML adapter makes XML Extensions (`imranfish.xmlextensions`, Workshop
`2574315206`) required when implemented; declare it with the first use, including external package,
content and shape guards. Harmony is required if native or optional C# patches are introduced;
prefer owned jobs/Defs over unnecessary patches. Do not copy the source's global sad-wander edit,
bundle upstream assemblies or spoof its package. MIT attribution applies to any later reused source
material; this draft independently specifies behavior. No Gateway product dependency.
