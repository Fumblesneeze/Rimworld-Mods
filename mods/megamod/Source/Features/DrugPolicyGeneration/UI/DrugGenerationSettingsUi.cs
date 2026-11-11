using System;
using System.Globalization;
using Megamod.Features.DrugPolicyGeneration.Core;
using Megamod.Features.DrugTolerance.Core;
using Megamod.Settings;
using UnityEngine;
using Verse;

namespace Megamod.Features.DrugPolicyGeneration;

public static class DrugGenerationSettingsUi
{
    private const string Category = "Megamod_DrugGeneration_Category";
    private const string Help = "Megamod_DrugGeneration_PreferenceHelp";
    public static void Register(SettingsRenderer renderer)
    {
        renderer.AddBoolean(DrugGenerationSettings.Enabled, "Megamod_DrugGeneration_Enabled", Category,
            "Megamod_DrugGeneration_EnabledHelp");
        renderer.AddComposite<float>(DrugGenerationSettings.InitialMood, "Megamod_DrugGeneration_InitialMood", Category,
            Help, 96, (_, _) => new PreferenceEditor(false));
        renderer.AddComposite<float>(DrugGenerationSettings.InitialJoy, "Megamod_DrugGeneration_InitialJoy", Category,
            Help, 96, (_, _) => new PreferenceEditor(false));
        renderer.AddComposite<DrugToleranceChoice>(DrugGenerationSettings.InitialTolerance,
            "Megamod_DrugGeneration_InitialTolerance", Category, "Megamod_DrugGeneration_ToleranceHelp",
            96, (_, _) => new PreferenceEditor(true));
    }

    // One presentation for the three percentage preferences, using the existing scoped commit.
    private sealed class PreferenceEditor : ISettingsCompositeEditor<float>, ISettingsCompositeEditor<DrugToleranceChoice>
    {
        private readonly bool allowAny;
        private readonly DrugGenerationPreferenceDraft draft;
        internal PreferenceEditor(bool allowAny) { this.allowAny = allowAny; draft = new(allowAny); }
        public void Reset() => draft.Reset();
        string? ISettingsCompositeEditor<float>.Draw(Rect rect, float value, bool enabled, Func<float, SettingEditResult> commit) =>
            Draw(rect, DrugToleranceChoice.Numeric(value), enabled, choice => commit(choice.Limit));
        string? ISettingsCompositeEditor<DrugToleranceChoice>.Draw(Rect rect, DrugToleranceChoice value, bool enabled,
            Func<DrugToleranceChoice, SettingEditResult> commit) => Draw(rect, value, enabled, commit);

        private string? Draw(Rect rect, DrugToleranceChoice value, bool enabled, Func<DrugToleranceChoice, SettingEditResult> commit)
        {
            draft.Synchronize(value, CultureInfo.CurrentCulture);
            var font = Text.Font; var anchor = Text.Anchor; bool wrap = Text.WordWrap;
            bool guiEnabled = GUI.enabled; var color = GUI.color;
            bool editable = enabled && guiEnabled;
            try
            {
                Text.Font = GameFont.Small; Text.Anchor = TextAnchor.UpperLeft; Text.WordWrap = false;
                GUI.enabled = editable;
                bool any = draft.Any;
                if (allowAny)
                {
                    Widgets.CheckboxLabeled(new Rect(rect.x, rect.y, rect.width, 24), "Megamod_DrugGeneration_Any".Translate(), ref any);
                    if (editable && any != draft.Any) draft.Edit(any, draft.Percentage);
                }
                else Widgets.Label(new Rect(rect.x, rect.y, rect.width, 24), "Megamod_DrugGeneration_Percentage".Translate());
                GUI.enabled = editable && !draft.Any;
                string text = Widgets.TextField(new Rect(rect.x, rect.y + 28, Math.Max(0, rect.width - 28), 28), draft.Percentage);
                if (editable && !draft.Any && text != draft.Percentage) draft.Edit(false, text);
                Widgets.Label(new Rect(rect.xMax - 24, rect.y + 28, 24, 28), "%");
                bool canApply = editable && draft.CanApply;
                GUI.enabled = canApply;
                if (Widgets.ButtonText(new Rect(rect.x, rect.y + 64, rect.width, 28), "Megamod_Settings_Apply".Translate()) && canApply)
                    draft.Apply(editable, commit);
                return editable ? draft.ErrorKey : null;
            }
            finally { Text.Font = font; Text.Anchor = anchor; Text.WordWrap = wrap; GUI.enabled = guiEnabled; GUI.color = color; }
        }
    }
}
