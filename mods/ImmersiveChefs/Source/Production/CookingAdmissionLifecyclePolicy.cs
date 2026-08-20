namespace ImmersiveChefs;

internal enum CookingAdmissionPhase
{
    CandidateEvaluation,
    AcceptedDriverStart
}

internal static class CookingAdmissionLifecyclePolicy
{
    internal static bool MayReserveWare(
        CookingAdmissionPhase phase,
        bool driverJobIsCurrent) =>
        phase == CookingAdmissionPhase.AcceptedDriverStart && driverJobIsCurrent;
}
