# Smarter Deconstruction and Mining (Continued)

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement **S061/P1-55**, `Mlie.SmarterDeconstructionAndMining`, Mlie's continuation of
legodude17's `legodude17.smartdecon`.
[Workshop 3261302741](https://steamcommunity.com/sharedfiles/filedetails/?id=3261302741),
[public source](https://github.com/emipa606/SmarterDeconstructionAndMining), inspected 2026-11-04.
Selected installed 1.7.0/assembly 1.7.0.0 DLL SHA256
`251C8222877678428E4C9AC8E3D4316A8655904F4403137B34AA5E34FDD629F8`;
public revision `12ee01b9689986523e8363e896205592cae18120` bundles the matching DLL.
MIT notices credit Mlie and legodude17. No source code, assemblies or artwork copied.

Status: complete selected source/native/consumer research; observable draft independently reviewed
and strictly validated. **Nothing implemented or verified in game.**
[Research](../refinement/2026-11-04-smarter-deconstruction-research.md),
[proposal](../../../../openspec/changes/specify-megamod-roof-safe-removal/proposal.md),
[design](../../../../openspec/changes/specify-megamod-roof-safe-removal/design.md),
[spec](../../../../openspec/changes/specify-megamod-roof-safe-removal/specs/megamod-roof-safe-removal/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-roof-safe-removal/tasks.md).

## Behavior inventory and coverage

All rows are **specified, not implemented**. Evidence is the source/native analysis linked above;
each requirement below belongs to the linked observable capability, not to the upstream mod name.

| ID | Action and selected result | Boundary / owning requirement |
| --- | --- | --- |
| DM01 | Mine, deconstruct or uninstall a support → prepare endangered roof | Enabled work/worker scope; no capability grant or combat protection |
| DM02 | Remove one or concurrent supports → use current actual roof connections | Current connected support, full footprints, mixed plans and final checks |
| DM03 | Start preparation → workers physically walk and remove required roof | Ordinary eligible labor; inaccessible/reserved work waits; provisional native 65 work |
| DM04 | Automatic unavailable mountain roof → stop relevant order with a reason | Mountain cancellation default true; designation-free construction retains a blocked parent; forced/Cancel=false accepts risk without granting removal permission |
| DM05 | Enable mountain removal and satisfy chosen research → permit real preparation | Default false; explicit None, retained missing project; no automatic provider/research heuristic |
| DM06 | Temporary roof orders overlap, change or finish → retain correct player intent | Map/level/operation ownership; later edits win; no instant rebuilding |
| DM07 | Interrupt/reload or run quality/replacement work → retain one authorized continuation | Progress, contents, quantities, budgets and ordinary queued orders preserved |
| DM08 | Configure the feature → common inherited global/save preferences | Seven ordered rows; main-menu save controls inactive; no new bottom tab |
| DM09 | Compatible, original or unsupported provider → admitted behavior or clear fallback | Minimal effective guards, stopped saved work, no foreign unpatch or source identity spoof |

## Interactions and intentional differences

S008 mining tools keep their selected material/cells and ordinary designations, without inventing
a persistent parent plan or recreating canceled hazardous work. Ordinary blueprint/frame clearing
retains its exact blocked parent and resources, with explicit retry and changed-condition recovery.
S013 quality cycles keep their costs, budgets and builder policy; roof waiting grants no retry.
S028 replacement retains old building/content/material transactions until safe commitment; continuous
actual support can retain roof, whereas a real support loss needs preparation, including install-over.
S060's covered construction-access rule remains an independent completion condition. These are
direct owned decisions, not patches against internal ports. Shared map facts stay native-first.

Intentional corrections: complete current support/concurrency checks, real roof-worker eligibility,
blocked-cell waiting, accurate MineVein/uninstall cancellation, map/save/player-edit-safe temporary
areas and complete continuation intent. Worker options become independent for player animals;
wild humans follow Other workers. Mountain removal consistently starts off, explicit missing research
never means None, and the source's delayed research-name matching is omitted. The provisional mountain
work cost is ordinary roof work, without extra yield or resources. DM-Q1 retains review of those
policy/balance choices. Forced mountain risk preserves ordinary consequences rather than making
the action safe by deleting roof.

## UI audit and settings

| Source surface | Purpose | Megamod destination | Difference |
| --- | --- | --- | --- |
| Six-checkbox mod-settings page and searchable research list | Configuration | Construction and mining / Roof preparation in common mod settings | Seven global/save rows, explicit None/unavailable choice and inheritance |
| Native mining/deconstruction/uninstall and roof areas | Actual gameplay orders | Existing native/contextual surfaces | Visible waiting/recovery and ownership-aware temporary roof areas |
| Source stopped-by-mountain message | Explain canceled work | Bounded contextual notification/inspect reason | Correct local order; no repeating notification loop |

Source supplies no bottom tab, debug override, corner toggle or manual mountain-roof tool. No new
one is selected. Settings persist through common globals/save overrides; actual map areas, targets
and jobs are gameplay state. Built-in defaults, effect timing, dormant overrides, save switching and
main-menu disabled callbacks are specified. Seven baseline languages remain implementation work.

## Consumed contracts and deliberate omissions

| Consumer/evidence | Actual binding | Selected treatment / verification |
| --- | --- | --- |
| As above, So below II, `astryl.AsAboveSoBelow2`, installed 1.7 | Source type and CheckForRoofsBeforeJob signature; wraps delegates from that source assembly, clamps scoped FloodFiller work to bands/gutters | Internal binding unsupported; direct narrow level adapter with separate exact provider-present player workflows |
| Source settings | Buildings, Mining, CancelThick, NonColonists, Animals, Thick, explicitNull and ThickResearch in global source configuration | No automatic migration; new common settings preserve intent explicitly |
| Source pending work | Native roof/removal jobs plus static unpersisted temporary coordinate lists | No general source-queue import or safe mid-save removal claim |
| Original/current provider present | `legodude17.smartdecon` or `Mlie.SmarterDeconstructionAndMining` | Owned overlap disabled with explanation; foreign state untouched and saved held owned work recoverable |

Hashes, signatures and bounded scan results are retained in research. A method-only facade would
not preserve the source-assembly/scoped-algorithm dependency. Optional research, roof and worker
providers need exact canonical identities and semantic admission; source marketing is not proof.
Arbitrary combat/fire, unsupported custom removal paths, automatic research-name matching, old
settings/jobs import, multiplayer certification and whole-mod safe-removal claims remain omitted
or unverified. No runtime XML/dependency/packaging change is made by this draft.

## Remaining implementation and verification

DM-Q1–4 in the [ledger](../refinement/specification-progress.md) retain policy review, effective native
mutation/continuation/area contracts, optional providers and runtime acceptance. Per-patch target
documents, honest focused RED/GREEN, package checks, independent implementation review, measured
balance/cost and personally observed native remove/roof/force/reload/settings workflows remain open.
No diagnostic-only assertion or clean startup can accept those gameplay tasks.

The draft has **9 requirements, 39 scenarios and 30 unchecked tasks**. Independent source review
identified one ordinary-construction cancellation gap; the parent/blocker hold and recovery contract
was added and its resolution rechecked. Independent composition review found no issues. Typed strict
validation passed **83 items, zero failed**; all eight scoped Markdown documents passed local-link
and whitespace checks. These documentation checks do not establish runtime compatibility.
