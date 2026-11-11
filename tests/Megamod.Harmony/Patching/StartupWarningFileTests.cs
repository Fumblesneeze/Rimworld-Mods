using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Patching;

[TestFixture]
public sealed class StartupWarningFileTests
{
    [Test]
    public void AnExistingPathIsNeverAdoptedOrOverwritten()
    {
        string root = Path.Combine(Path.GetTempPath(), "Megamod-warning-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "Logs"));
        try
        {
            using var output = new StartupWarningFile(root);
            File.WriteAllText(output.FilePath, "other evidence");
            Assert.Throws<IOException>(() => output.Append("required warning"));
            Assert.That(File.ReadAllText(output.FilePath), Is.EqualTo("other evidence"));
        }
        finally { Directory.Delete(root, true); }
    }
    [Test]
    public void RelativeRootCannotAcquireAFileUnderTheCurrentDirectory()
    {
        Assert.Throws<ArgumentException>(() => new StartupWarningFile("relative-save-data"));
    }
    [Test]
    public void SessionFallbackWritesEveryConcurrentWarningWithoutChangingOtherEvidence()
    {
        string root = Path.Combine(Path.GetTempPath(), "Megamod-warning-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "Logs"));
        string native = Path.Combine(root, "Logs", "Player.log");
        File.WriteAllText(native, "existing evidence");
        try
        {
            using var output = new StartupWarningFile(root);
            using var otherSession = new StartupWarningFile(root);
            Assert.That(output.FilePath, Is.Not.EqualTo(otherSession.FilePath));
            Parallel.For(0, 80, index => output.Append("Patch P" + index + " unavailable"));
            Assert.That(File.Exists(output.FilePath), Is.True);
            Assert.That(Path.GetDirectoryName(output.FilePath), Is.EqualTo(Path.Combine(root, "Logs")));
            using var reader = new StreamReader(new FileStream(output.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            string[] records = reader.ReadToEnd().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            Assert.That(records.Length, Is.EqualTo(80));
            Assert.That(records.Distinct().Count(), Is.EqualTo(80));
            Assert.That(File.ReadAllText(native), Is.EqualTo("existing evidence"));
        }
        finally { Directory.Delete(root, true); }
    }
}
