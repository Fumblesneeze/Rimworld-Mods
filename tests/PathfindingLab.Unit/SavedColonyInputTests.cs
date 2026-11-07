using System.Security.Cryptography;
using NUnit.Framework;
using PathfindingLab.EndToEndTests;

namespace PathfindingLab.Unit;

public sealed class SavedColonyInputTests
{
    [Test]
    public void ValidatesSavedTickWithoutRewindingOrAcceptingUnexpectedProgress()
    {
        Assert.That(SavedColonyInput.ValidateLoadTick(12000, 12000), Is.Zero);
        Assert.That(SavedColonyInput.ValidateLoadTick(12000, 12001), Is.EqualTo(1));
        Assert.Throws<IOException>(() => SavedColonyInput.ValidateLoadTick(12000, 11999));
        Assert.Throws<IOException>(() => SavedColonyInput.ValidateLoadTick(12000, 12002));
        Assert.Throws<IOException>(() => SavedColonyInput.ValidateLoadTick(-1, 0));
    }

    [Test]
    public void CopiesVerifiedMasterAndNeverOverwritesExistingInput()
    {
        string directory = Path.Combine(Path.GetTempPath(), "pathfinding-save-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string source = Path.Combine(directory, "master.rws"), target = Path.Combine(directory, "run.rws");
        byte[] original = { 1, 2, 3, 4 };
        try
        {
            File.WriteAllBytes(source, original);
            string hash = Convert.ToHexString(SHA256.HashData(original));
            SavedColonyInput.CopyVerified(source, target, hash);
            Assert.That(File.ReadAllBytes(target), Is.EqualTo(original));
            Assert.Throws<IOException>(() => SavedColonyInput.CopyVerified(source, target, hash));
            Assert.Throws<IOException>(() => SavedColonyInput.CopyVerified(source, source, hash));
            Assert.That(File.ReadAllBytes(source), Is.EqualTo(original));
            string rejected = Path.Combine(directory, "bad-hash.rws");
            Assert.Throws<IOException>(() => SavedColonyInput.CopyVerified(source, rejected, new string('0', 64)));
            Assert.That(File.Exists(rejected), Is.False, "A rejected master must not create a run input.");
        }
        finally { Directory.Delete(directory, true); }
    }
}
