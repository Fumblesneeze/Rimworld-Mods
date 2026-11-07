## Why

Monument obligations are easier to track when the quest name and remaining construction or
preservation time appear beside the monument. S101 brings that read-only information onto the map
without guessing a deadline from an unrelated quest timer or changing the quest itself.

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin replacement **S101/P2-31, Monument Highlight** (`espio.monhighlight`). This change specifies
future behavior only; no implementation, visual acceptance or compatibility is delivered.

## What Changes

- Display the current quest name and Build, Protect or Keep obligations beside placed monuments,
  using their actual active quest state and remaining time.
- Handle marker copies, multiple maps/quests, missing or ambiguous timing, phase changes, paused
  inspection and cleanup without stale, negative or invented deadlines.
- Distinguish an overdue but still buildable decree from its later forgotten/expiry deadline;
  finishing construction late still transitions to the actual Keep obligation.
- Use readable localized text and shared owned label resources, with safe placement at map edges,
  correct map/world/fog visibility and no source atlas or unbounded static text cache.
- Add a small bottom-right view toggle backed by the common inherited setting; add no bottom tab.
- Preserve ordinary quest selection, construction, penalties and rewards, with guarded optional
  content and single-provider handling.

## Capabilities

### New Capabilities

- `megamod-monument-labels`: Read-only quest labels, obligation timing, contextual visibility,
  lifecycle, common view preference and bounded compatibility fallback.

### Modified Capabilities

None. S093 floor-label behavior remains owned separately; share its rendering resources, not its
room/zone semantics or settings state.

## Impact

Core is required. **Royalty is optional for the megamod but required for these native monuments**;
without it this feature is unavailable, and other megamod features remain usable. External Labels
on Floor is neither required nor replaced by this slice. No other content pack is required.

Prefer owned components/native reads. Harmony becomes required only if actual native observation
or UI hooks are used; every such hook needs a semantic target contract. No XML edits are proposed;
any later XmlExtensions.* operation requires XML Extensions metadata and guards at first use.
Suppress owned overlap when original `espio.monhighlight` is active. No original assembly, bundled
Harmony or font atlas is shipped. No source license/reuse grant was established; independently
implement, using admitted native or appropriately licensed owned text resources.
