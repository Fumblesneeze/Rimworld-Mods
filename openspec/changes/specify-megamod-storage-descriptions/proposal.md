## Why

Players need to compare storage before building it, but handwritten descriptions can omit capacity
or retain obsolete values. S098 replaces that guesswork with current, clearly scoped storage facts
while preserving the rest of each building's description.

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin replacement: S098/P2-27, **Storage Stats in Description**. This change specifies future work;
it delivers no runtime behavior or compatibility acceptance.

## What Changes

- Add a readable storage summary to existing construction descriptions and information cards for
  supported storage buildings: physical capacity, per-cell capacity where meaningful and supported
  item types. Distinguish stack slots from item quantities, building from group and type from instance.
- Use actual storage semantics, including native bookcases and admitted optional providers. A bed's
  bedding filter or a machine's ingredient/ammunition filter does not make it a general warehouse.
- Correct misleading empty/partial filter summaries, stale startup-only values and unqualified
  multiplication for specialized capacity. Keep physical capacity separate from refill policy.
- Replace only an identified obsolete storage-stat block; preserve unrelated prose, formatting,
  later paragraphs and other providers' facts. Avoid duplicate owned summaries.
- Retain configurable heading color through common global/save settings, with one consistent reset,
  valid color input, immediate display updates and seven baseline languages.
- Compose directly with the owned Architect presentation, shared formatting and storage facts.
  Preserve external providers through explicit admission/coexistence rules rather than impersonation.

## Capabilities

### New Capabilities

- `megamod-storage-descriptions`: Current, truthful storage descriptions, safe legacy-block handling,
  contextual presentation and shared color configuration.

### Modified Capabilities

None. Existing linkage, refill, Architect and number-formatting contracts remain unchanged.

## Impact

Future owned storage-description presentation, optional storage adapters and common settings are
affected. Core is required; external Harmony becomes required when implementing the proposed native
presentation hooks. Adaptive Storage Framework, Reel's Expanded Storage, Relevant Stats In Description
and Soft Warm Beds are optional research/compatibility inputs, not hard dependencies. Original
`ferny.storagestatsindescription` disables owned overlap with an explanation.

No XML mutation is proposed. Any later XML patch requires XML Extensions, content/package/shape
guards and merge preservation in the same implementation slice. No source assembly, asset or
Dev Gateway dependency ships in the product. Source reuse rights and exact effective integrations
remain research questions; a public method with no proven consumer does not justify a facade.
