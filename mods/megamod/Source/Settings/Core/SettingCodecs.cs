using System.Globalization;

namespace Megamod.Settings;

public interface ISettingCodec<T>
{
    string Encode(T value);
    bool TryDecode(string encoded, out T value);
}

public static class SettingCodecs
{
    public static ISettingCodec<bool> Boolean { get; } = new BooleanCodec();
    public static ISettingCodec<int> Int32 { get; } = new Int32Codec();

    private sealed class Int32Codec : ISettingCodec<int>
    {
        public string Encode(int value) => value.ToString(CultureInfo.InvariantCulture);
        public bool TryDecode(string encoded, out int value) => int.TryParse(encoded, NumberStyles.Integer,
            CultureInfo.InvariantCulture, out value);
    }

    private sealed class BooleanCodec : ISettingCodec<bool>
    {
        public string Encode(bool value) => value ? "true" : "false";
        public bool TryDecode(string encoded, out bool value) => bool.TryParse(encoded, out value);
    }
}
