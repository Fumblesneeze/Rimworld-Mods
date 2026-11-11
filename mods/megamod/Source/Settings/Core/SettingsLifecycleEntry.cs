namespace Megamod.Settings;

// Main-thread entry shared by the pump and UI reads. It does not dispatch worker requests.
public sealed class SettingsLifecycleEntry
{
    private bool running;

    public void Run(Action reconcile)
    {
        if (running) return;
        running = true;
        try { reconcile(); }
        finally { running = false; }
    }
}
