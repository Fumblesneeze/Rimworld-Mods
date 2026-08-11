using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Circinus.Contract
{
    public sealed class RunDocument
    {
        public string Id = string.Empty;
        public int SchemaMajor = 1;
        public int SchemaMinor = 15;
        public string MethodsJson = string.Empty;
        public string PatchesJson = string.Empty;
        public int PatchesDropped;
        public string JsonIdOverride;

        public string ToJson() =>
            "{\"id\":\"" + (JsonIdOverride ?? Id) + "\",\"schemaMajor\":" + SchemaMajor +
            ",\"schemaMinor\":" + SchemaMinor + ",\"patchesDropped\":" + PatchesDropped +
            ",\"patches\":[" + PatchesJson + "],\"methods\":[" + MethodsJson + "]}";
    }

    public sealed class MethodRef
    {
        public string Key = string.Empty;
    }

    public sealed class PatchRef
    {
        public string Key = string.Empty;
    }
}

namespace Circinus.Session
{
    using Circinus.Contract;

    public sealed class RunRecorder
    {
        public static RunRecorder Current { get; } = new RunRecorder();

        public bool Recording { get; private set; }
        private string activeId = string.Empty;
        public string ActiveId
        {
            get
            {
                if (ThrowOnActiveId) throw new InvalidOperationException("active id failure");
                return activeId;
            }
        }
        public RunDocument Document { get; private set; } = new RunDocument();
        public int NextSchemaMajor = 1;
        public int NextSchemaMinor = 15;
        public int MarkerCount;
        public int StopCount;
        public bool ThrowOnActiveId;
        public bool ThrowOnStopForTests;
        private int runSequence;

        public bool Start(string label)
        {
            if (Recording || string.IsNullOrWhiteSpace(label)) return false;
            activeId = "fixture-run-" + (++runSequence);
            Document = new RunDocument
            {
                Id = activeId,
                SchemaMajor = NextSchemaMajor,
                SchemaMinor = NextSchemaMinor
            };
            Recording = true;
            return true;
        }

        public RunDocument Stop()
        {
            StopCount++;
            if (ThrowOnStopForTests) throw new InvalidOperationException("stop failure");
            Recording = false;
            return Document;
        }

        public void AddMarker(string name, string value)
        {
            if (!Recording) throw new InvalidOperationException("not recording");
            MarkerCount++;
        }

        public void ResetForTests()
        {
            Recording = false;
            activeId = string.Empty;
            Document = new RunDocument();
            NextSchemaMajor = 1;
            NextSchemaMinor = 15;
            MarkerCount = 0;
            StopCount = 0;
            ThrowOnActiveId = false;
            ThrowOnStopForTests = false;
            runSequence = 0;
        }
    }
}

namespace Circinus.Profiling
{
    public sealed class ProfileTarget
    {
        public ProfileTarget(string key, string category, int resolveCount)
        {
            Key = key;
            Category = category;
            ResolveCount = resolveCount;
        }

        public string Key;
        public string Category;
        public int ResolveCount;
        internal int ArmedMethods;
        internal readonly List<MethodBase> Methods = new List<MethodBase>();
        public int MethodCount => ArmedMethods;
    }

    public sealed class DevProfiler
    {
        public DevProfiler(MethodBase method)
        {
            Method = method;
        }

        public readonly MethodBase Method;
        public bool HandArmed;
        public int SampleShift { get; private set; }
        public long TotalCalls { get; private set; }
        public long TotalTimedCalls { get; private set; }
        public bool Empty { get; private set; } = true;
        public int CyclesSeen { get; private set; }

        public void SeedForTests(
            int sampleShift,
            long totalCalls,
            long totalTimedCalls,
            bool empty,
            int cyclesSeen)
        {
            SampleShift = sampleShift;
            TotalCalls = totalCalls;
            TotalTimedCalls = totalTimedCalls;
            Empty = empty;
            CyclesSeen = cyclesSeen;
        }
    }

    public static class Instrumenter
    {
        private static readonly HashSet<MethodBase> Methods = new HashSet<MethodBase>();
        private static readonly HashSet<ProfileTarget> Targets = new HashSet<ProfileTarget>();

        public static bool ThrowBeforeArmMethodForTests;
        public static bool ThrowAfterArmMethodForTests;
        public static bool ThrowAfterArmTargetForTests;
        public static bool ThrowOnDisarmMethodForTests;
        public static bool ThrowOnDisarmTargetForTests;
        public static bool IgnoreTargetHandArmedForTests;
        public static int ActiveTargetCountForTests => Targets.Count;

        public static bool ArmMethod(MethodBase method, string label)
        {
            if (ThrowBeforeArmMethodForTests) throw new InvalidOperationException("arm method before failure");
            var added = Methods.Add(method);
            if (ThrowAfterArmMethodForTests) throw new InvalidOperationException("arm method after failure");
            return added;
        }
        public static int Arm(ProfileTarget target, bool handArmed)
        {
            Targets.Add(target);
            target.ArmedMethods = target.ResolveCount;
            for (var index = 0; index < target.ResolveCount; index++)
            {
                var method = new DynamicMethod(
                    "TargetMethod" + index,
                    typeof(void),
                    Type.EmptyTypes,
                    typeof(Instrumenter).Module,
                    skipVisibility: true);
                target.Methods.Add(method);
                var profiler = ProfilerRegistry.Find(method);
                if (!IgnoreTargetHandArmedForTests) profiler.HandArmed = handArmed;
            }
            if (ThrowAfterArmTargetForTests) throw new InvalidOperationException("arm target after failure");
            return target.ArmedMethods;
        }
        public static bool DisarmMethod(MethodBase method)
        {
            if (ThrowOnDisarmMethodForTests) throw new InvalidOperationException("disarm method failure");
            ProfilerRegistry.RemoveForTests(method);
            return Methods.Remove(method);
        }
        public static int Disarm(ProfileTarget target)
        {
            if (ThrowOnDisarmTargetForTests) throw new InvalidOperationException("disarm target failure");
            if (!Targets.Remove(target)) return 0;
            var count = target.ArmedMethods;
            foreach (var method in target.Methods) ProfilerRegistry.RemoveForTests(method);
            target.Methods.Clear();
            target.ArmedMethods = 0;
            return count;
        }
        public static bool IsPatched(MethodBase method) => Methods.Contains(method);
        public static void DisarmAll()
        {
            Methods.Clear();
            foreach (var target in Targets) target.ArmedMethods = 0;
            Targets.Clear();
        }

        public static void ResetForTests()
        {
            DisarmAll();
            ThrowBeforeArmMethodForTests = false;
            ThrowAfterArmMethodForTests = false;
            ThrowAfterArmTargetForTests = false;
            ThrowOnDisarmMethodForTests = false;
            ThrowOnDisarmTargetForTests = false;
            IgnoreTargetHandArmedForTests = false;
        }

    }

    public static class ProfilerRegistry
    {
        private static readonly Dictionary<MethodBase, DevProfiler> Profilers =
            new Dictionary<MethodBase, DevProfiler>();

        public static volatile bool Enabled;
        public static volatile bool Recording;
        public static int CycleCount { get; private set; }
        public static int RecordedFrames { get; private set; }
        public static int SkippedFrames { get; private set; }
        public static double DutyPct { get; private set; } = 20d;

        public static DevProfiler Find(MethodBase method)
        {
            DevProfiler profiler;
            if (!Profilers.TryGetValue(method, out profiler))
            {
                profiler = new DevProfiler(method);
                Profilers.Add(method, profiler);
            }

            return profiler;
        }

        public static List<DevProfiler> All() => new List<DevProfiler>(Profilers.Values);

        public static void RemoveForTests(MethodBase method) => Profilers.Remove(method);

        public static void ResetAll()
        {
            Profilers.Clear();
            CycleCount = 0;
            RecordedFrames = 0;
            SkippedFrames = 0;
        }

        public static void ResetForTests()
        {
            Enabled = false;
            Recording = false;
            ResetAll();
            DutyPct = 20d;
        }
    }

    public static class TargetCatalogue
    {
        public static ProfileTarget Get(string key)
        {
            if (key == "fixture.valid") return new ProfileTarget(key, "fixture", 2);
            if (key == "fixture.overflow") return new ProfileTarget(key, "fixture", 1001);
            return null;
        }
    }
}

namespace Circinus.Identity
{
    using Circinus.Contract;

    public static class HarmonyIndex
    {
        private static readonly Dictionary<MethodBase, List<PatchRef>> Patches =
            new Dictionary<MethodBase, List<PatchRef>>();

        public static List<PatchRef> ForPatchMethod(MethodBase method)
        {
            List<PatchRef> refs;
            return Patches.TryGetValue(method, out refs) ? refs : null;
        }

        public static MethodRef RefOf(MethodBase method)
        {
            string identity;
            try
            {
                identity = method.Module.ModuleVersionId.ToString("N") + "-" + method.MetadataToken;
            }
            catch
            {
                identity = method.Name;
            }

            return new MethodRef { Key = "method-" + identity };
        }

        public static void RegisterPatchForTests(MethodBase method, string key)
        {
            Patches[method] = new List<PatchRef> { new PatchRef { Key = key } };
        }

        public static void RegisterPatchPairForTests(MethodBase method, string firstKey, string secondKey)
        {
            Patches[method] = new List<PatchRef>
            {
                new PatchRef { Key = firstKey },
                new PatchRef { Key = secondKey }
            };
        }

        public static void ResetForTests() => Patches.Clear();
    }
}

namespace Circinus.Bootstrap
{
    public sealed class CircinusSettings
    {
        public bool autoStartProfiler;
        public bool autoArmProfiler;
        public List<string> armedTargetKeys = new List<string>();
        public bool autoProfile;
        public int autoProfileAsked = 1;
        public bool showWarmupWindow;
        public int warmupSeconds;
        public bool ingestEnabled;
        public int consentVersion = 2;
        public bool autoRecord;
    }

    public static class CircinusMod
    {
        public static CircinusSettings Settings = new CircinusSettings();

        public static void ResetForTests()
        {
            Settings = new CircinusSettings();
        }
    }
}

namespace Circinus.UI
{
    public static class Window_Consent
    {
        public const int DisclosureVersion = 2;
    }

    public static class Window_AutoProfile
    {
        public const int AskedVersion = 1;
    }
}
