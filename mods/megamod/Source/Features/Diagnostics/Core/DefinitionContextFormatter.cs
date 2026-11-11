using System;
using System.IO;
using System.Globalization;
using System.Text;

namespace Megamod.Features.Diagnostics;

public static class DefinitionContextFormatter
{
    public static string Append(string original, DefinitionContext context) =>
        original + "\n\n[Def Error] Current definition context\n" +
        $"Definition: {Field(context.DefinitionName)}\nDeclared kind: {Field(context.DeclaredKind)}\n" +
        $"Source mod: {Field(context.ModName)}\nPackage: {Field(context.PackageId)}\n" +
        FormatPath(context);

    private static string Field(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "unknown";
        var escaped = new StringBuilder(value.Length);
        foreach (char character in value)
        {
            switch (character)
            {
                case '\\': escaped.Append("\\\\"); break;
                case '\n': escaped.Append("\\n"); break;
                case '\r': escaped.Append("\\r"); break;
                case '\t': escaped.Append("\\t"); break;
                default:
                    if (char.IsControl(character) || character is '\u2028' or '\u2029')
                        escaped.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    else escaped.Append(character);
                    break;
            }
        }
        return escaped.ToString();
    }

    private static string FormatPath(DefinitionContext context)
    {
        try { return FormatKnownPath(context); }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return "XML file (unresolved path): " + Field(context.SourceFilePath);
        }
    }

    private static string FormatKnownPath(DefinitionContext context)
    {
        if (string.IsNullOrEmpty(context.SourceFilePath)) return "XML file: unknown";
        if (!Path.IsPathFullyQualified(context.SourceFilePath))
            return "XML file (unresolved path): " + Field(context.SourceFilePath);
        if (string.IsNullOrEmpty(context.ModRootDirectory) || !Path.IsPathFullyQualified(context.ModRootDirectory))
            return "XML file (external path): " + Field(Path.GetFullPath(context.SourceFilePath));
        string relative = Path.GetRelativePath(context.ModRootDirectory, context.SourceFilePath);
        bool inside = relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !Path.IsPathRooted(relative);
        return inside ? "XML file (relative to source mod): " + Field(relative)
            : "XML file (external path): " + Field(Path.GetFullPath(context.SourceFilePath));
    }
}
