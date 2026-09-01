using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ImmersiveSignalFire.Buildings;
using ImmersiveSignalFire.Contacts;
using ImmersiveSignalFire.Signals;
using ImmersiveSignalFire.UI;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ImmersiveSignalFire.Interactions;

internal static class SignalFireMenu
{
    public static FloatMenuOption MakeTopLevel(Building_SignalFire fire, Pawn? lockedCaller)
    {
        return new FloatMenuOption(
            "ImmersiveSignalFire_Contact".Translate(),
            () => OpenContacts(fire, lockedCaller),
            MenuOptionPriority.Default,
            null,
            fire);
    }

    internal static void OpenNoPawnTopLevel(Building_SignalFire fire)
    {
        if (!fire.Spawned || fire.Map is null || fire.SignalComp.IsBusy)
        {
            return;
        }

        var menu = new FloatMenu(
            new List<FloatMenuOption> { MakeTopLevel(fire, null) })
        {
            givesColonistOrders = false,
            vanishIfMouseDistant = false,
        };
        Find.WindowStack.Add(menu);
    }

    private static void OpenContacts(Building_SignalFire fire, Pawn? lockedCaller)
    {
        if (!fire.Spawned || fire.Map is null || fire.SignalComp.IsBusy)
        {
            Messages.Message("ImmersiveSignalFire_ContactUnavailable".Translate(), MessageTypeDefOf.RejectInput);
            return;
        }

        IReadOnlyList<WorldContact> contacts = WorldContactCatalog.For(fire.Map);
        List<FloatMenuOption> options = contacts
            .Select(contact => new FloatMenuOption(
                $"{contact.Faction.Name} ({contact.NearestDistance})",
                () => SelectContact(fire, lockedCaller, contact.Faction),
                MenuOptionPriority.Default,
                null,
                fire))
            .ToList();

        if (options.Count == 0)
        {
            options.Add(new FloatMenuOption("ImmersiveSignalFire_NoContacts".Translate(), null));
        }

        Find.WindowStack.Add(new FloatMenu(options)
        {
            givesColonistOrders = lockedCaller is not null,
            vanishIfMouseDistant = false,
        });
    }

    private static void SelectContact(Building_SignalFire fire, Pawn? lockedCaller, Faction faction)
    {
        if (!fire.Spawned || fire.Map is null || !WorldContactCatalog.IsEligible(fire.Map, faction))
        {
            Messages.Message("ImmersiveSignalFire_ContactUnavailable".Translate(), MessageTypeDefOf.RejectInput);
            return;
        }

        if (lockedCaller is not null && !SignalParticipantUtility.IsEligible(lockedCaller, fire))
        {
            Messages.Message("ImmersiveSignalFire_InvalidParticipants".Translate(), fire, MessageTypeDefOf.RejectInput);
            return;
        }

        Find.WindowStack.Add(new Dialog_BeginSignalFire(fire, faction, lockedCaller));
    }
}

[HarmonyLib.HarmonyPatch(typeof(Selector), "SelectorOnGUI")]
internal static class NoPawnSignalFireMenuPatch
{
    private const string HarmonyOwner = "fumblesneeze.immersivesignalfire";
    private static readonly object InstallationLock = new();
    private static bool installed;

    internal static bool IsInstalled
    {
        get
        {
            var selectorTarget = AccessTools.DeclaredMethod(typeof(Selector), "SelectorOnGUI");
            var prefix = AccessTools.DeclaredMethod(typeof(NoPawnSignalFireMenuPatch), nameof(Prefix));
            var uiRootTarget = AccessTools.DeclaredMethod(typeof(UIRoot_Play), "UIRootOnGUI");
            var uiRootPostfix = AccessTools.DeclaredMethod(
                typeof(NoPawnSignalFireMenuPatch),
                nameof(UIRootPostfix));
            return selectorTarget is not null &&
                   prefix is not null &&
                   uiRootTarget is not null &&
                   uiRootPostfix is not null &&
                   Harmony.GetPatchInfo(selectorTarget)?.Prefixes.Any(patch =>
                       patch.owner == HarmonyOwner && patch.PatchMethod == prefix) == true &&
                   Harmony.GetPatchInfo(uiRootTarget)?.Postfixes.Any(patch =>
                       patch.owner == HarmonyOwner && patch.PatchMethod == uiRootPostfix) == true;
        }
    }

    internal static void EnsureInstalled()
    {
        lock (InstallationLock)
        {
            if (installed && IsInstalled)
            {
                return;
            }

            Install(new Harmony(HarmonyOwner));
            installed = IsInstalled;
            if (!installed)
            {
                throw new InvalidOperationException(
                    "Immersive Signal Fire's exact no-pawn input hooks are not live after installation.");
            }
        }
    }

    internal static void Install(Harmony harmony)
    {
        var selectorTarget = AccessTools.DeclaredMethod(typeof(Selector), "SelectorOnGUI");
        var prefix = AccessTools.DeclaredMethod(typeof(NoPawnSignalFireMenuPatch), nameof(Prefix));
        var uiRootTarget = AccessTools.DeclaredMethod(typeof(UIRoot_Play), "UIRootOnGUI");
        var uiRootPostfix = AccessTools.DeclaredMethod(
            typeof(NoPawnSignalFireMenuPatch),
            nameof(UIRootPostfix));
        if (selectorTarget is null ||
            prefix is null ||
            uiRootTarget is null ||
            uiRootPostfix is null)
        {
            throw new MissingMethodException(
                "A RimWorld 1.6 signal-fire input seam or its exact patch method is unavailable.");
        }

        if (Harmony.GetPatchInfo(selectorTarget)?.Prefixes.Any(patch =>
                patch.owner == harmony.Id && patch.PatchMethod == prefix) != true)
        {
            harmony.Patch(selectorTarget, prefix: new HarmonyMethod(prefix));
        }

        if (Harmony.GetPatchInfo(uiRootTarget)?.Postfixes.Any(patch =>
                patch.owner == harmony.Id && patch.PatchMethod == uiRootPostfix) != true)
        {
            harmony.Patch(uiRootTarget, postfix: new HarmonyMethod(uiRootPostfix));
        }

        if (Harmony.GetPatchInfo(selectorTarget)?.Prefixes.Any(patch =>
                patch.owner == harmony.Id && patch.PatchMethod == prefix) != true)
        {
            throw new InvalidOperationException(
                "Harmony did not retain the explicit Immersive Signal Fire no-pawn prefix.");
        }

        if (Harmony.GetPatchInfo(uiRootTarget)?.Postfixes.Any(patch =>
                patch.owner == harmony.Id && patch.PatchMethod == uiRootPostfix) != true)
        {
            throw new InvalidOperationException(
                "Harmony did not retain the explicit Immersive Signal Fire menu-delivery postfix.");
        }
    }

    private static void Prefix(Selector __instance)
    {
        Event? currentEvent = Event.current;
        if (currentEvent is null ||
            currentEvent.type != EventType.MouseUp ||
            currentEvent.button != 1 ||
            !WorldRendererUtility.DrawingMap ||
            Find.WindowStack?.AnyWindowAbsorbingAllInput == true ||
            Current.Game?.PlayerHasControl != true ||
            __instance.SelectedPawns.Any() ||
            Find.CurrentMap is not { } map)
        {
            return;
        }

        Vector2 clickPosition = currentEvent.mousePosition;
        clickPosition.y = Verse.UI.screenHeight - clickPosition.y;
        Vector3 clickPos = Verse.UI.UIToMapPosition(clickPosition);
        IntVec3 cell = IntVec3.FromVector3(clickPos);
        if (!cell.InBounds(map))
        {
            return;
        }

        List<Building_SignalFire> fires = cell.GetThingList(map)
            .OfType<Building_SignalFire>()
            .Where(fire => !fire.SignalComp.IsBusy)
            .ToList();
        if (fires.Count == 1)
        {
            map.GetComponent<MapComponent_SignalResponses>().QueueNoPawnMenu(fires[0]);
            currentEvent.Use();
        }
    }

    private static void UIRootPostfix()
    {
        Map? map = Find.CurrentMap;
        if (map is not null)
        {
            map.GetComponent<MapComponent_SignalResponses>().FlushNoPawnMenu();
        }
    }
}
