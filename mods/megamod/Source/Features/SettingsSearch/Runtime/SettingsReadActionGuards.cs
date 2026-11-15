using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Megamod.Features.SettingsSearch.Core;
using Megamod.Features.SettingsSearch.Harmony;
using Megamod.Patching;
using UnityEngine;
using Verse;

namespace Megamod.Features.SettingsSearch.Runtime;

// No production registration: the complete neutral reader owns initialization and entry.
public sealed class SettingsReadActionGuards : IDisposable
{
    private readonly Thread owner = Thread.CurrentThread;
    private readonly AdmissionCoordinator admission;
    private readonly List<SettingsReadActionGuard> guards = new();
    private readonly PatchGroup group;
    private bool initialized, disposed;

    public SettingsReadActionGuards(Action<string> warning)
    {
        admission = new AdmissionCoordinator(warning);
        group = new PatchGroup("settings-search.read-actions", AdmissionPhase.GameReady, new[]
        {
            Bind("SS-READ-WINDOW-ADD", typeof(WindowStack), "Add", false, typeof(void),
                "settings-read-window-add", typeof(Window)),
            Bind("SS-READ-IMMEDIATE-WINDOW", typeof(WindowStack), "ImmediateWindow", false, typeof(void),
                "settings-read-immediate-window", typeof(int), typeof(Rect), typeof(WindowLayer), typeof(Action),
                typeof(bool), typeof(bool), typeof(float), typeof(Action), typeof(bool)),
            Bind("SS-READ-WINDOW-REMOVE", typeof(WindowStack), "TryRemove", false, typeof(bool),
                "settings-read-window-remove", typeof(Window), typeof(bool)),
            Bind("SS-READ-SETTINGS-WRITE", typeof(LoadedModManager), "WriteModSettings", true, typeof(void),
                "settings-read-settings-write", typeof(string), typeof(string), typeof(ModSettings)),
            Bind("SS-READ-CLIPBOARD", typeof(GUIUtility), "set_systemCopyBuffer", true, typeof(void),
                "settings-read-clipboard", typeof(string)),
            Bind("SS-READ-IME", typeof(GUIUtility).Assembly.GetType("UnityStubs.Internal.ImeComposition"),
                "Set", true, typeof(void), "settings-read-ime", typeof(string), typeof(int))
        });
    }

    public bool Initialize()
    {
        RequireOwner();
        if (disposed) return false;
        initialized = admission.Apply(group, AdmissionPhase.GameReady);
        return Available;
    }

    public bool Available
    {
        get
        {
            RequireOwner();
            if (!initialized || disposed || guards.Count != 6) return false;
            try { return guards.All(guard => guard.Available); }
            catch (Exception) { return false; }
        }
    }

    public SettingsReadScope BeginRead()
    {
        RequireOwner();
        if (!Available) throw new InvalidOperationException("Native settings-read action guards are unavailable.");
        return SettingsReadScope.Begin();
    }

    public void Dispose()
    {
        RequireOwner();
        if (disposed) return;
        disposed = true;
        initialized = false;
        List<Exception>? failures = null;
        for (int index = group.Patches.Count - 1; index >= 0; index--)
            try { group.Patches[index].Rollback(); }
            catch (Exception failure) { (failures ??= new()).Add(failure); }
        if (failures != null) throw new AggregateException("Settings-read action guard cleanup failed.", failures);
    }

    private PatchCandidate Bind(string id, Type? declaringType, string name, bool isStatic,
        Type result, string reason, params Type[] arguments)
    {
        MethodInfo? target = null;
        try
        {
            target = declaringType?.GetMethod(name, BindingFlags.Public | BindingFlags.DeclaredOnly |
                (isStatic ? BindingFlags.Static : BindingFlags.Instance), null, arguments, null);
        }
        catch (Exception) { /* A failed exact lookup becomes a normal rejected C01 candidate. */ }
        if (target == null || target.DeclaringType != declaringType || target.ReturnType != result ||
            target.IsStatic != isStatic || !target.IsPublic || target.IsGenericMethod || target.ContainsGenericParameters ||
            !target.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(arguments))
            return new PatchCandidate(id, id, (declaringType?.FullName ?? "UnityStubs.Internal.ImeComposition") + "." + name,
                "automatic settings discovery", "ordinary settings pages",
                () => "The exact native settings-action callable shape is unavailable.", () => { }, () => { });
        var guard = new SettingsReadActionGuard(target, id, reason);
        guards.Add(guard);
        var candidate = guard.Candidate;
        return candidate with { Install = () => { candidate.Install(); guard.Enable(); } };
    }

    private void RequireOwner()
    {
        if (!ReferenceEquals(Thread.CurrentThread, owner))
            throw new InvalidOperationException("Settings-read action guards require their initializing thread.");
    }
}
