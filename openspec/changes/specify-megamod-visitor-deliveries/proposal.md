## Why

Caravan purchases and visitor gifts often arrive beside the visitors even when suitable storage is
nearby. Just Put It Over There makes delivery a courtesy performed by visitors; the owned version
needs to preserve that physical workflow without losing paid goods, commandeering the main trader,
or interfering with departure and other hauling behavior.

## What Changes

- Specify storage delivery of physically received caravan trade goods, including incoming sale-proceeds
  silver, and visitor gifts, including routine departure gifts from supported Hospitality visitors.
- Preserve trade/gift outcomes and ordinary ground delivery when no suitable courier or storage
  exists. Keep main traders, animals, emergency work and already departing trade parties out of
  delivery assignments; do not turn visitors into general colony haulers.
- Define finite single-load assignments, exact received quantities, receipt-only handling permission, storage priority/access,
  partial delivery and interruption/save recovery. Compose shared storage/capacity facts with
  S005/S021/S024, and preserve the user's distinct refill and per-stack limits for S049.
- Record source behavior, corrective assumptions, consumed boundaries and unresolved implementation
  questions. **Documentation only**: no gameplay or compatibility is delivered by this change.

## Capabilities

### New Capabilities

- `megamod-visitor-deliveries`: bounded physical delivery of received purchases/gifts by eligible
  visitors, exact quantities, storage selection, routine/emergency departure and safe fallback.

### Modified Capabilities

None. Existing owned hauling does not gain nonplayer batch workers; this is a separate contextual
courtesy using shared facts and ordinary carrying rules, not a general work permission change.

## Impact

Sole owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Source **S027 / P1-21**, Just Put It Over There, `Mlie.JustPutItOverThere`, Workshop **2856471776**.
See [research](../../../mods/megamod/docs/refinement/2026-11-02-visitor-deliveries-research.md) and
[replacement coverage](../../../mods/megamod/docs/replacements/just-put-it-over-there.md).

Future engine boundaries: physical trade/gift handover, visitor duties and normal hauling; candidate
targets and admission are in design. Harmony is **required** once eagerly referenced. Original
Just Put It Over There and Hospitality (`Orion.Hospitality`) are **optional** external providers;
ordinary caravan/visitor behavior must work without Hospitality. Exact original co-installation
withholds owned overlap. No XML edits, XML Extensions use, metadata changes or new runtime library
mod are introduced now. Repository-owned mods and the developer Gateway remain independent of the
product. The source's public API and save-removal claims are not assumed drop-in guarantees.
