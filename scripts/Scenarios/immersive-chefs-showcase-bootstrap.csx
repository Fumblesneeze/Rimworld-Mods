new System.Func<string>(() =>
{
    Find.TickManager.Pause();
    return "ready";
})()
