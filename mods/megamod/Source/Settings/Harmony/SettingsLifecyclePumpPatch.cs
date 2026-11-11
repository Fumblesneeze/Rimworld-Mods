using Megamod.Patching;
using System.Reflection;
using HarmonyLib;

namespace Megamod.Settings;

public sealed class SettingsLifecyclePumpPatch
{
    private static readonly Dictionary<MethodBase, SettingsLifecyclePumpPatch> Pumps = new();
    private readonly Type targetType;
    private readonly Action pump;
    private readonly Action<Exception> failed;
    private MethodInfo? target;
    private OwnedHarmonyPatch? owned;
    private bool installed;
    private bool enabled;
    public Exception? Failure { get; private set; }

    public SettingsLifecyclePumpPatch(Type targetType, Action pump, Action<Exception> failed)
    {
        this.targetType = targetType;
        this.pump = pump;
        this.failed = failed;
        Candidate = new PatchCandidate("SETTINGS-LIFECYCLE-PUMP", "SETTINGS-LIFECYCLE-PUMP",
            targetType.FullName + ".LongEventsUpdate", "Current-save settings lifecycle",
            "Save controls remain unavailable; global defaults remain available.", Validate, Install, Rollback);
    }
    public PatchCandidate Candidate { get; }
    public void Enable()
    {
        if (!installed || Failure != null) throw new InvalidOperationException("The settings lifecycle pump is unavailable.");
        enabled = true;
    }

    private string? Validate()
    {
        target = targetType.GetMethod("LongEventsUpdate", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
            null, new[] { typeof(bool).MakeByRefType() }, null);
        if (target == null) return "The public static LongEventsUpdate(out bool) target is missing.";
        if (target.ReturnType != typeof(void) || target.IsGenericMethod || target.ContainsGenericParameters ||
            target.GetParameters().Length != 1 || !target.GetParameters()[0].IsOut || target.GetParameters()[0].IsIn)
            return "The required non-generic void/out-bool callable shape changed.";
        owned ??= new OwnedHarmonyPatch(target, HarmonyPatchType.Postfix,
            typeof(SettingsLifecyclePumpPatch).GetMethod(nameof(Postfix), BindingFlags.NonPublic | BindingFlags.Static)!);
        return owned.ValidateOwnership();
    }

    private void Install()
    {
        if (target == null || owned == null) throw new InvalidOperationException("The lifecycle pump was not validated.");
        Pumps.Add(target, this);
        owned.Install();
        installed = true;
    }

    private void Rollback()
    {
        enabled = false;
        installed = false;
        try { owned?.Rollback(); }
        finally
        {
            if (target != null && Pumps.TryGetValue(target, out var owner) && ReferenceEquals(owner, this)) Pumps.Remove(target);
        }
    }

    private static void Postfix(MethodBase __originalMethod)
    {
        if (!Pumps.TryGetValue(__originalMethod, out var owner) || !owner.enabled) return;
        try { owner.pump(); }
        catch (Exception exception)
        {
            owner.enabled = false;
            owner.Failure = exception.GetBaseException();
            try { owner.failed(owner.Failure); }
            catch (Exception notificationFailure) { owner.Failure = new AggregateException(owner.Failure, notificationFailure); }
        }
    }
}
