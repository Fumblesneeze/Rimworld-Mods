# Auto-Cut Blight — replacement coverage

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Origin: replacement, S069/P1-63. Source **Auto-Cut Blight - 1.6**, `Defi.AutoCutBlight`,
[Workshop 3520167264](https://steamcommunity.com/sharedfiles/filedetails/?id=3520167264),
assembly/file version **1.1.1.0**, inspected **2026-11-04**. Installed About credits Defi;
Workshop display name is Captain Shitface. Source route supports up to 1.6, not verified 1.7.
No license file/source repository was found; no source code, binary or art copied into product.
[Research](../refinement/2026-11-04-auto-cut-blight-research.md) retains primary documentation,
complete five-class/XML inspection, exact hashes, native evidence and bounded consumer findings.

Status: **observable draft independently reviewed and validated. Nothing implemented.**
[Proposal](../../../../openspec/changes/specify-megamod-auto-cut-blight/proposal.md),
[design](../../../../openspec/changes/specify-megamod-auto-cut-blight/design.md),
[spec](../../../../openspec/changes/specify-megamod-auto-cut-blight/specs/megamod-auto-cut-blight/spec.md),
[tasks](../../../../openspec/changes/specify-megamod-auto-cut-blight/tasks.md).

## Behavior inventory and coverage

All behavior below is specified, **none implemented or verified**. Each row names the corresponding
requirement in the linked spec. Per-patch target documents, focused tests and live evidence remain open.

| ID / requirement | Input → result | Intentional difference / limits |
| --- | --- | --- |
| AB-01 New infection receives an ordinary cut order | New eligible revealed infection → one native cut order | Require actual transition and ordinary protection/authority; existing extraction excludes competing automatic cut; preserve equivalent/unrelated orders instead of custom CutBlight |
| AB-02 Blighted cutting participates in native work priorities | Work choice → blighted cut target preferred within Plant Cutting | Preserve current eligibility and relative emergency rules, not an unconditional priority-1/all-pawn override |
| AB-03 Manual and automatic orders share actual cutting behavior | Any eligible native cut order → same blight priority and real cutting | Manual/native/S008/compatible-provider orders participate; native adjacent blight continuation remains |
| AB-04 Event scope and cancellation remain explicit | Cancel/disable/load/recovery → explicit retained-order semantics | No old-outbreak scan; recovery/disable does not erase orders; repeated same-infection notification does not re-add them |
| AB-05 One common preference controls the behavior | Common setting or small toggle → same current-save policy | New initially-on shared control; source has no settings or control |
| AB-06 Availability and provider authority are truthful | Missing contract/provider → explained partial fallback or original authority | Own settings dormant under original provider; no source identity/save impersonation |

## Interactions and composed result

S008 manual blight/cancel tools, S006 linked growing-zone choices and S063 construction clearing share
native order/job behavior directly. No internal Harmony stack. Growing-zone links do not restrict
automatic blight cutting to linked zones. Manual native/S008 orders join the preference while enabled.
Choosing a blighted cut target with a coexisting harvest marker must still select cutting.
An existing tree-extraction order instead takes precedence over both owned automatic marking and
preferred cut selection, including a coexisting cut. It remains subject to normal extraction work;
cancelling extraction alone does not scan/re-mark the existing infection. Explicit manual cutting
remains available. This correction preserves S063's chosen physical-tree action instead of retaining
only its marker while destroying the tree.

Native completed cuts can designate/queue nearby blight, even after a prior cancellation; the toggle
does not promise to disable that game mechanic or other mods. No extra healthy-crop buffer, forced
work-priority edit, speed/yield/XP/spread change or whole-map catch-up is selected. No private Thing
cache: native per-designation collections suffice. Performance remains unmeasured.

## UI audit and settings

| Source surface | Purpose | Owned destination |
| --- | --- | --- |
| Native cut-style marker and pawn job report | Gameplay orders/work | Ordinary cut marker, cancellation and native cutting report |
| Plant Cutting work column | Pawn-specific work priority | Existing Work UI; no extra column or mod-settings row |
| No source toggle/settings/tab | New optional control over automation | Small bottom-right Automatic blight cutting toggle, with common Plants / Blight settings |

Built-in preference **on**, new save entries **Use global**. Bottom-right click writes an explicit
current-save override through the common path. Global default stays separate; dormant override
restoration, first-use seeding, main-menu disabled save editors/callbacks and save switching apply.
Accepted changes govern subsequent marking/work choices. Disable leaves ordinary orders and current
native work intact. Partial compatibility and original-provider authority remain visible without
discarding saved choices. Seven-language labels/tooltips/explanations required; no bottom tab/debug
button or new art. Per-plant orders are entity gameplay state, not settings.

## Contracts, providers and omissions

| Evidence / binding | Selected treatment | Unverified boundary |
| --- | --- | --- |
| Source custom CutBlight / BlightCutDesignated / AnotherRimworldMod driver saved identities | Do not recreate or silently convert; native jobs/orders for owned behavior | Source migration and mid-job removal need an explicit backed-up atomic migration contract if selected |
| XML/CS/JSON consumer search found source, RimPy and Use This Instead mappings | Retain attribution, no package spoofing or invented facade | Not an exhaustive binary-consumer audit; older mapped IDs need actual source analysis |
| Smart Farming Owlchemist.SmartFarming adds native cut orders on infection | Compatible equivalent order reuse; no mutation of its autoCutBlighted setting | Exact active-list ordering/admission/live behavior still to verify |
| Defi.AutoCutBlight active | Original sole provider; owned automation/priority inactive, controls explain, choices dormant | No claim original itself supports 1.7 merely because it is detected |
| Owned native jobs can retain work-giver references | Preserve current save state and verify lifecycle | Native job reuse alone does not prove safe own-mod uninstall |

Harmony is required for the event bridge. Planned guarded work-provider XML requires XML Extensions
with real inspected 1.7 loading and required metadata; no dependency/package edit yet. Use additive,
idempotent required-content guards, preserving unrelated entries. No DLC, source binaries, other
repository mod or Gateway dependency. Each actual patch requires a minimal semantic target document,
effective foreign-change handling, owned-only group rollback and bounded startup warning. Marking and
priority can decline independently with a truthful fallback. Compatible foreign patches are allowed.

## Verification and remaining work

ACB-Q1–4 track proposed correction/control scope, exact semantic/XML admission, providers/save behavior
and focused tests/settings/languages/costs/native acceptance. Native infection progression must produce
an observed order and actual pawn cut through a real player workflow; direct spawning/state writes are
only setup/diagnostics. Cancellation/adjacent continuation, manual orders, emergency priorities,
disabled/ineligible work and provider cases need their own observed results on the reviewed build.

Semantic pass: reaction time and labor allocation change; crop resources, infection/spread and native
cut effort do not. Emergency classification is not immunity to needs or a guarantee of immediate
response. Retained orders can cut recovered plants; native continuation limits cancellation promises.
No runtime tasks checked, build/install, tests, game launch or normal save/configuration changes.

Independent source/native review found no P1/P2 issues. Composition review's P2 extraction-precedence
gap is resolved in S069 and the related S063 contract/design/tasks/coverage; focused re-review found
no remaining P1/P2. Current S069 draft: **6 requirements, 23 scenarios and 22 unchecked tasks**.
Typed strict validation passed **91 items, zero failures**, exit 0 with no stderr. All **12 scoped
Markdown documents** passed local-link and whitespace checks. These checks do not accept gameplay.
