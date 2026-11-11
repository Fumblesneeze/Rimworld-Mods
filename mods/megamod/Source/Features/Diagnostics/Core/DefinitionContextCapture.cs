using System;
using System.IO;
using System.Xml;

namespace Megamod.Features.Diagnostics;

public sealed record DefinitionSource(string? ModName, string? PackageId, string? RootDirectory,
    string? Folder, string? FileName);

public static class DefinitionContextCapture
{
    public static DefinitionContext Read(XmlNode? node, DefinitionSource source, Action<Exception>? reportFailure = null) => new(
        Safe(() => DirectName(node), reportFailure), Safe(() => node?.Attributes?["Class"]?.Value ?? node?.Name, reportFailure),
        source.ModName, source.PackageId, source.RootDirectory,
        Safe(() => string.IsNullOrEmpty(source.FileName) ? null : string.IsNullOrEmpty(source.Folder)
            ? source.FileName : Path.Combine(source.Folder, source.FileName), reportFailure));

    private static string? Safe(Func<string?> read, Action<Exception>? report)
    {
        try { return read(); }
        catch (Exception exception)
        {
            try { report?.Invoke(exception); }
            catch (Exception) { }
            return null;
        }
    }

    private static string? DirectName(XmlNode? node)
    {
        int inspected = 0;
        for (XmlNode? child = node?.FirstChild; child != null && inspected++ < 4096; child = child.NextSibling)
        {
            if (child.NodeType != XmlNodeType.Element || child.Name != "defName") continue;
            XmlNode? text = child.FirstChild;
            return text != null && text.NextSibling == null && text.NodeType is XmlNodeType.Text or XmlNodeType.CDATA ? text.Value : null;
        }
        return null;
    }
}
