using System.Text;

namespace RimWorldModding.Mcp;

internal static class DurableFile
{
    public static void WriteAllText(string path, string content)
    {
        var exact = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(exact)!);
        var temporary = exact + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(
                       temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                       4096, FileOptions.WriteThrough))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(content);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, exact, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
