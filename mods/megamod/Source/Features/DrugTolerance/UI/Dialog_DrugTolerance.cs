using System;
using Megamod.Features.DrugTolerance.Core;
using UnityEngine;
using Verse;

namespace Megamod.Features.DrugTolerance;

// A native contextual draft. The policy is only changed by the session's current-context commit.
public sealed class Dialog_DrugTolerance : Window
{
    private DrugToleranceEditSession? session;
    private Func<bool>? current;
    private readonly string title;
    private string? failureKey;

    public Dialog_DrugTolerance(string title, DrugToleranceEditSession session, Func<bool> current)
    {
        this.title = title; this.session = session; this.current = current;
        forcePause = true; doCloseX = true; closeOnCancel = true; closeOnAccept = false;
        absorbInputAroundWindow = true; closeOnClickedOutside = false;
    }
    public override Vector2 InitialSize
    {
        get
        {
            var font = Text.Font;
            try
            {
                Text.Font = GameFont.Small;
                float help = Text.CalcHeight("Megamod.DrugTolerance.RuleHelp".Translate(), 464);
                float reason = Math.Max(Text.CalcHeight("Megamod.DrugTolerance.Unavailable".Translate(), 464),
                    Text.CalcHeight("Megamod.DrugTolerance.InvalidPercentage".Translate(), 464));
                return new(500, Math.Max(300, 205 + help + reason));
            }
            finally { Text.Font = font; }
        }
    }
    public bool Any => session?.Draft.Any ?? false;
    public string Percentage => session?.Draft.Percentage ?? string.Empty;

    public bool SetAny(bool value)
    {
        if (!IsOpen || session == null || current?.Invoke() != true) return false;
        session.Draft.Any = value; return true;
    }
    public bool SetPercentage(string value)
    {
        if (!IsOpen || session == null || current?.Invoke() != true || session.Draft.Any) return false;
        session.Draft.Percentage = value; return true;
    }
    public bool TryApply()
    {
        if (!IsOpen || session == null || current?.Invoke() != true) return false;
        try
        {
            if (!session.TryApply()) return false;
            Close(); return true;
        }
        catch (Exception)
        {
            session.Dispose(); current = null;
            failureKey = "Megamod.DrugTolerance.Unavailable";
            return false;
        }
    }
    public void Cancel() => Close();
    public override void PostClose()
    {
        session?.Dispose(); session = null; current = null;
        base.PostClose();
    }
    public override void DoWindowContents(Rect inRect)
    {
        if (session == null) return;
        var font = Text.Font; var anchor = Text.Anchor; var color = GUI.color;
        bool enabled = GUI.enabled, wrap = Text.WordWrap;
        try
        {
            Text.Font = GameFont.Medium; Text.Anchor = TextAnchor.UpperLeft; Text.WordWrap = true;
            Widgets.LabelEllipses(new Rect(0, 0, inRect.width, 34), title);
            TooltipHandler.TipRegion(new Rect(0, 0, inRect.width, 34), title);
            Text.Font = GameFont.Small;
            string help = "Megamod.DrugTolerance.RuleHelp".Translate();
            float helpHeight = Text.CalcHeight(help, inRect.width), anyY = 50 + helpHeight, percentageY = anyY + 36;
            Widgets.Label(new Rect(0, 42, inRect.width, helpHeight), help);
            bool contextCurrent = current?.Invoke() == true;
            GUI.enabled = enabled && contextCurrent;
            bool any = session.Draft.Any;
            Widgets.CheckboxLabeled(new Rect(0, anyY, inRect.width, 28), "Megamod.DrugTolerance.Any".Translate(), ref any);
            if (any != session.Draft.Any) SetAny(any);
            Widgets.Label(new Rect(0, percentageY, inRect.width - 150, 30), "Megamod.DrugTolerance.Percentage".Translate());
            GUI.enabled = enabled && contextCurrent && !session.Draft.Any;
            GUI.SetNextControlName("Megamod.DrugTolerance.Percentage");
            string percentage = Widgets.TextField(new Rect(inRect.width - 140, percentageY, 140, 30), session.Draft.Percentage);
            if (percentage != session.Draft.Percentage) SetPercentage(percentage);
            GUI.enabled = enabled;
            string? reason = failureKey ?? (!contextCurrent ? "Megamod.DrugTolerance.Unavailable" :
                !session.Draft.TryGetChoice(out _) ? "Megamod.DrugTolerance.InvalidPercentage" : null);
            if (reason != null) Widgets.Label(new Rect(0, percentageY + 36, inRect.width, inRect.height - 35 - percentageY - 44), reason.Translate());
            GUI.enabled = enabled && session.CanApply;
            if (Widgets.ButtonText(new Rect(inRect.width / 2 + 5, inRect.height - 35, inRect.width / 2 - 5, 35), "Megamod_Settings_Apply".Translate())) TryApply();
            GUI.enabled = enabled;
            if (Widgets.ButtonText(new Rect(0, inRect.height - 35, inRect.width / 2 - 5, 35), "Cancel".Translate())) Cancel();
        }
        finally { Text.Font = font; Text.Anchor = anchor; Text.WordWrap = wrap; GUI.color = color; GUI.enabled = enabled; }
    }
}
