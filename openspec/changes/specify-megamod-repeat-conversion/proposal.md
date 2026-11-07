## Why

A chosen conversion target should not require repeated manual orders whenever a guide's ability
recovers. S092 keeps that explicit assignment active through ordinary temporary interruptions while
preserving real travel, casting, certainty effects and shared cooldowns.

## Owner

Exactly one owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**. S092/P2-21, Keep Converting. Specification only.

## What Changes

- Add a contextual assign/cancel command for a guide who already possesses a supported conversion
  ability. Keep one chosen target and the intended ideology as pawn-specific saved gameplay state.
- Repeat ordinary conversion when eligible, respecting current jobs, native ability restrictions,
  travel limits, allowed areas, target sleep preferences and shared cooldowns.
- Retain assignments through temporary unavailability; finish or cancel them for conversion,
  departure, loss of the role/ability or other terminal changes. Prevent duplicate queued casts and
  remove only owned pending work on cancellation.
- Put timing, distance, waking and ritual preferences in the common global/save settings UI.
  Resolve the source's inconsistent distance default explicitly and keep assignments out of settings.
- Preserve admitted Vanilla Ideology Expanded leader conversion without granting abilities or
  automating rituals. Compose with owned specialist automation directly.

## Capabilities

### New Capabilities

- `megamod-repeat-conversion`: explicit recurring conversion assignments, safe native casts,
  temporary versus terminal lifecycle, inherited configuration and optional-provider admission.

### Modified Capabilities

None. S051 specialist work commands retain their own triggers; conversion is a separate assignment
which shares ordinary job and ability ownership rules rather than becoming another work-start buff.

## Impact

Future contextual pawn commands, small saved assignment records, native ability/job integration and
common settings. Core and external Harmony required on implementation. Ideology is required for
this feature but optional for the megamod as a whole: without it, the feature is inactive. Vanilla
Ideology Expanded - Memes and Structures and its required framework are optional providers.

The design does not require the source's Human-only XML component injection or global command-draw
patch. Any later XML use requires guarded XML Extensions and its dependency at first use. No source
assemblies/assets, package spoofing, shared runtime library mod or Gateway dependency is introduced.
Implementation, exact adapter admission and live acceptance remain future tasks.
