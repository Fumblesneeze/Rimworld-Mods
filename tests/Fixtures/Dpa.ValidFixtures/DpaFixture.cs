using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;

namespace Analyzer
{
    public sealed class Settings
    {
        public static bool disableThreadedPatching;
    }

    public static class Modbase
    {
        public static Settings Settings = new Settings();
        public static Harmony Harmony { get; } = new Harmony("Dubwise.DubsProfiler");
        public static bool isPatched;
    }
}

namespace Analyzer.Profiling
{
    public enum Category
    {
        Settings,
        Tick,
        Update,
        GUI,
        Modder
    }

    public sealed class ProfileLog { }

    public sealed class Profiler
    {
        public const int RECORDS_HELD = 2000;
        public Type type;
        public MethodBase meth;
        public string label;
        public string key;
        public int hitCounter;
        public readonly double[] times = new double[RECORDS_HELD];
        public readonly int[] hits = new int[RECORDS_HELD];
        public uint currentIndex;
        public bool Empty = true;

        public Profiler(string key, MethodBase method)
        {
            this.key = key;
            label = key;
            meth = method;
            type = method.DeclaringType;
        }
    }

    public static class ProfileController
    {
        public static ConcurrentDictionary<string, Profiler> profiles =
            new ConcurrentDictionary<string, Profiler>(StringComparer.Ordinal);

        public static ConcurrentDictionary<string, Profiler> Profiles => profiles;
        public static ConcurrentBag<GCHandle> Handles { get; } = new ConcurrentBag<GCHandle>();

        public static void RecordCycleForTests()
        {
            if (!Analyzer.CurrentlyProfiling) return;
            foreach (var profiler in Profiles.Values)
            {
                profiler.times[0] = 1.25;
                profiler.hits[0] = 4;
                profiler.currentIndex = 1;
                profiler.Empty = false;
            }
        }
    }

    public static class InternalMethodUtility
    {
        public static HarmonyMethod InternalProfiler =
            new HarmonyMethod(typeof(InternalMethodUtility), nameof(Transpiler));
        public static HashSet<MethodInfo> PatchedInternals = new HashSet<MethodInfo>();

        private static IEnumerable<CodeInstruction> Transpiler(
            MethodBase __originalMethod, IEnumerable<CodeInstruction> instructions) => instructions;
    }

    public static class TranspilerMethodUtility
    {
        public static List<MethodBase> PatchedMeths = new List<MethodBase>();
    }

    public static class GUIController
    {
        public static Dictionary<string, Type> types = new Dictionary<string, Type>(StringComparer.Ordinal);
    }

    public static class Utility
    {
        public static List<string> patchedAssemblies = new List<string>();
        public static List<string> patchedTypes = new List<string>();
        public static List<string> patchedMethods = new List<string>();
        public static MethodInfo FailPatchForTests;

        public static void PatchInternalMethod(MethodInfo method, Category category)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            InternalMethodUtility.PatchedInternals.Add(method);
            var ownerKey = method.DeclaringType + ":" + method.Name + "-int";
            if (!GUIController.types.TryGetValue(ownerKey, out var ownerType))
            {
                ownerType = DynamicType(ownerKey);
                GUIController.types.Add(ownerKey, ownerType);
            }
            if (method == FailPatchForTests) return;
            global::Analyzer.Modbase.Harmony.Patch(method, transpiler: InternalMethodUtility.InternalProfiler);
            var innerCallee = typeof(Math).GetMethod(nameof(Math.Abs), new[] { typeof(int) });
            var profiler = new Profiler(method.DeclaringType.FullName + ":" + method.Name + "::fixture-call", innerCallee)
            {
                type = null
            };
            ProfileController.Profiles.TryAdd(profiler.key, profiler);
        }

        private static Type DynamicType(string name)
        {
            var assemblyName = new AssemblyName("DpaFixtureDynamic" + Guid.NewGuid().ToString("N"));
            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var module = assembly.DefineDynamicModule(assemblyName.Name);
            return module.DefineType("DpaFixture_" + Math.Abs(name.GetHashCode()), TypeAttributes.Public).CreateType();
        }
    }

    public static class Analyzer
    {
        private static bool currentlyProfiling;
        public static bool ThrowOnCleanupForTests;
        private static bool holdCleanupForTests;
        private static readonly ManualResetEventSlim CleanupStarted = new ManualResetEventSlim(false);
        private static readonly ManualResetEventSlim CleanupRelease = new ManualResetEventSlim(true);
        public static bool CurrentlyCleaningUp { get; set; }
        public static bool CurrentlyProfiling => currentlyProfiling;
        public static List<ProfileLog> Logs { get; } = new List<ProfileLog>();

        public static void BeginProfiling() => currentlyProfiling = true;
        public static void EndProfiling() => currentlyProfiling = false;

        private static void CleanupBackground()
        {
            CleanupStarted.Set();
            if (holdCleanupForTests) CleanupRelease.Wait(TimeSpan.FromSeconds(10));
            CurrentlyCleaningUp = true;
            try
            {
                if (ThrowOnCleanupForTests) return;
                currentlyProfiling = false;
                global::Analyzer.Modbase.Harmony.UnpatchAll(global::Analyzer.Modbase.Harmony.Id);
                ProfileController.Profiles.Clear();
                InternalMethodUtility.PatchedInternals.Clear();
                TranspilerMethodUtility.PatchedMeths.Clear();
                Utility.patchedAssemblies.Clear();
                Utility.patchedTypes.Clear();
                Utility.patchedMethods.Clear();
                while (ProfileController.Handles.TryTake(out _)) { }
                Logs.Clear();
                global::Analyzer.Modbase.isPatched = false;
            }
            finally
            {
                CurrentlyCleaningUp = false;
            }
        }

        public static void Cleanup() => Task.Factory.StartNew(CleanupBackground);

        public static void HoldCleanupForTests()
        {
            holdCleanupForTests = true;
            CleanupStarted.Reset();
            CleanupRelease.Reset();
        }

        public static bool WaitForCleanupStartForTests() => CleanupStarted.Wait(TimeSpan.FromSeconds(5));

        public static void ReleaseCleanupForTests()
        {
            holdCleanupForTests = false;
            CleanupRelease.Set();
        }

        public static void ResetForTests()
        {
            ReleaseCleanupForTests();
            SpinWait.SpinUntil(() => !CurrentlyCleaningUp, TimeSpan.FromSeconds(5));
            ThrowOnCleanupForTests = false;
            CleanupBackground();
            CleanupStarted.Reset();
            global::Analyzer.Settings.disableThreadedPatching = false;
            Utility.FailPatchForTests = null;
        }
    }

    public static class H_RootUpdate
    {
        public static void Prefix() { }
        public static void Postfix() => ProfileController.RecordCycleForTests();
    }

    internal static class H_DoSingleTickUpdate
    {
        public static void Prefix() { }
        public static void Postfix() => ProfileController.RecordCycleForTests();
    }
}

namespace Verse
{
    public sealed class Root_Play
    {
        public void Update() { }
    }

    public sealed class TickManager
    {
        public void DoSingleTick() { }
    }
}
