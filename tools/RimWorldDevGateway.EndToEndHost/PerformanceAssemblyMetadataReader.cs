using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace RimWorldDevGateway.EndToEndHost;

public static class PerformanceAssemblyMetadataReader
{
    private const string AttributeTypeName =
        "RimWorldDevGateway.PerformanceTesting.RimWorldPerformanceTestAttribute";
    private const string SelectorAttributeTypeName =
        "RimWorldDevGateway.PerformanceTesting.PerformanceMethodSelectorAttribute";
    private const string CheckpointAttributeTypeName =
        "RimWorldDevGateway.PerformanceTesting.PerformanceThroughputCheckpointAttribute";
    private const string ContractTypeName =
        "RimWorldDevGateway.PerformanceTesting.IRimWorldPerformanceTest";
    private const string ThroughputContractTypeName =
        "RimWorldDevGateway.PerformanceTesting.IPerformanceThroughputCounter";
    private const string ContractAssemblyName = "RimWorldDevGateway.PerformanceTesting";

    public static PerformanceAssemblyMetadata Read(string assemblyPath)
    {
        var assembly = EndToEndAssemblyMetadataReader.Read(assemblyPath);
        var bytes = File.ReadAllBytes(assembly.AssemblyPath);
        using var stream = new MemoryStream(bytes, writable: false);
        using var peReader = new PEReader(stream, PEStreamOptions.PrefetchMetadata);
        var reader = peReader.GetMetadataReader();
        var declarations = new List<PerformanceMetadataDeclaration>();

        foreach (var typeHandle in reader.TypeDefinitions)
        {
            var definition = reader.GetTypeDefinition(typeHandle);
            CustomAttribute? benchmarkAttribute = null;
            var selectors = new List<PerformanceMetadataSelector>();
            var checkpoints = new List<PerformanceMetadataCheckpoint>();
            foreach (var attributeHandle in definition.GetCustomAttributes())
            {
                var attribute = reader.GetCustomAttribute(attributeHandle);
                var identity = GetAttributeTypeIdentity(reader, attribute, assembly.AssemblyName);
                if (!StringComparer.Ordinal.Equals(identity.AssemblyName, ContractAssemblyName))
                {
                    continue;
                }

                if (StringComparer.Ordinal.Equals(identity.TypeName, AttributeTypeName))
                {
                    if (benchmarkAttribute is not null)
                    {
                        throw new EndToEndDiscoveryException(
                            $"Performance type '{GetTypeName(reader, typeHandle)}' declares more than one benchmark attribute.");
                    }

                    benchmarkAttribute = attribute;
                }
                else if (StringComparer.Ordinal.Equals(identity.TypeName, SelectorAttributeTypeName))
                {
                    if (selectors.Count == PerformanceDiscoveryValidator.MaximumMethodSelectors)
                    {
                        throw new EndToEndDiscoveryException(
                            $"Performance type '{GetTypeName(reader, typeHandle)}' exceeds the published selector ceiling.");
                    }

                    selectors.Add(DecodeSelector(reader, typeHandle, attribute));
                }
                else if (StringComparer.Ordinal.Equals(identity.TypeName, CheckpointAttributeTypeName))
                {
                    if (checkpoints.Count == PerformanceDiscoveryValidator.MaximumThroughputCheckpoints)
                    {
                        throw new EndToEndDiscoveryException(
                            $"Performance type '{GetTypeName(reader, typeHandle)}' exceeds the published checkpoint ceiling.");
                    }

                    checkpoints.Add(DecodeCheckpoint(reader, typeHandle, attribute));
                }
            }

            if (benchmarkAttribute is null)
            {
                continue;
            }

            if (declarations.Count == PerformanceDiscoveryValidator.MaximumBenchmarks)
            {
                throw new EndToEndDiscoveryException(
                    $"Performance assembly '{assembly.AssemblyName}' exceeds the published benchmark ceiling.");
            }

            declarations.Add(DecodeDeclaration(
                reader,
                typeHandle,
                definition,
                benchmarkAttribute.Value,
                selectors,
                checkpoints,
                assembly.AssemblyName));
        }

        declarations.Sort((left, right) => StringComparer.Ordinal.Compare(left.TypeName, right.TypeName));
        return new PerformanceAssemblyMetadata(assembly, declarations);
    }

    private static PerformanceMetadataDeclaration DecodeDeclaration(
        MetadataReader reader,
        TypeDefinitionHandle typeHandle,
        TypeDefinition definition,
        CustomAttribute attribute,
        IEnumerable<PerformanceMetadataSelector> selectors,
        IEnumerable<PerformanceMetadataCheckpoint> checkpoints,
        string currentAssemblyName)
    {
        var value = Decode(reader, typeHandle, attribute, "benchmark");
        if (value.FixedArguments.Length != 4)
        {
            throw Shape(reader, typeHandle, "benchmark constructor");
        }

        var id = String(value.FixedArguments[0]);
        var owner = String(value.FixedArguments[1]);
        var subject = String(value.FixedArguments[2]);
        var packages = DecodeStringArray(value.FixedArguments[3].Value);
        var workload = "v1";
        string? comparisonId = null;
        var warmUp = 2500;
        var sample = 12000;
        var speed = 3;
        var repetitions = 3;
        var lens = 0;
        string? control = null;
        foreach (var named in value.NamedArguments)
        {
            switch (named.Name)
            {
                case "WorkloadVersion": workload = named.Value as string ?? string.Empty; break;
                case "ComparisonId": comparisonId = named.Value as string; break;
                case "WarmUpTicks": warmUp = Int32(named.Value); break;
                case "SampleTicks": sample = Int32(named.Value); break;
                case "GameSpeed": speed = Int32(named.Value); break;
                case "Repetitions": repetitions = Int32(named.Value); break;
                case "EvidenceLens": lens = Int32(named.Value); break;
                case "ProductAbsentControlId": control = named.Value as string; break;
                default: throw Shape(reader, typeHandle, $"unknown benchmark property '{named.Name}'");
            }
        }

        var attributes = definition.Attributes;
        var isConcrete = (attributes & TypeAttributes.Interface) == 0 &&
                         (attributes & TypeAttributes.Abstract) == 0;
        return new PerformanceMetadataDeclaration(
            GetTypeName(reader, typeHandle),
            id,
            owner,
            subject,
            packages,
            workload,
            string.IsNullOrWhiteSpace(comparisonId) ? id : comparisonId!,
            warmUp,
            sample,
            speed,
            repetitions,
            lens,
            control,
            selectors.OrderBy(item => item.Kind).ThenBy(item => item.Value, StringComparer.Ordinal),
            checkpoints.OrderBy(item => item.Id, StringComparer.Ordinal),
            isConcrete,
            ImplementsInterface(
                reader,
                typeHandle,
                currentAssemblyName,
                ContractTypeName,
                new HashSet<TypeDefinitionHandle>()),
            ImplementsInterface(
                reader,
                typeHandle,
                currentAssemblyName,
                ThroughputContractTypeName,
                new HashSet<TypeDefinitionHandle>()),
            HasPublicParameterlessConstructor(reader, definition));
    }

    private static PerformanceMetadataSelector DecodeSelector(
        MetadataReader reader,
        TypeDefinitionHandle typeHandle,
        CustomAttribute attribute)
    {
        var value = Decode(reader, typeHandle, attribute, "method selector");
        if (value.FixedArguments.Length != 3 || value.NamedArguments.Length != 0)
        {
            throw Shape(reader, typeHandle, "method-selector constructor");
        }

        return new PerformanceMetadataSelector(
            Int32(value.FixedArguments[0].Value),
            String(value.FixedArguments[1]),
            String(value.FixedArguments[2]));
    }

    private static PerformanceMetadataCheckpoint DecodeCheckpoint(
        MetadataReader reader,
        TypeDefinitionHandle typeHandle,
        CustomAttribute attribute)
    {
        var value = Decode(reader, typeHandle, attribute, "throughput checkpoint");
        if (value.FixedArguments.Length != 2 || value.NamedArguments.Length != 0)
        {
            throw Shape(reader, typeHandle, "throughput-checkpoint constructor");
        }

        return new PerformanceMetadataCheckpoint(
            String(value.FixedArguments[0]),
            Int64(value.FixedArguments[1].Value));
    }

    private static CustomAttributeValue<string> Decode(
        MetadataReader reader,
        TypeDefinitionHandle typeHandle,
        CustomAttribute attribute,
        string description)
    {
        try
        {
            return attribute.DecodeValue(AttributeTypeProvider.Instance);
        }
        catch (Exception exception)
        {
            throw new EndToEndDiscoveryException(
                $"Could not decode performance {description} on '{GetTypeName(reader, typeHandle)}': {exception.Message}");
        }
    }

    private static EndToEndDiscoveryException Shape(
        MetadataReader reader,
        TypeDefinitionHandle typeHandle,
        string member) =>
        new($"Performance attribute on '{GetTypeName(reader, typeHandle)}' has unexpected {member} shape.");

    private static string String(CustomAttributeTypedArgument<string> argument) =>
        argument.Value as string ?? string.Empty;

    private static int Int32(object? value) => value is int number ? number : 0;

    private static long Int64(object? value) => value is long number ? number : 0L;

    private static IReadOnlyList<string> DecodeStringArray(object? value)
    {
        if (value is not ImmutableArray<CustomAttributeTypedArgument<string>> arguments)
        {
            return Array.Empty<string>();
        }

        return arguments.Select(String).ToArray();
    }

    private static bool ImplementsInterface(
        MetadataReader reader,
        TypeDefinitionHandle typeHandle,
        string currentAssemblyName,
        string contractTypeName,
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
            var identity = GetTypeIdentity(reader, implementation.Interface, currentAssemblyName);
            if (StringComparer.Ordinal.Equals(identity.TypeName, contractTypeName) &&
                StringComparer.Ordinal.Equals(identity.AssemblyName, ContractAssemblyName))
            {
                return true;
            }
        }

        return definition.BaseType.Kind == HandleKind.TypeDefinition &&
               ImplementsInterface(
                   reader,
                   (TypeDefinitionHandle)definition.BaseType,
                   currentAssemblyName,
                   contractTypeName,
                   visited);
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

            if (!method.GetParameters().Select(reader.GetParameter)
                    .Any(parameter => parameter.SequenceNumber > 0))
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
        return attribute.Constructor.Kind switch
        {
            HandleKind.MemberReference => GetTypeIdentity(
                reader,
                reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent,
                currentAssemblyName),
            HandleKind.MethodDefinition => GetTypeIdentity(
                reader,
                reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType(),
                currentAssemblyName),
            _ => TypeIdentity.Empty
        };
    }

    private static TypeIdentity GetTypeIdentity(
        MetadataReader reader,
        EntityHandle handle,
        string currentAssemblyName)
    {
        return handle.Kind switch
        {
            HandleKind.TypeDefinition => new TypeIdentity(GetTypeName(reader, handle), currentAssemblyName),
            HandleKind.TypeReference => TypeReferenceIdentity(reader, (TypeReferenceHandle)handle, currentAssemblyName),
            _ => TypeIdentity.Empty
        };
    }

    private static TypeIdentity TypeReferenceIdentity(
        MetadataReader reader,
        TypeReferenceHandle handle,
        string currentAssemblyName)
    {
        var reference = reader.GetTypeReference(handle);
        return new TypeIdentity(
            JoinTypeName(reader.GetString(reference.Namespace), reader.GetString(reference.Name)),
            ResolutionAssemblyName(reader, reference.ResolutionScope, currentAssemblyName));
    }

    private static string ResolutionAssemblyName(
        MetadataReader reader,
        EntityHandle scope,
        string currentAssemblyName)
    {
        return scope.Kind switch
        {
            HandleKind.AssemblyReference => reader.GetString(
                reader.GetAssemblyReference((AssemblyReferenceHandle)scope).Name),
            HandleKind.TypeReference => GetTypeIdentity(reader, scope, currentAssemblyName).AssemblyName,
            HandleKind.ModuleDefinition or HandleKind.ModuleReference => currentAssemblyName,
            _ => string.Empty
        };
    }

    private static string GetTypeName(MetadataReader reader, EntityHandle handle)
    {
        return handle.Kind switch
        {
            HandleKind.TypeDefinition => GetTypeName(reader, (TypeDefinitionHandle)handle),
            HandleKind.TypeReference => GetTypeName(reader, (TypeReferenceHandle)handle),
            _ => string.Empty
        };
    }

    private static string GetTypeName(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var definition = reader.GetTypeDefinition(handle);
        return JoinTypeName(reader.GetString(definition.Namespace), reader.GetString(definition.Name));
    }

    private static string GetTypeName(MetadataReader reader, TypeReferenceHandle handle)
    {
        var reference = reader.GetTypeReference(handle);
        return JoinTypeName(reader.GetString(reference.Namespace), reader.GetString(reference.Name));
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
