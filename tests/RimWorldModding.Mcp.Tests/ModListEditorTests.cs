using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class ModListEditorTests
{
    [Test]
    public void Enable_InsertsOneEntryAfterExactAnchorAndPreservesOrder()
    {
        var path = WriteConfig("ludeon.rimworld", "orion.hospitality", "example.after");
        try
        {
            var before = ModListEditor.Read(path);
            var result = ModListEditor.Enable(path, "fumblesneeze.guestbedgizmo", "orion.hospitality", BackupRoot());
            var after = ModListEditor.Read(path);

            Assert.That(before.ActiveMods, Is.EqualTo(new[] { "ludeon.rimworld", "orion.hospitality", "example.after" }));
            Assert.That(after.ActiveMods, Is.EqualTo(new[]
            {
                "ludeon.rimworld", "orion.hospitality", "fumblesneeze.guestbedgizmo", "example.after"
            }));
            Assert.That(result.Changed, Is.True);
            Assert.That(File.Exists(result.BackupPath), Is.True);
            Assert.That(result.BeforeSha256, Is.Not.EqualTo(result.AfterSha256));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Enable_IsIdempotentAndRejectsMissingAnchor()
    {
        var path = WriteConfig("ludeon.rimworld", "orion.hospitality", "fumblesneeze.guestbedgizmo");
        try
        {
            var result = ModListEditor.Enable(path, "fumblesneeze.guestbedgizmo", "orion.hospitality", BackupRoot());
            Assert.That(result.Changed, Is.False);
            Assert.That(ModListEditor.Read(path).ActiveMods.Count(id => id == "fumblesneeze.guestbedgizmo"), Is.EqualTo(1));

            Assert.That(
                Assert.Throws<ModListException>(() => ModListEditor.Enable(path, "another.mod", "missing.anchor", BackupRoot()))!.Message,
                Does.Contain("anchor"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Enable_RepositionsAMisplacedExistingEntryAfterTheExactAnchor()
    {
        var path = WriteConfig("fumblesneeze.guestbedgizmo", "ludeon.rimworld", "orion.hospitality", "example.after");
        try
        {
            var result = ModListEditor.Enable(path, "fumblesneeze.guestbedgizmo", "orion.hospitality", BackupRoot());
            Assert.That(result.Changed, Is.True);
            Assert.That(ModListEditor.Read(path).ActiveMods, Is.EqualTo(new[]
            {
                "ludeon.rimworld", "orion.hospitality", "fumblesneeze.guestbedgizmo", "example.after"
            }));
            Assert.That(File.Exists(result.BackupPath), Is.True);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void InvalidXml_IsRejectedWithoutCreatingAReplacement()
    {
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"ModsConfig-{Guid.NewGuid():N}.xml");
        File.WriteAllText(path, "<ModsConfigData><activeMods>");
        var before = File.ReadAllBytes(path);
        try
        {
            Assert.Throws<ModListException>(() => ModListEditor.Enable(path, "new.mod", "anchor", BackupRoot()));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Disable_RemovesExactlyOneEntryAndRestoreRequiresExpectedCurrentHash()
    {
        var path = WriteConfig("ludeon.rimworld", "orion.hospitality", "fumblesneeze.guestbedgizmo", "example.after");
        try
        {
            var removed = ModListEditor.Disable(path, "fumblesneeze.guestbedgizmo", BackupRoot());
            Assert.That(removed.Changed, Is.True);
            Assert.That(ModListEditor.Read(path).ActiveMods, Is.EqualTo(new[]
            {
                "ludeon.rimworld", "orion.hospitality", "example.after"
            }));
            Assert.Throws<ModListException>(() =>
                ModListEditor.Restore(path, removed.BackupPath!, new string('A', 64)));

            var restored = ModListEditor.Restore(path, removed.BackupPath!, removed.AfterSha256);
            Assert.That(restored.ActiveMods, Does.Contain("fumblesneeze.guestbedgizmo"));
            Assert.That(restored.Sha256, Is.EqualTo(removed.BeforeSha256));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string WriteConfig(params string[] packageIds)
    {
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"ModsConfig-{Guid.NewGuid():N}.xml");
        var entries = string.Join(Environment.NewLine, packageIds.Select(id => $"      <li>{id}</li>"));
        File.WriteAllText(path, $"""
            <?xml version="1.0" encoding="utf-8"?>
            <ModsConfigData>
              <version>1.6.4871 rev591</version>
              <activeMods>
            {entries}
              </activeMods>
            </ModsConfigData>
            """);
        return path;
    }

    private static string BackupRoot()
    {
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "modlist-backups");
        Directory.CreateDirectory(path);
        return path;
    }
}
