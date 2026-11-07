## Context

Sole owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), 1.7 beta.
S130 specifies future work. [Research](../../../mods/megamod/docs/refinement/2026-11-06-dynamic-spacefarer-policy-research.md)
records author docs, complete selected1.7 assembly and native policy/filter UI contracts. No public
source/license was located. The source uses no Harmony: a static constructor mutates default tags
and a game component applies a once-per-Def addition to the first translated-name policy.

## Goals / Non-Goals

**Goals:** automatic additive vacuum-apparel policy maintenance, durable manual choices, reliable
policy identity and explicit safe adoption of an old save's selected policy.
**Non-goals:** perfect vacuum outfit planning, calculating total pawn resistance, automatic dressing,
new gear/stats, repeating the source's global tag mutation, package/save-key impersonation, multiple
simultaneously maintained policies or a generic policy automation framework.

## Decisions

### Preserve the observable criterion and additive result

The source accepts `apparel != null` and any `VacuumResistance` entry greater than0 in either
`equippedStatOffsets` or `statBases`. It does not sum entries, require full protection, inspect worn
quality/Stuff, compute pawn stats or require a current item instance. Owned classification preserves
that OR rule, adding finite-number/malformed-entry guards. This narrow data criterion belongs in
the spec because it controls which policies change. No mutable Def reference list becomes a
substitute for finalized loaded content; enumerate once per supported content/game-init cycle,
not per pawn tick or per map. Use a shared existing definition catalog if available; no Thing-index
duplication is needed. Runtime content injection outside admitted lifecycle remains unsupported.

Maintain native per-ThingDef filter allowances only. Preserve special filters, ranges, utility
allowances and every unrelated rule. This is user policy state, not an XML Def change. Do not mutate
`apparel.defaultOutfitTags` at runtime as source does. Native new-policy seeding reads those tags;
an admitted hook at the actual native Spacefarer creation supplies the same selected inclusion to
that policy directly. A blanket tag addition would also affect unrelated readers and needs its own
compatibility claim; no such required consumer was demonstrated. If future declarative tags are
actually required, use guarded additive XML Extensions and declare its dependency in that slice,
never a hidden static Def rewrite. Current design selects no XML modification.

Native `ThingFilter.SetAllow(ThingDef,true)` also recursively allows `thingDef.virtualDefs`; it is
not an independent bit setter. Derive the prospective affected set from the admitted native path
and current allowances with a bounded visited set, accounting for the setter's already-allowed early
return. Do not execute foreign callbacks speculatively to discover effects. Existing considered
exclusions, explicit preview declines and ineligible unrelated definitions have precedence. Hold
back roots whose mutation would violate any of those, expose the complete affected set/reason in
the preview and apply only the admitted nonconflicting batch atomically. Recompute when selections
change; do not wait for postcondition failure to reveal a known conflict. An eligible unconsidered
linked child successfully allowed by the batch is also recorded as considered, so a later exclusion
is not undone. Already allowed members need no write.

Held roots remain unconsidered unless explicitly declined; report them in the native policy view
and one bounded diagnostic, not every repaint. Reconsider only on the next supported load or
explicit review. Cyclic, ambiguous or unsupported provider-linked semantics fail safely to a held
proposal. Do not force conflicting choices by patching the global setter or writing its private
allowedDefs set. Snapshot/postcondition rollback remains necessary for unexpected drift, but is
not the user interaction for a known linked-choice conflict. This deliberately strengthens source
behavior, whose ordinary SetAllow can re-enable a linked child implicitly.

### Capture actual policy origin and persist identity safely

Native `OutfitDatabase.GenerateStartingOutfits` creates an Odyssey-only policy, clears its filter,
excludes tainted apparel and allows tagged Spacefarer or utility apparel. Capture the object at that
specific construction branch, after native defaults and before player interaction, then seed and
record candidates atomically. Do not search by `OutfitSpacefarer.Translate()`, list index or a fixed
ID. On load, the constructor's temporary generated policies must not be mistaken for deserialized
policies or mark loaded history as already processed.

Native `Policy` stores only integer id and label, and its load ID includes both; `MakeNewOutfit`
can reuse a removed highest ID. Use a small owned identity marker associated with each enrolled
policy, serialized with that policy through an admitted ExposeData hook, plus the game-owned target
reference/history. A native cross-reference alone is insufficient to promise rename/id-reuse safety.
Preserve identity across rename and save load; ordinary CopyFrom copies filter allowances, not the
marker or automatic tracking. Validate uniqueness/current membership before every maintenance
transaction. On legal deletion retire the target; on lost/duplicate/corrupt identity leave filters
unchanged and expose re-enrollment. No global/static target reference survives save switching.

One active policy per game suffices for the requested native Spacefarer behavior. Explicit release
or switch leaves prior policy history attached to that surviving policy for correct resumption.
No implicit tracking transfer from a copied filter, identical label or replaced object. Other
repository mods are not dependencies; the small policy-history state belongs to this feature.

### Persist consideration history, not a forced whitelist

For each enrolled policy record stable content identities of qualifying types already considered,
including entries declined at enrollment. Keep missing identities as inert history rather than
dropping null Def references on load as source does. Resolve against current content when needed;
the ordinary native filter stores active allowances. If an excluded type disappears and returns,
its retained considered identity prevents a second automatic allow. Native loss of a removed
allowed type's filter entry does not authorize us to infer/recreate its former permission; the
history guarantees no unsolicited re-add, not restoration of every foreign removed-content state.

Only previously unconsidered qualifying types enter an automatic addition batch. Already allowed
ones are recorded without a write; newly eligible ones are allowed once. Do not erase player edits
when a previously considered definition changes resistance, quality rules or provider order.
If a formerly unqualified definition first qualifies, it is new to this candidate history. No tick
refresh and no repeated callback which makes an excluded item reappear. Bound errors per content
identity; skip malformed isolated candidates without corrupting the remaining transaction.

### Existing saves need explicit, reviewable adoption

An old filter cannot reveal whether a disallowed type was intentionally excluded or never added;
source label matching also cannot distinguish renamed originals, same-name custom policies or
language changes. Do not invent that provenance. Offer a contextual action in the native apparel
policy editor to enroll the selected policy. Preview new eligible disallowed items, checked by
default for addition; let the player decline each. State that this is initial inclusion, not proof
of complete vacuum safety. Confirm records all considered candidates, applies only selected
nonconflicting allows and sets the single active binding. Explicit declines are considered; held
proposals are not, as specified above. Previously enrolled policies preserve their history.

Stage a copy of affected native filters and owned tracking, retain a pre-change recovery snapshot,
revalidate policy identity/content/selection, apply, then verify exact selected changes and untouched
ranges/special filters. Restore the snapshot on any failure; a changed preview is rejected for
refresh rather than silently updated under the player's selection. Normal save serialization
persists the committed result. No direct rewriting of normal save files/config or auto-save is
needed. New-game and load batches use the same transaction discipline without a user prompt when
already authorized by established tracking. No guessed imports from `dynamicSpacefarerAdded`.

This intentional old-save UI deviation protects manual choices; it is retained in DSP-Q2 for later
product review, not deferred as an unresolved implementation requirement. Whole-source save-history
migration remains a separate future contract requiring exact identity, backup and restoration.

### Contextual UI and composed authority

Target choice, enrollment, release and exclusions are policy gameplay state, so use existing
`Dialog_ManageApparelPolicies` context. No new mod settings row, bottom tab, debug tool or per-source
configuration loader. Common global/save inheritance is not applicable to an individual policy;
document this classification, not a special global-only setting. Reject mutation without Current.Game
or a still-valid selected policy and localize labels/explanations in seven languages.

S127 HUD opens the same native policy controls and reads actual assignments. S004 weather-ready
arrivals' vacuum outfit generation remains separate and must not interpret membership here as full
protection or high-tech availability. Future S132 Gear UI uses native allowances/actual apparel,
not another automatic dress rule. S034 formatting is reused for any displayed quantities. No
source-to-owned patches, map scan/cargo cache or independent pawn assignment authority.

### Target admission, optional content and original provider

Odyssey (`Ludeon.RimWorld.Odyssey`) is required for this module only; inspect actual required stat
and native creation branch before activation. No-DLC absence is expected inactivity, not a failed
patch. Candidate hook groups: native fresh-policy capture, policy identity serialization/deletion,
and contextual native policy UI. Each future hook needs a stable target document and executable
predicate for the consumed data flow/lifetime. Fresh generation must identify the Odyssey branch
and actual created policy/filter operation, not just a localized string or ordinal. Serialization
must preserve native fields and mode ordering; UI must preserve its selected policy and normal
filter editor. None is claimed implemented by this design.

Inspect original and required effective code, accepting compatible owners and unrelated edits.
Transpilers validate the incoming stream each invocation and leave it unchanged on failure;
never speculatively execute foreign transpilers. Handle skip-original prefixes and supported later
repatch invalidation. Failed groups roll back only newly added owned hooks and any staged policy
transaction, log one startup warning per skipped enabled patch/adapter and preserve normal policies.
Data corruption during load produces bounded runtime notice and no guessed mutation.

Detect original `asunib.DynamicSpacefarerPolicy` by package ID; withhold owned overlapping mutation
and controls with explanation. Its static Def changes/game component cannot be safely neutralized
by our policy code, so no foreign unpatch/setting edits or guarantee that its flaws are repaired.
No third-party consumer of source class/tag/history was demonstrated by the bounded binary/source
search. Default-tag readers found use unrelated Soldier/Worker tags or create costume definitions;
do not promise universal absence. Preserve native policy/filter contracts, not speculative facades.

## Risks / Trade-offs

- **Old-save ambiguity:** a one-time contextual review adds interaction compared with the source's
  silent first-load allow. It preserves unknown prior exclusions and avoids selecting a wrong policy.
- **Type-level criterion:** positive listed resistance does not guarantee adequate total protection.
  Explain this and preserve actual stats/body/quality/forced-apparel logic; no fabricated safety label.
- **Removed content/history:** retain inert identities to prevent unsolicited re-additions, but do
  not claim restoration of native filter choices lost when foreign definitions were removed.
- **Lifecycle identity:** native labels/IDs are mutable/reusable. Prove new versus loaded objects,
  copied policies, delete/recreate and language change before accepting persistence.
- **Third-party mutations:** provider code can alter policy state independently. Admit specific
  interactions or document unsupported cases; never overwrite unrelated denial/preferences.

## Migration Plan

No runtime/data changes now. Future fresh games auto-track the actual newly generated policy.
Existing untracked saves remain untouched until explicit enrollment preview/confirmation; no
automatic source-history import is selected. Native allowances survive removal, owned metadata
becomes inert, and no automatic dressing or policy replacement is performed. Future schema updates
need versioned validation and recovery snapshots; unrecognized/corrupt state fails to re-enrollment.

## Open Questions

DSP-Q1 source inventory is resolved. DSP-Q2 tracks the explicit enrollment deviation and any future
foreign-history import; DSP-Q3 effective beta identity/lifecycle hooks; DSP-Q4 content classification
and optional provider proof; DSP-Q5 exact native policy/filter/save/assignment acceptance. Questions
are recorded for later, not a request for the user to explain the existing mod.


## Reconciliation boundary

Existing-policy enrollment refers to policy changes/recovery within a colony started with megamod,
not original-mod save migration. S004 owns all-humanlike vacuum spawn protection, including supported
SOS2 environments; it may operate without S130/Odyssey policy maintenance. Reuse apparel facts while
keeping positive-declaration policy eligibility separate from effective complete-outfit protection.
No maintenance callback equips pawns, manufactures gear or reverses player exclusions.
