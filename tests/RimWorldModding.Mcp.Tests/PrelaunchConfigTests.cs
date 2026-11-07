using System.Diagnostics;
using System.Text;
using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

public class PrelaunchConfigTests
{
    [TestCase("valid", true)]
    [TestCase("malformed", false)]
    [TestCase("dtd", false)]
    [TestCase("reserved", false)]
    [TestCase("directory", false)]
    [TestCase("oversized", false)]
    [TestCase("reparse", false)]
    public async Task SharedPreflightValidatesWithoutChangingInput(string kind, bool valid)
    {
        string root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "prelaunch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string input = Path.Combine(root, kind == "reserved" ? "Prefs.xml" : "Mod_Lab.xml");
            if (kind == "directory") Directory.CreateDirectory(input);
            else if (kind == "reparse")
            {
                try { File.CreateSymbolicLink(input, Path.Combine(root, "missing.xml")); }
                catch (IOException exception) when (OperatingSystem.IsWindows() && (exception.HResult & 0xffff) == 1314)
                { Assert.Ignore("This host lacks the Windows symbolic-link privilege; the reparse fixture was not executed."); }
            }
            else
            {
                File.WriteAllText(input, kind switch
                {
                    "malformed" => "<Settings>",
                    "dtd" => "<!DOCTYPE x [<!ENTITY e 'bad'>]><x>&e;</x>",
                    _ => "<Settings><size>1000</size></Settings>"
                });
                if (kind == "oversized")
                {
                    using var stream = File.OpenWrite(input);
                    stream.SetLength(16 * 1024 * 1024 + 1);
                }
            }
            var filesBefore = Directory.GetFileSystemEntries(root);
            string module = Path.Combine(TestRepository.FindRoot(), "scripts", "RimWorldPrelaunchConfig.Support.psm1");
            static string Literal(string value) => "'" + value.Replace("'", "''") + "'";
            string command = "$ErrorActionPreference='Stop'; try { Import-Module " + Literal(module) +
                " -Force; $items = @(Get-RimWorldPrelaunchConfigFiles -SourceDirectory " + Literal(root) +
                "); Write-Output $items.Count; exit 0 } catch { [Console]::Error.WriteLine($_.Exception.Message); exit 2 }";
            var start = new ProcessStartInfo("pwsh") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add("-NoProfile");
            start.ArgumentList.Add("-EncodedCommand");
            start.ArgumentList.Add(Convert.ToBase64String(Encoding.Unicode.GetBytes(command)));
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch { if (!process.HasExited) process.Kill(entireProcessTree: true); throw; }
            Assert.That(process.ExitCode, Is.EqualTo(valid ? 0 : 2), await stdout + await stderr);
            Assert.That(Directory.GetFileSystemEntries(root), Is.EqualTo(filesBefore), "Preflight must not copy or stage input.");
            if (valid) Assert.That(File.ReadAllText(input), Is.EqualTo("<Settings><size>1000</size></Settings>"));
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
