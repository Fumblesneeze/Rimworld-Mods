using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Linq;
using RimWorldDevGateway.Contracts;

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
    public const int DefaultMaximumResultUtf8Bytes = 10 * 1024 * 1024;

    private readonly int maximumAssemblyBytes;
    private readonly int maximumResultUtf8Bytes;
    private readonly IGatewayAssemblyRuntimeExtensions runtimeExtensions;
    private int uploadCount;

    public GatewayAssemblyExecutor(int maximumAssemblyBytes)
        : this(
            maximumAssemblyBytes,
            DefaultMaximumResultUtf8Bytes,
            UnavailableRuntimeExtensions.Instance)
    {
    }

    public GatewayAssemblyExecutor(
        int maximumAssemblyBytes,
        int maximumResultUtf8Bytes,
        IGatewayAssemblyRuntimeExtensions runtimeExtensions)
    {
        if (maximumAssemblyBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAssemblyBytes));
        }

        if (maximumResultUtf8Bytes <= Encoding.UTF8.GetByteCount(GatewayAssemblyExecutionResult.TruncationMarker))
        {
            throw new ArgumentOutOfRangeException(nameof(maximumResultUtf8Bytes));
        }

        this.maximumAssemblyBytes = maximumAssemblyBytes;
        this.maximumResultUtf8Bytes = maximumResultUtf8Bytes;
        this.runtimeExtensions = runtimeExtensions ??
            throw new ArgumentNullException(nameof(runtimeExtensions));
    }

    public bool UnrestrictedExecutionEnabled => true;

    public int UploadCount => Volatile.Read(ref uploadCount);

    public string Execute(
        byte[] assemblyBytes,
        string entryType,
        string entryMethod,
        string requestJson) =>
        Execute(
            assemblyBytes,
            entryType,
            entryMethod,
            requestJson,
            requestId: "legacy-assembly-execution",
            CancellationToken.None).Value;

    public GatewayAssemblyExecutionResult Execute(
        byte[] assemblyBytes,
        string entryType,
        string entryMethod,
        string requestJson,
        string requestId,
        CancellationToken cancellationToken)
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

        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new GatewayExecutionException(
                "invalid_request_id",
                "A request ID is required for correlated assembly execution.");
        }

        cancellationToken.ThrowIfCancellationRequested();

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

        var namedMethods = type
            .GetMethods(
                BindingFlags.Public |
                BindingFlags.Static |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly)
            .Where(candidate => string.Equals(candidate.Name, entryMethod, StringComparison.Ordinal))
            .ToArray();
        if (namedMethods.Length != 1)
        {
            throw new GatewayExecutionException(
                "invalid_entry_point",
                namedMethods.Length == 0
                    ? $"Entry point '{entryType}.{entryMethod}' was not found."
                    : $"Entry point '{entryType}.{entryMethod}' is ambiguous.");
        }

        var match = namedMethods[0];
        var parameters = match.GetParameters();
        var usesContext =
            parameters.Length == 2 &&
            parameters[0].ParameterType == typeof(string) &&
            parameters[1].ParameterType == typeof(GatewayAssemblyExecutionContext);
        var usesLegacyContract =
            parameters.Length == 1 &&
            parameters[0].ParameterType == typeof(string);
        if (!match.IsStatic ||
            match.ReturnType != typeof(string) ||
            match.ContainsGenericParameters ||
            (!usesLegacyContract && !usesContext))
        {
            throw new GatewayExecutionException(
                "invalid_entry_point",
                $"Entry point '{entryType}.{entryMethod}' must be one declared non-generic public static string method with either (string requestJson) or (string requestJson, GatewayAssemblyExecutionContext context).");
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var arguments = usesContext
                ? new object?[]
                {
                    requestJson ?? string.Empty,
                    new GatewayAssemblyExecutionContext(
                        requestId,
                        cancellationToken,
                        runtimeExtensions)
                }
                : new object?[] { requestJson ?? string.Empty };
            var value = (string?)match.Invoke(null, arguments) ?? string.Empty;
            return NormalizeResult(value, maximumResultUtf8Bytes);
        }
        catch (TargetInvocationException exception)
            when (exception.InnerException is OperationCanceledException &&
                  cancellationToken.IsCancellationRequested)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw new GatewayExecutionException(
                "entry_point_failed",
                exception.InnerException.Message,
                exception.InnerException);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new GatewayExecutionException(
                "entry_point_failed",
                exception.Message,
                exception);
        }
    }

    internal static GatewayAssemblyExecutionResult NormalizeResult(
        string value,
        int maximumUtf8Bytes)
    {
        var originalUtf8Bytes = Encoding.UTF8.GetByteCount(value);
        if (originalUtf8Bytes <= maximumUtf8Bytes)
        {
            return new GatewayAssemblyExecutionResult(value, truncated: false, originalUtf8Bytes);
        }

        var marker = GatewayAssemblyExecutionResult.TruncationMarker;
        var valueBudget = maximumUtf8Bytes - Encoding.UTF8.GetByteCount(marker);
        var prefixCharacters = 0;
        var prefixUtf8Bytes = 0;
        while (prefixCharacters < value.Length)
        {
            var character = value[prefixCharacters];
            var characterCount = 1;
            int encodedBytes;
            if (character <= 0x7f)
            {
                encodedBytes = 1;
            }
            else if (character <= 0x7ff)
            {
                encodedBytes = 2;
            }
            else if (char.IsHighSurrogate(character) &&
                     prefixCharacters + 1 < value.Length &&
                     char.IsLowSurrogate(value[prefixCharacters + 1]))
            {
                characterCount = 2;
                encodedBytes = 4;
            }
            else
            {
                encodedBytes = 3;
            }

            if (prefixUtf8Bytes + encodedBytes > valueBudget)
            {
                break;
            }

            prefixUtf8Bytes += encodedBytes;
            prefixCharacters += characterCount;
        }

        return new GatewayAssemblyExecutionResult(
            value.Substring(0, prefixCharacters) + marker,
            truncated: true,
            originalUtf8Bytes);
    }

    private sealed class UnavailableRuntimeExtensions : IGatewayAssemblyRuntimeExtensions
    {
        public static UnavailableRuntimeExtensions Instance { get; } = new();

        public void RegisterSessionAutomation(
            GatewayAssemblyAutomationDescriptor descriptor,
            GatewayAssemblyAutomationHandler handler) =>
            throw new GatewayExecutionException(
                "assembly_runtime_extensions_unavailable",
                "Assembly runtime extensions are unavailable in this host.");
    }
}

public sealed class GatewayAssemblyExecutionResult
{
    public const string TruncationMarker = "\n[truncated]";

    public GatewayAssemblyExecutionResult(
        string value,
        bool truncated,
        int originalUtf8Bytes)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Truncated = truncated;
        OriginalUtf8Bytes = originalUtf8Bytes;
    }

    public string Value { get; }

    public bool Truncated { get; }

    public int OriginalUtf8Bytes { get; }
}
