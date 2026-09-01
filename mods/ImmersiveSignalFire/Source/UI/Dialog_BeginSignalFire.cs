using System;
using System.Collections.Generic;
using System.Linq;
using ImmersiveSignalFire.Buildings;
using ImmersiveSignalFire.Effects;
using ImmersiveSignalFire.Signals;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ImmersiveSignalFire.UI;

public sealed class Dialog_BeginSignalFire : Window
{
    private readonly Building_SignalFire fire;
    private readonly Faction faction;
    private readonly SignalParticipantWidget participantWidget;
    private Vector2 participantScroll;

    public Dialog_BeginSignalFire(Building_SignalFire fire, Faction faction, Pawn? lockedCaller)
        : this(fire, faction, new SignalParticipantWidget(fire, lockedCaller))
    {
    }

    private Dialog_BeginSignalFire(
        Building_SignalFire fire,
        Faction faction,
        SignalParticipantWidget participantWidget)
    {
        this.fire = fire;
        this.faction = faction;
        this.participantWidget = participantWidget;
        closeOnClickedOutside = true;
        forcePause = true;
        absorbInputAroundWindow = true;
        closeOnAccept = false;
    }

    public override Vector2 InitialSize => new(850f, 620f);

    public override void DoWindowContents(Rect inRect)
    {
        float y = 0f;
        using (new TextBlock(GameFont.Medium))
        {
            Widgets.Label(
                new Rect(0f, y, inRect.width, 36f),
                "ImmersiveSignalFire_DialogTitle".Translate(faction.Name));
        }

        y += 42f;
        string description = "ImmersiveSignalFire_DialogDescription".Translate();
        float descriptionHeight = Text.CalcHeight(description, inRect.width);
        Widgets.Label(new Rect(0f, y, inRect.width, descriptionHeight), description);
        y += descriptionHeight + 8f;
        string outcomes = "ImmersiveSignalFire_OutcomeBands".Translate();
        float outcomesHeight = Text.CalcHeight(outcomes, inRect.width);
        Widgets.Label(new Rect(0f, y, inRect.width, outcomesHeight), outcomes);
        y += outcomesHeight + 12f;

        const float buttonHeight = 40f;
        const float warningHeight = 28f;
        float bodyHeight = Math.Max(150f, inRect.height - y - warningHeight - buttonHeight - 18f);
        Rect participantOut = new(0f, y, 365f, bodyHeight);
        Rect participantView = new(
            0f,
            0f,
            participantOut.width - 16f,
            Math.Max(bodyHeight, participantWidget.ContentHeight));
        Widgets.BeginScrollView(participantOut, ref participantScroll, participantView);
        participantWidget.DrawPawnList(participantView);
        Widgets.EndScrollView();

        DrawQualityPanel(new Rect(390f, y, inRect.width - 390f, bodyHeight));

        string? issue = BlockingIssue();
        using (new TextBlock(TextAnchor.MiddleRight, ColorLibrary.RedReadable))
        {
            Widgets.Label(
                new Rect(0f, inRect.height - buttonHeight - warningHeight - 8f, inRect.width, warningHeight),
                issue ?? string.Empty);
        }

        Rect cancelRect = new(inRect.width - 410f, inRect.height - buttonHeight, 200f, buttonHeight);
        Rect beginRect = new(inRect.width - 200f, inRect.height - buttonHeight, 200f, buttonHeight);
        if (Widgets.ButtonText(cancelRect, "CancelButton".Translate()))
        {
            Close(doCloseSound: true);
        }

        bool canBegin = issue is null;
        using (new TextBlock(canBegin ? Color.white : Color.gray))
        {
            if (Widgets.ButtonText(
                    beginRect,
                    "ImmersiveSignalFire_Begin".Translate(),
                    drawBackground: true,
                    doMouseoverSound: true,
                    active: canBegin))
            {
                TryStart();
            }
        }
    }

    public override void WindowUpdate()
    {
        base.WindowUpdate();
        participantWidget.WindowUpdate();
    }

    public override void OnAcceptKeyPressed()
    {
        TryStart();
    }

    private void DrawQualityPanel(Rect rect)
    {
        float averageMelee = participantWidget.Selected.Count == 0
            ? 0f
            : (float)participantWidget.Selected.Average(pawn => SignalParticipantUtility.SkillLevel(pawn, SkillDefOf.Melee));
        float averageSocial = participantWidget.Selected.Count == 0
            ? 0f
            : (float)participantWidget.Selected.Average(pawn => SignalParticipantUtility.SkillLevel(pawn, SkillDefOf.Social));
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, 30f), "QualityFactors".Translate());
        DrawFactorRow(rect, rect.y + 32f, "ImmersiveSignalFire_MeleeFactor".Translate(), averageMelee, even: true);
        DrawFactorRow(rect, rect.y + 60f, "ImmersiveSignalFire_SocialFactor".Translate(), averageSocial, even: false);

        Widgets.DrawBox(new Rect(rect.x - 9f, rect.y - 9f, rect.width + 18f, 132f), 2);
        Widgets.Label(new Rect(rect.x, rect.y + 94f, rect.width, 30f), "ImmersiveSignalFire_Quality".Translate());
        using (new TextBlock(GameFont.Medium, TextAnchor.UpperRight))
        {
            Widgets.Label(
                new Rect(rect.x, rect.y + 88f, rect.width, 36f),
                participantWidget.Quality.ToStringPercent("F0"));
        }

        Widgets.Label(
            new Rect(rect.x, rect.y + 148f, rect.width, 30f),
            "ImmersiveSignalFire_DurationValue".Translate(
                MorseCadence.TotalDurationTicks / 60,
                MorseCadence.TotalDurationTicks));
    }

    private static void DrawFactorRow(Rect panel, float y, TaggedString label, float average, bool even)
    {
        Rect row = new(panel.x, y, panel.width, 28f);
        if (even)
        {
            Widgets.DrawLightHighlight(row);
        }

        Widgets.Label(new Rect(row.x + 8f, row.y, 205f, row.height), label);
        using (new TextBlock(TextAnchor.UpperRight, ColorLibrary.Green))
        {
            Widgets.Label(row, $"{average:0.#} / 20");
        }
    }

    private string? BlockingIssue()
    {
        if (participantWidget.Selected.Count == 0)
        {
            return "ImmersiveSignalFire_NeedCaller".Translate();
        }

        return participantWidget.Selected.Any(pawn => !SignalParticipantUtility.IsEligible(pawn, fire))
            ? "ImmersiveSignalFire_InvalidParticipants".Translate()
            : null;
    }

    private void TryStart()
    {
        if (BlockingIssue() is not null)
        {
            return;
        }

        if (!fire.SignalComp.TryBegin(
                faction,
                participantWidget.Selected,
                participantWidget.Quality,
                out string rejection))
        {
            Messages.Message(rejection, fire, MessageTypeDefOf.RejectInput);
            return;
        }

        Close(doCloseSound: true);
    }
}

internal static class SignalParticipantUtility
{
    public static IReadOnlyList<Pawn> EligibleFor(Building_SignalFire fire)
    {
        if (fire.Map is not { } map)
        {
            return Array.Empty<Pawn>();
        }

        return map.mapPawns.FreeColonistsSpawned
            .Where(pawn => IsEligible(pawn, fire))
            .OrderByDescending(Score)
            .ThenBy(pawn => pawn.LabelShort, StringComparer.Ordinal)
            .ToArray();
    }

    public static bool IsEligible(Pawn pawn, Building_SignalFire fire) =>
        pawn is not null && pawn.Spawned && pawn.Map == fire.Map && pawn.IsColonistPlayerControlled &&
        pawn.ageTracker.Adult && !pawn.Dead && !pawn.Downed && !pawn.InMentalState &&
        pawn.CanReach(fire, PathEndMode.Touch, Danger.Some);

    public static int SkillLevel(Pawn pawn, SkillDef skill)
    {
        SkillRecord? record = pawn.skills?.GetSkill(skill);
        return record is null || record.TotallyDisabled ? 0 : record.Level;
    }

    public static float Score(Pawn pawn) => SignalQuality.Individual(
        SkillLevel(pawn, SkillDefOf.Melee),
        SkillLevel(pawn, SkillDefOf.Social));
}

internal sealed class SignalParticipantWidget : IPawnRoleSelectionWidget
{
    private readonly Pawn? lockedCaller;
    private readonly List<Pawn> eligible;
    private readonly List<Pawn> selected;
    private Pawn? caller;

    public SignalParticipantWidget(Building_SignalFire fire, Pawn? lockedCaller)
    {
        eligible = SignalParticipantUtility.EligibleFor(fire).ToList();
        this.lockedCaller = lockedCaller;
        caller = this.lockedCaller ?? eligible.FirstOrDefault();
        selected = caller is null
            ? new List<Pawn>()
            : new[] { caller }.Concat(eligible.Where(pawn => pawn != caller).Take(2)).ToList();
    }

    public IReadOnlyList<Pawn> Selected => selected;

    public float ContentHeight => Math.Max(44f, eligible.Count * 44f);

    public float Quality => selected.Count == 0
        ? 0f
        : SignalQuality.Group(selected.Select(pawn => new SignalSkills(
            SignalParticipantUtility.SkillLevel(pawn, SkillDefOf.Melee),
            SignalParticipantUtility.SkillLevel(pawn, SkillDefOf.Social))));

    public void DrawPawnList(Rect rect)
    {
        Widgets.BeginGroup(rect);
        float y = 0f;
        foreach (Pawn pawn in eligible)
        {
            Rect row = new(0f, y, rect.width, 42f);
            if (Mouse.IsOver(row))
            {
                Widgets.DrawHighlight(row);
            }

            bool included = selected.Contains(pawn);
            bool previous = included;
            Widgets.Checkbox(new Vector2(4f, y + 8f), ref included, 24f, pawn == lockedCaller);
            if (included != previous)
            {
                ToggleParticipant(pawn, included);
            }

            bool isCaller = caller == pawn;
            bool callerDisabled = lockedCaller is not null && lockedCaller != pawn;
            if (Widgets.RadioButton(new Vector2(36f, y + 9f), isCaller, callerDisabled) && !callerDisabled)
            {
                SelectCaller(pawn);
            }

            string role = isCaller
                ? "ImmersiveSignalFire_Caller".Translate()
                : "ImmersiveSignalFire_Helper".Translate();
            Widgets.Label(
                new Rect(64f, y, rect.width - 64f, 24f),
                $"{pawn.LabelShortCap} — {role}");
            Widgets.Label(
                new Rect(64f, y + 20f, rect.width - 64f, 22f),
                "ImmersiveSignalFire_PawnSkillSummary".Translate(
                    SignalParticipantUtility.SkillLevel(pawn, SkillDefOf.Melee),
                    SignalParticipantUtility.SkillLevel(pawn, SkillDefOf.Social),
                    SignalParticipantUtility.Score(pawn).ToStringPercent()));
            y += 44f;
        }

        Widgets.EndGroup();
    }

    public void WindowUpdate()
    {
    }

    private void ToggleParticipant(Pawn pawn, bool include)
    {
        if (include)
        {
            AddParticipant(pawn);
            return;
        }

        if (pawn == lockedCaller)
        {
            return;
        }

        selected.Remove(pawn);
        if (caller == pawn)
        {
            caller = selected.FirstOrDefault();
        }
    }

    private void AddParticipant(Pawn pawn)
    {
        if (selected.Contains(pawn))
        {
            return;
        }

        if (selected.Count >= 3)
        {
            Messages.Message("ImmersiveSignalFire_TooMany".Translate(), MessageTypeDefOf.RejectInput);
            return;
        }

        selected.Add(pawn);
        caller ??= pawn;
    }

    private void SelectCaller(Pawn pawn)
    {
        IReadOnlyList<Pawn> updated = ParticipantSelectionPolicy.SelectCaller(
            selected,
            caller,
            pawn,
            maximumParticipants: 3);
        selected.Clear();
        selected.AddRange(updated);

        if (selected.Contains(pawn))
        {
            caller = pawn;
        }
    }
}
