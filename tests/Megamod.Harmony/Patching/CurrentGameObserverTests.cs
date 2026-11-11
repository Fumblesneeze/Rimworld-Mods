using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.HarmonyTests;

[TestFixture]
public sealed class CurrentGameObserverTests
{
    [Test]
    public void AnAdmittedObserverReceivesTheActualNewGame()
    {
        var observed = new List<object?>();
        var patch = new CurrentGameObserverPatch(typeof(FixtureCurrent), typeof(FixtureGame), observed.Add);
        var coordinator = new AdmissionCoordinator(_ => { });
        try
        {
            Assert.That(coordinator.Apply(new PatchGroup("settings", AdmissionPhase.Early,
                new[] { patch.Candidate }), AdmissionPhase.Early), Is.True);
            FixtureCurrent.Game = new FixtureGame();
            Assert.That(observed, Is.Empty, "Installation must not enable callbacks before group admission.");
            patch.Enable();
            var game = new FixtureGame();

            FixtureCurrent.Game = game;

            Assert.That(observed, Is.EqualTo(new object?[] { game }));
        }
        finally { patch.Candidate.Rollback(); FixtureCurrent.Game = null; }
    }

    public sealed class FixtureGame { }

    [Test]
    public void AForeignSkipOriginalPrefixStillObservesTheUnchangedActualGame()
    {
        const string foreignOwner = "test.settings.foreign-observer";
        var foreign = new HarmonyLib.Harmony(foreignOwner);
        var setter = typeof(FixtureCurrent).GetProperty(nameof(FixtureCurrent.Game))!.SetMethod!;
        var actual = new FixtureGame();
        FixtureCurrent.Game = actual;
        var observed = new List<object?>();
        var patch = new CurrentGameObserverPatch(typeof(FixtureCurrent), typeof(FixtureGame), observed.Add);
        try
        {
            foreign.Patch(setter, prefix: new HarmonyMethod(typeof(CurrentGameObserverTests), nameof(SkipOriginal)));
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(new PatchGroup("settings",
                AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.True);
            patch.Enable();

            FixtureCurrent.Game = new FixtureGame();

            Assert.That(observed, Is.EqualTo(new object?[] { actual }));
            patch.Candidate.Rollback();
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(setter)!.Owners, Does.Contain(foreignOwner));
        }
        finally
        {
            patch.Candidate.Rollback();
            foreign.Unpatch(setter, HarmonyPatchType.Prefix, foreignOwner);
            FixtureCurrent.Game = null;
        }
    }

    private static bool SkipOriginal() => false;

    [Test]
    public void RecheckingAdmissionCannotDiscardTheAcquiredRollbackReceipt()
    {
        var setter = typeof(FixtureCurrent).GetProperty(nameof(FixtureCurrent.Game))!.SetMethod!;
        var patch = new CurrentGameObserverPatch(typeof(FixtureCurrent), typeof(FixtureGame), _ => { });
        try
        {
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(new PatchGroup("settings",
                AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.True);

            Assert.That(patch.Candidate.Validate(), Is.Not.Null);
            patch.Candidate.Rollback();

            Assert.That(HarmonyLib.Harmony.GetPatchInfo(setter)?.Owners.Contains(OwnedHarmonyPatch.OwnerId)
                ?? false, Is.False);
        }
        finally
        {
            patch.Candidate.Rollback();
            new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId).Unpatch(setter,
                HarmonyPatchType.Postfix, OwnedHarmonyPatch.OwnerId);
        }
    }

    [Test]
    public void AFailedObservationDisablesFurtherCallbacksWithoutBreakingTheNativeSetter()
    {
        var failures = new List<Exception>();
        int observations = 0;
        var patch = new CurrentGameObserverPatch(typeof(FixtureCurrent), typeof(FixtureGame), _ =>
        {
            observations++;
            throw new InvalidOperationException("save context unavailable");
        }, failures.Add);
        try
        {
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(new PatchGroup("settings",
                AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.True);
            patch.Enable();
            var next = new FixtureGame();

            Assert.DoesNotThrow(() => FixtureCurrent.Game = new FixtureGame());
            Assert.DoesNotThrow(() => FixtureCurrent.Game = next);

            Assert.That(FixtureCurrent.Game, Is.SameAs(next));
            Assert.That(observations, Is.EqualTo(1));
            Assert.That(failures.Single().Message, Is.EqualTo("save context unavailable"));
        }
        finally { patch.Candidate.Rollback(); FixtureCurrent.Game = null; }
    }

    [Test]
    public void InitialGetterFailureUsesTheSameBoundedUnavailableFallback()
    {
        var failures = new List<Exception>();
        int observations = 0;
        var patch = new CurrentGameObserverPatch(typeof(ThrowingCurrent), typeof(FixtureGame),
            _ => observations++, failures.Add);
        try
        {
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(new PatchGroup("settings",
                AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.True);
            patch.Enable();

            Assert.DoesNotThrow(patch.ObserveCurrent);
            Assert.DoesNotThrow(patch.ObserveCurrent);
            Assert.DoesNotThrow(() => ThrowingCurrent.Game = new FixtureGame());

            Assert.That(observations, Is.Zero);
            Assert.That(failures.Single().Message, Is.EqualTo("current game unavailable"));
            Assert.That(patch.Failure, Is.SameAs(failures.Single()));
            Assert.Throws<InvalidOperationException>(patch.Enable);
        }
        finally { patch.Candidate.Rollback(); }
    }

    public static class ThrowingCurrent
    {
        public static FixtureGame? Game
        {
            [MethodImpl(MethodImplOptions.NoInlining)] get => throw new InvalidOperationException("current game unavailable");
            [MethodImpl(MethodImplOptions.NoInlining)] set { }
        }
    }

    [TestCase(typeof(WrongGetter))]
    [TestCase(typeof(WrongSetter))]
    [TestCase(typeof(GenericGetter))]
    [TestCase(typeof(BroadSetter))]
    public void ChangedAccessorShapeLeavesTheObserverInactive(Type currentType)
    {
        var warnings = new List<string>();
        var patch = new CurrentGameObserverPatch(currentType, typeof(FixtureGame), _ => { });
        try
        {
            var coordinator = new AdmissionCoordinator(warnings.Add);

            bool admitted = coordinator.Apply(new PatchGroup("settings", AdmissionPhase.Early,
                new[] { patch.Candidate }), AdmissionPhase.Early);

            Assert.That(admitted, Is.False);
            Assert.That(warnings.Single(), Does.Contain("SETTINGS-GAME-CONTEXT"));
            Assert.Throws<InvalidOperationException>(() => patch.Enable());
        }
        finally { patch.Candidate.Rollback(); }
    }

    public static class WrongGetter
    {
        public static object? get_Game() => null;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void set_Game(FixtureGame? value) { }
    }

    public static class WrongSetter
    {
        public static FixtureGame? get_Game() => null;
        [MethodImpl(MethodImplOptions.NoInlining)] public static int set_Game(FixtureGame? value) => 0;
    }

    public static class GenericGetter
    {
        public static FixtureGame? get_Game<T>() => null;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void set_Game(FixtureGame? value) { }
    }

    public static class BroadSetter
    {
        public static FixtureGame? get_Game() => null;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void set_Game(object? value) { }
    }

    public static class FixtureCurrent
    {
        private static FixtureGame? game;
        public static FixtureGame? Game
        {
            [MethodImpl(MethodImplOptions.NoInlining)] get => game;
            [MethodImpl(MethodImplOptions.NoInlining)] set => game = value;
        }
    }
}
