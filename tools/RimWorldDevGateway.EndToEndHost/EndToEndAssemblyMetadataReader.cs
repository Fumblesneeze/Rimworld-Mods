using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

namespace RimWorldDevGateway.EndToEndHost;

public static class EndToEndAssemblyMetadataReader
{
    private const string AttributeTypeName =
        "RimWorldDevGateway.EndToEndTesting.RimWorldEndToEndTestAttribute";
    private const string ContractTypeName =
        "RimWorldDevGateway.EndToEndTesting.IRimWorldEndToEndTest";
    private const string ContractAssemblyName = "RimWorldDevGateway.EndToEndTesting";

    public static EndToEndAssemblyMetadata Read(string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath))
        {
            throw new ArgumentException("An assembly path is required.", nameof(assemblyPath));
        }

        var fullPath = Path.GetFullPath(assemblyPath);
        if (!File.Exists(fullPath))
        {
            throw new EndToEndDiscoveryException($"E2E assembly does not exist: {fullPath}");
        }

        var assemblyBytes = File.ReadAllBytes(fullPath);
        var hash = Convert.ToHexString(SHA256.HashData(assemblyBytes)).ToLowerInvariant();
        using var stream = new MemoryStream(assemblyBytes, writable: false);
        using var peReader = new PEReader(stream, PEStreamOptions.PrefetchMetadata);
        if (!peReader.HasMetadata)
        {
            throw new EndToEndDiscoveryException($"E2E assembly has no CLR metadata: {fullPath}");
        }

        var reader = peReader.GetMetadataReader();
        if (!reader.IsAssembly)
        {
            throw new EndToEndDiscoveryException($"E2E output is not an assembly: {fullPath}");
        }

        var assemblyDefinition = reader.GetAssemblyDefinition();
        var assemblyName = reader.GetString(assemblyDefinition.Name);
        var moduleDefinition = reader.GetModuleDefinition();
        var assemblyCulture = assemblyDefinition.Culture.IsNil
            ? "neutral"
            : reader.GetString(assemblyDefinition.Culture);
        var publicKeyBytes = assemblyDefinition.PublicKey.IsNil
            ? Array.Empty<byte>()
            : reader.GetBlobBytes(assemblyDefinition.PublicKey);
        var assemblyPublicKey = publicKeyBytes.Length == 0
            ? "null"
            : Convert.ToHexString(publicKeyBytes).ToLowerInvariant();
        var assemblyReferences = reader.AssemblyReferences
            .Select(handle => ReadAssemblyReference(reader, handle))
            .OrderBy(reference => reference.Name, StringComparer.Ordinal)
            .ThenBy(reference => reference.Version, StringComparer.Ordinal)
            .ToArray();
        var declarations = new List<EndToEndMetadataDeclaration>();
        foreach (var typeHandle in reader.TypeDefinitions)
        {
            var typeDefinition = reader.GetTypeDefinition(typeHandle);
            foreach (var attributeHandle in typeDefinition.GetCustomAttributes())
            {
                var attribute = reader.GetCustomAttribute(attributeHandle);
                var attributeType = GetAttributeTypeIdentity(reader, attribute, assemblyName);
                if (!StringComparer.Ordinal.Equals(attributeType.TypeName, AttributeTypeName) ||
                    !StringComparer.Ordinal.Equals(attributeType.AssemblyName, ContractAssemblyName))
                {
                    continue;
                }

                declarations.Add(DecodeDeclaration(reader, typeHandle, typeDefinition, attribute, assemblyName));
            }
        }

        declarations.Sort((left, right) => StringComparer.Ordinal.Compare(left.TypeName, right.TypeName));
        return new EndToEndAssemblyMetadata(
            fullPath,
            assemblyName,
            assemblyDefinition.Version.ToString(),
            assemblyCulture,
            assemblyPublicKey,
            reader.GetGuid(moduleDefinition.Mvid),
            assemblyBytes.LongLength,
            hash,
            assemblyReferences,
            declarations);
    }

    private static EndToEndAssemblyReference ReadAssemblyReference(
        MetadataReader reader,
        AssemblyReferenceHandle handle)
    {
        var reference = reader.GetAssemblyReference(handle);
        var culture = reference.Culture.IsNil ? "neutral" : reader.GetString(reference.Culture);
        var keyBytes = reference.PublicKeyOrToken.IsNil
            ? Array.Empty<byte>()
            : reader.GetBlobBytes(reference.PublicKeyOrToken);
        var keyKind = (reference.Flags & AssemblyFlags.PublicKey) != 0
            ? "PublicKey"
            : "PublicKeyToken";
        var key = keyBytes.Length == 0 ? "null" : Convert.ToHexString(keyBytes).ToLowerInvariant();
        return new EndToEndAssemblyReference(
            reader.GetString(reference.Name),
            reference.Version.ToString(),
            culture,
            keyKind,
            key);
    }

    private static EndToEndMetadataDeclaration DecodeDeclaration(
        MetadataReader reader,
        TypeDefinitionHandle typeHandle,
        TypeDefinition typeDefinition,
        CustomAttribute attribute,
        string currentAssemblyName)
    {
        CustomAttributeValue<string> value;
        try
        {
            value = attribute.DecodeValue(AttributeTypeProvider.Instance);
        }
        catch (Exception exception)
        {
            throw new EndToEndDiscoveryException(
                $"Could not decode E2E attribute on '{GetTypeName(reader, typeHandle)}': {exception.Message}");
        }

        if (value.FixedArguments.Length != 3)
        {
            throw new EndToEndDiscoveryException(
                $"E2E attribute on '{GetTypeName(reader, typeHandle)}' has an unexpected constructor shape.");
        }

        var id = value.FixedArguments[0].Value as string ?? string.Empty;
        var owner = value.FixedArguments[1].Value as string ?? string.Empty;
        var packages = DecodeStringArray(value.FixedArguments[2].Value);
        var maxFrames = 3_600;
        var maxGameTicks = 60_000;
        var maxWallClockSeconds = 120;
        foreach (var namedArgument in value.NamedArguments)
        {
            var intValue = namedArgument.Value is int number ? number : 0;
            switch (namedArgument.Name)
            {
                case "MaxFrames":
                    maxFrames = intValue;
                    break;
                case "MaxGameTicks":
                    maxGameTicks = intValue;
                    break;
                case "MaxWallClockSeconds":
                    maxWallClockSeconds = intValue;
                    break;
            }
        }

        var attributes = typeDefinition.Attributes;
        var isConcrete = (attributes & TypeAttributes.Interface) == 0 &&
                         (attributes & TypeAttributes.Abstract) == 0;

        return new EndToEndMetadataDeclaration(
            GetTypeName(reader, typeHandle),
            id,
            owner,
            packages,
            maxFrames,
            maxGameTicks,
            maxWallClockSeconds,
            isConcrete,
            ImplementsContract(reader, typeHandle, currentAssemblyName, new HashSet<TypeDefinitionHandle>()),
            HasPublicParameterlessConstructor(reader, typeDefinition));
    }

    private static IReadOnlyList<string> DecodeStringArray(object? value)
    {
        if (value is not ImmutableArray<CustomAttributeTypedArgument<string>> arguments)
        {
            return Array.Empty<string>();
        }

        return arguments.Select(argument => argument.Value as string ?? string.Empty).ToArray();
    }

    private static bool ImplementsContract(
        MetadataReader reader,
        TypeDefinitionHandle typeHandle,
        string currentAssemblyName,
        ISet<TypeDefinitionHandle> visited)
    {
        if (!visited.Add(typeHandle))
        {
            return false;
        }

        var definition = reader.GetTypeDefinition(typeHandle);
        foreach (var implementationHandle in definition.GetInterfaceImplementations())
        {
            var implementation = reader.GetInterfaceImplementation(implementationHandle);
            var interfaceType = GetTypeIdentity(reader, implementation.Interface, currentAssemblyName);
            if (StringComparer.Ordinal.Equals(interfaceType.TypeName, ContractTypeName) &&
                StringComparer.Ordinal.Equals(interfaceType.AssemblyName, ContractAssemblyName))
            {
                return true;
            }
        }

        return definition.BaseType.Kind == HandleKind.TypeDefinition &&
               ImplementsContract(reader, (TypeDefinitionHandle)definition.BaseType, currentAssemblyName, visited);
    }

    private static bool HasPublicParameterlessConstructor(MetadataReader reader, TypeDefinition definition)
    {
        foreach (var methodHandle in definition.GetMethods())
        {
            var method = reader.GetMethodDefinition(methodHandle);
            if (!StringComparer.Ordinal.Equals(reader.GetString(method.Name), ".ctor") ||
                (method.Attributes & MethodAttributes.Public) == 0 ||
                (method.Attributes & MethodAttributes.Static) != 0)
            {
                continue;
            }

            var hasInputParameter = method.GetParameters()
                .Select(reader.GetParameter)
                .Any(parameter => parameter.SequenceNumber > 0);
            if (!hasInputParameter)
            {
                return true;
            }
        }

        return false;
    }

    private static TypeIdentity GetAttributeTypeIdentity(
        MetadataReader reader,
        CustomAttribute attribute,
        string currentAssemblyName)
    {
        switch (attribute.Constructor.Kind)
        {
            case HandleKind.MemberReference:
                return GetTypeIdentity(
                    reader,
                    reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent,
                    currentAssemblyName);
            case HandleKind.MethodDefinition:
                var method = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor);
                return GetTypeIdentity(reader, method.GetDeclaringType(), currentAssemblyName);
            default:
                return TypeIdentity.Empty;
        }
    }

    private static TypeIdentity GetTypeIdentity(
        MetadataReader reader,
        EntityHandle handle,
        string currentAssemblyName)
    {
        switch (handle.Kind)
        {
            case HandleKind.TypeDefinition:
                return new TypeIdentity(GetTypeName(reader, (TypeDefinitionHandle)handle), currentAssemblyName);
            case HandleKind.TypeReference:
                var reference = reader.GetTypeReference((TypeReferenceHandle)handle);
                return new TypeIdentity(
                    JoinTypeName(reader.GetString(reference.Namespace), reader.GetString(reference.Name)),
                    GetResolutionAssemblyName(reader, reference.ResolutionScope, currentAssemblyName));
            default:
                return TypeIdentity.Empty;
        }
    }

    private static string GetResolutionAssemblyName(
        MetadataReader reader,
        EntityHandle resolutionScope,
        string currentAssemblyName)
    {
        switch (resolutionScope.Kind)
        {
            case HandleKind.AssemblyReference:
                return reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)resolutionScope).Name);
            case HandleKind.TypeReference:
                return GetTypeIdentity(reader, resolutionScope, currentAssemblyName).AssemblyName;
            case HandleKind.ModuleDefinition:
            case HandleKind.ModuleReference:
                return currentAssemblyName;
            default:
                return string.Empty;
        }
    }

    private static string GetTypeName(MetadataReader reader, EntityHandle handle)
    {
        switch (handle.Kind)
        {
            case HandleKind.TypeDefinition:
                return GetTypeName(reader, (TypeDefinitionHandle)handle);
            case HandleKind.TypeReference:
                var reference = reader.GetTypeReference((TypeReferenceHandle)handle);
                return JoinTypeName(reader.GetString(reference.Namespace), reader.GetString(reference.Name));
            default:
                return string.Empty;
        }
    }

    private static string GetTypeName(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var definition = reader.GetTypeDefinition(handle);
        return JoinTypeName(reader.GetString(definition.Namespace), reader.GetString(definition.Name));
    }

    private static string JoinTypeName(string typeNamespace, string typeName) =>
        string.IsNullOrEmpty(typeNamespace) ? typeName : typeNamespace + "." + typeName;

    private sealed class AttributeTypeProvider : ICustomAttributeTypeProvider<string>
    {
        public static readonly AttributeTypeProvider Instance = new();

        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();

        public string GetSystemType() => "System.Type";

        public string GetSZArrayType(string elementType) => elementType + "[]";

        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
            GetTypeName(reader, handle);

        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
            GetTypeName(reader, handle);

        public string GetTypeFromSerializedName(string name) => name;

        public PrimitiveTypeCode GetUnderlyingEnumType(string type) => PrimitiveTypeCode.Int32;

        public bool IsSystemType(string type) => StringComparer.Ordinal.Equals(type, "System.Type");
    }

    private readonly struct TypeIdentity
    {
        public static readonly TypeIdentity Empty = new(string.Empty, string.Empty);

        public TypeIdentity(string typeName, string assemblyName)
        {
            TypeName = typeName;
            AssemblyName = assemblyName;
        }

        public string TypeName { get; }

        public string AssemblyName { get; }
    }
}
