# XML, Defs, stuff, and patching

## Def design

Use a mod-specific prefix for every `defName`. Prefer abstract base Defs for repeated behavior and shallow concrete variants for gameplay items. Put calculations and stateful rules in comps/domain code instead of duplicating XML values.

- Declare stuff categories deliberately; `MadeFromStuff` alone does not establish sensible allowed materials.
- Verify that stuff color actually reaches the chosen graphic/shader. Test representative wood, metal, and precious-metal instances in game.
- Keep recipes explicit about work table, skill, work amount, products, ingredient filters, research, and stuff consumption.
- A stack cannot safely represent per-item quality, dirt, ingredients, plate ownership, or temperature unless stack splitting/merging preserves identical metadata. Specify and test that invariant before enabling stacking.
- Use comps with complete save/load (`ExposeData`) behavior for dirty flags, meal temperature/quality, prepared-food provenance, or other instance state.

## XML-first Def changes

Prefer declarative XML for static Def creation and mutation:

1. Define an owning mod's new static Defs directly in `Defs/`.
2. Use the narrowest vanilla patch operation for existing Def XML when it can express the change safely.
3. Use the most specific XML Extensions operation when vanilla patching would require broad replacement, duplicated conditionals, display-name matching, or manual C# mutation.
4. Use C# or Harmony only for runtime state, lifecycle-sensitive or computed behavior, or a documented seam that XML cannot express safely.

Do not iterate `DefDatabase` from a mod constructor or static initializer merely to add, remove, or replace static fields that an XML patch can own. Do not introduce a C# integration-status layer solely to mirror an XML-only patch.

## Package-ID-gated optional patches

Vanilla `PatchOperationFindMod` matches mod display names, not package IDs. For an optional XML integration, use XML Extensions' [`XmlExtensions.FindMod`](https://github.com/15adhami/XmlExtensions/wiki/XmlExtensions.FindMod) with `<packageId>true</packageId>` so the condition uses the canonical package ID. The official operation also supports `or`/`and` matching and explicit true/false branches.

```xml
<Operation Class="XmlExtensions.FindMod">
  <mods>
    <li>external.mod.packageid</li>
  </mods>
  <packageId>true</packageId>
  <caseTrue>
    <Operation Class="PatchOperationAdd">
      <xpath>/Defs/ThingDef[defName="External_Def"]/comps</xpath>
      <value><!-- owning-mod comp --></value>
    </Operation>
  </caseTrue>
</Operation>
```

Using any `XmlExtensions.*` operation makes XML Extensions a hard runtime dependency of the owning mod. Declare package ID `imranfish.xmlextensions`, Workshop item `2574315206`, and `loadAfter` in the owning About/release metadata; include XML Extensions after Core and before the owning mod in exact test lists. Never copy or bundle `XmlExtensions.dll`. XML Extensions itself requires Harmony. Use the dependency block published in the [official XML Extensions wiki](https://github.com/15adhami/XmlExtensions/wiki#overview).

The [official patch-operation index](https://github.com/15adhami/XmlExtensions/wiki/Patch-Operations) includes safe add/remove/replace, attribute, copy, sorting, conditional, loop, and finalized-Def operations. Inspect the operation's current 1.6 documentation and installed read-only assembly before choosing it; use the narrowest operation that preserves unrelated nodes.

- Do not overwrite another mod's entire list when an append or targeted replacement suffices.
- Make patches safe when the target node is absent or changed; log compatibility status once where diagnosis matters.
- Put each external XML integration in a clearly named compatibility patch file. If the owning mod already exposes an integration-status catalog, register only package ID, selected strategy, and status there; do not introduce C# solely to mirror an XML-only patch.
- Use load-order hints only for mods whose Defs are actually patched; target-mod hints are not hard dependencies. XML Extensions is a hard dependency whenever its operations are used.
- Avoid broad XPath matches that can silently patch unrelated Defs from other mods.

After build, inspect the generated package—not source alone—for `About.xml`, version folders, assemblies, Defs, patches, textures, and accidental dependencies. Verify the XML Extensions dependency in both RimWorld metadata and the Workshop/release graph, then launch with XML Extensions active and the optional target mod both absent and present.
