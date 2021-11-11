﻿using HarmonyLib;
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Bundles;
using Modding;
using UI;
using Ships;

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

    [HarmonyPatch(typeof(ModalModManager), "ApplyActiveMods")]
    class Patch_ApplyActiveMods
    {
        static bool Prefix(ref ModalModManager __instance)
        {
            Type type = typeof(ModalModManager);

            FieldInfo activeMods = type.GetField("_activeMods", BindingFlags.NonPublic | BindingFlags.Instance);
            List<ModListItem> _activeMods = (List<ModListItem>)activeMods.GetValue(__instance);
            ModDatabase.Instance.SetModsToLoad(_activeMods.ConvertAll<ModRecord>((ModListItem x) => x.Mod));

            FieldInfo pendingChanges = type.GetField("_pendingChanges", BindingFlags.NonPublic | BindingFlags.Instance);
            bool _pendingChanges = (bool)pendingChanges.GetValue(__instance);
            _pendingChanges = false;

            MethodInfo updateButtons = type.GetMethod("UpdateButtons", BindingFlags.NonPublic | BindingFlags.Instance);
            updateButtons.Invoke(__instance, new object[] { });

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
        public static bool Prefix(ref BundleManager.ModLoadReport report)
        {
            foreach (ModRecord modRecord in report.Loaded)
            {
                CH_Utilities.ActivateHighlanderEventHook(modRecord, "OnModLoadedAtStartup", Plugin.logEventHooks);
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(LobbyModPane), "UpdateActiveTab")]
    class Patch_UpdateActiveTab
    {
        public static bool Prefix(
            ref ulong[] ____neededMods,
            ref List<ulong> ____missingMods,
            ref Coroutine ____downloadCoroutine)
        {
            if (____downloadCoroutine == null && ____missingMods.Count == 0 && ____neededMods.Length > 0)
            {
                foreach (ulong modID in ____neededMods)
                {
                    ModRecord modRecord = ModDatabase.Instance.GetModByID(modID);

                    CH_Utilities.ActivateHighlanderEventHook(modRecord, "OnModLoadedInLobby", Plugin.logEventHooks);
                }
            }

            return true;
        }
    }
}