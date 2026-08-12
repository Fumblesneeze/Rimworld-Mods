# XML, Defs, stuff, and patching

## Def design

Use a mod-specific prefix for every `defName`. Prefer abstract base Defs for repeated behavior and shallow concrete variants for gameplay items. Put calculations and stateful rules in comps/domain code instead of duplicating XML values.

- Declare stuff categories deliberately; `MadeFromStuff` alone does not establish sensible allowed materials.
- Verify that stuff color actually reaches the chosen graphic/shader. Test representative wood, metal, and precious-metal instances in game.
- Keep recipes explicit about work table, skill, work amount, products, ingredient filters, research, and stuff consumption.
- A stack cannot safely represent per-item quality, dirt, ingredients, plate ownership, or temperature unless stack splitting/merging preserves identical metadata. Specify and test that invariant before enabling stacking.
- Use comps with complete save/load (`ExposeData`) behavior for dirty flags, meal temperature/quality, prepared-food provenance, or other instance state.

## Optional Def patches

Prefer `PatchOperationFindMod` around XML that names external Defs. In RimWorld 1.6 this operation's `<mods>` values match the external mod's exact display name from About metadata, not its canonical package ID. Use canonical package IDs for C# detection and OpenSpec identity; inspect the installed About metadata before writing XML.

```xml
<Operation Class="PatchOperationFindMod">
  <mods>
    <li>Exact External Mod Display Name</li>
  </mods>
  <match Class="PatchOperationSequence">
    <operations>
      <li Class="PatchOperationAdd">
        <xpath>/Defs/ThingDef[defName="External_Def"]/comps</xpath>
        <value><!-- owning-mod comp --></value>
      </li>
    </operations>
  </match>
</Operation>
```

- Do not overwrite another mod's entire list when an append or targeted replacement suffices.
- Make patches safe when the target node is absent or changed; log compatibility status once where diagnosis matters.
- Put each external XML integration in a clearly named compatibility patch file. If the owning mod already exposes an integration-status catalog, register only package ID, selected strategy, and status there; do not introduce C# solely to mirror an XML-only patch.
- Use load-order hints only for mods whose Defs are actually patched; hints are not hard dependencies.
- Avoid broad XPath matches that can silently patch unrelated Defs from other mods.

After build, inspect the generated package—not source alone—for `About.xml`, version folders, assemblies, Defs, patches, textures, and accidental dependencies. Then launch with the optional mod both absent and present.
