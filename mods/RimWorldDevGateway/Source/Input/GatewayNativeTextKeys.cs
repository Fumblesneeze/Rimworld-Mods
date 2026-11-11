namespace RimWorldDevGateway;

// Ownership for only the fixed Ctrl/A/Backspace replacement sequence. Native key identifiers
// stay in the adapter; this receipt has no input API and is safe to exercise in the unit tier.
internal sealed class GatewayNativeTextKeys
{
    private readonly Dictionary<int, GatewayNativeInputEdgeReceipt> edges = new();
    private readonly HashSet<int> held = new();
    private bool revoked;
    public bool IsHeld(int key) => held.Contains(key);
    public void BeforePress(int key)
    {
        if (revoked) throw new InvalidOperationException("Text key ownership was revoked.");
        Receipt(key); held.Add(key);
    }
    public void Record(int key, int generation, bool pressed)
    {
        if (revoked) throw new InvalidOperationException("Text key ownership was revoked during input.");
        Receipt(key).Record(generation, pressed);
    }
    public void Released(int key) => held.Remove(key);
    public bool HasForeignEdges(int key, int generation, bool down, bool up, bool repeat)
        => revoked || (edges.TryGetValue(key, out var receipt) ? receipt.HasForeignEdges(generation, down, up, repeat) : down || up || repeat);
    public void Invalidate()
    {
        revoked = true;
        foreach (var receipt in edges.Values) receipt.Invalidate();
    }
    private GatewayNativeInputEdgeReceipt Receipt(int key)
    {
        if (edges.TryGetValue(key, out var receipt)) return receipt;
        if (edges.Count == 3) throw new InvalidOperationException("Text replacement owns at most three keys.");
        edges.Add(key, receipt = new()); return receipt;
    }
    public void Cleanup(Action<int> releaseOwnedKey, Action cleanCharacter)
    {
        List<Exception>? failures = null;
        foreach (int key in held.ToArray())
        {
            try { releaseOwnedKey(key); }
            catch (Exception exception) { (failures ??= new()).Add(exception); }
        }
        try { cleanCharacter(); }
        catch (Exception exception) { (failures ??= new()).Add(exception); }
        if (failures != null) throw new AggregateException("Native text input cleanup was incomplete.", failures);
    }
}
