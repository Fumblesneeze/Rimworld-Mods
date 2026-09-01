using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace RimWorldDevGateway;

public enum GatewayMouseButton
{
    Left,
    Right,
    Middle
}

public readonly struct GatewayClientPoint
{
    public GatewayClientPoint(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }
}

public readonly struct GatewayNativeRect
{
    public GatewayNativeRect(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public int Left { get; }

    public int Top { get; }

    public int Right { get; }

    public int Bottom { get; }

    public int Width => Right - Left;

    public int Height => Bottom - Top;

    public bool Contains(GatewayClientPoint point) =>
        point.X >= Left && point.X < Right && point.Y >= Top && point.Y < Bottom;
}

public sealed class GatewayInputEvent
{
    internal GatewayInputEvent(
        string kind,
        int? clientX = null,
        int? clientY = null,
        int? screenX = null,
        int? screenY = null,
        string? key = null,
        int? delayMilliseconds = null)
    {
        Kind = kind;
        ClientX = clientX;
        ClientY = clientY;
        ScreenX = screenX;
        ScreenY = screenY;
        Key = key;
        DelayMilliseconds = delayMilliseconds;
    }

    public string Kind { get; }

    public int? ClientX { get; }

    public int? ClientY { get; }

    public int? ScreenX { get; }

    public int? ScreenY { get; }

    public string? Key { get; }

    public int? DelayMilliseconds { get; }
}

public sealed class GatewayInputResult
{
    internal GatewayInputResult(string operation, IntPtr window, IList<GatewayInputEvent> events)
    {
        Operation = operation;
        WindowHandle = window.ToInt64();
        Events = new ReadOnlyCollection<GatewayInputEvent>(events.ToArray());
    }

    public string Operation { get; }

    public long WindowHandle { get; }

    public IReadOnlyList<GatewayInputEvent> Events { get; }
}

public sealed class GatewayInputException : Exception
{
    public GatewayInputException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public GatewayInputException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}

public interface IGatewayWindowsInputPlatform
{
    bool IsSupported { get; }

    IntPtr GetMainWindowHandle(int processId);

    bool IsWindow(IntPtr window);

    int GetWindowProcessId(IntPtr window);

    bool IsMinimized(IntPtr window);

    bool RestoreWindow(IntPtr window);

    IDisposable? TryAcquireForegroundWindow(IntPtr window);

    IntPtr GetForegroundWindow();

    bool TryGetClientRect(IntPtr window, out GatewayNativeRect rectangle);

    bool TryClientToScreen(IntPtr window, GatewayClientPoint client, out GatewayClientPoint screen);

    bool SendMouseMove(int screenX, int screenY);

    bool SendMouseButton(GatewayMouseButton button, bool down);

    bool SendVirtualKey(ushort virtualKey, bool down);

    bool SendUnicode(char character, bool down);

    void Delay(TimeSpan duration, CancellationToken cancellationToken);
}

public sealed class GatewayWindowsInput
{
    public const int MaximumDragSteps = 256;
    public const int MaximumDragDurationMilliseconds = 10_000;
    public const int MaximumTextLength = 4_096;

    private readonly IGatewayWindowsInputPlatform platform;
    private readonly int processId;
    private readonly SemaphoreSlim inputGate = new(1, 1);
    private static readonly IReadOnlyDictionary<string, ushort> KeyCodes = CreateKeyCodes();
    private static readonly HashSet<ushort> ModifierKeys = new() { 0x10, 0x11, 0x12, 0x5B, 0x5C };

    public GatewayWindowsInput()
        : this(new GatewayWin32InputPlatform(), Process.GetCurrentProcess().Id)
    {
    }

    public GatewayWindowsInput(IGatewayWindowsInputPlatform platform, int? processId = null)
    {
        this.platform = platform ?? throw new ArgumentNullException(nameof(platform));
        this.processId = processId ?? Process.GetCurrentProcess().Id;
        if (this.processId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(processId));
        }
    }

    public GatewayInputResult MayMaximizeWindowClick(
        GatewayClientPoint point,
        GatewayMouseButton button = GatewayMouseButton.Left,
        bool activate = true,
        CancellationToken cancellationToken = default)
    {
        return ExecuteSerialized(
            cancellationToken,
            () => ClickCore(point, button, activate, cancellationToken));
    }

    private GatewayInputResult ClickCore(
        GatewayClientPoint point,
        GatewayMouseButton button,
        bool activate,
        CancellationToken cancellationToken)
    {
        var events = new List<GatewayInputEvent>();
        for (var attempt = 0; attempt < 2; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var down = false;
            try
            {
                using var targetLease = AcquireTarget(activate);
                var target = targetLease.Window;
                cancellationToken.ThrowIfCancellationRequested();
                var screen = Move(target, point, events);
                cancellationToken.ThrowIfCancellationRequested();
                SendButton(target, button, true, point, screen, events);
                down = true;
                cancellationToken.ThrowIfCancellationRequested();
                SendButton(target, button, false, point, screen, events);
                down = false;
                return new GatewayInputResult("may-maximize-window-click", target, events);
            }
            catch (GatewayInputException exception) when (
                exception.Code == "focus_lost" && activate && !down && attempt == 0)
            {
                events.Add(new GatewayInputEvent("focus_reacquire"));
            }
            finally
            {
                if (down)
                {
                    TryEmergencyMouseUp(button);
                }
            }
        }

        throw Error("focus_lost", "The RimWorld window could not retain foreground focus for a click.");
    }

    public GatewayInputResult MayMaximizeWindowDrag(
        GatewayClientPoint start,
        GatewayClientPoint end,
        GatewayMouseButton button,
        TimeSpan duration,
        int steps,
        bool activate = true,
        CancellationToken cancellationToken = default)
    {
        return ExecuteSerialized(
            cancellationToken,
            () => DragCore(start, end, button, duration, steps, activate, cancellationToken));
    }

    private GatewayInputResult DragCore(
        GatewayClientPoint start,
        GatewayClientPoint end,
        GatewayMouseButton button,
        TimeSpan duration,
        int steps,
        bool activate,
        CancellationToken cancellationToken)
    {
        if (steps < 1 || steps > MaximumDragSteps ||
            duration < TimeSpan.Zero ||
            duration.TotalMilliseconds > MaximumDragDurationMilliseconds)
        {
            throw Error("invalid_drag", "Drag duration or interpolation steps are outside the supported bounds.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        using var targetLease = AcquireTarget(activate);
        var target = targetLease.Window;
        ResolveScreenPoint(target, start);
        ResolveScreenPoint(target, end);
        var events = new List<GatewayInputEvent>();
        cancellationToken.ThrowIfCancellationRequested();
        var screen = Move(target, start, events);
        var down = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            SendButton(target, button, true, start, screen, events);
            down = true;
            var totalMilliseconds = (int)Math.Round(duration.TotalMilliseconds, MidpointRounding.AwayFromZero);
            var baseDelay = totalMilliseconds / steps;
            var extraMilliseconds = totalMilliseconds % steps;
            for (var step = 1; step <= steps; step++)
            {
                var delayMilliseconds = baseDelay + (step <= extraMilliseconds ? 1 : 0);
                if (delayMilliseconds > 0)
                {
                    var delay = TimeSpan.FromMilliseconds(delayMilliseconds);
                    platform.Delay(delay, cancellationToken);
                    events.Add(new GatewayInputEvent("delay", delayMilliseconds: delayMilliseconds));
                }

                cancellationToken.ThrowIfCancellationRequested();
                var point = new GatewayClientPoint(
                    Interpolate(start.X, end.X, step, steps),
                    Interpolate(start.Y, end.Y, step, steps));
                screen = Move(target, point, events);
            }

            cancellationToken.ThrowIfCancellationRequested();
            SendButton(target, button, false, end, screen, events);
            down = false;
            return new GatewayInputResult("may-maximize-window-drag", target, events);
        }
        finally
        {
            if (down)
            {
                TryValidateForCleanup(target);
                TryEmergencyMouseUp(button);
            }
        }
    }

    public GatewayInputResult MayMaximizeWindowPressKey(
        string key,
        bool activate = true,
        CancellationToken cancellationToken = default)
    {
        return MayMaximizeWindowSendChord(
            Array.Empty<string>(),
            key,
            activate,
            cancellationToken);
    }

    public GatewayInputResult MayMaximizeWindowSendChord(
        IReadOnlyList<string> modifiers,
        string key,
        bool activate = true,
        CancellationToken cancellationToken = default)
    {
        return ExecuteSerialized(
            cancellationToken,
            () => SendChordCore(modifiers, key, activate, cancellationToken));
    }

    private GatewayInputResult SendChordCore(
        IReadOnlyList<string> modifiers,
        string key,
        bool activate,
        CancellationToken cancellationToken)
    {
        if (modifiers is null)
        {
            throw new ArgumentNullException(nameof(modifiers));
        }

        if (modifiers.Count > 4)
        {
            throw Error("unsupported_key", "A chord can contain at most four modifiers.");
        }

        var resolvedModifiers = new List<KeyValuePair<string, ushort>>(modifiers.Count);
        var distinctModifiers = new HashSet<ushort>();
        foreach (var modifier in modifiers)
        {
            var resolved = ResolveKey(modifier);
            if (!ModifierKeys.Contains(resolved.Value) || !distinctModifiers.Add(resolved.Value))
            {
                throw Error("unsupported_key", $"'{modifier}' is not a supported unique modifier key.");
            }

            resolvedModifiers.Add(resolved);
        }

        var primary = ResolveKey(key);
        cancellationToken.ThrowIfCancellationRequested();
        using var targetLease = AcquireTarget(activate);
        var target = targetLease.Window;
        var events = new List<GatewayInputEvent>();
        var pressedModifiers = new List<KeyValuePair<string, ushort>>();
        var primaryDown = false;
        try
        {
            foreach (var modifier in resolvedModifiers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SendKey(target, modifier, true, events);
                pressedModifiers.Add(modifier);
            }

            cancellationToken.ThrowIfCancellationRequested();
            SendKey(target, primary, true, events);
            primaryDown = true;
            cancellationToken.ThrowIfCancellationRequested();
            SendKey(target, primary, false, events);
            primaryDown = false;

            for (var index = pressedModifiers.Count - 1; index >= 0; index--)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SendKey(target, pressedModifiers[index], false, events);
                pressedModifiers.RemoveAt(index);
            }

            return new GatewayInputResult("may-maximize-window-keys", target, events);
        }
        finally
        {
            if (primaryDown)
            {
                TryValidateForCleanup(target);
                TryEmergencyKeyUp(primary.Value);
            }

            for (var index = pressedModifiers.Count - 1; index >= 0; index--)
            {
                TryValidateForCleanup(target);
                TryEmergencyKeyUp(pressedModifiers[index].Value);
            }
        }
    }

    public GatewayInputResult MayMaximizeWindowSendText(
        string text,
        bool activate = true,
        CancellationToken cancellationToken = default)
    {
        return ExecuteSerialized(
            cancellationToken,
            () => SendTextCore(text, activate, cancellationToken));
    }

    private GatewayInputResult SendTextCore(
        string text,
        bool activate,
        CancellationToken cancellationToken)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (text.Length > MaximumTextLength)
        {
            throw Error("input_too_large", $"Text input exceeds the {MaximumTextLength}-character limit.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        using var targetLease = AcquireTarget(activate);
        var target = targetLease.Window;
        var events = new List<GatewayInputEvent>();
        foreach (var character in text)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var down = false;
            try
            {
                SendUnicode(target, character, true, events);
                down = true;
                cancellationToken.ThrowIfCancellationRequested();
                SendUnicode(target, character, false, events);
                down = false;
            }
            finally
            {
                if (down)
                {
                    TryValidateForCleanup(target);
                    TryEmergencyUnicodeUp(character);
                }
            }
        }

        return new GatewayInputResult("may-maximize-window-keys", target, events);
    }

    private GatewayInputResult ExecuteSerialized(
        CancellationToken cancellationToken,
        Func<GatewayInputResult> operation)
    {
        inputGate.Wait(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return operation();
        }
        finally
        {
            inputGate.Release();
        }
    }

    private WindowInputLease AcquireTarget(bool activate)
    {
        EnsureSupported();
        var target = platform.GetMainWindowHandle(processId);
        if (target == IntPtr.Zero)
        {
            throw Error("target_window_missing", "The RimWorld process has no main window.");
        }

        ValidateOwnership(target);
        if (platform.IsMinimized(target) && (!activate || !platform.RestoreWindow(target)))
        {
            throw Error("focus_lost", "The RimWorld window could not be restored.");
        }

        IDisposable? foregroundLease = null;
        if (platform.GetForegroundWindow() != target)
        {
            if (!activate ||
                (foregroundLease = platform.TryAcquireForegroundWindow(target)) is null ||
                platform.GetForegroundWindow() != target)
            {
                foregroundLease?.Dispose();
                throw Error("focus_lost", "The RimWorld window could not be focused.");
            }
        }

        try
        {
            ValidateStep(target);
            return new WindowInputLease(target, foregroundLease);
        }
        catch
        {
            foregroundLease?.Dispose();
            throw;
        }
    }

    private sealed class WindowInputLease : IDisposable
    {
        private readonly IDisposable? foregroundLease;

        internal WindowInputLease(IntPtr window, IDisposable? foregroundLease)
        {
            Window = window;
            this.foregroundLease = foregroundLease;
        }

        internal IntPtr Window { get; }

        public void Dispose()
        {
            foregroundLease?.Dispose();
        }
    }

    private GatewayClientPoint Move(
        IntPtr target,
        GatewayClientPoint client,
        ICollection<GatewayInputEvent> events)
    {
        var screen = ResolveScreenPoint(target, client);
        if (!platform.SendMouseMove(screen.X, screen.Y))
        {
            throw Error("input_injection_failed", "Mouse movement injection failed.");
        }

        events.Add(new GatewayInputEvent("mouse_move", client.X, client.Y, screen.X, screen.Y));
        return screen;
    }

    private GatewayClientPoint ResolveScreenPoint(IntPtr target, GatewayClientPoint client)
    {
        ValidateStep(target);
        if (!platform.TryGetClientRect(target, out var rectangle))
        {
            throw Error("target_window_unavailable", "The RimWorld client rectangle is unavailable.");
        }

        if (!rectangle.Contains(client))
        {
            throw Error("point_out_of_bounds", "The requested point is outside the RimWorld client area.");
        }

        if (!platform.TryClientToScreen(target, client, out var screen))
        {
            throw Error("target_window_unavailable", "Client-to-screen coordinate conversion failed.");
        }

        return screen;
    }

    private void SendButton(
        IntPtr target,
        GatewayMouseButton button,
        bool down,
        GatewayClientPoint client,
        GatewayClientPoint screen,
        ICollection<GatewayInputEvent> events)
    {
        ValidateStep(target);
        if (!platform.SendMouseButton(button, down))
        {
            throw Error("input_injection_failed", "Mouse button injection failed.");
        }

        events.Add(new GatewayInputEvent(
            "mouse_" + button.ToString().ToLowerInvariant() + (down ? "_down" : "_up"),
            client.X,
            client.Y,
            screen.X,
            screen.Y));
    }

    private void ValidateStep(IntPtr target)
    {
        EnsureSupported();
        ValidateOwnership(target);
        if (platform.IsMinimized(target) || platform.GetForegroundWindow() != target)
        {
            throw Error("focus_lost", "The RimWorld window lost foreground focus.");
        }
    }

    private void SendKey(
        IntPtr target,
        KeyValuePair<string, ushort> key,
        bool down,
        ICollection<GatewayInputEvent> events)
    {
        ValidateStep(target);
        if (!platform.SendVirtualKey(key.Value, down))
        {
            throw Error("input_injection_failed", $"Key injection failed for '{key.Key}'.");
        }

        events.Add(new GatewayInputEvent(down ? "key_down" : "key_up", key: key.Key));
    }

    private void SendUnicode(
        IntPtr target,
        char character,
        bool down,
        ICollection<GatewayInputEvent> events)
    {
        ValidateStep(target);
        if (!platform.SendUnicode(character, down))
        {
            throw Error("input_injection_failed", "Unicode text injection failed.");
        }

        events.Add(new GatewayInputEvent(down ? "unicode_down" : "unicode_up", key: character.ToString()));
    }

    private void ValidateOwnership(IntPtr target)
    {
        if (!platform.IsWindow(target) ||
            platform.GetMainWindowHandle(processId) != target ||
            platform.GetWindowProcessId(target) != processId)
        {
            throw Error("target_window_mismatch", "The target window no longer belongs to this RimWorld process.");
        }
    }

    private void EnsureSupported()
    {
        if (!platform.IsSupported)
        {
            throw Error("input_backend_unavailable", "Windows SendInput is unavailable on this platform.");
        }
    }

    private void TryEmergencyMouseUp(GatewayMouseButton button)
    {
        try
        {
            platform.SendMouseButton(button, false);
        }
        catch
        {
            // Best-effort release prevents a logically stuck injected button.
        }
    }

    private void TryEmergencyKeyUp(ushort virtualKey)
    {
        try
        {
            platform.SendVirtualKey(virtualKey, false);
        }
        catch
        {
        }
    }

    private void TryEmergencyUnicodeUp(char character)
    {
        try
        {
            platform.SendUnicode(character, false);
        }
        catch
        {
        }
    }

    private void TryValidateForCleanup(IntPtr target)
    {
        try
        {
            ValidateStep(target);
        }
        catch
        {
            // Cleanup release is still attempted to avoid a globally stuck input state.
        }
    }

    private static int Interpolate(int start, int end, int step, int steps)
    {
        return start + (int)Math.Round(
            (end - start) * (step / (double)steps),
            MidpointRounding.AwayFromZero);
    }

    private static KeyValuePair<string, ushort> ResolveKey(string key)
    {
        if (key is null || !KeyCodes.TryGetValue(key.Trim(), out var virtualKey))
        {
            throw Error("unsupported_key", $"'{key}' is not a supported key name.");
        }

        return new KeyValuePair<string, ushort>(CanonicalKeyName(key.Trim()), virtualKey);
    }

    private static string CanonicalKeyName(string key)
    {
        return key.Length == 1 ? key.ToUpperInvariant() : key;
    }

    private static IReadOnlyDictionary<string, ushort> CreateKeyCodes()
    {
        var keys = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase)
        {
            ["Backspace"] = 0x08, ["Tab"] = 0x09, ["Enter"] = 0x0D,
            ["Shift"] = 0x10, ["Control"] = 0x11, ["Ctrl"] = 0x11, ["Alt"] = 0x12,
            ["Escape"] = 0x1B, ["Esc"] = 0x1B, ["Space"] = 0x20,
            ["PageUp"] = 0x21, ["PageDown"] = 0x22, ["End"] = 0x23, ["Home"] = 0x24,
            ["Left"] = 0x25, ["Up"] = 0x26, ["Right"] = 0x27, ["Down"] = 0x28,
            ["Insert"] = 0x2D, ["Delete"] = 0x2E,
            ["LeftWindows"] = 0x5B, ["RightWindows"] = 0x5C
        };
        for (var character = '0'; character <= '9'; character++)
        {
            keys[character.ToString()] = character;
        }

        for (var character = 'A'; character <= 'Z'; character++)
        {
            keys[character.ToString()] = character;
        }

        for (var index = 1; index <= 12; index++)
        {
            keys["F" + index] = (ushort)(0x6F + index);
        }

        return new ReadOnlyDictionary<string, ushort>(keys);
    }

    private static GatewayInputException Error(string code, string message) => new(code, message);
}

internal sealed class GatewayWin32InputPlatform : IGatewayWindowsInputPlatform
{
    private const int RestoreWindowCommand = 9;
    private const int VirtualScreenLeftMetric = 76;
    private const int VirtualScreenTopMetric = 77;
    private const int VirtualScreenWidthMetric = 78;
    private const int VirtualScreenHeightMetric = 79;
    private const uint MouseInputType = 0;
    private const uint KeyboardInputType = 1;
    private const uint MouseMove = 0x0001;
    private const uint MouseLeftDown = 0x0002;
    private const uint MouseLeftUp = 0x0004;
    private const uint MouseRightDown = 0x0008;
    private const uint MouseRightUp = 0x0010;
    private const uint MouseMiddleDown = 0x0020;
    private const uint MouseMiddleUp = 0x0040;
    private const uint MouseVirtualDesk = 0x4000;
    private const uint MouseAbsolute = 0x8000;
    private const uint KeyExtended = 0x0001;
    private const uint KeyUp = 0x0002;
    private const uint KeyUnicode = 0x0004;

    public bool IsSupported => Environment.OSVersion.Platform == PlatformID.Win32NT;

    public IntPtr GetMainWindowHandle(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            process.Refresh();
            return process.MainWindowHandle;
        }
        catch (ArgumentException)
        {
            return IntPtr.Zero;
        }
        catch (InvalidOperationException)
        {
            return IntPtr.Zero;
        }
    }

    public bool IsWindow(IntPtr window) => NativeMethods.IsWindow(window);

    public int GetWindowProcessId(IntPtr window)
    {
        NativeMethods.GetWindowThreadProcessId(window, out var processId);
        return unchecked((int)processId);
    }

    public bool IsMinimized(IntPtr window) => NativeMethods.IsIconic(window);

    public bool RestoreWindow(IntPtr window)
    {
        NativeMethods.ShowWindow(window, RestoreWindowCommand);
        return !NativeMethods.IsIconic(window);
    }

    public IDisposable? TryAcquireForegroundWindow(IntPtr window)
    {
        if (window == IntPtr.Zero || !NativeMethods.IsWindow(window))
        {
            return null;
        }

        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground == window)
        {
            return NoopDisposable.Instance;
        }

        var currentThread = NativeMethods.GetCurrentThreadId();
        var targetThread = NativeMethods.GetWindowThreadProcessId(window, out _);
        var foregroundThread = foreground == IntPtr.Zero
            ? 0U
            : NativeMethods.GetWindowThreadProcessId(foreground, out _);
        var attachedForeground = false;
        var attachedTarget = false;
        var handedOff = false;
        try
        {
            if (foregroundThread != 0 && foregroundThread != currentThread)
            {
                attachedForeground = NativeMethods.AttachThreadInput(currentThread, foregroundThread, true);
            }

            if (targetThread != 0 && targetThread != currentThread && targetThread != foregroundThread)
            {
                attachedTarget = NativeMethods.AttachThreadInput(currentThread, targetThread, true);
            }

            NativeMethods.BringWindowToTop(window);
            NativeMethods.SetForegroundWindow(window);
            NativeMethods.SetActiveWindow(window);
            NativeMethods.SetFocus(window);
            if (NativeMethods.GetForegroundWindow() != window)
            {
                return null;
            }

            handedOff = true;
            return new InputQueueLease(
                currentThread,
                foregroundThread,
                targetThread,
                attachedForeground,
                attachedTarget);
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            if (!handedOff && attachedTarget)
            {
                NativeMethods.AttachThreadInput(currentThread, targetThread, false);
            }

            if (!handedOff && attachedForeground)
            {
                NativeMethods.AttachThreadInput(currentThread, foregroundThread, false);
            }
        }
    }

    private sealed class InputQueueLease : IDisposable
    {
        private readonly uint currentThread;
        private readonly uint foregroundThread;
        private readonly uint targetThread;
        private readonly bool attachedForeground;
        private readonly bool attachedTarget;
        private int disposed;

        internal InputQueueLease(
            uint currentThread,
            uint foregroundThread,
            uint targetThread,
            bool attachedForeground,
            bool attachedTarget)
        {
            this.currentThread = currentThread;
            this.foregroundThread = foregroundThread;
            this.targetThread = targetThread;
            this.attachedForeground = attachedForeground;
            this.attachedTarget = attachedTarget;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
            {
                return;
            }

            if (attachedTarget)
            {
                NativeMethods.AttachThreadInput(currentThread, targetThread, false);
            }

            if (attachedForeground)
            {
                NativeMethods.AttachThreadInput(currentThread, foregroundThread, false);
            }
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        internal static readonly NoopDisposable Instance = new();

        public void Dispose()
        {
        }
    }

    public IntPtr GetForegroundWindow() => NativeMethods.GetForegroundWindow();

    public bool TryGetClientRect(IntPtr window, out GatewayNativeRect rectangle)
    {
        if (!NativeMethods.GetClientRect(window, out var native))
        {
            rectangle = default;
            return false;
        }

        rectangle = new GatewayNativeRect(native.Left, native.Top, native.Right, native.Bottom);
        return true;
    }

    public bool TryClientToScreen(IntPtr window, GatewayClientPoint client, out GatewayClientPoint screen)
    {
        var native = new NativePoint { X = client.X, Y = client.Y };
        if (!NativeMethods.ClientToScreen(window, ref native))
        {
            screen = default;
            return false;
        }

        screen = new GatewayClientPoint(native.X, native.Y);
        return true;
    }

    public bool SendMouseMove(int screenX, int screenY)
    {
        var left = NativeMethods.GetSystemMetrics(VirtualScreenLeftMetric);
        var top = NativeMethods.GetSystemMetrics(VirtualScreenTopMetric);
        var width = NativeMethods.GetSystemMetrics(VirtualScreenWidthMetric);
        var height = NativeMethods.GetSystemMetrics(VirtualScreenHeightMetric);
        if (width <= 1 || height <= 1)
        {
            return false;
        }

        var normalizedX = NormalizeAbsolute(screenX, left, width);
        var normalizedY = NormalizeAbsolute(screenY, top, height);
        return Send(new NativeInput
        {
            Type = MouseInputType,
            Value = new NativeInputValue
            {
                Mouse = new NativeMouseInput
                {
                    X = normalizedX,
                    Y = normalizedY,
                    Flags = MouseMove | MouseAbsolute | MouseVirtualDesk
                }
            }
        });
    }

    public bool SendMouseButton(GatewayMouseButton button, bool down)
    {
        var flags = button switch
        {
            GatewayMouseButton.Left => down ? MouseLeftDown : MouseLeftUp,
            GatewayMouseButton.Right => down ? MouseRightDown : MouseRightUp,
            GatewayMouseButton.Middle => down ? MouseMiddleDown : MouseMiddleUp,
            _ => 0U
        };
        return flags != 0 && Send(new NativeInput
        {
            Type = MouseInputType,
            Value = new NativeInputValue
            {
                Mouse = new NativeMouseInput { Flags = flags }
            }
        });
    }

    public bool SendVirtualKey(ushort virtualKey, bool down)
    {
        var flags = down ? 0U : KeyUp;
        if (IsExtendedKey(virtualKey))
        {
            flags |= KeyExtended;
        }

        return Send(new NativeInput
        {
            Type = KeyboardInputType,
            Value = new NativeInputValue
            {
                Keyboard = new NativeKeyboardInput
                {
                    VirtualKey = virtualKey,
                    Flags = flags
                }
            }
        });
    }

    public bool SendUnicode(char character, bool down)
    {
        return Send(new NativeInput
        {
            Type = KeyboardInputType,
            Value = new NativeInputValue
            {
                Keyboard = new NativeKeyboardInput
                {
                    ScanCode = character,
                    Flags = KeyUnicode | (down ? 0U : KeyUp)
                }
            }
        });
    }

    public void Delay(TimeSpan duration, CancellationToken cancellationToken)
    {
        if (duration > TimeSpan.Zero)
        {
            if (cancellationToken.WaitHandle.WaitOne(duration))
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }

    private static bool Send(NativeInput input)
    {
        return NativeMethods.SendInput(
            1,
            new[] { input },
            Marshal.SizeOf(typeof(NativeInput))) == 1;
    }

    private static int NormalizeAbsolute(int coordinate, int origin, int size)
    {
        var normalized = (long)(coordinate - origin) * 65_535L / (size - 1);
        return (int)Math.Max(0, Math.Min(65_535, normalized));
    }

    private static bool IsExtendedKey(ushort virtualKey)
    {
        return virtualKey is >= 0x21 and <= 0x2E or 0x5B or 0x5C;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeInput
    {
        public uint Type;
        public NativeInputValue Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct NativeInputValue
    {
        [FieldOffset(0)]
        public NativeMouseInput Mouse;

        [FieldOffset(0)]
        public NativeKeyboardInput Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMouseInput
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeKeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr window);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AttachThreadInput(uint attachThread, uint attachToThread, bool attach);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr window);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr window, int command);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr window);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BringWindowToTop(IntPtr window);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetActiveWindow(IntPtr window);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetFocus(IntPtr window);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetClientRect(IntPtr window, out NativeRect rectangle);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ClientToScreen(IntPtr window, ref NativePoint point);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int index);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint count, [In] NativeInput[] inputs, int size);
    }
}
