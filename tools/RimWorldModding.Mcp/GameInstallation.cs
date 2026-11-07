using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text.Json;

namespace RimWorldModding.Mcp;

public sealed record GameInstallation(
    string Root, string Version, string MajorVersion, string ExecutablePath,
    string ManagedPath, string TargetFramework)
{
    public static GameInstallation ForExecutable(string executablePath, string liveVersion)
    {
        var installation = Inspect(Path.GetDirectoryName(Path.GetFullPath(executablePath))!);
        if (!StringComparer.OrdinalIgnoreCase.Equals(Path.GetFullPath(executablePath), installation.ExecutablePath) ||
            !StringComparer.Ordinal.Equals(liveVersion.Trim(), installation.Version))
            throw new ArgumentException("The live executable/version does not match the selected RimWorld installation.");
        return installation;
    }

    internal static GameInstallation ForSession(string manifestPath, int expectedProcessId)
    {
        if (new FileInfo(manifestPath).Length > 64 * 1024)
            throw new ArgumentException("Gateway session manifest exceeds 64 KiB.");
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var manifest = document.RootElement;
        if (manifest.GetProperty("processId").GetInt32() != expectedProcessId ||
            !DateTimeOffset.TryParse(manifest.GetProperty("processStartUtc").GetString(), out var start) ||
            RunProcessIdentity.Inspect(expectedProcessId, start).State != "running")
            throw new ArgumentException("Gateway compilation requires the exact live session PID/start identity.");
        using var process = Process.GetProcessById(expectedProcessId);
        var executable = process.MainModule?.FileName ?? throw new ArgumentException("Cannot inspect the session executable.");
        return ForExecutable(executable, manifest.GetProperty("gameVersion").GetString() ?? "");
    }

    public static GameInstallation Inspect(string root)
    {
        root = Path.GetFullPath(root);
        var versionPath = Path.Combine(root, "Version.txt");
        RequireFile(versionPath);
        if (new FileInfo(versionPath).Length > 256)
            throw new ArgumentException("RimWorld Version.txt exceeds 256 bytes.");
        var version = File.ReadAllText(versionPath).Trim();
        var match = Regex.Match(version, @"\A(1\.[67])\.[0-9]+(?: rev[0-9]+)?\z", RegexOptions.CultureInvariant);
        if (!match.Success) throw new ArgumentException($"Unsupported RimWorld version: '{version}'. Expected 1.6 or 1.7.");
        var major = match.Groups[1].Value;
        var beta = major == "1.7";
        var executable = Path.Combine(root, beta ? "RimWorld.exe" : "RimWorldWin64.exe");
        var managed = beta ? root : Path.Combine(root, "RimWorldWin64_Data", "Managed");
        RequireFile(executable);
        RequireFile(Path.Combine(managed, "Assembly-CSharp.dll"));
        RequireFile(Path.Combine(managed, beta ? "UnityStubs.dll" : "UnityEngine.CoreModule.dll"));
        return new(root, version, major, executable, managed, beta ? "net10.0" : "net480");
    }

    private static void RequireFile(string path)
    {
        if (!File.Exists(path)) throw new ArgumentException($"Required RimWorld file is missing: {path}");
    }
}
