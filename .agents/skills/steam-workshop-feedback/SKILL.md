---
name: steam-workshop-feedback
description: Read or synchronize comments from published Steam Workshop mods, triage only actionable bug reports, compatibility issues, and feature requests, and maintain the repository-root feedback.md human-review ledger. Use when importing Workshop comments, checking published mods for player feedback, deduplicating reports, recording a human promote/drop decision, or promoting accepted feedback into OpenSpec.
---

# Steam Workshop Feedback

## Purpose

Turn actionable player comments into a small, auditable backlog without treating triage as product approval. Store concise spec-like summaries in root `feedback.md`; do not copy the original comment body. A human decides whether each pending item is promoted to OpenSpec or dropped.

This workflow is read-only on Steam. Never reply to, moderate, delete, award, or otherwise mutate a Workshop comment while collecting feedback.

## Resolve the published mod exactly

1. Read root and local `AGENTS.md` files.
2. Resolve the mod from its repository release manifest and use the manifest's exact package ID, display name, and Steam `PublishedFileId`. When asked to check all published repository mods, enumerate only manifests with a declared publication identity and process them in package-ID order.
3. If the release-manifest tooling has not been implemented yet, accept an exact Workshop item URL or item ID supplied by the user. Do not search by display name and guess which item is theirs.
4. Verify that the Workshop page identifies the expected mod before reading its comments. Stop on an ownership, package, or item-identity mismatch.
5. Do not require a publisher API key to read public comments. Prefer the public Workshop item page. Use an already authenticated browser session only when Steam hides required pagination or comment metadata from an anonymous request. Never persist session cookies or credentials in the repository.

At the time this skill was authored, Steam's documented Workshop Web APIs did not provide a public comment-list method. Do not invent one. Recheck Valve's official API documentation before replacing page reading with an API. If Steam changes the page or prevents a complete read, report the exact coverage limit instead of claiming the item was fully synchronized.

## Read and identify comments

1. Traverse the comment pagination from newest to oldest through the last accessible page on every import. Ignored comments are deliberately not recorded, so a high-water mark based only on `feedback.md` would be incomplete.
2. Capture the Workshop item ID, stable Steam comment ID, canonical comment/permalink when exposed, author display name, public author profile URL when exposed, and Steam's comment timestamp.
3. Normalize an exact timestamp to ISO 8601. If Steam exposes only a calendar date, store `YYYY-MM-DD`; do not invent a time or timezone. If Steam exposes only relative time, resolve it against the retrieval time and mark the stored date `approximate`.
4. Use `steam-workshop-comment:<PublishedFileId>:<CommentId>` as the source identity. For two independent actionable requests in one comment, append `:1`, `:2`, and so on.
5. If no stable comment ID or equivalent canonical source identity is available, do not import automatically. Report that deduplication is unsafe and request human direction.
6. Compare source identities before classifying. Never create a second ledger item for the same atomic comment outcome. If a known comment was edited to add material context, update only its summary or reproduction context and preserve its checkbox and human decision.

## Triage narrowly

Add an item only when the comment contains at least one actionable outcome in one of these categories:

- **Bug** — shipped behavior is wrong or broken, including crashes, errors, regressions, data loss or duplication, unusable UI, or gameplay that contradicts the advertised behavior.
- **Compatibility issue** — behavior breaks or changes only with another named mod, DLC, RimWorld version, load order, or optional package chain. Record only names and versions the reporter actually supplied.
- **Feature request** — the reporter asks for a new or changed player-facing capability, setting, content item, integration, or workflow.

Ignore without adding a ledger entry:

- thanks, praise, ratings, reactions, jokes, memes, and social conversation;
- questions or support requests with no identifiable defect or requested product change;
- arguments about other commenters;
- spam, advertisements, and unrelated content;
- an exact repeat by the same author with no new context; do not create a second entry for it.

Do not record ignored comments, rejection rationales, or raw comment excerpts. The ledger is an actionable backlog, not a mirror of the comment section.

When one comment contains independent outcomes, split it into atomic entries. When it is actionable but underspecified, record the smallest truthful behavior and explicitly say which reproduction detail is unknown. Do not infer mod versions, root causes, technical solutions, severity, or acceptance criteria the player did not provide.

## Write a spec-like summary

Use one of these shapes:

- Bug: `Given <reported context>, when <player action>, <expected behavior>; instead <observed behavior>.`
- Compatibility: `With <reported mod/version/chain> active, when <player action>, <expected integration or fallback>; instead <observed conflict>.`
- Feature: `The mod should <requested behavior> when <condition>, producing <player-observable result>.`

Keep the summary implementation-neutral. Preserve player-important constraints, affected things/buildings/jobs, and reproduction context. Do not prescribe Harmony patches, Def changes, APIs, or a specific design unless the comment itself is explicitly requesting that public contract.

## Maintain `feedback.md`

Create the root file from the repository template if it is absent. Allocate the next monotonically increasing `SWF-NNNN` ID; never recycle IDs or renumber existing records.

Every entry must use this shape:

```markdown
- [ ] `SWF-0001` **Bug — Short player-facing title**
  - Mod: Immersive Chefs (`fumblesneeze.immersivechefs`; Workshop `1234567890`)
  - Category: Bug
  - Reported by: [Display name](https://steamcommunity.com/...) on 2026-08-12
  - Source: [Workshop comment](https://steamcommunity.com/...#comment_...) <!-- steam-workshop-comment:1234567890:9876543210 -->
  - Summary: Given ..., when ..., the mod should ...; instead ...
  - Human decision: Pending.
```

Requirements:

- Store author and date for every report. Use the visible display name exactly, without collecting unrelated profile data.
- Link the author only when Steam exposes a public profile URL; otherwise store the display name as plain text.
- Keep the source link and hidden identity marker, but not the original comment body.
- Prefer the exact comment permalink. If Steam exposes an ID but no permalink, link the Workshop item's comment section and retain the exact ID in the hidden marker.
- Keep pending entries unchecked and set `Human decision: Pending.`
- Preserve all existing human decisions and wording outside the minimal changed records.
- Never sort or rewrite the entire file merely to append feedback.
- Remove the `No actionable Steam Workshop comments have been imported yet.` placeholder when adding the first item.

For a duplicate report of a pending item, append:

```markdown
  - Also reported by: [Display name](https://steamcommunity.com/...) on 2026-08-13 ([comment](https://steamcommunity.com/...)) <!-- steam-workshop-comment:1234567890:9876543211 -->
```

Do not change the summary unless the later report supplies a material reproduction constraint. A duplicate of an already-promoted item may be appended without reopening it. A new report matching a human-dropped item becomes a new pending entry that references the dropped ID, so the human sees the new evidence.

## Protect the human decision boundary

Import and triage do not authorize acceptance, rejection, implementation, or an OpenSpec edit.

Only check an item after the user or another explicitly identified human reviewer gives a decision:

- **Promoted:** create or update the requested OpenSpec change through the applicable OpenSpec skill. Only after that artifact exists, check the item and write `Human decision: Promoted to OpenSpec <linked change-id> on YYYY-MM-DD.`
- **Dropped:** require a concise human-supplied reason, check the item, and write `Human decision: Dropped on YYYY-MM-DD — <reason>.`

Never delete a resolved record. Never interpret silence, an agent recommendation, an implementation commit, or a matching existing task as human approval. If the human asks for analysis but gives no decision, leave the checkbox unchecked.

## Validate and commit

Before committing:

1. Inspect the diff and confirm it contains no raw comment bodies, credentials, cookies, or unrelated edits.
2. Confirm every new item has a unique `SWF` ID and source identity, one allowed category, mod identity, author, date, source link, spec-like summary, and pending decision.
3. Confirm every checked item has either an existing linked OpenSpec change or an explicit drop date and reason.
4. Confirm ignored comments produced no records and duplicate pending reports were consolidated.
5. Report the exact Workshop items and pages covered, plus any inaccessible/truncated range.
6. Stage only `feedback.md` for a routine import or human-decision update. Commit it with a focused message such as `feedback: triage Immersive Chefs Workshop comments`. Do not push unless asked.

When initially adding or changing this skill, validate it with the repository's skill-validation workflow and commit the skill and ledger template as one focused repository-maintenance change.

If no actionable new comments exist, do not create an empty commit. Report that the checked range produced no ledger changes.
