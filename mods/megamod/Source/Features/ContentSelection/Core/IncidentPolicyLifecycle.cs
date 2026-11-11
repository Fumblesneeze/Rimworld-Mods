using System;
using System.Collections.Generic;
using System.Threading;
using Megamod.Settings;

namespace Megamod.Features.ContentSelection.Core;

// The main-thread incident boundary is deliberately later than early map-finalization families.
public sealed class IncidentPolicyLifecycle<TOwner> where TOwner : class
{
    private readonly ContentSettings settings;
    private readonly ContentApplication application;
    private readonly ContentCatalog catalog;
    private readonly IReadOnlyCollection<ContentKey> admittedKeys;
    private readonly Func<SettingsLifecycleRequest<TOwner>> readReceipt;
    private readonly Func<TOwner?> readOwner;
    private SettingsLifecycleRequest<TOwner>? attempted;
    private sealed record Publication(SettingsLifecycleRequest<TOwner> Receipt, AppliedContentPolicy Policy);
    private Publication? candidate;
    private Publication? published;

    public IncidentPolicyLifecycle(ContentSettings settings, ContentApplication application, ContentCatalog catalog,
        IReadOnlyCollection<ContentKey> admittedKeys, Func<SettingsLifecycleRequest<TOwner>> readReceipt,
        Func<TOwner?> readOwner)
    {
        this.settings = settings;
        this.application = application;
        this.catalog = catalog;
        this.admittedKeys = new List<ContentKey>(admittedKeys).AsReadOnly();
        this.readReceipt = readReceipt;
        this.readOwner = readOwner;
    }

    public bool Process(bool nativeReady, bool patchesAvailable)
    {
        var receipt = readReceipt();
        if (!nativeReady || !patchesAvailable || !receipt.Available || receipt.Save == null || receipt.Owner == null ||
            !IsCurrent(receipt))
        {
            Volatile.Write(ref published, null);
            return false;
        }
        if (attempted == null || attempted.Generation != receipt.Generation || !ReferenceEquals(attempted.Owner, receipt.Owner))
        {
            Volatile.Write(ref published, null);
            candidate = null;
            attempted = receipt;
            var boundary = application.BeginBoundary(catalog, receipt.Save);
            var requested = settings.CaptureRequested(catalog, receipt.Save);
            if (!IsCurrent(receipt)) return false;
            application.Publish(boundary, requested, admittedKeys);
            if (!IsCurrent(receipt)) { application.EndBoundary(); return false; }
            candidate = new(receipt, application.Applied!);
        }
        Volatile.Write(ref published, candidate);
        return ReadApplied() != null;
    }

    // Worker callbacks read only immutable published values and lifecycle receipts, never Store.
    public AppliedContentPolicy? ReadApplied()
    {
        var value = Volatile.Read(ref published);
        return value != null && IsCurrent(value.Receipt) && ReferenceEquals(value, Volatile.Read(ref published))
            ? value.Policy : null;
    }

    private bool IsCurrent(SettingsLifecycleRequest<TOwner> receipt) => ReferenceEquals(receipt, readReceipt()) &&
        ReferenceEquals(receipt.Owner, readOwner()) && ReferenceEquals(receipt, readReceipt());
}
