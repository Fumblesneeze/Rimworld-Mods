using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class VerseGatewayEndToEndArchitectCategoryActionsTests
{
    [Test]
    public void Open_uses_native_tab_activation_then_the_exact_loaded_category()
    {
        var runtime = new RecordingRuntime();

        var outcome = VerseGatewayEndToEndArchitectCategoryActions.Apply(
            new ArchitectCategoryActionStep("open", "Production", open: true),
            runtime);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.True);
            Assert.That(runtime.Calls, Is.EqualTo(new[] { "exists:Production", "open", "select:Production" }));
            Assert.That(runtime.IsArchitectOpen, Is.True);
            Assert.That(runtime.SelectedCategory, Is.EqualTo("Production"));
        });
    }

    [Test]
    public void Missing_player_control_or_category_fails_before_mutating_the_tab()
    {
        var noControl = new RecordingRuntime { PlayerHasControl = false };
        var missing = new RecordingRuntime { CategoryExists = false };
        var step = new ArchitectCategoryActionStep("open", "Production", open: true);

        var noControlOutcome = VerseGatewayEndToEndArchitectCategoryActions.Apply(step, noControl);
        var missingOutcome = VerseGatewayEndToEndArchitectCategoryActions.Apply(step, missing);

        Assert.Multiple(() =>
        {
            Assert.That(noControlOutcome.FailureCode, Is.EqualTo("architect_player_control_required"));
            Assert.That(noControl.Calls, Is.Empty);
            Assert.That(missingOutcome.FailureCode, Is.EqualTo("architect_category_missing"));
            Assert.That(missing.Calls, Is.EqualTo(new[] { "exists:Production" }));
        });
    }

    [Test]
    public void Close_uses_the_native_current_tab_escape_path_and_confirms_closure()
    {
        var runtime = new RecordingRuntime { IsArchitectOpen = true };

        var outcome = VerseGatewayEndToEndArchitectCategoryActions.Apply(
            new ArchitectCategoryActionStep("close", "Production", open: false),
            runtime);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.True);
            Assert.That(runtime.Calls, Is.EqualTo(new[] { "exists:Production", "close" }));
            Assert.That(runtime.IsArchitectOpen, Is.False);
        });
    }

    [Test]
    public void Close_fails_before_mutation_when_the_exact_category_tab_is_missing()
    {
        var runtime = new RecordingRuntime
        {
            IsArchitectOpen = true,
            CategoryExists = false
        };

        var outcome = VerseGatewayEndToEndArchitectCategoryActions.Apply(
            new ArchitectCategoryActionStep("close", "Production", open: false),
            runtime);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.FailureCode, Is.EqualTo("architect_category_missing"));
            Assert.That(runtime.Calls, Is.EqualTo(new[] { "exists:Production" }));
            Assert.That(runtime.IsArchitectOpen, Is.True);
        });
    }

    [Test]
    public void Open_fails_when_the_category_click_leaves_architect_inactive()
    {
        var runtime = new RecordingRuntime { CloseArchitectAfterSelection = true };

        var outcome = VerseGatewayEndToEndArchitectCategoryActions.Apply(
            new ArchitectCategoryActionStep("open", "Production", open: true),
            runtime);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.FailureCode, Is.EqualTo("architect_category_selection_failed"));
            Assert.That(runtime.Calls, Is.EqualTo(new[] { "exists:Production", "open", "select:Production" }));
            Assert.That(runtime.IsArchitectOpen, Is.False);
        });
    }

    private sealed class RecordingRuntime : IGatewayEndToEndArchitectCategoryRuntime
    {
        public bool PlayerHasControl { get; set; } = true;

        public bool IsArchitectOpen { get; set; }

        public bool CategoryExists { get; set; } = true;

        public bool CloseArchitectAfterSelection { get; set; }

        public string? SelectedCategory { get; private set; }

        public List<string> Calls { get; } = new();

        public bool HasCategory(string categoryDefName)
        {
            Calls.Add("exists:" + categoryDefName);
            return CategoryExists;
        }

        public void OpenArchitect()
        {
            Calls.Add("open");
            IsArchitectOpen = true;
        }

        public bool SelectCategory(string categoryDefName)
        {
            Calls.Add("select:" + categoryDefName);
            SelectedCategory = categoryDefName;
            if (CloseArchitectAfterSelection)
            {
                IsArchitectOpen = false;
            }

            return true;
        }

        public void CloseArchitect()
        {
            Calls.Add("close");
            IsArchitectOpen = false;
        }
    }
}
