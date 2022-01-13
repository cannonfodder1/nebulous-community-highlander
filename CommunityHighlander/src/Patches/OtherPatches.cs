using System;
using System.Linq;
using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;
using TMPro;

using UI;
using Ships;
using Ships.Controls;
using Modding;
using FleetEditor;
using Bundles;
using Munitions;

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

    // If components are hidden by mods, prevent displaying of them in the fleet editor
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

    // If munitions are hidden by mods, prevent displaying of them in the fleet editor
    [HarmonyPatch(typeof(SettingsMagazineLoadout), "AddMagazine")]
    class Patch_AddMagazine
    {
        static bool Prefix(ref SettingsMagazineLoadout __instance)
        {
            Hull hull = (Hull)Utilities.GetPrivateValue(__instance, "_hull");
            IMagazineProvider provider = (IMagazineProvider)Utilities.GetPrivateValue(__instance, "_provider");
            GameObject ammoSelectPrefab = (GameObject)Utilities.GetPrivateValue(__instance, "_ammoSelectPrefab");

            SettingsMagazineLoadout instance = __instance;

            if (!(provider.UsedCapacity >= provider.MaxCapacity))
            {
                List<SelectableListItem> validAmmo = new List<SelectableListItem>();
                using (IEnumerator<IMunition> enumerator = BundleManager.Instance.AllMunitions.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        IMunition ammo = enumerator.Current;

                        if (provider.RestrictionCheck(ammo) && !NCH_BundleManager.Instance.IsItemHidden(ammo.SaveKey))
                        {
                            GameObject entryObj = UnityEngine.Object.Instantiate<GameObject>(ammoSelectPrefab);
                            entryObj.name = ammo.MunitionName;
                            DoubleTextListItem entry = entryObj.GetComponent<DoubleTextListItem>();
                            entry.Set(ammo.MunitionName, ammo.GetDescription(), null, ammo);
                            validAmmo.Add(entry);
                            entry.SetDetailsCallback(delegate (out string title, out string subtitle, out Sprite image, out string details)
                            {
                                title = ammo.MunitionName;
                                subtitle = string.Format("{0}pts. per {1} units", ammo.PointCost, ammo.PointDivision);
                                image = ammo.DetailScreenshot;
                                details = ammo.GetDetailText();
                            });
                        }
                    }
                }
                validAmmo.Sort((SelectableListItem a, SelectableListItem b) => a.name.CompareTo(b.name));

                ModalListSelectDetailed select = MenuController.Instance.OpenMenu<ModalListSelectDetailed>("List Select Detailed");
                select.Set("Select Munition", "Confirm", validAmmo, delegate (SelectableListItem selected)
                {
                    IMagazine mag = provider.AddToMagazine(selected.Data as IMunition, 1U);
                    if (mag != null)
                    {
                        Utilities.CallPrivateMethod(instance, "CreateMagazineEntry", new object[] { mag });
                        instance.UpdateQuantities();
                    }
                }, null, 0, true);

                if (provider.CanFeedExternally)
                {
                    List<IWeapon> weapons = hull.CollectComponents<IWeapon>();
                    select.SetFilter("For Current Mounts Only", delegate (object data)
                    {
                        IMunition ammo = data as IMunition;
                        bool flag3 = ammo != null;
                        return flag3 && weapons.Any((IWeapon x) => x.NeedsExternalAmmoFeed && x.IsAmmoCompatible(ammo));
                    });
                }
            }

            return false;
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
