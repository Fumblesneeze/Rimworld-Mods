# Concrete startup admission

Owner: `fumblesneeze.megamod`, C01 / P0-A. The pure coordinator's review findings are resolved.
The scoped Harmony receipt, native diagnostic targets and protected sinks have focused host tests
and independent review. S094's shared warning/error reporting group and loader adapters are current
consumers. Supported repatch checks belong to their individual target contracts; native acceptance
remains scoped to the exact reviewed build and observed workflows.

`AdmissionCoordinator.Apply` runs on the serialized startup owner, after the required C02 settings
load and at the candidate's Early, DefsReady or GameReady phase. The root bootstrap chooses phase,
enabled state and an optional unavailable reason from active providers or required capabilities.
Wrong-phase and explicitly disabled candidates are untouched and silent; they can be considered
later at their enabled phase. An unavailable enabled group is rejected before probing its targets.

Each `PatchCandidate` names its stable patch/spec IDs, target, unavailable behavior and native fallback.
Its validator returns null only when the actual target contract passes; a reason or exception rejects
the whole group. Candidate lists are copied before validation. All validation finishes before any
installation. A candidate may not swap unvalidated work into that same attempt.

`Install` and `Rollback` are explicit owned side-effect boundaries. The adapter establishes exact
cleanup ownership before Apply. The coordinator records each current rollback before invoking its
installer; failures unwind current partial work and prior new members in reverse order. Every cleanup
is attempted even if another cleanup fails. Adapters may remove only newly installed owned members,
never a whole owner, preexisting members or another provider. A false result does not prove physical
cleanup if the adapter failed: any possibly remaining owned handler must also respect its feature's
inactive publication gate. Actual game-target ownership still needs the native integration evidence.

`Harmony/OwnedHarmonyPatch` supplies a receipt for one exact target/kind using the package-ID owner.
It checks the slot before validation and again at acquisition. A preexisting own member makes that
acquisition unavailable. Its rollback preserves foreign owners (even the same callback), other
targets and other kinds. A changed slot containing another owned member causes an explicit cleanup
failure instead of removing that member. Serialized owned admission must coordinate one adapter for
features sharing a target/kind. Host fixtures cover actual Harmony acquisition, preservation and a
throwing group install; these are scoped host methods, not native game targets.
An empty metadata slot after a thrown Harmony installation is not proof that no detour occurred:
the inspected provider detours before publishing shared metadata. Rollback still attempts to rebuild
that exact target/kind without the owned slot. This ordinary cleanup can rerun remaining foreign
transpilers; it is not a speculative validation probe. If it fails, the coordinator exposes the
cleanup failure and leaves the feature inactive rather than asserting physical removal.

One group ID identifies one stable startup registration. Results are cached, including rejections;
recursive attempts see unavailable while the group is being validated/installed. `IsAdmitted` becomes
true only after the complete group succeeds. These APIs and delivery-failure inspections belong to
the serialized startup owner; worker diagnostic callbacks consume an atomically published immutable
feature snapshot and never inspect the mutable coordinator or SettingsStore dictionaries.

Changing settings later changes effective behavior through that snapshot. Apply is not an uninstall,
retry or effective-code revalidation mechanism. The first actual runtime adapter must provide its
documented supported repatch check before later patching can be claimed safe. No speculative patch
registry, foreign-transpiler runner or whole-method IL matcher is provided by this module.

The root supplies the protected primary warning sink and independent fallback explicitly for runtime
use. Pure admission performs no implicit file, console, Verse or UI I/O. Each skipped enabled patch
gets one startup warning per attempted registration. The warning's six metadata fields each retain
at most 256 characters, replace control characters with spaces and visibly mark truncation. Original
ordinary logs and definition context are never passed through this admission-warning bound.
An installation rejection with failed cleanup starts with the cleanup-failure count, then reserves
96 characters each for the installation reason and first cleanup failure. A long install exception
cannot conceal that owned work may remain installed; later cleanup failures are included in the count.

If primary delivery fails, try the fallback once. If fallback is absent or fails, retain the first
64 immutable failure records plus the total count; do not recursively log or block an unrelated
group. `DeliveryFailures` and `UndeliveredWarningCount` are diagnostic state, not delivery proof.
The optional missing-fallback case supports an inspectable fail-closed result; production bootstrap
must provide both real destinations before enabling any owned ordinary capture suppression.

Focused source-linked tests live in `tests/Megamod.Unit/Patching/AdmissionCoordinatorTests.cs`;
their fake installer callbacks do not load mods or install Harmony patches. Durable local receipts
are in `artifacts/Implementation/megamod-p0-a/tdd-ledger.md`. Final native acceptance remains the
orchestrator's responsibility on the reviewed integrated build.
