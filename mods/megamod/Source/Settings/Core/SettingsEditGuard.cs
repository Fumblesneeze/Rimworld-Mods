namespace Megamod.Settings;

// Feature eligibility is derived on the UI thread; this guard owns no setting value.
public sealed class SettingsEditGuard
{
    private readonly Func<string?> unavailableReasonKey;

    public SettingsEditGuard(Func<string?> unavailableReasonKey) => this.unavailableReasonKey = unavailableReasonKey;

    public string? UnavailableReasonKey => unavailableReasonKey();

    public SettingEditResult Commit(Func<SettingEditResult> commit)
    {
        string? reason = unavailableReasonKey();
        return reason == null ? commit() : new(false, reason);
    }
}
