using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ThinWalls;

public sealed class ThinWallsMod : Mod
{
    public const string PackageId = "fumblesneeze.thinwalls";

    public ThinWallsMod(ModContentPack content) : base(content)
    {
        new Harmony(PackageId).PatchAll(Assembly.GetExecutingAssembly());
    }

    public override string SettingsCategory() => "TW_ModName".Translate();
}
