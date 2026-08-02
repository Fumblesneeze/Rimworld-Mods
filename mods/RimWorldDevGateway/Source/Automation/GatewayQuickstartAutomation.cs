using System.Collections.ObjectModel;
using System.Collections;
using RimWorld;
using Verse;

namespace RimWorldDevGateway;

public sealed class GatewayQuickstartCell
{
    public GatewayQuickstartCell(int x, int z)
    {
        X = x;
        Z = z;
    }

    public int X { get; }

    public int Z { get; }

    public GatewayQuickstartCell Offset(int x, int z) => new(X + x, Z + z);

    public override string ToString() => $"({X},0,{Z})";
}

public sealed class GatewayQuickstartDefinition
{
    public GatewayQuickstartDefinition(
        string category,
        string defName,
        object nativeDefinition,
        bool isBuilding = false,
        int stackLimit = 1,
        bool isItem = false,
        bool isStuff = false,
        bool madeFromStuff = false)
    {
        Category = category;
        DefName = defName;
        NativeDefinition = nativeDefinition ?? throw new ArgumentNullException(nameof(nativeDefinition));
        IsBuilding = isBuilding;
        StackLimit = Math.Max(1, stackLimit);
        IsItem = isItem;
        IsStuff = isStuff;
        MadeFromStuff = madeFromStuff;
    }

    public string Category { get; }

    public string DefName { get; }

    public bool IsBuilding { get; }

    public int StackLimit { get; }

    public bool IsItem { get; }

    public bool IsStuff { get; }

    public bool MadeFromStuff { get; }

    internal object NativeDefinition { get; }
}

public sealed class GatewayQuickstartPlacementReport
{
    public GatewayQuickstartPlacementReport(
        bool accepted,
        IEnumerable<GatewayQuickstartCell> occupiedCells,
        string? rejectionReason = null)
    {
        if (occupiedCells is null)
        {
            throw new ArgumentNullException(nameof(occupiedCells));
        }

        Accepted = accepted;
        OccupiedCells = new ReadOnlyCollection<GatewayQuickstartCell>(occupiedCells.ToArray());
        RejectionReason = rejectionReason;
    }

    public bool Accepted { get; }

    public IReadOnlyList<GatewayQuickstartCell> OccupiedCells { get; }

    public string? RejectionReason { get; }
}

public sealed class GatewayQuickstartSpawnSpec
{
    public GatewayQuickstartSpawnSpec(
        string path,
        string category,
        GatewayQuickstartDefinition definition,
        GatewayQuickstartDefinition? stuff,
        int count,
        string? quality,
        GatewayQuickstartCell position,
        bool powerOn)
    {
        Path = path;
        Category = category;
        Definition = definition;
        Stuff = stuff;
        Count = count;
        Quality = quality;
        Position = position;
        PowerOn = powerOn;
    }

    public string Path { get; }

    public string Category { get; }

    public GatewayQuickstartDefinition Definition { get; }

    public GatewayQuickstartDefinition? Stuff { get; }

    public int Count { get; }

    public string? Quality { get; }

    public GatewayQuickstartCell Position { get; }

    public bool PowerOn { get; }
}

public sealed class GatewayQuickstartSpawnedObject
{
    public GatewayQuickstartSpawnedObject(
        string handle,
        string category,
        string defName,
        string? stuff,
        int count,
        string? quality,
        GatewayQuickstartCell position)
    {
        Handle = handle;
        Category = category;
        DefName = defName;
        Stuff = stuff;
        Count = count;
        Quality = quality;
        Position = position;
    }

    public string Handle { get; }

    public string Category { get; }

    public string DefName { get; }

    public string? Stuff { get; }

    public int Count { get; }

    public string? Quality { get; }

    public GatewayQuickstartCell Position { get; }
}

public sealed class GatewayQuickstartWarning
{
    public GatewayQuickstartWarning(string code, string message, string path)
    {
        Code = code;
        Message = message;
        Path = path;
    }

    public string Code { get; }

    public string Message { get; }

    public string Path { get; }
}

public sealed class GatewayQuickstartWorldSpawnResult
{
    public GatewayQuickstartWorldSpawnResult(
        IEnumerable<GatewayQuickstartSpawnedObject> spawned,
        IEnumerable<GatewayQuickstartWarning>? warnings = null,
        bool powerEnabled = false)
    {
        Spawned = ReadOnly(spawned);
        Warnings = ReadOnly(warnings ?? Array.Empty<GatewayQuickstartWarning>());
        PowerEnabled = powerEnabled;
    }

    public IReadOnlyList<GatewayQuickstartSpawnedObject> Spawned { get; }

    public IReadOnlyList<GatewayQuickstartWarning> Warnings { get; }

    public bool PowerEnabled { get; }

    private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) =>
        new ReadOnlyCollection<T>(values.ToArray());
}

public sealed class GatewayQuickstartResolvedDefinition
{
    public GatewayQuickstartResolvedDefinition(string path, string category, string defName)
    {
        Path = path;
        Category = category;
        DefName = defName;
    }

    public string Path { get; }

    public string Category { get; }

    public string DefName { get; }
}

public sealed class GatewayQuickstartMutation
{
    public GatewayQuickstartMutation(
        int sequence,
        string operation,
        string path,
        string? handle,
        GatewayQuickstartCell? position,
        IReadOnlyDictionary<string, object?>? details = null)
    {
        Sequence = sequence;
        Operation = operation;
        Path = path;
        Handle = handle;
        Position = position;
        var detailCopy = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (details is not null)
        {
            foreach (var detail in details)
            {
                detailCopy.Add(detail.Key, detail.Value);
            }
        }

        Details = new ReadOnlyDictionary<string, object?>(detailCopy);
    }

    public int Sequence { get; }

    public string Operation { get; }

    public string Path { get; }

    public string? Handle { get; }

    public GatewayQuickstartCell? Position { get; }

    public IReadOnlyDictionary<string, object?> Details { get; }
}

public sealed class GatewayQuickstartResult
{
    public GatewayQuickstartResult(
        GatewayQuickstartCell center,
        IEnumerable<GatewayQuickstartResolvedDefinition>? resolvedDefinitions = null,
        IEnumerable<GatewayQuickstartSpawnedObject>? spawned = null,
        IEnumerable<GatewayQuickstartWarning>? warnings = null,
        IEnumerable<GatewayQuickstartMutation>? mutations = null)
    {
        Center = center;
        ResolvedDefinitions = ReadOnly(resolvedDefinitions ?? Array.Empty<GatewayQuickstartResolvedDefinition>());
        Spawned = ReadOnly(spawned ?? Array.Empty<GatewayQuickstartSpawnedObject>());
        Warnings = ReadOnly(warnings ?? Array.Empty<GatewayQuickstartWarning>());
        Mutations = ReadOnly(mutations ?? Array.Empty<GatewayQuickstartMutation>());
    }

    public GatewayQuickstartCell Center { get; }

    public IReadOnlyList<GatewayQuickstartResolvedDefinition> ResolvedDefinitions { get; }

    public IReadOnlyList<GatewayQuickstartSpawnedObject> Spawned { get; }

    public IReadOnlyList<GatewayQuickstartWarning> Warnings { get; }

    public IReadOnlyList<GatewayQuickstartMutation> Mutations { get; }

    private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) =>
        new ReadOnlyCollection<T>(values.ToArray());
}

public interface IGatewayQuickstartWorld
{
    GatewayAutomationAvailability GetAvailability();

    GatewayQuickstartCell DefaultCenter { get; }

    bool IsInBounds(GatewayQuickstartCell cell);

    IReadOnlyList<GatewayQuickstartCell> RadialCells(GatewayQuickstartCell center, int radius);

    GatewayQuickstartDefinition? ResolveDefinition(string category, string defName);

    GatewayQuickstartPlacementReport EvaluatePlacement(
        GatewayQuickstartDefinition definition,
        GatewayQuickstartDefinition? stuff,
        GatewayQuickstartCell position);

    IReadOnlyList<GatewayQuickstartSpawnedObject> ClearCell(GatewayQuickstartCell cell);

    GatewayQuickstartWorldSpawnResult Spawn(GatewayQuickstartSpawnSpec request);

    bool IsResearchFinished(GatewayQuickstartDefinition research);

    void FinishResearch(GatewayQuickstartDefinition research);

    string StartGameCondition(GatewayQuickstartDefinition condition, int durationTicks);

    GatewayQuickstartWarning? FocusCamera(GatewayQuickstartCell cell);
}

public static class GatewayQuickstartAutomation
{
    public const string Name = "quickstart.spawn";
    public const string Version = "1.0";
    public const int MaximumClearRadius = 20;
    public const int MaximumEntriesPerSection = 64;
    public const int MaximumSpawnCount = 256;
    public const int MaximumItemCount = 5_000;
    public const int MaximumPawnCount = 32;
    public const int MaximumOffset = 64;
    public const int MaximumConditionDurationTicks = 3_600_000;
    public const int PlacementSearchRadius = 12;

    public static void Register(GatewayAutomationRegistry registry) =>
        Register(registry, new VerseGatewayQuickstartWorld());

    public static void Register(GatewayAutomationRegistry registry, IGatewayQuickstartWorld world)
    {
        if (registry is null) throw new ArgumentNullException(nameof(registry));
        if (world is null) throw new ArgumentNullException(nameof(world));

        registry.RegisterBuiltIn(
            Descriptor(),
            (context, arguments) => Execute(context, arguments, world),
            world.GetAvailability);
    }

    public static GatewayAutomationDescriptor Descriptor()
    {
        var offset = ObjectSchema(
            new Dictionary<string, object?>
            {
                ["x"] = IntegerSchema(-MaximumOffset, MaximumOffset),
                ["z"] = IntegerSchema(-MaximumOffset, MaximumOffset)
            });
        var thingFields = new Dictionary<string, object?>
        {
            ["defName"] = StringSchema(),
            ["stuff"] = StringSchema(),
            ["quality"] = new Dictionary<string, object?>
            {
                ["type"] = "string",
                ["enum"] = new[] { "Awful", "Poor", "Normal", "Good", "Excellent", "Masterwork", "Legendary" }
            },
            ["offset"] = offset
        };
        var buildingFields = new Dictionary<string, object?>(thingFields, StringComparer.Ordinal)
        {
            ["count"] = IntegerSchema(1, MaximumSpawnCount),
            ["powerOn"] = new Dictionary<string, object?> { ["type"] = "boolean" }
        };
        var itemFields = new Dictionary<string, object?>(thingFields, StringComparer.Ordinal)
        {
            ["count"] = IntegerSchema(1, MaximumItemCount)
        };

        return new GatewayAutomationDescriptor(
            Name,
            Version,
            "Clear a bounded map area and spawn a deterministic developer verification scene.",
            new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["required"] = new[] { "version" },
                ["properties"] = new Dictionary<string, object?>
                {
                    ["version"] = new Dictionary<string, object?> { ["type"] = "integer", ["const"] = 1 },
                    ["center"] = ObjectSchema(new Dictionary<string, object?>
                    {
                        ["x"] = new Dictionary<string, object?> { ["type"] = "integer" },
                        ["z"] = new Dictionary<string, object?> { ["type"] = "integer" }
                    }),
                    ["clearRadius"] = IntegerSchema(0, MaximumClearRadius),
                    ["buildings"] = ArraySchema(
                        ObjectSchema(buildingFields, "defName"),
                        MaximumEntriesPerSection),
                    ["items"] = ArraySchema(
                        ObjectSchema(itemFields, "defName"),
                        MaximumEntriesPerSection),
                    ["pawns"] = ArraySchema(
                        ObjectSchema(new Dictionary<string, object?>
                        {
                            ["kindDefName"] = StringSchema(),
                            ["count"] = IntegerSchema(1, MaximumPawnCount),
                            ["offset"] = offset
                        }, "kindDefName"),
                        MaximumEntriesPerSection),
                    ["research"] = ArraySchema(
                        new Dictionary<string, object?>
                        {
                            ["oneOf"] = new object[]
                            {
                                StringSchema(),
                                ObjectSchema(
                                    new Dictionary<string, object?> { ["defName"] = StringSchema() },
                                    "defName")
                            }
                        },
                        MaximumEntriesPerSection),
                    ["gameConditions"] = ArraySchema(
                        ObjectSchema(new Dictionary<string, object?>
                        {
                            ["defName"] = StringSchema(),
                            ["durationTicks"] = IntegerSchema(1, MaximumConditionDurationTicks)
                        }, "defName"),
                        MaximumEntriesPerSection)
                }
            },
            new[] { "program-state-playing", "current-map", "no-long-event" },
            mutating: true);
    }

    private static IReadOnlyDictionary<string, object?> ObjectSchema(
        IReadOnlyDictionary<string, object?> properties,
        params string[] required)
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["properties"] = properties,
            ["required"] = required
        };
    }

    private static IReadOnlyDictionary<string, object?> ArraySchema(
        IReadOnlyDictionary<string, object?> items,
        int maximumItems)
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "array",
            ["maxItems"] = maximumItems,
            ["items"] = items
        };
    }

    private static IReadOnlyDictionary<string, object?> IntegerSchema(int minimum, int maximum)
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "integer",
            ["minimum"] = minimum,
            ["maximum"] = maximum
        };
    }

    private static IReadOnlyDictionary<string, object?> StringSchema()
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "string",
            ["minLength"] = 1,
            ["maxLength"] = 128
        };
    }

    private static GatewayQuickstartResult Execute(
        GatewayAutomationContext context,
        IReadOnlyDictionary<string, object?> arguments,
        IGatewayQuickstartWorld world)
    {
        var plan = context.RunStep("parse-descriptor", () => Parse(arguments, world.DefaultCenter));
        context.RunStep("preflight", () => Preflight(plan, world));
        context.ReportProgress("preflight", "All definitions and target cells resolved.", 0.1);

        var spawned = new List<GatewayQuickstartSpawnedObject>();
        var warnings = new List<GatewayQuickstartWarning>();
        var mutations = new List<GatewayQuickstartMutation>();
        var sequence = 0;

        foreach (var cell in plan.ClearCells)
        {
            var cleared = context.RunStep("clear-area", () => world.ClearCell(cell));
            foreach (var clearedObject in cleared)
            {
                mutations.Add(new GatewayQuickstartMutation(
                    ++sequence,
                    "clear",
                    "clearRadius",
                    clearedObject.Handle,
                    clearedObject.Position,
                    new Dictionary<string, object?> { ["defName"] = clearedObject.DefName }));
            }
        }

        context.ReportProgress("clear-area", "Bounded area clearing completed.", 0.25);
        foreach (var spawnPlan in plan.Spawns)
        {
            var remainingItemCount = spawnPlan.Count;
            foreach (var position in spawnPlan.Positions)
            {
                var requestCount = spawnPlan.Category == "item"
                    ? Math.Min(remainingItemCount, spawnPlan.Definition!.StackLimit)
                    : 1;
                var request = new GatewayQuickstartSpawnSpec(
                    spawnPlan.Path,
                    spawnPlan.Category,
                    spawnPlan.Definition!,
                    spawnPlan.Stuff,
                    requestCount,
                    spawnPlan.Quality,
                    position,
                    spawnPlan.PowerOn);
                var outcome = context.RunStep("spawn-" + spawnPlan.Category, () => world.Spawn(request));
                warnings.AddRange(outcome.Warnings);
                foreach (var spawnedObject in outcome.Spawned)
                {
                    spawned.Add(spawnedObject);
                    mutations.Add(new GatewayQuickstartMutation(
                        ++sequence,
                        "spawn." + spawnPlan.Category,
                        spawnPlan.Path,
                        spawnedObject.Handle,
                        spawnedObject.Position,
                        SpawnDetails(spawnedObject)));
                }

                if (spawnPlan.PowerOn)
                {
                    if (outcome.PowerEnabled)
                    {
                        mutations.Add(new GatewayQuickstartMutation(
                            ++sequence,
                            "power.on",
                            spawnPlan.Path + ".powerOn",
                            outcome.Spawned.FirstOrDefault()?.Handle,
                            position));
                    }
                    else if (!outcome.Warnings.Any(warning => warning.Code == "power_unavailable"))
                    {
                        warnings.Add(new GatewayQuickstartWarning(
                            "power_unavailable",
                            $"'{spawnPlan.DefName}' has no controllable power component.",
                            spawnPlan.Path + ".powerOn"));
                    }
                }


                if (spawnPlan.Category == "item")
                {
                    remainingItemCount -= requestCount;
                }
            }
        }

        context.ReportProgress("spawn", "Requested buildings, items, and pawns were spawned.", 0.75);
        foreach (var research in plan.Research)
        {
            if (world.IsResearchFinished(research.Definition!))
            {
                warnings.Add(new GatewayQuickstartWarning(
                    "research_already_finished",
                    $"Research '{research.DefName}' was already completed.",
                    research.Path));
                continue;
            }

            context.RunStep("complete-research", () => world.FinishResearch(research.Definition!));
            mutations.Add(new GatewayQuickstartMutation(
                ++sequence,
                "research.complete",
                research.Path,
                "research:" + research.DefName,
                null,
                new Dictionary<string, object?> { ["defName"] = research.DefName }));
        }

        foreach (var condition in plan.Conditions)
        {
            var handle = context.RunStep(
                "start-game-condition",
                () => world.StartGameCondition(condition.Definition!, condition.DurationTicks));
            mutations.Add(new GatewayQuickstartMutation(
                ++sequence,
                "condition.start",
                condition.Path,
                handle,
                null,
                new Dictionary<string, object?>
                {
                    ["defName"] = condition.DefName,
                    ["durationTicks"] = condition.DurationTicks
                }));
        }

        var cameraWarning = context.RunStep("focus-camera", () => world.FocusCamera(plan.Center));
        if (cameraWarning is not null)
        {
            warnings.Add(cameraWarning);
        }

        context.ReportProgress("complete", "Quickstart scene preparation completed.", 1);
        return new GatewayQuickstartResult(
            plan.Center,
            plan.ResolvedDefinitions,
            spawned,
            warnings,
            mutations);
    }

    private static IReadOnlyDictionary<string, object?> SpawnDetails(GatewayQuickstartSpawnedObject spawned)
    {
        return new Dictionary<string, object?>
        {
            ["category"] = spawned.Category,
            ["defName"] = spawned.DefName,
            ["stuff"] = spawned.Stuff,
            ["count"] = spawned.Count,
            ["quality"] = spawned.Quality
        };
    }

    private static QuickstartPlan Parse(
        IReadOnlyDictionary<string, object?> arguments,
        GatewayQuickstartCell defaultCenter)
    {
        EnsureOnlyProperties(
            arguments,
            string.Empty,
            "version",
            "center",
            "clearRadius",
            "buildings",
            "items",
            "pawns",
            "research",
            "gameConditions");
        var version = ReadRequiredInteger(arguments, "version", "version");
        if (version != 1)
        {
            throw new GatewayAutomationException(
                "unsupported_quickstart_version",
                $"Quickstart descriptor version 1 is required; received {version}.");
        }

        var plan = new QuickstartPlan
        {
            Center = arguments.TryGetValue("center", out var centerValue)
                ? ReadCell(centerValue, "center", defaultCenter)
                : defaultCenter,
            ClearRadius = ReadOptionalInteger(
                arguments,
                "clearRadius",
                "clearRadius",
                0,
                0,
                MaximumClearRadius)
        };

        ReadThingEntries(arguments, "buildings", "building", plan, MaximumSpawnCount);
        ReadThingEntries(arguments, "items", "item", plan, MaximumItemCount);
        ReadPawnEntries(arguments, plan);
        ReadResearchEntries(arguments, plan);
        ReadConditionEntries(arguments, plan);

        var spawnedEntities = plan.Spawns
            .Where(spawn => spawn.Category != "item")
            .Sum(spawn => spawn.Count);
        if (spawnedEntities > MaximumSpawnCount)
        {
            throw LimitError("buildings/pawns", MaximumSpawnCount);
        }

        return plan;
    }

    private static void ReadThingEntries(
        IReadOnlyDictionary<string, object?> arguments,
        string property,
        string category,
        QuickstartPlan plan,
        int maximumCount)
    {
        if (!arguments.TryGetValue(property, out var raw) || raw is null)
        {
            return;
        }

        var entries = ReadArray(raw, property, MaximumEntriesPerSection);
        for (var index = 0; index < entries.Count; index++)
        {
            var path = $"{property}[{index}]";
            var entry = ReadObject(entries[index], path);
            EnsureOnlyProperties(
                entry,
                path,
                category == "building"
                    ? new[] { "defName", "stuff", "count", "quality", "offset", "powerOn" }
                    : new[] { "defName", "stuff", "count", "quality", "offset" });
            var offset = ReadOptionalCell(entry, "offset", path + ".offset");
            plan.Spawns.Add(new SpawnPlan
            {
                Path = path,
                Category = category,
                DefinitionCategory = "thing",
                DefName = ReadRequiredString(entry, "defName", path + ".defName"),
                StuffName = ReadOptionalString(entry, "stuff", path + ".stuff"),
                Count = ReadOptionalInteger(entry, "count", path + ".count", 1, 1, maximumCount),
                Quality = ReadOptionalQuality(entry, path),
                OffsetX = offset.X,
                OffsetZ = offset.Z,
                PowerOn = category == "building" && ReadOptionalBoolean(entry, "powerOn", path + ".powerOn", false)
            });
        }
    }

    private static void ReadPawnEntries(IReadOnlyDictionary<string, object?> arguments, QuickstartPlan plan)
    {
        if (!arguments.TryGetValue("pawns", out var raw) || raw is null)
        {
            return;
        }

        var entries = ReadArray(raw, "pawns", MaximumEntriesPerSection);
        for (var index = 0; index < entries.Count; index++)
        {
            var path = $"pawns[{index}]";
            var entry = ReadObject(entries[index], path);
            EnsureOnlyProperties(entry, path, "kindDefName", "count", "offset");
            var offset = ReadOptionalCell(entry, "offset", path + ".offset");
            plan.Spawns.Add(new SpawnPlan
            {
                Path = path,
                Category = "pawn",
                DefinitionCategory = "pawnKind",
                DefName = ReadRequiredString(entry, "kindDefName", path + ".kindDefName"),
                Count = ReadOptionalInteger(entry, "count", path + ".count", 1, 1, MaximumPawnCount),
                OffsetX = offset.X,
                OffsetZ = offset.Z
            });
        }
    }

    private static void ReadResearchEntries(IReadOnlyDictionary<string, object?> arguments, QuickstartPlan plan)
    {
        if (!arguments.TryGetValue("research", out var raw) || raw is null)
        {
            return;
        }

        var entries = ReadArray(raw, "research", MaximumEntriesPerSection);
        for (var index = 0; index < entries.Count; index++)
        {
            var path = $"research[{index}]";
            string defName;
            if (entries[index] is string text)
            {
                defName = RequiredText(text, path);
            }
            else
            {
                var entry = ReadObject(entries[index], path);
                EnsureOnlyProperties(entry, path, "defName");
                defName = ReadRequiredString(entry, "defName", path + ".defName");
            }

            plan.Research.Add(new DefinitionPlan(path, "research", defName));
        }
    }

    private static void ReadConditionEntries(IReadOnlyDictionary<string, object?> arguments, QuickstartPlan plan)
    {
        if (!arguments.TryGetValue("gameConditions", out var raw) || raw is null)
        {
            return;
        }

        var entries = ReadArray(raw, "gameConditions", MaximumEntriesPerSection);
        for (var index = 0; index < entries.Count; index++)
        {
            var path = $"gameConditions[{index}]";
            var entry = ReadObject(entries[index], path);
            EnsureOnlyProperties(entry, path, "defName", "durationTicks");
            plan.Conditions.Add(new ConditionPlan(
                path,
                ReadRequiredString(entry, "defName", path + ".defName"),
                ReadOptionalInteger(
                    entry,
                    "durationTicks",
                    path + ".durationTicks",
                    60_000,
                    1,
                    MaximumConditionDurationTicks)));
        }
    }

    private static void Preflight(QuickstartPlan plan, IGatewayQuickstartWorld world)
    {
        var missing = new List<string>();
        foreach (var spawn in plan.Spawns)
        {
            spawn.Definition = Resolve(
                world,
                spawn.DefinitionCategory,
                spawn.DefName,
                spawn.Path + (spawn.Category == "pawn" ? ".kindDefName" : ".defName"),
                plan.ResolvedDefinitions,
                missing);
            if (spawn.StuffName is not null)
            {
                spawn.Stuff = Resolve(
                    world,
                    "thing",
                    spawn.StuffName,
                    spawn.Path + ".stuff",
                    plan.ResolvedDefinitions,
                    missing);
            }
        }

        foreach (var research in plan.Research)
        {
            research.Definition = Resolve(
                world,
                "research",
                research.DefName,
                research.Path,
                plan.ResolvedDefinitions,
                missing);
        }

        foreach (var condition in plan.Conditions)
        {
            condition.Definition = Resolve(
                world,
                "gameCondition",
                condition.DefName,
                condition.Path + ".defName",
                plan.ResolvedDefinitions,
                missing);
        }

        if (missing.Count > 0)
        {
            throw new GatewayAutomationException(
                "def_not_found",
                "Quickstart could not resolve: " + string.Join(", ", missing) + ".");
        }

        ValidateResolvedSpawns(plan.Spawns);

        var outOfBounds = new List<string>();
        if (!world.IsInBounds(plan.Center))
        {
            outOfBounds.Add("center=" + plan.Center);
        }

        foreach (var spawn in plan.Spawns)
        {
            spawn.BasePosition = plan.Center.Offset(spawn.OffsetX, spawn.OffsetZ);
            if (!world.IsInBounds(spawn.BasePosition))
            {
                outOfBounds.Add(spawn.Path + ".offset=" + spawn.BasePosition);
            }
        }

        if (outOfBounds.Count > 0)
        {
            throw new GatewayAutomationException(
                "cell_out_of_bounds",
                "Quickstart cells are outside the current map: " + string.Join(", ", outOfBounds) + ".");
        }

        if (plan.ClearRadius > 0)
        {
            plan.ClearCells.AddRange(DistinctInBounds(
                world.RadialCells(plan.Center, plan.ClearRadius),
                world));
        }

        var usedPositions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var spawn in plan.Spawns)
        {
            var requiredPositions = spawn.Category == "item"
                ? (spawn.Count + spawn.Definition!.StackLimit - 1) / spawn.Definition.StackLimit
                : spawn.Count;
            string? lastPlacementRejection = null;
            foreach (var candidate in DistinctInBounds(
                         world.RadialCells(spawn.BasePosition, PlacementSearchRadius),
                         world))
            {
                var placement = world.EvaluatePlacement(spawn.Definition!, spawn.Stuff, candidate);
                if (!placement.Accepted)
                {
                    lastPlacementRejection = placement.RejectionReason;
                    continue;
                }

                if (placement.OccupiedCells.Count == 0 ||
                    placement.OccupiedCells.Any(cell => !world.IsInBounds(cell)) ||
                    placement.OccupiedCells.Any(cell => usedPositions.Contains(CellKey(cell))))
                {
                    continue;
                }

                foreach (var occupiedCell in placement.OccupiedCells)
                {
                    usedPositions.Add(CellKey(occupiedCell));
                }

                spawn.Positions.Add(candidate);
                if (spawn.Positions.Count == requiredPositions)
                {
                    break;
                }
            }

            if (spawn.Positions.Count != requiredPositions)
            {
                throw new GatewayAutomationException(
                    "placement_unavailable",
                    $"'{spawn.Path}' requires {requiredPositions} deterministic cells near {spawn.BasePosition}, " +
                    $"but only {spawn.Positions.Count} are available." +
                    (string.IsNullOrWhiteSpace(lastPlacementRejection)
                        ? string.Empty
                        : " Last placement rejection: " + lastPlacementRejection));
            }
        }
    }

    private static void ValidateResolvedSpawns(IEnumerable<SpawnPlan> spawns)
    {
        var errors = new List<string>();
        foreach (var spawn in spawns)
        {
            if (spawn.Category == "building" && !spawn.Definition!.IsBuilding)
            {
                errors.Add(spawn.Path + ".defName is not a building ThingDef");
            }
            else if (spawn.Category == "item" && !spawn.Definition!.IsItem)
            {
                errors.Add(spawn.Path + ".defName is not an item ThingDef");
            }

            if (spawn.Stuff is not null && !spawn.Stuff.IsStuff)
            {
                errors.Add(spawn.Path + ".stuff is not a Stuff ThingDef");
            }

            if (spawn.Stuff is not null && !spawn.Definition!.MadeFromStuff)
            {
                errors.Add(spawn.Path + ".defName does not accept Stuff");
            }
        }

        if (errors.Count > 0)
        {
            throw new GatewayAutomationException(
                "invalid_quickstart_definition",
                "Quickstart definition compatibility failed: " + string.Join(", ", errors) + ".");
        }
    }

    private static IReadOnlyList<GatewayQuickstartCell> DistinctInBounds(
        IEnumerable<GatewayQuickstartCell> cells,
        IGatewayQuickstartWorld world)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<GatewayQuickstartCell>();
        foreach (var cell in cells)
        {
            if (world.IsInBounds(cell) && seen.Add(CellKey(cell)))
            {
                result.Add(cell);
            }
        }

        return result;
    }

    private static string CellKey(GatewayQuickstartCell cell) => cell.X + ":" + cell.Z;

    private static GatewayQuickstartDefinition? Resolve(
        IGatewayQuickstartWorld world,
        string category,
        string defName,
        string path,
        ICollection<GatewayQuickstartResolvedDefinition> resolved,
        ICollection<string> missing)
    {
        var definition = world.ResolveDefinition(category, defName);
        if (definition is null)
        {
            missing.Add(path + "='" + defName + "'");
        }
        else
        {
            resolved.Add(new GatewayQuickstartResolvedDefinition(path, category, definition.DefName));
        }

        return definition;
    }

    private static GatewayQuickstartCell ReadCell(object? value, string path, GatewayQuickstartCell fallback)
    {
        var coordinates = ReadObject(value, path);
        EnsureOnlyProperties(coordinates, path, "x", "z");
        return new GatewayQuickstartCell(
            ReadOptionalInteger(coordinates, "x", path + ".x", fallback.X, int.MinValue, int.MaxValue),
            ReadOptionalInteger(coordinates, "z", path + ".z", fallback.Z, int.MinValue, int.MaxValue));
    }

    private static GatewayQuickstartCell ReadOptionalCell(
        IReadOnlyDictionary<string, object?> owner,
        string property,
        string path)
    {
        if (!owner.TryGetValue(property, out var raw) || raw is null)
        {
            return new GatewayQuickstartCell(0, 0);
        }

        var coordinates = ReadObject(raw, path);
        EnsureOnlyProperties(coordinates, path, "x", "z");
        return new GatewayQuickstartCell(
            ReadOptionalInteger(
                coordinates,
                "x",
                path + ".x",
                0,
                -MaximumOffset,
                MaximumOffset),
            ReadOptionalInteger(
                coordinates,
                "z",
                path + ".z",
                0,
                -MaximumOffset,
                MaximumOffset));
    }

    private static IReadOnlyDictionary<string, object?> ReadObject(object? value, string path)
    {
        if (value is IReadOnlyDictionary<string, object?> readOnly)
        {
            return readOnly;
        }

        if (value is IDictionary dictionary)
        {
            var copy = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is not string key)
                {
                    throw DescriptorError(path, "object property names must be strings");
                }

                copy.Add(key, entry.Value);
            }

            return copy;
        }

        throw DescriptorError(path, "an object is required");
    }

    private static void EnsureOnlyProperties(
        IReadOnlyDictionary<string, object?> owner,
        string path,
        params string[] supported)
    {
        var allowed = new HashSet<string>(supported, StringComparer.Ordinal);
        foreach (var property in owner.Keys)
        {
            if (!allowed.Contains(property))
            {
                var propertyPath = string.IsNullOrEmpty(path) ? property : path + "." + property;
                throw DescriptorError(propertyPath, "is not a supported field");
            }
        }
    }

    private static IReadOnlyList<object?> ReadArray(object value, string path, int maximumCount)
    {
        if (value is string || value is IDictionary || value is not IEnumerable sequence)
        {
            throw DescriptorError(path, "an array is required");
        }

        var result = new List<object?>();
        foreach (var item in sequence)
        {
            if (result.Count == maximumCount)
            {
                throw LimitError(path, maximumCount);
            }

            result.Add(item);
        }

        return result;
    }

    private static string ReadRequiredString(
        IReadOnlyDictionary<string, object?> owner,
        string property,
        string path)
    {
        if (!owner.TryGetValue(property, out var value) || value is not string text)
        {
            throw DescriptorError(path, "a string is required");
        }

        return RequiredText(text, path);
    }

    private static string RequiredText(string text, string path)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 128)
        {
            throw DescriptorError(path, "must contain 1 to 128 characters");
        }

        return text;
    }

    private static string? ReadOptionalString(
        IReadOnlyDictionary<string, object?> owner,
        string property,
        string path)
    {
        if (!owner.TryGetValue(property, out var value) || value is null)
        {
            return null;
        }

        return value is string text
            ? RequiredText(text, path)
            : throw DescriptorError(path, "a string is required");
    }

    private static string? ReadOptionalQuality(IReadOnlyDictionary<string, object?> owner, string path)
    {
        var quality = ReadOptionalString(owner, "quality", path + ".quality");
        if (quality is null)
        {
            return null;
        }

        var supported = new[] { "Awful", "Poor", "Normal", "Good", "Excellent", "Masterwork", "Legendary" };
        var canonical = supported.FirstOrDefault(value =>
            string.Equals(value, quality, StringComparison.OrdinalIgnoreCase));
        return canonical ?? throw DescriptorError(path + ".quality", "is not a supported RimWorld quality");
    }

    private static bool ReadOptionalBoolean(
        IReadOnlyDictionary<string, object?> owner,
        string property,
        string path,
        bool fallback)
    {
        if (!owner.TryGetValue(property, out var value) || value is null)
        {
            return fallback;
        }

        return value is bool boolean
            ? boolean
            : throw DescriptorError(path, "a boolean is required");
    }

    private static int ReadRequiredInteger(
        IReadOnlyDictionary<string, object?> owner,
        string property,
        string path)
    {
        if (!owner.TryGetValue(property, out var value))
        {
            throw DescriptorError(path, "an integer is required");
        }

        return ReadInteger(value, path);
    }

    private static int ReadOptionalInteger(
        IReadOnlyDictionary<string, object?> owner,
        string property,
        string path,
        int fallback,
        int minimum,
        int maximum)
    {
        if (!owner.TryGetValue(property, out var value) || value is null)
        {
            return fallback;
        }

        var integer = ReadInteger(value, path);
        if (integer < minimum || integer > maximum)
        {
            throw new GatewayAutomationException(
                "quickstart_limit_exceeded",
                $"'{path}' must be between {minimum} and {maximum}; received {integer}.");
        }

        return integer;
    }

    private static int ReadInteger(object? value, string path)
    {
        try
        {
            var integer = value switch
            {
                sbyte number => number,
                byte number => number,
                short number => number,
                ushort number => number,
                int number => number,
                uint number when number <= int.MaxValue => (int)number,
                long number when number >= int.MinValue && number <= int.MaxValue => (int)number,
                ulong number when number <= int.MaxValue => (int)number,
                decimal number when decimal.Truncate(number) == number && number >= int.MinValue && number <= int.MaxValue => (int)number,
                double number when !double.IsNaN(number) && !double.IsInfinity(number) && Math.Truncate(number) == number && number >= int.MinValue && number <= int.MaxValue => (int)number,
                float number when !float.IsNaN(number) && !float.IsInfinity(number) && Math.Truncate(number) == number && number >= int.MinValue && number <= int.MaxValue => (int)number,
                _ => throw DescriptorError(path, "an integer is required")
            };
            return integer;
        }
        catch (OverflowException)
        {
            throw DescriptorError(path, "an integer is required");
        }
    }

    private static GatewayAutomationException DescriptorError(string path, string expectation) =>
        new("invalid_quickstart_descriptor", $"'{path}' {expectation}.");

    private static GatewayAutomationException LimitError(string path, int maximum) =>
        new("quickstart_limit_exceeded", $"'{path}' exceeds the maximum of {maximum} entries.");

    private sealed class QuickstartPlan
    {
        public GatewayQuickstartCell Center { get; set; } = null!;
        public int ClearRadius { get; set; }
        public List<SpawnPlan> Spawns { get; } = new();
        public List<DefinitionPlan> Research { get; } = new();
        public List<ConditionPlan> Conditions { get; } = new();
        public List<GatewayQuickstartResolvedDefinition> ResolvedDefinitions { get; } = new();
        public List<GatewayQuickstartCell> ClearCells { get; } = new();
    }

    private sealed class SpawnPlan
    {
        public string Path { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string DefinitionCategory { get; set; } = string.Empty;
        public string DefName { get; set; } = string.Empty;
        public string? StuffName { get; set; }
        public int Count { get; set; }
        public string? Quality { get; set; }
        public int OffsetX { get; set; }
        public int OffsetZ { get; set; }
        public bool PowerOn { get; set; }
        public GatewayQuickstartDefinition? Definition { get; set; }
        public GatewayQuickstartDefinition? Stuff { get; set; }
        public GatewayQuickstartCell BasePosition { get; set; } = null!;
        public List<GatewayQuickstartCell> Positions { get; } = new();
    }

    private class DefinitionPlan
    {
        public DefinitionPlan(string path, string category, string defName)
        {
            Path = path;
            Category = category;
            DefName = defName;
        }

        public string Path { get; }
        public string Category { get; }
        public string DefName { get; }
        public GatewayQuickstartDefinition? Definition { get; set; }
    }

    private sealed class ConditionPlan : DefinitionPlan
    {
        public ConditionPlan(string path, string defName, int durationTicks)
            : base(path, "gameCondition", defName)
        {
            DurationTicks = durationTicks;
        }

        public int DurationTicks { get; }
    }
}

public sealed class VerseGatewayQuickstartWorld : IGatewayQuickstartWorld
{
    public GatewayAutomationAvailability GetAvailability()
    {
        if (Current.ProgramState != ProgramState.Playing)
        {
            return new GatewayAutomationAvailability(false, "RimWorld is not in ProgramState.Playing.");
        }

        if (Current.Game?.CurrentMap is null)
        {
            return new GatewayAutomationAvailability(false, "There is no current playable map.");
        }

        if (LongEventHandler.AnyEventNowOrWaiting)
        {
            return new GatewayAutomationAvailability(false, "A RimWorld long event is active or waiting.");
        }

        return GatewayAutomationAvailability.AvailableNow;
    }

    public GatewayQuickstartCell DefaultCenter
    {
        get
        {
            var center = Map.Center;
            return new GatewayQuickstartCell(center.x, center.z);
        }
    }

    public bool IsInBounds(GatewayQuickstartCell cell) => ToIntVec3(cell).InBounds(Map);

    public IReadOnlyList<GatewayQuickstartCell> RadialCells(GatewayQuickstartCell center, int radius) =>
        new ReadOnlyCollection<GatewayQuickstartCell>(GenRadial
            .RadialCellsAround(ToIntVec3(center), radius, useCenter: true)
            .Where(cell => cell.InBounds(Map))
            .Select(cell => new GatewayQuickstartCell(cell.x, cell.z))
            .ToArray());

    public GatewayQuickstartDefinition? ResolveDefinition(string category, string defName)
    {
        Def? definition = category switch
        {
            "thing" => DefDatabase<ThingDef>.GetNamedSilentFail(defName),
            "pawnKind" => DefDatabase<PawnKindDef>.GetNamedSilentFail(defName),
            "research" => DefDatabase<ResearchProjectDef>.GetNamedSilentFail(defName),
            "gameCondition" => DefDatabase<GameConditionDef>.GetNamedSilentFail(defName),
            _ => null
        };
        if (definition is null)
        {
            return null;
        }

        var thingDef = definition as ThingDef;
        return new GatewayQuickstartDefinition(
            category,
            definition.defName,
            definition,
            thingDef?.category == ThingCategory.Building,
            thingDef?.stackLimit ?? 1,
            thingDef?.category == ThingCategory.Item,
            thingDef?.IsStuff ?? false,
            thingDef?.MadeFromStuff ?? false);
    }

    public GatewayQuickstartPlacementReport EvaluatePlacement(
        GatewayQuickstartDefinition definition,
        GatewayQuickstartDefinition? stuff,
        GatewayQuickstartCell position)
    {
        var center = ToIntVec3(position);
        if (definition.IsItem)
        {
            var itemDef = (ThingDef)definition.NativeDefinition;
            var accepted = GenSpawn.CanSpawnAt(
                itemDef,
                center,
                Map,
                Rot4.North,
                canWipeEdifices: false);
            return new GatewayQuickstartPlacementReport(
                accepted,
                new[] { position },
                accepted ? null : $"RimWorld rejected item placement at {position}.");
        }

        if (!definition.IsBuilding && definition.NativeDefinition is PawnKindDef pawnKind)
        {
            var accepted = GenSpawn.CanSpawnAt(
                pawnKind.race,
                center,
                Map,
                Rot4.North,
                canWipeEdifices: false);
            return new GatewayQuickstartPlacementReport(
                accepted,
                new[] { position },
                accepted ? null : $"RimWorld rejected pawn placement at {position}.");
        }

        if (!definition.IsBuilding)
        {
            return new GatewayQuickstartPlacementReport(
                accepted: false,
                new[] { position },
                $"Definition '{definition.DefName}' is not a spawnable building, item, or pawn kind.");
        }

        var thingDef = (ThingDef)definition.NativeDefinition;
        var occupied = GenAdj.OccupiedRect(center, Rot4.North, thingDef.size)
            .Cells
            .Select(cell => new GatewayQuickstartCell(cell.x, cell.z))
            .ToArray();
        var stuffDef = stuff?.NativeDefinition as ThingDef;
        if (stuffDef is null && thingDef.MadeFromStuff)
        {
            stuffDef = thingDef.defaultStuff;
        }

        var report = GenConstruct.CanPlaceBlueprintAt(
            thingDef,
            center,
            Rot4.North,
            Map,
            godMode: true,
            thingToIgnore: null,
            thing: null,
            stuffDef,
            ignoreEdgeArea: false,
            ignoreInteractionSpots: false,
            ignoreClearableFreeBuildings: true);
        return new GatewayQuickstartPlacementReport(
            report.Accepted,
            occupied,
            report.Accepted ? null : report.Reason);
    }

    public IReadOnlyList<GatewayQuickstartSpawnedObject> ClearCell(GatewayQuickstartCell cell)
    {
        var cleared = new List<GatewayQuickstartSpawnedObject>();
        foreach (var thing in Map.thingGrid.ThingsListAt(ToIntVec3(cell)).ToArray())
        {
            if (thing.Destroyed || thing is Pawn || !thing.def.destroyable)
            {
                continue;
            }

            var snapshot = Snapshot(thing, "cleared");
            thing.Destroy(DestroyMode.Vanish);
            cleared.Add(snapshot);
        }

        return new ReadOnlyCollection<GatewayQuickstartSpawnedObject>(cleared);
    }

    public GatewayQuickstartWorldSpawnResult Spawn(GatewayQuickstartSpawnSpec request)
    {
        var warnings = new List<GatewayQuickstartWarning>();
        var spawned = new List<GatewayQuickstartSpawnedObject>();
        var powerEnabled = false;
        switch (request.Category)
        {
            case "building":
            {
                var thing = MakeThing(request, warnings);
                SetPlayerFaction(thing);
                var result = GenSpawn.Spawn(
                    thing,
                    ToIntVec3(request.Position),
                    Map,
                    WipeMode.VanishOrMoveAside);
                powerEnabled = ApplyPowerRequest(result, request, warnings);
                spawned.Add(Snapshot(result, request.Category));
                break;
            }
            case "item":
            {
                var remaining = request.Count;
                while (remaining > 0)
                {
                    var stackCount = Math.Min(remaining, request.Definition.StackLimit);
                    var thing = MakeThing(request, warnings);
                    thing.stackCount = stackCount;
                    if (!GenPlace.TryPlaceThing(
                            thing,
                            ToIntVec3(request.Position),
                            Map,
                            ThingPlaceMode.Near,
                            out var placed,
                            null,
                            candidate => candidate.InBounds(Map),
                            null,
                            GatewayQuickstartAutomation.PlacementSearchRadius))
                    {
                        if (!thing.Destroyed)
                        {
                            thing.Destroy(DestroyMode.Vanish);
                        }

                        throw new GatewayAutomationException(
                            "placement_failed",
                            $"Could not place '{request.Definition.DefName}' near {request.Position}.");
                    }

                    spawned.Add(Snapshot(placed, request.Category));
                    remaining -= stackCount;
                }

                break;
            }
            case "pawn":
            {
                var pawn = PawnGenerator.GeneratePawn(
                    (PawnKindDef)request.Definition.NativeDefinition,
                    Faction.OfPlayer);
                var result = GenSpawn.Spawn(
                    pawn,
                    ToIntVec3(request.Position),
                    Map,
                    WipeMode.VanishOrMoveAside);
                spawned.Add(Snapshot(result, request.Category));
                break;
            }
            default:
                throw new GatewayAutomationException(
                    "invalid_quickstart_definition",
                    $"Unsupported spawn category '{request.Category}'.");
        }

        return new GatewayQuickstartWorldSpawnResult(spawned, warnings, powerEnabled);
    }

    private static Thing MakeThing(
        GatewayQuickstartSpawnSpec request,
        ICollection<GatewayQuickstartWarning> warnings)
    {
        var definition = (ThingDef)request.Definition.NativeDefinition;
        var stuff = request.Stuff?.NativeDefinition as ThingDef;
        if (stuff is null && definition.MadeFromStuff)
        {
            stuff = definition.defaultStuff;
        }

        var thing = ThingMaker.MakeThing(definition, stuff);
        ApplyQuality(thing, request, warnings);
        return thing;
    }

    private static void ApplyQuality(
        Thing thing,
        GatewayQuickstartSpawnSpec request,
        ICollection<GatewayQuickstartWarning> warnings)
    {
        if (request.Quality is null)
        {
            return;
        }

        var quality = thing.TryGetComp<CompQuality>();
        if (quality is null)
        {
            warnings.Add(new GatewayQuickstartWarning(
                "quality_unavailable",
                $"'{request.Definition.DefName}' has no CompQuality; requested quality was not applied.",
                request.Path + ".quality"));
            return;
        }

        quality.SetQuality(
            (QualityCategory)Enum.Parse(typeof(QualityCategory), request.Quality, ignoreCase: false),
            ArtGenerationContext.Colony);
    }

    private static bool ApplyPowerRequest(
        Thing thing,
        GatewayQuickstartSpawnSpec request,
        ICollection<GatewayQuickstartWarning> warnings)
    {
        if (!request.PowerOn)
        {
            return false;
        }

        var power = thing.TryGetComp<CompPowerTrader>();
        if (power is null)
        {
            warnings.Add(new GatewayQuickstartWarning(
                "power_unavailable",
                $"'{request.Definition.DefName}' has no CompPowerTrader; power-on was not applied.",
                request.Path + ".powerOn"));
            return false;
        }

        power.PowerOn = true;
        return power.PowerOn;
    }

    private static void SetPlayerFaction(Thing thing)
    {
        if (thing.def.CanHaveFaction)
        {
            thing.SetFaction(Faction.OfPlayer);
        }
    }

    private static GatewayQuickstartSpawnedObject Snapshot(Thing thing, string category)
    {
        var quality = thing.TryGetComp<CompQuality>();
        return new GatewayQuickstartSpawnedObject(
            thing.GetUniqueLoadID(),
            category,
            thing.def.defName,
            thing.Stuff?.defName,
            thing.stackCount,
            quality?.Quality.ToString(),
            new GatewayQuickstartCell(thing.Position.x, thing.Position.z));
    }

    public bool IsResearchFinished(GatewayQuickstartDefinition research) =>
        ((ResearchProjectDef)research.NativeDefinition).IsFinished;

    public void FinishResearch(GatewayQuickstartDefinition research) =>
        Find.ResearchManager.FinishProject((ResearchProjectDef)research.NativeDefinition, false, null, false);

    public string StartGameCondition(GatewayQuickstartDefinition condition, int durationTicks)
    {
        var gameCondition = GameConditionMaker.MakeCondition((GameConditionDef)condition.NativeDefinition, durationTicks);
        Map.GameConditionManager.RegisterCondition(gameCondition);
        return gameCondition.GetUniqueLoadID();
    }

    public GatewayQuickstartWarning? FocusCamera(GatewayQuickstartCell cell)
    {
        try
        {
            Find.CameraDriver.JumpToCurrentMapLoc(ToIntVec3(cell));
            return null;
        }
        catch (Exception exception)
        {
            return new GatewayQuickstartWarning(
                "camera_focus_failed",
                "The scene was created, but the camera could not be centered: " + exception.Message,
                "center");
        }
    }

    private static Map Map => Current.Game?.CurrentMap ??
        throw new GatewayAutomationException("automation_unavailable", "There is no current playable map.");

    private static IntVec3 ToIntVec3(GatewayQuickstartCell cell) => new(cell.X, 0, cell.Z);
}
