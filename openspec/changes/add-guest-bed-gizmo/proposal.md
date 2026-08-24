## Why

Hospitality predates RimWorld's unified bed-owner command and still adds a separate `For guests` toggle while disabling the newer vanilla selector on guest beds. This leaves one ownership mode outside the native `For colonists` / `For prisoners` / `For slaves` workflow and makes otherwise identical bed management inconsistent.

## What Changes

- Add a small RimWorld 1.6 compatibility mod that contributes `For guests` as the fourth option in vanilla's Ideology bed-owner menu while Hospitality is active.
- Remove only Hospitality's legacy standalone guest-bed toggle; retain its guest-bed rental, attractiveness, claiming, and visit behavior.
- Convert selected beds between Hospitality's guest-bed representation and the corresponding vanilla owner type through the native bed command.
- Detect the locally supported Hospitality package and runtime shape without compiling against or shipping Hospitality assemblies, and fail closed with one bounded warning if that shape changes.
- Load safely without Hospitality or Ideology and make no bed-gizmo changes unless both supported prerequisites are active.
- Package the mod with a concise Workshop description and a short Steam preview card derived from verified in-game behavior.

This change implements the gameplay and publishing behavior now; it is not a specification-only proposal.

## Capabilities

### New Capabilities

- `guest-bed-owner-selection`: Native bed-owner command integration, Hospitality shape guarding, selected-bed conversion, absence behavior, and live player-workflow verification owned by Guest Bed Gizmo.
- `guest-bed-publishing`: Product metadata, concise Workshop copy, and one behavior-specific Steam preview card owned by Guest Bed Gizmo.

### Modified Capabilities

None.

## Affected Mods

- Guest Bed Gizmo — `fumblesneeze.guestbedgizmo` — `mods/GuestBedGizmo` (new and sole owning mod).

Hospitality Continued (`Orion.Hospitality`) is a read-only optional integration input and is not owned or modified by this change.

## Impact

- Adds one product assembly, About metadata, keyed localization, focused host/in-game/E2E tests, and Workshop presentation files under the new owning mod.
- Requires Harmony at runtime and declares Ideology and Hospitality as optional load-after targets; both must be active before the compatibility patch activates.
- Patches vanilla's `Command_SetBedOwnerType` and the installed Hospitality bed-gizmo seams only after exact package/type/member validation.
- Does not introduce any dependency on `fumblesneeze.rimworlddevgateway` and does not copy Harmony, Hospitality, RimWorld, or test assemblies into the product package.
