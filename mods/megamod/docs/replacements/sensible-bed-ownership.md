# Sensible Bed Ownership

## Identity and provenance

- Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
- Origin **replacement**; S115. Source **Sensible Bed Ownership**,1trickPwnyta,
  `sensiblebedownership.1trickPwnyta`, [Workshop3328702448](https://steamcommunity.com/sharedfiles/filedetails/?id=3328702448).
- modVersion1.3.0, selected1.6 DLL55296bytes, SHA256
  `7EF8EFCF54C0BD4DB2FEA1748CFDE4E7009049C2AFB24CD3FBE54959C5BA699D`;
  source commit6395d20a39320aa689380c9c689f11d7a9f895a1, inspected2026-11-06.
- Public/source/binary/native evidence, attribution, no located explicit license and consumer-scan
  limits are in [research](../refinement/2026-11-06-sensible-bed-ownership-research.md).
- **Specified only. Nothing implemented, migrated or verified in game.** No source code/art reused.

## Behavior inventory

All IDs refer to the [observable capability](../../../../openspec/changes/specify-megamod-bed-ownership/specs/megamod-bed-ownership/spec.md).

| ID | Player input and observable result | Defaults / edge cases | Status |
| --- | --- | --- | --- |
| BO01 | Assign bed on another map without losing prior claim | One ordinary + one wholly-substructure allowance per map/kind; native special-building scope | Draft |
| BO02 | Prefer an assigned bed and retain meaningful travel bedroom information | One preference; local ground-first fallback; no remote sleep or borrowed comfort | Draft |
| BO03 | Search owners/candidates and identify partners | Literal short-name search; owners/disabled reasons retained; transient window state | Draft |
| BO04 | Right-click single/animal/group assignment | Explicit affected set, revalidation and scoped full-bed reassignment | Draft |
| BO05 | Unassign target owners or go to one owner | Local claim scope; normal navigation; unavailable reasons | Draft |
| BO06 | Ordinary relationship/status change updates valid assignments | No automatic third-party eviction; once-only breakup cleanup; actual size eligibility | Draft |
| BO07 | Reload, pack, move or destroy actual bed | Stable physical identity; collision pending intent, no extra slot | Draft |
| BO08 | Travel/use temporary beds without defeating home ownership | Release false by default, optional guarded per-allowance memory; S073 composed directly | Draft |
| BO09 | Use nearby comfortable seat instead of ground | Enabled;4 then12 cells; bed/deployment/care precedence and native effects | Draft |
| BO10–11 | Configure inherited defaults and receive bounded compatibility fallback | Seven languages; one provider; safe unreadable/unsupported-state boundary | Draft |

[Proposal, design and unchecked tasks](../../../../openspec/changes/specify-megamod-bed-ownership/)
contain implementation planning, admission dependencies and final native evidence requirements.

## Interactions and composed result

S073's [portable beds/home memory](use-bedrolls.md) now retains claims by default; optional release
uses one common setting and distinct per-map/allowance memory. Temporary physical bed custody is not
another permanent ownership slot. Within the same allowance a temporary rest target leaves the
ordinary claim intact; remote manual choices do not cancel unrelated home memory. Bed/deployment
precedes chair fallback. S062's real supply commitments and existing safety rules remain authoritative.
S135 loading is still queued. Direct owned policy calls replace source-on-source patch stacking.

Use native listers/shared map projections with current action rechecks, not a private all-map cache.
No custom ownership/provider support or runtime speed improvement is claimed from static inspection.

## UI audit and relocation

| Source surface | Purpose | Owned destination / intentional change |
| --- | --- | --- |
| Native assignment dialog | Owners/candidates/search/relationships | Same contextual dialog, per-window query, disabled reasons retained |
| Pawn/animal right-click menu | Assign selected pawns | Same ordinary menu, no suppression of unrelated options or silent partial group |
| Building gizmos | Unassign, preference, go to owner | Contextual controls; entity state, no settings/tab |
| New collision/reclaim explanation | Resolve pending intent | Affected bed/pawn controls; choose/retry/cancel as applicable |
| Mod settings | Global SleepInChairs | Common Pawns / Rest and portable beds row; global/save/Use-global, true default |

No source bottom tab/debug command was found. No new tab/bottom-right control is added. Actual
settings use shared load/store/resolve/render, dormant overrides and main-menu disabled save
callbacks. Preference, owners and pending intent are saved entity state; search is transient.
Translate all owned text in the seven baseline languages. No texture has been selected.

## Consumed contracts and coverage

| Consumer / contract | Evidence | Treatment |
| --- | --- | --- |
| Use This Instead replacement recommendation | Installed3396308787/replacements.json references source package as Better Bed Assignment successor | Recommendation identity is not ours; no spoofing or automatic rewriting |
| Possible compiled/reflected callers of Utility/settings/patch types | Public source exists; bounded text/ASCII-DLL scans found no other actual caller | No unsupported facade or blanket drop-in claim; BO-Q2 |
| Native owner lists, source preferredBed and SleepInChairs | Exact source/save calls and native reconciliation inspected | Preserve valid native assignments; owned versioned state, no automatic source import |
| Reverse Commands | Source conditionally patches a named type/method | Optional integration candidate only; canonical package/actual need/shape must be proven |
| Original source co-installation | Same ownership, UI and rest surfaces | Original owns overlapping behavior; owned overlap stays off with warning, records readable |

Implemented portion is **none** for every row. Deliberately omitted source mechanisms: unguarded
PatchAll, first-local/first-constant matching, blanket load-repair suppression, same-pawn-any-job
reservation success, global search/cache, blind group eviction and source-specific corrupt-save
repair. Chosen differences are observable corrections described in BO01–11, not hidden claims of
source parity. Ordinary native assignment limits, physical sleep effects and optional content remain.

No XML mutation selected. Harmony becomes required with actual patch use. Optional content/providers
are absent-safe and shape-guarded; missing special semantics disable the affected path. No dependency
on another repository product or on the Dev Gateway. No copied assemblies or source art.

## Verification and acceptance

Documentation review/validation is recorded in the [ledger](../refinement/specification-progress.md).
BO-Q1–4 retain current runtime admission, consumers/migration, actual native workflows, playability,
performance and UI evidence. Final acceptance requires focused RED/GREEN, reviewed build/package,
actual player assignment/travel/rest/relationship/transfer actions in fresh minimized isolated runs,
personally inspected causal outcomes and retained cleanup evidence. No implementation task is checked.
