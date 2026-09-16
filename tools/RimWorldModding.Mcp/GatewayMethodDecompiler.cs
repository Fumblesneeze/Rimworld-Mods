using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;

namespace RimWorldModding.Mcp;

public sealed record GatewayDecompiledMethod(string Code, string InputSha256, string ModuleMvid, int MetadataToken);

public static class GatewayMethodDecompiler
{
    public const int MaximumAssemblyBytes = 64 * 1024 * 1024;

    public static GatewayDecompiledMethod Decompile(byte[] bytes, Guid? expectedMvid, int token,
        string assemblyPath, CancellationToken cancellationToken)
    {
        if (bytes.Length == 0 || bytes.Length > MaximumAssemblyBytes)
            throw new ArgumentException("Assembly input must be nonempty and at most 64 MiB.");
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new MemoryStream(bytes, writable: false);
        using var pe = new PEFile(assemblyPath, stream, PEStreamOptions.PrefetchEntireImage);
        var metadata = pe.Metadata;
        var mvid = metadata.GetGuid(metadata.GetModuleDefinition().Mvid);
        if (expectedMvid.HasValue && mvid != expectedMvid.Value)
            throw new ArgumentException("Assembly bytes no longer match the module loaded in the selected game process.");
        var handle = MetadataTokens.EntityHandle(token);
        if (handle.Kind != HandleKind.MethodDefinition || MetadataTokens.GetRowNumber(handle) < 1 ||
            MetadataTokens.GetRowNumber(handle) > metadata.MethodDefinitions.Count)
            throw new ArgumentException("Token must identify a method definition in the exact module.");
        var resolver = new UniversalAssemblyResolver(assemblyPath, false, ".NETFramework,Version=v4.8");
        resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath)!);
        var decompiler = new CSharpDecompiler(pe, resolver, new DecompilerSettings
            { ThrowOnAssemblyResolveErrors = false }) { CancellationToken = cancellationToken };
        var code = decompiler.DecompileAsString(handle);
        if (code.Length > 1024 * 1024) throw new InvalidOperationException("Decompiled method exceeds the 1 Mi-character output policy bound.");
        return new GatewayDecompiledMethod(code, Convert.ToHexString(SHA256.HashData(bytes)), mvid.ToString("D"), token);
    }
}
