using System;
using System.Globalization;
using Megamod.Settings;
using UnityEngine;
using Verse;

namespace Megamod.Features.MessagePlacement;

public sealed class MessageOffsetsEditor : ISettingsCompositeEditor<MessageOffsets>
{
    private readonly MessageOffsetDraft draft = new();
    public void Reset() => draft.Reset();

    public string? Draw(Rect rect, MessageOffsets value, bool enabled, Func<MessageOffsets, SettingEditResult> commit)
    {
        CultureInfo culture = CultureInfo.CurrentCulture;
        draft.Synchronize(value, culture);
        Color color = GUI.color;
        GameFont font = Text.Font;
        TextAnchor anchor = Text.Anchor;
        bool wrap = Text.WordWrap;
        try
        {
            Text.Font = GameFont.Small; Text.Anchor = TextAnchor.UpperLeft; Text.WordWrap = false;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 24), "Megamod_Messages_Horizontal".Translate());
            Widgets.Label(new Rect(rect.x, rect.y + 56, rect.width, 24), "Megamod_Messages_Vertical".Translate());
            var horizontal = new Rect(rect.x, rect.y + 24, rect.width, 28);
            var vertical = new Rect(rect.x, rect.y + 80, rect.width, 28);
            if (enabled)
            {
                string x = Widgets.TextField(horizontal, draft.Horizontal);
                string y = Widgets.TextField(vertical, draft.Vertical);
                if (x != draft.Horizontal || y != draft.Vertical) draft.Edit(x, y, culture);
            }
            else
            {
                Inactive(horizontal, value.HorizontalPercent.ToString("G", culture), false);
                Inactive(vertical, value.VerticalPercent.ToString("G", culture), false);
            }
            bool canApply = enabled && draft.CanApply(culture);
            if (Button(new Rect(rect.x, rect.y + 116, rect.width, 28), "Megamod_Messages_ApplyPair".Translate(), canApply) && canApply)
                draft.Apply(enabled, culture, commit);
            var reset = new Rect(rect.x, rect.y + 148, rect.width, 28);
            if (Button(reset, "Megamod_Messages_ResetPair".Translate(), enabled) && enabled)
                draft.ResetValue(enabled, culture, commit);
            TooltipHandler.TipRegion(reset, "Megamod_Messages_ResetPairHelp".Translate());
            return enabled ? draft.ErrorKey : null;
        }
        finally { GUI.color = color; Text.Font = font; Text.Anchor = anchor; Text.WordWrap = wrap; }
    }

    private static bool Button(Rect rect, string label, bool enabled)
    {
        if (enabled) return Widgets.ButtonText(rect, label);
        Inactive(rect, label, true);
        return false;
    }

    private static void Inactive(Rect rect, string label, bool centered)
    {
        Color color = GUI.color;
        TextAnchor anchor = Text.Anchor;
        try
        {
            GUI.color = color * Widgets.InactiveColor;
            Widgets.DrawBox(rect);
            Text.Anchor = centered ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rect.x + 4, rect.y, Math.Max(0, rect.width - 8), rect.height), label);
        }
        finally { GUI.color = color; Text.Anchor = anchor; }
    }
}
