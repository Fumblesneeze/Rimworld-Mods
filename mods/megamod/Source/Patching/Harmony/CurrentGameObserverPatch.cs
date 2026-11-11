using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Megamod.Patching;

// One serialized startup observer. Its callback reads the getter, never the requested setter value.
public sealed class CurrentGameObserverPatch
{
    private static readonly Dictionary<MethodBase, CurrentGameObserverPatch> Observers = new();
    private readonly Type currentType;
    private readonly Type gameType;
    private readonly Action<object?> observe;
    private readonly Action<Exception>? observationFailed;
    private MethodInfo? getter;
    private MethodInfo? setter;
    private OwnedHarmonyPatch? ownedPatch;
    private bool installed;
    private bool enabled;

    public CurrentGameObserverPatch(Type currentType, Type gameType, Action<object?> observe,
        Action<Exception>? observationFailed = null)
    {
        this.currentType = currentType;
        this.gameType = gameType;
        this.observe = observe;
        this.observationFailed = observationFailed;
        Candidate = new PatchCandidate("SETTINGS-GAME-CONTEXT", "SETTINGS-GAME-CONTEXT",
            currentType.FullName + ".set_Game", "Current-save settings and dependent features",
            "Global settings remain available; save overrides are retained.",
            Validate, Install, Rollback);
    }

    public PatchCandidate Candidate { get; }
    public Exception? Failure { get; private set; }
    public void Enable()
    {
        if (!installed || Failure != null) throw new InvalidOperationException("The game observer is unavailable.");
        enabled = true;
    }

    public void ObserveCurrent()
    {
        if (!enabled) return;
        try { observe(getter!.Invoke(null, null)); }
        catch (Exception exception)
        {
            enabled = false;
            Failure = exception.GetBaseException();
            try { observationFailed?.Invoke(Failure); }
            catch (Exception failureNotification)
            {
                Failure = new AggregateException(Failure, failureNotification);
            }
        }
    }

    private string? Validate()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
        getter = currentType.GetMethod("get_Game", flags, null, Type.EmptyTypes, null);
        setter = currentType.GetMethod("set_Game", flags, null, new[] { gameType }, null);
        if (getter == null || setter == null) return "The required public static game accessors are missing.";
        if (getter.ReturnType != gameType || setter.ReturnType != typeof(void) ||
            getter.ContainsGenericParameters || setter.ContainsGenericParameters ||
            getter.IsGenericMethod || setter.IsGenericMethod ||
            getter.GetParameters().Length != 0 || setter.GetParameters().Length != 1 ||
            setter.GetParameters()[0].ParameterType != gameType)
            return "The required game accessor return or generic shape has changed.";
        ownedPatch ??= new OwnedHarmonyPatch(setter, HarmonyPatchType.Postfix,
            typeof(CurrentGameObserverPatch).GetMethod(nameof(Postfix), BindingFlags.NonPublic | BindingFlags.Static)!);
        return ownedPatch.ValidateOwnership();
    }

    private void Install()
    {
        if (ownedPatch == null || setter == null) throw new InvalidOperationException("The game observer was not validated.");
        Observers.Add(setter, this);
        ownedPatch.Install();
        installed = true;
    }

    private void Rollback()
    {
        enabled = false;
        installed = false;
        try { ownedPatch?.Rollback(); }
        finally
        {
            if (setter != null && Observers.TryGetValue(setter, out var owner) && ReferenceEquals(owner, this))
                Observers.Remove(setter);
        }
    }

    private static void Postfix(MethodBase __originalMethod)
    {
        if (Observers.TryGetValue(__originalMethod, out var observer)) observer.ObserveCurrent();
    }
}
