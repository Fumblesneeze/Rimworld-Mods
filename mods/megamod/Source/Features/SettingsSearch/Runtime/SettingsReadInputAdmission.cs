using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using Verse;
using InputMouse = UnityEngine.InputSystem.Mouse;
using InputPointer = UnityEngine.InputSystem.Pointer;

namespace Megamod.Features.SettingsSearch.Runtime;

public enum SettingsReadInputStatus { Idle, Busy, Unavailable }

// Call Check immediately before entering the synchronous neutral-reader scope, at an actual
// completed native callback boundary. This class does not establish that outer boundary itself.
public sealed class SettingsReadInputAdmission
{
    // Source-measured native array shape, not Keyboard.KeyCount (110), which omits extended keys.
    // A future larger native contract needs inspection; never silently skip its extra controls.
    private const int NativeKeySlots = 127;
    private readonly Thread owner;
    private readonly Func<Keyboard?> keyboard;
    private readonly Func<InputMouse?> mouse;
    private readonly Func<Keyboard, KeyControl[]> keys;
    private readonly Func<ButtonControl, bool> buttonBusy;
    private readonly Func<InputMouse, ButtonControl>[] mouseButtons;
    private readonly Func<InputPointer, DeltaControl> mouseDelta;
    private readonly Func<InputMouse, DeltaControl> scroll;
    private readonly Func<Vector2Control, Vector2> vectorValue;
    private readonly Func<string?> composition;
    private readonly Type imeType;
    private readonly Func<object?> imeCurrent;
    private readonly Func<object, bool> imeComposing;
    private readonly Func<object, bool> imeFailed;
    private readonly Func<object, IntPtr> imeWindow;

    public SettingsReadInputAdmission()
    {
        // UnityData.CopyUnityData supplies this authority; merely capturing an arbitrary
        // constructor thread ID is not proof that it is the game/GUI thread.
        if (!UnityData.IsInMainThread || Event.current == null)
            throw new InvalidOperationException("settings-input-gui-thread-required");
        if (!OperatingSystem.IsWindows())
            throw new InvalidOperationException("settings-input-platform-unverified");
        owner = Thread.CurrentThread;

        var native = typeof(GUIUtility).Assembly;
        var input = native.GetType("UnityEngine.InputSystem.InputRuntime", true)!;
        var ime = native.GetType("UnityStubs.Internal.ImeComposition", true)!;
        var engine = AppDomain.CurrentDomain.GetAssemblies().Single(assembly => assembly.GetName().Name == "RimWorld");
        imeType = engine.GetType("RimWorldWaylith.WindowsImeBridge", true)!;
        if (!imeType.IsSealed || imeType.IsGenericType || !imeType.IsNotPublic)
            throw new InvalidOperationException("settings-input-ime-shape");

        keyboard = Static<Keyboard?>(Field(input, "_keyboard", typeof(Keyboard), true, false));
        mouse = Static<InputMouse?>(Field(input, "_mouse", typeof(InputMouse), true, false));
        keys = Instance<Keyboard, KeyControl[]>(Field(typeof(Keyboard), "_keys", typeof(KeyControl[]), false, true));
        var extendedCount = typeof(Keyboard).GetField("ExtendedKeyCount", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        if (extendedCount == null || !extendedCount.IsLiteral || extendedCount.FieldType != typeof(int) ||
            (int)extendedCount.GetRawConstantValue()! + 1 != NativeKeySlots)
            throw new InvalidOperationException("settings-input-key-inventory-shape");

        var parameter = Expression.Parameter(typeof(ButtonControl), "button");
        var value = Field(typeof(AxisControl), "_value", typeof(float), false, false);
        // Native SetPressed produces 0/1. Treat any nonzero/nonfinite custom value as busy;
        // do not call a potentially patched ReadValue/isPressed getter merely to inspect state.
        Expression activity = Expression.NotEqual(Expression.Field(parameter, value), Expression.Constant(0f));
        foreach (string name in new[] { "_wasPressedThisFrame", "_wasReleasedThisFrame", "_wasRepeatedThisFrame" })
            activity = Expression.OrElse(activity, Expression.Field(parameter,
                Field(typeof(ButtonControl), name, typeof(bool), false, false)));
        buttonBusy = Expression.Lambda<Func<ButtonControl, bool>>(activity, parameter).Compile();
        mouseButtons = new[] { "_leftButton", "_rightButton", "_middleButton", "_forwardButton", "_backButton" }
            .Select(name => Instance<InputMouse, ButtonControl>(Field(typeof(InputMouse), name, typeof(ButtonControl), false, true))).ToArray();
        mouseDelta = Instance<InputPointer, DeltaControl>(Field(typeof(InputPointer), "_delta", typeof(DeltaControl), false, true));
        scroll = Instance<InputMouse, DeltaControl>(Field(typeof(InputMouse), "_scroll", typeof(DeltaControl), false, true));
        vectorValue = Instance<Vector2Control, Vector2>(Field(typeof(Vector2Control), "_value", typeof(Vector2), false, false));
        composition = Static<string?>(Field(ime, "<Text>k__BackingField", typeof(string), true, false));

        // Current is internal static, not a public property. Resolve it directly rather than
        // adding a dependency on Bootstrap._imeBridge. A missing/unattached/failed bridge is a gap.
        var current = imeType.GetField("Current", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
        if (current == null || !current.IsAssembly || current.IsInitOnly || current.FieldType != imeType)
            throw new InvalidOperationException("settings-input-ime-current-shape");
        imeCurrent = Static<object?>(current);
        imeComposing = Instance<object, bool>(Field(imeType, "composing", typeof(bool), false, false));
        imeFailed = Instance<object, bool>(Field(imeType, "failed", typeof(bool), false, false));
        imeWindow = Instance<object, IntPtr>(Field(imeType, "hwnd", typeof(IntPtr), false, false));
    }

    public SettingsReadInputStatus Check(out string? reason)
    {
        reason = null;
        if (!ReferenceEquals(owner, Thread.CurrentThread) || !UnityData.IsInMainThread)
            return Result(SettingsReadInputStatus.Unavailable, "settings-input-wrong-thread", out reason);
        var inputEvent = Event.current;
        if (inputEvent == null || inputEvent.type != EventType.Repaint || inputEvent.rawType != EventType.Repaint)
            return Result(SettingsReadInputStatus.Busy, "settings-input-not-repaint", out reason);
        // Native Repaint carries the physical displacement; no Event mutation/consumption here.
        if (inputEvent.delta.x != 0f || inputEvent.delta.y != 0f)
            return Result(SettingsReadInputStatus.Busy, "settings-input-pointer-motion", out reason);
        try
        {
            var currentKeyboard = keyboard();
            var currentMouse = mouse();
            if (currentKeyboard?.GetType() != typeof(Keyboard) || currentMouse?.GetType() != typeof(InputMouse))
                return Result(SettingsReadInputStatus.Unavailable, "settings-input-devices-unavailable", out reason);
            var currentKeys = keys(currentKeyboard);
            if (currentKeys == null || currentKeys.Length != NativeKeySlots)
                return Result(SettingsReadInputStatus.Unavailable, "settings-input-key-inventory-shape", out reason);
            for (int index = 0; index < currentKeys.Length; index++)
            {
                var key = currentKeys[index];
                if (key == null) continue; // Native unregistered/None slots are deliberately empty.
                if (key.GetType() != typeof(KeyControl))
                    return Result(SettingsReadInputStatus.Unavailable, "settings-input-key-control-shape", out reason);
                if (buttonBusy(key))
                    return Result(SettingsReadInputStatus.Busy, "settings-input-keyboard-active", out reason);
            }
            for (int index = 0; index < mouseButtons.Length; index++)
            {
                var button = mouseButtons[index](currentMouse);
                if (button?.GetType() != typeof(ButtonControl))
                    return Result(SettingsReadInputStatus.Unavailable, "settings-input-mouse-control-shape", out reason);
                if (buttonBusy(button))
                    return Result(SettingsReadInputStatus.Busy, "settings-input-mouse-active", out reason);
            }
            var delta = mouseDelta(currentMouse);
            var wheel = scroll(currentMouse);
            if (delta?.GetType() != typeof(DeltaControl) || wheel?.GetType() != typeof(DeltaControl))
                return Result(SettingsReadInputStatus.Unavailable, "settings-input-delta-shape", out reason);
            Vector2 movement = vectorValue(delta), scrolling = vectorValue(wheel);
            if (movement.x != 0f || movement.y != 0f || scrolling.x != 0f || scrolling.y != 0f)
                return Result(SettingsReadInputStatus.Busy, "settings-input-pointer-active", out reason);
            string? text = composition();
            if (text == null)
                return Result(SettingsReadInputStatus.Unavailable, "settings-input-composition-shape", out reason);
            var bridge = imeCurrent();
            if (bridge?.GetType() != imeType || imeFailed(bridge) || imeWindow(bridge) == IntPtr.Zero)
                return Result(SettingsReadInputStatus.Unavailable, "settings-input-ime-unavailable", out reason);
            if (text.Length != 0 || imeComposing(bridge))
                return Result(SettingsReadInputStatus.Busy, "settings-input-ime-active", out reason);
            // keyboardControl/hotControl/focus alone deliberately do not prevent an idle read.
            return SettingsReadInputStatus.Idle;
        }
        catch (Exception)
        {
            // Fixed category only: no key/text value, foreign setting or exception text is logged.
            return Result(SettingsReadInputStatus.Unavailable, "settings-input-read-failed", out reason);
        }
    }

    private static SettingsReadInputStatus Result(SettingsReadInputStatus status, string category, out string? reason)
    { reason = category; return status; }

    private static FieldInfo Field(Type type, string name, Type valueType, bool isStatic, bool readOnly)
    {
        var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.DeclaredOnly |
            (isStatic ? BindingFlags.Static : BindingFlags.Instance));
        if (field == null || field.DeclaringType != type || !field.IsPrivate || field.IsStatic != isStatic ||
            field.FieldType != valueType || field.IsInitOnly != readOnly || field.IsLiteral)
            throw new InvalidOperationException("settings-input-field-shape:" + type.Name + "." + name);
        return field;
    }

    private static Func<T> Static<T>(FieldInfo field)
        => Expression.Lambda<Func<T>>(Expression.Convert(Expression.Field(null, field), typeof(T))).Compile();

    private static Func<T, TResult> Instance<T, TResult>(FieldInfo field)
    {
        var instance = Expression.Parameter(typeof(T), "instance");
        return Expression.Lambda<Func<T, TResult>>(Expression.Convert(Expression.Field(
            Expression.Convert(instance, field.DeclaringType!), field), typeof(TResult)), instance).Compile();
    }
}
