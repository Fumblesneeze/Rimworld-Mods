# Allow Tool — sequential research journal

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target 1.7 beta.
Queue S008 / P1-08. **Research notes only**, not implementation authorization. The completed source
pass is consolidated in [replacement coverage](../replacements/allow-tool.md) and the
[owning change](../../../../openspec/changes/specify-megamod-allow-tool/). The initial observations
below are retained as a research journal; the closing reconciliation supersedes its early open items.
Remaining AT-Q1–4 stay recorded instead of becoming questions about existing mod behavior.

## Provenance and reading order

1. Read [Steam description](https://steamcommunity.com/sharedfiles/filedetails/?id=761421485) and
   installed About.xml first. The linked [author forum](https://ludeon.com/forums/index.php?topic=17218.0)
   returned HTTP 403. Steam's generic removed/incompatible boilerplate is not used as evidence.
2. Installed source: `F:/Steam/steamapps/workshop/content/294100/761421485`, package
   `UnlimitedHugs.AllowTool`, metadata version **3.14.0**, declared support 1.0–1.6. Source requires
   HugsLib (its Version.xml requires 9.0.0). This does not establish our dependency or 1.7 support.
3. [Author repository](https://github.com/UnlimitedHugs/RimworldAllowTool), commit
   `cc168e824f883582ef2e1e3fab9225ae9594ba1d`, cloned read-only to ignored
   `artifacts/Refinement/megamod-spec-20261102/allow-tool/upstream`.
4. Installed `v1.6/Assemblies/AllowTool.dll`, SHA256
   `471F01CBC116269CE5CCD2588E6748C704CB099F512B91531847D6944147DA5C`, assembly version **3.6.0.0**,
   file version **3.14.0**, decompiled to adjacent `decompiled/`. The repository's packaged v1.6 DLL
   has the same hash. Repository `license.txt` is a
   public-domain dedication. Do not assume unrelated asset provenance from that alone.

## Inspected behavior so far

- **Allow/forbid:** area tools act on revealed forbiddable objects whose state actually changes.
  Allow all affects revealed haulable objects by default, excludes rotten corpses, Shift includes
  those remains and Ctrl includes non-haulable forbiddables. It reports affected count or no result.
- **Select similar:** groups by item definition plus material, not label alone. Single-cell click
  chooses a seed; Shift adds a seed; drag adds matches; no initial constraint makes the first selected
  object a seed. Source limit is 200, configurable 50–100000 in steps of 50; single-cell selection
  and Alt bypass it. Source uses an arithmetic short-hash key that can collide; ours must preserve
  actual type/material identity. Ordinary fog restrictions apply; source god-mode bypass needs the
  user's separate debug-action placement rule. Context actions include map/visible/home scopes.
- **Urgent hauling:** marks eligible haulable items outside valid best storage, adds ordinary haul
  designations where needed (e.g. chunks) and unforbids them. Shift requests native job reconsideration
  from free colonists; it is not an unconditional job teleport/override. Default separate Haul+
  work category is enabled. Source work giver intersects urgent designations with native haulables,
  then checks per-pawn hauling eligibility and gives a native storage haul job.
- **Pick Up And Haul:** source has an explicit `PickUpAndHaulCompatHandler` that creates its
  `WorkGiver_HaulToInventory` and routes urgent hauling to it when its base class/constructor fit.
  This supports cooperation; it is not evidence for the previously invented conflict. Our owned
  features will compose directly and respect real capacity. External package-ID admission still
  needs inspection; the source only looks up a type.
- **Urgent storage alert/cache:** high-priority alert for marked unreserved items with no better
  destination on the current map; lists up to five names plus ellipsis. Source caches designated/
  native-haulable intersection for one real-time second; distributes no-destination checks over
  updates and separately caches reservations. This design is evidence to replace, not a mandate for
  another cache. Use the shared map Thing index/native listers and recheck actual action eligibility.
  Cleanup removes urgent marks when the item leaves native haulables and finish marks on invalid
  targets; source cycles maps over 60 ticks. Inspect cleanup/placement interactions before specifying
  exact persistence and partial-stack outcomes.
- **Finish off:** downed living pawns only. Friendly targets require Shift to designate/force;
  automatic work uses existing explicit designations. Worker must be violence-capable with a melee
  verb and able to reserve/reach. Non-animal targets require Melee 6 by default; animals bypass that
  threshold. A developer-visible setting bypasses the threshold. Normal tool and reverse tool govern
  work-giver availability; separate Finish work category is initially off. Right-click supports
  drafted and undrafted workers, with disabled explanations.
- **Finish job:** reaches the victim, waits 60 ticks, performs a melee effect and execution damage,
  then ensures death. Source applies friendly-execution thoughts, prisoner tale and animal-slaughter
  record, optionally unforbids the surrounding 3×3 cells (initially on), and can chain designated
  targets within 8 cells when not player-forced. Interrupts if target is gone/dead/not downed or its
  designation is removed. These are game actions, not the custom item-destruction feature.
- **Drafted party hunting:** named drafted violence-capable pawns get an entity toggle (initially
  available); undrafting clears membership. Waiting combat pawns with Fire at will attack nearest
  visible, unfogged, factionless animals in weapon min/max range, excluding tame designations.
  Source uses range 2 for melee acquisition. Auto-finish defaults on, designated-only off, unforbid
  drops off; these three policies are currently save-wide despite being shown in a pawn gizmo menu.
  Source waits for nearby party members within 20 cells to stop being busy before finishing; searches
  within 20 cells, reserves normal finish work and queues return to the original position. Distinguish
  this from the user's later hunting-seat/dog feature; no replacement precedence has been chosen.
- **Fully grown harvest:** mature harvestable plants only, excludes protected special trees; Shift
  selects standard crops, Ctrl wood, both union. Source removes all existing designations before
  placing harvest: inspect conflicts and preserve unrelated designations unless explicit replacement
  is needed. Source ordinary harvest/chop/cut menu variants still need full inspection.
- **Connected ore mining:** starts from selected ores plus revealed existing mine designations,
  expands through adjacent same-kind resource rock, including connected fogged cells; does not seed
  entirely hidden veins from a fogged designation. Source accesses neighbor cells without explicit
  bounds checks and logs exceptions. Capture intended boundary/fog behavior instead of copying that.
- **Strip mining:** one-cell grid lines with five untouched cells between lines in each axis by
  default. Initial offset follows drag start; fixed alignment can reuse saved offset. Preview
  distinguishes mineable from invalid cells; fogged cells can receive planned mine orders without
  exposing their contents. Initially opens configuration on release; Shift inverts immediate/apply
  mode. Accept commits, cancel restores prior configuration; exact dialog ranges and retained
  parameter/settings classification still need inspection.
- **Context menus:** entries and order declared in `DesignatorContextMenuController`. Cancel offers
  selected/all designations/all blueprints; mass designation cancel preserves planning and
  noncancelable entries. Harvest/chop/full-grown have map/home variants; cut has blight; urgent haul
  map/visible; similar selection map/visible/home; other map actions include haul/hunt/strip/finish.
  General mass actions exclude fog and defer to each designator's eligibility. Context key invokes
  first enabled custom action; native context options are appended but not invoked by that key.
  Reverse gizmos can pick up a reusable designator via Shift. With no selected designator and
  extended context enabled, the key tries the first visible reverse designator with an enabled
  custom action. Native direct/reverse lists and current-map presence constrain these entry points.
- **Settings inventory:** global hotkeys, context overlay/watermark, separate Haul+, extended context
  key, reverse picking, finish-unforbid, party hunting availability and storage alert initially on;
  icon replacement and separate Finish category off. Tool/menu/reverse entry visibility toggles
  initially on. Source uses HugsLib globals plus world/pawn state; ours uses the shared settings and
  entity-state distinction. Key bindings remain native input preferences with documented scope.
- **Work-priority initialization:** `EnsureAllColonistsHaveWorkTypeEnabled` assigns priority 3 to
  every eligible considered pawn whose current priority differs, not only those at zero. It includes
  player-faction pawns plus prisoners for prison-labor integration. Trace the controller's call timing
  before specifying a preservation policy; do not silently copy a reset of established player choices.

## Initial remaining work (subsequently reconciled below)

- Read remaining controller/utility, context subclasses, reverse-handler, key definitions, dialog
  ranges, special-tree fixes, patches and finalized XML load-folder selection. Check installed DLL
  parity for key paths, especially latest version changes and defaults.
- Inspect actual public/reflected/Def/save consumers and exact IDs, including work-category switches,
  original-provider co-installation and source-save migration boundaries. Source declarations are
  not proof of incoming consumers. No drop-in contract is promised yet.
- Review 1.7 native counterparts: already-vanilla behavior, lifecycle, ordering and supported hooks.
  Specify the effects of disabling a tool versus disabling existing queued work, and no-storage
  alert freshness without per-feature scans.
- Account for every source feature with observable scenarios, default/range values and edge cases;
  place technical hooks/cache analysis in design and intended deviations in proposal/report.
  UI audit must relocate save-wide policy from pawn context menus to common settings while retaining
  pawn hunting membership and current strip-mine geometry as gameplay state.
- Resolve demonstrable composition with PUAH, micro-designations, custom hunting and auto-blight
  under their own later changes. Do not invent a precedence conflict from names alone.

## Closing source reconciliation

Read remaining controller/utility/context classes, reverse handler, dialog, special-tree rules,
patches, Defs and load folders. The 1.6 source load list selects root plus v1.6; root work XML is
active. The old Designator_ProcessInput source file is not compiled. Checked installed decompilation
for null-before-fog failures, premature urgency removal and hidden-priority setup.

Corrections and additions to the early shorthand:

- CancelSelected means cancel matching designation types **throughout the map**, not cancel only
  selected objects. Our label is explicit and protects plans/noncancelable entries too.
- Bulk urgent actions deliberately exclude designate-haulable objects such as chunks; direct
  urgent selection includes them. Strip all excludes player-faction targets.
- Connected mining uses all eight neighboring cells, including diagonals. Source traversal lacks
  an explicit bounds check; ours must terminate safely at map edges.
- Strip spacing is 1–50, step one or Shift five, also adjustable by wheel. Source stores one
  world-wide origin; ours separates map planning geometry from initial global/save defaults.
- Source map loading sets hidden work types to priority 3 even if a pawn previously had another
  value. Ours retains visible-mode choices and makes hidden automatic priority explicit.
- Source removes urgency before attempted placement and when absent from native haul candidates;
  native 1.7 placement can redirect/fail. Our spec retains undelivered urgency through interruptions,
  partial stacks and merges. A reservation expiration comparison adds the 50-update interval twice.
- Source party auto-finish does not exclude tame marks even though shooting does. Our automatic
  selection protects both; explicit finishing remains separate. New commands supersede old returns.
- Its kill hook allows any corpse killed by a party member, not only shot animal prey. The final
  spec preserves this with an accurate setting label and melee/finishing/non-prey scenarios.
  Recovery removes finishing authorization, rather than leaving a dormant execution mark.
- Core work-type comparison confirms Haul+ between Bed rest and Basic, Finish between Basic and
  Warden when manual priorities tie or simple priorities apply. Explicit unequal priorities win.
- Native 1.7 already has generic designate-all/remove-all context entries and plant cutting guards;
  reuse/preserve those semantics instead of duplicating menus or bypassing protected plants.
- Source forbiddable enhancement returns only its first gizmo. Preserve unrelated controls in ours.

Concrete consumer research found PUAH's display-name/urgent-Def binding, TD Pack's package-gated work
type and texture, Lifters' work XML plus original-package dependency, Some Things Float's Def-only
integration, and the compiled repair patch's reflected AllowTool types. Exact bindings, input hashes,
proposed treatment and remaining compiled parity work are in the replacement report/design. These
are compatibility evidence, not permission to expand the selected replacement queue.

One owned change now has four observable specs, a proposal, technical design and unchecked tasks.
Remaining research is AT-Q1 native target semantics, AT-Q2 consumers/adapters, AT-Q3 save/order
lifecycle and AT-Q4 measured balance/freshness. No code, package, native game state or runtime
acceptance has been changed by this research.
