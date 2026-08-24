using System;

namespace GuestBedGizmo.Beds;

internal static class PatchInstallTransaction
{
    internal static bool TryApply(
        Action apply,
        Action rollback,
        out string? failure)
    {
        failure = null;
        try
        {
            apply();
            return true;
        }
        catch (Exception exception)
        {
            string applyFailure = exception.GetBaseException().Message;
            try
            {
                rollback();
                failure = applyFailure;
            }
            catch (Exception rollbackException)
            {
                failure = applyFailure + "; rollback also failed: " +
                          rollbackException.GetBaseException().Message;
            }

            return false;
        }
    }
}
