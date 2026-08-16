namespace ImmersiveChefs;

internal static class DiningReservationSafety
{
    internal static bool KeepNativeResult(bool nativeReservationSucceeded, bool diningAttachmentSucceeded)
    {
        _ = diningAttachmentSucceeded;
        return nativeReservationSucceeded;
    }
}
