using Verse;

namespace ImmersiveChefs;

internal static class OptionalIntegrationDiagnostics
{
    private static readonly HashSet<OptionalIntegration> Warned = new();

    internal static void WarnOnce(OptionalIntegration integration, string detail)
    {
        lock (Warned)
        {
            if (!Warned.Add(integration))
            {
                return;
            }
        }

        Log.Warning(
            $"[ImmersiveChefs] {integration} integration disabled: {detail}. " +
            "Base Immersive Chefs behavior remains active; update the optional mod or turn its integration Off.");
    }
}
