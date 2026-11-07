# SmarterDoctors (Continued)

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement S058/P1-52; package `zal.smarterdoctors`, author Zaljerem, original Maarx.
[Continued Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3744377058),
[original source](https://github.com/maarxx/SmarterDoctors). Inspected2026-11-04, About1.0.1,
advertised1.6 only; selected DLL SHA256
`0E4FB6312F285E431C0F3517B283700FB365C44F16FE23D0DEBE96000B32ED8A`.
No reuse grant found in inspected source roots/headers. Independent implementation, no copied code,
assembly or art. Analysis of the1.6 reference does not establish1.7 compatibility.

[Research](../refinement/2026-11-04-smarter-doctors-research.md) records the complete selected behavior,
included/original comparison, current native contracts and bounded consumer search.
Status: analyzed, observable draft independently reviewed and strictly validated. Nothing implemented.
[Proposal](../../../../openspec/changes/specify-megamod-smarter-work-priorities/proposal.md),
[design](../../../../openspec/changes/specify-megamod-smarter-work-priorities/design.md),
[spec](../../../../openspec/changes/specify-megamod-smarter-work-priorities/specs/megamod-smarter-work-priorities/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-smarter-work-priorities/tasks.md).

## Behavior inventory

All rows are specified, not implemented or verified.

| ID | Intended behavior | Boundary |
| --- | --- | --- |
| SW01 | Better next automatic target | Existing work-type priorities, eligibility, explicit orders and current jobs remain |
| SW02 | Imminent danger before routine tending classes | Comparable urgent death predictions first; routine ties disease, bleeding, other; current care preserved |
| SW03 | Feed current starvation by malnutrition severity | Empty food need and malnutrition together; ordinary food/feeding rules |
| SW04 | Tameness first, then concentrated lessons | Native next requested lesson; category then steps; optional types absent-safe |
| SW05 | Fertile growing locations first | Hay tie-break only; growing buildings and native harvest batches retained |
| SW06 | Cut other plants before hay | No new designation, harvest conversion or permission |
| SW07 | Finish structures, floors, then spike traps | Supplied frames only; material delivery/repair costs unchanged |
| SW08 | Rescue urgent patients first, colonists before animals at equal urgency | Native eligible targets/beds only; no automatic non-faction extension |
| SW09 | Direct once-only composition | Care/food/medicine owners remain; unsupported non-neutral provider ordering wins whole selection |
| SW10 | Contextual controls and safe fallback | No new UI/state cache; source-present suppression and independent family warnings |

## Deviations, interactions and omissions

The source's huge float scores erase disease and some bleeding differences. It also uses the safest
disease gap with the wrong comparison direction. The draft corrects both while explicitly retaining
disease-before-bleeding only as an equal-urgency routine tie-break; imminent danger takes precedence. It does not
claim optimal triage. Training categories cannot be crossed by extreme modded step counts.

Growing follows documented fertility-first intent; the source subtracts a whole fertility point
from hay zones. The draft uses actual harvested crop/requested sow crop and growing buildings too.
Native harvest batching remains, so individual plants in a batch are not guaranteed fertility order.

The source's additional old-code rescue eligibility override is **deliberately omitted**: it removes
the faction restriction and can reverse current native/foreign rejections while leaving scanning
inconsistent. The selected urgency-first rescue ordering retains native eligibility. Explicit native outside-faction rescue
remains. A safe automatic extension is a recorded decision, not silently considered implemented.

S024 housekeeping yields to care; S042 self-tending and S056 emergency surgery retain their own
initiation. S055/S057 own food selection; shared quantities stay physical. S062/S124/S134 medical
sources and S061 construction sources require later sequential composition based on actual evidence.
S060 [Smarter Construction](smarter-construction.md) now composes directly: structure/floor/spike-trap
categories precede its within-category local blocking-frame/neighbor order, with current access and
worker eligibility preserved. No invented conflict or internal patch stack is selected.

## UI, settings, persistence and external contracts

No source settings, Defs, menu/tab or custom saved state; none added for this feature. Native work,
health, training and growing controls remain entity gameplay state. Later configurable policy uses
the common global/save system. No source-save migration, serialized ranking or separate Thing cache.

Bounded installed ASCII/UTF16LE search found source files and RimPy/replacement catalog entries,
with no executable caller found. Internal types and methods could be reflected by unknown consumers;
no namespace facade, assembly/package spoof or blanket drop-in claim follows. Original
`maarx.smarterdoctors` or continued `zal.smarterdoctors` active => suppress all owned overlap and warn;
leave the external implementation untouched. Other unsupported families fall back independently.

## Coverage and verification

**Nothing implemented.** Source inventory and observable artifacts exist; effective target specs,
honest RED/GREEN, finalized policy, package/languages, independent implementation review and causal
native workflows remain unchecked. SD-Q1–4 in the [ledger](../refinement/specification-progress.md)
retain medical balance/composition, omitted rescue choice, effective provider/performance contracts
and final acceptance. No game, build or gameplay test run is claimed by this documentation work.

Independent scoped review found no issues. After the separately reviewed S060 composition addition,
the draft has **10 requirements, 42 scenarios and 24 unchecked tasks**. Typed strict validation at
that follow-up passed **82 items, zero failed**, and all twelve scoped Markdown documents passed
local-link and whitespace checks. Native acceptance must observe actual worker actions and outcomes;
priority queries and direct state setup cannot substitute for it.

## Reconciled medical priority — 2026-11-07

SW02 now puts imminent life-threatening needs before routine disease, bleeding or rescue classes.
S134 consumes that same classification across rescue/tending; current care and native eligibility
remain protected. Source disease-first behavior is an intentional deviation, not our selected policy.
