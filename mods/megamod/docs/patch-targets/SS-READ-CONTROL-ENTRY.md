# SS-READ-CONTROL-ENTRY: neutral settings control entry

## Ownership and traceability

Owner `fumblesneeze.megamod`, S141 SS-06 in the
[settings search spec](../../../../openspec/changes/specify-megamod-settings-search/specs/megamod-settings-search/spec.md).
This prefix family is implemented by `SettingsReadControlGuard` with concrete native callbacks in
`SettingsReadControlGuards`; every bound target has its own `<targetId>.prefix` candidate and
receipt. Target IDs/signatures are listed in the companion exit contract and silent-call documents.
Only existing native controls are intercepted; there is no patch of owned code or package whitelist.
Harmony is needed because native rendering APIs have no general observational invocation mode.

## Target and minimal predicates

Require the exact declared native callable, static/instance/public/private shape, parameter/ref and
return types recorded for each binding. Require a concrete synchronous managed body, no open
generic target or generic declaring type. Only explicitly closed int/float numeric value-type
targets may be generic, after actual provider closure-isolation proof. Callback declaring types
are concrete nongeneric types; paired prefix/finalizer share the same state contract/declaring type.
No native argument is changed by this family. Capture input before later prefixes and let native
drawing run on the hidden backend; finalizers restore outward values. This consumes callable and
ordering facts, not a complete body fingerprint. Unrelated body edits/foreign owners are allowed.

## Composition, ordering, and invalidation

Use existing C01 safe publication/ownership inspection. Prove the acquired callback is the first
effective outer prefix, including current repatches; priority is insufficient. Prefix factories or
unreadable consumed ordering fail closed without executing factories or foreign attributes.
Paired targets also require SS-READ-CONTROL-EXIT. Ordinary foreign bodies/postfixes/finalizers are
allowed when the needed entry/exit order is known and they do not share the pair's private state.
Reject duplicate registrations of a paired callback and distinct concrete prefix/postfix/finalizer
callbacks that consume the same declaring type's state slot. Unresolved postfix factories also
refuse paired admission: their returned callback may share that state, and inspection must not
execute them to guess otherwise. State-free concrete callbacks remain compatible; prefix-only
guards do not consume finalizer/state guarantees. Mid-callback arbitrary repatching and unrelated
foreign side effects remain explicit limits. No speculative replay of transpilers is performed.

## Admission, failure, and diagnostics

Acquire only own prefix/finalizer slots through C01; initialize idempotently and roll back attempted
members in reverse on failure. Preserve foreign owners and other owned patch kinds. A missing
guard disables automatic read entry; ordinary native operations outside a read remain unchanged.
A reached unavailable guard throws a fixed latched read failure. Whole control/action groups must
be current before dispatch. No production hidden callbacks are registered by a component alone.
Each skipped enabled candidate receives one C01 bounded warning with patch/spec/target/reason and
ordinary-page fallback. Failure/status logging contains no foreign displayed settings values.

## Verification matrix and final observation

Focused `Megamod.Harmony` tests must prove ordinary and scoped invocation, first/last ordering,
caught failures, exception restoration, compatible owners, pair rollback, removal/repatch and
closed numeric sibling independence, duplicate/state-sharing callback refusal, and disabled routing
after failed owned rollback. Native control fixtures must show preserved text/numeric/ref/
scroll state and subsequent real editing on the reviewed build. The current component passes 40
focused host checks and 31 native steps with five personally inspected captures. Original text and
slider preservation plus subsequent real edits are directly observed; the other native assertions
are supporting diagnostics. See local discovery/values/root/verification.md. The integrated reader,
all remaining native controls and full S141 are not accepted by this component workflow.
