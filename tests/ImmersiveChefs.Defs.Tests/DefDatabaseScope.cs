using Verse;

namespace ImmersiveChefs.Defs.Tests;

internal sealed class DefDatabaseScope<T> : IDisposable where T : Def
{
    private static readonly object Gate = new();
    private static bool active;

    private bool disposed;

    private DefDatabaseScope()
    {
    }

    public static DefDatabaseScope<T> Register(params T[] defs)
    {
        if (defs is null)
        {
            throw new ArgumentNullException(nameof(defs));
        }

        if (defs.Any(def => def is null || string.IsNullOrWhiteSpace(def.defName)))
        {
            throw new ArgumentException("Every scoped Def must be non-null and have a defName.", nameof(defs));
        }

        lock (Gate)
        {
            if (active)
            {
                throw new InvalidOperationException($"A {typeof(T).Name} DefDatabase scope is already active.");
            }

            if (DefDatabase<T>.DefCount != 0)
            {
                throw new InvalidOperationException(
                    $"The isolated {typeof(T).Name} DefDatabase must be empty before registering test fixtures.");
            }

            active = true;
        }

        try
        {
            DefDatabase<T>.Add(defs);
            return new DefDatabaseScope<T>();
        }
        catch (Exception registrationFailure)
        {
            try
            {
                DefDatabase<T>.Clear();
                Release();
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException(
                    $"Registering {typeof(T).Name} test Defs failed and cleanup also failed.",
                    registrationFailure,
                    cleanupFailure);
            }

            throw;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        DefDatabase<T>.Clear();
        disposed = true;
        Release();
    }

    private static void Release()
    {
        lock (Gate)
        {
            active = false;
        }
    }
}
