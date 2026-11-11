namespace Megamod.Settings;

// UI metadata only; sections and setting rows share ordering without creating persisted values.
public sealed record SettingsRowLayout(string Id, string CategoryId, int Order,
    Func<bool>? Visible = null, Func<int>? DisplayOrder = null)
{
    public static IReadOnlyList<SettingsRowPlacement> Capture(IEnumerable<SettingsRowLayout> rows)
    {
        var captured = new List<SettingsRowPlacement>();
        foreach (var row in rows)
            if (row.Visible?.Invoke() ?? true)
                captured.Add(new(row.Id, row.CategoryId, row.DisplayOrder?.Invoke() ?? row.Order));
        return captured.OrderBy(row => row.CategoryId, StringComparer.Ordinal).ThenBy(row => row.Order)
            .ThenBy(row => row.Id, StringComparer.Ordinal).ToArray();
    }
}

public sealed record SettingsRowPlacement(string Id, string CategoryId, int Order);
