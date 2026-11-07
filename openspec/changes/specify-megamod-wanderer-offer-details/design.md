## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S109 is documentation only. [Research](../../../mods/megamod/docs/refinement/2026-11-05-wander-joins-plus-research.md)
separates the selected 1.6 DLL, historical public 1.4 source, native 1.7.4998 rev1299 and the
installed VEE 1.7 provider. See [proposal](proposal.md) and
[observable contract](specs/megamod-wanderer-join-offers/spec.md).

The current source intercepts quest letter delivery, reconstructs a generic walk-in letter and
depends on an XML replacement of the global AcceptJoiner letter class. It copies only selected
fields and starts a fresh 60,000-tick timeout. The native game already owns consent; the source's
useful addition is contextual information. Historical direct-incident interception is a different
implementation, absent from the selected DLL, and is not a template for the beta port.

## Goals / Non-Goals

**Goals:** Preserve the native event and its pawn while adding a visible summary and info card;
retain medical, charity, deadline and provider semantics; keep optional support narrow and absent-safe.

**Non-goals:** New arrival incidents, new recruitment odds, replacement quest generation, source
assembly/package impersonation, arbitrary incident interception, legacy-save migration, a new
settings framework or UI, a global pawn index, runtime work or a compatibility acceptance claim.

## Decisions

### 1. Augment the existing letter and delegate its choices

Prefer a narrowly admitted association between the actual quest-produced letter, pawn and quest,
then add presentation through the existing letter/choice surface. Retain the letter object, native
text, look targets, timeout, map rules, signals, quest, hyperlinks and provider fields. Do not rebuild
it with LetterMaker, globally replace LetterDef.letterClass, regenerate the pawn or replace the
whole Choices enumerator. Preserve provider ordering and inject More information once after the
standard Accept/Reject pair only where that pair is identified, not at an assumed numeric index.
Unknown foreign layouts require their own adapter or lose only our added action.

Added summary formatting uses visible work restrictions, traits and passions, handles missing
trackers, distinguishes None from unavailable, and does not expose hidden hediffs. It can share a
small ordinary-information formatter with S108 through direct owned calls. Event policy stays
separate: S109 preserves timed letters and native denial consequences. S108 is a rescuee-specific
consent gate; its no-added-penalty rule does not erase native S109 DeniedJoining thoughts or charity.

Native methods remain responsible for Accept/Reject. An enrichment wrapper must not send signals,
call recruitment or spawn independently, retry a foreign callback, roll back a partly executed
native choice, or announce success merely because a handler returned. Missing information has a
local fallback; it must not commandeer valid decisions. Refresh only the owned summary segment
from current visible facts; preserve the original event prose and avoid duplicate append operations.

### 2. Keep event-specific arrival and timing

Core WalkIn creates a native AcceptJoiner letter, a 60,000-tick quest delay and a 60,000-tick letter
timeout. Accept signals SetFaction followed by PawnsArrive and success; Reject signals native
DeniedJoining thoughts and failure. The native arrival part can find another suitable map if the
original map is gone. Preserve that recovery instead of enforcing a new hard map lock. Never
assert a particular destination as guaranteed when the underlying event may retarget it.

Royalty's WandererJoinAbasia uses the Core assembly's abasia quest root but Royalty-owned Defs.
It adds the Abasia condition and schedules pods before the decision, with joinPlayer false;
acceptance subsequently changes faction. Preserve afflicted text, the pawn look target, care
obligations and any later tending/rescue workflow. No automatic cure, teleport, rescue or destruction.
Both native roots use a one-day letter timeout; native expiry and Reject need not signal the same
quest path. Do not synthesize a rejection on timeout. The base root's 15,000-tick killed-before
charity allowance remains native context, not a new S109 balance setting.

### 3. Bind only identified optional events

VEE is optional by canonical package ID VanillaExpanded.VEE. Its selected 1.7 quest/incident Defs
identify VEE_WandererJoinsTraitor and the subclass of WalkIn. Preserve the provider's pawn and
hidden Traitor state, not a new owned substitute. Ordinary visible summaries cannot branch on that
hidden state. Inspect effective dispatch before admitting this route: the installed subclass
overrides GeneratePawn(), while the native RunInt path calls GeneratePawn_NewTemp(Map), which
WalkIn overrides directly. Static evidence therefore does not establish that the traitor generator
runs; a foreign compatibility bridge is unverified. Do not secretly fix that provider by adding a
Traitor hediff or copying its generator. Until the exact provider route is proven, classify this
optional enhancement as unavailable and preserve the provider's existing behavior. Its inherited
walk-in presentation is otherwise indistinguishable from an ordinary Core offer: omitting our
controls only on the concealed event would itself reveal the secret. Therefore the Core walk-in
and admitted hidden-provider presentation form one declared failure group whenever that provider
is active. If the hidden route cannot be admitted or later becomes invalid, withdraw the owned
summary and information action from all comparable walk-in offers uniformly, including already
pending/reopened/reloaded letters. Keep their native choices and original text/deadline untouched.
Abasia remains an independent, visibly distinct route. Capability diagnostics name the provider and
failed target, never a concealed pawn or which pending letter belongs to the hidden route.

Package presence alone is insufficient: validate actual quest, letter and pawn association,
consumed types/members and native/provider consequences. HAR is not required; ordinary visible
race labels and info-card behavior can be preserved without a hard dependency. The source's
HAR flag is unused by its current ReplacementLetter call. Historical Alpha Animals mime code is
not a current selected route and does not establish support; record it without a speculative adapter.

### 4. Minimal presentation state, explicit legacy boundary

Use the actual native quest/letter/pawn references, not labels, pawn names, current map or global
scratch variables as identity. If extra persistence is necessary to restore off-map pawn association,
keep a minimal game-owned record keyed to the actual letter/quest identity, using normal saved
references and pruning dead/resolved associations. Do not retain every rejected pawn forever.
Archived letters do not gain live decisions; an ordinary resolvable info card may be read-only.
Lost or ambiguous association disables only our details/action with a localized explanation.
No ticks, map scans, private pawn cache or shared Thing index is justified for these sparse offers.

The source's serialized type WandererJoinPlus.ChoiceLetter_AcceptJoinerPlus and its joiner key
are a real save contract, including archived letters. A new owned type is not a drop-in substitute.
No migration is selected. State that source-letter saves are unsupported for replacement; neither
resolving pending offers nor disabling the owned enhancement proves safe source removal. If migration
is later requested, add an explicit backup/atomic conversion/failure restoration/postcondition
contract first. Do not fabricate a compatibility assembly or spoof Ogliss.WanderJoinsPlus.

### 5. Semantic patch admission before any runtime work

Before implementation, create indexed executable target documents for the concrete patch IDs below;
these names reserve scope, not implemented guards. Separate independent Core and optional routes.

| Planned patch scope | Minimal semantic conditions | Failure boundary |
| --- | --- | --- |
| WJP.offer-binding | Identified native WalkIn/Abasia delivery exposes the same letter, quest and pawn, with the expected receive operation and identity/data flow | No enrichment for the affected route; original letter unchanged |
| WJP.offer-details | Actual letter presentation permits a separate owned visible summary without overwriting event prose or protected information | Omit added summary; native content/decisions remain |
| WJP.offer-information | Active choice construction provides a safe insertion point, exact associated pawn and non-mutating native info-card operation | Omit added control; retain original choices |
| WJP.offer-persistence | Normal serialization/lifecycle can retain and resolve the owned association without changing native deadline/decision state | Treat dependent enrichment/binding as one atomic group; no new orphan associations |
| WJP.vee-offer | Exact optional package/Defs, event identity and effective generation/letter route preserve the provider pawn and secret behavior | Uniform native presentation for all comparable Core/VEE walk-in offers; independent Abasia route unaffected |

A purely observational callback may need only existence and consumed shape if original execution
is not relied on. Any argument or IL rewrite needs the actual consumed semantic snippet, operands,
stack/data flow and cardinality. The source's four-instruction look-ahead and unchecked casts are
not sufficient. Deduplicate inherited method targets; do not patch all similarly named types or
repeated inherited MethodBases. Validators must distinguish original IL from effective code and
account for skip-original prefixes and incoming transpiler streams. Every transpiler revalidates
its actual input each run and returns that input unchanged on failure. Do not speculatively invoke
foreign transpilers. No whole-body equality, version/hash gate or no-other-owner rule.

Register idempotently; false/unknown predicates, exceptions and installation failures skip only the
affected patch/atomic group. Roll back only newly installed owned patches. Warn once per skipped
enabled patch with package, patch/spec ID, target, failed predicate, lost behavior and fallback.
Optional absence means no candidate; active-but-incompatible optional content means a warned
skipped candidate. An active source provider disables our overlap with the same bounded explanation.
Supported later repatching invalidates the affected owned group until revalidation without clearing
native offers or deadlines. No foreign unpatching or repair of third-party state.

This design uses no XML patch. If actual implementation needs a declarative edit, first specify
its exact intent and use XML Extensions package/Def/node guards; add imranfish.xmlextensions as a
hard dependency with its first operation. Preserve unrelated entries and classes. Do not reintroduce
the source's unconditional global letterClass replacement as a shortcut. Harmony is external and
required when used, never bundled. No Gateway dependency or product runtime reliance.

### 6. UI, balance and verification boundaries

Use contextual native letters and info cards. Offer choices are event/pawn state; there is no new
configuration or Use global exception to implement. Translate owned headings, actions and unavailable
reasons in all seven baseline languages. Preserve translated names/casing; long lists require native
scrolling or equivalent usable layout. No player art or icon is selected in this change.

The semantic balance pass compares Core WalkIn, Royalty Abasia and VEE's advertised traitor route,
with ManInBlack as the emergency exclusion. Preserve storyteller chances, generation, arrival work,
medical needs, time limits, denial/charity effects and hidden risks. More information intentionally
improves selection; it must not become secret-state detection. Source XML/static values are research,
not finalized active-Def or live-play evidence. Finalized provider behavior and native acceptance
remain mandatory before a gameplay or compatibility claim.

## Risks / Trade-offs

- Foreign letter presentation → narrow association and additive formatting; unsupported route falls
  back without changing its decision or letter class.
- Off-map pawn lifetime and archives → minimal saved references, no guessed reconstruction or
  KeepForever retention; explicit unsupported source-save boundary.
- Provider generation drift → retain the VEE dispatch question and require effective-route proof;
  no silent content repair or universal modded-wanderer claim.
- Inspection advantage leaks hidden information → ordinary visibility policy and same UI for
  concealed and ordinary pawns; compare hidden-event outcomes in provider acceptance.
- Failure after a native choice starts → never replay or reverse foreign/native effects on behalf
  of presentation; native result remains authoritative and enrichment reports no invented success.

## Migration Plan

No runtime deployment in this change. Future implementation starts with exact target documents,
focused honest-environment RED/GREEN, reviewed guards and scoped package checks. Keep original
provider overlap inactive. Test saves made with the owned feature separately from unsupported
source-letter saves. Rollback removes owned presentation hooks/state only when safe; it must not
delete original letters, quests or pawns. Do not claim arbitrary uninstall safety.

## Open Questions

- **WJP-Q1:** Exact effective letter association/presentation/persistence seams and independent
  guard groups on the implementation revision, including foreign callbacks and later repatches.
- **WJP-Q2:** Exact VEE generation dispatch and optional/legacy provider contracts; current public
  source revision, license clarification and any real external consumers beyond the bounded scan.
- **WJP-Q3:** Owned association serialization, stale/archive behavior and long translated layouts;
  legacy source-save migration remains explicitly outside scope.
- **WJP-Q4:** Focused TDD, final semantic/balance review, exact provider matrices and reviewed-build
  native action-to-result evidence. These are research/implementation tasks, not user interview prompts.
