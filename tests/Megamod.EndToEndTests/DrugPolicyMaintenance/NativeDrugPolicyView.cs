using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Megamod.Features.DrugTolerance.Core;
using Megamod.Features.DrugTolerance.Runtime;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.DrugPolicyMaintenance;

// Read-only geometry for the inspected 1.7 native controls. Never invokes their action callbacks.
// All returned rectangles are relative to the captured outer window, in logical GUI units.
internal sealed class NativeDrugPolicyView
{
    internal DrugPolicy? Selected(Dialog_ManageDrugPolicies dialog) =>
        Read<DrugPolicy?>(typeof(Dialog_ManagePolicies<DrugPolicy>), dialog, "policyInt");

    internal Rect ManageButton(MainTabWindow_Assign assign)
    {
        var (_, column, header) = DrugColumn(assign);
        return Visible(assign, new Rect(6 + column.x, 6 + header - 65, Math.Min(column.width, 360), 32));
    }

    internal Rect PawnPolicyCell(MainTabWindow_Assign assign, Pawn pawn)
    {
        var (table, column, header) = DrugColumn(assign);
        var pawns = Read<List<Pawn>>(typeof(PawnTable), table, "cachedPawns");
        var heights = Read<List<float>>(typeof(PawnTable), table, "cachedRowHeights");
        var scroll = Read<Vector2>(typeof(PawnTable), table, "scrollPosition");
        int index = pawns.FindIndex(item => ReferenceEquals(item, pawn));
        EndToEndAssert.True(index >= 0 && heights.Count == pawns.Count);
        EndToEndAssert.True(scroll == Vector2.zero, "Only an initially visible native assignment row is supported.");
        float y = heights.Take(index).Sum(height => (int)height);
        var size = Read<Vector2>(typeof(PawnTable), table, "cachedSize");
        EndToEndAssert.True(y + (int)heights[index] <= (int)size.y - header, "Assignment row needs a native scrolling workflow.");
        return Visible(assign, new Rect(6 + column.x, 6 + header + y + 2, column.width, (int)heights[index] - 4));
    }

    private static (PawnTable Table, Rect Column, int Header) DrugColumn(MainTabWindow_Assign assign)
    {
        var table = Read<PawnTable>(typeof(MainTabWindow_PawnTable), assign, "table");
        EndToEndAssert.False(Read<bool>(typeof(PawnTable), table, "dirty"), "Wait for the native Assign table to prepare before capturing geometry.");
        var columns = Read<List<PawnColumnDef>>(typeof(PawnTable), table, "columns");
        var widths = Read<List<float>>(typeof(PawnTable), table, "cachedColumnWidths");
        int index = columns.FindIndex(column => column.workerClass == typeof(PawnColumnWorker_DrugPolicy));
        EndToEndAssert.True(index >= 0 && widths.Count == columns.Count && !columns[index].groupable);
        int x = widths.Take(index).Sum(width => (int)width);
        var size = Read<Vector2>(typeof(PawnTable), table, "cachedSize");
        int width = index == columns.Count - 1 ? (int)(size.x - 16 - x) : (int)widths[index];
        int header = (int)Read<float>(typeof(PawnTable), table, "cachedHeaderHeight");
        EndToEndAssert.True(width > 0 && header >= 65);
        return (table, new Rect(x, 0, width, header), header);
    }

    internal Rect PolicyRow(Dialog_ManageDrugPolicies dialog, DrugPolicy policy)
    {
        EndToEndAssert.True(Read<Vector2>(typeof(Dialog_ManagePolicies<DrugPolicy>), dialog, "scroll") == Vector2.zero);
        var quickSearch = Read<QuickSearchWidget>(typeof(Dialog_ManagePolicies<DrugPolicy>), dialog, "quickSearch");
        var policies = Current.Game.drugPolicyDatabase.AllPolicies;
        EndToEndAssert.True(policies.All(item => quickSearch.filter.Matches(item.label)), "The fresh policy list must have no active search filter.");
        var order = policies.OrderBy(item => !ReferenceEquals(item, policies[0])).ThenBy(item => item.label).ToArray();
        int index = Array.IndexOf(order, policy);
        EndToEndAssert.True(index >= 0);
        // Native title/tip each32, ten-unit gap, policy heading32, drug-header offset36, gap4.
        const float listTop = 146;
        float listBottom = dialog.windowRect.height - 36 - Window.CloseButSize.y - 10 - 24 - 10 - Window.CloseButSize.y - 10 - 10;
        EndToEndAssert.True(listTop + order.Length * 32 < listBottom, "This narrow scenario requires the complete native policy list without scrolling.");
        return Visible(dialog, new Rect(18, 18 + listTop + index * 32, 200, 32));
    }

    internal Rect BeerJoyCheckbox(Dialog_ManageDrugPolicies dialog, DrugPolicy policy)
    {
        EndToEndAssert.True(ReferenceEquals(Selected(dialog), policy));
        EndToEndAssert.True(Read<Vector2>(typeof(Dialog_ManageDrugPolicies), dialog, "scrollPosition") == Vector2.zero);
        int index = Enumerable.Range(0, policy.Count).Single(i => policy[i].drug.defName == "Beer");
        const float rightX = 210, rowTop = 147;
        float rowWidth = dialog.windowRect.width - 36 - rightX - 2;
        // S138 reserves an owned column before native DoEntryRow calculates its columns.
        // Read that current geometry; never invoke Prepare, drawing or an action callback.
        var runtime = Read<NativeDrugToleranceRuntime?>(typeof(MegamodMod), LoadedModManager.GetMod<MegamodMod>(), "drugTolerance");
        if (runtime?.Available == true)
        {
            float column = (float)(typeof(NativeDrugToleranceRuntime).GetMethod("MeasureColumn", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(typeof(NativeDrugToleranceRuntime).FullName, "MeasureColumn")).Invoke(null, null)!;
            EndToEndAssert.True(DrugToleranceLayout.TryReserve(rowWidth, column, false, out var reservation));
            rowWidth = reservation.NativeWidth;
        }
        float bottom = dialog.windowRect.height - 36 - Window.CloseButSize.y - 10 - 1;
        EndToEndAssert.True(rowTop + policy.Count * 35 < bottom, "This narrow scenario requires all native drug rows visible; do not guess scrollbar-adjusted columns.");
        float variable = rowWidth - 27 - 108;
        float joyX = 18 + rightX + 1 + 27 + variable * 0.25f + variable * 0.15f + 36;
        return Visible(dialog, new Rect(joyX, 18 + rowTop + index * 35 + 2, 24, 24));
    }

    internal Rect MenuOption(FloatMenu menu, string label)
    {
        var font = Text.Font;
        try
        {
            // Native menu layout getters select Small font; restore the observer's GUI state.
            var options = Read<List<FloatMenuOption>>(typeof(FloatMenu), menu, "options");
            EndToEndAssert.False(Property<bool>(menu, "UsingScrollbar"));
            EndToEndAssert.Equal(1, Property<int>(menu, "ColumnCount"));
            int index = options.FindIndex(option => option.Label == label);
            EndToEndAssert.True(index >= 0 && options.Count(option => option.Label == label) == 1 && !options[index].Disabled);
            float y = options.Take(index).Sum(option => option.RequiredHeight - 1);
            float width = Property<float>(menu, "ColumnWidth");
            return Visible(menu, new Rect(0, y, width, options[index].RequiredHeight));
        }
        finally { Text.Font = font; }
    }

    private static Rect Visible(Window window, Rect rect)
    {
        EndToEndAssert.True(rect.width > 0 && rect.height > 0 && float.IsFinite(rect.x) && float.IsFinite(rect.y) &&
            rect.xMin >= 0 && rect.yMin >= 0 && rect.xMax <= window.windowRect.width && rect.yMax <= window.windowRect.height,
            "The inspected native control must be entirely inside the actual window.");
        EndToEndAssert.True(window.windowRect.xMin >= 0 && window.windowRect.yMin >= 0 &&
            window.windowRect.xMax <= UI.screenWidth && window.windowRect.yMax <= UI.screenHeight,
            "The native control window must be entirely inside the current logical viewport.");
        return rect;
    }
    private static T Read<T>(Type owner, object instance, string name) =>
        (T)(owner.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
            ?? throw new MissingFieldException(owner.FullName, name)).GetValue(instance)!;
    private static T Property<T>(object instance, string name) =>
        (T)(instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new MissingMemberException(instance.GetType().FullName, name)).GetValue(instance)!;
}
