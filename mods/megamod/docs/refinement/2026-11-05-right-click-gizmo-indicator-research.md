# S110 — Right-Click Gizmo Indicator research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, **1.7 beta**.
Origin **replacement**. Inspected **2026-11-05**. Specification only; no runtime or visual acceptance.
[Change](../../../../openspec/changes/specify-megamod-right-click-indicators/) and
[coverage](../replacements/right-click-gizmo-indicator.md).

## Public documentation and exact source identity

Read [Workshop3750529401](https://steamcommunity.com/sharedfiles/filedetails/?id=3750529401)
and the [author repository](https://github.com/CheaterEater-RM/Right-Click-Gizmo-Indicator) first.
The author describes a native-style top-right marker revealing right-click command menus, with
no settings/save state. README qualifies custom-drawn gizmos, naming mechanitor control groups as
an exception. It claims zero performance cost and says the check is cached per button; source
instead caches by Type and explicitly calls the first-instance result an accepted imperfection.
Neither promotional performance wording nor compatibility comments count as measured/live proof.

- Name **Right-Click Gizmo Indicator**, author **CheaterEater**, package
  **CheaterEater.RightClickGizmoIndicator**, Workshop **3750529401**.
- Installed root `F:/Steam/steamapps/workshop/content/294100/3750529401`; About modVersion **1.02**,
  supported1.6/1.7, required Harmony. Core/DLC loadAfter entries are not hard DLC dependencies.
  No LoadFolders file; version folders contain their respective assemblies.
- Selected `1.7/Assemblies/RightClickGizmoIndicator.dll`: **7,680 bytes**, SHA-256
  `CC34FDFCCBA080D5DA4980BFE9BE1219BA4D6BFDE4C255831324D772B0D53890`,
  assembly/file version **1.0.0.0**, informational **1.0.0**, **Release**,
  MVID **520c4d25-da9f-4ee0-b0ad-2330bffbd08e**.
- Author main commit **557a05c5717a996aaf9bf1b65b5b649a1d92f90a**, **2026-10-29T20:46:32Z**.
  The public1.7 DLL at that commit is byte-identical to the installed binary. All three runtime
  source files and all three decompiled runtime classes were read. Public README still says1.6;
  About/project/changelog and the installed folder explicitly include the1.7 update.
- Installed/public **MIT license, copyright2026 CheaterEater**; preserve copyright/license notices
  for any actual code reuse. Its explicit exception excludes included RimWorld art/other separately
  licensed textures. No binary, texture, preview or icon is copied or selected for the product.

Ignored evidence directory: `artifacts/Refinement/megamod-spec-20261105/right-click-gizmo-indicator/`.
It retains Workshop HTML, public commit/tree, ten raw public text files with size/hash/Git-blob
manifest, public/installed binary comparison, PE metadata, three-class decompilation, eighteen
initial native type extracts and three optional-provider class extracts. `input-manifest.json`
identifies thirteen initial source/native/optional/composition/decompiler-reference inputs. The
native update records described below retain a separate pinned snapshot and repeated extraction;
the original manifest is historical, not silently overwritten. No target assembly was executed;
the agent did not modify installed content, normal saves or configuration.

## Complete source inventory

| File / class | Inspected behavior | Owned choice |
| --- | --- | --- |
| Setup / RightClickGizmoIndicator_Init | Static startup, Harmony owner com.cheatereater.rightclickgizmoindicator, PatchAll; DevMode startup log | Guarded, idempotent admission using owned package identity; no source API impersonation |
| Patch_Command_GizmoOnGUIInt.Postfix | Runs on repaint; skips Disabled; allowed-area subtype shortcut; otherwise checks two type dictionaries; draws native icon; ErrorOnce on outer exception | Current instance/context capability, safe optional routes, no repeated frame diagnostics |
| TypeMayHaveMenuCached | Reflects RightClickFloatMenuOptions getter and records whether declaring type differs from Gizmo | Metadata is not effective behavior: foreign patches can add options to a base getter |
| HasMenuOnceForType | First enabled queried instance determines the type's menu answer for the application lifetime | No permanent type-wide availability; update actual command and modifier/selection state |
| EvaluateHasMenu | Suppresses Messages, calls property and FirstOrDefault, returns false on exception; finally pops suppression | No speculative stateful probe or hide-the-side-effect strategy; direct pure facts/admitted query |
| DrawExtraOptionsMarker | Forces white, calls Designator_Dropdown.DrawExtraOptionsIcon, restores previous color without finally | Native appearance reference, no duplicate marker; guaranteed render-state restoration |
| Patch_Messages_Suppress | ThreadStatic depth; prefix skips Message(Message,bool) within probe; nested Push/Pop | Omitted. It mutes feed/archive/sound but cannot undo other side effects |
| LICENSE/About/PublishedFileId | MIT with art exception, Harmony required,1.6/1.7 metadata | Attribution and correct dependency distinction |
| Public Analyzer.xml | Method-name profiling entries, including stale HasMenuOnce (runtime is HasMenuOnceForType) | Not installed and no demonstrated external consumer; no speculative facade |
| Public project/changelog/README | net10.0,1.7 output, game/UnityStubs/Harmony references; compatibility fragment | Build provenance only, no source rebuild or beta acceptance claimed |

Installed payload has LICENSE, two version DLLs and About metadata/images only. No Defs, patches,
settings, localization catalog, saved component or runtime texture files. The marker comes from
the native game. The public source cites docs/IMPLEMENTATION.md, but that path is absent from the
inspected repository tree; comments do not establish an additional unseen behavior contract.

The source cache avoids repeated probing but is semantically stale by construction for variable
instances/state. EvaluateHasMenu's swallowed exception can become a lifetime false result for
that type. Thread-local scoped message suppression still suppresses any synchronous message
produced during that probe, and does not cover windows, direct sounds, logging or game mutations.
The source says this explicitly. The replacement omits the suppression patch rather than claim
that hiding a message makes an arbitrary getter safe.

## Native1.7 route observations

Initial version **1.7.4998 rev1299**; Assembly-CSharp **15,633,920 bytes**, SHA-256
`FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B`.
During the final fingerprint check, the installation had changed to **1.7.5000 rev1275**:
Assembly-CSharp **15,636,480 bytes**, SHA-256
`D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`;
Version.txt **18 bytes**, SHA-256
`BAD5BED449D17B2AF39925A3A25007164B2B1F89B29A4E24AFA3B505EDCC535A`.
New bytes were pinned under ignored `native-update-input/` solely for offline inspection, with
`native-update-manifest.json`. All eighteen types were re-decompiled into `native-updated/`;
`native-update-comparison.json` records **18/18 byte-identical decompiled files**. The eight menu
getter declarations were independently re-enumerated on the pinned updated assembly and were
unchanged. Final installed hashes matched the pinned update. These comparisons support retaining
the scoped findings on the newer snapshot, not whole-game equivalence or patched/live acceptance.
The original manifest's two native entries refer to the earlier revision; its other eleven inputs
still match. Both revisions remain explicit.

Offline metadata enumerated every declaration named get_RightClickFloatMenuOptions: Gizmo,
Designator, MechanitorControlGroupGizmo, Command_Hide, Designator_Plan_Add,
Designator_ZoneAddStockpile, Designator_ZoneAdd_Growing and Designator_ZoneAdd_Fishing.
All relevant getter bodies were inspected; this enumeration is not a universal list of input
methods or foreign runtime patches that can open menus.

| Native evidence | Behavior and consequence |
| --- | --- |
| Gizmo / Command | Default menu is empty; ordinary/compact commands share GizmoOnGUIInt; disabled input returns Mouseover before menu routing; default size75, compact36 |
| GizmoGridDrawer | The displayed representative's first option determines float-menu versus ProcessInput routing. Only a chosen float-menu path combines eligible grouped members' options/actions. Do not infer a menu from any selected peer alone |
| Designator | Can yield designate-all and remove-designation entries, including disabled None rows. Getter can scan Map.listerThings and mutate shared tmpAllDesignations; marker needs presence, not counts or constructed callbacks |
| Designator_AreaAllowed | ProcessInput opens the allowed-area picker with management option after CheckCanInteract; does not expose it through a dedicated options override. Never call input to discover it |
| Designator_Plan_Add | Visibility options gated by virtual CanRightClickToggleVisibility; left-click color picker is separate. CanSelectColor and current input route matter; no universal all-ProcessInput-means-menu rule |
| Three zone-add types | Stockpile/growing/fishing override options using virtual ShowRightClickHideOptions; concrete hide menus remain nonempty even if no matching zone exists |
| Command_Hide and four concrete helpers | Plans/stockpile/growing/fishing return Show/Hide options; effects occur in callbacks. Some helpers lazily resolve icons, so a pure capability check need not enumerate them |
| Designator_Dropdown | Already draws PlusOptions; native helper16px,1px inset. ProcessInput builds float/grid choices; existing cue must not be duplicated |
| Designator_Build | Already draws native cue for MadeFromStuff even if later input rejects for missing materials; preserve those native-owned rules rather than remove its icon |
| MechanitorControlGroupGizmo | Direct custom Gizmo rendering and work-mode menu; outside ordinary Command hook, matching README's documented limitation |
| Messages | Message(Message,bool) controls accepted message feed, optional archive and sound; suppression can erase all three during the probe |

Eighteen inspected native types are the eight getter declarations plus Command, GizmoGridDrawer,
Designator_AreaAllowed, Designator_Dropdown, Designator_Build, Messages and the four concrete
Command_Hide helpers. The type inventory and source bodies support a specification; no screenshots,
finalized active-mod menus or effective patched render behavior were observed in a running game.

## Selected-source composition and optional provider

### Quick Stockpile Creation / S107

S107 already owns [quick storage](../replacements/quick-stockpile-creation.md). The source
SmartStockpileGizmo getter gathers current selected haulables/categories/filters and can emit a
no-common-categories message; it also always draws an existing native marker in GizmoOnGUI.
Its underlying priority helper is another reason not to probe merely while painting. The relevant
selected DLL is **57,344 bytes**, SHA-256
`95116E5400C9435E1C125A48C8AA1B19BBF09DCA1533E589A9A5DA29D46A0398`.
The source's code comment cites max-priority messages as its suppression motivation. Owned S107
commands must instead expose current menu facts directly and retain their specified no-unsolicited
popup behavior. Reuse the same owned marker path, no duplicate draw or internal patch stack.

### I Aint Building That: reference, not added replacement scope

[Provider documentation](https://steamcommunity.com/sharedfiles/filedetails/?id=3118060751)
describes right-click hiding/restoring build buttons and optional pawn-ability hiding. Installed
package **Taggerung.IAintBuildingThat**, supported through1.6, requires Harmony. LoadFolders selects
Common+1.6 under native older-version fallback. `1.6/Assemblies/IAintBuildingThat.dll` is
**30,208 bytes**, SHA-256 `754229A026809B974E60D52255C1EA7DB92A723A147443C2FE5EFE237FFE87F1`.
Three relevant classes were inspected; this is not a full I Aint Building That analysis.

- PatchInHideMenuOptionToDesignator adds choices after Designator.RightClickFloatMenuOptions,
  gated by menuCombo.ShouldShowMenu. It distinguishes placed/dropdown content; dropdown enumeration
  mutates DropdownGroupDefs bookkeeping. Do not invoke it as a passive capability probe.
- AbilityRightClickPatch patches the **Gizmo base getter**, then checks enableAbilityHiding,
  Command_Ability, that command's ability-owned CompAbilityHide and current modifier policy before appending an option.
  This demonstrates why the source's declaring-type filter can miss a real foreign-added menu.
- PatchInHideMenuOptionToDesignatorProcessInput tracks right clicks and supplies a fallback menu
  for place designators if no float menu is already open, also respecting current modifier policy.

The getter Harmony attributes were initially undecodable without the matching Harmony reference;
re-decompilation with the installed1.6 Harmony directory resolved exact Gizmo/Designator getter
targets. This is static target evidence, not proof the old provider works on beta. Its selected XML
patch file contains only commented examples; no XML mutation is proposed for this integration.

Workshop comments complain that many buttons receive markers when this provider is used. That
does not prove false positives: the provider really adds menus. Owned policy indicates genuine
current menus, including Hide/Restore, without an unrequested blacklist. Preserve the provider's
settings and modifier behavior; do not adopt its entire hiding feature or UI into S110.

## Consumers, UI and persistence

Bounded case-insensitive raw scan of installed Workshop/local Mods `*.xml`, `*.cs`, `*.dll` for
RightClickGizmoIndicator/package identity found only its About and two DLLs. No actual external
consumer found; constructed reflection, UTF-16, archives and remote code are not ruled out.
Public initializer and private patch/type names need no speculative facade. No source save or
settings keys. No runtime data migration; this is not a promise about uninstalling all megamod features.

UI is an affordance on an existing command, not a new gameplay window, setting or operational
toggle. No bottom tab, bottom-right control or debug toolbar entry. No new configuration/global-save
row. No new user text is necessary for a native marker; any added player-facing explanation must
be localized, and all seven language layouts remain in the future verification scope. No image
was selected/generated/copied; actual native appearance acceptance remains gated by the asset skill.

## Draft corrections and semantic review

Adopt normal/compact markers, allowed-area handling and ordinary modded menus; preserve existing
native indicators. Correct per-type stale answers, base-getter false negatives, unsafe probes,
suppressed real messages, duplicate native/provider draws, corner collisions and missing guaranteed
render-state restoration. Keep explicit custom-renderer limitations rather than expand this slice
to an unrelated UI redesign. Original indicator active → owned overlap inactive with warning.

Comparators are native order menus (including disabled rows), allowed-area pickers, plan/zone
visibility menus and material/dropdown cues. No economic or progression value changes; original
orders, group target counts, messages, costs, eligibility and input timing remain authoritative.
No private Thing index or map scan is needed to determine these native menu-presence facts.
Cold/warm/recreated-command costs and actual action preservation remain unmeasured/unverified.

| ID | Remaining work |
| --- | --- |
| RCGI-Q1 | Effective render/menu/input/group targets, existing marker ownership, ordinary native coverage and later repatches |
| RCGI-Q2 | Safe current optional capability/provider beta gates, actual consumers and direct owned composition |
| RCGI-Q3 | Scaled/counter/compact appearance, performance, seven-language usability, honest TDD and reviewed-build native/visual acceptance |

Open questions remain in the ledger; no user interview is required for source behavior. All runtime
tasks remain unchecked. Independent document review and strict validation are recorded separately.
