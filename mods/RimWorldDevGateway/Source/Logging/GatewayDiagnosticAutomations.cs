using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using Verse;

namespace RimWorldDevGateway;

public static class GatewayDiagnosticAutomations
{
    public static void Register(GatewayAutomationRegistry registry, GatewayErrorStore errors)
    {
        RegisterOne(registry, "diagnostics.errors", false, new[] { "id", "after", "limit", "filter" }, args =>
        {
            var id = Text(args, "id");
            if (id.Length != 0)
            {
                var error = errors.Get(id);
                return new { Error = error, Attribution = error.Frames.Concat(error.Causes.SelectMany(cause => cause.Frames))
                    .Select(frame => frame.MethodHandle).Where(handle => handle.Length != 0).Distinct().Take(576)
                    .Select(DescribeCaptured).ToArray(), AttributionTime = "current-query-time" };
            }
            var page = errors.Query(Number(args, "after", 0), checked((int)Number(args, "limit", 50)), Text(args, "filter"));
            return new { Entries = page.Entries.Select(error => new { error.Id, error.Sequence, error.Type, error.Message,
                error.FirstSeenUtc, error.LastSeenUtc, error.Occurrences, error.LastRequestId, error.Truncated }).ToArray(),
                page.NextCursor, page.PageTruncated, page.EvictedErrors };
        });
        RegisterOne(registry, "diagnostics.methods", false, new[] { "typeName", "assemblyName", "methodName", "offset", "limit" }, args =>
        {
            var typeName = Text(args, "typeName");
            if (typeName.Length == 0) throw new ArgumentException("typeName must name one exact loaded type.");
            var assemblyName = Text(args, "assemblyName");
            var methodName = Text(args, "methodName");
            var offset = Number(args, "offset", 0);
            var limit = Number(args, "limit", 50);
            if (offset < 0 || offset > 100000 || limit < 1 || limit > 100)
                throw new ArgumentException("offset must be 0–100000 and limit 1–100.");
            var types = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic &&
                (assemblyName.Length == 0 || assembly.GetName().Name == assemblyName))
                .Select(assembly => assembly.GetType(typeName, false)).Where(type => type is not null).Take(2).ToArray();
            if (types.Length != 1) throw new ArgumentException("Type is missing or ambiguous; specify its exact assemblyName.");
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                       BindingFlags.Static | BindingFlags.DeclaredOnly;
            var methods = types[0]!.GetMethods(flags).Cast<MethodBase>().Concat(types[0]!.GetConstructors(flags))
                .Where(method => methodName.Length == 0 || method.Name == methodName).OrderBy(method => method.MetadataToken)
                .Skip((int)offset).Take((int)limit + 1).ToArray();
            return new { Methods = methods.Take((int)limit).Select(DescribeAvailable).ToArray(),
                PageTruncated = methods.Length > limit, NextOffset = offset + Math.Min(methods.Length, limit) };
        });
        RegisterOne(registry, "diagnostics.method", false, new[] { "methodHandle" }, args =>
            Describe(GatewayMethodIdentity.Resolve(Text(args, "methodHandle"))));
        RegisterOne(registry, "diagnostics.merged", true, new[] { "methodHandle" }, args =>
        {
            var method = GatewayMethodIdentity.Resolve(Text(args, "methodHandle"));
            var before = Describe(method);
            using var merged = GatewayPatchedMethodBuilder.Build(method) ??
                throw new ArgumentException("The selected method has no current Harmony patches.");
            using var stream = new System.IO.MemoryStream();
            GatewayPatchedMethodAssemblyWriter.WriteAssembly(stream, merged);
            if (stream.Length > 512 * 1024) throw new ArgumentException("Reconstructed method exceeds the 512 KiB PE policy bound.");
            var after = Describe(method);
            if (before.CompositionHash != after.CompositionHash)
                throw new InvalidOperationException("Harmony composition changed during reconstruction.");
            return new { Method = before, AssemblyBase64 = Convert.ToBase64String(stream.ToArray()),
                MetadataToken = 0x06000001, Mode = "reconstructed-current", HistoricalCrashLine = (int?)null };
        });
    }

    private static object DescribeCaptured(string handle)
    {
        try { return Describe(GatewayMethodIdentity.Resolve(handle)); }
        catch (Exception exception) { return new { MethodHandle = handle, Unavailable = exception.Message }; }
    }

    private static object DescribeAvailable(MethodBase method) => GatewayMethodIdentity.TryHandle(method) is null
        ? (object)new { Signature = GatewayMethodIdentity.Signature(method),
            Unsupported = "Generic or dynamic context cannot be represented by an exact diagnostic handle." }
        : Describe(method);

    private static GatewayMethodDescription Describe(MethodBase method)
    {
        var info = HarmonySharedState.GetPatchInfo(method);
        var patches = new List<GatewayPatchDescription>();
        var truncated = false;
        void Add(IEnumerable<Patch> source, string kind)
        {
            var bounded = source.Take(257).ToArray();
            truncated |= bounded.Length > 256;
            foreach (var patch in bounded.Take(256))
            {
                truncated |= patch.before.Length > 64 || patch.after.Length > 64 ||
                    (patch.innerMethod?.positions.Length ?? 0) > 64;
                patches.Add(new GatewayPatchDescription(kind, patch.owner, patch.priority, patch.index,
                    patch.before.Take(64).ToArray(), patch.after.Take(64).ToArray(),
                    GatewayMethodIdentity.TryHandle(patch.PatchMethod), GatewayMethodIdentity.Signature(patch.PatchMethod),
                    patch.innerMethod is null ? null : GatewayMethodIdentity.Signature(patch.innerMethod.Method),
                    patch.innerMethod?.positions.Take(64).ToArray()));
            }
        }
        if (info is not null)
        {
            Add(info.prefixes, "prefix"); Add(info.postfixes, "postfix"); Add(info.transpilers, "transpiler");
            Add(info.finalizers, "finalizer"); Add(info.innerprefixes, "inner-prefix"); Add(info.innerpostfixes, "inner-postfix");
        }
        using var sha = SHA256.Create();
        // Hash Harmony's full composition, independently of display bounds, including infix targets/positions.
        var hash = BitConverter.ToString(sha.ComputeHash(info is null ? Array.Empty<byte>() :
            PatchInfoSerialization.Serialize(info))).Replace("-", "");
        var mod = LoadedModManager.RunningModsListForReading.FirstOrDefault(pack =>
            pack.assemblies.loadedAssemblies.Contains(method.Module.Assembly));
        var path = method.Module.FullyQualifiedName;
        return new GatewayMethodDescription(GatewayMethodIdentity.Handle(method), GatewayMethodIdentity.Signature(method),
            method.Module.ModuleVersionId.ToString("D"), method.MetadataToken, method.Module.Assembly.FullName,
            path, mod?.PackageId ?? (method.Module.Assembly.GetName().Name == "Assembly-CSharp" ? "ludeon.rimworld" : null),
            patches.AsReadOnly(), hash, truncated);
    }

    private static void RegisterOne(GatewayAutomationRegistry registry, string name, bool mutating,
        string[] fields, Func<IReadOnlyDictionary<string, object?>, object> action)
    {
        var properties = fields.ToDictionary(field => field, field => (object?)new Dictionary<string, object?>
            { ["type"] = field is "after" or "offset" or "limit" ? "integer" : "string" });
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor(name, "1.0", "Bounded Gateway error/method diagnostics.",
            new Dictionary<string, object?> { ["type"] = "object", ["additionalProperties"] = false, ["properties"] = properties },
            Array.Empty<string>(), mutating), (context, args) => context.RunStep(name, () =>
            {
                if (args.Keys.Any(key => !fields.Contains(key))) throw new ArgumentException("Unknown diagnostic argument.");
                context.CancellationToken.ThrowIfCancellationRequested();
                return action(args);
            }));
    }

    private static string Text(IReadOnlyDictionary<string, object?> args, string name)
    {
        if (!args.TryGetValue(name, out var value) || value is null) return string.Empty;
        if (value is not string text || text.Length > 1024) throw new ArgumentException(name + " must be a string of at most 1024 characters.");
        return text;
    }
    private static long Number(IReadOnlyDictionary<string, object?> args, string name, long fallback)
    {
        if (!args.TryGetValue(name, out var value)) return fallback;
        if (value is int integer) return integer;
        if (value is long longer) return longer;
        if (value is decimal number && number >= 0 && number <= long.MaxValue && number == decimal.Truncate(number)) return (long)number;
        throw new ArgumentException(name + " must be an integer.");
    }
}

public sealed class GatewayPatchDescription
{
    public GatewayPatchDescription(string kind, string owner, int priority, int index, string[] before, string[] after, string? handle, string signature,
        string? innerTarget, int[]? innerPositions)
    { Kind = kind; Owner = owner; Priority = priority; Index = index; Before = before; After = after; MethodHandle = handle; Signature = signature;
        InnerTarget = innerTarget; InnerPositions = innerPositions; }
    public string Kind { get; }
    public string Owner { get; }
    public int Priority { get; }
    public int Index { get; }
    public string[] Before { get; }
    public string[] After { get; }
    public string? MethodHandle { get; }
    public string? Unsupported => MethodHandle is null ? "Generic or dynamic patch context has no exact handle." : null;
    public string Signature { get; }
    public string? InnerTarget { get; }
    public int[]? InnerPositions { get; }
}

public sealed class GatewayMethodDescription
{
    public GatewayMethodDescription(string handle, string signature, string mvid, int token, string assembly, string path,
        string? packageId, IReadOnlyList<GatewayPatchDescription> patches, string hash, bool truncated)
    { MethodHandle = handle; Signature = signature; ModuleMvid = mvid; MetadataToken = token; Assembly = assembly;
        AssemblyPath = path; PackageId = packageId; Patches = patches; CompositionHash = hash; PatchesTruncated = truncated; }
    public string MethodHandle { get; }
    public string Signature { get; }
    public string ModuleMvid { get; }
    public int MetadataToken { get; }
    public string Assembly { get; }
    public string AssemblyPath { get; }
    public string? PackageId { get; }
    public IReadOnlyList<GatewayPatchDescription> Patches { get; }
    public string CompositionHash { get; }
    public bool PatchesTruncated { get; }
    public string PatchListOrder => "attachment-order (not execution order)";
}
