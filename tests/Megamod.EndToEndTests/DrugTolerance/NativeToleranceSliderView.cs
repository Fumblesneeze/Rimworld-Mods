using System.Globalization;
using System.Reflection;
using Megamod.Features.DrugTolerance.Core;
using Megamod.Features.DrugTolerance.Runtime;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.DrugTolerance;

// Read-only hit geometry for the ordinary native slider, never a product setter or draw call.
internal static class NativeToleranceSliderView
{
    internal static NativeWindowDragActionStep Drag(string name, EndToEndNativeWindow window, string pointerScope,
        Dialog_ManageDrugPolicies manager, DrugPolicy policy, DrugPolicyEntry entry, float percent)
    {
        var start = Point(manager, policy, entry, DrugToleranceSlider.Position(NativeDrugToleranceState.Read(entry)));
        var end = Point(manager, policy, entry, percent);
        // Even an unchanged endpoint needs actual native movement to adjust/repair the value.
        end.y += 1;
        return new(name, window.Handle, (window.X + start.x) * window.UiScale, (window.Y + start.y) * window.UiScale,
            (window.X + end.x) * window.UiScale, (window.Y + end.y) * window.UiScale, null, pointerScope);
    }

    internal static Vector2 Point(Dialog_ManageDrugPolicies manager, DrugPolicy policy, DrugPolicyEntry entry, float percent)
    {
        EndToEndAssert.True(percent >= 0 && percent <= 101);
        var scroll = (Vector2)typeof(Dialog_ManageDrugPolicies).GetField("scrollPosition", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!;
        EndToEndAssert.True(scroll == Vector2.zero);
        int index = Enumerable.Range(0, policy.Count).Single(i => ReferenceEquals(policy[i], entry));
        EndToEndAssert.True(147 + policy.Count * 35 < manager.windowRect.height - 36 - Window.CloseButSize.y - 11);
        var font = Text.Font;
        float measured;
        try
        {
            Text.Font = GameFont.Small;
            var labels = new[] { "Megamod.DrugTolerance.Any".Translate().ToString(), "Megamod.DrugTolerance.NeedsReview".Translate().ToString(),
                "Megamod.DrugTolerance.UnavailableShort".Translate().ToString(), "Megamod.DrugTolerance.Header".Translate().ToString(), 1f.ToString("P2", CultureInfo.CurrentCulture) };
            measured = labels.Concat(labels.Take(2).Concat(new[] { labels[4] }).Select(label => "Megamod.DrugTolerance.Inactive".Translate(label).ToString()))
                .Max(label => Text.CalcSize(label).x) + 16;
        }
        finally { Text.Font = font; }
        EndToEndAssert.True(DrugToleranceLayout.TryReserve(manager.windowRect.width - 36 - 210 - 2, measured, false, out var reservation));
        var native = new Rect(18 + 210 + 1 + reservation.ColumnOffset, 18 + 147 + index * 35 + 2, reservation.ColumnWidth, 35).ContractedBy(4);
        native.y += Mathf.Round((native.height - 10) / 2) + 5; // Native middleAlignment and value label.
        var point = new Vector2(native.x + 6 + (native.width - 12) * (percent / 101), native.y + 6);
        EndToEndAssert.True(point.x >= 18 && point.x < manager.windowRect.width - 18 && point.y >= 18 && point.y < manager.windowRect.height - 18);
        return point;
    }
}
