using System.IO;

namespace RimWorldDevGateway.Client;

public sealed class GatewayClientFileSystem : IGatewayClientFileSystem
{
    public string ReadAllText(string path) => File.ReadAllText(path);

    public void WriteAllBytes(string path, byte[] contents) => File.WriteAllBytes(path, contents);
}
