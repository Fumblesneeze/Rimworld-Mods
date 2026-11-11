using System;

namespace RimWorldDevGateway;

// Keeps native dialog identity separate from reusable UI-state index/type labels.
internal sealed class GatewayNativeWindowLease
{
    private readonly object window;
    private readonly object stack;

    public GatewayNativeWindowLease(object window, object stack)
    {
        this.window = window ?? throw new ArgumentNullException(nameof(window));
        this.stack = stack ?? throw new ArgumentNullException(nameof(stack));
    }

    public string? Validate(object currentWindow, object currentStack)
    {
        if (!ReferenceEquals(window, currentWindow)) return "stale_window_instance";
        return ReferenceEquals(stack, currentStack) ? null : "stale_window_stack";
    }
}
