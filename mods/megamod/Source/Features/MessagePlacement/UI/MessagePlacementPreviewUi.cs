using System;
using Megamod.Settings;
using UnityEngine;
using Verse;

namespace Megamod.Features.MessagePlacement;

// Stateless diagram: it does not create Message, Window, Archive or sound objects.
internal sealed class MessagePlacementPreviewUi
{
    private readonly SettingsStore settings;
    private readonly Func<bool> placementAvailable;
    private readonly Func<MessageBarBoundsReceipt?>? barBounds;

    internal MessagePlacementPreviewUi(SettingsStore settings, Func<bool> placementAvailable, Func<MessageBarBoundsReceipt?>? barBounds = null)
    {
        this.settings = settings;
        this.placementAvailable = placementAvailable;
        this.barBounds = barBounds;
    }

    internal void Draw(Rect rect)
    {
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.WordWrap = true;
        int width = UI.screenWidth, height = UI.screenHeight;
        // This is an explicitly labeled sample, not speculative measurement of live Messages.
        Vector2 origin = Messages.MessagesTopLeftStandard;
        if (Current.Game != null && Find.ActiveLesson.ActiveLessonVisible)
            origin.y += Find.ActiveLesson.Current.MessagesYOffset;
        var samples = new[]
        {
            new MessageBounds(origin.x, origin.y, 280, 26),
            new MessageBounds(origin.x, origin.y + 26, 340, 26),
            new MessageBounds(origin.x, origin.y + 52, 220, 26)
        };
        var preview = MessagePlacementPreview.Capture(settings, samples, width, height, placementAvailable(), barBounds?.Invoke());
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, 28), preview.ScopeKey.Translate());
        float statusHeight = Math.Max(34, Text.CalcHeight(preview.StatusKey.Translate(), rect.width));
        Widgets.Label(new Rect(rect.x, rect.y + 28, rect.width, statusHeight), preview.StatusKey.Translate());
        var available = new Rect(rect.x, rect.y + 32 + statusHeight, rect.width, Math.Max(0, rect.height - statusHeight - 60));
        if (width > 0 && height > 0 && available.width > 0 && available.height > 0)
        {
            float scale = Math.Min(available.width / width, available.height / height);
            var viewport = new Rect(available.x + (available.width - width * scale) / 2,
                available.y, width * scale, height * scale);
            Widgets.DrawBoxSolid(viewport, new Color(0.08f, 0.09f, 0.11f));
            Widgets.DrawBox(viewport);
            foreach (var sample in preview.Messages)
            {
                var shape = new Rect(viewport.x + (float)sample.X * scale, viewport.y + (float)sample.Y * scale,
                    (float)sample.Width * scale, (float)sample.Height * scale);
                // The scaled viewport is a diagram of native clipping, not another scrollable renderer.
                float left = Math.Max(viewport.x, shape.x), top = Math.Max(viewport.y, shape.y);
                float right = Math.Min(viewport.xMax, shape.xMax), bottom = Math.Min(viewport.yMax, shape.yMax);
                if (right > left && bottom > top)
                    Widgets.DrawBoxSolid(new Rect(left, top, right - left, bottom - top), new Color(0.57f, 0.77f, 0.88f));
            }
        }
        Text.Font = GameFont.Tiny;
        Widgets.Label(new Rect(rect.x, rect.yMax - 24, rect.width, 24), "Megamod_Messages_PreviewSample".Translate());
    }
}
