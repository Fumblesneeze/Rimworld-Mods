using System.Net;
using System.Text;

namespace RimWorldDevGateway;

public static class GatewayAuthenticator
{
    private const string BearerPrefix = "Bearer ";

    public static bool Authorize(IPEndPoint? remoteEndPoint, string? authorizationHeader, string expectedToken)
    {
        if (remoteEndPoint is null || !IPAddress.Loopback.Equals(remoteEndPoint.Address))
        {
            return false;
        }

        if (expectedToken.Length == 0 ||
            authorizationHeader is null ||
            authorizationHeader.Length == 0 ||
            !authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var suppliedToken = authorizationHeader.Substring(BearerPrefix.Length);
        return FixedTimeEquals(suppliedToken, expectedToken);
    }

    private static bool FixedTimeEquals(string supplied, string expected)
    {
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var difference = suppliedBytes.Length ^ expectedBytes.Length;
        var length = Math.Max(suppliedBytes.Length, expectedBytes.Length);

        for (var index = 0; index < length; index++)
        {
            var suppliedByte = index < suppliedBytes.Length ? suppliedBytes[index] : (byte)0;
            var expectedByte = index < expectedBytes.Length ? expectedBytes[index] : (byte)0;
            difference |= suppliedByte ^ expectedByte;
        }

        return difference == 0;
    }
}
