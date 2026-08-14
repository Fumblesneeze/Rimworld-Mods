# Using Real Ruins as design evidence

Real Ruins provides thousands of player-created base snapshots. Use it as a measured placement corpus, not a
four-image mood board and not a blueprint generator. Its open-source 1.6 client is
[woolstrand/RealRuins](https://github.com/woolstrand/RealRuins).

## Reproducible workflow

Use the repository CLI; never reimplement ad-hoc download/parsing in a shell:

```powershell
dotnet run --project tools/RealRuinsCorpus/RealRuinsCorpus.csproj -c Release -- `
  analyze --metadata-limit 5000 --blueprint-limit 2000 --concurrency 16 `
  --output-directory artifacts/BaseDesignResearch/<run-id> -o table
```

The operation first prefers an existing `metadata.json` and `.bp` bodies in its ignored run directory, so it is
safe to resume. It downloads metadata from the current client endpoint and bodies from the current bucket. It
sorts the cohort by canonical GUID before choosing bodies, records every parsed/rejected object and hash, and
writes `summary.json`. Raw blueprints and the full summary stay ignored because they contain player layouts.

Render a safely parsed body for review:

```powershell
dotnet run --project tools/RealRuinsCorpus/RealRuinsCorpus.csproj -c Release -- `
  render --blueprint artifacts/BaseDesignResearch/<run-id>/blueprints/<guid>.bp `
  --output-file artifacts/BaseDesignResearch/<run-id>/visual-review/<guid>.svg --scale 4
```

Select a stratified review set: compact/sprawling, mountain/open, high/low table count, high/low workstation
count, relevant optional Defs, and counterexamples. Inspect at least 20 schematics for an initial rule family.
Aggregate statistics identify candidates; schematics determine whether a statistic reflects coherent layouts.

## Untrusted-input contract

The analyzer enforces these repository budgets:

- metadata 1..10,000; requested bodies 1..5,000; concurrency 1..24;
- 30-second request timeout, exact HTTPS hosts, redirects disabled;
- 8 MiB compressed per object and 4 GiB compressed per operation;
- 64 MiB expanded per blueprint, XML depth 32, 500x500 bounds, 250,000 cells, 1,000,000 item nodes and
  64 attributes per element;
- DTDs prohibited and external entities disabled;
- canonical GUID object names mapped to independently constructed local paths;
- atomic writes, safe resume and explicit failure records.

Unknown Def names remain data. The analyzer never loads assemblies or instantiates snapshot types. Promote only
statistics based on exact resolved footprints/semantics; keep unresolved/modded Def discovery separate.

## 2026-08-14 benchmark cohort

Ignored run: `artifacts/BaseDesignResearch/20260814-real-ruins-corpus/`.

- metadata: 5,000 rows; SHA-256
  `D2297942477AAFDC44A78A31C2A73B478F9219A44AC0A90DCE634A6C0363FF91`;
- attempted bodies: 2,200; parsed: 2,199; rejected: 1; compressed bytes: 115,207,808;
- resolved dining tables: 4,869; resolved Core workstations: 6,049; resolved conduit cells: 904,440;
- 20 stratified schematics were inspected from compact, sprawling, mountain, town-grid and fragmented colonies.

Promoted evidence from this cohort:

- **recurrent:** 4,735/6,049 resolved Core workstations (78.3%) touch a wall. Use a wall run by default; a central
  island needs a coherent workflow/storage reason and clear interaction aisle.
- **recurrent:** 598,238/904,440 conduit cells (66.1%) share a wall cell; 681,913 (75.4%) are under or cardinally
  adjacent to a wall. Median per-blueprint shares are 70.8% and 82.5%. Route presentation wiring through walls
  or service corridors.
- **recurrent:** dining seats follow actual table perimeter. Median adjacent chairs are 2 for 1x2 tables, 4 for
  2x2, 7 for 2x4 and 6 for 3x3. Size the table for the chairs rather than spacing chairs around an imagined center.
- **archetype (Dubs):** 322/459 resolved `WaterTowerS` instances (70.2%) are unroofed. Combined with the utility
  role and inspected schematics, prefer an exterior service yard, never an occupied kitchen/dining room.
- **mechanical:** Core `ChemfuelPoweredGenerator` is 2x2, `Graphic_Single`, and non-rotatable. The corpus roof
  statistic is not permission to rotate or place it among workstations.

Do not copy a blueprint, infer intent from one placement, or claim these resolved Core/Dubs results cover unknown
modded Defs. Re-run the corpus when the endpoint/client contract or the relevant Def inventory materially changes.

## What Real Ruins cannot prove

- that a room looked good or was finished;
- that every serialized building was functional with its original mod list;
- why a player chose a placement;
- the pawn traffic at capture time;
- permission to republish a player's base.

Pair promoted rules with exact game mechanics and live visual review.
