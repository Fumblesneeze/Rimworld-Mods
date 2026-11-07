# S131 — Quality Colors (Continued) research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement; read-only analysis on 2026-11-06. [Coverage](../replacements/quality-colors.md),
[owned change](../../../../openspec/changes/specify-megamod-quality-colors/). No behavior implemented.

## Documentation first and exact identity

Read the [author's Steam page](https://steamcommunity.com/sharedfiles/filedetails/?id=3513846773)
before compiled inspection. It describes quality-based trade colors, customization, six presets,
extended gear/mouseover/inspect coloring, condition text and a revised picker/cache. Its universal
compatibility assertion is not accepted. Credits include legodude17, Doom, Erin and translators.
The source link redirects from BloockyPixels/QualityColours to
[DawnsGlow/QualityColours](https://github.com/DawnsGlow/QualityColours). Root listing was visible,
but source subtree/API/direct fetches failed or timed out; no commit/source snapshot was obtained.
No raw Workshop HTML was retained for this entry; web-read author evidence is distinct from local
decompilation. No license file was found in installed content or the visible repository root;
this does not establish universal absence. Independently engineer; no source/code/assets/DLL copying.

Installed root `F:/Steam/steamapps/workshop/content/294100/3513846773`, author **DawnsGlow**,
package **DawnsGlow.qualcolor**, advertised1.2–1.7, requires Harmony, no modVersion/LoadFolders found.
Selected1.7 assembly/file version1.0.0.0. Installed languages English/German/ChineseTraditional.
No source/readme/license located locally. All selected assembly source files decompiled read-only.

| Input | Bytes | SHA256 |
| --- | ---: | --- |
| About/About.xml | 1307 | `bbcaf5db4cdf5633d7e200e1b8319a5ede483ac7ca708f8bd5b556b57bb140e4` |
| 1.7/Assemblies/QualityColors.dll | 54784 | `3d3003b6fa4d6e31b4dac1f728f530c6cd67a23159378e22dc28139aecedd1de` |
| Languages/English/Keyed/Settings.xml | 1636 | `c83154306ce822d0165245a1a93b1a2dda15339bb43df25a3cf1169927df120e` |
| Native Assembly-CSharp.dll,1.7.5000 rev1275 | 15636480 | `d55aa8e47fa3c5463ca85b0eccd51f195b4bbcd04f56da3af21c2f6688a81254` |
| Native UnityStubs.dll, Color constants | 1024512 | `ea1dfbaea8d21cf838efe30b1417949843372908d14ba070aa4c192db8da2714` |

## Complete selected behavior inventory

| Inspected files/types | Actual behavior | Selected handling |
| --- | --- | --- |
| QualityColorsMod, PatchManager | Static settings/Harmony dawnsglow.QualityColors; constructor patches, applies settings; WriteSettings reapplies | Common owned settings and admitted idempotent startup; no PatchAll |
| ColorSettings | Six static mutable dictionaries; Colors initially aliases Default; colors/fullLabel serialized globally, FullLabel false | Exact palette values in observable spec, immutable copies, collection inheritance, no foreign key import |
| SettingsUI | FullLabel checkbox, preset menu, seven edit buttons; selecting preset copies dictionary | Ordered Interface settings, explicit global/save scope and coherent editor |
| LabelPatch | Full/short quality text wrapped; neutral white transferable label changes only for Thing with quality; CompQuality inspect result wrapped | Preserve surfaces and neutral/status distinction, add neutral hover/readability fallback |
| StringPatch | Truncate strip/re-add state; full-title GUI tint; expand quality color into condition parentheses; gear and corpse-worn apparel repair | Structured localized spans, lexical draw-state restoration, no substring replacement or repeated base inspect call |
| Patch_ITabPawnGear_DrawThingRow | Injects color helper before every matching Widgets.Label(Rect,string); explicitly installed and also attributed for PatchAll | Exact semantic label site/cardinality; no broad label match/double install |
| HelperPatches | Reverse-patch ThingWithComps.GetInspectString via ApparelBaseInspectString; unpatched stub throws | Do not copy reverse-call workaround; preserve returned components/taint at own display boundary |
| QColor | Rich-text regex, substring maps and static formatting caches; RGB-only hex; English enum-name matching | Occurrence-aware spans, all languages, consistent RGBA and safe truncation |
| Window_ColorPicker |720x650 forced-pause modal; wheel, slider, palette, previews and inputs; Save callback then close, Cancel close; dispose textures | Shared scoped transaction, cancel/stale/failure behavior; incidental dimensions not a mandated layout |
| ColorPickerColorState, ColorPickerInputState, InputFieldState, ParserUtils, ColorUtils | RGB/HSL/alpha/hex conversion, focused buffers; hex6/8 with optional#; display/current culture versus invariant parser mismatch | One model, finite/range and locale validation; no hidden sign stripping or repeated conversion drift |
| ColorPickerUserInteractionHandler, ColorPickerDrawer, ColorPickerLayoutHelper, ColorPickerConstants | Wheel and slider interaction, ColorDef swatches, neutral reset after colored Current/New labels, numeric fields; slider/triangle limited to lightness50 | Coherent full0–100 lightness, neutral accessible labels, seven languages |
| ColorPickerTextureManager | HSV wheel at value1, HSL gradient to50 except saturation0 to100; regenerated on hue/saturation changes, textures destroyed on Dispose | One consistent model for wheel/gradient/selector; future visual acceptance, no source texture reuse |
| Cache, QualityCache | Five large HashSets; Thing→quality and whole-Def negative caching; multiple color/text maps | Live native quality, no duplicate item index or persistent Thing references |
| ManagedCache, IManagedCache, CacheManager, QualityColorsGameComponent | FIFO stores; default max20000,hard50000,buffer2000,evict100,initial capacity51000; work every100 ticks; hashsets clear near8000 | No tick-dependent correctness/bounds; only measured small render caches |
| CacheDumpHelper, CacheSizeEstimator | CSV helpers and approximate size formatting; no selected-assembly caller/UI for dump helpers found | No new user/debug menu or compatibility facade for unused helpers |
| Properties/AssemblyInfo | Version1.0.0.0,copyright2021 | Provenance only, no runtime compatibility predicate |

Exact six palettes are in the spec. WoW Good is cyan-like (.12,1,1), not an assumed canonical WoW
green. Source Color.yellow resolves to (1,.922,.016,1) in this exact beta's UnityEngine.Color getter,
not pure (1,1,0); Default/Legacy Legendary and RPG Masterwork preserve that value. Other named
source colors were also cross-checked. Colorblind repeats Awful/Legendary; readable names remain essential. Source custom alpha is
used by GUI tints but dropped by QColor.ColorToHex, so markup differs from full-row color. Picker
numeric HSL permits lightness100 but slider selects at most50; HSV wheel versus HSL selection can
disagree, especially at low saturation. These are code-derived issues, not observed live failures.

QColor's ReAddColor cache key lacks color-map/palette identity and removes entries from the supplied
dictionary. StripColor stores/returns that mutable dictionary. Repeated identical substrings lose
distinct colors and re-add globally replaces their occurrences. ColorQuality cache lacks palette
identity and searches lowercase English enum words. Applying settings clears native label and
inspect caches, but not all owned formatting caches/sets. Cache.ClearAll does not clear the five
sets. Static Thing quality can stale; tick-only eviction stops while paused and does not establish
cross-game cleanup. These mechanisms are not copied or advertised as optimizations.

## Native 1.7 cross-checks

Nine game types plus the native color value type extracted: RimWorld.QualityUtility, GenLabel, CompQuality, TransferableUIUtility,
ITab_Pawn_Gear, Apparel, MainTabWindow_Inspect, Selector, plus Verse.GenText.
UnityEngine.Color from UnityStubs supplies the named preset constants.

- TryGetQuality uses CompQuality, including MinifiedThing.InnerThing; false returns Normal as an
  out-value but does not imply actual Normal quality. Full/short labels are translated.
- GenLabel.LabelExtras composes quality, damaged-single-stack HP percentage and corpse-worn marker.
  Preserve include flags and source text; color does not add a missing condition percentage.
- Native string Truncate cuts raw text characters; TaggedString's stripping is not a complete
  rich-text-safe solution. No generic helper is assumed safe without causal UI evidence.
- TransferableUIUtility.DrawTransferableInfo receives labelColor, applies it to the name draw,
  restores white and adds native tooltip; empty/non-Thing paths exist. A prefix altering color
  must prove that intended argument consumption remains, not merely signature existence.
- Gear DrawThingRow composes Thing.LabelCap with forced/locked indicators, then truncates/draws;
  info/drop/ingest controls and permission checks are separate and must remain unchanged.
- Apparel.GetInspectString calls base and appends corpse-worn text. No need to execute base twice
  merely to recover colors in an independently composed presentation.
- MainTabWindow_Inspect.GetLabel delegates AdjustedLabelFor. Selector.SingleSelectedThing is null
  unless exactly one Thing is selected, so source's same-quality multi-selection alternative cannot
  produce whole-title coloring here. Proposed scope is explicitly single item.

## Proven external consumers

Bounded installed DLL text search found these two consumers. This does not prove no others exist.
Complete consumer projects were extracted, but only QC discovery/call/setting/render paths were
analyzed; neither whole external mod is claimed specified by this entry.

| Consumer | Selected DLL bytes / SHA256 | Contract and consequence |
| --- | --- | --- |
| Dynamic Trade Interface / Zeracronius, zeracronius.dynamictradeinterface,3020706506 |154624 / `f4a2aafcdd29e35f273c40b60518bc9c2678e8005d88da0952a7d3a368847ebd` | Static source-package gate then reverse-patch QualityColors.QualityColorsMod:AddColors; GetColor(Transferable,Rect,ref Color) feeds ColumnCaption/ColumnQuality PostOpen caches. Selected source has global QualityColorsMod and no AddColors; this specific binding is stale by inspection |
| Nice Inventory Tab / Andromeda, Andromeda.NiceInventoryTab,3609897594 |195584 / `06de113199d6bee864d77205c5d4921beaee4bfeb0af1093752d7a5a618b6355` | Exact package+assembly lookup, Match button reads global QualityColorsMod.Settings and QualityColors.ColorSettings.Colors dictionary then SetQualityColor per category. Manual settings copy, not live synchronization |

No assembly/package masquerade or broad mutable facade is selected. These bindings cannot discover
the megamod as-is; document omitted bridges instead of inferring drop-in parity from native coloring.
A future DTI adapter needs its own custom-column/refresh/status contract; a NIT export needs explicit
target/scope, complete preview, atomic backup/rollback and foreign persistence rules. Source provider
active leaves it authoritative and suppresses owned overlap with warning. Absent consumers are normal.

## UI audit, direct composition and decisions

Source configuration is already a mod-settings page, not a bottom tab. Move it into common ordered
Interface / Quality colors with collection-level global/save inheritance and a separate full-title
row. Per-color edits are inside that palette editor; no entity policy is hidden in settings. Source
picker/debug cache helpers do not justify a gameplay tab. No bottom-right toggle is needed.

S132 consumes this same quality palette directly; its separate condition/background/selection
colors need their own analysis. Source RPG preset is just a preset name. S127 HUD and other owned
item displays can consume the resolver without another cache or internal patches. No XML Def
changes selected, no new XML Extensions dependency. Harmony required when actual hooks are added.

Proposed corrections are explicit in the observable contract: current values while paused, finite
validation, immutable presets, per-save inheritance, safe transaction/cancel, coherent full-range
picker, consistent alpha, neutral readability, status precedence, proper localization/truncation
and fail-closed semantic admission. No claim that these fixes have run in game.

## Remaining gates and evidence

QC-Q1 resolved for inventory/provenance. QC-Q2 exact admitted call paths and live beta coverage,
QC-Q3 external adapters/source/license retrieval, QC-Q4 S132 composition and QC-Q5 native
language/readability/performance evidence remain tracked. Future implementation follows focused
TDD, independent review and personally observed native actions in fresh minimized Gateway runs.
Semantic review: cosmetic only, no costs/research/items, no quality reroll, capacity change, market
value change or permission bypass. Availability of native actions must be observed, not inferred
from screenshots of colored labels. No runtime/game/Workshop modification during this work.

Local evidence: `artifacts/Refinement/megamod-spec-20261106/quality-colors/` with exact input hashes,
selected DLL/consumer/native extracts, checks, review and validation records. Hashes identify these
inspection inputs, not a runtime patch admission rule.
