using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Megamod.Patching;
using RimWorld;
using UnityEngine;
using Verse;

namespace Megamod.Features.DrugTolerance.Runtime;

// Construction resolves metadata only. Screen geometry is read on demand after the owning
// runtime establishes its live-game context. Admission never invokes a native drawing body.
public sealed class NativeDrugToleranceGeometry
{
    private readonly List<DrugToleranceGeometryAdmission> admissions = new();
    private readonly Action<string> report;
    private bool reported;
    private readonly string? resolutionFailure;
    public NativeDrugToleranceGeometry(Action<string> report)
    {
        this.report = report;
        try
        {
            var manager = typeof(Dialog_ManageDrugPolicies);
            var generic = typeof(Dialog_ManagePolicies<DrugPolicy>);
            var members = new Dictionary<string, MemberInfo>
            {
                ["screenWidth"] = Property(typeof(Screen), "width"),
                ["minimum"] = Method(typeof(Mathf), "Min", typeof(int), typeof(int)),
                ["vector"] = typeof(Vector2).GetConstructor(new[] { typeof(float), typeof(float) })!,
                ["vectorX"] = typeof(Vector2).GetField("x")!, ["vectorY"] = typeof(Vector2).GetField("y")!,
                ["rectangle"] = typeof(Rect).GetConstructor(Enumerable.Repeat(typeof(float), 4).ToArray())!,
                ["windowRect"] = typeof(Window).GetField("windowRect", All)!,
                ["initial"] = Property(typeof(Window), "InitialSize"), ["margin"] = Property(typeof(Window), "Margin"),
                ["atZero"] = Method(typeof(GenUI), "AtZero", typeof(Rect)),
                ["rounded"] = Method(typeof(GenUI), "Rounded", typeof(Rect)),
                ["contract"] = Method(typeof(GenUI), "ContractedBy", typeof(Rect), typeof(float)),
                ["contents"] = Method(typeof(Window), "DoWindowContents", typeof(Rect)),
                ["rightContents"] = Method(generic, "DoContentsRect", typeof(Rect)),
                ["config"] = Method(manager, "DoPolicyConfigArea", typeof(Rect)),
                ["header"] = Method(manager, "DoColumnLabels", typeof(Rect)),
                ["row"] = Method(manager, "DoEntryRow", typeof(Rect), typeof(DrugPolicyEntry), typeof(int)),
                ["adjust"] = Method(typeof(Widgets), "AdjustRectsForScrollView", typeof(Rect), typeof(Rect).MakeByRefType(), typeof(Rect).MakeByRefType())
            };
            foreach (string name in new[] { "x", "width", "xMax", "y", "yMin", "yMax", "height" })
            {
                var property = typeof(Rect).GetProperty(name)!;
                members[name] = property.GetMethod!;
                members["set" + char.ToUpperInvariant(name[0]) + name.Substring(1)] = property.SetMethod!;
            }
            Add("INITIAL", Property(manager, "InitialSize"), DrugToleranceGeometryStage.InitialSize);
            Add("MARGIN", (MethodInfo)members["margin"], DrugToleranceGeometryStage.Margin);
            Add("SET-WINDOW", Method(typeof(Window), "SetInitialSizeAndPosition"), DrugToleranceGeometryStage.SetWindow);
            Add("INNER-WINDOW", Method(typeof(Window), "InnerWindowOnGUI", typeof(int)), DrugToleranceGeometryStage.InnerWindow);
            Add("MANAGER", Method(generic, "DoWindowContents", typeof(Rect)), DrugToleranceGeometryStage.Manager);
            Add("FORWARD", Method(manager, "DoContentsRect", typeof(Rect)), DrugToleranceGeometryStage.ForwardConfig);
            Add("CONFIG", (MethodInfo)members["config"], DrugToleranceGeometryStage.Config);
            Add("SCROLL", (MethodInfo)members["adjust"], DrugToleranceGeometryStage.Scroll);
            void Add(string id, MethodInfo target, DrugToleranceGeometryStage stage) => admissions.Add(new(id, target, new(members, stage)));
            Candidates = admissions.Select(a => a.Candidate).ToArray();
        }
        catch (Exception error)
        {
            resolutionFailure = error.GetBaseException().Message;
            Candidates = new[] { new PatchCandidate("DT-GEOMETRY-RESOLVE", "DT-POLICY-EDITOR", "native geometry contracts", "tolerance editing and enforcement",
                "native policy editor; retained tolerance choices", () => resolutionFailure, () => { }, () => { }) };
        }
    }
    public IReadOnlyList<PatchCandidate> Candidates { get; }
    public string? Failure { get; private set; }
    public bool Available
    {
        get
        {
            Failure = resolutionFailure;
            if (Failure == null)
                foreach (var admission in admissions) if (!admission.Available) { Failure = admission.Failure ?? "Native geometry publication is unavailable."; break; }
            if (Failure == null) { reported = false; return true; }
            if (!reported) { reported = true; report("Megamod.DrugTolerance.LayoutUnavailable"); }
            return false;
        }
    }
    public bool TryGetProspectiveWidths(out float headerWidth, out float rowWidth)
    {
        headerWidth = rowWidth = 0;
        if (!UnityData.IsInMainThread || !Available) return false;
        int physicalWidth = Screen.width;
        if (physicalWidth <= 50) return false;
        float windowWidth = Math.Min(physicalWidth - 50, 1300);
        headerWidth = windowWidth - 36 - 210;
        // Native contracts one unit on each side and reserves at most twenty for scrolling.
        // The invocation bridge separately validates the real header/row input and GUI fit.
        rowWidth = headerWidth - 22;
        return rowWidth > 0 && Available;
    }
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static MethodInfo Method(Type type, string name, params Type[] args) => type.GetMethod(name, All, null, args, null) ?? throw new MissingMethodException(type.FullName, name);
    private static MethodInfo Property(Type type, string name) => type.GetProperty(name, All)?.GetMethod ?? throw new MissingMemberException(type.FullName, name);
}
