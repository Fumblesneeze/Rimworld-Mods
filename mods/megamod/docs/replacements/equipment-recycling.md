# Original weapons/apparel recycling

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Queue **S022**, intake **P1-17**, **origin: original**. **Specification drafted; nothing implemented
or accepted in game.** The user confirmed equipment scope. Recycle This is a research reference,
not a selected full replacement or a promise of its settings, work column, destruction or APIs.

[Proposal/design/tasks](../../../../openspec/changes/specify-megamod-equipment-recycling/),
[observable behavior](../../../../openspec/changes/specify-megamod-equipment-recycling/specs/megamod-equipment-recycling/spec.md)
and [research/provenance/questions](../refinement/2026-11-02-equipment-recycling-research.md).

## Coverage and deliberate choices

| Behavior | Evidence / proposal | Implemented / remaining |
| --- | --- | --- |
| Mark weapons/apparel | Confirmed user scope; Orders and contextual item workflow proposed | Nothing; native designation and physical job remain |
| Other crafted objects | Outside confirmed scope | Deliberately omitted |
| Station recycling | Smeltable equipment at applicable smelter; others at suitable tailor/crafting spot | Nothing; actual two-leg access, reservations and all-route usability need proof |
| Work/priority | Proposed 1600 base work, native smelting/general labor speed, existing Crafting bill work first | No new work column, global priority edit or skill XP; native behavior unverified |
| Construction refunds | Selected25% basic construction materials, actual material/volume, HP scaling and one stochastic rounding | Nothing; displayed ranges, three source comparators and finalized balance work recorded |
| Complex/fixed salvage | Complex ingredients excluded initially; fixed salvage independent of return percentage but scaled by HP with construction returns | Intentional deviation from reference's component-on default and unfiltered fixed extras |
| Damage/quality | Current remaining HP fraction scales all eligible returns; no extra quality/skill multiplier | Selected deviation; near-destroyed inputs yield barely usable materials |
| Marked quantities | Only currently authorized units; per-portion suspension survives split/merge; unspecified native consumption retires authorization first | Shared S023 safety proposal; no promotion of unmarked/suspended survivors; uncertain attribution blocks |
| Cancellation/protection | No stripping equipped/worn/kept items; explicit processing replacement; preserve unrelated orders | Nothing; safe lifecycle and cancellation before commit required |
| Completion/recovery | One input/result, bounded partial output recovery, no retry/reroll duplication | Nothing; native conversion and exception proof remain critical |
| Save/source coexistence | Suspend unsafe/new work, retain outputs, explicit resume; no foreign import | No migration or broad compatibility accepted |

## UI and settings

Orders, selected-item commands and contextual status/preview/cancel/resume control ordinary gameplay.
Output recovery remains accessible if the bench disappears. No bottom tab, cheat override or extra
work column. Per-item choices are saved gameplay state. Construction return (25%, integer 0–100),
and Show selected-item recycling command (on) use the common
Global default / Current save / Use global rows, with dormant overrides and main-menu disabled save
callbacks. Captured recovery terms remain on a started item; its actual HP is rechecked at consumption. Unstarted items use current preferences.
Hiding contextual entry does not hide Orders or existing-state cancellation. Seven-language UI remains.

## References, contracts and potential conflicts

Reference **Recycle This (Continued)**, `Mlie.RecycleThis`, Workshop **3253550009**; installed 1.7.0
DLL and public source revisions/hashes are pinned in research. [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3253550009)
and [source](https://github.com/emipa606/RecycleThis) were read before decompilation. MIT notices apply
to any substantial later reuse; no downloaded code, assembly or artwork is packaged here.

Installed XML uses a separate work type and moves native smelting into it; that is not adopted.
Its saved settings/work counters, source designation/JobDef identities, public SmeltProducts helper
and Harmony owner are not emulated. A real consumer, [AV] Mechtech, conditionally patches the source
workgiver/worktype by package/Def identity; that bridge does not activate for megamod. Native Crafting
ability may permit cooperation, but actual mech and beta compatibility are unverified.

Original Recycle This present withholds overlapping owned new/unfinished processing and preserves
committed output recovery; explicit Resume is required after safe activation. Custom workbench and
salvage providers need actual validated capabilities. No unsupported special input is converted into
guessed outputs. Normal recipe bills remain available independently. Unknown dynamic consumers may
exist; the bounded literal search is not proof of universal compatibility.

S023 destruction and S096 Micro Designations compose directly with the owned processing choice;
neither is an internal compatibility patch. S021 quantity/capacity and S033 shared item facts are
reused; native or linked bill lists/counts are not edited. Required future dependencies: Harmony and
XML Extensions for guarded additive Orders registration. Optional providers/Biotech stay optional;
no required source mod, other repository product or Dev Gateway. Every actual patch needs its own
minimal semantic target/admission and startup warning; XML additions preserve unrelated entries.

## Remaining work and balance limits

The [S096 recipe-order draft](item-recipe-orders.md) now defines the other recipe choices, retaining
native work/yield separately from this custom recycling while sharing processing authorization.

ER-Q1–4 retain proposed choices, exact beta irreversible boundaries, optional contracts and all
implementation/UI/native proof. Default recycling is lossy and costs work, but recovering wood,
cloth and leather expands reuse beyond native smelting. High return settings can alter
progression; no general third-party craft-loop safety is claimed. Source XML measurements are not
finalized active-mod playtests. Actual designation, carrying, work, cancellation and physical refunds
must be observed on the reviewed build before any feature or compatibility is accepted.

## Reconciled recovery — 2026-11-07

The selected initial policy is25% basic construction materials with complex ingredients excluded;
no initial complex-recovery toggle. Combine construction and separately declared fixed salvage per
material, scale both by current remaining HP fraction, then round once. A ruined item therefore
does not retain guaranteed full fixed salvage. Shared settings now have two rows: return percentage
and contextual-command visibility. Real returns, costs and native workflows remain unverified.
