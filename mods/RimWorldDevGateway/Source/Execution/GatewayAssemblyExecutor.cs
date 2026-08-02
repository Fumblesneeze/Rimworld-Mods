using System.Reflection;
using System.Threading;

namespace RimWorldDevGateway;

public sealed class GatewayExecutionException : Exception
{
    public GatewayExecutionException(string code, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }

    public string Code { get; }
}

public sealed class GatewayAssemblyExecutor
{
    private readonly int maximumAssemblyBytes;
    private int uploadCount;

    public GatewayAssemblyExecutor(int maximumAssemblyBytes)
    {
        if (maximumAssemblyBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAssemblyBytes));
        }

        this.maximumAssemblyBytes = maximumAssemblyBytes;
    }

    public bool UnrestrictedExecutionEnabled => true;

    public int UploadCount => Volatile.Read(ref uploadCount);

    public string Execute(
        byte[] assemblyBytes,
        string entryType,
        string entryMethod,
        string requestJson)
    {
        if (assemblyBytes is null)
        {
            throw new ArgumentNullException(nameof(assemblyBytes));
        }

        if (assemblyBytes.Length == 0)
        {
            throw new GatewayExecutionException("invalid_assembly", "The uploaded assembly is empty.");
        }

        if (assemblyBytes.Length > maximumAssemblyBytes)
        {
            throw new GatewayExecutionException(
                "assembly_too_large",
                $"The uploaded assembly exceeds the {maximumAssemblyBytes}-byte limit.");
        }

        if (string.IsNullOrWhiteSpace(entryType) || string.IsNullOrWhiteSpace(entryMethod))
        {
            throw new GatewayExecutionException(
                "invalid_entry_point",
                "Both an exact entry type and entry method are required.");
        }

        Assembly assembly;
        try
        {
            assembly = Assembly.Load(assemblyBytes);
            Interlocked.Increment(ref uploadCount);
        }
        catch (Exception exception)
        {
            throw new GatewayExecutionException(
                "invalid_assembly",
                "The uploaded bytes could not be loaded as a compatible assembly.",
                exception);
        }

        var type = assembly.GetType(entryType, throwOnError: false, ignoreCase: false);
        if (type is null)
        {
            throw new GatewayExecutionException(
                "invalid_entry_point",
                $"Entry type '{entryType}' was not found in the uploaded assembly.");
        }

        MethodInfo? match = null;
        foreach (var candidate in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            if (!string.Equals(candidate.Name, entryMethod, StringComparison.Ordinal) ||
                candidate.ReturnType != typeof(string))
            {
                continue;
            }

            var parameters = candidate.GetParameters();
            if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string))
            {
                continue;
            }

            if (match is not null)
            {
                throw new GatewayExecutionException(
                    "invalid_entry_point",
                    $"Entry point '{entryType}.{entryMethod}' is ambiguous.");
            }

            match = candidate;
        }

        if (match is null)
        {
            throw new GatewayExecutionException(
                "invalid_entry_point",
                $"Entry point '{entryType}.{entryMethod}' must be declared public static string {entryMethod}(string requestJson).");
        }

        try
        {
            return (string)match.Invoke(null, new object?[] { requestJson ?? string.Empty })!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw new GatewayExecutionException(
                "entry_point_failed",
                exception.InnerException.Message,
                exception.InnerException);
        }
        catch (Exception exception)
        {
            throw new GatewayExecutionException(
                "entry_point_failed",
                exception.Message,
                exception);
        }
    }
}
