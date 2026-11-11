using System;
using System.Globalization;

namespace Megamod.Features.ContentSelection.Core;

public sealed record ContentKey
{
    public ContentKey(string sourcePackageId, string fullTypeName, string defName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePackageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullTypeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(defName);
        SourcePackageId = sourcePackageId.ToLowerInvariant();
        FullTypeName = fullTypeName;
        DefName = defName;
    }

    public string SourcePackageId { get; }
    public string FullTypeName { get; }
    public string DefName { get; }

    public string ToStorageToken() => Segment(SourcePackageId) + Segment(FullTypeName) + Segment(DefName);

    private static string Segment(string value) => value.Length.ToString(CultureInfo.InvariantCulture) + ":" + value;

    public static bool TryParse(string? token, out ContentKey? key)
    {
        key = null;
        if (token is null)
            return false;
        var offset = 0;
        if (!ReadSegment(out var source) || !ReadSegment(out var type) || !ReadSegment(out var name) || offset != token.Length)
            return false;
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(name))
            return false;
        key = new ContentKey(source, type, name);
        return true;

        bool ReadSegment(out string value)
        {
            value = string.Empty;
            var separator = token.IndexOf(':', offset);
            if (separator < 0 || !int.TryParse(token.AsSpan(offset, separator - offset),
                NumberStyles.None, CultureInfo.InvariantCulture, out var length) || length > token.Length - separator - 1)
                return false;
            value = token.Substring(separator + 1, length);
            offset = separator + 1 + length;
            return true;
        }
    }
}
