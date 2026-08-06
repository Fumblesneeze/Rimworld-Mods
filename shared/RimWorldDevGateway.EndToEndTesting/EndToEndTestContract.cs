using System;
using System.Collections.Generic;
using System.Linq;

namespace RimWorldDevGateway.EndToEndTesting;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RimWorldEndToEndTestAttribute : Attribute
{
    public RimWorldEndToEndTestAttribute(string id, string ownerPackageId, params string[] activePackageIds)
    {
        Id = id;
        OwnerPackageId = ownerPackageId;
        ActivePackageIds = activePackageIds ?? Array.Empty<string>();
    }

    public string Id { get; }

    public string OwnerPackageId { get; }

    public string[] ActivePackageIds { get; }

    public int MaxFrames { get; set; } = 3_600;

    public int MaxGameTicks { get; set; } = 60_000;

    public int MaxWallClockSeconds { get; set; } = 120;
}

public interface IRimWorldEndToEndTest
{
    void Arrange(IEndToEndContext context);

    IEnumerator<EndToEndStep> Execute(IEndToEndContext context);
}

public interface IEndToEndContext
{
    long FrameCount { get; }

    int GameTick { get; }

    object? GetService(Type serviceType);

    void DeferCleanup(Action cleanupAction);
}

public interface IEndToEndFloatMenuCatalog
{
    IReadOnlyList<EndToEndFloatMenuOption> Query(string actorRuntimeId, string targetRuntimeId);
}

public interface IEndToEndGizmoCatalog
{
    IReadOnlyList<EndToEndGizmoOption> Query(
        IReadOnlyList<string> targetRuntimeIds,
        IReadOnlyList<string> architectCategoryDefNames);
}

public sealed class EndToEndGizmoOption
{
    public EndToEndGizmoOption(
        string stableId,
        string runtimeType,
        string label,
        bool disabled,
        EndToEndGizmoInteraction? interaction,
        string? buildableDefName)
        : this(
            stableId,
            runtimeType,
            label,
            disabled,
            interaction,
            buildableDefName,
            null,
            null)
    {
    }

    public EndToEndGizmoOption(
        string stableId,
        string runtimeType,
        string label,
        bool disabled,
        EndToEndGizmoInteraction? interaction,
        string? buildableDefName,
        bool? toggleState,
        string? hotKeyDefName)
    {
        StableId = Required(stableId, nameof(stableId));
        RuntimeType = Required(runtimeType, nameof(runtimeType));
        Label = Required(label, nameof(label));
        Disabled = disabled;
        Interaction = interaction;
        BuildableDefName = string.IsNullOrWhiteSpace(buildableDefName)
            ? null
            : buildableDefName!.Trim();
        ToggleState = toggleState;
        HotKeyDefName = string.IsNullOrWhiteSpace(hotKeyDefName)
            ? null
            : hotKeyDefName!.Trim();
    }

    public string StableId { get; }

    public string RuntimeType { get; }

    public string Label { get; }

    public bool Disabled { get; }

    public EndToEndGizmoInteraction? Interaction { get; }

    public string? BuildableDefName { get; }

    public bool? ToggleState { get; }

    public string? HotKeyDefName { get; }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }

        return value.Trim();
    }
}

public sealed class EndToEndFloatMenuOption
{
    public EndToEndFloatMenuOption(string stableId, string label, bool disabled)
    {
        StableId = Required(stableId, nameof(stableId));
        Label = Required(label, nameof(label));
        Disabled = disabled;
    }

    public string StableId { get; }

    public string Label { get; }

    public bool Disabled { get; }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }

        return value.Trim();
    }
}

public static class EndToEndContextExtensions
{
    public static T GetRequiredService<T>(this IEndToEndContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var service = context.GetService(typeof(T));
        if (service is T typed)
        {
            return typed;
        }

        throw new EndToEndContractException(
            $"The E2E context does not provide required service '{typeof(T).FullName}'.");
    }
}

public sealed class EndToEndTestDescriptor
{
    internal EndToEndTestDescriptor(
        Type testType,
        string id,
        string ownerPackageId,
        IReadOnlyList<string> activePackageIds,
        EndToEndDeadline deadline)
    {
        TestType = testType;
        Id = id;
        OwnerPackageId = ownerPackageId;
        ActivePackageIds = activePackageIds;
        Deadline = deadline;

        var launched = new string[activePackageIds.Count + 1];
        for (var index = 0; index < activePackageIds.Count; index++)
        {
            launched[index] = activePackageIds[index];
        }

        launched[launched.Length - 1] = EndToEndTestContract.GatewayPackageId;
        LaunchedPackageIds = Array.AsReadOnly(launched);
    }

    public Type TestType { get; }

    public string Id { get; }

    public string OwnerPackageId { get; }

    public IReadOnlyList<string> ActivePackageIds { get; }

    public IReadOnlyList<string> LaunchedPackageIds { get; }

    public EndToEndDeadline Deadline { get; }
}

public sealed class EndToEndContractException : Exception
{
    public EndToEndContractException(string message) : base(message)
    {
    }
}

public static class EndToEndTestContract
{
    public const string CorePackageId = "ludeon.rimworld";
    public const string GatewayPackageId = "fumblesneeze.rimworlddevgateway";

    public static EndToEndTestDescriptor Describe(Type testType)
    {
        if (testType is null)
        {
            throw new ArgumentNullException(nameof(testType));
        }

        var attributes = testType
            .GetCustomAttributes(typeof(RimWorldEndToEndTestAttribute), inherit: false)
            .Cast<RimWorldEndToEndTestAttribute>()
            .ToArray();
        if (attributes.Length != 1)
        {
            throw Invalid(testType, "must declare exactly one RimWorldEndToEndTest attribute");
        }

        if (!testType.IsClass || testType.IsAbstract)
        {
            throw Invalid(testType, "must be a concrete class");
        }

        if (!typeof(IRimWorldEndToEndTest).IsAssignableFrom(testType))
        {
            throw Invalid(testType, $"must implement {nameof(IRimWorldEndToEndTest)}");
        }

        if (testType.GetConstructor(Type.EmptyTypes) is null)
        {
            throw Invalid(testType, "must expose a public parameterless constructor");
        }

        var attribute = attributes[0];
        var id = RequiredToken(attribute.Id, "test ID", testType);
        var owner = NormalizePackageId(attribute.OwnerPackageId, "owner package ID", testType);
        var packages = (attribute.ActivePackageIds ?? Array.Empty<string>())
            .Select((packageId, index) => NormalizePackageId(packageId, $"active package ID at index {index}", testType))
            .ToArray();

        if (packages.Length == 0)
        {
            throw Invalid(testType, "must declare a non-empty active package sequence");
        }

        if (!StringComparer.OrdinalIgnoreCase.Equals(packages[0], CorePackageId))
        {
            throw Invalid(testType, $"must list {CorePackageId} first");
        }

        var duplicate = packages
            .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw Invalid(testType, $"contains duplicate active package '{duplicate.Key}'");
        }

        if (packages.Contains(GatewayPackageId, StringComparer.OrdinalIgnoreCase))
        {
            throw Invalid(testType, $"must not list implicit Gateway package {GatewayPackageId}");
        }

        if (!StringComparer.OrdinalIgnoreCase.Equals(owner, GatewayPackageId) &&
            !packages.Contains(owner, StringComparer.OrdinalIgnoreCase))
        {
            throw Invalid(testType, $"does not contain owner package '{owner}'");
        }

        if (attribute.MaxFrames <= 0 || attribute.MaxGameTicks <= 0 || attribute.MaxWallClockSeconds <= 0)
        {
            throw Invalid(testType, "must declare positive frame, game-tick, and wall-clock deadline values");
        }

        return new EndToEndTestDescriptor(
            testType,
            id,
            owner,
            Array.AsReadOnly(packages),
            new EndToEndDeadline(
                attribute.MaxFrames,
                attribute.MaxGameTicks,
                TimeSpan.FromSeconds(attribute.MaxWallClockSeconds)));
    }

    private static string RequiredToken(string? value, string field, Type testType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Invalid(testType, $"has an empty {field}");
        }

        return value!.Trim();
    }

    private static string NormalizePackageId(string? value, string field, Type testType)
    {
        return RequiredToken(value, field, testType).ToLowerInvariant();
    }

    private static EndToEndContractException Invalid(Type testType, string reason)
    {
        return new EndToEndContractException($"E2E test '{testType.FullName ?? testType.Name}' {reason}.");
    }
}

public sealed class EndToEndAssertionException : Exception
{
    public EndToEndAssertionException(string message) : base(message)
    {
    }
}

public static class EndToEndAssert
{
    public static void True(bool condition, string? message = null)
    {
        if (!condition)
        {
            Fail(message ?? "Expected the condition to be true.");
        }
    }

    public static void False(bool condition, string? message = null)
    {
        if (condition)
        {
            Fail(message ?? "Expected the condition to be false.");
        }
    }

    public static void NotNull(object? value, string? message = null)
    {
        if (value is null)
        {
            Fail(message ?? "Expected a non-null value.");
        }
    }

    public static void Equal<T>(T expected, T actual, string? message = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            var prefix = string.IsNullOrWhiteSpace(message) ? string.Empty : message + ": ";
            Fail($"{prefix}expected <{Format(expected)}> but was <{Format(actual)}>.");
        }
    }

    public static void Fail(string message)
    {
        throw new EndToEndAssertionException(
            string.IsNullOrWhiteSpace(message) ? "The E2E assertion failed." : message);
    }

    private static string Format<T>(T value) => value?.ToString() ?? "null";
}
