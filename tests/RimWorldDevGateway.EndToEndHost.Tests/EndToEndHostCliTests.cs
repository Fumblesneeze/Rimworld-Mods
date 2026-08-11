using NUnit.Framework;
using System;
using System.IO;

namespace RimWorldDevGateway.EndToEndHost.Tests;

[TestFixture]
public sealed class EndToEndHostCliTests
{
    [Test]
    public void Help_is_discoverable_and_successful()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = EndToEndHostCli.Invoke(new[] { "--help" }, output, error);

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.Zero);
            Assert.That(output.ToString(),
                Does.Contain("plan").And.Contain("stage").And.Contain("clean")
                    .And.Contain("performance-plan").And.Contain("performance-stage"));
            Assert.That(error.ToString(), Is.Empty);
        });
    }

    [Test]
    public void Missing_required_options_return_invalid_usage_exit_code()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = EndToEndHostCli.Invoke(new[] { "plan" }, output, error);

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(2));
            Assert.That(error.ToString(), Does.Contain("--repository-root"));
        });
    }

    [Test]
    public void Invalid_existing_path_returns_invalid_usage_without_a_stack_trace()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = EndToEndHostCli.Invoke(new[]
        {
            "plan",
            "--repository-root", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
            "--mods-root", TestContext.CurrentContext.WorkDirectory,
            "--rimworld-version", "1.6",
            "--package-id", "ludeon.rimworld",
            "--output", "json"
        }, output, error);

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(2));
            Assert.That(error.ToString(), Does.Contain("does not exist"));
            Assert.That(error.ToString(), Does.Not.Contain(" at "));
            Assert.That(output.ToString(), Is.Empty);
        });
    }

    [Test]
    public void Missing_package_id_file_is_invalid_usage_without_a_stack_trace()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "packages.txt");

        var exitCode = EndToEndHostCli.Invoke(new[]
        {
            "plan",
            "--repository-root", TestContext.CurrentContext.WorkDirectory,
            "--mods-root", TestContext.CurrentContext.WorkDirectory,
            "--rimworld-version", "1.6",
            "--package-id-file", missing,
            "--output", "json"
        }, output, error);

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(2));
            Assert.That(error.ToString(), Does.Contain("package ID file").And.Contain("does not exist"));
            Assert.That(error.ToString(), Does.Not.Contain(" at "));
            Assert.That(output.ToString(), Is.Empty);
        });
    }
}
