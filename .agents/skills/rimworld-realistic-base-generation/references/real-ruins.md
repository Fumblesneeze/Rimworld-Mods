# Using Real Ruins as design evidence

Real Ruins periodically uploads player maps and downloads other players' compressed blueprints. Its open-source
1.6 client is at [woolstrand/RealRuins](https://github.com/woolstrand/RealRuins). It is useful for aggregate
footprints and real mod-Def combinations, but it is not a curated gallery or a stable public design API.

## Source hierarchy

1. Prefer a user's already-populated local cache at
   `%USERPROFILE%/AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/RealRuins/`.
2. Prefer an explicitly exported local/offline blueprint supplied for research.
3. Only when fresh remote evidence materially helps, make one bounded read-only request through the exact
   open-source client endpoint, with a small limit (12 or fewer). Do not loop, crawl, enumerate seeds, upload,
   or treat server availability as required for development.

The public client obtains a random metadata list, then downloads named `.bp` objects from its bucket. These
details can change without notice. Re-read the current client source before any remote call and stop on rate
limits, access errors, or a changed contract.

## Treat remote blueprints as untrusted input

The following are repository research-tool budgets, not limits promised by Real Ruins. They were chosen with
substantial headroom over the 2026-08-13 sample and must be reviewed deliberately if a legitimate blueprint is
rejected; do not silently raise them:

- at most 12 metadata entries and 12 object downloads in one invocation;
- a 30-second request deadline, redirects disabled or restricted to the same HTTPS host, and at most 8 MiB
  compressed per object / 48 MiB compressed for the invocation;
- current remote object names must be canonical GUIDs; generate local filenames independently and never join an
  untrusted object name directly to a directory. For an existing legacy local cache, require a leaf-only name,
  reject separators, rooted paths, `..`, control characters, and names over 128 characters;
- stream GZip decompression into a counting stream; stop above 64 MiB expanded per blueprint or 100:1 expansion,
  whichever occurs first. Never call `ReadToEnd`, `[xml]`, or an equivalent eager parser on remote bytes;
- parse with DTD processing prohibited, external entity resolution disabled, a 64-MiB character ceiling, and a
  maximum XML depth of 32;
- reject dimensions above 500x500, more than 250,000 serialized cells, more than 1,000,000 item nodes, more than
  64 attributes on one element, or more than 1 MiB of UTF-8 text/attribute content for one field;
- accept only the expected blueprint root and known structural elements for aggregation. Unknown Def names are
  data, not types or paths; never instantiate them during offline analysis.

Write into a newly created ignored run directory, keep a manifest of requested URL host, response byte count,
expanded byte count, hash, and rejection reason, and clean only files owned by that invocation. A partial or
rejected sample is still useful evidence; do not retry in a loop to fill the quota.

## Blueprint contract

- `.bp` is GZip-compressed XML.
- The snapshot root includes captured bounds, biome, map size and in-game year.
- Each serialized cell may carry constructed terrain/floor, roof, one or more items, rotation, Stuff, stack
  count, wall/door flags and some contained/pawn data.
- Modded buildings remain exact Def names. Missing mods mean the visual/functional meaning may be unknown.
- Natural terrain, mountain shape, water and the player's full visual context are not a faithful colony render.
  Bounds may contain empty or cropped space, frames, clutter and unfinished construction.

Never spawn an untrusted blueprint into a normal save. Analyze copies beneath ignored artifacts. Never commit or
redistribute raw player blueprints. The exploratory 2026-08-13 sample predates this hardened parser contract and
must not be reused as parser-safety evidence.

## Safe aggregate analysis

For each blueprint, record:

- width, height, biome and year;
- constructed floor, roof, wall and door counts;
- wall/floor Stuff and Def frequency;
- room/connected-component dimensions where they can be derived without guessing missing Def shapes;
- known kitchen, cold-storage, dining, bedroom, workshop, sanitation and storage Defs;
- relative positions and nearest-path/door adjacency for exact resolved Defs;
- unresolved Def names separately, never silently classified by substring.

Use the active mod library to resolve known Defs and footprints. A keyword scan is discovery only: for example,
`HydroponicsBasin` is not a kitchen sink. Validate every promoted example against the source mod's actual Def.

## 2026-08-13 bounded sample

A one-shot random sample of 12 blueprints was retained only under ignored
`artifacts/BaseDesignResearch/20260813-reference-pass/real-ruins-sample/`:

- captured bounds ranged from 75x64 to 253x240;
- biomes included temperate forest, tropical rainforest, boreal forest and ice sheet;
- capture years ranged from 5500 to 5512;
- 860 distinct item Def names appeared, illustrating how modded real colonies mix ecosystems;
- exact discovered kitchen-adjacent Defs included Core stoves/butchery/coolers, Dubs sinks/basins,
  several RimFridge sizes, kitchen cupboards, an electric oven, a canning stove and a freezer unit.

All 12 had broad keyword signals for food, storage, dining and sleeping, but the scan also produced false
positives and the endpoint is not statistically representative. The useful conclusion is qualitative: real
colonies commonly combine Core and modded service buildings, varied ages/materials and large ranges of footprint.
Do not infer a canonical room size, popularity ranking or compatibility promise from this sample.

## What Real Ruins cannot prove

- that a room looked good or was finished;
- that a building was functional with the source mod list;
- why the player placed something;
- which pawns or traffic patterns used the room;
- that the random sample represents all colonies;
- that a blueprint may be republished as a showcase.

Pair every promoted design rule with inspected screenshots or an exact gameplay/mechanics contract.
