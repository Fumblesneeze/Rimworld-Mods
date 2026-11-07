## Why

Players should be able to send a colonist to an available recreation activity without temporarily
rewriting work priorities or schedules. P1-02 names Chill the Fork Out; its current source also
handles ordered reading and optional Odyssey swimming, which need explicit eligibility and exit rules.

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target: **RimWorld 1.7 beta**. This change specifies future behavior only; no gameplay is delivered yet.

## What Changes

- Specify contextual orders for supported recreation buildings and safe reasons when unavailable.
- Let an explicit recreation/reading/swimming order continue during Work or Anything without
  permanently changing the timetable or forcing endless recreation.
- Specify optional swimming only when Odyssey is active and its swimming facilities are available.
- Put the recreation admission threshold into the shared global/save settings system.
- Preserve actual activity eligibility and normal interruption/completion; do not copy private
  job markers or unsafe broad patches as part of the product contract.

## Capabilities

### New Capabilities

- `megamod-directed-recreation`: native contextual recreation orders and their lifecycle.

### Modified Capabilities

None. Foundation settings and compatibility admission are reused.

## Impact

Source: `KT.ChillOut`, Workshop `3523235444`. See the
[source report](../../../mods/megamod/docs/replacements/chill-the-fork-out.md).
The original mod is not required. Harmony is a **required future dependency if used** for schedule
integration; package metadata is unchanged in this documentation change. Odyssey is **optional**:
its absence removes swimming only, while ordinary directed recreation and available reading remain.
Other recreation providers are optional content, admitted through their actual available activities.
No XML Extensions operation or dependency is proposed. No new bottom tab is needed.
