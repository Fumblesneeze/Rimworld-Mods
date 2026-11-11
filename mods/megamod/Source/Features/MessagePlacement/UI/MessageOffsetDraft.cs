using System;
using System.Globalization;
using Megamod.Settings;

namespace Megamod.Features.MessagePlacement;

// Transient UI text only; common settings own every persisted value and scope decision.
public sealed class MessageOffsetDraft
{
    private MessageOffsets? stored;
    private string? cultureName;
    public string Horizontal { get; private set; } = "0";
    public string Vertical { get; private set; } = "0";
    public string? ErrorKey { get; private set; }
    public void Synchronize(MessageOffsets value, CultureInfo culture)
    {
        if (stored != value || cultureName != culture.Name) SetDraft(value, culture);
    }
    public void Edit(string horizontal, string vertical, CultureInfo culture)
    {
        Horizontal = horizontal; Vertical = vertical;
        ErrorKey = CanApply(culture) ? null : "Megamod_Messages_InvalidOffsetPair";
    }
    public bool CanApply(CultureInfo culture) => MessageOffsetInput.TryParse(Horizontal, Vertical, culture, out _);
    public SettingEditResult Apply(bool enabled, CultureInfo culture, Func<MessageOffsets, SettingEditResult> commit)
    {
        if (!enabled) return new(false);
        if (!MessageOffsetInput.TryParse(Horizontal, Vertical, culture, out var value))
            return new(false, ErrorKey = "Megamod_Messages_InvalidOffsetPair");
        var result = commit(value);
        ErrorKey = result.ErrorKey;
        if (result.Succeeded) SetDraft(value, culture);
        return result;
    }
    public SettingEditResult ResetValue(bool enabled, CultureInfo culture, Func<MessageOffsets, SettingEditResult> commit)
    {
        if (!enabled) return new(false);
        var result = commit(default);
        ErrorKey = result.ErrorKey;
        if (result.Succeeded) SetDraft(default, culture);
        return result;
    }
    public void Reset() { stored = null; cultureName = null; ErrorKey = null; }
    private void SetDraft(MessageOffsets value, CultureInfo culture)
    {
        stored = value; cultureName = culture.Name;
        Horizontal = value.HorizontalPercent.ToString("G", culture);
        Vertical = value.VerticalPercent.ToString("G", culture);
        ErrorKey = null;
    }
}
