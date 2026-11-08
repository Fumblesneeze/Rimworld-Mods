# Drug-policy additions and original-mod scope

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, RimWorld1.7 beta.
User decisions recorded2026-11-08. This refinement note is not an implementation or acceptance claim.

## Additions

| ID | Requested reference | Intended work | Current status |
| --- | --- | --- | --- |
| S138 | Only If 'Tolerance' Below in Drug Policy | Independently specify its observable tolerance condition and native drug-policy controls, then compose with S029 maintenance and existing scheduled-use restrictions. | [Research](2026-11-08-drug-tolerance-research.md) and [draft](../../../../openspec/changes/specify-megamod-drug-tolerance/) complete; route/state/UI prerequisites remain. |
| S139 | Safe Drug Policy Generator | Take the idea, not the code. Independently design generation around our policy fixes and preserve existing choices and usable policy state on failure. | [Research](2026-11-08-safe-drug-policy-generator-research.md) and [draft](../../../../openspec/changes/specify-megamod-drug-policy-generation/) follow S138; calculation and atomic-creation prerequisites remain. |

The user's report that the generator historically broke things is motivation for independent design,
not proof of a particular current source defect. Do not translate its implementation or assume its
calculations are safe. Research observable behavior and native drug mechanics, distinguish known
results from assumptions, and record unresolved questions for later. “Safe” must name the conditions
it actually covers; do not imply protection from every drug interaction or modded side effect.

Both entries belong to P0-C alongside S029. Coordinate later with S024 scheduled restrictions and
S124/S134 automatic treatment; do not introduce another policy store, dose authority or internal
patch stack. These additions do not reopen accepted user choices elsewhere. No new bottom tab is
implied: policy-specific controls remain in the policy workflow, rather than global mod settings.

## Original-mod compatibility decision

The user first excluded original-mod compatibility work except public APIs, then explicitly removed
that exception too. **Do not investigate, implement or verify drop-in/API/binary/package/save parity
with replaced originals or their consumers.** In particular, consumer package-ID checks are not a
reason to impersonate the source package or add a compatibility facade. Source behavior remains a
research reference, and deviations/omissions must still be documented.

The user explicitly retained a **startup warning** when a known original is active. Suppression is
**best effort**: apply the ordinary effective feature-off settings path as if the corresponding
feature had been turned off. Do not permanently overwrite the player's saved preferences. Do not
claim this is a full compatibility test, that all upstream overlap has been discovered, or that the
combination is supported. Do not modify the original mod. Inactive installed sources do not trigger
this path; unrelated owned features stay enabled where their ordinary dependencies permit.

This decision supersedes earlier requirements to preserve consumed original APIs, inspect their
consumers, build facades or run original-mod coexistence matrices. It does **not** remove semantic
game-target admission, honest test tiers, owned save continuity, or checks for actual third-party
integrations the feature uses. Those are separate from compatibility with the replaced original.

## Follow-through

- Add S138/S139 to the queue, replacement index and P0-C allocation; research one after the other.
- Give each its own proposal, observable specification, technical design and tasks.
- Reconcile foundation instructions, active feature requirements, shared plan and coverage with the
  compatibility decision. Retain historical research as provenance, clearly superseded for scope.
- Review and validate documentation. Implement only from the resulting owned contracts, with focused
  tests and orchestrator-run native workflows. No gameplay task closes from this note.
