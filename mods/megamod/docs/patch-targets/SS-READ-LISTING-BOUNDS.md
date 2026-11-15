# SS-READ-LISTING-BOUNDS: read offscreen standard-listing options

Owner `fumblesneeze.megamod`, S141 SS-06 in the
[settings search spec](../../../../openspec/changes/specify-megamod-settings-search/specs/megamod-settings-search/spec.md).
This is the target contract for the pending getter integration. The original-instruction checker
and passive generic-family inventory are prerequisites, not an installed getter guard.

## Target and consumed behavior

Exact target: public instance nonvirtual nongeneric
`Verse.Listing_Standard.get_BoundingRectCached():System.Nullable<UnityEngine.Rect>`, with no arguments,
declared on Listing_Standard. During a neutral read, return the native null sentinel without
executing the getter. Outside that scope, preserve ordinary native execution. A postfix alone is
insufficient: the original can invoke an unknown scroll delegate and mutate its frame/cache state.
No private bounds-field mutation, fabricated large rectangle or delegate call is required.

Admission requires a proved null-to-draw relationship in every direct native consumer: Label,
LabelDouble, the full RadioButton overload, both direct CheckboxLabeled overloads,
CheckboxLabeledSelectable, ButtonText, ButtonTextLabeledPct, ButtonImage, both generic numeric
listing methods, IntRange, IntEntry, LabelCheckboxDebug, ButtonDebug, ButtonDebugPinnable and
CheckboxPinnable. Bind exact declarations/signatures and exact drawing operands. These 17 consumers
are native prerequisites; they do not define a list of eligible foreign settings pages.
The binder must also check the declaring native class for additional direct getter consumers;
an unfamiliar native consumer is an unproved prerequisite, not silently omitted from this gate.

The nullable getter must consume the current receiver. Its null edge must reach the same required
drawing stages as successful overlap, bypassing Value and Overlaps even through later backedges.
Preserve row/projection provenance and relevant aliases. Account for sequential double-label and
label/button calls, legitimate long/short-label alternatives and native pre-cull rectangle edits.
Generic widget operands retain the consumer's own type parameters/arguments. Unknown or changed
consumed relationships refuse admission; unrelated work, local renumbering and intact producer
reentry do not by themselves require refusal. No whole-body hash is an admission predicate.

## Composition and lifecycle

Use the existing first-prefix/last-finalizer control-guard mechanism. An acquired prefix skips the
native getter and supplies null; its paired finalizer retains null through intervening result
postfixes. Outside a read it does nothing. Exceptions remain failures rather than partial captures.
Preserve acquired state independently of later scope/admission changes. Require paired-state and
ordering checks, and roll back only patches acquired by this owner through C01.

Original-body semantics must be tied to coherent effective-composition evidence. Existing owned
value/cache wrappers can add exception regions outside that body; their concrete contracts must
be proved rather than treating every wrapper as either safe or incompatible. Transformed consumers
need their actual published body, without executing foreign transpilers or factories as probes.
For generic consumers, inspect the whole declaration's publication family, including new siblings;
an empty inventory or unchanged single closure is not a semantic proof. Relevant republication
invalidates the old capture permanently, while unrelated publications do not. Unsupported consumed
provider/body shapes remain explicit gaps, never a numeric-type or mod eligibility whitelist.

## Failure and scope

Do not enter hidden reading while any required consumer or getter boundary is unproved. A reached
failure latches the read's refusal; preserve normal page access and emit bounded C01 diagnostics.
Do not register automatic discovery until its other input/state/value prerequisites are integrated.
Direct custom uses of the property and arbitrary foreign callback effects are not transactional.
Custom virtualization can still withhold rows and must remain an honest coverage gap. Unculling
can change native gap placement; reveal must resolve fresh visible layout instead of treating
captured rectangles as persistent control identity.

## Pending native verification

The focused English workflow must use a real button to read an offscreen native option, observe
the found label/help and unchanged viewport/value, scroll normally to the original control, edit
it and personally inspect the causal captures. Supporting probes cover all 17 consumers, cold
throwing scroll delegates, warm cache preservation, null/default bounds, result replacement,
callback exceptions, compatible composition and relevant repatch invalidation. Ordinary execution
and exact-owner cleanup must still work afterward. Retain reviewed build/process/config identities
and logs under local `discovery/listing/` evidence. No native acceptance is claimed by this document.
