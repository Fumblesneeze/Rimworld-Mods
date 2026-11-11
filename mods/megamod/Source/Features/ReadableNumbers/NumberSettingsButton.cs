using UnityEngine;
using Verse;

namespace Megamod.Features.ReadableNumbers;

internal static class NumberSettingsButton
{
    public static bool Draw(Rect rect, string label, bool enabled)
    {
        Color previousColor = GUI.color;
        TextAnchor previousAnchor = Text.Anchor;
        bool previousWrap = Text.WordWrap;
        try
        {
            if (enabled) return Widgets.ButtonText(rect, label);

            // Native ButtonText(active:false) still draws hover/pressed feedback and plays sounds.
            GUI.color = previousColor * Widgets.InactiveColor;
            Widgets.DrawAtlas(rect, Widgets.ButtonBGAtlas);
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.WordWrap = false;
            Widgets.Label(rect, label);
            return false;
        }
        finally
        {
            GUI.color = previousColor;
            Text.Anchor = previousAnchor;
            Text.WordWrap = previousWrap;
        }
    }
}
