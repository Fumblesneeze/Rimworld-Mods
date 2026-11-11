using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Megamod.Features.MessagePlacement;

internal static class NativeMessageBarLabels
{
    internal static MessageBarLabelAdmission Create()
    {
        var labelArguments = new[] { typeof(Pawn), typeof(Vector2), typeof(float), typeof(float),
            typeof(Dictionary<string, string>), typeof(GameFont), typeof(bool), typeof(bool) };
        var position = Method(typeof(GenMapUI), nameof(GenMapUI.DrawPawnLabel), labelArguments);
        labelArguments[1] = typeof(Rect);
        var rectangle = Method(typeof(GenMapUI), nameof(GenMapUI.DrawPawnLabel), labelArguments);
        var colonist = Method(typeof(ColonistBarColonistDrawer), nameof(ColonistBarColonistDrawer.DrawColonist),
            typeof(Rect), typeof(Pawn), typeof(Map), typeof(bool), typeof(bool));
        var helperArguments = new[] { typeof(Pawn), typeof(float), typeof(Dictionary<string, string>), typeof(GameFont) };
        var members = new Dictionary<string, MemberInfo>
        {
            ["bar"] = Property(typeof(ColonistBarColonistDrawer), "ColonistBar"),
            ["scale"] = Property(typeof(ColonistBar), nameof(ColonistBar.Scale)),
            ["space"] = Property(typeof(ColonistBar), nameof(ColonistBar.SpaceBetweenColonistsHorizontal)),
            ["center"] = Property(typeof(Rect), nameof(Rect.center)), ["bottom"] = Property(typeof(Rect), nameof(Rect.yMax)),
            ["width"] = Property(typeof(Rect), nameof(Rect.width)), ["rectX"] = Property(typeof(Rect), nameof(Rect.x)),
            ["height"] = Property(typeof(Rect), nameof(Rect.height)), ["position"] = Property(typeof(Rect), nameof(Rect.position)),
            ["rectY"] = Property(typeof(Rect), nameof(Rect.y)), ["x"] = typeof(Vector2).GetField(nameof(Vector2.x))!,
            ["y"] = typeof(Vector2).GetField(nameof(Vector2.y))!,
            ["vector"] = typeof(Vector2).GetConstructor(new[] { typeof(float), typeof(float) })!,
            ["rectangle"] = typeof(Rect).GetConstructor(new[] { typeof(float), typeof(float), typeof(float), typeof(float) })!,
            ["cache"] = typeof(ColonistBarColonistDrawer).GetField("pawnLabelsCache", BindingFlags.Instance | BindingFlags.NonPublic)!,
            ["positionLabel"] = position, ["rectangleLabel"] = rectangle,
            ["nameWidth"] = Method(typeof(GenMapUI), "GetPawnLabelNameWidth", helperArguments),
            ["getLabel"] = Method(typeof(GenMapUI), "GetPawnLabel", helperArguments),
            ["disableTiny"] = Property(typeof(Prefs), nameof(Prefs.DisableTinyText)),
            ["font"] = typeof(Text).GetProperty(nameof(Text.Font))!.SetMethod!,
            ["anchor"] = typeof(Text).GetProperty(nameof(Text.Anchor))!.SetMethod!,
            ["calcSize"] = Method(typeof(Text), nameof(Text.CalcSize), typeof(string)),
            ["label"] = Method(typeof(Widgets), nameof(Widgets.Label), typeof(Rect), typeof(string)),
            ["tiny"] = Property(typeof(Text), nameof(Text.TinyFontSupported)),
            ["drafted"] = Property(typeof(Pawn), nameof(Pawn.Drafted)),
            ["line"] = Method(typeof(Widgets), nameof(Widgets.DrawLineHorizontal), typeof(float), typeof(float), typeof(float)),
            ["drawTexture"] = Method(typeof(GUI), nameof(GUI.DrawTexture), typeof(Rect), typeof(Texture)),
            ["fill"] = Method(typeof(Widgets), nameof(Widgets.FillableBar), typeof(Rect), typeof(float), typeof(Texture2D), typeof(Texture2D), typeof(bool)),
            ["contract"] = Method(typeof(GenUI), nameof(GenUI.ContractedBy), typeof(Rect), typeof(float)),
            ["textBackground"] = typeof(TexUI).GetField(nameof(TexUI.GrayTextBG))!,
            ["healthTexture"] = typeof(GenMapUI).GetField(nameof(GenMapUI.OverlayHealthTex), BindingFlags.Public | BindingFlags.Static)!,
            ["clearTexture"] = typeof(BaseContent).GetField(nameof(BaseContent.ClearTex))!
        };
        members["size"] = Property(typeof(ColonistBar), nameof(ColonistBar.Size));
        members["textureSize"] = typeof(ColonistBarColonistDrawer).GetField("PawnTextureSize", BindingFlags.Static | BindingFlags.Public)!;
        members["multiplyVector"] = Method(typeof(Vector2), "op_Multiply", typeof(Vector2), typeof(float));
        members["tip"] = Method(typeof(TipSignal), "op_Implicit", typeof(string));
        members["tipRegion"] = Method(typeof(TooltipHandler), nameof(TooltipHandler.TipRegion), typeof(Rect), typeof(TipSignal));
        var iconType = typeof(ColonistBarColonistDrawer).GetNestedType("IconDrawCall", BindingFlags.NonPublic)!;
        var icons = typeof(List<>).MakeGenericType(iconType);
        members["iconArea"] = typeof(ColonistBarColonistDrawer).GetField("BaseIconAreaWidth", BindingFlags.Static | BindingFlags.NonPublic)!;
        members["iconMax"] = typeof(ColonistBarColonistDrawer).GetField("BaseIconMaxSize", BindingFlags.Static | BindingFlags.NonPublic)!;
        members["iconList"] = typeof(ColonistBarColonistDrawer).GetField("tmpIconsToDraw", BindingFlags.Static | BindingFlags.NonPublic)!;
        members["iconCount"] = Property(icons, "Count");
        members["min"] = Method(typeof(Mathf), nameof(Mathf.Min), typeof(float), typeof(float));
        members["iconTexture"] = iconType.GetField("texture")!;
        members["iconTooltip"] = iconType.GetField("tooltip")!;
        members["drawIcon"] = Method(typeof(ColonistBarColonistDrawer), "DrawIcon", typeof(Texture2D), typeof(Vector2).MakeByRefType(), typeof(float), typeof(string));
        members["drawIcons"] = Method(typeof(ColonistBarColonistDrawer), "DrawIcons", typeof(Rect), typeof(Pawn));
        members["textureRect"] = Method(typeof(ColonistBarColonistDrawer), nameof(ColonistBarColonistDrawer.GetPawnTextureRect), typeof(Vector2));
        members["beginGroup"] = Method(typeof(Widgets), nameof(Widgets.BeginGroup), typeof(Rect));
        members["endGroup"] = Method(typeof(Widgets), nameof(Widgets.EndGroup));
        members["atZero"] = Method(typeof(GenUI), nameof(GenUI.AtZero), typeof(Rect));
        members["setColor"] = typeof(GUI).GetProperty(nameof(GUI.color))!.SetMethod!;
        members["box"] = Method(typeof(Widgets), nameof(Widgets.DrawBox), typeof(Rect), typeof(int), typeof(Texture2D));
        members["boxSolid"] = Method(typeof(Widgets), nameof(Widgets.DrawBoxSolid), typeof(Rect), typeof(Color));
        members["backgroundTexture"] = typeof(ColonistBar).GetField(nameof(ColonistBar.BGTex))!;
        members["moodTexture"] = typeof(ColonistBarColonistDrawer).GetField("MoodBGTex", BindingFlags.Static | BindingFlags.NonPublic)!;
        members["gradientTexture"] = typeof(ColonistBarColonistDrawer).GetField("MoodGradient", BindingFlags.Static | BindingFlags.NonPublic)!;
        members["deadTexture"] = typeof(ColonistBarColonistDrawer).GetField("DeadColonistTex", BindingFlags.Static | BindingFlags.NonPublic)!;
        members["needs"] = typeof(Pawn).GetField(nameof(Pawn.needs))!;
        members["mood"] = typeof(Pawn_NeedsTracker).GetField(nameof(Pawn_NeedsTracker.mood))!;
        members["level"] = Property(typeof(Need), nameof(Need.CurLevelPercentage));
        members["setYMin"] = typeof(Rect).GetProperty(nameof(Rect.yMin))!.SetMethod!;
        members["setHeight"] = typeof(Rect).GetProperty(nameof(Rect.height))!.SetMethod!;
        var exclusionTargets = new MessageBarColonistExclusionTargets(
            Method(typeof(MoodThresholdExtensions), nameof(MoodThresholdExtensions.CurrentMoodThresholdFor), typeof(Pawn)),
            Method(typeof(MoodThresholdExtensions), nameof(MoodThresholdExtensions.EdgeExpansion), typeof(MoodThreshold)),
            Method(typeof(GenUI), nameof(GenUI.ExpandedBy), typeof(Rect), typeof(float)),
            Method(typeof(Widgets), nameof(Widgets.DrawAtlas), typeof(Rect), typeof(Texture2D)),
            typeof(ColonistBarColonistDrawer).GetField("MoodAtlas", BindingFlags.Static | BindingFlags.NonPublic)!,
            Property(typeof(Pawn), nameof(Pawn.Dead)), Property(typeof(Pawn), nameof(Pawn.Corpse)),
            Property(typeof(Find), nameof(Find.Selector)), Property(typeof(Selector), nameof(Selector.SelectedObjects)),
            Method(typeof(List<object>), nameof(List<object>.Contains), typeof(object)),
            Property(typeof(WorldRendererUtility), nameof(WorldRendererUtility.WorldSelected)),
            Method(typeof(ColonistBarColonistDrawer), "DrawSelectionOverlayOnGUI", typeof(Pawn), typeof(Rect)),
            Method(typeof(CaravanUtility), nameof(CaravanUtility.GetCaravan), typeof(Thing)),
            Method(typeof(ColonistBarColonistDrawer), "DrawCaravanSelectionOverlayOnGUI", typeof(Caravan), typeof(Rect)),
            (MethodInfo)members["contract"], (MethodInfo)members["bar"], (MethodInfo)members["scale"]);
        members["outwardAtlas"] = exclusionTargets.DrawAtlas;
        return new(new(new(members, new[] { typeof(GUI), typeof(Widgets), typeof(GUIUtility) }), new(exclusionTargets)), (colonist, MessageBarLabelStage.ColonistGeometry),
            (position, MessageBarLabelStage.Position), (rectangle, MessageBarLabelStage.Rectangle),
            (Method(typeof(ColonistBarColonistDrawer), nameof(ColonistBarColonistDrawer.GetPawnTextureRect), typeof(Vector2)), MessageBarLabelStage.PortraitTexture),
            (Method(typeof(ColonistBarColonistDrawer), "DrawIcons", typeof(Rect), typeof(Pawn)), MessageBarLabelStage.Icons),
            ((MethodInfo)members["drawIcon"], MessageBarLabelStage.Icon));
    }
    private static MethodInfo Method(Type owner, string name, params Type[] arguments) => owner.GetMethod(name,
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, arguments, null) ??
        throw new MissingMethodException(owner.FullName, name);
    private static MethodInfo Property(Type owner, string name) => owner.GetProperty(name,
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)?.GetMethod ??
        throw new MissingMemberException(owner.FullName, name);
}
