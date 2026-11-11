using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Megamod.Patching;

public sealed class ProtectedNativeWarning
{
    private readonly Action<string>? write;
    private readonly Func<bool>? ready;
    public ProtectedNativeWarning(Type logType, Type fileLogType)
    {
        try
        {
            const BindingFlags declared = BindingFlags.DeclaredOnly | BindingFlags.Static;
            FieldInfo? category = logType.GetField("logCategory", declared | BindingFlags.NonPublic);
            if (category == null || logType.IsGenericType || category.FieldType.IsGenericType)
                throw new InvalidOperationException("The native category field changed.");
            var methods = category.FieldType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => method.Name == "Warning" && !method.IsGenericMethod && method.ReturnType == typeof(void) &&
                    method.GetParameters().Select(p => p.ParameterType).Skip(1).SequenceEqual(new[] { typeof(string), typeof(string), typeof(int) })).ToArray();
            if (methods.Length != 1) throw new InvalidOperationException("The native warning output changed or is ambiguous.");
            MethodInfo warning = methods[0];
            Type text = warning.GetParameters()[0].ParameterType;
            if (!text.IsValueType || !text.IsGenericType || text.ContainsGenericParameters)
                throw new InvalidOperationException("The native warning string shape changed.");
            var conversions = text.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => method.Name == "op_Implicit" && !method.IsGenericMethod && method.ReturnType == text &&
                    method.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(string) })).ToArray();
            if (conversions.Length != 1) throw new InvalidOperationException("The native string conversion changed.");
            var enabledMethods = category.FieldType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => method.Name == "IsEnabled" && method.ReturnType == typeof(bool) && !method.IsGenericMethod &&
                    method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType.IsEnum).ToArray();
            if (enabledMethods.Length != 1) throw new InvalidOperationException("The native category readiness changed.");
            MethodInfo isEnabled = enabledMethods[0];
            Type levelType = isEnabled.GetParameters()[0].ParameterType;
            if (Enum.GetUnderlyingType(levelType) != typeof(int) || !Enum.IsDefined(levelType, "Warning"))
                throw new InvalidOperationException("The native warning severity changed.");
            MethodInfo? disabled = fileLogType.GetMethod("get_Disabled", declared | BindingFlags.Public, null, Type.EmptyTypes, null);
            MethodInfo? started = fileLogType.GetMethod("get_StartedRoot", declared | BindingFlags.Public, null, Type.EmptyTypes, null);
            FieldInfo? shutdown = fileLogType.GetField("_shutdown", declared | BindingFlags.NonPublic);
            if (fileLogType.IsGenericType || disabled == null || disabled.IsGenericMethod || disabled.ReturnType != typeof(bool) ||
                started == null || started.IsGenericMethod || started.ReturnType != typeof(string) || shutdown?.FieldType != typeof(bool))
                throw new InvalidOperationException("The native file availability contract changed.");
            var enabledCheck = new DynamicMethod("MegamodProtectedWarningEnabled", typeof(bool), Type.EmptyTypes,
                typeof(ProtectedNativeWarning).Module, true);
            var checkIl = enabledCheck.GetILGenerator();
            checkIl.Emit(OpCodes.Ldsfld, category);
            checkIl.Emit(OpCodes.Ldc_I4, Convert.ToInt32(Enum.Parse(levelType, "Warning")));
            checkIl.Emit(OpCodes.Callvirt, isEnabled);
            checkIl.Emit(OpCodes.Ret);
            var categoryEnabled = (Func<bool>)enabledCheck.CreateDelegate(typeof(Func<bool>));
            var fileDisabled = (Func<bool>)disabled.CreateDelegate(typeof(Func<bool>));
            var startedRoot = (Func<string?>)started.CreateDelegate(typeof(Func<string?>));
            ready = () => categoryEnabled() && !fileDisabled() && !string.IsNullOrEmpty(startedRoot()) && !(bool)shutdown.GetValue(null)!;
            // LogString<T> is a ref struct. Keep it on the emitted stack; reflection boxing is invalid.
            var forward = new DynamicMethod("MegamodProtectedWarning", typeof(void), new[] { typeof(string) },
                typeof(ProtectedNativeWarning).Module, true);
            var il = forward.GetILGenerator();
            il.Emit(OpCodes.Ldsfld, category);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, conversions[0]);
            il.Emit(OpCodes.Ldstr, "MegamodProtectedWarning");
            il.Emit(OpCodes.Ldstr, "");
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Callvirt, warning);
            il.Emit(OpCodes.Ret);
            write = (Action<string>)forward.CreateDelegate(typeof(Action<string>));
        }
        catch (Exception exception) { UnavailableReason = exception.GetBaseException().Message; }
    }
    public string? UnavailableReason { get; }
    public bool TryWrite(string message)
    {
        if (write == null || ready == null || !ready()) return false;
        write(message);
        return ready();
    }
}
