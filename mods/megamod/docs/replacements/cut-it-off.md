# Cut It Off

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target1.7 beta.
Origin replacement S056/P1-50. Source `LegendaryMinuteman.CutItOff`,
[Workshop3628405994](https://steamcommunity.com/sharedfiles/filedetails/?id=3628405994), metadata
author LegendaryMinuteman, Workshop display Shakecataz. Selected v1.7 assembly/file1.0.0.0 SHA256
`32D54A0DE28246E9B2C9C012E532D87FB8CA7CF1BAEE6E44F07B2BB2100822C0`, inspected2026-11-04.
No source/license located in the bounded installed/public search. Independently engineer behavior;
no binary, code, icon or permission to copy is implied by subscription.

[Research](../refinement/2026-11-04-cut-it-off-research.md) records every selected behavior class/XML,
native1.7.4998 comparisons, potential bindings and source-description discrepancies.
Status: analyzed; observable draft independently reviewed and strictly validated. Nothing implemented.
[Proposal](../../../../openspec/changes/specify-megamod-emergency-self-amputation/proposal.md),
[design](../../../../openspec/changes/specify-megamod-emergency-self-amputation/design.md),
[spec](../../../../openspec/changes/specify-megamod-emergency-self-amputation/specs/megamod-emergency-self-amputation/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-emergency-self-amputation/tasks.md).

## Behavior inventory

Every row is specified, not implemented or runtime verified.

| ID | Action → intended result | Boundary |
| --- | --- | --- |
| EA01 | Select infected colonist → emergency command when eligible | No actually available assigned/capable/access-permitted medical helper, actual self-tend/Doctor/care policy, no automatic surgery |
| EA02 | Inspect infections → choose a safe expendable part | Highest eligible severity, exact player choice, no healthy/internal/vital/implanted removal |
| EA03 | Confirm choice → stop work and begin once | Named part/medicine/chance/risk, disclosed queue clearing, cancel has no effect |
| EA04 | Collect medicine → one permitted physical unit | Own inventory then reachable map supply, care/access/claim/capacity respected; explicit none allowed except No Care |
| EA05 | Perform work → stated duration and chance | 1.92 game hours; proposed unmedicated50–90% over Medical0–20; medicine uses at least both source chance and the positive potency benefit, capped100% |
| EA06 | Complete → physical removal or failed cut | Native physiology, actual selected part, failure 15–29 ordinary damage, no loot/aftercare/automatic retry |
| EA07 | Observe living result → matching memory/tale | Success +30 for 3 days; failure -18 for 0.5 day; nonstacking refresh/opposite removal, no false survival |
| EA08 | Cancel/load/change conditions → safe recovery | Same part/unit/progress/chance; no duplicate cost/outcome or unconfirmed substitution |
| EA09 | Combined or unavailable feature → coherent care | Shared quantities/policy, source-present suppression, useful startup warning and native fallback |

## Intentional changes and interactions

Use actual available medical help instead of the source's total-free-colonist count. Assignment,
capability, care permission and practical access determine whether another pawn can help. Mere
physical presence, including a dependent child, does not alone block the emergency action. A worse ineligible infection
does not hide an eligible limb. Add confirmation and explicit part choice, care/claims, one exact
medicine cost, no-medicine baseline, actual removal verification and safe saved work. These are
proposed corrections, not claims the original already does them or that the user confirmed each one.

S042 owns self-tend policy; S046 supplies actual capacity state; S007 displays actual health.
S021/S024 provide shared cargo attribution/interruption. S124 Drug Response and S134 Priority Treatment
will be reconciled when researched. No speculative conflict is declared. Owned behaviors compose
directly, without Harmony patches against our own feature implementations or independent caches.

## UI, settings and saved state

The action belongs in the selected pawn's contextual commands and confirmation, not a new bottom
tab, settings window or cheat toolbar. It is constrained ordinary gameplay with lasting cost.
Self-tend, medical care, selected part and active job are pawn gameplay state. EA10 places an atomic Medical0/20 curve control and a positive minimum medicine-benefit control in the common
global/save inheritance UI, with protected main-menu editing. Other values remain named constants.

Owned saved jobs retain exact part/unit/work/chance/resolution state. Removing or disabling a feature
does not regrow actual amputations or remove earned memories. Safe removal of the entire package
from an existing save is not promised.

## Contracts consumed by mods or saves

| Evidence | Contract | Selected policy |
| --- | --- | --- |
| Public source helpers/postfix/driver | CutItOff.SelfSurgeryUtility functions, Patch_Pawn_GetGizmos, JobDriver_SelfAmputation | No installed executable consumer found in bounded scan; no unproven facade |
| Source saved job | SelfAmputation / source driver type | Owned replacement identity; no automatic source-job import |
| Source memories/tale | SurvivedSelfAmputation, SelfAmputationDespair, SelfAmputationSurvived | Owned identities; no false drop-in save promise |
| Active original package | Source command and job own the overlap | Suppress ours with warning; preserve original patches and data |

Any future explicit migration needs backup, atomicity, failure restoration and verified outcome.
No package spoofing or modifying another repository product to manufacture compatibility.

## Coverage and verification

**Nothing implemented.** All ten requirements, semantic target checks, translations, tests,
package changes and final native workflows remain pending. General self-surgery, healthy harvesting,
implant recovery, automatic retries/aftercare and arbitrary race/provider compatibility are omitted.
No source binary/icon is reused. Harmony becomes required with its integration; no DLC/source-mod
dependency. Any later XML patch requires XML Extensions and guarded merge-preserving behavior.

The earlier review counted9 requirements,36 scenarios and24 unchecked tasks; the reconciliation adds EA10 and revised scenarios/tasks. EA-Q1–4 in the
[ledger](../refinement/specification-progress.md) retain product/anatomy, effective runtime,
finalized balance/medical composition and implementation/acceptance work. Independent review found
no issues. Typed strict validation passed 78 items, zero failed; eight scoped documents passed local
links/whitespace. Native acceptance must show the pawn's actual command,
collection/work and health/medicine/mood result; logs, a successful roll or a spawned missing part
are not acceptance. No runtime code, build/install or game launch occurred in this documentation slice.

## Reconciled emergency policy — 2026-11-07

Available medical help now means actual eligible, assigned and accessible care, not merely another
mobile colonist. Unmedicated chance is a selected draft50/70/90% at Medical0/10/20, replacing the
rejected13/19.5/26%. EA05 preserves at least both the positive medicine benefit and source medicine
chance; EA10 adds two common inherited rows for its three values, keeping curve endpoints together. Source facts remain provenance; actual
medical outcomes and balance are unverified.
