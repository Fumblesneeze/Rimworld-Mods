## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S044 replaces Auto Name Babies. [Research](../../../mods/megamod/docs/refinement/2026-11-03-auto-name-babies-research.md)
pins the entire installed 1.7 DLL, six source hooks, current native lifecycle and evidence limits.
This document proposes implementation; no code, tests, migration or native acceptance is delivered.

## Goals / Non-Goals

**Goals:** generated baby names without required prompts; explicit parent/Ideology surname policy;
preserved notifications and manual names; one-time conservative catch-up; shared scoped settings;
minimal semantic admission and safe fallback.

**Non-goals:** changing birth outcomes, parentage, development or naming deadlines; general name/race
framework; arbitrary rename override; guessing legacy intent from every translation of Baby; source
package emulation/config import; independent pawn index or repeating world scan.

## Decisions

### Own pending-name provenance and one successful assignment

Use native generation/birth context to identify a genuinely temporary name before losing that
context. `GiveAppropriateBioAndNameTo` deliberately sets a translated placeholder for player Baby
NameTriples; birth then establishes parent relations, genes and letter details. Capture the pending
identity there, but generate the final name only once relevant birth inputs are established. Do not
name a half-constructed pawn. Do not rely solely on localized First or a numeric age threshold.
S031 toddler life stages still have `DevelopmentalStage.Baby`: include their confirmed pending names
in catch-up/adoption, as the source does, rather than inventing an infant-only age cutoff. Supported
alien stages use the actual admitted developmental-stage and naming contracts, not human ages.

Keep only minimal save-owned provenance/results keyed by pawn identity, not a second pawn collection:
confirmed pending or successful owned assignment, and enough name identity to detect an intervening
manual/provider change. State is gameplay state, never a setting. Retain through death/containment/
caravan transitions while the pawn is retained; clean up with native pawn discard. Own-created pending
state is language-independent. An unexpected current name or unsupported name representation prevents
assignment. Native naming acceptance (`babyNamingDeadline=-1`) is a decisive exclusion, including
intentional Baby. Do not erase a manual name to recover stale pending state.

At FinalizeInit, use the native all-map/world/temporary alive-or-dead enumeration once, deduplicated
by pawn identity. Include confirmed native pending placeholders in supported saves even without own
history; require the inspected native pending state and unaccepted name, not spelling alone. Missing
history is not proof every Baby is temporary. Old-language or provider states that cannot be proven
pending remain native; record bounded diagnostics rather than guessing. Expired but still confirmed
pending names can be caught up without renewing their deadline. No repeated tick polling or setting
change sweep. New births and supported adoption callbacks handle later arrivals.

### Choose a surname once in the correct birth context

Keep source modes/default and fallbacks, including Mother→genetic mother→birth parent→father. Native
parent NameTriple.Last or NameSingle.Name supplies a usable surname; unsupported forms contribute none.
For Random at a new birth, preserve native `RandomLastName` parent-entry weighting. Distinct birth
parent means a distinct pawn, not a distinct surname; equal surname strings retain native weight.
For Random/Ideology during finalization or load, preserve a nonempty already prepared surname.
Only when none exists use source fallback uniform distinct nonempty parental surname strings.

Father/Mother choose their ordered fallback at the final naming attempt. For newly owned Ideology
births, choose and mark the prepared surname once with established parent context. Take primary
ideology from genetic mother (birth parent only if no genetic mother); if it and father's differ,
choose 50/50. Otherwise primary, father, then birth parent. Keep names/unknown/missing precept uses
random distinct surnames; always modes use native marriage roles; usually/random use native
willingness and name-change roll, with no-change falling back to random distinct surnames.

Use native pair selection described in research; equal-gender role ordering follows native identity
ordering. Missing pair/surname follows documented source fallbacks. Do not require an absent
UsuallyMans Def or hardcode the final odds of willingness checks. Treat unsupported multiple/conflicting
MarriageName semantics as an unavailable policy adapter, with the specified Random fallback, rather
than arbitrarily imposing a new precept. Re-evaluate optional DLC/Def/member availability safely.

The source globally prefixes RandomLastName without newborn ownership context. Do not copy that
leak: derive player ownership from the actual birth (including growth vat), and thread the decision
only through that birth. A method name/signature alone cannot prove the parameter still determines
the newborn's surname. Avoid rerolling native-prepared Random/Ideology names in letter and return
callbacks. If no parent or existing surname is usable, retain the generator's usable surname rather
than overwrite it with null. If no valid complete name results, fail without partial assignment.

Use native GeneratePawnName Full with selected surname and actual xenotype, preserving generated
nickname when explicitly set. Supported NameSingle results can supply First as in the source; do not
invent a generic adapter for unknown name classes. Names must pass the appropriate native validity
rules. Successful generated First equal to Baby remains completed by provenance, not rerolled forever.

### Commit name and notification outcome together

Prepare the proposed name and supported notification transformation before publishing either.
Capture original name, deadline, relevant letter state and owned provenance as an in-memory rollback
snapshot; this is not a file migration. Recheck pawn identity, faction, stage, pending status and
current name at commit. Commit once, verify the resulting name and letter state, then record success.
On preparation/commit failure restore only our changes to that snapshot, leave provider-owned changes
intact and preserve native naming availability. Never rerun birth, generate another baby or swallow
unrelated birth exceptions. Log failures with bounded per-event diagnostics, not retry spam.

Native birth assembles and Starts its letter before ApplyBirthOutcome returns; a return postfix alone
cannot guarantee a coherent name/notification transaction. Resolve pre-publication semantic seams
during AN-Q1. A letter-suppressed birth can commit the name without inventing a notification. A
stillbirth uses the same identity flow on Corpse.InnerPawn without changing its death or history.
Adoption only names a genuinely pending baby after legitimate faction transfer; an already named
adoptee retains its existing native name and letter behavior. Never grant adoption eligibility.

For successful owned names, replace only the known temporary-name/deadline paragraph with a localized
full-name announcement. Preserve health, parent/surrogacy, outcome, related faction, look targets,
label/history and foreign non-naming additions. Avoid transiently swapping Pawn.Name just to reconstruct
translated text. Prefer a guarded construction boundary for the naming paragraph; if a supported
foreign letter cannot be safely transformed, abandon this event's assignment and keep native behavior.
Do not use a translated button caption as action identity or remove arbitrary foreign choices.

Success suppresses required Name/Postpone and auto-open for naming, retains plain native Jump and
Close, and preserves unrelated actions. Preserve archive identity and ordinary dismissal, including
missing/dead targets. Use the saved success identity for reload/language changes and already received
letters. Native naming deadline stays unchanged: setting -1 would hide ChoiceLetter_BabyBirth from
the letter stack and prematurely remove ordinary First/Last editing. Manual rename remains a native
context action for its remaining window; if that action later completes naming, native letter
visibility/expiry behavior remains authoritative. No permanent rename permission or deadline renewal.

### One shared policy row and no new navigation

Proposed common setting ID `colonists.naming.surnameMode`, enum Father/Mother/Random/Ideology,
built-in Random. Location: Colonists / Family and names. Use common load/validation/resolve/render/
store, with global default, Use global and Current save. Missing entries inherit; dormant overrides
survive checking Use global; first override seeds current effective default. Invalid persisted enum
uses Random with a bounded diagnostic; invalid UI choices are rejected.

Ideology absence makes stored Ideology effectively Random without erasing it; show requested and
effective policy and disabled reason. Missing Biotech makes the feature unavailable while stored
configuration remains editable at its valid scope. Main-menu save editor, Use global, resets and
callbacks cannot mutate a save. Committed settings affect future successful naming only, not a mass
rename. Use all seven baseline languages for policy, accurate fallback help and notification text.
Individual names/history stay with pawns and ordinary native contextual controls. No bottom tab,
bottom-right toggle or extra debug override is introduced.

### One atomic feature group and semantic patch admission

C# is justified by event-specific naming, parent policy, saved provenance and letter actions; no
declarative Def mutation is needed. Before implementation create stable target documents per hook,
linking observable behavior, validator, tests and later live evidence. Candidate seams:

| Candidate seam | Minimal semantics to prove |
| --- | --- |
| Native placeholder generation | Correct player Baby and name assignment are observable after generation, without treating all named babies as pending |
| Birth surname/final name commit | Actual ownership, established parent/xenotype inputs and chosen surname flow to the intended newborn; corpse and no-letter returns supported |
| Letter preparation and action/auto-open adapters | Correct baby identity, naming paragraph construction and exact native action roles; other content/actions survive; success is known before publication |
| Adoption/load reconciliation | Completed faction transfer or initialized native game collections; callbacks cannot force birth/adoption or run against the previous game |
| Manual completion/provenance cleanup if needed | Observe only native accepted name/discard; never replace the native manual result |

Admit required hooks as one atomic group; no successful auto-name followed by an unsupported prompt
adapter. Validate relevant original and effective semantics, skip-original prefixes and foreign
transpilers; compatible other owners/unrelated edits are allowed. Every owned transpiler validates
its incoming instructions each execution and returns them unchanged on failure. Never speculatively
replay foreign transpilers. Missing/ambiguous semantics, validator exceptions or installation failures
disable the affected group and roll back only newly installed owned patches. Idempotent startup and
supported later repatch invalidation/revalidation are required. Exact hashes identify research builds,
not compatibility predicates. One bounded startup warning per skipped enabled patch must identify
package/patch/spec ID, target, reason, unavailable behavior and native/provider fallback.

Biotech is required to activate this feature, optional for the product; absent DLC is an explained
unavailable feature, not a failed patch candidate. Harmony becomes a hard product dependency when
the hooks are implemented. Ideology and source packages are optional. Active `cruesoe.autonamebabies`
suppresses the owned overlap before patching; never unpatch its owner or rename pawns to undo its
choices. Other name/race/letter providers require package/shape-guarded narrow adapters. No binary
consumer was found; do not create a speculative facade or spoof source package identity.

Use native lists and event data. There is no derived Thing-index need; any future shared consumer
uses the common map subsystem. Clear own transient work on game teardown, never persist live global
references, and keep Unity/Verse access on its owning thread. S031 stage eligibility is consumed
directly, not patched internally. No runtime dependency on another repository product or Dev Gateway.

## Risks / Trade-offs

- Legacy name intent can be ambiguous → leave it unchanged with native controls; do not trade names
  for wider apparent coverage. Supported new pending provenance remains language-independent.
- Name and notification callbacks occur at different times → prepare/commit atomically at verified
  seams and restore only owned changes on failure; don't suppress a prompt based on spelling.
- Surname choices depend on parent/generator/Ideology state → record exact fallbacks, preserve prepared
  stochastic results, validate optional content and test actual native roles before claiming parity.
- Source coexistence could double-name or rewrite letters → explicit source suppression, no guessed
  universal compatibility, exact provider-present runs before acceptance.

## Migration Plan

No on-disk save rewriting, upstream config import or automatic name conversion. Missing owned policy
inherits global defaults; trustworthy pending native babies are handled by the documented runtime
catch-up, with per-event snapshot/rollback and verified name/letter postconditions. Existing names,
relationships and native deadlines persist. Valid saved owned provenance survives reload; malformed
or unsupported provenance suspends assignment for that pawn rather than guessing. Disabling the
feature retains assigned native names and stops new automation. Safe removal/provider swapping is
unproved; any future file migration needs its own backup, atomic publication, restoration and tests.

## Open Questions

AN-Q1–4 in refinement retain effective target/transaction seams, native/provider naming and language
coverage, shared policy/provenance lifecycle, and reviewed-build native acceptance. No unresolved
technical seam is treated as permission to weaken the observable name-preservation contract.
