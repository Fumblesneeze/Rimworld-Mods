# Original on-site destruction by melee

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Queue **S023**, intake **P1-17**, **origin: original**. **Specification drafted; nothing implemented
or verified in game.** The user requires real repeated melee attacks against items with HP where
they lie. Destroy Item is an inspected comparator, not a selected full replacement.

[Proposal/design/tasks](../../../../openspec/changes/specify-megamod-melee-item-destruction/),
[observable behavior](../../../../openspec/changes/specify-megamod-melee-item-destruction/specs/megamod-melee-item-destruction/spec.md)
and [research/provenance/questions](../refinement/2026-11-02-melee-item-destruction-research.md).

## Behavior inventory and coverage

| Behavior | Confirmed or proposed contract | Implemented / remaining |
| --- | --- | --- |
| Item designation | Confirmed HP-using items only; proposed loose-item classification includes qualifying corpses/packed wrappers | Nothing; installed buildings, plants, living pawns and HP-less objects excluded |
| Actual melee | Confirmed real repeated attacks at the item's location | Nothing; native attack selection, release, recovery and effects require proof |
| Work assignment | Proposed ordinary Hauling after normal hauling at equal priority, one worker, real manipulation/melee capability | No new work type or animal/mech ability; native reachability and priority remain to verify |
| Damage consequences | Preserve ordinary HP damage, drops, contents, hazards, quest/record effects | No artificial refunds, safe disposal promise or copied source thoughts |
| Exact quantities | Currently authorized portions alone; mixed states require safe same-cell separation preserving HP/traits; unspecified consumption retires orders first | Nothing; suspension cannot spread or disappear through merging; no attack-then-recreate |
| Interruptions | Cancel unreleased attacks; retain damage and ordinary cooldown | Nothing; released effects are not undone and attacks never auto-retarget nearby items |
| Stalled work | Proposed ten completed ineffective cycles, visible suspension and bounded retry; unfinished count survives unchanged restart/reload | No tick-based failure counter, forced damage or automatic unchanged loop |
| Lifecycle | Held items pause; equip/wear/keep/install cancels; map departure suspends with explicit Resume | Nothing; saved orders cannot replay a strike or bypass failed admission |
| Processing composition | S022 recycling and S096 actions replace the same owned processing choice directly | No self-patch stack or broad erasure of unrelated designations |

## UI and settings

Use Orders and selected-item contextual controls for ordinary gameplay. Show HP/marked quantity,
known consequences, blocked reasons and Cancel/Resume/Retry. No bottom tab or new work column.
Per-item orders and suspension are gameplay state, not mod preferences. No global/save setting is
introduced: real native attacks govern speed; the proposed ten-cycle safety bound is a named policy
constant. Source instant deletion, speed multiplier and simultaneous-worker preferences are omitted.
Seven-language UI and real native controls remain implementation work.

The reference source has an Orders designator, contextual reverse-designator control, X shortcut
and global settings; it registers the designator during map GUI processing and has no bottom tab.
That repeated registration mechanism and its instant-mode preference are not copied. A shortcut is
not assigned here without checking the owned Orders key map.

## Provenance, contracts and conflicts

Reference **Destroy Item**, Garwel, `Garwel.DestroyItem`, Workshop **2423311270**.
[Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2423311270) and
[official source](https://github.com/GarwelGarwel/DestroyItem) were read before implementation
inspection. Installed assembly/file **1.5.0.0**, public checkout
`53706807c41e2e2ba8ba89e495258aafe35adc4f`; exact DLL/About/source correspondence and native beta
hashes are recorded in research. MIT copyright 2021 Garwel applies to substantial later reuse.
No upstream assembly, source or artwork is added to a product package by this documentation change.

The reference performs timed DPS/labor damage and manual final destruction; it does **not** execute
actual melee attacks. Its HP-less eligibility, configurable speed, multiple workers and bespoke
memories/record are deliberately outside this original feature. Its designation/job/Def/settings
identities and foreign saved jobs are not emulated or imported.

A bounded literal consumer scan found VEF's similarly named but independent animal workgiver, not
a demonstrated binding to Garwel's implementation. This is not proof that dynamic consumers cannot
exist. No compatibility facade or drop-in claim is selected. Original Destroy Item present suspends
overlapping owned attacks; later safe activation requires explicit Resume. Recycle This's bench
disposal does not alone disable this feature, but conflicting external orders on the same item need
a verified ownership boundary or visible blocking. Compatible combat/content providers retain their
actual effects; unsupported cancellation/targeting/stack paths do not fall back to instant deletion.

Future selected Harmony/XML seams require individual semantic target documents and executable
startup admission, effective-code/repatch handling, owned rollback and one warning per skipped
enabled patch. Orders additions use guarded, merge-preserving XML Extensions; Harmony and XML
Extensions become required dependencies in the implementing slice. Source providers remain optional.
Use S021/S033 shared item facts and current per-pawn checks, never another private full-map cache.

## Verification and open work

The [S096 recipe-order draft](item-recipe-orders.md) now defines native bench processing alternatives;
its burn/destroy recipes do not replace the actual melee swings specified here.

Research and the observable draft do not prove runtime behavior. All implementation tasks, native
damage/cancellation/quantity paths, package checks, translations and provider workflows remain open.
MD-Q1–4 retain proposed classification/worker/retry choices, exact native action lifecycle, quantity
and provider composition, and implementation/acceptance evidence. Final acceptance requires personally
observed real swings, HP loss, physical destruction and relevant side effects from native player
orders on the reviewed build. No game is launched for this prose-only slice.

Independent document review found and corrected two shared quantity gaps: suspended portions must
stay separate from active permission, and unspecified native consumption cannot promote surviving
unmarked units. S022 carries the same contract. Source/provenance/native consequence review found
no additional issues; actual runtime compatibility remains unverified.
