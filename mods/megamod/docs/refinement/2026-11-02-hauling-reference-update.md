# Hauling reference selection update — 2026-11-02

Updated **2026-11-03** with the Armful Haul comparison and required visible cargo.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
This records the user's updated research inputs. It is not a completed source comparison, a new
behavior spec, or an assertion that any downloaded implementation is accepted on beta.

**Subsequent completion:** the [2026-11-03 composition decision](2026-11-03-hauling-composition-decisions.md)
now closes PH-Q5 for the reviewed specification draft after Continued, Reworked, Armful and the
narrow modern While You're Up audit. Pending-comparison statements below record earlier checkpoints.
The linked decision supersedes those states; it does not complete full S125/S133/S135 analysis or
any implementation/native acceptance.

## User direction and queue ownership

- **S021 / P1-16:** use **Pick Up and Haul - Continued** as the primary current reference in place
  of the previously analyzed Mehni release. Also inspect **Pick Up And Haul - Reworked** as an
  alternative. The user's suggestion that it may be more modern is a research hypothesis, not a
  conclusion about correctness, performance or compatibility.
- **S125:** add **While You're Up** as selected functionality. It previously appeared only as an
  incoming compatibility consumer in S021 research, not as a selected queue row. Use the user's
  newly installed **1.7 port** as a current reference, retaining CodeOptimist's original for
  provenance and comparison.
- **S126:** inspect **dd.Armful Haul** as another hauling implementation reference. The user
  explicitly requires **visible hauled stacks** at minimum; this selected subset gets its own
  separate megamod-owned change for that source during the deep dive. Broader Armful behavior is comparison input, not an
  automatic request to adopt every option or stack-capacity multiplier.
- Continued/Reworked variants are S021 references, not additional duplicated feature entries.
  S021 retains its owning change; S125 gets its own change during its sequential deep dive. Compose
  opportunistic and batch hauling directly in owned code, without internal patches or separate caches.
- Carrying-capacity limits, shared Ctrl+F data and all prior user requirements remain authoritative.
  No fork's claimed unlimited/extra carrying allowance, API, settings, UI or cache is automatically adopted.

## Exact installed references

All paths are beneath `F:/Steam/steamapps/workshop/content/294100/`. Installed means present on disk,
not active or verified. Read metadata and public documentation before inspecting implementation.

| Queue / role | Installed title / author | Package ID | Workshop | Metadata version / advertised game versions |
| --- | --- | --- | --- | --- |
| S021 primary | Pick Up and Haul - Continued / Zzyxzz | `Zzyxzz.PickUpAndHaulReworked` | [3829549663](https://steamcommunity.com/sharedfiles/filedetails/?id=3829549663) | 0.3.0 / 1.6, 1.7 |
| S021 alternative | Pick Up And Haul - Reworked / Dreadarm, original Mehni | `Dreadarm.PickUpAndHaul` | [3813653998](https://steamcommunity.com/sharedfiles/filedetails/?id=3813653998) | 0.1.40 / 1.6, 1.7 |
| S125 current reference | While You're Up/顺路搬运 / xiaopu | `patch1.7.whileyoureup` | [3832180338](https://steamcommunity.com/sharedfiles/filedetails/?id=3832180338) | No modVersion in About / 1.6, 1.7 |
| S125 original reference | While You're Up (with add-on for PUAH) / CodeOptimist | `CodeOptimist.JobsOfOpportunity` | [2034960453](https://steamcommunity.com/sharedfiles/filedetails/?id=2034960453) | Advertises through 1.5 |
| S126 visible cargo and S021 implementation comparator | dd.Armful Haul / dd (Workshop author moontmsai) | `dd.armfulhaul` | [3808100512](https://steamcommunity.com/sharedfiles/filedetails/?id=3808100512) | 0.4.2 / 1.6, 1.7 |

The Continued title has **Reworked in its package ID**. It is distinct from Dreadarm's Reworked
item. Preserve these identities rather than matching the word Reworked alone.

| Evidence file | SHA256 |
| --- | --- |
| `3829549663/About/About.xml` | `B5BC3A0D2A8A076C8062198DF63210557CA311D22881439D3FB6234BE57AA10C` |
| `3829549663/1.7/Assemblies/PickUpAndHaul.dll` | `C23E389F023C74397E2F7D53C53EF03825EF67A2718C9E3491AEADB0A4AE243C` |
| `3813653998/About/About.xml` | `4067FD072A8D718384449255C60EA01B1850AF973052AB59E71AD288C8924AAB` |
| `3813653998/1.7/Assemblies/Dreadarm.PickUpAndHaul.dll` | `26E8D95E44BAB8B703617E06A7BD53EF85E23CB62E8DB0708E13A20E9D453BBE` |
| `3832180338/About/About.xml` | `D0B24A68FD7D3CCE2391C6629C88C87F8DE48481B9D2AC6FB5D7B76017854F54` |
| `3832180338/1.7/Assemblies/WhileYoureUp.dll` | `867B0C299677621C03888204FDD1045DBF64EBA257FBBBAA38F42C4647F2E1F2` |
| `2034960453/About/About.xml` | `7715238E6045DA797EB0F4C1EA3EB3DE466D1A4E62F409F6573154FE0F4670E4` |
| `3808100512/About/About.xml` | `29410BE7EC8206891DE33934C3DF52725AE5738EE72CEEE13CDE2387932965BD` |
| `3808100512/1.7/Assemblies/ArmfulHaul.dll` | `0CE113A67871A95F74867FCAF7A83CE9A72AF9912B7894D61708F3A081289FCB` |

Reworked explicitly loads `Common` plus `1.7`; the new While You're Up loads `/` plus `1.7`.
Continued has separate 1.6/1.7 assemblies. These hashes identify research inputs, never runtime
compatibility admission predicates. No installed files were changed or enabled.
Armful Haul explicitly loads its `1.7` folder on 1.7; source files and a README are present for
read-only analysis. Its README was read at the later preparation checkpoint below; the source audit
remains next.

## Documentation inspected and comparison leads

The [Continued public repository](https://github.com/Zzyxz/RimWorld-PickUpAndHaulContinued)
and installed About describe a maintained 1.6/1.7 continuation, restored Combat Extended support
and protection of personal supplies; About also names compatibility with Stack Gap, Simple Baby
Carry and Achtung. Its metadata requires Harmony and excludes the original Mehni package.
The repository describes MIT licensing. These are author claims requiring source/consumer checks;
they supersede any assumption that the old source's stubbed CE helpers describe this fork.

The Reworked Workshop page links its
[user guide and API reference](https://rmapi.dreadarm.net/mods/pick-up-and-haul/).
Installed About describes opportunistic batches, transport loading, storage merging, prioritized
hauling, personal supplies, configurable search/recovery and physical orphan-cargo drops. It warns
that old settings/cargo/active jobs are not imported, unlike Continued's advertised easy save swap.
Read the exact implementation before choosing behavior or promising migration. Preserve its
UPSTREAM-LICENSE notice and inspect any other licensing before considering reuse; independent
implementation remains the plan.

The original While You're Up Workshop description identifies opportunistic route hauling,
moving supplies closer to work, improved multi-destination unloading, destination caching and
storage-building filters. The new port's installed English About describes those features, a
developer path overlay and live switch, plus claimed 1.7 code/translation fixes. It identifies the
original as AGPL-3.0 and credits the temporary 1.6 port, Workshop 3516529053. No new-port source
repository is supplied in About; inspect available files or decompile read-only for gaps.

The new port's description says PUAH has no 1.7 release, while both selected hauling references
advertise 1.7 and ship versioned assemblies. Treat that sentence as stale documentation, not proof
that its optional integration binds either fork. Its exact package/type/assembly/driver assumptions
must be inspected. Developer path visualization follows our debug/UI rules; individual entity
choices and true mod settings must be distinguished before reproducing any controls.

Web retrieval succeeded for Continued's GitHub, Reworked's Workshop and original While You're Up's
Workshop. Continued/new-WYU Workshop pages and the RMAPI guide were inaccessible through the web
tool in this intake pass; their installed metadata was readable. This does not imply those sites
or mods are globally unavailable. Further documentation/source retrieval remains research work.

## Required reconciliation and truthful status

The Armful Haul Workshop page **does explicitly state incompatibility with Pick Up And Haul**.
That establishes the author's stated policy, not a diagnosed cause or proof against every fork.
Its public documentation describes multiple hand-carried stacks, native delivery paths and visible
cargo on the pawn and contextual UI. It describes a per-stack native limit and extra stack counts;
these must not silently become several independent carrying allowances in our implementation.
All variants are research inputs, not a request to run their competing patches together.

**Confirmed minimum for S126:** hauled stacks are visible on the moving pawn during the actual
haul, rather than represented only by hidden inventory or text. Visibility must follow the actual
held cargo as pickups, deliveries and interruptions change it; it grants no extra items or carrying
capacity. The subsequent [source audit](2026-11-03-armful-haul-research.md) and
[S126 draft](../../../../openspec/changes/specify-megamod-visible-haul-cargo/) now record layout,
stack representation, view controls and the limits of broader adoption. Any player-visible raster selection/modification and
final rendering acceptance follows the repository asset/live-review gates.

Compare the implementations on concrete behavior and simplicity: native job reuse, amount/capacity
accounting, reservations and multi-hauler coordination, travel/unload ordering, interruption and
save recovery, personal supplies, external contracts, patch assumptions, bounded search and shared
data cost, plus readable physical cargo. Record advantages, defects and chosen composed behavior
with evidence. “More modern” or the author's performance claims do not decide the winner. The
visible-cargo requirement remains selected even if another implementation supplies the hauling logic.

The [earlier S021 research](2026-11-02-pick-up-and-haul-research.md) inspected
**Mehni.PickUpAndHaul / 1279012058**, not these continuations. Retain its hashes and actual legacy
consumer evidence as historical comparison; never relabel them as new-fork findings.
The existing [S021 change](../../../../openspec/changes/specify-megamod-batch-hauling/) stays a
**draft requiring reference reconciliation (PH-Q5)**. Its prior review/validation established the
legacy-based draft only. User capacity/shared-data decisions remain valid; source-derived defaults,
defects, public contracts, co-installation, optional integrations and save claims must be revisited.

The [Continued source audit](2026-11-03-pick-up-and-haul-continued-research.md) now covers all 17 C#
files and selected installed 1.7 bodies. The [Reworked audit](2026-11-03-pick-up-and-haul-reworked-research.md)
also covers its author guide/API and all 44 decompiled behavior/API/support files. The
[Armful audit](2026-11-03-armful-haul-research.md) now covers all 17 source files and selected
installed bodies, with a comparison table and its separate selected-subset visual draft.
Next research: the narrow current While You're Up intersection, one implementation at a time;
record observed differences and selected outcomes in research/proposal/design/spec/tasks before
closing PH-Q5. S125's complete opportunistic-hauling analysis remains separately queued with its
modern reference (WY-Q1); S126 owns the required Armful-inspired visual subset, now
[drafted](../../../../openspec/changes/specify-megamod-visible-haul-cargo/), with AH-Q1–4 retaining
integration, visual/provider proof and acceptance. A narrow intersection is not complete S125 coverage. Then
continue the existing source order, with S031 still the next untouched original queue entry.

## Later transport-loading addition

Later addition **S133 / Bulk Load For Transporters** (`Ilarion.BulkLoadForTransporters`, Workshop
3559292908) has a separate [identity intake](2026-11-01-source-audit.md#later-additions-outside-the-photograph-audit)
and **BLT-Q1** research. Compare its loading/unloading workflows and actual PUAH bindings against
loading already supplied by these forks. Its eventual owned behavior composes directly with S021,
using shared data and one carrying allowance; no internal self-patches or required upstream PUAH.
The narrow comparison does not complete S133's separate specification.

Later **S135 / Basic Caravan Loading** (`LM420.BCL`, Workshop 3370434966) assigns caravan
gathering and carrier unloading to Basic work in its inspected XML. **BCL-Q1** owns the later
observable priority/worker specification and guarded XML work. Check the S021/S133 unloading and
loading composition without assuming a conflict or silently
reprioritizing all hauling. Its identity/patch hash is in the same audit supplement.

## Continued preparation checkpoint and subsequent audit

The following records the initial preparation checkpoint. It is superseded for behavior coverage
by the [full Continued audit](2026-11-03-pick-up-and-haul-continued-research.md), which traces capacity,
drafting, collection, cargo, recovery and optional integrations. Reworked/Armful comparison and
reconciliation of the observable draft remain pending under PH-Q5.

The public Continued checkout was pinned at `172f225578de7243a97b649b58fd00451fc1189f`
(2026-11-01, commit subject `Leave cart cargo out of hauling runs`) under ignored local evidence
`artifacts/Refinement/megamod-spec-20261103/pick-up-and-haul-continued/upstream`.
The README and `Source/PickUpAndHaul/Settings.cs` were read. Read-only decompilation of the installed
1.7 `PickUpAndHaul.Settings` type is retained beside it in `installed-settings/`.
Its setting declarations/defaults and persistence agree with that source class: corpses off,
animals/mechs on, 20% minimum free-space UI default, `haulCarryFactor` 1.3 with a 100–200% UI range
and 1–2 load clamp, and `dropHauledWhenDrafted` true. These are source settings, not selected megamod
defaults. At that checkpoint, capacity callers and drafting behavior had not yet been traced, so
the names/defaults alone did not establish their effective behavior or a carrying-capacity defect. This comparison
does not establish whole-checkout correspondence to the installed assembly.

No completed multi-reference reconciliation, new behavior acceptance, build or game launch is claimed by
this selection update. Metadata version numbers and author claims do not establish a better implementation.

The subsequent Reworked audit retrieved the author overview, guide and API as static HTML through
a direct HTTP read. The earlier web-reader limitation no longer blocks that documentation. Both
current PUAH implementations have source audits; the full multi-reference decision remains PH-Q5.

## Armful documentation checkpoint and subsequent audit

After the two PUAH source audits, Armful's Workshop description, installed About and full Korean
README were read. About/DLL hashes remain as recorded above. README SHA256:
`18F9BE2A5F653836E6F58F50AF09D78EC0C6E700998659EC393E063ABB78DA0C`.
There are **17 C# files** in `3808100512/Source/ArmfulHaul`; their implementation had not yet been
read at this initial checkpoint. The subsequent [full audit](2026-11-03-armful-haul-research.md)
supersedes that coverage state: every source file and selected installed 1.7 bodies were read.

The README describes native-job hand stacks for storage, construction, bill ingredients and
transporter loading; it explicitly excludes inventory-based caravan packing. It also documents a
1.7 contextual prison-food haul command, DSU/port and Logical Floors integrations, stack rendering,
per-worker stack multipliers and bounded searches. These are additional comparison leads, not
automatic scope adoption or verified runtime behavior. Its own verification notes retain live-run
gaps; no author assertion substitutes for our final acceptance. S126 still requires visible cargo
without extra carrying allowances, while broader chosen behavior belongs in the appropriate owned
hauling/loading/bill change with provenance preserved.

The audit confirms multiple physical hand stacks with separate native per-stack allowances,
native storage/construction/bill/loading wrappers, reflected floor/port integrations and three
display hooks. It records source-level recovery/admission risks without claiming a reproduced
crash or universal fork incompatibility. S126's observable draft covers the selected visible
subset, common settings and truthful contextual information; final S021 defaults/implementation
selection still await the narrow modern While You're Up intersection and PH-Q5 reconciliation.
