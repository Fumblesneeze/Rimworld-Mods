using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayQuickstartAutomationTests
{
    [Test]
    public void Registered_empty_quickstart_is_discoverable_and_uses_the_world_center()
    {
        var registry = new GatewayAutomationRegistry();
        var world = new RecordingWorld
        {
            Availability = GatewayAutomationAvailability.AvailableNow,
            DefaultCenter = new GatewayQuickstartCell(40, 60)
        };
        GatewayQuickstartAutomation.Register(registry, world);

        var descriptor = registry.Describe().Single(item => item.Name == "quickstart.spawn");
        var run = registry.StartRun(
            "quickstart.spawn",
            "request-empty",
            new Dictionary<string, object?> { ["version"] = 1 },
            "empty-scene");
        var result = (GatewayQuickstartResult)run.Result!;

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Version, Is.EqualTo("1.0"));
            Assert.That(descriptor.Mutating, Is.True);
            Assert.That(descriptor.Prerequisites, Is.EqualTo(new[] { "program-state-playing", "current-map", "no-long-event" }));
            Assert.That(descriptor.ArgumentSchema["type"], Is.EqualTo("object"));
            Assert.That(run.State, Is.EqualTo("succeeded"));
            Assert.That(result.Center.X, Is.EqualTo(40));
            Assert.That(result.Center.Z, Is.EqualTo(60));
            Assert.That(result.ResolvedDefinitions, Is.Empty);
            Assert.That(result.Spawned, Is.Empty);
            Assert.That(result.Warnings, Is.Empty);
            Assert.That(result.Mutations, Is.Empty);
            Assert.That(world.MutationCount, Is.Zero);
        });
    }

    [Test]
    public void Every_feasible_definition_is_preflighted_before_a_missing_def_can_mutate_the_map()
    {
        var registry = new GatewayAutomationRegistry();
        var world = new RecordingWorld
        {
            DefaultCenter = new GatewayQuickstartCell(20, 30)
        };
        world.MissingDefinitions.Add("thing:MissingBench");
        world.MissingDefinitions.Add("research:MissingResearch");
        GatewayQuickstartAutomation.Register(registry, world);

        var run = registry.StartRun(
            "quickstart.spawn",
            "request-preflight",
            new Dictionary<string, object?>
            {
                ["version"] = 1,
                ["clearRadius"] = 3,
                ["buildings"] = new object[]
                {
                    new Dictionary<string, object?>
                    {
                        ["defName"] = "MissingBench",
                        ["stuff"] = "Steel",
                        ["count"] = 1,
                        ["quality"] = "Good",
                        ["powerOn"] = true,
                        ["offset"] = new Dictionary<string, object?> { ["x"] = 1, ["z"] = 0 }
                    }
                },
                ["items"] = new object[]
                {
                    new Dictionary<string, object?> { ["defName"] = "ComponentIndustrial", ["count"] = 5 }
                },
                ["pawns"] = new object[]
                {
                    new Dictionary<string, object?> { ["kindDefName"] = "Colonist", ["count"] = 1 }
                },
                ["research"] = new object[] { "Electricity", "MissingResearch" },
                ["gameConditions"] = new object[]
                {
                    new Dictionary<string, object?> { ["defName"] = "Eclipse", ["durationTicks"] = 1200 }
                }
            });

        Assert.Multiple(() =>
        {
            Assert.That(run.State, Is.EqualTo("failed"));
            Assert.That(run.Error?.Code, Is.EqualTo("def_not_found"));
            Assert.That(run.Error?.Message, Does.Contain("buildings[0].defName"));
            Assert.That(run.Error?.Message, Does.Contain("research[1]"));
            Assert.That(world.ResolutionRequests, Is.EquivalentTo(new[]
            {
                "thing:MissingBench",
                "thing:Steel",
                "thing:ComponentIndustrial",
                "pawnKind:Colonist",
                "research:Electricity",
                "research:MissingResearch",
                "gameCondition:Eclipse"
            }));
            Assert.That(world.MutationCount, Is.Zero);
        });
    }

    [Test]
    public void Valid_scene_returns_stable_spawned_objects_warnings_and_ordered_mutation_ledger()
    {
        var registry = new GatewayAutomationRegistry();
        var world = new RecordingWorld { DefaultCenter = new GatewayQuickstartCell(10, 10) };
        GatewayQuickstartAutomation.Register(registry, world);

        var run = registry.StartRun(
            "quickstart.spawn",
            "request-scene",
            new Dictionary<string, object?>
            {
                ["version"] = 1,
                ["center"] = new Dictionary<string, object?> { ["x"] = 100, ["z"] = 200 },
                ["clearRadius"] = 1,
                ["buildings"] = new object[]
                {
                    new Dictionary<string, object?>
                    {
                        ["defName"] = "ElectricStove",
                        ["stuff"] = "Steel",
                        ["count"] = 2,
                        ["quality"] = "Excellent",
                        ["powerOn"] = true,
                        ["offset"] = new Dictionary<string, object?> { ["x"] = 2, ["z"] = 1 }
                    }
                },
                ["items"] = new object[]
                {
                    new Dictionary<string, object?>
                    {
                        ["defName"] = "ComponentIndustrial",
                        ["count"] = 5,
                        ["quality"] = "Good",
                        ["offset"] = new Dictionary<string, object?> { ["x"] = -1, ["z"] = 0 }
                    }
                },
                ["pawns"] = new object[]
                {
                    new Dictionary<string, object?>
                    {
                        ["kindDefName"] = "Colonist",
                        ["count"] = 2,
                        ["offset"] = new Dictionary<string, object?> { ["x"] = 0, ["z"] = 2 }
                    }
                },
                ["research"] = new object[] { "Electricity" },
                ["gameConditions"] = new object[]
                {
                    new Dictionary<string, object?> { ["defName"] = "Eclipse", ["durationTicks"] = 1200 }
                }
            },
            "scene-key");
        var result = (GatewayQuickstartResult)run.Result!;

        Assert.Multiple(() =>
        {
            Assert.That(run.State, Is.EqualTo("succeeded"));
            Assert.That(result.Center.X, Is.EqualTo(100));
            Assert.That(result.Center.Z, Is.EqualTo(200));
            Assert.That(result.ResolvedDefinitions, Has.Count.EqualTo(6));
            Assert.That(result.Spawned, Has.Count.EqualTo(5));
            Assert.That(result.Spawned.Count(item => item.Category == "building"), Is.EqualTo(2));
            Assert.That(result.Spawned.Single(item => item.Category == "item").Count, Is.EqualTo(5));
            Assert.That(result.Spawned.Single(item => item.Category == "item").Quality, Is.EqualTo("Good"));
            Assert.That(result.Spawned.Count(item => item.Category == "pawn"), Is.EqualTo(2));
            Assert.That(result.Warnings, Is.Empty);
            Assert.That(result.Mutations.Select(item => item.Sequence),
                Is.EqualTo(Enumerable.Range(1, result.Mutations.Count)));
            Assert.That(result.Mutations.Select(item => item.Operation), Does.Contain("spawn.building"));
            Assert.That(result.Mutations.Select(item => item.Operation), Does.Contain("spawn.item"));
            Assert.That(result.Mutations.Select(item => item.Operation), Does.Contain("spawn.pawn"));
            Assert.That(result.Mutations.Select(item => item.Operation), Does.Contain("power.on"));
            Assert.That(result.Mutations.Select(item => item.Operation), Does.Contain("research.complete"));
            Assert.That(result.Mutations.Select(item => item.Operation), Does.Contain("condition.start"));
            Assert.That(world.SpawnRequests.Select(request => request.Position.ToString()).Distinct().Count(),
                Is.EqualTo(world.SpawnRequests.Count));
            Assert.That(world.FinishedResearch, Is.EqualTo(new[] { "Electricity" }));
            Assert.That(world.StartedConditions, Is.EqualTo(new[] { "Eclipse:1200" }));
        });
    }

    [Test]
    public void Preflight_reserves_complete_building_footprints_before_choosing_later_positions()
    {
        var registry = new GatewayAutomationRegistry();
        var world = new RecordingWorld { DefaultCenter = new GatewayQuickstartCell(25, 25) };
        world.BuildingWidths["WideBenchA"] = 3;
        world.BuildingWidths["WideBenchB"] = 3;
        GatewayQuickstartAutomation.Register(registry, world);

        var run = registry.StartRun(
            "quickstart.spawn",
            "request-footprints",
            new Dictionary<string, object?>
            {
                ["version"] = 1,
                ["buildings"] = new object[]
                {
                    new Dictionary<string, object?> { ["defName"] = "WideBenchA" },
                    new Dictionary<string, object?> { ["defName"] = "WideBenchB" }
                }
            });

        var occupied = world.SpawnRequests
            .SelectMany(request => world.FootprintFor(request.Definition.DefName, request.Position))
            .Select(cell => cell.ToString())
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(run.State, Is.EqualTo("succeeded"), run.Error?.Message);
            Assert.That(world.SpawnRequests, Has.Count.EqualTo(2));
            Assert.That(occupied.Distinct().Count(), Is.EqualTo(occupied.Length),
                "Quickstart placed multi-cell building footprints on top of one another.");
        });
    }

    [Test]
    public void Preflight_skips_building_cells_rejected_by_the_world_placement_rules()
    {
        var registry = new GatewayAutomationRegistry();
        var world = new RecordingWorld { DefaultCenter = new GatewayQuickstartCell(12, 18) };
        world.BuildingWidths["TerrainSensitiveBench"] = 2;
        world.RejectedBuildingCenters.Add("12:18");
        GatewayQuickstartAutomation.Register(registry, world);

        var run = registry.StartRun(
            "quickstart.spawn",
            "request-placement-rules",
            new Dictionary<string, object?>
            {
                ["version"] = 1,
                ["buildings"] = new object[]
                {
                    new Dictionary<string, object?> { ["defName"] = "TerrainSensitiveBench" }
                }
            });

        Assert.Multiple(() =>
        {
            Assert.That(run.State, Is.EqualTo("succeeded"), run.Error?.Message);
            Assert.That(world.SpawnRequests, Has.Count.EqualTo(1));
            Assert.That(world.SpawnRequests.Single().Position.ToString(), Is.Not.EqualTo("(12,0,18)"));
        });
    }

    [Test]
    public void Preflight_skips_non_building_cells_rejected_by_the_world_spawn_rules()
    {
        var registry = new GatewayAutomationRegistry();
        var world = new RecordingWorld { DefaultCenter = new GatewayQuickstartCell(14, 20) };
        world.RejectedNonBuildingCenters.Add("14:20");
        GatewayQuickstartAutomation.Register(registry, world);

        var run = registry.StartRun(
            "quickstart.spawn",
            "request-item-placement-rules",
            new Dictionary<string, object?>
            {
                ["version"] = 1,
                ["items"] = new object[]
                {
                    new Dictionary<string, object?>
                    {
                        ["defName"] = "ComponentIndustrial",
                        ["count"] = 1
                    }
                }
            });

        Assert.Multiple(() =>
        {
            Assert.That(run.State, Is.EqualTo("succeeded"), run.Error?.Message);
            Assert.That(world.SpawnRequests, Has.Count.EqualTo(1));
            Assert.That(world.SpawnRequests.Single().Position.ToString(), Is.Not.EqualTo("(14,0,20)"));
        });
    }

    [Test]
    public void Out_of_range_offsets_fail_before_definition_resolution_or_mutation()
    {
        var registry = new GatewayAutomationRegistry();
        var world = new RecordingWorld();
        GatewayQuickstartAutomation.Register(registry, world);

        var run = registry.StartRun(
            "quickstart.spawn",
            "request-offset-limit",
            new Dictionary<string, object?>
            {
                ["version"] = 1,
                ["items"] = new object[]
                {
                    new Dictionary<string, object?>
                    {
                        ["defName"] = "ComponentIndustrial",
                        ["offset"] = new Dictionary<string, object?>
                        {
                            ["x"] = GatewayQuickstartAutomation.MaximumOffset + 1,
                            ["z"] = 0
                        }
                    }
                }
            });

        Assert.Multiple(() =>
        {
            Assert.That(run.State, Is.EqualTo("failed"));
            Assert.That(run.Error?.Code, Is.EqualTo("quickstart_limit_exceeded"));
            Assert.That(run.Error?.Message, Does.Contain("items[0].offset.x"));
            Assert.That(world.ResolutionRequests, Is.Empty);
            Assert.That(world.MutationCount, Is.Zero);
        });
    }

    [Test]
    public void Descriptor_publishes_the_complete_bounded_version_one_schema()
    {
        var schema = GatewayQuickstartAutomation.Descriptor().ArgumentSchema;
        var properties = (IReadOnlyDictionary<string, object?>)schema["properties"]!;
        var clearRadius = (IReadOnlyDictionary<string, object?>)properties["clearRadius"]!;
        var buildings = (IReadOnlyDictionary<string, object?>)properties["buildings"]!;
        var items = (IReadOnlyDictionary<string, object?>)properties["items"]!;
        var pawns = (IReadOnlyDictionary<string, object?>)properties["pawns"]!;
        var conditions = (IReadOnlyDictionary<string, object?>)properties["gameConditions"]!;

        Assert.Multiple(() =>
        {
            Assert.That(schema["additionalProperties"], Is.False);
            Assert.That(properties.Keys, Is.EquivalentTo(new[]
            {
                "version", "center", "clearRadius", "buildings", "items", "pawns", "research", "gameConditions"
            }));
            Assert.That(clearRadius["maximum"], Is.EqualTo(GatewayQuickstartAutomation.MaximumClearRadius));
            Assert.That(buildings["maxItems"], Is.EqualTo(GatewayQuickstartAutomation.MaximumEntriesPerSection));
            Assert.That(items["maxItems"], Is.EqualTo(GatewayQuickstartAutomation.MaximumEntriesPerSection));
            Assert.That(pawns["maxItems"], Is.EqualTo(GatewayQuickstartAutomation.MaximumEntriesPerSection));
            Assert.That(conditions["maxItems"], Is.EqualTo(GatewayQuickstartAutomation.MaximumEntriesPerSection));
        });
    }

    [Test]
    public void Successful_setup_focuses_the_camera_and_surfaces_a_non_fatal_focus_warning()
    {
        var registry = new GatewayAutomationRegistry();
        var world = new RecordingWorld
        {
            DefaultCenter = new GatewayQuickstartCell(70, 80),
            CameraWarning = new GatewayQuickstartWarning(
                "camera_focus_failed",
                "Test camera is unavailable.",
                "center")
        };
        GatewayQuickstartAutomation.Register(registry, world);

        var run = registry.StartRun(
            "quickstart.spawn",
            "request-camera",
            new Dictionary<string, object?> { ["version"] = 1 });
        var result = (GatewayQuickstartResult)run.Result!;

        Assert.Multiple(() =>
        {
            Assert.That(run.State, Is.EqualTo("succeeded"));
            Assert.That(world.FocusedCells.Select(cell => cell.ToString()), Is.EqualTo(new[] { "(70,0,80)" }));
            Assert.That(result.Warnings.Select(warning => warning.Code), Is.EqualTo(new[] { "camera_focus_failed" }));
        });
    }

    [Test]
    public void Undocumented_descriptor_fields_are_rejected_before_preflight()
    {
        var registry = new GatewayAutomationRegistry();
        var world = new RecordingWorld();
        GatewayQuickstartAutomation.Register(registry, world);

        var run = registry.StartRun(
            "quickstart.spawn",
            "request-unknown-field",
            new Dictionary<string, object?>
            {
                ["version"] = 1,
                ["buildings"] = new object[]
                {
                    new Dictionary<string, object?>
                    {
                        ["defName"] = "ElectricStove",
                        ["rotation"] = "North"
                    }
                }
            });

        Assert.Multiple(() =>
        {
            Assert.That(run.State, Is.EqualTo("failed"));
            Assert.That(run.Error?.Code, Is.EqualTo("invalid_quickstart_descriptor"));
            Assert.That(run.Error?.Message, Does.Contain("buildings[0].rotation"));
            Assert.That(world.ResolutionRequests, Is.Empty);
            Assert.That(world.MutationCount, Is.Zero);
        });
    }

    [Test]
    public void Automation_request_json_nested_descriptor_reaches_the_registered_automation()
    {
        var payload = GatewayAutomationRequestJson.Read(
            "{\"arguments\":{\"version\":1,\"center\":{\"x\":15,\"z\":25}," +
            "\"items\":[{\"defName\":\"ComponentIndustrial\",\"count\":3}]}," +
            "\"idempotencyKey\":\"json-scene\"}");
        var registry = new GatewayAutomationRegistry();
        var world = new RecordingWorld();
        GatewayQuickstartAutomation.Register(registry, world);

        var run = registry.StartRun(
            "quickstart.spawn",
            "request-json",
            payload.Arguments,
            payload.IdempotencyKey);
        Assert.That(run.State, Is.EqualTo("succeeded"), run.Error?.Message);
        var result = (GatewayQuickstartResult)run.Result!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Center.ToString(), Is.EqualTo("(15,0,25)"));
            Assert.That(result.Spawned.Single().DefName, Is.EqualTo("ComponentIndustrial"));
            Assert.That(result.Spawned.Single().Count, Is.EqualTo(3));
        });
    }

    [Test]
    public void Automation_request_json_rejects_malformed_and_excessively_nested_input()
    {
        var malformed = Assert.Throws<System.Runtime.Serialization.SerializationException>(() =>
            GatewayAutomationRequestJson.Read("{\"arguments\":{\"items\":[}}"));
        var nested = "{\"arguments\":" +
                     new string('[', GatewayAutomationRequestJson.MaximumDepth + 1) +
                     "null" +
                     new string(']', GatewayAutomationRequestJson.MaximumDepth + 1) +
                     "}";
        var tooDeep = Assert.Throws<System.Runtime.Serialization.SerializationException>(() =>
            GatewayAutomationRequestJson.Read(nested));

        Assert.Multiple(() =>
        {
            Assert.That(malformed!.Message, Does.Contain("not valid JSON"));
            Assert.That(tooDeep!.Message, Does.Contain("maximum depth"));
        });
    }

    [Test]
    public void Automation_router_preserves_nested_quickstart_arguments_on_the_dispatcher()
    {
        var dispatcher = new GatewayDispatcher();
        var registry = new GatewayAutomationRegistry();
        var world = new RecordingWorld();
        GatewayQuickstartAutomation.Register(registry, world);
        var router = new GatewayApiRouter(
            dispatcher,
            new PlayingStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(automations: registry),
            responseTimeout: TimeSpan.FromSeconds(2));
        var json =
            "{\"arguments\":{\"version\":1,\"center\":{\"x\":35,\"z\":45}," +
            "\"items\":[{\"defName\":\"ComponentIndustrial\",\"count\":4}]}," +
            "\"idempotencyKey\":\"router-scene\"}";
        var request = new GatewayHttpRequest(
            "POST",
            "/api/v1/automations/quickstart.spawn/runs",
            "/api/v1/automations/quickstart.spawn/runs",
            string.Empty,
            new Dictionary<string, string>(),
            System.Text.Encoding.UTF8.GetBytes(json));

        var responseTask = Task.Run(() => router.Handle(request, "router-quickstart"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        var response = responseTask.GetAwaiter().GetResult();
        var body = System.Text.Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200), body);
            Assert.That(body, Does.Contain("succeeded"));
            Assert.That(body, Does.Contain("ComponentIndustrial"));
            Assert.That(world.SpawnRequests.Single().Count, Is.EqualTo(4));
            Assert.That(world.SpawnRequests.Single().Position.ToString(), Is.EqualTo("(35,0,45)"));
        });
    }

    private sealed class RecordingWorld : IGatewayQuickstartWorld
    {
        public GatewayAutomationAvailability Availability { get; set; } = GatewayAutomationAvailability.AvailableNow;

        public GatewayQuickstartCell DefaultCenter { get; set; } = new(0, 0);

        public int MutationCount { get; private set; }

        public ISet<string> MissingDefinitions { get; } = new HashSet<string>(StringComparer.Ordinal);

        public IList<string> ResolutionRequests { get; } = new List<string>();

        public IList<GatewayQuickstartCell> ClearedCells { get; } = new List<GatewayQuickstartCell>();

        public IList<GatewayQuickstartSpawnSpec> SpawnRequests { get; } = new List<GatewayQuickstartSpawnSpec>();

        public IList<string> FinishedResearch { get; } = new List<string>();

        public IList<string> StartedConditions { get; } = new List<string>();

        public IList<GatewayQuickstartCell> FocusedCells { get; } = new List<GatewayQuickstartCell>();

        public IDictionary<string, int> BuildingWidths { get; } = new Dictionary<string, int>(StringComparer.Ordinal);

        public ISet<string> RejectedBuildingCenters { get; } = new HashSet<string>(StringComparer.Ordinal);

        public ISet<string> RejectedNonBuildingCenters { get; } = new HashSet<string>(StringComparer.Ordinal);

        public GatewayQuickstartWarning? CameraWarning { get; set; }

        public GatewayAutomationAvailability GetAvailability() => Availability;

        public bool IsInBounds(GatewayQuickstartCell cell) => true;

        public IReadOnlyList<GatewayQuickstartCell> RadialCells(GatewayQuickstartCell center, int radius)
        {
            var count = radius == 0 ? 1 : Math.Min(1 + radius * 4, 64);
            return Enumerable.Range(0, count)
                .Select(index => center.Offset(index, 0))
                .ToArray();
        }

        public GatewayQuickstartDefinition? ResolveDefinition(string category, string defName)
        {
            var key = category + ":" + defName;
            ResolutionRequests.Add(key);
            return MissingDefinitions.Contains(key)
                ? null
                : new GatewayQuickstartDefinition(
                    category,
                    defName,
                    new object(),
                    isBuilding: defName == "ElectricStove" || BuildingWidths.ContainsKey(defName),
                    stackLimit: defName == "ComponentIndustrial" ? 50 : 1,
                    isItem: defName == "ComponentIndustrial",
                    isStuff: defName == "Steel",
                    madeFromStuff: defName == "ElectricStove");
        }

        public IReadOnlyList<GatewayQuickstartCell> FootprintFor(
            string defName,
            GatewayQuickstartCell position)
        {
            var width = BuildingWidths.TryGetValue(defName, out var configuredWidth)
                ? configuredWidth
                : 1;
            return Enumerable.Range(0, width)
                .Select(offset => position.Offset(offset, 0))
                .ToArray();
        }

        public GatewayQuickstartPlacementReport EvaluatePlacement(
            GatewayQuickstartDefinition definition,
            GatewayQuickstartDefinition? stuff,
            GatewayQuickstartCell position)
        {
            var key = position.X + ":" + position.Z;
            var accepted = definition.IsBuilding
                ? !RejectedBuildingCenters.Contains(key)
                : !RejectedNonBuildingCenters.Contains(key);
            return new GatewayQuickstartPlacementReport(
                accepted,
                FootprintFor(definition.DefName, position),
                accepted ? null : "The configured terrain rejects this building.");
        }

        public IReadOnlyList<GatewayQuickstartSpawnedObject> ClearCell(GatewayQuickstartCell cell)
        {
            MutationCount++;
            ClearedCells.Add(cell);
            return new[]
            {
                new GatewayQuickstartSpawnedObject(
                    "cleared-" + MutationCount,
                    "cleared",
                    "Obstacle",
                    null,
                    1,
                    null,
                    cell)
            };
        }

        public GatewayQuickstartWorldSpawnResult Spawn(GatewayQuickstartSpawnSpec request)
        {
            MutationCount++;
            SpawnRequests.Add(request);
            return new GatewayQuickstartWorldSpawnResult(
                new[]
                {
                    new GatewayQuickstartSpawnedObject(
                        "spawned-" + MutationCount,
                        request.Category,
                        request.Definition.DefName,
                        request.Stuff?.DefName,
                        request.Count,
                        request.Quality,
                        request.Position)
                },
                powerEnabled: request.PowerOn);
        }

        public bool IsResearchFinished(GatewayQuickstartDefinition research) => false;

        public void FinishResearch(GatewayQuickstartDefinition research)
        {
            MutationCount++;
            FinishedResearch.Add(research.DefName);
        }

        public string StartGameCondition(GatewayQuickstartDefinition condition, int durationTicks)
        {
            MutationCount++;
            StartedConditions.Add(condition.DefName + ":" + durationTicks);
            return "condition-" + condition.DefName;
        }

        public GatewayQuickstartWarning? FocusCamera(GatewayQuickstartCell cell)
        {
            FocusedCells.Add(cell);
            return CameraWarning;
        }
    }

    private sealed class PlayingStateProvider : IGatewayStateProvider
    {
        public object CaptureStatus() => new { ProgramState = "Playing" };

        public object CaptureUiState() => new { };
    }
}
