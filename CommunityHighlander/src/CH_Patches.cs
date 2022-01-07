﻿using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;
using TMPro;
using Steamworks;
using Steamworks.Data;

using Bundles;
using Modding;
using UI;
using Ships;
using FleetEditor;
using Networking;
using Game;

namespace CommunityHighlander
{
    ///////////////////////////
    /// REDIRECTION HELPERS ///
    ///////////////////////////

    internal class Redirection_Helpers
    {
        public static IEnumerable<MethodBase> RedirectionPatchTargets(int parameters)
        {
            return AccessTools.GetTypesFromAssembly(Assembly.Load("Nebulous.dll"))
                .SelectMany(type => type.GetMethods())
                .Where(method =>
                    method.DeclaringType == typeof(BundleManager) &&
                    method.GetParameters().Length == parameters &&
                    !Plugin.patchBlacklist.Contains(method.Name))
                .Cast<MethodBase>();
        }

        public static void RedirectionPatchPrefix(MethodBase __originalMethod, ref object __result, object[] parameters)
        {
            MethodInfo method = typeof(CH_BundleManager).GetMethod(__originalMethod.Name);

            CH_BundleManager bundleManager = null;

            if (!method.IsStatic)
            {
                bundleManager = CH_BundleManager.Instance;
            }

            if (typeof(BundleManager).GetMethod(__originalMethod.Name).ReturnType == typeof(void))
            {
                method.Invoke(bundleManager, parameters);
            }
            else
            {
                __result = method.Invoke(bundleManager, parameters);
            }
        }
    }



    ///////////////////////////
    /// REDIRECTION PATCHES ///
    ///////////////////////////

    [HarmonyPatch]
    class Patch_Arguments_0
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            return Redirection_Helpers.RedirectionPatchTargets(0);
        }

        static bool Prefix(MethodBase __originalMethod, ref object __result)
        {
            if (Plugin.logRedirections) Debug.Log($"Redirecting {__originalMethod.Name}() to Community Highlander");

            Redirection_Helpers.RedirectionPatchPrefix(__originalMethod, ref __result, null);

            return false;
        }
    }

    [HarmonyPatch]
    class Patch_Arguments_1
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            return Redirection_Helpers.RedirectionPatchTargets(1);
        }

        static bool Prefix(MethodBase __originalMethod, ref object __result, ref object __0)
        {
            if (Plugin.logRedirections) Debug.Log($"Redirecting {__originalMethod.Name}() to Community Highlander");

            Redirection_Helpers.RedirectionPatchPrefix(__originalMethod, ref __result, new[] { __0 });

            return false;
        }
    }

    [HarmonyPatch(typeof(BundleManager), "GetMunition", new Type[] { typeof(string) })]
    class Patch_GetMunition_FromKey
    {
        static bool Prefix(ref object __result, ref string key)
        {
            if (Plugin.logRedirections) Debug.Log("Redirecting GetMunition() to Community Highlander");
            __result = CH_BundleManager.Instance.GetMunition(key);
            return false;
        }
    }

    [HarmonyPatch(typeof(BundleManager), "GetMunition", new Type[] { typeof(Guid) })]
    class Patch_GetMunition_FromGuid
    {
        static bool Prefix(ref object __result, ref Guid munitionKey)
        {
            if (Plugin.logRedirections) Debug.Log("Redirecting GetMunition() to Community Highlander");
            __result = CH_BundleManager.Instance.GetMunition(munitionKey);
            return false;
        }
    }

    [HarmonyPatch(typeof(BundleManager), "Instance", MethodType.Getter)]
    class Patch_Instance
    {
        static bool Prefix(ref BundleManager __result)
        {
            if (Plugin.logRedirections) Debug.Log("Redirecting Instance() to Community Highlander");
            __result = CH_BundleManager.Instance;
            return false;
        }
    }

    [HarmonyPatch(typeof(BundleManager), "IsInitialized", MethodType.Getter)]
    class Patch_IsInitialized
    {
        static bool Prefix(ref bool __result)
        {
            if (Plugin.logRedirections) Debug.Log("Redirecting IsInitialized() to Community Highlander");
            __result = CH_BundleManager.IsInitialized;
            return false;
        }
    }

    [HarmonyPatch(typeof(BundleManager), "ProcessAssetBundle")]
    class Patch_ProcessAssetBundle
    {
        static bool Prefix(AssetBundle bundle, ModInfo fromMod)
        {
            if (Plugin.logRedirections) Debug.Log("Redirecting ProcessAssetBundle() to Community Highlander");
            CH_BundleManager.Instance.ProcessAssetBundle(bundle, fromMod);
            return false;
        }

        static void Postfix()
        {
            if (Plugin.logMiscellaneous) Debug.Log($"Community Highlander Total Components: {CH_BundleManager.Instance.AllComponents.Count}");
        }
    }



    /////////////////////
    /// OTHER PATCHES ///
    /////////////////////

    [HarmonyPatch(typeof(VersionText), "Awake")]
    class Patch_VersionText
    {
        static bool Prefix(ref VersionText __instance)
        {
            TextMeshProUGUI text = __instance.GetComponent<TextMeshProUGUI>();
            text.text = "Version " + Application.version + "\n" + "Highlander " + CH_Utilities.GetHighlanderVersion();

            return false;
        }
    }

    [HarmonyPatch(typeof(ComponentPalette), "CreateItem")]
    class Patch_CreateItem
    {
        static bool Prefix(ref HullComponent component)
        {
            if (component != null)
            {
                return !CH_BundleManager.Instance.IsItemHidden(component.SaveKey);
            }
            else
            {
                return true;
            }
        }
    }

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

    [HarmonyPatch(typeof(HullComponent), "GetDesignWarnings")]
    class Patch_GetDesignWarnings
    {
        static bool Prefix(ref HullComponent __instance, ref List<string> warnings)
        {
            if (CH_BundleManager.Instance.IsItemHidden(__instance.SaveKey))
            {
                ModRecord mod = CH_BundleManager.Instance.GetHiddenByMod(__instance.SaveKey);
                warnings.Add(
                    "'" + __instance.ComponentName + "' has been hidden from the fleet editor " +
                    "by the mod '" + mod.Info.ModName + "'");
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(ModalModManager), "ApplyActiveMods")]
    class Patch_ApplyActiveMods
    {
        static bool Prefix(ref ModalModManager __instance)
        {
            Type type = typeof(ModalModManager);

            List<ModListItem> _activeMods = (List<ModListItem>)CH_Utilities.GetPrivateValue(__instance, "_activeMods");
            ModDatabase.Instance.SetModsToLoad(_activeMods.ConvertAll<ModRecord>((ModListItem x) => x.Mod));

            CH_Utilities.SetPrivateValue(__instance, "_pendingChanges", false);
            CH_Utilities.CallPrivateMethod(__instance, "UpdateButtons", new object[] { });

            ModalConfirm warning = MenuController.Instance.OpenMenu<ModalConfirm>("Confirm");
            warning.Set("Changes to the active mod list have been applied.\nThe game will now exit.", "OK", false, delegate
            {
                Application.Quit();
            }, null);

            return false;
        }
    }

    [HarmonyPatch(typeof(MainMenu), "OnFinishedLoading")]
    class Patch_OnFinishedLoading
    {
        static bool Prefix(ref BundleManager.ModLoadReport report, ref List<CH_EventHookManager.HookRegisterReport> __state)
        {
            __state = new();
            List<ulong> modIDs = new();

            foreach (ModRecord modRecord in report.Loaded)
            {
                __state.Add(CH_EventHookManager.Instance.RegisterEventHook(modRecord, Plugin.logEventHooks));

                modIDs.Add(modRecord.Info.UniqueIdentifier);
            }

            CH_EventHookManager.Instance.TriggerEvent("OnModLoadedAtStartup", modIDs, Plugin.logEventHooks);

            return true;
        }

        static void Postfix(ref List<CH_EventHookManager.HookRegisterReport> __state)
        {
            string current = CH_Utilities.GetHighlanderVersion();

            foreach (CH_EventHookManager.HookRegisterReport report in __state)
            {
                ModalConfirm warning = MenuController.Instance.OpenMenu<ModalConfirm>("Confirm");
                warning.Set(
                    $"{report.modName} requires a highlander version between {report.minimum} and {report.maximum}.\n" +
                    $"Your installed version is {current}.\n\n" +
                    $"This mod may not work correctly or cause technical issues. Please install a valid highlander version.",
                    "Understood",
                    false, null, null
                );
            }
        }
    }

    [HarmonyPatch(typeof(LobbyModPane), "UpdateActiveTab")]
    class Patch_UpdateActiveTab
    {
        static bool Prefix(
            ref ulong[] ____neededMods,
            ref List<ulong> ____missingMods,
            ref Coroutine ____downloadCoroutine)
        {
            if (____downloadCoroutine == null && ____missingMods.Count == 0 && ____neededMods.Length > 0)
            {
                List<ulong> modIDs = new();

                foreach (ulong modID in ____neededMods)
                {
                    ModRecord modRecord = ModDatabase.Instance.GetModByID(modID);

                    CH_EventHookManager.Instance.RegisterEventHook(modRecord, Plugin.logEventHooks);

                    modIDs.Add(modID);
                }

                CH_EventHookManager.Instance.TriggerEvent("OnModLoadedInLobby", modIDs, Plugin.logEventHooks);
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(PortableNetworkManager), "SetLobbyName")]
    class Patch_SetLobbyName
    {
        static void Postfix(ref PortableNetworkManager __instance)
        {
            Debug.Log("PortableNetworkManager::SetLobbyName CALLED");

            if (__instance == null)
            {
                Debug.Log("Manager NULL");
                return;
            }

            Lobby? hostedLobby = (Lobby?)CH_Utilities.GetPrivateValue(__instance, "_hostedLobby");
            if (hostedLobby == null)
            {
                Debug.Log("Lobby NULL");
                return;
            }

            hostedLobby.Value.SetData("NCH_host", CH_Utilities.GetHighlanderVersion());
        }
    }

    [HarmonyPatch(typeof(MPMatchEntry), "Set")]
    class Patch_Set
    {
        static void Postfix(ref MPMatchEntry __instance, ref SteamLobby lobby)
        {
            Debug.Log("MPMatchEntry::Set CALLED");

            if (__instance == null)
            {
                Debug.Log("Match NULL");
                return;
            }

            Lobby? lobbyData = (Lobby?)CH_Utilities.GetPrivateValue(lobby, "_lobby");
            if (lobbyData == null)
            {
                Debug.Log("Lobby NULL");
                return;
            }

            string version = lobbyData.Value.GetData("NCH_version");

            Debug.Log($"{lobby.Name} version is {((version != null && version.Length > 0) ? version : "NULL")}");

            TextMeshProUGUI matchName = (TextMeshProUGUI)CH_Utilities.GetPrivateValue(__instance, "_matchName");
            matchName.text = "<b>" + CH_Utilities.FormatRemoteVersionTag(matchName.text, version) + "</b>";
        }
    }
    
    [HarmonyPatch(typeof(SkirmishLobbyMenu), "HandlePlayerAdded")]
    class Patch_HandlePlayerAdded
    {
        static void Postfix(ref SkirmishLobbyMenu __instance, ref IPlayer player)
        {
            Debug.Log("SkirmishLobbyMenu::HandlePlayerAdded CALLED");

            GameManager gameManager = GameManager.Instance;

            if (player.PlayerName == "Joining Player..." || player.IsBot || gameManager.IsSoloGame) return;

            PortableNetworkManager networkManager = (PortableNetworkManager)CH_Utilities.GetPrivateProperty(gameManager, "_netManager");
            Lobby? lobby = (Lobby?)CH_Utilities.GetPrivateValue(networkManager.LobbyInfo, "_lobby");

            if (lobby.Value.GetData("NCH_host").Length > 0)
            {
                List<SkirmishLobbyPlayerSlot> slots = (List<SkirmishLobbyPlayerSlot>)CH_Utilities.GetPrivateValue(__instance, "_slots");
                SkirmishLobbyPlayerSlot slot = slots.Last();

                string finalText = CH_Utilities.FormatMissingVersionTag(player.PlayerName);
                slot.SetPlayerName(finalText);
            }
        }
    }
    
    [HarmonyPatch(typeof(LobbyPlayer), "SyncPlayerNameChangedInternal")]
    class Patch_SyncPlayerNameChangedInternal
    {
        static void Postfix(ref LobbyPlayer __instance, ref string oldName, ref string newName)
        {
            Debug.Log("LobbyPlayer::SyncPlayerNameChangedInternal CALLED");

            GameManager gameManager = GameManager.Instance;

            if (newName == "Joining Player..." || __instance.IsBot || gameManager.IsSoloGame) return;

            PortableNetworkManager networkManager = (PortableNetworkManager)CH_Utilities.GetPrivateProperty(gameManager, "_netManager");
            Lobby? lobby = (Lobby?)CH_Utilities.GetPrivateValue(networkManager.LobbyInfo, "_lobby");

            if (lobby.Value.GetData("NCH_host").Length > 0)
            {
                string finalText = CH_Utilities.FormatMissingVersionTag(__instance.PlayerName);
                CH_Utilities.CallPrivateMethod(__instance, "OnPlayerNameChanged", new object[] { finalText });
            }
        }
    }
    
    [HarmonyPatch(typeof(SkirmishLobbyPlayer), "OnStartAuthority")]
    class Patch_OnStartAuthority
    {
        static void Postfix(ref SkirmishLobbyPlayer __instance)
        {
            Debug.Log("LobbyPlayer::OnStartAuthority CALLED");

            GameManager gameManager = GameManager.Instance;
            if (__instance.IsBot || gameManager.IsSoloGame) return;

            PortableNetworkManager networkManager = (PortableNetworkManager)CH_Utilities.GetPrivateProperty(gameManager, "_netManager");
            Lobby? lobby = (Lobby?)CH_Utilities.GetPrivateValue(networkManager.LobbyInfo, "_lobby");
            string lobbyVersion = lobby.Value.GetData("NCH_host");

            if (lobbyVersion.Length > 0)
            {
                string finalText = CH_Utilities.FormatLocalVersionTag(SteamClient.Name, lobbyVersion);
                CH_Utilities.CallPrivateMethod(__instance, "CmdSetPlayerName", new object[] { finalText }, true);
            }
        }
    }
}
