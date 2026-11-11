namespace RimWorldDevGateway;

// Pure receipt for one native layout/key boundary. The provider supplies the actual Event
// reference and values before other prefixes, then rechecks them at the terminal prefix.
internal sealed class GatewayNativeTextPassGate
{
    private object? input;
    private bool drawing, capturedLayout, capturedKey, layoutCompleted, admitted;
    private int type, raw, key, modifiers;
    private char character;
    public string? Rejection { get; private set; }
    public void Capture(object identity, bool drawing, bool layout, bool keyEvent, int type, int raw, int key, char character, int modifiers)
    {
        input = identity; this.drawing = drawing; capturedLayout = layout; capturedKey = keyEvent;
        this.type = type; this.raw = raw; this.key = key; this.character = character; this.modifiers = modifiers;
        admitted = false;
        if (layout) layoutCompleted = false;
    }
    public bool Before(object? identity, bool drawing, bool keyEvent, int type, int raw, int key, char character, int modifiers,
        bool original, bool authority, Func<string?> validate)
    {
        if (capturedLayout)
        {
            if (!ReferenceEquals(input, identity) || keyEvent || drawing != this.drawing || drawing ||
                type != this.type || raw != this.raw || key != this.key || character != this.character || modifiers != this.modifiers)
                Rejection ??= "native_text_pass_changed";
            else if (!original || !authority) Rejection ??= "native_input_not_dispatched";
            admitted = Rejection == null;
            return admitted;
        }
        if (!capturedKey && !keyEvent) return true;
        if (!capturedKey || !keyEvent || !ReferenceEquals(input, identity) || drawing != this.drawing || drawing ||
            type != this.type || raw != this.raw || key != this.key || character != this.character || modifiers != this.modifiers || !layoutCompleted)
            Rejection ??= "native_text_pass_changed";
        else if (!original || !authority) Rejection ??= "native_input_not_dispatched";
        else Rejection ??= validate();
        layoutCompleted = false;
        admitted = Rejection == null;
        return admitted;
    }
    public bool After(bool original)
    {
        if (capturedLayout) layoutCompleted = admitted && original;
        bool delivered = capturedKey && admitted && original;
        input = null; capturedLayout = capturedKey = admitted = false;
        return delivered;
    }
}
