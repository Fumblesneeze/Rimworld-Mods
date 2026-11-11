# MSG-BAR-ADORNMENTS: native bar adornment eligibility

## Ownership and traceability

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, S071 message placement.
This implements a participant in the existing `MSG-BAR-PREPARED` transpiler, not another patch.
It supports the bounded automatic path which declines currently visible weapons and group frames.
It does not measure those adornments or close their positive clearance obligations.

Contracts: `openspec/changes/specify-megamod-message-placement/specs/megamod-message-placement/spec.md`,
automatic placement follows visible content and unsupported contributions preserve manual placement;
`MSG-BAR-LAYOUT-PREP.md`, actual effective geometry and consumer relationships.
Implementation: `Source/Features/MessagePlacement/BarBounds/Harmony/MessageBarAdornmentGuard.cs`,
composed by `MessageBarGeometryGuard`. Tests: `tests/Megamod.Harmony/Features/MessagePlacement/BarBounds/MessageBarAdornmentGuardTests.cs`.

The inspected input is RimWorld1.7.5005 rev1082 `ColonistBar.ColonistBarOnGUI():void`, public instance,
non-generic. Original IL is retained under `artifacts/Megamod/P0-B/automatic-1.7.5005/il/`.
That identity is research evidence, never an admission fingerprint.

## Minimal predicates

The existing geometry owner supplies the actual incoming body, captured preparation boundary,
entries local, admitted entry/rectangle locals and the actual `DrawColonist` call.

1. The captured frame flag has a distinct, unmodified local with no escaped address. Native frame
   drawing consumes `this.drawer` and the admitted entry group after `flag & capturedFrames`.
   Forcing that predicate's false edge makes the draw unreachable. The particular group-change flag
   value is immaterial when capturedFrames is false. Frame interaction is independently unreachable
   through the false capturedFrames gate; its drawer/group argument comes from the prepared entries.
2. Weapon preparation obtains `entry.pawn.equipment?.Primary`, preserving the native null merge and
   its actual local store. No alternative branch may supply that store or enter the chain midway.
3. Visibility is `Always(2) || (WhileDrafted(1) && entry.pawn.Drafted)`, followed by nonnull primary
   equipment and its `def.IsWeapon`. The inspected native boolean merge and equivalent optimized
   short-circuit compiler shape are admitted. Failed gates cannot enter the icon path before renewed
   preparation. This does not infer eligibility from a method name or a count of matching calls.
4. The outward `ThingIcon` uses the primary equipment and the native rectangle
   `(portrait.x, portrait.y + portrait.height*1.05, portrait.width, portrait.height).ScaledBy(.75)`.
   Its remaining native arguments are `1f`, a default nullable rotation, `true`, `1f`, `false`.
   The host fixture uses that same seven-argument shape. Incoming call/argument changes decline
   automatic geometry. The core geometry proof already guards the native entry/rectangle producers;
   the participant rejects later relevant local writes and primary/frame-entry address escapes.
5. Every relevant call is unique. Matched expression/store intervals reject external branch entry,
   and bounded graph reachability checks relevant gates/dominance across native loops. Exception
   regions, tail transfer, unresolved/duplicate labels and bodies over10,000 instructions decline.
6. The containing geometry guard inventories direct calls in the supplied native drawer,
   Widgets (from the actual ThingIcon binding) and explicitly bound GUI drawing families. A call
   consuming Rect/ref Rect, named Draw*, or changing the known GUI matrix/pivot transform needs its
   proved native contribution; an added unproved
   consumer declines automatic geometry. Native binding includes GUIUtility for its scale/rotate
   operations. Exact admitted native calls and the supplied rectangle
   scaling helper remain allowed. Parameterless observations remain eligible. This finite boundary
   does not recursively certify arbitrary external helpers or their global side effects.

Offsets, branch short forms, compiler local numbering and NOPs are not identities. Unrelated work
outside matched expressions is permitted. This is a target-local proof, not a general IL interpreter
or a guarantee against arbitrary foreign global side effects.
The geometry predecessor graph removes unreachable sources before proving producer dominance,
so an inserted unreachable NOP after an unconditional branch cannot invent a bypass path. Reachable
alternate branches still require the same actual producer and protected store.

## Publication, failure and diagnostics

The participant returns original operation indices to the existing geometry guard. Its required
operations are unioned into the prepared owner's single incoming generation and C01 publication
receipt, including eligibility getters/fields/constants and outward calls. No foreign transpiler
is replayed for inspection. Relevant inner rewrites decline geometry through that same receipt;
later composition revokes old authority and a fresh supported generation may recover.

Failure returns a bounded reason and no geometry operations. The owner preserves the incoming
native body and its separately admitted manual handoff. No foreign patch is removed; no runtime
state is manufactured. The runtime's existing unavailable-geometry status/diagnostic path remains
responsible for player explanation. A geometry guard alone never authorizes drawing or placement.

## Verification and acceptance state

The honest Harmony fixture patches a scoped host target with no Verse/loaded mods/Def databases.
It exercises inner eligibility rewriting, ordinary condition rewriting, the native boolean merge,
frame/weapon branch changes, local mutation/address escape, unrelated effects and NOPs. Every test
removes its exact owned patches in cleanup and checks retained native/manual behavior where relevant.
The focused RED/GREEN ledger is retained under `artifacts/Megamod/P0-B/messages-adornments/`.

This implementation is **unverified natively**. Root must review the integrated build, inspect its
native input admission and personally observe actual messages with no-visible-adornment clearance,
then native weapon/frame settings changing the bounded path to manual fallback. Final evidence must
include build/mod-list identity, player actions, before/after screenshots, logs, configuration hashes
and cleanup. Full visible-adornment clearance, other S071 interactions and S001 remain separate,
unfinished acceptance obligations.
