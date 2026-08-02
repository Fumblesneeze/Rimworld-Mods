using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayStateProviderTests
{
    [Test]
    public void Verse_source_does_not_dereference_Find_game_services_at_the_main_menu()
    {
        Assert.That(Verse.Current.Game, Is.Null, "The framework fixture must represent the entry state.");
        var source = new VerseGatewaySnapshotSource();

        Assert.Multiple(() =>
        {
            Assert.That(source.Tick, Is.Null);
            Assert.That(source.Map, Is.Null);
            Assert.That(source.Selection, Is.Empty);
        });
    }

    [Test]
    public void Status_is_a_bounded_immutable_snapshot_with_danger_and_dispatch_state()
    {
        var dispatcher = new GatewayDispatcher(capacity: 8);
        var source = new StubSnapshotSource
        {
            ProgramState = "Playing",
            RootType = "Root_Play",
            Tick = 1234,
            Map = new GatewayMapSnapshot("map-7", "Temperate Forest", 250, 250),
            Windows = new[] { new GatewayWindowSnapshot("window-1", "MainTabWindow_Architect", false) },
            Selection = new[] { new GatewaySelectionSnapshot("thing-42", "Steel") }
        };
        var provider = new RimWorldGatewayStateProvider(source, dispatcher, processId: 99);

        var json = Encoding.UTF8.GetString(GatewayJsonWriter.Write(provider.CaptureStatus()));

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("\"developerOnly\":true"));
            Assert.That(json, Does.Contain("\"unrestrictedExecutionEnabled\":true"));
            Assert.That(json, Does.Contain("\"processId\":99"));
            Assert.That(json, Does.Contain("\"programState\":\"Playing\""));
            Assert.That(json, Does.Contain("\"pendingDispatches\":0"));
            Assert.That(json, Does.Contain("\"map-7\""));
        });
    }

    [Test]
    public void Ui_state_contains_only_stable_snapshot_values()
    {
        var source = new StubSnapshotSource
        {
            ProgramState = "Entry",
            RootType = "Root_Entry",
            Windows = new[] { new GatewayWindowSnapshot("window-3", "Page_ModsConfig", true) },
            Selection = new[] { new GatewaySelectionSnapshot("thing-5", "Cookware") }
        };
        var provider = new RimWorldGatewayStateProvider(source, new GatewayDispatcher(), processId: 7);

        var json = Encoding.UTF8.GetString(GatewayJsonWriter.Write(provider.CaptureUiState()));

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("\"window-3\""));
            Assert.That(json, Does.Contain("\"Page_ModsConfig\""));
            Assert.That(json, Does.Contain("\"thing-5\""));
            Assert.That(json, Does.Contain("\"Cookware\""));
            Assert.That(json, Does.Not.Contain(nameof(StubSnapshotSource)));
        });
    }

    private sealed class StubSnapshotSource : IGatewaySnapshotSource
    {
        public string ProgramState { get; set; } = string.Empty;

        public string RootType { get; set; } = string.Empty;

        public long? Tick { get; set; }

        public GatewayMapSnapshot? Map { get; set; }

        public IReadOnlyList<GatewayWindowSnapshot> Windows { get; set; } = new GatewayWindowSnapshot[0];

        public IReadOnlyList<GatewaySelectionSnapshot> Selection { get; set; } = new GatewaySelectionSnapshot[0];
    }
}
