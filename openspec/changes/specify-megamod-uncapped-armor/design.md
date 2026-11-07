## Context

Sole owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Specification only. Read [proposal](proposal.md), [observable contract](specs/megamod-uncapped-armor/spec.md)
and [source/native research](../../../mods/megamod/docs/refinement/2026-11-04-unlimited-armor-research.md).
The complete source is one XML removal of ArmorRatingBase.maxValue. Native stats and their consumers
already perform all relevant calculations; no new combat algorithm is required.

## Goals / Non-Goals

Goals: preserve armor values above the inherited ceiling, natural/apparel parity, actual native
consumer contracts and intentional scalar-only removal with useful failure behavior.

Non-goals: stronger base armor, new equipment, changing armor penetration or hit coverage, a replacement
overall-protection metric, CE adaptation, source identity spoofing, private stat caches, per-save
combat settings or runtime mutation of static Defs. The absence of source settings is not an excuse
for a separate settings framework; this slice simply introduces no setting.

## Decisions

### Preserve the source's inherited-stat boundary

Use guarded declarative XML to remove only the upper-bound node of the compatible shared abstract
ArmorRatingBase. Native Sharp/Blunt/Heat and compatible added descendants inherit the result; any
explicit child bound is preserved as independent provider policy. Resolve effective inheritance
before claiming an individual stat is uncapped. Do not erase every maxValue in all stats or append
another competing bound. This is intentional scalar removal: keeping that scalar would defeat the
feature, whereas replacing quality/Stuff parts or whole StatDefs would destroy unrelated content.

Native StatDef's absent-field default is9999999. Do not replace it with float infinity, a larger
guessed number or a new maximum setting. The behavioral claim is removal of the inherited armor
ceiling, not arbitrary-precision arithmetic. Preserve minValue0, default/hide values, formatting,
quality parts, Stuff parts, scenario factors and other current calculations. Native stat identities
stay unchanged. Apparel and natural armor use the same stats without faction filtering.

The XML parent's current max2 is evidence, not an exact-value admission fingerprint. A compatible
finite scalar ceiling changed to another value is still that same removable policy. A uniquely
identified, otherwise compatible parent with no ceiling is already satisfied: no insertion,
compensating cap or repeated warning. An absent parent, duplicate/ambiguous bound, malformed scalar
or incompatible stat semantics is different and must leave the target unchanged with a warning.

### Minimal XML admission, idempotence and provider authority

Use XML Extensions Conditional for cardinality/content predicates, FindMod with packageId=true for
source/unsupported-provider selection, and Log warning for a skipped enabled contribution. Keep
the actual narrow removal native where sufficient. Inspect the exact loading/inheritance phase and
selected XML Extensions build before implementation; these candidate operations do not themselves
prove transactionality, C# method semantics or1.7 compatibility.

Before mutation, establish one applicable shared armor parent, the required native rating descendants,
their percentage/nonnegative stat contract and a unique removable numeric scalar or already-absent
state. Check the target's actual inheritance and effective overrides, not merely its name. Allow
unrelated fields, compatible extra descendants, other owners and changed quality/material parts.
An independent child cap remains out of scope and must not be reported as removed. Never delete a
provider's separate balance policy by recursively targeting every descendant maxValue.

One owned scalar removal is one contribution. Preflight all its predicates before changing XML;
if execution fails, leave/restore only that owned edit in the current load document. Do not describe
a generic operation sequence as a transaction, or restore a stale whole-node snapshot over foreign
changes. Stable target ID, exact predicate implementation, failure injection and repeated-resolution
tests belong in UA-Q1 before code. Verify the final loaded bound/inheritance and observable stats
after all applicable operations; a later explicit provider bound remains its policy, not permission
to repeatedly overwrite it. Unsupported later hot changes require fresh admitted loading, not a
watchdog that mutates Defs or fights another mod.

Original Paragon.Hanul.UnLimitedArmor active: the original alone owns this feature; suppress owned
mutation before it runs regardless of relative order and retain an explanation. CETeam.CombatExtended
active: suppress this contribution as outside the selected model, leaving CE and unrelated megamod
features intact. Both are optional canonical package checks, not whole-megamod incompatibleWith flags.
If both are active, do not remove or repair the original mod's own patch. No automatic mod-list edit.

Every skipped enabled XML patch needs one bounded startup warning containing package/patch/spec ID,
target, failed predicate/reason, lost behavior and fallback. Deliberate original/CE authority is
explained as inactive policy, not an unexpected failure. Already-satisfied valid content is not a
failure. No per-tick log/check loop. An XML-only edit needs no C# status layer merely to mirror it.
If later work adds an actual Harmony boundary, it separately needs effective/incoming semantic
checks, compatible-foreign-owner handling, late-repatch policy and owned-only rollback.

XML Extensions becomes required at first use, with its Harmony dependency and proper load order.
Its inspected metadata only advertises1.6; prove the beta route before accepting the package.
Do not copy dependency/source assemblies or add Gateway/product-mod references. No feature runtime
Harmony patch is selected by this design.

### Let native stats and combat remain the implementation

StatWorker.FinalizeValue computes ordinary parts/factors and clamps against the resulting bounds.
Native ArmorUtility requests those stats for each covering worn piece and then natural armor.
It subtracts penetration, evaluates half/full residual thresholds and preserves armor wear and
Sharp-to-Blunt conversion. The max2 constant in the inspected class does not clamp that input.
Thus rewriting ApplyArmor would add a competing implementation without solving a missing behavior.

Do not synthesize aggregate protection by adding ratings. ITab_Pawn_Gear's coverage-weighted summary
normalizes each layer to0..1 and caps the displayed result at200%. Preserve that separate metric.
Ordinary individual info cards can show higher ratings. S009/S010/S012 retain Normal previews and
semantic stat comparison rather than importing upstream set-optimization or aggregate armor caps.
S074 follows ordinary information links. S132 must distinguish individual values from its selected
summary when researched; no second self-patch or speculative inventory facade is selected here.

Use native stat caches/lifecycle. No map-owned index, shared Thing scan or saved derived ratings are
needed. There is no measurable optimization claim. Enabling/disabling the mod is a fresh content-load
decision, not a mid-session mutable setting. Existing items are recalculated through normal stats
after loading; no item/save conversion or source-settings import exists.

### Limit provider claims to demonstrated contracts

Bounded source search found no callable source API. Generic consumers of the existing stats can see
the changed finalized values without a source package alias. This is not a universal compatibility
promise for private calculations, different units or independent caps.

The inspected Yayo's Combat3 Continued branch consumes ordinary stats but replaces injury handling
when advanced armor is enabled. It has its own technology, durability and random behavior. Preserve
that calculation, do not impose native probabilities on it or patch it to recreate our XML change.
Current package/type/Def and effective1.7 behavior must be admitted before claiming supported combat;
the inspected1.6 fallback and author's tentative statement are not that evidence. A benign provider
change alone is not incompatibility; incompatible required rating semantics disable the owned XML
contribution. No public source facade or automatic migration is selected.

## Risks / Trade-offs

- Shared stats also affect natural/enemy armor → explicit observable scope and native workflows;
  do not advertise a colonist-only apparel benefit.
- Higher armor can strongly resist penetration → retain ordinary costs/research/quality, inspect
  finalized comparators and test actual attacks; no compensating cost or damage adjustment guessed.
- Overall summary remains capped → document its different meaning; no invented sum or chance claim.
- Foreign child caps/load order can narrow the effect → preserve independent overrides, report only
  actual inherited-bound removal and verify the final load result.
- XML dependency/phase changes → no silent Always success; exact semantic target guards and bounded
  fallback before mutation, with beta dependency proof retained as future work.

## Migration Plan

No source settings/saved types or owned state. Ordinary item/quality identities persist and current
loaded stats determine their ratings. Source-authority selection must happen before XML edits.
Reverting the owning change removes only its XML contribution on the next fresh load; other mods'
bounds remain authoritative. Do not promise universal safe mod removal or overwrite save data.

## Open Questions

UA-Q1–4 retain exact XML phase/admission, finalized balance/scope, actual provider/UI consumers and
focused implementation/native evidence. No source behavior questionnaire is required.
