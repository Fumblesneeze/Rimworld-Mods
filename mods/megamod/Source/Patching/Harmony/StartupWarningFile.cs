using System;
using System.IO;
using System.Text;

namespace Megamod.Patching;

public sealed class StartupWarningFile : IDisposable
{
    private readonly object writeLock = new();
    private FileStream? stream;
    private bool disposed;
    public StartupWarningFile(string saveDataRoot)
    {
        if (!Path.IsPathFullyQualified(saveDataRoot)) throw new ArgumentException("An absolute active save-data root is required.", nameof(saveDataRoot));
        FilePath = Path.Combine(Path.GetFullPath(saveDataRoot), "Logs", "Megamod-startup-" + Guid.NewGuid().ToString("N") + ".log");
    }
    public string FilePath { get; }
    public void Append(string warning)
    {
        lock (writeLock)
        {
            if (disposed) throw new ObjectDisposedException(nameof(StartupWarningFile));
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            // CreateNew and retaining this handle preserve ownership; never append a preexisting file.
            stream ??= new FileStream(FilePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            byte[] record = Encoding.UTF8.GetBytes(warning + Environment.NewLine);
            stream.Write(record, 0, record.Length);
            stream.Flush(true);
        }
    }
    public void Dispose()
    {
        lock (writeLock)
        {
            disposed = true;
            stream?.Dispose();
            stream = null;
        }
    }
}
