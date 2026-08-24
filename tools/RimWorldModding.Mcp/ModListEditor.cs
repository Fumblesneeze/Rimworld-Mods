using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace RimWorldModding.Mcp;

public sealed record ModListState(string ConfigPath, string Sha256, IReadOnlyList<string> ActiveMods);

public sealed record ModListMutationResult(
    string ConfigPath,
    string PackageId,
    string AnchorPackageId,
    bool Changed,
    string? BackupPath,
    string BeforeSha256,
    string AfterSha256,
    IReadOnlyList<string> ActiveMods);

public sealed class ModListException(string message) : Exception(message);

public static class ModListEditor
{
    public static ModListState Read(string configPath)
    {
        var path = Path.GetFullPath(configPath);
        if (!File.Exists(path)) throw new ModListException($"ModsConfig.xml does not exist: {path}");
        var document = Load(path);
        var active = ActiveMods(document);
        return new ModListState(path, Hash(path), active.Elements("li").Select(node => node.Value.Trim()).ToArray());
    }

    public static ModListMutationResult Enable(
        string configPath,
        string packageId,
        string anchorPackageId,
        string backupRoot)
    {
        var path = Path.GetFullPath(configPath);
        var canonicalPackage = RequiredPackageId(packageId, "package ID");
        var canonicalAnchor = RequiredPackageId(anchorPackageId, "anchor package ID");
        var document = Load(path);
        var active = ActiveMods(document);
        var ids = active.Elements("li").Select(node => node.Value.Trim()).ToArray();
        var duplicates = ids.Count(id => string.Equals(id, canonicalPackage, StringComparison.OrdinalIgnoreCase));
        if (duplicates > 1)
            throw new ModListException($"ModsConfig.xml already contains duplicate package ID '{canonicalPackage}'.");
        var beforeHash = Hash(path);
        var anchor = active.Elements("li").SingleOrDefault(node =>
            string.Equals(node.Value.Trim(), canonicalAnchor, StringComparison.OrdinalIgnoreCase));
        if (anchor is null) throw new ModListException($"Exact anchor package ID was not found: {canonicalAnchor}");
        var existing = active.Elements("li").SingleOrDefault(node =>
            string.Equals(node.Value.Trim(), canonicalPackage, StringComparison.OrdinalIgnoreCase));
        if (existing is not null && ReferenceEquals(anchor.ElementsAfterSelf("li").FirstOrDefault(), existing))
        {
            return new ModListMutationResult(path, canonicalPackage, canonicalAnchor, false, null, beforeHash, beforeHash, ids);
        }

        Directory.CreateDirectory(backupRoot);
        var backup = Path.Combine(
            Path.GetFullPath(backupRoot),
            $"ModsConfig.{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}.{beforeHash[..12]}.xml");
        File.Copy(path, backup, overwrite: false);

        var temporary = Path.Combine(Path.GetDirectoryName(path)!, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            existing?.Remove();
            anchor.AddAfterSelf(new XElement("li", canonicalPackage));
            using (var writer = XmlWriter.Create(temporary, new XmlWriterSettings
                   {
                       Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                       Indent = true,
                       NewLineChars = Environment.NewLine,
                       NewLineHandling = NewLineHandling.Replace
                   }))
            {
                document.Save(writer);
            }

            var verification = Load(temporary);
            var afterIds = ActiveMods(verification).Elements("li").Select(node => node.Value.Trim()).ToArray();
            var expectedBefore = ids.Where(id => !string.Equals(id, canonicalPackage, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (afterIds.Count(id => string.Equals(id, canonicalPackage, StringComparison.OrdinalIgnoreCase)) != 1 ||
                !PreservesInsertion(expectedBefore, afterIds, canonicalPackage, canonicalAnchor))
                throw new ModListException("Generated ModsConfig.xml did not preserve unrelated mod order and one anchored insertion.");

            File.Move(temporary, path, overwrite: true);
            var reparsed = Read(path);
            return new ModListMutationResult(
                path,
                canonicalPackage,
                canonicalAnchor,
                true,
                backup,
                beforeHash,
                reparsed.Sha256,
                reparsed.ActiveMods);
        }
        catch
        {
            if (File.Exists(backup)) File.Copy(backup, path, overwrite: true);
            throw;
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public static ModListMutationResult Disable(
        string configPath,
        string packageId,
        string backupRoot)
    {
        var path = Path.GetFullPath(configPath);
        var canonicalPackage = RequiredPackageId(packageId, "package ID");
        var document = Load(path);
        var active = ActiveMods(document);
        var ids = active.Elements("li").Select(node => node.Value.Trim()).ToArray();
        var matches = active.Elements("li")
            .Where(node => string.Equals(node.Value.Trim(), canonicalPackage, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matches.Length > 1)
            throw new ModListException($"ModsConfig.xml already contains duplicate package ID '{canonicalPackage}'.");
        var beforeHash = Hash(path);
        if (matches.Length == 0)
            return new ModListMutationResult(path, canonicalPackage, "", false, null, beforeHash, beforeHash, ids);

        var backup = CreateBackup(path, backupRoot, beforeHash);
        var temporary = Path.Combine(Path.GetDirectoryName(path)!, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            matches[0].Remove();
            Save(document, temporary);
            var afterIds = ActiveMods(Load(temporary)).Elements("li").Select(node => node.Value.Trim()).ToArray();
            if (!afterIds.SequenceEqual(ids.Where(id => !string.Equals(id, canonicalPackage, StringComparison.OrdinalIgnoreCase)), StringComparer.Ordinal))
                throw new ModListException("Generated ModsConfig.xml did not preserve unrelated mod order during removal.");
            File.Move(temporary, path, overwrite: true);
            var reparsed = Read(path);
            return new ModListMutationResult(
                path, canonicalPackage, "", true, backup, beforeHash, reparsed.Sha256, reparsed.ActiveMods);
        }
        catch
        {
            if (File.Exists(backup)) File.Copy(backup, path, overwrite: true);
            throw;
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public static ModListState Restore(string configPath, string backupPath, string expectedCurrentSha256)
    {
        var path = Path.GetFullPath(configPath);
        var backup = Path.GetFullPath(backupPath);
        if (!File.Exists(backup)) throw new ModListException($"ModsConfig backup does not exist: {backup}");
        if (expectedCurrentSha256.Length != 64 || expectedCurrentSha256.Any(character => !Uri.IsHexDigit(character)))
            throw new ModListException("expectedCurrentSha256 is invalid.");
        var currentHash = Hash(path);
        if (!string.Equals(currentHash, expectedCurrentSha256, StringComparison.OrdinalIgnoreCase))
            throw new ModListException("Current ModsConfig.xml hash changed; refusing to overwrite it with a backup.");
        _ = Load(backup);
        var temporary = Path.Combine(Path.GetDirectoryName(path)!, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.restore.tmp");
        try
        {
            File.Copy(backup, temporary, overwrite: false);
            _ = Load(temporary);
            File.Move(temporary, path, overwrite: true);
            return Read(path);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public static string DefaultConfigPath()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(
            userProfile,
            "AppData",
            "LocalLow",
            "Ludeon Studios",
            "RimWorld by Ludeon Studios",
            "Config",
            "ModsConfig.xml");
    }

    private static XDocument Load(string path)
    {
        try
        {
            return XDocument.Load(path, LoadOptions.PreserveWhitespace);
        }
        catch (Exception exception) when (exception is IOException or XmlException)
        {
            throw new ModListException($"ModsConfig.xml is invalid or unreadable: {exception.Message}");
        }
    }

    private static XElement ActiveMods(XDocument document) =>
        document.Root?.Element("activeMods") ?? throw new ModListException("ModsConfig.xml has no activeMods element.");

    private static string RequiredPackageId(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsWhiteSpace))
            throw new ModListException($"A nonblank canonical {label} is required.");
        return value.Trim().ToLowerInvariant();
    }

    private static bool PreservesInsertion(
        IReadOnlyList<string> before,
        IReadOnlyList<string> after,
        string packageId,
        string anchorPackageId)
    {
        if (after.Count != before.Count + 1) return false;
        var insertion = after.ToList().FindIndex(id => string.Equals(id, packageId, StringComparison.OrdinalIgnoreCase));
        if (insertion <= 0 || !string.Equals(after[insertion - 1], anchorPackageId, StringComparison.OrdinalIgnoreCase)) return false;
        return before.SequenceEqual(after.Where((_, index) => index != insertion), StringComparer.Ordinal);
    }

    private static string CreateBackup(string path, string backupRoot, string beforeHash)
    {
        Directory.CreateDirectory(backupRoot);
        var backup = Path.Combine(
            Path.GetFullPath(backupRoot),
            $"ModsConfig.{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}.{beforeHash[..12]}.xml");
        File.Copy(path, backup, overwrite: false);
        return backup;
    }

    private static void Save(XDocument document, string path)
    {
        using var writer = XmlWriter.Create(path, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            NewLineChars = Environment.NewLine,
            NewLineHandling = NewLineHandling.Replace
        });
        document.Save(writer);
    }

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
