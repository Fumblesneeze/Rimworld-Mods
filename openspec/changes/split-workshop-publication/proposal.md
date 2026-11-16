## Why

Package publication currently also updates page copy, and inline images depend on a separate Steam preview upload. This couples unrelated changes and leaves stale CDN identities.

## What Changes

- Expose separate package upload and single-action page update operations.
- First package upload creates a Private draft and persists its Workshop ID for later page updates.
- Declare slideshow previews independently from inline images. Inline images use immutable GitHub raw-content URLs after the agent commits, pushes, and verifies the mirror; tools never commit or push image changes.
- Retain immutable plans, exact item ownership, bounded recovery, and remote verification.

## Owner

Repository tooling: `RimWorldModding.Mcp`, including its Gateway Steam adapter.

## Capabilities

### New Capabilities

- `split-workshop-publication`: Independent package and page transactions with GitHub-hosted inline media.

## Impact

Release tooling, Gateway publishing adapter, tests, operator documentation, and Immersive Chefs presentation declarations. No gameplay or raster bytes change. This explicitly supersedes the earlier coupled publish and Steam-hosted inline-image requirements.
