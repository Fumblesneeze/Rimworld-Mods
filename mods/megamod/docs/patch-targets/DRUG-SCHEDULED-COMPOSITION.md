# DRUG-SCHEDULED-COMPOSITION: scheduled mood and tolerance

Owner: `fumblesneeze.megamod`. This shared native boundary implements S024 scheduled potential
mood and S138 scheduled tolerance. It has exactly two consumers, with no internal Megamod patch,
new scheduler, final-ingestion veto or patient-treatment implementation.

Contracts: [S024 practical needs](../../../../openspec/changes/specify-megamod-common-sense/specs/megamod-practical-needs/spec.md)
and [S138 tolerance](../../../../openspec/changes/specify-megamod-drug-tolerance/specs/megamod-drug-tolerance/spec.md).
Native inspection: RimWorld 1.7.5005 rev1082, Assembly-CSharp SHA256
`82387F78262461E53BB06A67C3945912B876D1FDB07235122BBB6E59D073F125` (research evidence, not admission).

| Boundary | Consumed contract |
| --- | --- |
| `Pawn_DrugPolicyTracker.AllowedToTakeScheduledNow(ThingDef)` | Exact public instance nongeneric bool; tracker, actual pawn and drug identities; original native bool |
| `Pawn_DrugPolicyTracker.curPolicy` | Exact declared instance `DrugPolicy` field, read after native execution; no lazy `CurrentPolicy` getter |
| `DrugPolicyDatabase.policies` | Exact declared instance `List<DrugPolicy>`; exact current Game database membership |
| Current unique `DrugPolicyEntry` | Existing guarded row reader; exact drug identity and scheduled permission, without factory or repair |
| `Need.CurInstantLevelPercentage` | Actual native current-thought/difficulty potential via virtual `Need_Mood.CurInstantLevel`; native tick cache retained |

Map and caravan native scheduling reach `ShouldTryToTakeScheduledNow`, which consumes this
Allowed boundary before additional native risk and timing checks. Caravan consumption remains
its ordinary world-tick ingestion, with no map job invented. Dose history, intervals, stock
attachment and the later native ingestion path remain authoritative.

`NativeScheduledDrugDecision` owns one `ScheduledDrugDecisionPatch`, which reuses the existing
`DrugToleranceDecisionPatch` publication/receipt machinery. The last capture prefix, first result
postfix and last restriction finalizer preserve native false, other outer rejection and native
exceptions. Neither reader runs when native or effective outer permission is already false.
Both fixed consumers run once when eligible; only their `Rejected` results add a veto.
`Unavailable` is distinct from `Allowed`, contributes no veto and cannot disable the other rule.

One synchronous native context surrounds both consumers. It captures and rechecks exact current
Game, common binding, database, tracker/pawn, policy, unique row and current drug definition.
If foreign getter code departs that context, neither old-context veto is published. Nothing is
retained after return. The shared context/dispatch and publication failures remain outside the
local read exception boundary.

The mood adapter requires its own effective setting and shared boundary, independently of S138's
editor, geometry, persistence or setting. For a relevant threshold below 1, both current and
native potential mood must be strictly below it. Equality rejects. Threshold 1 and no mood need
add no restriction without reading potential. Invalid relevant percentages and thrown native
reads are unavailable, never clamped or replaced with zero. Actual getter evaluation runs inside
the production `ScheduledDrugReadBoundary`. Its once-per-Game localized notice cannot escape or
invalidate the independent tolerance predicate. The tolerance adapter uses that same exception
conversion seam around its actual tolerance read and rule, after its complete S138 availability
gate. Its existing unreadable-data semantics and saved choices remain unchanged.

The shared Early group is `drug-policy.scheduled-decision`. S138 acquires only the read-only
`DT-SCHEDULED-DEPENDENCY`, validating and rechecking the actual shared receipt; rollback does
nothing. A failed S138 editor install cannot remove the independent mood restriction. Every S138
use still requires the shared receipt and no shared failure. Removed/changed/skipped shared
callbacks revoke both scheduled additions and the entire S138 enforcement/editor capability.
Supported compatible republication can re-admit under the existing publication contract.
Invocation refusal revokes both the receipt and its pending generation; an availability/dependency
query cannot recover the same refused generation. Only a fresh compatible observed publication can.
Shared failure diagnostics identify package, patch, target, target spec, lost behavior and fallback;
normalize each reason to one line, truncate it to 256 characters and retain at most 16 distinct
reasons per owner. Repeated failures and a throwing sink cannot cause per-selection delivery.
Ordinary feature switches retain owners and change only their next eligible decisions.

S024's `practicalNeeds.scheduledPotentialMood` setting defaults on, uses common global/save
inheritance and dormant overrides, and takes effect on the next selection. Exact active package
`avilmask.commonsense` applies ordinary effective-off to this setting with one startup warning;
preferences remain stored. This is not a provider coexistence or save-compatibility claim.

Focused host evidence exercises actual patched callables, independent outcomes, thrown production
read boundaries, failed S138 rollback, context departure, original skip/throw and receipt removal.
Native acceptance additionally requires a recovering pawn rejection and a positive relevant
threshold dose with S024 enabled, real map/caravan consumption, the fresh native-eligible zero
control while S024 is off, ordinary setting actions and retained save state. Tests, descriptors
and installed callbacks alone do not accept these workflows.
