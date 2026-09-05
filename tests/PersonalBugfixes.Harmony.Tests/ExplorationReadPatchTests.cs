using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using NUnit.Framework;
using PersonalBugfixes.Exploration;

namespace PersonalBugfixes.Harmony.Tests;

[TestFixture]
public sealed class ExplorationReadPatchTests
{
    public sealed class Component { public List<bool> learnedFeatures = new(); public List<bool> unrelated = new(); }
    private static FieldInfo Field(string name) => typeof(Component).GetField(name)!;
    private static List<CodeInstruction> Read(FieldInfo field) => new()
    {
        new(OpCodes.Ldloc_0), new(OpCodes.Ldfld, field), new(OpCodes.Ldloc_1),
        new(OpCodes.Callvirt, ExplorationReadPatch.ListRead), new(OpCodes.Pop)
    };

    [Test]
    public void Matches_relevant_read_even_when_unrelated_instructions_change()
    {
        var field = Field("learnedFeatures");
        var code = Read(field);
        code.Insert(0, new CodeInstruction(OpCodes.Nop));
        code.Add(new CodeInstruction(OpCodes.Ret));
        Assert.That(ExplorationReadPatch.FindRead(code, field), Is.EqualTo(4));
    }

    [Test]
    public void Ambiguous_or_other_field_access_is_rejected()
    {
        var field = Field("learnedFeatures");
        Assert.That(ExplorationReadPatch.FindRead(Read(Field("unrelated")), field), Is.EqualTo(-1));
        Assert.That(ExplorationReadPatch.FindRead(Read(field).Concat(Read(field)).ToList(), field), Is.EqualTo(-1));
    }
}
