using Megamod.Settings;
using UnityEngine;
using Verse;

namespace Megamod.Features.ContentSelection.Runtime;

// Reuses the common scoped editors and store; the window owns only its browsing presentation.
public sealed class ContentSelectionWindow : Window
{
    private readonly SettingsRenderer renderer;

    public ContentSelectionWindow(SettingsRenderer renderer)
    {
        this.renderer = renderer;
        doCloseX = true;
        doCloseButton = true;
        absorbInputAroundWindow = true;
        forcePause = true;
    }

    public override Vector2 InitialSize => new(900, 700);

    public override void DoWindowContents(Rect inRect)
    {
        var oldFont = Text.Font;
        try
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 34), "Megamod_Content_Category".Translate());
        }
        finally { Text.Font = oldFont; }
        renderer.Draw(new Rect(inRect.x, inRect.y + 40, inRect.width, Mathf.Max(0, inRect.height - 80)));
    }
}
