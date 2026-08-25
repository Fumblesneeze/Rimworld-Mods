# Hospitality + Ideology Patch

Package ID: `fumblesneeze.guestbedgizmo`

This small RimWorld 1.6 compatibility mod moves Hospitality guest beds into RimWorld's ordinary bed-owner command. A selected supported bed offers one menu with **For colonists**, **For prisoners**, **For slaves**, and **For guests** instead of Hospitality's legacy standalone guest toggle.

## Requirements

- RimWorld 1.6
- Harmony (`brrainz.harmony`)
- Ideology
- Hospitality Continued (`Orion.Hospitality`)

Harmony must load first. The patch is absent-safe and activates only when the supported Ideology and Hospitality packages and runtime shapes are present. If an upstream shape changes, it fails closed with one bounded warning rather than partially replacing bed controls.

## Player behavior

Select a humanlike bed and use the normal owner command. Choosing **For guests** converts the selected eligible beds to Hospitality guest beds; choosing a vanilla owner type converts affected guest beds back through the native owner workflow.

The integration preserves Hospitality's rental, attractiveness, claiming, and visit behavior. Vanilla prisoner-room validation, mixed selections, room-wide owner changes, cancellation, and confirmation remain native behaviors. Medical, baby, animal, and otherwise ineligible beds keep their existing controls.

The mod has no settings or saved data and is safe to add or remove from an existing save. Removing it restores Hospitality's own legacy guest-bed command.

## Development

Use the repository MCP/CLI from the repository root:

```powershell
$mcp = '.\tools\RimWorldModding.Mcp\RimWorldModding.Mcp.csproj'

dotnet run --project $mcp -- tool call mod_build `
  --arguments '{"packageId":"fumblesneeze.guestbedgizmo","configuration":"Release"}' -o json

dotnet run --project $mcp -- tool call test_run `
  --arguments '{"suite":"GuestBedGizmo.Unit","filter":null,"configuration":"Release"}' -o json

dotnet run --project $mcp -- tool call e2e_run_start `
  --arguments '{"testId":"guest-bed-gizmo.unified-owner-menu","language":"English","timeoutSeconds":300,"dryRun":true}' -o json
```

The owning contract is [`openspec/changes/add-guest-bed-gizmo`](../../openspec/changes/add-guest-bed-gizmo/). Workshop copy and presentation sources live under [`Release/workshop`](Release/workshop/); the universal release profile is [`Release/release.json`](Release/release.json).

