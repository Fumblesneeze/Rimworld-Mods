namespace Megamod.Settings;

public sealed record SettingsLifecycleRequest<TOwner>(TOwner? Owner, long Generation,
    bool Available, SaveSettingsSnapshot? Save) where TOwner : class;

public sealed class SettingsLifecycleMailbox<TOwner> where TOwner : class
{
    private readonly object gate = new();
    private SettingsLifecycleRequest<TOwner> current = new(null, 0, false, null);
    private SaveSettingsBinding? bound;
    public void SetAvailable(bool available)
    {
        lock (gate)
            if (current.Available != available)
            {
                bound?.Revoke();
                bound = null;
                current = current with { Available = available, Generation = current.Generation + 1, Save = null };
            }
    }
    public void Observe(TOwner? owner)
    {
        lock (gate)
            if (!ReferenceEquals(current.Owner, owner))
            {
                bound?.Revoke();
                bound = null;
                current = new(owner, current.Generation + 1, current.Available, null);
            }
    }
    public SettingsLifecycleRequest<TOwner> Read() { lock (gate) return current; }
    public bool TryReconcileOwner(Func<TOwner?> readOwner, Action<Exception> reportFailure, out TOwner? owner)
    {
        var beforeRead = Read();
        bool readable = TryReadOwner(readOwner, reportFailure, out owner);
        lock (gate)
        {
            // A worker may publish a different owner or finalization during the native getter.
            // Reconcile only the receipt that existed before the read; preserve every newer one.
            if (!readable || !ReferenceEquals(current, beforeRead))
            {
                owner = null;
                return false;
            }
            Observe(owner);
            return true;
        }
    }
    public bool TryReadOwner(Func<TOwner?> readOwner, Action<Exception> reportFailure, out TOwner? owner)
    {
        owner = null;
        if (!Read().Available) return false;
        try
        {
            owner = readOwner();
            return Read().Available;
        }
        catch (Exception exception)
        {
            ReportFailure(exception, reportFailure);
            return false;
        }
    }

    public void ReportFailure(Exception exception, Action<Exception> reportFailure)
    {
        lock (gate)
        {
            if (!current.Available) return;
            bound?.Revoke();
            bound = null;
            current = current with { Available = false, Generation = current.Generation + 1, Save = null };
        }
        try { reportFailure(exception); }
        catch (Exception) { /* A diagnostic sink cannot break the unavailable fallback. */ }
    }
    public bool TryBind(SettingsLifecycleRequest<TOwner> request, SaveSettingsBinding binding)
    {
        lock (gate)
        {
            if (!ReferenceEquals(current, request) || !current.Available || current.Save == null || binding.IsRevoked)
            {
                binding.Revoke();
                return false;
            }
            bound = binding;
            return true;
        }
    }
    public bool NotifyFinalized(TOwner owner, long generation, SaveSettingsSnapshot snapshot)
    {
        lock (gate)
        {
            if (!ReferenceEquals(current.Owner, owner) || current.Generation != generation) return false;
            current = current with { Save = snapshot };
            return true;
        }
    }
    public bool TryGetReady(TOwner? actualOwner, bool finished, out SettingsLifecycleRequest<TOwner> request)
    {
        request = Read();
        return finished && request.Available && request.Save != null && actualOwner != null &&
            ReferenceEquals(request.Owner, actualOwner);
    }
}
