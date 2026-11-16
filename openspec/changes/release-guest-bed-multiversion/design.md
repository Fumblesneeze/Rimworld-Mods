## Owner and scope
Owner: Hospitality + Ideology Patch (`fumblesneeze.guestbedgizmo`). Support RimWorld 1.6 and 1.7 beta in one release using separately compiled version payloads and shared source. Only major/minor game selection is permitted; no revision, build, depot-manifest or game-file hash pins.

## Decisions
- Reuse VersionedGame build imports and the repository's combined-package operations. Native test bundles reference the frozen product assembly and must not rebuild it.
- Preserve one four-choice bed-owner menu, Hospitality's native swaps/rental controls, native prisoner-room rejection and deferred confirmation/cancellation. Preserve absent-safe activation and consumed-interface guards; add compatibility branches only when inspected code or execution demonstrates a difference.
- Use installed Hospitality Continued's own 1.6/1.7 assemblies as read-only runtime inputs. The product has no Hospitality assembly reference and ships no external or Gateway binaries.
- Keep Harmony as the product's hard mod dependency. Ideology and Hospitality remain activation prerequisites/load-after entries; Steam retains its required Hospitality/Harmony items and Ideology DLC declaration.
- Update existing Public Workshop item 3789536584, preserving title and reviewed raster bytes. Description has no inline image URLs; no new image-hosting workflow is required.
- No balance, research, costs, translations, persistent state or new compatibility claims are introduced.

## Acceptance
After focused tests and independent review, run the existing unified-menu and edge workflows on both targets: inspect ordinary four-choice menu, multi-bed guest conversion, rental control preservation, cancelled owner removal, accepted native prisoner conversion including room siblings, outdoor rejection and unsupported-bed filtering. Exercise absence behavior separately on each target. Personally inspect exact-process screenshots and logs; retain source/package identity, ordered mod lists, config hashes and cleanup. Publish the accepted combined package and update the page through separate main-menu operations.

## Demonstrated differences
Both target assemblies compile from shared product source. Read-only inspection of the 1.7 game owner-menu callback and Hospitality's gizmo/guest-bed swap confirms the consumed shapes still match the existing guards. No product runtime branch is needed. The newer compiler's nullable annotations require explicit acknowledgments of the inspected non-null closure list and successful non-null replacement lookup; these annotations do not change executable behavior. Native acceptance remains a separate gate; compilation and inspection alone do not establish compatibility.
