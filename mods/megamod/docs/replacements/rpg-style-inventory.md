# RPG Style Inventory Revamped

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement **S132**, selected source `Sandy.RPGStyleInventory.avilmask.Revamped`,
[Workshop2478833213](https://steamcommunity.com/sharedfiles/filedetails/?id=2478833213).
Sandy original, avilmask revamp, Workshop avil. Inspected2026-11-06–07, installed1.6 main assembly
version1.0.0.0 SHA256`cf30287e5a95597866690b27892097e8fd9ba8762a55418e9f4a3080ea96d143`;
CE adapter SHA256`7c8a2193a917d8dee033270c50284dce830449ec3f0ed4c6b9715032d62024e5`.
Metadata only advertises1.2–1.6. Author beta/compatibility claims remain unaccepted.

[Research](../refinement/2026-11-07-rpg-style-inventory-research.md) records author documentation,
complete installed code/XML, exact native beta/consumer hashes, source defects and unresolved
provider evidence. Public root/README read, full public source/license not established. No upstream
code, bundled Harmony, assemblies or artwork copied; no runtime implementation or game launch.

## Behavior inventory and coverage

All rows **specified only**, with no implementation, RED/GREEN or live acceptance.
[Observable contract](../../../../openspec/changes/specify-megamod-gear-presentation/specs/megamod-gear-presentation/spec.md).

| ID | Native input/action → selected result | Defaults / corrections / limits |
| --- | --- | --- |
| GP01 | Open/switch Gear layouts → complete real equipment/inventory | Classic default; Simple/List alternatives; one actionable entry, subordinate coverage ghosts, fallback items, bounded layout and scroll |
| GP02 | Inspect/hover item → current quality/condition/status | S131 palette; bars stay on hover; readable forced/locked/tainted/cargo and neutral details; no invented HP/quality |
| GP03 | Info/force/drop/ingest → actual eligible native action | Current restrictions, holder recheck and confirmations; no source-era permission clone or instant fabricated result |
| GP04 | Inspect stats → actual mass/temp/vacuum/armor | Native capped overall summary distinct from individual uncapped ratings; provider units preserved |
| GP05 | View/edit supplies → same owned pawn orders/cargo | S021/S126/S062/S123/S124 direct composition, real quantities/capacity, S127 subject/S130 policy authority |
| GP06 | Configure or switch view → scoped preference or temporary view | Common global/save rows, per-race default collection, bottom-right current views, top debug overrides; no new bottom tab |
| GP07 | Use admitted provider gear → real provider action/information | RWoM enchantments, Fortified control; CE weight/bulk/equipment/loadouts deferred; essential drift retains provider Gear |
| GP08 | Start with compatible/changed/original provider → admitted UI or safe fallback | Exact entry substitution, sibling preservation, bounded warning, no source-ID spoof/import |

## Deviations and direct composition

- Preserve three useful layouts and slot extension intent, while correcting duplicated/hidden fallback
  items, Human-only coverage assumptions, unbounded malformed coordinates, scroll/overlap and state leaks.
- Retain current native action permissions and confirmation paths, including being carried,
  destruction-on-drop and bandwidth loss. Source's advertised cosmetic scope does not prove its copied
  actions match the current game. Actual orders still consume time/resources and follow native rules.
- One shared quality palette/editor replaces RPG's separate seven colors and noncancelable picker.
  Initial palette is S131 Default, with RPG selectable. Independent condition/status/cargo meanings stay.
- Source HP/temperature initializer=false versus saved-key default=true resolves explicitly to true.
  Size706×500 is preferred, screen-fit remains safe; rendering never writes settings.
- Current views move to bottom-right; per-race layout defaults/settings use common scope; developer
  gear options move to top toolbar. Portrait is inspection, not an appearance override.
- Owned medicine/meal/drug stocks, cargo, HUD and apparel-policy controls compose directly. No
  self-patching, enlarged hauling inventory, separate map scan or duplicated target store.
- No legacy source settings migration, package/assembly/type spoof or automatic external XML alias.
  Sidearms/multiplayer claims and exact current CE/RWoM provider proof remain open research.

## UI and settings audit

| Source entry | Purpose | Owned destination |
| --- | --- | --- |
| Gear tab, item menus, inventory lists | Contextual inspection and item orders | Existing native Gear, no new bottom main tab |
| Show list / Simple / portrait hat toggle | Current view | Compact contextual bottom-right view control; no settings write |
| Portrait rotation | Inspect same pawn from another angle | Portrait control; no pawn mutation |
| Dimensions/backgrounds/bars/colors/race preference | Configuration | Ordered common Interface / Gear presentation and shared Quality colors settings |
| Dev gear button | Unrestricted equipment override | Top debug toolbar, developer mode only |
| Supply orders / optional CE loadouts | Pawn gameplay choices | Contextual Gear/provider management, not global/save-inherited configuration |

All real preferences use global defaults plus save overrides and Use global. Inheritance disables
the save-value editor, not the loaded-save Use global checkbox; main menu disables both. Per-race
defaults inherit as one collection. Temporary view state resets on game change/reload. Missing/invalid
values fall back safely; failed/stale commits preserve prior values. Seven languages remain required.

## External contracts and persistence

| Evidence | Preserved / adapted / unsupported |
| --- | --- |
| Fortified AOBA.Framework selected1.7 RPG/native CanControlColonist postfixes | Consume effective native/provider authority; no fake RPG class. Exact action workflow remains future proof |
| Smart Medicine Continued PatchRPG short-name reflection into rows/drop/FillTab | Direct owned S062 composition; external reflection binding not promised; provider co-install uses its own overlap policy |
| Public ItemSlotDef/anchors/source Def identifiers | Useful owned declarative semantics; legacy fully-qualified XML needs explicit port/adapter. Bounded installed scan found no actual XML consumer, not universal absence |
| Source CEAccess/CEPatches and RWoMIntegration | CE deferred; optional RWoM isolated adapter, actual release shape/content gates required; shipped references do not establish installed provider presence |
| Source settings and per-race name keys | No automatic import; owned defaults/inheritance, no foreign file mutation or item conversion |

CE and RWoM canonical packages were not found in the inspected local/Workshop metadata; the
CombatExtended.dll inside Bill Doors' framework is not the actual CE provider. Source integrations
were inspected fully, but their current-provider acceptance cannot be inferred. No general drop-in
or multiplayer compatibility claim is made.

Original or Revamped RPG active → withhold owned overlapping tab/layout with bounded explanation,
leave provider authoritative. Essential unknown provider behavior → retain provider Gear. Independent
cosmetic failure need not disable native actions. Required with implementation: Core, Harmony and
XML Extensions; optional RWoM/Fortified/source providers; CE deferred and relevant DLC content. XML uses
canonical package/content checks, additive layouts and only the intended tab-entry replacement;
preserve sibling tabs and foreign lists. Every actual patch needs indexed target spec and executable
admission, including effective code, compatible owners, failure groups and owned-only rollback.

## Verification and acceptance

[Tasks](../../../../openspec/changes/specify-megamod-gear-presentation/tasks.md) remain unchecked.
Independent document review/typed validation are tracked in the
[progress ledger](../refinement/specification-progress.md). They do not accept gameplay. Future
reviewed-build native evidence must show actual Gear actions/results, provider fallback, supply/cargo
composition, controls in their required locations, all languages/scales and save/main-menu behavior.
No resource, armor, capacity, research, policy or economy changes are intended; semantic review and
observed native outcomes remain required. RPG-Q2–Q5 retain explicit evidence gaps.
