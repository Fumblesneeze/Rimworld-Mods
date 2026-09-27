using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using NUnit.Framework;
using RimWorld;
using ThinWalls.Rendering;
using UnityEngine;
using Verse;

namespace ThinWalls.Harmony.Tests;

[TestFixture]
public sealed class ThinWallPointerSelectionTests
{
    [Test]
    public void NonSelectionQueriesAreUntouchedWithoutLookingUpAGameMap()
    {
        var original = new List<Thing> { null! };
        ThinWallPointerSelectionPatch.Postfix(Vector3.zero, new TargetingParameters(), null!, original);
        Assert.That(original.Count, Is.EqualTo(1));
        Assert.That(original[0], Is.Null);
    }

    [Test]
    public void PointerPatchTargetsTheNativeFractionalPositionQueryAsAPostfix()
    {
        MethodInfo target = AccessTools.Method(typeof(GenUI), nameof(GenUI.ThingsUnderMouse),
            new[] { typeof(Vector3), typeof(float), typeof(TargetingParameters), typeof(ITargetingSource) });
        Assert.That(target, Is.Not.Null);
        Assert.That(target.ReturnType, Is.EqualTo(typeof(List<Thing>)));
        Assert.That(typeof(ThinWallPointerSelectionPatch).GetMethod("Postfix")!
            .GetCustomAttribute<HarmonyPostfix>(), Is.Not.Null);
    }
}
