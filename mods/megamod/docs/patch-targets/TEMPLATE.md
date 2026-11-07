# <stable patch ID>: <player-facing feature>

Template only. Complete this document and the executable validator before registering the patch.

## Ownership and traceability

- Owner: `fumblesneeze.megamod` (`mods/megamod`).
- Patch ID; owning OpenSpec requirement/scenarios; source analysis/composition report if applicable.
- Kind: prefix / postfix / transpiler / finalizer / XML.
- Implementation, startup validator, and focused test links; supported game/mod evidence identities.
- Reason a patch is necessary rather than XML/composition/public API; no patch of owned megamod code.

## Target and minimal predicates

- Game or canonical external package; assembly/type; exact method signature (static/instance,
  generics, parameters including by-ref, return type, accessibility) as actually required for binding.
- For XML: required package and `XmlExtensions.FindMod` gate, exact Def/node selectors, expected
  cardinality/shape, and the XML Extensions content guard used before mutation.
- Relevant lifecycle/startup phase and the facts available at that phase.
- Numbered predicates with their rationale and executable check. For an argument write, state the
  argument → operation/operand → control-flow relationship. For a transpiler, name structural
  landmarks, match cardinality, and how unchanged input is preserved on failure.
- Explicitly permitted variations (other Harmony owners, unrelated IL, offsets/local numbering).
  Hashes/versions identify evidence; they are not sole admission predicates.

## Composition, ordering, and invalidation

- Original metadata/code or effective patched surface; why this is the relevant observation.
- Relevant foreign prefixes, transpilers, skip-original behavior, and argument writers; supported
  ordering and how their effects are checked without blanket exclusions.
- Supported later repatch lifecycle, invalidation and revalidation; unverified limits.
- XML merge strategy, existing third-party entries preserved, duplicate prevention, and ordering.
  If the feature intentionally replaces a list: why merging is unsuitable, intended final semantics,
  what is discarded, and compatibility consequences. A convenient broad replacement is insufficient.

## Admission, failure, and diagnostics

- Enabled-candidate and optional-provider policy; one idempotent registration path.
- Independent patch or atomic feature group, prerequisites, rollback of only newly added owned patches.
- Failure for missing/changed/ambiguous/unreadable targets or validator/install exceptions.
- Safe native fallback or unavailable player behavior; no partial state or foreign unpatching.
- Startup warning example: `[fumblesneeze.megamod] Patch <ID> not applied; target=<...>;
  targetSpec=<path/ID>; predicate=<...>; reason=<...>; unavailable=<feature>; fallback=<...>`.
  Bound values and emit once per skipped enabled patch, never per tick/job.

## Verification matrix and final observation

| Case | Expected admission/result | Honest test tier / test ID | Evidence / state |
| --- | --- | --- | --- |
| Supported target | Applied once; native result matches spec | <tier> | Pending |
| Benign edits / compatible foreign patch | Admitted, other owner preserved | <tier> | Pending |
| Missing/changed required use or ambiguous match | Absent / unchanged input; startup warning | <tier> | Pending |
| Validator/install failure; repeat initialization | Isolated failure; no duplicate/partial state | <tier> | Pending |
| Relevant later repatch | Revalidate or safely disable | <tier> | Pending |
| XML additions already present / other mods extend list | Other entries preserved, no duplicate | <tier or not applicable> | Pending |

Retain build identity, ordered active mods, actual player action and personally observed result,
screenshots, log warnings, request/config hashes and cleanup. A successful validator or loaded patch
does not itself verify the gameplay feature. Mark cases not applicable only with an explicit reason.
