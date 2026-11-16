## Context and owner

Owner: repository tooling `RimWorldModding.Mcp`. Reuse the existing immutable preparation/admission/worker/recovery pipeline, recording `package` or `page` in each new plan. Old admitted plans remain recoverable.

## Decisions

- `release_package_upload(packageId)` prepares and uploads only package content, with the authored package change note. Existing page title, description, visibility, tags, previews, and relationships remain unchanged. A first upload sets only a title and Private visibility alongside content and backfills the exact ID to the profile and About identity. Its identity-only commit remains permitted; image commits/pushes are agent work.
- `release_page_update(packageId)` requires an existing ID, validates and freezes presentation, then updates title, description, primary preview, slideshow, tags, visibility, links and relationships as one user action. It neither builds nor uploads product content and does not consume the package change note.
- Lower-level `release_prepare` defaults to package preparation; an explicit action selects page preparation. `release_publish` consumes the exact action-bound plan. The old preview-sync public operation is retired to prevent the former two-step page flow.
- Inline media have a separate `description-images.json` inventory mapping repository paths to `https://raw.githubusercontent.com/<owner>/<repo>/<40-hex-commit>/<path>` URLs. Page preparation verifies committed bytes and downloads the exact remote bytes with bounded HTTPS requests and no redirects. Description image tags must match that inventory exactly. Missing, stale, unpushed, mismatched, or undeclared images fail before Steam mutation.
- Slideshow order remains in `presentation.json`; its card inventory can include cards absent from the slideshow. Changing slideshow slots never changes inline URLs.
- Use the existing package lease and durable submit recovery for both actions. Verify only the changed action's desired fields plus preservation of the other action's fields. Do not retry an uncertain submission under a new plan.
- Verification on Immersive Chefs updates existing item `3782589902`, retaining published visibility and existing artwork. No new showcase is authorized; human-deferred media stay excluded.

## Build and publisher runtime separation

Publication stages the already built canonical positive-allowlist package; `mod_build` remains the explicit build/install action. A publication trigger continues to assert that this exact product build has been tested. Package validation inspects the package itself rather than requiring the currently installed game to match the product target. Both publication actions can use any supported isolated Gateway runtime with the exact owning Steam account; they do not load or retarget the product. Immersive Chefs' retained DLL matches the accepted 1.6 build `0937C7721341DDA4B925A75B3107A0FD1AC0ECF404F9FF48E1CB408BB240E147`; no 1.7 compatibility claim is made.

## Verification

Focused host RED/GREEN covers action dispatch, disjoint Steam setter requests, first-draft rules, independent image inventories, hash/URL rejection, and admission/recovery. After independent code review, upload the current Immersive Chefs package and update its page through fresh isolated Gateway processes, inspect the actual Workshop page and retain before/after remote evidence and process cleanup.
