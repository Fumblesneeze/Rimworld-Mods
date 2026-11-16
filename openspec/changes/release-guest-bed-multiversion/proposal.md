## Why
The user requested a combined Hospitality + Ideology Patch release for RimWorld 1.6 and 1.7 on 2026-11-16.

## What Changes
- Compile the existing shared product source separately for both major/minor targets.
- Adapt only demonstrated game or Hospitality interface differences, preserving native owner-menu behavior.
- Verify the supported and absent-prerequisite player workflows on both targets and publish one combined package, followed by its separate Steam page update.

## Owner
Mod: Hospitality + Ideology Patch (`fumblesneeze.guestbedgizmo`).

## Capabilities
### New Capabilities
- `guest-bed-version-compatibility`: Native guest-bed owner integration across the declared major/minor matrix.

## Impact
Uses the existing repository matrix and split publishing operations. Supersedes the original change's 1.6-only scope for this release. Stable package/Workshop identity, dependencies, save behavior, localization and existing artwork remain unchanged.
