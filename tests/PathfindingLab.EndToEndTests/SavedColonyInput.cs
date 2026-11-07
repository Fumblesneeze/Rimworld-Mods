using System;
using System.IO;
using System.Security.Cryptography;

namespace PathfindingLab.EndToEndTests;

public static class SavedColonyInput
{
    public static int ValidateLoadTick(int savedTick, int currentTick)
    {
        long elapsed = (long)currentTick - savedTick;
        if (savedTick < 0 || elapsed < 0 || elapsed > 1)
            throw new IOException("Unexpected native save-load tick handoff.");
        return (int)elapsed;
    }

    public static void CopyVerified(string source, string destination, string expectedHash)
    {
        if (File.Exists(destination)) throw new IOException("Run save already exists; refusing overwrite.");
        using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (input.Length == 0 || input.Length > 256L * 1024 * 1024)
            throw new IOException("Save outside the declared 256 MiB input bound.");
        string hash = Convert.ToHexString(SHA256.HashData(input));
        if (!string.Equals(hash, expectedHash, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Saved colony checksum mismatch.");
        input.Position = 0;
        using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        input.CopyTo(output);
    }
}
