using UnityEngine;

namespace Megamod.Settings;

/// <summary>A native editor for one complete setting value. Persistence and scope stay in the renderer.</summary>
public interface ISettingsCompositeEditor<T>
{
    string? Draw(Rect rect, T value, bool enabled, Func<T, SettingEditResult> commit);
    void Reset();
}
