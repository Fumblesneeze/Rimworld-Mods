using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.SettingsSearch.Harmony;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.SettingsSearch;

[TestFixture, NonParallelizable]
public sealed class SettingsChooserSpliceTests
{
    public sealed record Page(string Name);
    public class Window(Page page) { public Page Page = page; }
    public sealed class ProviderWindow(Page page) : Window(page);
    public sealed class Stack
    {
        public readonly List<Window> Opened = new();
        [MethodImpl(MethodImplOptions.NoInlining)] public void Add(Window window) => Opened.Add(window);
    }
    public static class Native
    {
        public static readonly Stack Windows = new();
        public static bool Click;
        public static bool Throw;
        [MethodImpl(MethodImplOptions.NoInlining)] public static Stack GetStack() => Windows;
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool Button(int rect) => Click;
        [MethodImpl(MethodImplOptions.NoInlining)] public static Window Factory(Page page) =>
            Throw ? throw new InvalidOperationException("provider-open-failed") : new ProviderWindow(page);
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool NoButton(int rect) => false;
    }
    public sealed class Chooser
    {
        public IEnumerable<Page> Pages = Array.Empty<Page>();
        public int Drawn;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Draw(object listing)
        {
            foreach (var page in Pages)
            {
                if (page.Name.Contains("hidden")) continue;
                if (Native.Button(40)) Native.GetStack().Add(new Window(page));
                Drawn++;
            }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void OwnedSelectionUsesTheExistingOpeningBranchAndNativeFallback(bool provider)
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.chooser.provider");
        var target = typeof(Chooser).GetMethod(nameof(Chooser.Draw))!;
        var chosen = new Page("hidden option owner");
        var native = new Page("ordinary");
        bool enabled = true;
        int draws = 0;
        using var patch = Create((object source, object dialog, object listing, out object? context) =>
        {
            context = enabled ? new object() : null;
            return enabled ? new[] { chosen, chosen } : source;
        }, (_, page) => ++draws == 2);
        try
        {
            Native.Windows.Opened.Clear(); Native.Click = false;
            if (provider) foreign.Patch(target, transpiler: new HarmonyMethod(typeof(SettingsChooserSpliceTests), nameof(Factory)));
            Assert.That(patch.Install(), Is.True, patch.Failure);
            var chooser = new Chooser { Pages = new[] { native } };
            chooser.Draw(new object());
            Assert.That(draws, Is.EqualTo(2));
            Assert.That(chooser.Drawn, Is.Zero);
            Assert.That(Native.Windows.Opened, Has.Count.EqualTo(1));
            Assert.That(Native.Windows.Opened[0].Page, Is.SameAs(chosen));
            Assert.That(Native.Windows.Opened[0], provider ? Is.TypeOf<ProviderWindow>() : Is.TypeOf<Window>());
            enabled = false; Native.Click = true;
            chooser.Draw(new object());
            Assert.That(chooser.Drawn, Is.EqualTo(1));
            Assert.That(Native.Windows.Opened[1].Page, Is.SameAs(native));
        }
        finally
        {
            patch.Dispose();
            foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id);
            Native.Windows.Opened.Clear(); Native.Click = false;
        }
    }

    [Test]
    public void SkipOriginalNeverDrawsOrOpensOwnedRows()
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.chooser.skip");
        var target = typeof(Chooser).GetMethod(nameof(Chooser.Draw))!;
        int selections = 0;
        using var patch = Create((object source, object dialog, object listing, out object? context) =>
        { selections++; context = null; return source; }, (_, _) => true);
        try
        {
            foreign.Patch(target, prefix: new HarmonyMethod(typeof(SettingsChooserSpliceTests), nameof(Skip)));
            Assert.That(patch.Install(), Is.True, patch.Failure);
            new Chooser().Draw(new object());
            Assert.That(selections, Is.Zero);
        }
        finally { patch.Dispose(); foreign.Unpatch(target, HarmonyPatchType.Prefix, foreign.Id); }
    }

    public static bool Skip() => false;
    [Test]
    public void ProviderOpeningExceptionStillDisposesTheOriginalForeach()
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.chooser.throw");
        var target = typeof(Chooser).GetMethod(nameof(Chooser.Draw))!;
        int disposed = 0;
        IEnumerable<Page> Pages()
        { try { yield return new Page("provider"); } finally { disposed++; } }
        using var patch = Create((object source, object dialog, object listing, out object? context) =>
        { context = new object(); return source; }, (_, _) => true);
        try
        {
            foreign.Patch(target, transpiler: new HarmonyMethod(typeof(SettingsChooserSpliceTests), nameof(Factory)));
            Assert.That(patch.Install(), Is.True, patch.Failure);
            Native.Throw = true;
            Assert.That(() => new Chooser { Pages = Pages() }.Draw(new object()), Throws.InvalidOperationException.With.Message.EqualTo("provider-open-failed"));
            Assert.That(disposed, Is.EqualTo(1));
        }
        finally { Native.Throw = false; patch.Dispose(); foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id); }
    }

    [Test]
    public void AnUnrecognizedClickEdgeLeavesTheIncomingNativeBehavior()
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.chooser.changed");
        var target = typeof(Chooser).GetMethod(nameof(Chooser.Draw))!;
        int selected = 0;
        using var patch = Create((object source, object dialog, object listing, out object? context) =>
        { selected++; context = new object(); return source; }, (_, _) => true);
        try
        {
            foreign.Patch(target, transpiler: new HarmonyMethod(typeof(SettingsChooserSpliceTests), nameof(ChangeButton)));
            Assert.That(patch.Install(), Is.False);
            var chooser = new Chooser { Pages = new[] { new Page("native") } };
            chooser.Draw(new object());
            Assert.That(chooser.Drawn, Is.EqualTo(1));
            Assert.That(selected, Is.Zero);
            Assert.That(Native.Windows.Opened, Is.Empty);
        }
        finally { patch.Dispose(); foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id); }
    }

    public static IEnumerable<CodeInstruction> ChangeButton(IEnumerable<CodeInstruction> incoming)
    {
        foreach (var instruction in incoming)
        {
            if (instruction.Calls(typeof(Native).GetMethod(nameof(Native.Button)))) instruction.operand = typeof(Native).GetMethod(nameof(Native.NoButton));
            yield return instruction;
        }
    }
    public static IEnumerable<CodeInstruction> Factory(IEnumerable<CodeInstruction> incoming)
    {
        var constructor = typeof(Window).GetConstructor(new[] { typeof(Page) });
        foreach (var instruction in incoming)
            yield return instruction.opcode == OpCodes.Newobj && Equals(instruction.operand, constructor)
                ? new CodeInstruction(OpCodes.Call, typeof(Native).GetMethod(nameof(Native.Factory)))
                    { labels = instruction.labels, blocks = instruction.blocks }
                : instruction;
    }
    private static SettingsChooserSplice Create(SettingsChooserSplice.SelectSource select, Func<object, object, bool> draw) =>
        new(typeof(Chooser).GetMethod(nameof(Chooser.Draw))!, typeof(Chooser).GetField(nameof(Chooser.Pages))!,
            typeof(Native).GetMethod(nameof(Native.Button))!, typeof(Stack).GetMethod(nameof(Stack.Add))!, select, draw, _ => { });
}
