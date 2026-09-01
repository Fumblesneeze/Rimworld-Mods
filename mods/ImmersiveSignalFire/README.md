# Immersive Signal Fire

Package ID: `fumblesneeze.immersivesignalfire`

Immersive Signal Fire is a RimWorld 1.6 Neolithic communications mod. Colonists use a cold, purpose-built smoke hearth to request warriors from allied tribal or medieval settlements within ten world tiles—without opening the comms-console negotiation screen.

## Requirements

- RimWorld 1.6
- Harmony (`brrainz.harmony`)
- Dynamic Effects Forge (`blues.forge`)

Show Me Your Tools or Show Me Your Tools - Forked (`meathax.showmeyourtools`) is optional. When its supported runtime marker is present, every participant uses an in-sync smoke blanket during the signal.

## Player workflow

Research **Smoke signals** at Neolithic technology, then build the 2×2 signal fire from 60 wood. Its neat fresh-and-seasoned log stack is deliberately cold when idle: it provides no light, heat, cooking, or perpetual fire.

Right-click the hearth with a colonist—or with no colonist selected—and choose **Send a smoke signal...**. The submenu lists allied sub-industrial factions that currently own a settlement no more than ten world tiles away. If none qualify, it shows the inactive explanation `no allied neolithic or medieval settlements nearby`.

Choose a faction, then use the shared ritual-style setup window to choose one caller and up to two helpers. Signal quality is the group average of each participant's Melee and Social skills:

- Above 50%: warriors answer immediately.
- Above 30% through 50%: warriors answer after 600–3,600 ticks.
- From 15% through 30%: the smoke is noticed but misunderstood.
- Below 15%: the signal is not noticed.

The active signal lasts 600 ticks at normal speed. Dynamic Effects Forge renders the temporary flame and discrete dark SOS puffs that rise only while the synchronized blankets are raised. Every terminal active attempt consumes the single-use fire and leaves exactly 16 thickness of ordinary cleanable ash; cancelling while pawns are still gathering leaves the unused stack intact.

## Development

The owning OpenSpec change is [`openspec/changes/add-immersive-signal-fire`](../../openspec/changes/add-immersive-signal-fire/). Host suites are registered as `ImmersiveSignalFire.Unit`, `ImmersiveSignalFire.Harmony`, and `ImmersiveSignalFire.Defs`; final behavior is accepted only through `immersive-signal-fire.player-workflow` in a fresh isolated RimWorld process.

```powershell
$mcp = '.\tools\RimWorldModding.Mcp\RimWorldModding.Mcp.csproj'

dotnet run --project $mcp -- tool call mod_build `
  --arguments '{"packageId":"fumblesneeze.immersivesignalfire","configuration":"Release"}' -o json

.\scripts\Invoke-Tests.ps1 -Suite ImmersiveSignalFire -Configuration Release

dotnet run --project $mcp -- tool call e2e_run_start `
  --arguments '{"testId":"immersive-signal-fire.player-workflow","language":"English","timeoutSeconds":300,"dryRun":true}' -o json
```

Player-visible asset measurements, candidate hashes, prompts, and normalized package hashes are recorded in [`Art/asset-provenance.md`](Art/asset-provenance.md).
