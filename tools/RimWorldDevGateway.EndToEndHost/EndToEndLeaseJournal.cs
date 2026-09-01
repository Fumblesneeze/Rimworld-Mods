using System.Diagnostics;
using System.Text;

namespace RimWorldDevGateway.EndToEndHost;

public static class EndToEndLeaseJournal
{
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly TimeSpan RetryLimit = TimeSpan.FromSeconds(2);

    public static void WriteAtomically(string path, string content)
    {
        string destination = RequiredPath(path);
        ArgumentNullException.ThrowIfNull(content);
        string parent = Path.GetDirectoryName(destination) ??
                        throw new ArgumentException("The lease journal has no parent directory.", nameof(path));
        Directory.CreateDirectory(parent);
        string temporary = destination + ".tmp." + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temporary, content, Utf8WithoutBom);
            RetryTransientFileOperation(
                () => File.Move(temporary, destination, overwrite: true),
                () => File.Exists(temporary));
        }
        finally
        {
            RetryTransientFileOperation(
                () =>
                {
                    if (File.Exists(temporary))
                    {
                        File.Delete(temporary);
                    }
                },
                () => File.Exists(temporary));
        }
    }

    public static void Clear(string path)
    {
        string destination = RequiredPath(path);
        RetryTransientFileOperation(
            () =>
            {
                if (File.Exists(destination))
                {
                    File.Delete(destination);
                }
            },
            () => File.Exists(destination));
    }

    private static void RetryTransientFileOperation(Action operation, Func<bool> stillApplicable)
    {
        var deadline = Stopwatch.StartNew();
        int delayMilliseconds = 25;
        while (true)
        {
            try
            {
                operation();
                return;
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException &&
                stillApplicable() &&
                deadline.Elapsed < RetryLimit)
            {
                Thread.Sleep(delayMilliseconds);
                delayMilliseconds = Math.Min(delayMilliseconds * 2, 250);
            }
        }
    }

    private static string RequiredPath(string path) =>
        Path.GetFullPath(string.IsNullOrWhiteSpace(path)
            ? throw new ArgumentException("A lease journal path is required.", nameof(path))
            : path);
}
