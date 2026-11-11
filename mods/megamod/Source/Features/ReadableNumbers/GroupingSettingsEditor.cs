using Megamod.Settings;
using UnityEngine;
using Verse;

namespace Megamod.Features.ReadableNumbers;

public sealed class GroupingSettingsEditor : ISettingsCompositeEditor<NumberGrouping>
{
    public void Reset() { }

    public string? Draw(Rect rect, NumberGrouping value, bool enabled, Func<NumberGrouping, SettingEditResult> commit)
    {
        string key = value switch
        {
            NumberGrouping.ThreeThenTwoDigits => "Megamod_Numbers_GroupingThreeThenTwo",
            NumberGrouping.FourDigits => "Megamod_Numbers_GroupingFour",
            NumberGrouping.None => "Megamod_Numbers_GroupingNone",
            _ => "Megamod_Numbers_GroupingThree"
        };
        if (NumberSettingsButton.Draw(rect, key.Translate(), enabled) && enabled)
            return commit((NumberGrouping)(((int)value + 1) % 4)).ErrorKey;
        return null;
    }
}
