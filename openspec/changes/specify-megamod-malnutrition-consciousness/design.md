## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S045 source is four XML operations, fully recorded in
[research](../../../mods/megamod/docs/refinement/2026-11-03-malnutrition-consciousness-research.md).
Native 1.7 still has the four negative consciousness offsets and extreme cap. No runtime work or
acceptance accompanies this draft.

## Goals / Non-Goals

**Goals:** retain source ceilings, preserve other capacities/health rules, identify semantic targets,
publish one coherent XML change and keep compatibility diagnostics honest.

**Non-goals:** immunity to starvation/injury, raised minimum consciousness, condition conversion,
new hunger/death logic, a tuning UI, source package emulation or general XML migration framework.

## Decisions

### Transform only the intended XML fields

Use declarative XML with required XML Extensions guards, not a per-pawn Harmony capacity postfix or
runtime Def mutation. The intended semantic change is **negative offset → maximum** in the unique
Consciousness modifier of four ordinary Malnutrition intervals: [0,.2), [.2,.4), [.4,.6), [.6,.8),
yielding .95/.90/.80/.70. Native extreme from .8 and lethal severity 1 remain untouched.

Locate one Malnutrition HediffDef and one Consciousness PawnCapacityDef. Resolve inherited/effective
stage structure at a proven supported XML phase. Identify the stages by effective minSeverity and
next boundary, not list ordinal or translated label. Omitted first minSeverity means zero. Require
unique intervals and relevant capacity modifiers; split/missing/ambiguous intervals fail admission.
Adding an unrelated capacity or field is allowed and must survive. Do not require whole-file identity,
an exact source hash, absence of foreign patches, or an exact value for an otherwise recognized
negative offset. A positive offset, duplicate relevant modifier or unrecognized semantic replacement
is not a supported conversion. This intentionally replaces the recognized negative deduction even
if another supported provider changed its magnitude; the target ceiling is the selected policy.

Remove only that modifier's negative offset and merge one scalar setMax. If it has a supported finite
positive scalar cap already, retain min(existing cap, selected ceiling); never loosen a stronger cap.
Leave postFactor, unrelated fields and other capacities intact. Reject unsupported dynamic cap curves
instead of adding a scalar the curve would ignore. An already zero/absent offset with an established
cap at or below the selected ceiling is an idempotent satisfied case; don't duplicate setMax or
reinterpret arbitrary missing data as success. Unsupported extra relevant offsets cannot be erased.

No authored stages/capMods/whole-Def replacement is permitted. This is intentional scalar replacement,
because keeping the old deduction as well as adding a cap would defeat the feature. Existing foreign
entries, attributes, ordering and all unrelated Def fields remain structurally preserved. Do not
redefine Malnutrition, its class, severity thresholds or native save identity.

### One atomic XML group with checked publication

All four stage edits form one declared group; partial conversion is not acceptable. Preflight all
targets and provider policy before mutation. XML Extensions Conditional/FindMod/Log supply concrete
guard/diagnostic primitives, but their case containers and vanilla PatchOperationSequence are not
proven transactions. Inspect current framework facilities before choosing an atomic application route.

If no supported atomic merge operation exists, a small mod-owned PatchOperation may prepare these
field edits on a clone of the **current** target subtree, validate all changes and preserved entries,
then publish once; wrap it in the required XML Extensions content/provider guards. This is a guarded
merge of current data, not replacement with an authored vanilla stages list. Keep this operation local
to the feature until another concrete consumer needs shared machinery. Failure restores only our
changes using the pre-edit snapshot; never discard foreign additions or reload a hardcoded baseline.

Resolve a suitable final XML boundary and inspect the effective result after relevant providers before
deserialization/public play. Later supported XML mutations must trigger revalidation at that boundary;
cannot accept early success when a later patch removed a cap or reinstated a deduction. Do not repair
an unsupported final result by mutating loaded Defs. If the loader/framework cannot guarantee coherent
publication/restoration, keep implementation unaccepted and the owned transformation unavailable.
Dynamic runtime Def editors are unsupported unless separately integrated; no live setting-driven XML
reapplication or new per-save balance switch is introduced.

### Target documents and provider composition

Create a stable target document for the atomic XML group and each reported stage edit before code.
Link the owning observable requirements, exact guards/validator, tests and eventual live evidence.
Minimal prerequisites are existence and unique stage/capacity meaning, supported negative-offset/
scalar-max composition and native capacity semantics that apply max after additive/factor effects.
The native code is read to establish the XML meaning, not patched by this feature. If a provider
replaces that interpretation, prove a narrow adapter or leave this feature unavailable.

Use XmlExtensions.FindMod with `packageId=true` for original/optional-provider gating and Conditional
for actual required Def/node existence/shape. A package match is not proof of its current content.
Original `ProfileName.MalCon` suppresses owned overlap before edits; leave its XML/state untouched.
Malnutrition Reworked is a concrete unsupported alternative stage model. Better GC's unrelated
mothball field is compatible in principle and must be retained. Simply Expansive's historical stage
rewrite is an example of shape rejection, not a tested beta-provider claim. Other providers are not
globally blacklisted merely because they touch Malnutrition or Consciousness.

False/unknown guards, validator exceptions and application failures leave the group unchanged. One
bounded startup warning per skipped enabled patch identifies package/patch/spec ID, target/interval,
failed predicate, unavailable behavior and native/provider fallback. Repeated initialization is
idempotent; no repeated per-pawn logs. Check already-satisfied cases without treating them as errors.
Any future Harmony admission hook independently requires a semantic target, effective-code checks,
owned-only rollback and actual incoming-stream validation if it transpiles; none is justified now.

S046's [physiological contribution contract](../specify-megamod-physiological-consciousness/proposal.md)
now specifies the combined result: supported contributions are calculated once, then these
malnutrition ceilings remain effective in the native outer maximum phase. All inputs at 200% with
default S046 weights give 158.4% from a 100% brain/pain baseline before later effects, but trivial
malnutrition still caps final consciousness at 95%. No patch against our own implementation is used.
The ordering is resolved at specification level; exact native/provider combined acceptance remains open.
Native starvation/care stays with its ordinary owners; S007 Health UI reads the actual resulting state.

### No new settings, UI, saves or caches

Source has no setting or player entry point. The fixed selected ceiling policy needs no new mod menu,
bottom tab, operational toggle or debug control. This is **not** a global-only setting exemption:
there is no configuration row. If configurable ceilings are later selected, specify a safe common
scope/lifecycle first; don't bolt per-save choices onto shared startup Defs. Existing condition severity
and health are native pawn state; native health labels/tooltips remain localized in ordinary surfaces.

No new pawn/Thing index, health cache, tick loop or serialized mod state. Native capacity caching and
dirtying already track health changes. Do not resurrect, heal, clear conditions or reset severity on
load. Existing affected saves use the admitted finalized definition on normal capacity evaluation.
No source settings/type migration or on-disk rewrite is needed. Removing the feature can restore
flat deductions on next startup and thereby change survival; safe mid-save removal is not promised.

## Risks / Trade-offs

- Extra stages/capacities can invalidate ordinal selectors → explicit intervals and Consciousness
  identity; retain unrelated entries, reject alternative models instead of rewriting them.
- Four independent source operations can partially apply → one atomic prepared group and effective
  final validation; do not assume a sequence is rollback-safe.
- Absolute ceilings restrict boosted pawns more than offsets → retain this deliberate source balance
  choice and verify ordinary, impaired and boosted comparators in the actual supported mod list.
- Framework currently declares only 1.6 → ML-Q1 must establish supported beta selection before code;
  do not silently ship a private framework copy or reinterpret metadata as proof.

## Migration Plan

No file migration, source import or new saved type. Preserve native condition identity/severity and
all other health state. Runtime acceptance must prove load/save/feeding transitions under the exact
finalized Defs. At XML load, keep a pre-edit snapshot for owned-only failure restoration and verify the
published four-field semantics plus all preserved data. Source switching/removal and arbitrary health
overhauls remain unverified; any later conversion requires a separate backup/atomicity/recovery contract.

## Open Questions

ML-Q1–3 retain beta framework/atomic XML/finalized contracts, S046 and real provider verification,
and focused tests/balance/native acceptance. They are implementation research, not requests for the
user to explain the existing mod.
