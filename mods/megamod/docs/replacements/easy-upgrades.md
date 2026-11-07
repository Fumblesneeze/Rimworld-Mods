# Easy Upgrades (Continued)

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Replacement **S082/P2-11**, `Mlie.EasyUpgrades`, ToyWalrus / continuation Mlie,
[Workshop2904429048](https://steamcommunity.com/sharedfiles/filedetails/?id=2904429048).
Installed manifest1.6.3; supported1.1–1.6; assembly1.6.0.0/file1.6.3. Selected DLL SHA256
`378FF74A590A92C819C53E1404AD7488B11E9A739C107FD340B11B2B3DC37796`, matching committed DLL in
[source b2170be74708814fa4520e0dfe83ce0005939b0a](https://github.com/emipa606/EasyUpgrades/tree/b2170be74708814fa4520e0dfe83ce0005939b0a).
MIT notices recorded in [research](../refinement/2026-11-04-easy-upgrades-research.md). No source
assembly, art or code copied into product; binary identity is not beta compatibility evidence.

[Proposal](../../../../openspec/changes/specify-megamod-easy-upgrades/proposal.md),
[design](../../../../openspec/changes/specify-megamod-easy-upgrades/design.md),
[conversion spec](../../../../openspec/changes/specify-megamod-easy-upgrades/specs/megamod-building-conversion/spec.md),
[quality spec](../../../../openspec/changes/specify-megamod-easy-upgrades/specs/megamod-quality-reworking/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-easy-upgrades/tasks.md).

## Observable coverage

All rows **specified only; nothing implemented or accepted**.

| Scope | Player action → proposed result | Boundary |
| --- | --- | --- |
| Core conversions | Upgrade/downgrade one of six pairs → paid differential conversion | Includes installed stool/chair; current destination prerequisites and material validity |
| Accounting | Deliver/work/cancel → exact charge/refund/recovery | No whole merged-stack deletion, stale price, double fuel or full salvage added to differential refund |
| State | Convert configured building → retained supported state | Bills/groups/owners/contents/style and original quality/damage fraction; unsafe provider state refuses |
| Quality attempt | Designate eligible building/ground equipment/sculpture → one outcome | Real worker/station/route, one item and payment; no automatic repeat |
| Outcomes | Skilled work → one-step improvement/deterioration or unchanged | Exact defaults/chances/work/HP/XP and floor1; no replay, unrelated designation removal or new production credit |
| Settings/UI | Configure common rows/use contextual orders | Global/save inheritance; protected main menu; no new bottom tab |
| Compatibility | Changed/source provider → one overlap owner or local fallback | Semantic target checks and warnings; external routes require provider proof |

## Deviations and composition

Explicit conversion differs from S028 reconstruction: common structure is retained and quality/damage
are preserved, not rerolled/healed for a low conversion price. S013 retains unmet quality budgets;
reworking that achieves the target closes its run normally and clears its count. Later deterioration
requires an explicit new construction request; neither action silently starts another cycle. S054 repair knowledge
governs quality work because outcomes can heal. Linked bills/storage, material delivery and item facts
compose through their owned policies without patches against our own features or duplicate caches.

Correct source research/work-assignment/operability inconsistencies, unsafe cancellation/save paths,
whole-stack accounting, fuel-return risks and UI chance/name handling. Source one-material pricing,
bulk multiplier and short work caps remain explicit provisional baselines. The user selected a
Masterwork ceiling; five eligible starting qualities replace the source Legendary reworking path,
subject to EU-Q1 measurements. Full repair on quality success and partial repair on neutral are
retained; deterioration stops at1HP. These are draft choices, not observed source fixes.
External target destruction cancels conversion and recovers surviving delivered inputs only, never
recreating the building or refunding its structure.

## UI and settings audit

Source contextual Upgrade/Downgrade/quality gizmos, Orders upgrade/quality designators and normal job
feedback remain contextual mechanic controls. No bottom tab found. Orders and worker/station identity
are entity state. Source global settings become Work / Upgrades and quality reworking rows with
Global default, Current save and Use global, dormant override retention, safe resets and both save
controls disabled at the main menu. Reference skill is labeled; actual worker chances are shown when
known. Seven-language work remains pending; do not copy the ItemForbid hotkey collision.

## External contracts, omissions and implementation state

Colony Manager Redux's installed package-gated XML consumes the source comp/property identities and
Stuff-substitution convention for manager stations. A proven optional route adapter is preferable
to package spoofing; no such adapter or safe provider-state transfer is implemented. Public source
helper/job/designator names are inventoried without speculative facades. Original providers own new
overlap when active; existing owned work retains safe recovery. No original-save/settings migration
or safe assembly removal mid-job is promised.

Research inventories **17 optional routes /38 directed source conversions including Core**, plus the
separate incoming manager pair. VGP/VFE/Medieval content and Architect material mapping are not yet
verified on beta. Quality Of Building graphics support is a source intent, not a tested provider
claim. Missing provider absence is quiet; changed required shape warns locally.

**Implemented: nothing.** Core, future XML Extensions and only actually used Harmony dependencies
must be packaged truthfully; other content remains optional. Every actual patch still needs a target
document and executable minimal-semantic admission, owned-only rollback and later revalidation.
EU-Q1–5 retain balance, current-native lifecycle, consumer/provider, composition and native UI evidence
work. All runtime tasks remain unchecked; schema validation is not gameplay acceptance.
