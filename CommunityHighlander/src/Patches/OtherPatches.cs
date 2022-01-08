using System;
using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;
using TMPro;

using UI;
using Ships;
using Modding;
using FleetEditor;

using CommunityHighlander.Overrides;
using CommunityHighlander.Helpers;

namespace CommunityHighlander.Patches
{
    // Print NCH version number to the corner of the main menu
    [HarmonyPatch(typeof(VersionText), "Awake")]
    class Patch_VersionText
    {
        static bool Prefix(ref VersionText __instance)
        {
            TextMeshProUGUI text = __instance.GetComponent<TextMeshProUGUI>();
            text.text = "Nebulous v" + Application.version + "\n" + "Highlander v" + Utilities.GetHighlanderVersion();

            return false;
        }
    }

    // If items are hidden by mods, prevent displaying of them in the fleet editor
    [HarmonyPatch(typeof(ComponentPalette), "CreateItem")]
    class Patch_CreateItem
    {
        static bool Prefix(ref HullComponent component)
        {
            if (component != null)
            {
                return !NCH_BundleManager.Instance.IsItemHidden(component.SaveKey);
            }
            else
            {
                return true;
            }
        }
    }

    // Modded copies of existing components are set inactive for the copying process,
    // so they need to be set back to active when installed in a hull socket
    [HarmonyPatch(typeof(HullComponent), "SocketSet")]
    class Patch_SocketSet
    {
        static bool Prefix(ref HullComponent __instance)
        {
            GameObject prefab = __instance.gameObject;

            if (!prefab.activeSelf) prefab.SetActive(true);

            return true;
        }
    }

    // Display design warnings for fleets using mod-hidden components
    [HarmonyPatch(typeof(HullComponent), "GetDesignWarnings")]
    class Patch_GetDesignWarnings
    {
        static bool Prefix(ref HullComponent __instance, ref List<string> warnings)
        {
            if (NCH_BundleManager.Instance.IsItemHidden(__instance.SaveKey))
            {
                ModRecord mod = NCH_BundleManager.Instance.GetHiddenByMod(__instance.SaveKey);
                warnings.Add(
                    "'" + __instance.ComponentName + "' has been hidden from the fleet editor " +
                    "by the mod '" + mod.Info.ModName + "'");
            }

            return true;
        }
    }

    // Completely quit the game after altering mods, rather than restarting,
    // since Harmony patches won't function on the restart
    [HarmonyPatch(typeof(ModalModManager), "ApplyActiveMods")]
    class Patch_ApplyActiveMods
    {
        static bool Prefix(ref ModalModManager __instance)
        {
            Type type = typeof(ModalModManager);

            List<ModListItem> _activeMods = (List<ModListItem>)Utilities.GetPrivateValue(__instance, "_activeMods");
            ModDatabase.Instance.SetModsToLoad(_activeMods.ConvertAll<ModRecord>((ModListItem x) => x.Mod));

            Utilities.SetPrivateValue(__instance, "_pendingChanges", false);
            Utilities.CallPrivateMethod(__instance, "UpdateButtons", new object[] { });

            ModalConfirm warning = MenuController.Instance.OpenMenu<ModalConfirm>("Confirm");
            warning.Set("Changes to the active mod list have been applied.\nThe game will now exit.", "OK", false, delegate
            {
                Application.Quit();
            }, null);

            return false;
        }
    }
}
