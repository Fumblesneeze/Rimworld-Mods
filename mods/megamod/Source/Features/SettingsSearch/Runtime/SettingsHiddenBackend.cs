using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using UnityEngine;
using UnityStubs.Gui;
using Waylith.Rendering;

namespace Megamod.Features.SettingsSearch.Runtime;

// One synchronous hidden callback after the normal callback has completed. The caller owns
// admission, neutral Event, GUI state isolation, global backend ownership and observations.
public sealed class SettingsHiddenBackend : IGuiBackend
{
    private const int MaximumOpenStructures = 256;
    private const float MaximumCoordinate = 10000000f;
    [ThreadStatic] private static SettingsHiddenBackend? activeMetric;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly IGuiBackend nativeMetrics;
    private readonly FieldInfo layoutField;
    private readonly FieldInfo hostField;
    private readonly FieldInfo layoutHostField;
    private readonly FieldInfo fontsField;
    private readonly FieldInfo hostDisposedField;
    private readonly object layout;
    private readonly object host;
    private readonly ICollection fonts;
    private readonly FieldInfo scaleField;
    private readonly List<GroupState> groups = new();
    private readonly List<RenderClip> clips = new();
    private readonly List<Bounds> windows = new();
    private float m00 = 1f;
    private float m01;
    private float m10;
    private float m11 = 1f;
    private float determinant = 1f;
    private float scale = 1f;
    private Vector2 translation;
    private Vector2 offset;
    private bool rotated;

    public SettingsHiddenBackend(IGuiBackend nativeMetrics)
    {
        ArgumentNullException.ThrowIfNull(nativeMetrics);
        this.nativeMetrics = nativeMetrics;
        try
        {
            Type backendType = nativeMetrics.GetType();
            Assembly assembly = backendType.Assembly;
            if (assembly.GetName().Name != "RimWorld"
                || backendType.FullName != "RimWorldWaylith.Imgui.ImGuiGuiBackend"
                || !backendType.IsSealed)
                throw ShapeFailure("the exact native ImGui backend is required");

            Type layoutType = RequireNativeType(assembly, "RimWorldWaylith.Imgui.TextLayout");
            Type hostType = RequireNativeType(assembly, "RimWorldWaylith.Imgui.ImGuiHost");
            layoutField = RequireField(backendType, "textLayout", layoutType, true);
            hostField = RequireField(backendType, "host", hostType, true);
            layoutHostField = RequireField(layoutType, "host", hostType, true);
            layout = RequireInstance(layoutField.GetValue(nativeMetrics), layoutType, "text layout");
            host = RequireInstance(hostField.GetValue(nativeMetrics), hostType, "font host");
            if (!ReferenceEquals(layoutHostField.GetValue(layout), host))
                throw ShapeFailure("the native backend and text layout must share their font host");

            FieldInfo? candidateFonts = hostType.GetField("fonts", InstanceFields);
            Type? fontType = candidateFonts?.FieldType.IsGenericType == true
                ? candidateFonts.FieldType.GetGenericArguments()[0] : null;
            if (fontType == null || !fontType.IsValueType || fontType.FullName != "Brutal.ImGuiApi.ImFontPtr")
                throw ShapeFailure("the native font pointer collection shape changed");
            Type listType = typeof(List<>).MakeGenericType(fontType);
            fontsField = RequireField(hostType, "fonts", listType, true);
            fonts = (ICollection)RequireInstance(fontsField.GetValue(host), listType, "font list");
            RequireProperty(hostType, "Fonts", typeof(IReadOnlyList<>).MakeGenericType(fontType), false);
            hostDisposedField = RequireField(hostType, "disposed", typeof(bool), false);

            RequireProperty(layoutType, "Scale", typeof(float), true);
            // The inspected property is an auto-property. Lease its scalar directly so restore
            // cannot dispatch a patched accessor. Admission never sets its value.
            scaleField = RequireField(layoutType, "<Scale>k__BackingField", typeof(float), false);
            CheckNativeReferences();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("The native hidden-settings metric backend could not be admitted.", exception);
        }
    }

    public bool Failed => FailureReason != null;
    public string? FailureReason { get; private set; }
    public bool IsBalanced => groups.Count == 0 && clips.Count == 0 && windows.Count == 0;
    public int GroupCount => groups.Count;
    // Includes the render clip associated with every open group and standalone clip.
    public int ClipDepth => clips.Count;
    public int WindowContentDepth => windows.Count;
    public bool MouseOutsideHitClip => true;
    public bool InlineControlsObscured => false;

    public Vector2 MeasureText(int fontIndex, float fontSize, FontStyle fontStyle, string text,
        float wrapWidth, bool richText = true) =>
        Metric(() => nativeMetrics.MeasureText(fontIndex, fontSize, fontStyle, text, wrapWidth, richText), true);

    public float LineSpacing(int fontIndex, float fontSize) =>
        Metric(() => nativeMetrics.LineSpacing(fontIndex, fontSize), false);

    public void FontMetrics(int fontIndex, out float importSize, out float ascent, out float lineSpacing)
    {
        var result = Metric(() =>
        {
            nativeMetrics.FontMetrics(fontIndex, out float size, out float rise, out float spacing);
            return (size, rise, spacing);
        }, false);
        (importSize, ascent, lineSpacing) = result;
    }

    public int TextIndexAt(int fontIndex, float fontSize, FontStyle fontStyle, string text,
        float wrapWidth, Vector2 local, bool richText = true) =>
        Metric(() => nativeMetrics.TextIndexAt(fontIndex, fontSize, fontStyle, text, wrapWidth, local, richText), true);

    public Vector2 TextCursorPos(int fontIndex, float fontSize, FontStyle fontStyle, string text,
        float wrapWidth, int index, out float lineHeight, bool richText = true)
    {
        var result = Metric(() =>
        {
            Vector2 position = nativeMetrics.TextCursorPos(fontIndex, fontSize, fontStyle, text,
                wrapWidth, index, out float height, richText);
            return (position, height);
        }, true);
        lineHeight = result.height;
        return result.position;
    }

    public void TextSelectionRects(int fontIndex, float fontSize, FontStyle fontStyle, string text,
        float wrapWidth, int from, int to, List<Rect> rects, bool richText = true)
    {
        if (rects == null)
            throw Fail("The hidden text selection destination is unavailable.");
        // The caller's list must belong to the capture or be covered by explicit GUI isolation.
        Metric(() =>
        {
            nativeMetrics.TextSelectionRects(fontIndex, fontSize, fontStyle, text, wrapWidth, from, to, rects, richText);
            return true;
        }, true);
    }

    public int RegisterFont(string name, byte[] fontData, float importSize, int renderingMode) =>
        throw Fail("Font registration is unsupported during hidden settings discovery.");

    public void SetTextLanguage(string languageFolderName) =>
        throw Fail("Changing the native text language is unsupported during hidden settings discovery.");

    public void ActiveRenderTextureChanged(UnityEngine.RenderTexture active) =>
        throw Fail("Changing the native render target is unsupported during hidden settings discovery.");

    // Rendering calls intentionally produce no commands and never inspect textures or materials.
    // The ordinary recorder may wrap this backend to retain detached text and geometry.
    public void DrawText(Rect rect, string text, int fontIndex, float fontSize, FontStyle fontStyle,
        TextAnchor anchor, bool wordWrap, Color color, Rect clipRect = default, bool richText = true) => EnsureUsable();
    public void DrawTexture(Rect rect, UnityEngine.Texture image, ScaleMode scaleMode, Color color) => EnsureUsable();
    public void DrawTextureWithTexCoords(Rect rect, UnityEngine.Texture image, Rect texCoords, Color color) => EnsureUsable();
    public void DrawTextureWithMaterial(Rect rect, UnityEngine.Texture image, Rect texCoords, Color color,
        UnityEngine.Material material, Waylith.Rendering.Material passMaterial) => EnsureUsable();
    public void FillRect(Rect rect, Color color) => EnsureUsable();
    public void FillRectRounded(Rect rect, Color color, float rounding) => EnsureUsable();
    public void DrawTextureQuad(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, UnityEngine.Texture image, Color color) => EnsureUsable();
    public void DrawPrimitives(int mode, ReadOnlySpan<GuiVertex> vertices, UnityEngine.Texture texture, bool screenSpace) => EnsureUsable();

    public void SetMatrix(Matrix4x4 matrix)
    {
        EnsureUsable();
        float a = matrix.m00, b = matrix.m01, c = matrix.m10, d = matrix.m11;
        float tx = matrix.m03, ty = matrix.m13;
        if (!float.IsFinite(a) || !float.IsFinite(b) || !float.IsFinite(c) || !float.IsFinite(d)
            || !float.IsFinite(tx) || !float.IsFinite(ty))
            throw Fail("The hidden GUI transform is nonfinite.");
        float nextScale = MathF.Sqrt(a * a + c * c);
        if (nextScale == 0f)
        {
            // Exact native zero-x-column normalization; translation remains in effect.
            a = d = nextScale = 1f;
            b = c = 0f;
        }
        float nextDeterminant = a * d - b * c;
        if (!float.IsFinite(nextScale) || !float.IsFinite(nextDeterminant) || nextDeterminant == 0f)
            throw Fail("The hidden GUI transform is not safely invertible.");
        m00 = a;
        m01 = b;
        m10 = c;
        m11 = d;
        determinant = nextDeterminant;
        scale = nextScale;
        translation = new Vector2(tx, ty);
        rotated = b != 0f || c != 0f || a < 0f || d < 0f;
        // No native SetMatrix, Scale write, or Event.current mouse update here.
    }

    public void PushGroup(Rect rect) => PushGroupCore(rect, new Vector2(rect.x, rect.y));

    public void PushScrollView(Rect clipRect, Vector2 contentOffset) => PushGroupCore(clipRect, contentOffset);

    private void PushGroupCore(Rect rect, Vector2 contentDelta)
    {
        EnsureUsable();
        ReserveStructure();
        Bounds hit = ScreenBounds(rect);
        if (!rotated)
            hit = hit.Snap(0.51f);
        Bounds parentHit = groups.Count == 0 ? Bounds.Unbounded : groups[^1].Hit;
        Bounds nextHit = hit.Intersect(parentHit);
        Bounds nextRender = hit.Intersect(CurrentRenderClip);
        Vector2 previousOffset = offset;
        Vector2 nextOffset = Checked(new Vector2(offset.x + contentDelta.x, offset.y + contentDelta.y));
        if (!rotated)
        {
            // Preserve native ToScreen -> floor -> ToGui -> offset addition order. Snapping
            // each nested origin is observably different from snapping the final sum once.
            Vector2 origin = ToDevice(default, nextOffset);
            Vector2 correction = DeviceToLocal(new Vector2(MathF.Floor(origin.x + 0.5f),
                MathF.Floor(origin.y + 0.5f)), nextOffset);
            nextOffset = Checked(new Vector2(nextOffset.x + correction.x, nextOffset.y + correction.y));
        }
        groups.Add(new GroupState(rect, nextOffset, previousOffset, nextHit));
        clips.Add(new RenderClip(nextRender, true));
        offset = nextOffset;
    }

    public void PopGroup()
    {
        EnsureUsable();
        if (groups.Count == 0 || clips.Count == 0 || !clips[^1].FromGroup)
            throw Fail("Hidden GUI groups and clips were closed out of order.");
        offset = groups[^1].PreviousOffset;
        groups.RemoveAt(groups.Count - 1);
        clips.RemoveAt(clips.Count - 1);
    }

    public void GetGroup(int index, out Rect rect, out Vector2 contentOrigin)
    {
        EnsureUsable();
        if ((uint)index >= (uint)groups.Count)
            throw Fail("A hidden GUI group index is unavailable.");
        GroupState group = groups[index];
        rect = group.Rect;
        contentOrigin = group.Origin;
    }

    public void PushClipRect(Rect rect)
    {
        EnsureUsable();
        ReserveStructure();
        // Native standalone render clips neither snap with +.51 nor alter the hit-clip stack.
        clips.Add(new RenderClip(ScreenBounds(rect).Intersect(CurrentRenderClip), false));
    }

    public void PopClipRect()
    {
        EnsureUsable();
        if (clips.Count == 0 || clips[^1].FromGroup)
            throw Fail("Hidden GUI clips and groups were closed out of order.");
        clips.RemoveAt(clips.Count - 1);
    }

    public void BeginWindowContent(Rect windowRect)
    {
        EnsureUsable();
        ReserveStructure();
        windows.Add(ScreenBounds(windowRect));
    }

    public void EndWindowContent()
    {
        EnsureUsable();
        if (windows.Count == 0)
            throw Fail("A hidden GUI window was closed without matching content.");
        windows.RemoveAt(windows.Count - 1);
    }

    public Vector2 ScreenToGuiPoint(Vector2 screenPoint)
    {
        EnsureUsable();
        // Native public screen projection subtracts offset BEFORE inverse matrix multiplication.
        float x = screenPoint.x - offset.x - translation.x;
        float y = screenPoint.y - offset.y - translation.y;
        return Checked(new Vector2((m11 * x - m01 * y) / determinant,
            (m00 * y - m10 * x) / determinant));
    }

    public Vector2 GuiToScreenPoint(Vector2 guiPoint)
    {
        EnsureUsable();
        // This differs intentionally from the native device projection below.
        return Checked(new Vector2(offset.x + m00 * guiPoint.x + m01 * guiPoint.y + translation.x,
            offset.y + m10 * guiPoint.x + m11 * guiPoint.y + translation.y));
    }

    public Vector2 GuiToDevicePoint(Vector2 guiPoint)
    {
        EnsureUsable();
        return ToDevice(guiPoint, offset);
    }

    private Vector2 ToDevice(Vector2 point, Vector2 contentOrigin)
    {
        float x = contentOrigin.x + point.x;
        float y = contentOrigin.y + point.y;
        return Checked(new Vector2(m00 * x + m01 * y + translation.x,
            m10 * x + m11 * y + translation.y));
    }

    private Vector2 DeviceToLocal(Vector2 point, Vector2 contentOrigin)
    {
        float x = point.x - translation.x;
        float y = point.y - translation.y;
        return Checked(new Vector2((m11 * x - m01 * y) / determinant - contentOrigin.x,
            (m00 * y - m10 * x) / determinant - contentOrigin.y));
    }

    private Bounds ScreenBounds(Rect rect)
    {
        float left = FiniteCoordinate(rect.x);
        float top = FiniteCoordinate(rect.y);
        float right = FiniteCoordinate(rect.x + rect.width);
        float bottom = FiniteCoordinate(rect.y + rect.height);
        Vector2 first = ToDevice(new Vector2(left, top), offset);
        Vector2 third = ToDevice(new Vector2(right, bottom), offset);
        if (!rotated)
            return new Bounds(first.x, first.y, third.x, third.y);
        Vector2 second = ToDevice(new Vector2(right, top), offset);
        Vector2 fourth = ToDevice(new Vector2(left, bottom), offset);
        return new Bounds(
            MathF.Min(MathF.Min(first.x, second.x), MathF.Min(third.x, fourth.x)),
            MathF.Min(MathF.Min(first.y, second.y), MathF.Min(third.y, fourth.y)),
            MathF.Max(MathF.Max(first.x, second.x), MathF.Max(third.x, fourth.x)),
            MathF.Max(MathF.Max(first.y, second.y), MathF.Max(third.y, fourth.y)));
    }

    private static float FiniteCoordinate(float value) =>
        float.IsNaN(value) ? 0f : Math.Clamp(value, -MaximumCoordinate, MaximumCoordinate);

    private Vector2 Checked(Vector2 point)
    {
        if (!float.IsFinite(point.x) || !float.IsFinite(point.y))
            throw Fail("A hidden GUI coordinate is nonfinite.");
        return point;
    }

    private Bounds CurrentRenderClip => clips.Count == 0 ? Bounds.Unbounded : clips[^1].Bounds;

    private void ReserveStructure()
    {
        // Every group already has one entry in clips, so count it once, not twice.
        if (clips.Count + windows.Count >= MaximumOpenStructures)
            throw Fail("The hidden GUI exceeded its 256-open-structure safety budget.");
    }

    private T Metric<T>(Func<T> operation, bool leaseScale)
    {
        EnsureUsable();
        if (activeMetric != null)
        {
            activeMetric.Latch("Nested hidden native text metrics are unsupported.");
            throw Fail("Nested hidden native text metrics are unsupported.");
        }
        try
        {
            CheckNativeReferences();
        }
        catch (Exception exception)
        {
            Latch("The admitted native text metric state changed or became unavailable.");
            throw new InvalidOperationException(FailureReason, exception);
        }

        float previousScale;
        try
        {
            previousScale = leaseScale ? (float)scaleField.GetValue(layout)! : 0f;
        }
        catch (Exception exception)
        {
            Latch("The native text metric scale could not be read.");
            throw new InvalidOperationException(FailureReason, exception);
        }

        T result = default!;
        Exception? failure = null;
        activeMetric = this;
        try
        {
            if (leaseScale)
                scaleField.SetValue(layout, scale);
            result = operation();
        }
        catch (Exception exception)
        {
            Latch("A hidden native text metric call failed.");
            failure = exception;
        }
        finally
        {
            try
            {
                if (leaseScale)
                    scaleField.SetValue(layout, previousScale);
            }
            catch (Exception exception)
            {
                Latch("The native text metric scale could not be restored.");
                failure = failure == null ? exception : new AggregateException(
                    "The hidden text metric call and scale restoration failed.", failure, exception);
            }
            finally
            {
                activeMetric = null;
            }
        }
        if (failure != null)
            ExceptionDispatchInfo.Capture(failure).Throw();
        return result;
    }

    private void CheckNativeReferences()
    {
        if (!ReferenceEquals(layoutField.GetValue(nativeMetrics), layout)
            || !ReferenceEquals(hostField.GetValue(nativeMetrics), host)
            || !ReferenceEquals(layoutHostField.GetValue(layout), host)
            || !ReferenceEquals(fontsField.GetValue(host), fonts)
            || hostDisposedField.GetValue(host) is not false || fonts.Count == 0)
            throw ShapeFailure("the original native layout and initialized font host are required");
    }

    private void EnsureUsable()
    {
        if (ownerThread != Environment.CurrentManagedThreadId)
            throw Fail("The hidden settings backend was used outside its capturing GUI thread.");
        if (Failed)
            throw new InvalidOperationException(FailureReason);
    }

    private void Latch(string reason) => FailureReason ??= reason;

    private InvalidOperationException Fail(string reason)
    {
        Latch(reason);
        return new InvalidOperationException(FailureReason);
    }

    private const BindingFlags InstanceFields = BindingFlags.Public | BindingFlags.NonPublic
        | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    private static Type RequireNativeType(Assembly assembly, string name)
    {
        Type? type = assembly.GetType(name, false, false);
        if (type == null || !type.IsClass || !type.IsSealed)
            throw ShapeFailure("an exact native metric type is unavailable");
        return type;
    }

    private static FieldInfo RequireField(Type owner, string name, Type fieldType, bool readOnly)
    {
        FieldInfo? field = owner.GetField(name, InstanceFields);
        if (field == null || field.DeclaringType != owner || field.FieldType != fieldType
            || field.IsStatic || field.IsInitOnly != readOnly || field.IsLiteral)
            throw ShapeFailure("a required native metric field shape is unavailable");
        return field;
    }

    private static PropertyInfo RequireProperty(Type owner, string name, Type propertyType, bool writable)
    {
        PropertyInfo? property = owner.GetProperty(name,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        MethodInfo? getter = property?.GetGetMethod();
        MethodInfo? setter = property?.GetSetMethod();
        if (property == null || property.PropertyType != propertyType || property.GetIndexParameters().Length != 0
            || getter == null || getter.IsStatic || getter.GetParameters().Length != 0 || getter.ReturnType != propertyType
            || (writable && (setter == null || setter.IsStatic || setter.ReturnType != typeof(void)
                || setter.GetParameters().Length != 1 || setter.GetParameters()[0].ParameterType != propertyType))
            || (!writable && setter != null))
            throw ShapeFailure("a required native metric property shape is unavailable");
        return property;
    }

    private static object RequireInstance(object? value, Type exactType, string role)
    {
        if (value == null || value.GetType() != exactType)
            throw ShapeFailure("the native " + role + " is uninitialized or replaced");
        return value;
    }

    private static InvalidOperationException ShapeFailure(string reason) =>
        new("Hidden settings text metrics are unavailable: " + reason + ".");

    private readonly record struct GroupState(Rect Rect, Vector2 Origin, Vector2 PreviousOffset, Bounds Hit);
    private readonly record struct RenderClip(Bounds Bounds, bool FromGroup);

    private readonly record struct Bounds(float Left, float Top, float Right, float Bottom)
    {
        internal static Bounds Unbounded => new(-1e9f, -1e9f, 1e9f, 1e9f);
        internal Bounds Snap(float bias) => new(MathF.Floor(Left + bias), MathF.Floor(Top + bias),
            MathF.Floor(Right + bias), MathF.Floor(Bottom + bias));
        internal Bounds Intersect(Bounds parent) => new(Math.Max(Left, parent.Left), Math.Max(Top, parent.Top),
            Math.Min(Right, parent.Right), Math.Min(Bottom, parent.Bottom));
    }
}
