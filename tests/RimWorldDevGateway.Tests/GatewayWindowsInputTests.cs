using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayWindowsInputTests
{
    [Test]
    public void Click_targets_current_process_client_coordinates_and_emits_one_pair()
    {
        var platform = FakePlatform.Ready();
        var input = new GatewayWindowsInput(platform, processId: 77);

        var result = input.Click(new GatewayClientPoint(20, 30), GatewayMouseButton.Left);

        Assert.Multiple(() =>
        {
            Assert.That(result.Operation, Is.EqualTo("click"));
            Assert.That(result.WindowHandle, Is.EqualTo(42));
            Assert.That(result.Events.Select(entry => entry.Kind),
                Is.EqualTo(new[] { "mouse_move", "mouse_left_down", "mouse_left_up" }));
            Assert.That(result.Events[0].ClientX, Is.EqualTo(20));
            Assert.That(result.Events[0].ClientY, Is.EqualTo(30));
            Assert.That(result.Events[0].ScreenX, Is.EqualTo(120));
            Assert.That(result.Events[0].ScreenY, Is.EqualTo(230));
            Assert.That(platform.Injected,
                Is.EqualTo(new[] { "move:120,230", "mouse:Left:down", "mouse:Left:up" }));
        });
    }

    [Test]
    public void Drag_interpolates_bounded_steps_and_releases_after_focus_loss()
    {
        var platform = FakePlatform.Ready();
        platform.LoseFocusAfterInjectionCount = 2;
        var input = new GatewayWindowsInput(platform, processId: 77);

        var exception = Assert.Throws<GatewayInputException>(() => input.Drag(
            new GatewayClientPoint(10, 10),
            new GatewayClientPoint(40, 40),
            GatewayMouseButton.Left,
            TimeSpan.FromMilliseconds(30),
            steps: 3));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo("focus_lost"));
            Assert.That(platform.Injected,
                Is.EqualTo(new[]
                {
                    "move:110,210",
                    "mouse:Left:down",
                    "delay:10",
                    "mouse:Left:up"
                }));
            Assert.That(platform.MainWindowReads, Is.GreaterThanOrEqualTo(3));
            Assert.That(platform.OwnerReads, Is.GreaterThanOrEqualTo(3));
            Assert.That(platform.ForegroundReads, Is.GreaterThanOrEqualTo(3));
        });
    }

    [Test]
    public void Drag_success_records_every_interpolated_move_and_timing_step()
    {
        var platform = FakePlatform.Ready();
        var input = new GatewayWindowsInput(platform, processId: 77);

        var result = input.Drag(
            new GatewayClientPoint(10, 10),
            new GatewayClientPoint(40, 40),
            GatewayMouseButton.Right,
            TimeSpan.FromMilliseconds(30),
            steps: 3);

        Assert.Multiple(() =>
        {
            Assert.That(result.Operation, Is.EqualTo("drag"));
            Assert.That(result.Events.Where(entry => entry.Kind == "mouse_move").Select(entry => entry.ClientX),
                Is.EqualTo(new int?[] { 10, 20, 30, 40 }));
            Assert.That(result.Events.Where(entry => entry.Kind == "delay").Select(entry => entry.DelayMilliseconds),
                Is.EqualTo(new int?[] { 10, 10, 10 }));
            Assert.That(platform.Injected.Last(), Is.EqualTo("mouse:Right:up"));
        });
    }

    [Test]
    public void Concurrent_input_waits_until_an_active_timed_drag_has_released_its_button()
    {
        var platform = FakePlatform.Ready();
        platform.BlockDelay = true;
        var input = new GatewayWindowsInput(platform, processId: 77);
        var drag = Task.Run(() => input.Drag(
            new GatewayClientPoint(10, 10),
            new GatewayClientPoint(40, 40),
            GatewayMouseButton.Left,
            TimeSpan.FromMilliseconds(30),
            steps: 1));
        Assert.That(platform.DelayStarted.Wait(TimeSpan.FromSeconds(1)), Is.True);

        var click = Task.Run(() => input.Click(
            new GatewayClientPoint(20, 20),
            GatewayMouseButton.Left));
        bool clickCompletedDuringDrag;
        try
        {
            clickCompletedDuringDrag = click.Wait(TimeSpan.FromMilliseconds(150));
        }
        finally
        {
            platform.ReleaseDelay.Set();
        }

        Task.WaitAll(drag, click);

        Assert.Multiple(() =>
        {
            Assert.That(clickCompletedDuringDrag, Is.False);
            Assert.That(platform.Injected, Is.EqualTo(new[]
            {
                "move:110,210",
                "mouse:Left:down",
                "delay:30",
                "move:140,240",
                "mouse:Left:up",
                "move:120,220",
                "mouse:Left:down",
                "mouse:Left:up"
            }));
        });
    }

    [Test]
    public void Cancelling_a_timed_drag_interrupts_its_delay_and_releases_the_mouse_button()
    {
        var platform = FakePlatform.Ready();
        platform.BlockDelay = true;
        var input = new GatewayWindowsInput(platform, processId: 77);
        using var cancellation = new CancellationTokenSource();
        var drag = Task.Run(() => input.Drag(
            new GatewayClientPoint(10, 10),
            new GatewayClientPoint(40, 40),
            GatewayMouseButton.Left,
            TimeSpan.FromSeconds(5),
            steps: 1,
            activate: true,
            cancellationToken: cancellation.Token));
        Assert.That(platform.DelayStarted.Wait(TimeSpan.FromSeconds(1)), Is.True);

        cancellation.Cancel();
        var completedPromptly = SpinWait.SpinUntil(() => drag.IsCompleted, TimeSpan.FromSeconds(1));
        try
        {
            Assert.Multiple(() =>
            {
                Assert.That(completedPromptly, Is.True);
                Assert.That(
                    () => drag.GetAwaiter().GetResult(),
                    Throws.TypeOf<OperationCanceledException>());
                Assert.That(platform.Injected, Is.EqualTo(new[]
                {
                    "move:110,210",
                    "mouse:Left:down",
                    "delay:5000",
                    "mouse:Left:up"
                }));
            });
        }
        finally
        {
            platform.ReleaseDelay.Set();
        }
    }

    [Test]
    public void Chord_validates_all_keys_then_releases_modifiers_in_reverse_order()
    {
        var platform = FakePlatform.Ready();
        var input = new GatewayWindowsInput(platform, processId: 77);

        var result = input.SendChord(new[] { "Control", "Shift" }, "S");

        Assert.Multiple(() =>
        {
            Assert.That(result.Operation, Is.EqualTo("keys"));
            Assert.That(platform.Injected, Is.EqualTo(new[]
            {
                "key:17:down",
                "key:16:down",
                "key:83:down",
                "key:83:up",
                "key:16:up",
                "key:17:up"
            }));
        });
    }

    [Test]
    public void Text_uses_paired_unicode_events_for_each_UTF16_character()
    {
        var platform = FakePlatform.Ready();
        var input = new GatewayWindowsInput(platform, processId: 77);

        var result = input.SendText("Hi");

        Assert.Multiple(() =>
        {
            Assert.That(result.Operation, Is.EqualTo("text"));
            Assert.That(platform.Injected, Is.EqualTo(new[]
            {
                "text:72:down", "text:72:up",
                "text:105:down", "text:105:up"
            }));
        });
    }

    [TestCase("unsupported", "input_backend_unavailable")]
    [TestCase("owner", "target_window_mismatch")]
    [TestCase("bounds", "point_out_of_bounds")]
    [TestCase("key", "unsupported_key")]
    public void Invalid_targets_and_arguments_fail_with_stable_codes_without_partial_input(
        string failure,
        string expectedCode)
    {
        var platform = FakePlatform.Ready();
        var input = new GatewayWindowsInput(platform, processId: 77);
        if (failure == "unsupported")
        {
            platform.IsSupported = false;
        }
        else if (failure == "owner")
        {
            platform.OwnerProcessId = 88;
        }

        TestDelegate action = failure switch
        {
            "bounds" => () => input.Click(new GatewayClientPoint(800, 10)),
            "key" => () => input.SendChord(new[] { "Control" }, "Not-A-Key"),
            _ => () => input.Click(new GatewayClientPoint(10, 10))
        };

        Assert.Multiple(() =>
        {
            Assert.That(action, Throws.TypeOf<GatewayInputException>()
                .With.Property(nameof(GatewayInputException.Code)).EqualTo(expectedCode));
            Assert.That(platform.Injected, Is.Empty);
        });
    }

    [Test]
    public void Changed_main_window_is_detected_before_the_next_injection_step()
    {
        var platform = FakePlatform.Ready();
        platform.ReplaceMainWindowAfterInjectionCount = 1;
        var input = new GatewayWindowsInput(platform, processId: 77);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => input.Click(new GatewayClientPoint(10, 10)),
                Throws.TypeOf<GatewayInputException>()
                    .With.Property(nameof(GatewayInputException.Code)).EqualTo("target_window_mismatch"));
            Assert.That(platform.Injected, Is.EqualTo(new[] { "move:110,210" }));
        });
    }

    [Test]
    public void Chord_focus_loss_releases_every_modifier_that_was_already_pressed()
    {
        var platform = FakePlatform.Ready();
        platform.LoseFocusAfterInjectionCount = 1;
        var input = new GatewayWindowsInput(platform, processId: 77);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => input.SendChord(new[] { "Control", "Shift" }, "S"),
                Throws.TypeOf<GatewayInputException>()
                    .With.Property(nameof(GatewayInputException.Code)).EqualTo("focus_lost"));
            Assert.That(platform.Injected,
                Is.EqualTo(new[] { "key:17:down", "key:17:up" }));
        });
    }

    [Test]
    public void Drag_and_text_limits_fail_before_targeting_or_injecting()
    {
        var platform = FakePlatform.Ready();
        var input = new GatewayWindowsInput(platform, processId: 77);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => input.Drag(
                    new GatewayClientPoint(0, 0),
                    new GatewayClientPoint(1, 1),
                    GatewayMouseButton.Left,
                    TimeSpan.Zero,
                    0),
                Throws.TypeOf<GatewayInputException>()
                    .With.Property(nameof(GatewayInputException.Code)).EqualTo("invalid_drag"));
            Assert.That(
                () => input.SendText(new string('x', GatewayWindowsInput.MaximumTextLength + 1)),
                Throws.TypeOf<GatewayInputException>()
                    .With.Property(nameof(GatewayInputException.Code)).EqualTo("input_too_large"));
            Assert.That(platform.MainWindowReads, Is.Zero);
            Assert.That(platform.Injected, Is.Empty);
        });
    }

    private sealed class FakePlatform : IGatewayWindowsInputPlatform
    {
        public bool IsSupported { get; set; }

        public IntPtr MainWindow { get; set; }

        public int OwnerProcessId { get; set; }

        public IntPtr ForegroundWindow { get; set; }

        public bool Minimized { get; set; }

        public GatewayNativeRect ClientRect { get; set; }

        public List<string> Injected { get; } = new();

        public int LoseFocusAfterInjectionCount { get; set; } = int.MaxValue;

        public int ReplaceMainWindowAfterInjectionCount { get; set; } = int.MaxValue;

        public int MainWindowReads { get; private set; }

        public int OwnerReads { get; private set; }

        public int ForegroundReads { get; private set; }

        public bool BlockDelay { get; set; }

        public ManualResetEventSlim DelayStarted { get; } = new(false);

        public ManualResetEventSlim ReleaseDelay { get; } = new(false);

        public static FakePlatform Ready() => new()
        {
            IsSupported = true,
            MainWindow = new IntPtr(42),
            OwnerProcessId = 77,
            ForegroundWindow = new IntPtr(42),
            ClientRect = new GatewayNativeRect(0, 0, 800, 600)
        };

        public IntPtr GetMainWindowHandle(int processId)
        {
            MainWindowReads++;
            return MainWindow;
        }

        public bool IsWindow(IntPtr window) => window == MainWindow;

        public int GetWindowProcessId(IntPtr window)
        {
            OwnerReads++;
            return OwnerProcessId;
        }

        public bool IsMinimized(IntPtr window) => Minimized;

        public bool RestoreWindow(IntPtr window)
        {
            Minimized = false;
            return true;
        }

        public bool SetForegroundWindow(IntPtr window)
        {
            ForegroundWindow = window;
            return true;
        }

        public IntPtr GetForegroundWindow()
        {
            ForegroundReads++;
            return ForegroundWindow;
        }

        public bool TryGetClientRect(IntPtr window, out GatewayNativeRect rectangle)
        {
            rectangle = ClientRect;
            return true;
        }

        public bool TryClientToScreen(IntPtr window, GatewayClientPoint client, out GatewayClientPoint screen)
        {
            screen = new GatewayClientPoint(client.X + 100, client.Y + 200);
            return true;
        }

        public bool SendMouseMove(int screenX, int screenY)
        {
            Injected.Add($"move:{screenX},{screenY}");
            LoseFocusIfRequested();
            return true;
        }

        public bool SendMouseButton(GatewayMouseButton button, bool down)
        {
            Injected.Add($"mouse:{button}:{(down ? "down" : "up")}");
            LoseFocusIfRequested();
            return true;
        }

        public bool SendVirtualKey(ushort virtualKey, bool down)
        {
            Injected.Add($"key:{virtualKey}:{(down ? "down" : "up")}");
            LoseFocusIfRequested();
            return true;
        }

        public bool SendUnicode(char character, bool down)
        {
            Injected.Add($"text:{(int)character}:{(down ? "down" : "up")}");
            LoseFocusIfRequested();
            return true;
        }

        public void Delay(TimeSpan duration, CancellationToken cancellationToken)
        {
            Injected.Add($"delay:{duration.TotalMilliseconds:0}");
            DelayStarted.Set();
            if (BlockDelay)
            {
                var outcome = WaitHandle.WaitAny(
                    new[] { ReleaseDelay.WaitHandle, cancellationToken.WaitHandle },
                    TimeSpan.FromSeconds(5));
                if (outcome == WaitHandle.WaitTimeout)
                {
                    throw new TimeoutException("The test did not release the blocked input delay.");
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        private void LoseFocusIfRequested()
        {
            if (Injected.Count(entry => !entry.StartsWith("delay:", StringComparison.Ordinal)) >= LoseFocusAfterInjectionCount)
            {
                ForegroundWindow = new IntPtr(99);
            }

            if (Injected.Count(entry => !entry.StartsWith("delay:", StringComparison.Ordinal)) >= ReplaceMainWindowAfterInjectionCount)
            {
                MainWindow = new IntPtr(100);
            }
        }
    }
}
