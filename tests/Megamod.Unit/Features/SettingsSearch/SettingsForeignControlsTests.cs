using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch.Core;
using NUnit.Framework;

namespace Megamod.Unit.Features.SettingsSearch;

[TestFixture]
public sealed class SettingsForeignControlsTests
{
    private static readonly SettingsPage Page = new("page", "open", "Foreign", "Foreign", "Author", "foreign");
    private static SettingsCaptureSnapshot Capture(params SettingsCaptureRecord[] records) =>
        new(Page, "root", records, SettingsCaptureStatus.Partial, SettingsCaptureFailure.None);

    [Test]
    public void ActualWidgetsRetainHelpAndDisplayedValueWhileStandaloneTextIsNotAnOption()
    {
        var bounds = new SettingsCaptureRect(10, 40, 300, 30);
        var capture = Capture(new(0, SettingsCaptureKind.Text, "Section", new(10, 0, 300, 30)),
            new(1, SettingsCaptureKind.Text, "Enabled", bounds),
            new(2, SettingsCaptureKind.Tooltip, "Help from the real tooltip", bounds));
        var options = SettingsForeignControls.Project(capture,
            new[] { new SettingsForeignWidget(SettingsForeignControlKind.Checkbox, "Enabled", "On", bounds, new[] { 1, 3 }) },
            Array.Empty<SettingsForeignTab>());
        Assert.That(options, Has.Count.EqualTo(1));
        Assert.That(options[0].Entry.Help, Is.EqualTo("Help from the real tooltip"));
        Assert.That(options[0].Entry.DisplayedValue, Is.EqualTo("On"));
        Assert.That(options[0].Scrolls, Is.EqualTo(new[] { 1, 3 }));
    }

    [Test]
    public void CurrentLayoutMayMoveButAnIdenticalSecondControlMakesRevealAmbiguous()
    {
        var before = SettingsForeignControls.Project(Capture(), new[] {
            new SettingsForeignWidget(SettingsForeignControlKind.Checkbox, "Enabled", "On", new(0, 600, 300, 30), new[] { 0 }) },
            Array.Empty<SettingsForeignTab>()).Single();
        var current = SettingsForeignControls.Project(Capture(), new[] {
            new SettingsForeignWidget(SettingsForeignControlKind.Checkbox, "Enabled", "Off", new(0, 90, 300, 30), new[] { 2 }) },
            Array.Empty<SettingsForeignTab>());
        Assert.That(SettingsForeignControls.Resolve(before.Target, Page, current, out var failure), Is.SameAs(current[0]));
        Assert.That(failure, Is.Null);
        Assert.That(SettingsForeignControls.Resolve(before.Target, Page, new[] { current[0], current[0] }, out failure), Is.Null);
        Assert.That(failure, Is.EqualTo("settings-foreign-control-ambiguous"));
    }

    [Test]
    public void EqualPageMetadataAndEqualTabLabelsDoNotConflateOriginalIdentities()
    {
        var widget = new SettingsForeignWidget(SettingsForeignControlKind.Checkbox, "Enabled", "On", new(0, 0, 300, 30), Array.Empty<int>());
        var first = SettingsForeignControls.Project(Capture(), new[] { widget }, new[] { new SettingsForeignTab("first", "Options") }).Single();
        var second = SettingsForeignControls.Project(Capture(), new[] { widget }, new[] { new SettingsForeignTab("second", "Options") });
        Assert.That(SettingsForeignControls.Resolve(first.Target, Page, second, out _), Is.Null);
        Assert.That(SettingsForeignControls.Resolve(first.Target, Page with { }, new[] { first }, out _), Is.Null);
    }

    [Test]
    public void AnUnlabeledEditorNeedsOneUnambiguousAdjacentLabel()
    {
        var widget = new SettingsForeignWidget(SettingsForeignControlKind.Text, null, "12", new(150, 0, 140, 30), Array.Empty<int>());
        var label = new SettingsCaptureRecord(0, SettingsCaptureKind.Text, "Maximum", new(0, 0, 140, 30));
        var options = SettingsForeignControls.Project(Capture(label), new[] { widget }, Array.Empty<SettingsForeignTab>());
        Assert.That(options.Single().Entry.Label, Is.EqualTo("Maximum"));
        Assert.That(SettingsForeignControls.Project(Capture(label, label with { Text = "Other" }), new[] { widget },
            Array.Empty<SettingsForeignTab>()), Is.Empty);
    }

    [Test]
    public void ControlsBeforeTheTabBarKeepRootIdentityAndControlsAfterItKeepTheirSelectedPath()
    {
        var root = new SettingsForeignWidget(SettingsForeignControlKind.Checkbox, "Common", "On", new(0, 0, 300, 30),
            Array.Empty<int>(), Array.Empty<SettingsForeignTab>());
        var local = root with { Label = "Local", Bounds = new(0, 100, 300, 30),
            Tabs = new[] { new SettingsForeignTab("first", "Amber") } };
        var options = SettingsForeignControls.Project(Capture(), new[] { root, local }, new[] { new SettingsForeignTab("first", "Amber") });
        Assert.That(options[0].Target.Tabs, Is.Empty);
        Assert.That(options[0].Entry.PathIdentity, Is.EqualTo("foreign:"));
        Assert.That(options[1].Entry.Tab, Is.EqualTo("Amber"));
    }

    [Test]
    public void FailedDrawCannotPublishControlsAndPathEncodingCannotCollideOnSeparators()
    {
        var widget = new SettingsForeignWidget(SettingsForeignControlKind.Checkbox, "Enabled", "On", new(0, 0, 300, 30), Array.Empty<int>());
        Assert.That(SettingsForeignControls.Project(Capture() with { Status = SettingsCaptureStatus.Failed }, new[] { widget },
            Array.Empty<SettingsForeignTab>()), Is.Empty);
        Assert.That(SettingsForeignControls.PathIdentity(new[] { new SettingsForeignTab("a:b", "c") }),
            Is.Not.EqualTo(SettingsForeignControls.PathIdentity(new[] { new SettingsForeignTab("a", "b:c") })));
    }

    [Test]
    public void RepeatedRowsShareOneGeneratedPathInsteadOfMultiplyingLargeTabNames()
    {
        var tabs = new[] { new SettingsForeignTab(new string('g', 1000), new string('t', 1000)) };
        var widgets = Enumerable.Range(0, 100).Select(i => new SettingsForeignWidget(SettingsForeignControlKind.Checkbox,
            "row" + i, "On", new(0, i * 30, 300, 30), Array.Empty<int>(), tabs.ToArray())).ToArray();
        var projection = SettingsForeignControls.ProjectBounded(Capture(), widgets, Array.Empty<SettingsForeignTab>(), 7000);
        Assert.That(projection.Truncated, Is.False);
        Assert.That(projection.Options, Has.Count.EqualTo(100));
        Assert.That(projection.Options[99].Target.Tabs, Is.SameAs(projection.Options[0].Target.Tabs));
        Assert.That(projection.Options[99].Entry.PathIdentity, Is.SameAs(projection.Options[0].Entry.PathIdentity));
        Assert.That(projection.Options[99].Entry.Tab, Is.SameAs(projection.Options[0].Entry.Tab));
    }

    [Test]
    public void RepeatedOverlappingTooltipsStopAtABoundedPrefixBeforeJoiningMoreHelp()
    {
        var first = new SettingsForeignWidget(SettingsForeignControlKind.Checkbox, "First", "On", new(0, 0, 300, 30), Array.Empty<int>());
        var second = first with { Label = "Second", Bounds = new(0, 30, 300, 30) };
        var help = new SettingsCaptureRecord(0, SettingsCaptureKind.Tooltip, new string('h', 100), new(0, 0, 300, 60));
        var projection = SettingsForeignControls.ProjectBounded(Capture(help), new[] { first, second }, Array.Empty<SettingsForeignTab>(), 180);
        Assert.That(projection.Truncated, Is.True);
        Assert.That(projection.Options, Has.Count.EqualTo(1));
        Assert.That(projection.Options[0].Entry.Help, Is.EqualTo(help.Text));
    }

    [Test]
    public void ExcessiveGeneratedPathIsTruncatedWhileUnknownNavigationKeepsSearchableControls()
    {
        var root = new SettingsForeignWidget(SettingsForeignControlKind.Checkbox, "Root", "On", new(0, 0, 300, 30),
            Array.Empty<int>(), Array.Empty<SettingsForeignTab>());
        var later = root with { Label = "Later", Tabs = new[] { new SettingsForeignTab(new string('g', 200), "Tab") } };
        var projection = SettingsForeignControls.ProjectBounded(Capture(), new[] { root, later }, Array.Empty<SettingsForeignTab>(), 200);
        Assert.That(projection.Truncated, Is.True);
        Assert.That(projection.Options.Select(option => option.Entry.Label), Is.EqualTo(new[] { "Root" }));
        var unavailable = SettingsForeignControls.Project(Capture(), new[] { root with { CanReveal = false } }, Array.Empty<SettingsForeignTab>()).Single();
        Assert.That(unavailable.Entry.Label, Is.EqualTo("Root"));
        Assert.That(SettingsForeignControls.Resolve(unavailable.Target, Page, new[] { unavailable }, out var reason), Is.Null);
        Assert.That(reason, Is.EqualTo("settings-foreign-tab-navigation-incomplete"));
    }
}
