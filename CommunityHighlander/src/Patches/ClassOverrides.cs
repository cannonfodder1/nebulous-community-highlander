using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;
using HarmonyLib;

using Bundles;
using Modding;

using CommunityHighlander.Overrides;

namespace CommunityHighlander.Patches
{
    internal class Redirection_Helpers
    {
        public static IEnumerable<MethodBase> RedirectionPatchTargets(int parameters)
        {
            return AccessTools.GetTypesFromAssembly(Assembly.Load("Nebulous.dll"))
                .SelectMany(type => type.GetMethods())
                .Where(method =>
                    method.DeclaringType == typeof(BundleManager) &&
                    method.GetParameters().Length == parameters &&
                    !Plugin.redirectionBlacklist.Contains(method.Name))
                .Cast<MethodBase>();
        }

        public static void RedirectionPatchPrefix(MethodBase __originalMethod, ref object __result, object[] parameters)
        {
            MethodInfo method = typeof(NCH_BundleManager).GetMethod(__originalMethod.Name);

            NCH_BundleManager bundleManager = null;

            if (!method.IsStatic)
            {
                bundleManager = NCH_BundleManager.Instance;
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
            __result = NCH_BundleManager.Instance.GetMunition(key);
            return false;
        }
    }

    [HarmonyPatch(typeof(BundleManager), "GetMunition", new Type[] { typeof(Guid) })]
    class Patch_GetMunition_FromGuid
    {
        static bool Prefix(ref object __result, ref Guid munitionKey)
        {
            if (Plugin.logRedirections) Debug.Log("Redirecting GetMunition() to Community Highlander");
            __result = NCH_BundleManager.Instance.GetMunition(munitionKey);
            return false;
        }
    }

    [HarmonyPatch(typeof(BundleManager), "Instance", MethodType.Getter)]
    class Patch_Instance
    {
        static bool Prefix(ref BundleManager __result)
        {
            if (Plugin.logRedirections) Debug.Log("Redirecting Instance() to Community Highlander");
            __result = NCH_BundleManager.Instance;
            return false;
        }
    }

    [HarmonyPatch(typeof(BundleManager), "IsInitialized", MethodType.Getter)]
    class Patch_IsInitialized
    {
        static bool Prefix(ref bool __result)
        {
            if (Plugin.logRedirections) Debug.Log("Redirecting IsInitialized() to Community Highlander");
            __result = NCH_BundleManager.IsInitialized;
            return false;
        }
    }

    [HarmonyPatch(typeof(BundleManager), "ProcessAssetBundle")]
    class Patch_ProcessAssetBundle
    {
        static bool Prefix(AssetBundle bundle, ModInfo fromMod)
        {
            if (Plugin.logRedirections) Debug.Log("Redirecting ProcessAssetBundle() to Community Highlander");
            NCH_BundleManager.Instance.ProcessAssetBundle(bundle, fromMod);
            return false;
        }

        static void Postfix()
        {
            if (Plugin.logMiscellaneous) Debug.Log($"Community Highlander Total Components: {NCH_BundleManager.Instance.AllComponents.Count}");
        }
    }
}
