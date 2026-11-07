# Skilled Slaughtering

## Identity and provenance

- Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target1.7 beta.
- Origin **replacement**, S117/P2-46. MrHydralisk, `MrHydralisk.SkilledSlaughtering`,
  [Workshop2873284405](https://steamcommunity.com/sharedfiles/filedetails/?id=2873284405).
- Installed manifest1.3.0.0, selected1.6 DLL assembly/file1.0.0.0,6656bytes,SHA256
  `C51BA483A3970248643A80766172AF91803206D7E9373F6DFF830CA0B3857294`.
- [Research](../refinement/2026-11-06-skilled-slaughtering-research.md) records source selection,
  complete four-type/one-Def inventory, current native behavior and consumer limitations.
- No public source/license located in bounded inspection. Independent behavior implementation
  planned; no binary/code/art copying or original-package emulation.

## Behavior inventory

All rows are **specified, not implemented or verified** in the
[owned contract](../../../../openspec/changes/specify-megamod-skilled-slaughtering/specs/megamod-skilled-slaughtering/spec.md).

| ID | Observable behavior | Source / intentional change |
| --- | --- | --- |
| SSK01 | Native selected animal handling, including mental-state Slaughter jobs | Preserves actual source/native scope and all eligibility/outcome rules; unrelated kills/animal work excluded |
| SSK02 | Animals/Melee/work/capacity speed factors and10% floor | Retains source contribution and ordinary other stat effects; no new XP |
| SSK03 | Two-stage rounding of180/speed and size multiplier; fresh attempt facts | Retains source curve; adds minimum1tick/invalid fallback and removes stale global cache |
| SSK04 | Same finite animal/handler duration, proper interruption and cleanup | Corrects source captured180-tick animal wait; no permanent hold or foreign-job cancellation |
| SSK05 | Saved total/remaining work and native completion once | Explicit lifecycle protection; native base yields unchanged by this source feature |
| SSK06 | Native stat explanation/progress in seven languages | Existing context only; no new menu/settings/toggle; inactive owned stat does not imply active timing |
| SSK07 | Safe shared stat identity and one selected provider | Guarded additive SlaughteringSpeed definition; no overwriting conflicting content or duplicate source behavior |
| SSK08 | Atomic target admission, bounded warnings/fallback and revalidation | Replaces unguarded source patch/string-duration lookup; compatible stages/modifiers retained |

## Interactions and UI placement

[S118](slaughter-yield.md) owns the requested Cooking contribution and slaughter-origin yield
extension. Its draft adds Cooking before the final speed minimum, shares this handling snapshot,
and records deliberate slaughter recovery on the resulting corpse for later meat/leather processing.
It preserves base values; this report does not claim the source implements Cooking, extra products
or improved butchering. S008 hunting/finishing
and S065 bonded-companion kills receive no S117 timing or implied S118 bonus. S064/S066 animal
wandering respects the ordinary temporary job rather than an independent immobilization patch.

The source adds only a pawn-work stat and changes job duration. Its placement remains native pawn
information and ordinary job progress. No bottom tab/debug control/view toggle or configuration
window exists to port. Actual work/animal/wait state is gameplay state, not a Use-global setting.
No independent map index or long-lived per-source pawn cache; compute current facts once per attempt.

## Contracts and provider policy

| Contract | Evidence and decision |
| --- | --- |
| `SlaughteringSpeed` StatDef | Preserve name where safe; ordinary admitted stat contributions remain; XML Extensions checks prerequisite content and conflicting definitions without replacing lists |
| Source package, assembly, Harmony owner/method and DefOf type | No proven callable consumer; no facade or package spoof; bounded scans cannot exclude dynamic/uninstalled consumers |
| Four Manifest suggestions and RimPy entry | Recommendations/catalog metadata, not runtime API contracts or dependencies |
| Source active | Its timing/stat authoritative; owned overlap suppressed, no duplicate Def; source hold/cache limitations are not falsely fixed |
| Other slaughter/stat provider | Optional package/shape admission; compatible modifications allowed, unknown affected contract fails closed with bounded explanation |
| Saved jobs | Source lacks own keys but modifies native duration; no automatic source/owned in-flight migration or broad safe-removal promise |

Harmony and XML Extensions are required with planned implementation use, never bundled. No DLC,
upstream mod, repository product or Gateway runtime dependency. Every actual patch needs its own
target document, executable admission, focused tests and later live acceptance.

## Implementation, deviations and omissions

| Scope | Implemented here | Deviations / missing work |
| --- | --- | --- |
| Complete source timing/stat behavior | Nothing | Fresh entry snapshot; coordinated finite waits, cancellation/reload ownership, minimum-positive duration and guarded admission are deliberate corrections |
| Source rate/size balance | Nothing | Source values retained and static rat/cow/thrumbo comparators recorded; finalized live timing/playability still pending |
| Cooking/yield | Nothing | Separate S118 custom feature, not an omitted source behavior; earlier global doubling/skill16 proposal superseded |
| Milk/wool, hunting, prisoner/entity execution | Nothing | Not this source's behavior, not adopted through comments or shared execution calls |
| Code/contracts/verification | Nothing | All runtime code, guarded XML, target docs, TDD, localization, package and native acceptance remain open |

## Verification

[Ledger](../refinement/specification-progress.md) tracks SSK-Q1–4 and document review/validation.
Future proof requires real manual/auto slaughter, native eligibility and mental-state paths, current
size/skills/capacities, long handling, progress, cancellation/emergencies, exact-wait cleanup,
save/reload, source/provider cases and ordinary corpse/outcomes in a fresh minimized isolated
reviewed-build game. Inspect causal before/action/after views personally; retain exact build/mods,
actions/screenshots, supporting logs, normal config hashes and cleanup. Tests, finalized Def reads,
startup logs or prepared death-state screenshots alone cannot accept this gameplay.
