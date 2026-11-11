# Harmony and optional-mod compatibility

## Patch only when necessary

Prefer, in order: XML/Def composition, public extension points or comps, a narrowly isolated adapter, then Harmony. A Harmony patch should bridge one engine boundary and delegate immediately into a testable domain module.

Before coding, classify Harmony as either a design-time fallback because the inspected version has no stable XML/public seam, or a runtime variant for explicitly supported external-mod surfaces. Never let XML/public-API and Harmony paths mutate the same behavior concurrently. A runtime selector must choose exactly one strategy, expose it in diagnostics, and pass duplicate-initialization tests.

- Use the gameplay mod's stable package ID as the Harmony ID.
- Let the RimWorld ModSdk compile against Harmony; do not ship a private `0Harmony.dll`.
- Prefer postfixes for observation and additive behavior. Use prefixes only when the original must be skipped or arguments/results must change.
- Avoid transpilers unless no stable call boundary exists. If unavoidable, match structural landmarks, fail closed with a clear warning, and add a live compatibility probe for the exact installed game/mod version.
- Keep patches idempotent and side-effect-free during discovery. Avoid hot-path allocations and recursive logging.
- Never touch Unity or Verse live state from network, timer, threaded-log, or worker callbacks. Queue it to the game main thread.

If an eagerly loaded product assembly references Harmony types, declare `brrainz.harmony` as a required dependency and load it before the owning mod. The external compatibility mod remains optional and must not appear under hard dependencies. Default external-API integration to string/reflection lookup; permit a compile-time adapter only when a documented loader boundary and an absent-mod Unity smoke prove it is safe.

## Optional integrations

Detect a mod by canonical package ID using `LoadedModManager.RunningModsListForReading`. Do not put an optional mod's type in a field, signature, attribute, generic argument, or eagerly JIT-compiled core method: that can trigger type resolution even while the mod is absent.

Resolve optional Defs by exact Def name only after the package is active. For C# APIs, keep reflection or a separately guarded adapter at the edge and validate:

- assembly and type identity;
- target method signature, declaring type, and return type;
- required fields/properties and lifecycle timing;
- graceful no-op behavior when any probe fails.

Validate the complete callable shape, not just method name and parameter count: declaring type,
static/instance kind, accessibility, generic state, exact parameter types, and exact return type. A
supported `void UseBucket()` must fail closed if a future version returns another type. Record the
guarded assembly identity/MVID/hash so the selected strategy is auditable.

An available object is not necessarily eligible. Call and honor the upstream mod's native assignment,
access, `PawnAllowed`, `Working`, power, network, guest/prisoner/animal, or equivalent reports both
when selecting a target and immediately before committing the operation. Thread the actual pawn and
context through the adapter; never bypass native restrictions because a fixture exists.

Log one bounded compatibility warning, not one per tick/job. Preserve the actual failed prerequisite
and affected content/target through the protected bounded sink; an exception type alone discards the
actionable reason. The composition owner must report a failed revalidation when the unavailable
callback will return before its own diagnostic. Use the existing attempted-generation gate to avoid
repeating that warning every pump. Expose adapter availability in diagnostics and write tests for
absent, present-supported, and present-incompatible cases.

## Patch hygiene

- Patch in a single initialization path and guard duplicate initialization.
- Preserve the original exception behavior unless the spec explicitly changes it.
- Avoid capturing mutable Verse objects beyond the tick/job in which they are valid.
- Use stable IDs or `ThingID` handles across queued work, then resolve and revalidate on execution.
- For job-giver and reservation changes, test cancellation, save/load, destroyed targets, drafted/downed pawns, map changes, and competing pawns.
- For compatibility claims, inspect the locally downloaded 1.6 assembly and prove the patch point in an isolated in-game run.
- When a patch temporarily suppresses or redirects global/component state for an inspect or render
  operation, use a Prefix plus Finalizer restoration path so exceptions cannot leak the suppression.
- Prove root-sequence patch ownership with the exact direct operation/patch classes. Descendant-wide
  XML/Harmony test searches can become ambiguous when another optional sequence is nested later.

## Semantic admission and composition failures

Apply these checks when a patch depends on consumed IL or on other patches' effective behavior.
An observation-only hook should still require only the callable and execution facts it consumes.

- Prove the producer-to-consumer relationship, not merely the presence/order of matching instructions.
  A policy store dominating a loop does not prove its value came from the preceding creation call:
  a branch may enter the store with a different object on the stack. Account for branch/switch entry
  into the consumed receiver/call/store sequence, including labeled no-ops, and for escaped locals.
  Cover addresses captured before the producing write and supplied argument addresses too: later
  alias writes can replace a protected value without another direct `ldloca` or `starg`. Exercise
  the bypass with valid executable fixture IL, not only a rearranged diagnostic instruction list.
  Reject only the unproved relationship; harmless aliases and unrelated edits should remain eligible.
  Exercise the alternate incoming path through a real owned-patch fixture before calling it guarded.
  Pair a rejection with a compatible counterexample: a branch entering at the start of an intact
  producer chain is not a bypass when it still executes that complete chain.
- Distinguish the original body, actual incoming transpiler stream and final provider composition.
  On providers with inner patches, being last among ordinary transpilers does not establish that a
  required call/store survives. Check relevant final effects with the existing supported adapter;
  do not replay foreign transpilers or invent a whole-method fingerprint to obtain this proof.
- Treat metadata inspection as a possible failure and side-effect boundary. The inspected 1.7
  Harmony provider's public `GetPatchInfo` path can construct foreign parameter attributes. For
  side-effect-free admission, use the existing shape-guarded publication reader where applicable
  (`mods/megamod/Source/Patching/Harmony/HarmonyPublication.cs`), not copied parsing or an assumed
  inert reflection call. Inspect one coherent snapshot for a decision. Filter acquired-owner
  registrations before projecting rollback receipts; unrelated factories need not be resolved or
  rejected when their ordering/effects are not consumed. Actual provider composition may execute
  factories; inspection must not execute them as probes.
- Verify how the installed provider retains callback identity. A provider that persists only
  module/token can lose a closed generic declaring type when it rehydrates a patch method. Prove
  actual patch construction, invocation and cleanup; when constructed identity is not retained,
  use concrete wrappers for typed engine arguments around the owned non-generic route. This does
  not prohibit ordinary generic helpers or emitted generic calls whose identity is preserved.
  Also inspect the provider's `__state` key: callbacks declared on the same type can share one
  state slot even when registered separately. Co-installed families with distinct state types
  need distinct concrete declaring types, or one deliberately shared state contract. A partial
  class does not separate those identities. Prove co-installation, normal/throwing invocation and
  exact-owner cleanup together; isolated callback tests cannot reveal a shared-slot collision.
  For inherited target lookup, distinguish `ReflectedType` from the actual declaration: looking up
  one inherited method through a subclass can produce unequal MethodInfo objects. Compare the
  module/token and exact declaring type within the admitted callable shape. Do not substitute
  GetBaseDefinition equality, which can authorize an override whose effective body was never proved.
- Classify patch inputs by the provider's effective injected binding, not raw ParameterInfo.Name.
  Parameter/method/type HarmonyArgument metadata can rename a result, argument or mutable argument
  array. Read metadata without constructing foreign attributes, honor the inspected provider's
  precedence, and refuse only unproved consumed bindings. Test an actual aliased output writer and
  a harmless renamed observer; a reflection-only raw-name test cannot establish composition safety.
  Classify the injected payload as well as its declared type: a mutable argument array remains
  writable through object/interface parameters, while by-ref private patch state is not a target
  argument or result writer. Pair those rejection and compatible-state cases on the actual provider.
- Preserve machine arithmetic when validating layout or numeric producer chains. Real-number
  affine equality does not prove Single/Double evaluation equivalence: large cancellation,
  overflow and reassociation can change consumed values. Keep the relevant operation order or
  prove a bounded safe transformation; pair admitted arithmetic variants with actual output checks
  at representative/boundary inputs. A small absolute-magnitude bound alone does not make a
  transformation safe: preserve explicit precision conversions and intermediate Single rounding,
  including at typed stores. Track writes through every represented value type and managed-reference
  alias; a Vector field write cannot be ignored merely because a Rect write is already checked.
  Add a changed-consumer regression for each newly modeled aggregate. Do not broaden this into a
  general-purpose IL evaluator; refuse unproved forms within the narrow consumed chain.
- Revoke old admission before evaluating a newly composed stream. A validator/metadata failure
  inside a transpiler must preserve its incoming stream and cannot prevent another owner's patch
  removal. Test the actual failing provider path, including a subsequent successful rebuild when
  recovery is claimed. Public instruction inspection outside composition is not a publication receipt.
  Cached admission must state which mutations invalidate it; observer removal does not execute the
  removed observer. Do not claim that removal is detected unless an existing check actually proves it.
- Verify fallback by invoking the target and checking its native effects, values and exceptions.
  `Available == false` alone does not prove that already-emitted owned branches are inactive. Check
  that an unavailable captured context really bypasses owned decisions. Keep unsupported rewrites
  explicit instead of expanding into a speculative framework for arbitrary hostile modifications.
- Acquired transient scopes need cleanup even when later compatibility inspection fails. Put the
  acquired-token cleanup in an independent `finally`, preserve the original exception, and isolate
  reporting failures. Rejected nested scopes must not borrow a parent's provenance or authority.

Keep provider-specific details in the owned adapter and target document. These checks do not justify
rejecting all foreign owners, requiring pristine game methods, or building a general IL interpreter.
