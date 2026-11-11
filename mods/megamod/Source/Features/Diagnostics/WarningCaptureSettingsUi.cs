using System;
using Megamod.Settings;
using UnityEngine;
using Verse;

namespace Megamod.Features.Diagnostics;

public static class WarningCaptureSettingsUi
{
    public static void Register(SettingsRenderer renderer, WarningCaptureRuntime runtime) => renderer.AddComposite(
        WarningCaptureSettings.MuteWarnings, "Megamod_Diagnostics_MuteWarnings", "Megamod_Diagnostics_CaptureCategory",
        "Megamod_Diagnostics_MuteWarningsHelp", 100, (_, global) => new Editor(runtime, global),
        "Megamod_Diagnostics_CaptureGlobalOnlyReason");

    private sealed class Editor : ISettingsCompositeEditor<bool>
    {
        private readonly WarningCaptureRuntime runtime;
        private readonly bool global;
        public Editor(WarningCaptureRuntime runtime, bool global) { this.runtime = runtime; this.global = global; }
        public void Reset() { }

        public string? Draw(Rect rect, bool value, bool enabled, Func<bool, SettingEditResult> commit)
        {
            bool edited = value;
            Widgets.CheckboxLabeled(new Rect(rect.x, rect.y, rect.width, 28),
                (value ? "Megamod_Settings_On" : "Megamod_Settings_Off").Translate(), ref edited, disabled: !enabled);
            string? error = enabled && edited != value ? commit(edited).ErrorKey : null;
            if (global)
            {
                bool previousWrap = Text.WordWrap;
                try
                {
                    Text.WordWrap = true;
                    Widgets.Label(new Rect(rect.x, rect.y + 32, rect.width, 68), runtime.StatusKey.Translate());
                }
                finally { Text.WordWrap = previousWrap; }
            }
            return error;
        }
    }
}
