using System;
using System.Collections.Generic;

namespace RimWorldDevGateway.IntegrationTesting;

public enum RunAt
{
    MainMenuLoaded = 0,
    PlayableMapLoaded = 1
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class IntegrationTestAttribute : Attribute
{
    public IntegrationTestAttribute(RunAt runAt)
    {
        if (!Enum.IsDefined(typeof(RunAt), runAt))
        {
            throw new ArgumentOutOfRangeException(nameof(runAt));
        }

        RunAt = runAt;
    }

    public RunAt RunAt { get; }
}

public sealed class IntegrationTestAssertionException : Exception
{
    public IntegrationTestAssertionException(string message) : base(message)
    {
    }
}

public static class IntegrationAssert
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

    public static void Null(object? value, string? message = null)
    {
        if (value is not null)
        {
            Fail(message ?? "Expected a null value.");
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
        throw new IntegrationTestAssertionException(
            string.IsNullOrWhiteSpace(message) ? "The integration-test assertion failed." : message);
    }

    private static string Format<T>(T value) => value?.ToString() ?? "null";
}
