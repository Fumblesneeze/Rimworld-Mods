namespace ImmersiveChefs;

public readonly struct CookingWorkPropState
{
    public CookingWorkPropState(
        bool currentJobMatches,
        bool currentDriverIsDoBill,
        bool workStarted,
        bool productsCompleted,
        bool cookwareExists,
        bool cookwareHeldByCook)
    {
        CurrentJobMatches = currentJobMatches;
        CurrentDriverIsDoBill = currentDriverIsDoBill;
        WorkStarted = workStarted;
        ProductsCompleted = productsCompleted;
        CookwareExists = cookwareExists;
        CookwareHeldByCook = cookwareHeldByCook;
    }

    public bool CurrentJobMatches { get; }
    public bool CurrentDriverIsDoBill { get; }
    public bool WorkStarted { get; }
    public bool ProductsCompleted { get; }
    public bool CookwareExists { get; }
    public bool CookwareHeldByCook { get; }
}

public static class CookingWorkPropPolicy
{
    public static bool ShouldDraw(CookingWorkPropState state) =>
        state.CurrentJobMatches &&
        state.CurrentDriverIsDoBill &&
        state.WorkStarted &&
        !state.ProductsCompleted &&
        state.CookwareExists &&
        state.CookwareHeldByCook;
}
