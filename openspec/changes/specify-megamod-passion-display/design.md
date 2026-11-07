## Context

Sole owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), 1.7 beta.
S128 is specification-only. Read the [proposal](proposal.md), [observable contract](specs/megamod-passion-display/spec.md)
and [research](../../../mods/megamod/docs/refinement/2026-11-06-rimhud-passion-icons-research.md).
Public source is pinned at `3d32cc578f956b2ea5a1affcbaf54f900fa25df7`; installed 1.7 core and conditional
VSE assemblies were decompiled read-only. Hashes identify evidence, not runtime admission predicates.

## Goals / Non-Goals

**Goals:** truthful native/provider passion icons, readable labels, full skill details, bounded
fallback, unchanged gameplay and direct composition with the owned S127 skill row.

**Non-goals:** new passions, learning/stat changes, a generic icon framework, duplicated HUD settings,
foreign startup replay, source package impersonation or reproduction of private RimHUD IL layouts.
Older provider repair, additional provider binary parity and texture-art acceptance are not delivered.

## Decisions

### Compose structured passion information directly

The source patches `ValueModel.Build` and `ValueWidget.Draw`, extracts `SkillValue._skill`, removes
a marker suffix from `StandardWidget.Label` and stores a texture in a weak table. The owned HUD
already controls its skill model/row; supply passion identity, optional texture, fallback text and
tooltip information directly. Do not strip translated labels, reflect on our own fields, add
Harmony self-patches or retain a second passion cache. Read the skill's current state through
S127's selected-subject snapshot; selection/action/content changes invalidate relevant display.

Minor/major read current `SkillUI.PassionMinorIcon`/`PassionMajorIcon`; None remains iconless.
Optional VSE (`vanillaexpanded.skills`, Workshop3400246558) exposes registered `PassionDef` through
`PassionManager.Passions`, with index-based `PassionToDef`, `Icon`, `Indicator`, `LabelCap` and
`FullDescription`. Its array accessor does not bounds-check: validate registry readiness, index,
entry identity and the actual members consumed before querying. Do not infer passion strength or
generate strings from its numeric index. Keep the adapter absent-safe with no unconditional VSE
type binding in the core assembly. Additional content using that admitted registry can participate
without hardcoded enum cases; Alpha Skills itself remains an unverified provider fixture.

Resolve active content's icon once in the owning HUD/provider lifecycle, not a map scan or per-row
disk load. No negative cache survives relevant content/session changes. Reject missing/error
textures and failed access to the local text fallback; do not manufacture native icons for unknown
identity. Preserve known name even if art fails; an unresolved value gets localized unknown text.
Use existing skill tooltip calculations and provider description only when admitted; do not
duplicate multipliers or present provider-supplied text as proof of calculated learning behavior.

### Reserve layout space before drawing

Source places a square icon after measured label text clamped to the label's right edge, but never
reserves that space from the label; long labels can overlap it. Lay out text and icon together in
S127's row, fitting within available height and preserving source aspect ratio. Shorten only the
visible text; share the full tooltip over the name/icon region and retain the native row action.
Follow HUD font/scale preferences rather than adding another setting. Restore GUI color and all
temporarily changed draw state in guaranteed cleanup; provider texture failures affect one row.
No source textures are copied or selected for shipping in this documentation phase. Future visual
work follows the asset skill and final native zoom/scale acceptance requirements.

### Do not duplicate the already-modern provider repair

The upstream optional assembly prefixes `VSE.PrepareModeratelyPatches.Do`, skips its original body,
patches two Prepare Moderately filters and conditionally retries later VSE initialization. It checks
missing targets and duplicate exact patch methods but not equivalent other patches or Prepatcher
changes. A fallback can invoke `RimHUDPatches.Do` again and changes `ModCompat.SD_passion` directly.

Fresh current VSE extraction already targets both `HasPassion` and `HasPassionsAtLevel` methods
`DoEditInterface`, `Summary` and `Randomize`, using its own `PickRandomPassionPostfix`. Its
`MakeMenu` integration is also a Prepatcher path. Current Prepare Moderately6.10.1.0 has those
methods and no obsolete `GetRandomOfEnum` target is used by this VSE repair path. The source
author's older failure description is retained as provenance, not asserted for these inputs.

Select preservation of current provider behavior; no owned patches to these filters or foreign
initialization are warranted. A second Randomize postfix could draw a second passion; exact-method
deduplication would not prevent equivalent code from doing so. Do not replay VSE init to compensate
for its original RimHUD package gate. Read its passion data through the owned adapter instead.
If an older broken provider becomes a supported requirement, inspect its exact effective targets
and create a separate repair contract before implementing it. No blanket VSE/Prepare Moderately
compatibility claim follows from the present read-only comparison.

### Contracts, settings and lifecycle

The only concrete consumer found for upstream public `PassionIconPatches.Resolver` is its own VSE
assembly, assigning `PassionIconResolver(SkillRecord,out string)` to replace the native resolver.
That internal relationship becomes the direct owned provider adapter. A bounded installed DLL
string search found only this mod's 1.6/1.7 core/optional assemblies; this is not universal evidence
of no other consumers. No speculative binary facade or false `Jaxe.RimHUD` dependency satisfaction.

No new preferences, save keys, migrations or operational controls are introduced. S127 owns common
HUD global/save settings and show/hide state; pawn passions remain native/provider gameplay state.
Co-installation with original RimHUD withholds the whole owned HUD overlap, including icons. The
original icons mod requires original RimHUD; retaining it does not turn it into an adapter for us.
No third-party files or settings are edited, and removing our display leaves pawn data unchanged.

### Patch target admission

Direct owned row composition needs no new patch against S127. Any genuinely necessary future
native hook must have a stable target document, executable minimum-semantic predicate and linked
scenario before implementation. Candidate observation/lifecycle hooks need their callback and
subject lifetime, not an exact method hash or an empty Harmony-owner list. A hook that changes
arguments or replaces code must prove the consumed operation in effective code; a transpiler
must validate its actual incoming stream every run and return it unchanged on failure.

Package ID alone never proves provider data shape. Admit only the adapter's required registry and
members; isolate failure, warn once per skipped enabled patch/adapter, and roll back only newly
added owned hooks in its declared group. Respect compatible foreign owners, do not speculatively
execute their transpilers, and re-admit or withdraw on supported later repatching. Runtime row
failures use bounded diagnostics and local fallback, not global unpatching. No XML mutation is
selected; if later required, add XML Extensions dependency and guarded merge-preserving contract.

## Risks / Trade-offs

- **Provider artwork/data:** supplied identities/textures can be missing or changed. Retain truthful
  text fallback and exact absent/present/invalid fixture workflows; do not claim Alpha/retexture
  compatibility from the author's description alone.
- **Private source bindings:** package/assembly-dependent source consumers cannot use an owned row
  merely because it looks the same. Record unsupported bindings, avoid a facade without a consumer.
- **Historical repair:** omitting redundant code means older broken provider versions are not repaired.
  Record that limitation explicitly; only a demonstrated supported case justifies adding a repair.
- **Beta and readability:** metadata/source reading cannot accept layout, native clicks or loading.
  Future live proof must use the exact 1.7 build and actual provider versions, including constrained
  width, localization, long names, selection changes and GUI-state preservation.

## Migration Plan

Documentation only. Later implementation derives display from existing pawn state, defaults to
S127's existing effective preferences and writes no foreign config or passion data. No automatic
source settings import is needed. Withdrawal returns to S127's truthful text/native fallback;
original-RimHUD co-installation returns display authority to that original provider.

## Open Questions

RPI-Q1 is the completed source inventory. RPI-Q2–RPI-Q4 in the refinement ledger retain exact beta
provider/visual proof, any demonstrated older repair requirement and concrete external consumers.
These are research/acceptance gates, not questions asking the user to describe existing mod behavior.
