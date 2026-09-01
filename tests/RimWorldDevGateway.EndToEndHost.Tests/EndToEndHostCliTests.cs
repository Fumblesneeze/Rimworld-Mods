using NUnit.Framework;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;

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

    [Test]
    [Platform(Include = "Win")]
    public void Lease_journal_retries_short_lived_locks_during_replace_and_clear()
    {
        string directory = Path.Combine(Path.GetTempPath(), "gateway-stage-journal-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "stage.lease.json");
        Directory.CreateDirectory(directory);
        File.WriteAllText(path, "old");

        try
        {
            using (var replaceBlocker = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var release = new Thread(() =>
                {
                    Thread.Sleep(125);
                    replaceBlocker.Dispose();
                });
                release.Start();
                EndToEndLeaseJournal.WriteAtomically(path, "new");
                release.Join();
            }

            Assert.That(File.ReadAllText(path), Is.EqualTo("new"));

            using (var clearBlocker = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var release = new Thread(() =>
                {
                    Thread.Sleep(125);
                    clearBlocker.Dispose();
                });
                release.Start();
                EndToEndLeaseJournal.Clear(path);
                release.Join();
            }

            Assert.That(File.Exists(path), Is.False);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Test]
    [Platform(Include = "Win")]
    public void Lease_journal_persistent_replace_failure_preserves_old_content_and_removes_owned_temporary()
    {
        string directory = Path.Combine(Path.GetTempPath(), "gateway-stage-journal-failure-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "stage.lease.json");
        Directory.CreateDirectory(directory);
        File.WriteAllText(path, "old");

        try
        {
            using (var blocker = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Exception? failure = Assert.Catch(
                    () => EndToEndLeaseJournal.WriteAtomically(path, "new"));
                Assert.That(
                    failure,
                    Is.TypeOf<IOException>().Or.TypeOf<UnauthorizedAccessException>());
            }

            Assert.Multiple(() =>
            {
                Assert.That(File.ReadAllText(path), Is.EqualTo("old"));
                Assert.That(
                    Directory.EnumerateFiles(directory, "stage.lease.json.tmp.*"),
                    Is.Empty,
                    "A failed journal replacement must remove only the exact temporary sibling it created.");
            });
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Test]
    [Platform(Include = "Win")]
    public void Clean_command_retries_a_short_lived_lock_on_its_exact_lease_journal()
    {
        string directory = Path.Combine(Path.GetTempPath(), "gateway-clean-journal-" + Guid.NewGuid().ToString("N"));
        string destination = Path.Combine(directory, "mods", "owner", "1.6", EndToEndStagePaths.ContentDirectoryName);
        string leasePath = Path.Combine(directory, "stage.lease.json");
        Directory.CreateDirectory(directory);
        File.WriteAllText(leasePath, JsonSerializer.Serialize(new[]
        {
            new
            {
                destinationDirectory = destination,
                ownerPackageId = "owner",
                rimWorldVersion = "1.6",
                transactionId = Guid.NewGuid().ToString("N"),
                state = (int)EndToEndStageLeaseState.RolledBack,
            }
        }));

        try
        {
            using var blocker = new FileStream(leasePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var release = new Thread(() =>
            {
                Thread.Sleep(125);
                blocker.Dispose();
            });
            release.Start();
            using var output = new StringWriter();
            using var error = new StringWriter();

            int exitCode = EndToEndHostCli.Invoke(new[]
            {
                "clean",
                "--lease-file", leasePath,
                "--output", "json",
            }, output, error);
            release.Join();

            Assert.Multiple(() =>
            {
                Assert.That(exitCode, Is.Zero);
                Assert.That(File.Exists(leasePath), Is.False);
                Assert.That(output.ToString(), Does.Contain("\"status\": \"cleaned\""));
                Assert.That(error.ToString(), Is.Empty);
            });
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
