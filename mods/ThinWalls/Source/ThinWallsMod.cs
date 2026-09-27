using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ThinWalls;

public sealed class ThinWallsMod : Mod
{
    public const string PackageId = "fumblesneeze.thinwalls";
    public static ThinWallsSettings Settings { get; private set; } = new();

    public ThinWallsMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<ThinWallsSettings>();
        new Harmony(PackageId).PatchAll(Assembly.GetExecutingAssembly());
    }

    public override string SettingsCategory() => "TW_ModName".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.CheckboxLabeled("TW_ShowShrinkGizmo".Translate(), ref Settings.ShowShrinkGizmo);
        listing.CheckboxLabeled("TW_ShowOffsetGizmo".Translate(), ref Settings.ShowOffsetGizmo);
        listing.Gap();
        listing.Label("TW_AppearanceSettingsHint".Translate());
        listing.End();
    }
}
