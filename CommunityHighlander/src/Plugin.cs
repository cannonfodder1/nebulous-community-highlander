using BepInEx;
using HarmonyLib;

using System.Reflection;
using System.Collections.Generic;

namespace CommunityHighlander
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static bool logMiscellaneous = false;
        public static bool logRedirections = false;
        public static bool logEventHooks = false;
        public static bool logOperations = false;

        public static List<string> redirectionBlacklist;

        private void Awake()
        {
            Logger.LogInfo($"Community Highlander version {PluginInfo.PLUGIN_VERSION} is loaded!");
            
            redirectionBlacklist = new List<string>();
            redirectionBlacklist.Add("get_Instance");
            redirectionBlacklist.Add("get_IsInitialized");
            redirectionBlacklist.Add("ProcessAssetBundle");
            redirectionBlacklist.Add("GetMunition");
            // Any methods with more than 1 parameter are automatically blacklisted
            // Because going anywhere near the asynchronous methods is a recipe for disaster

            var harmony = new Harmony("CommunityHighlander");
            harmony.PatchAll();

            if (logMiscellaneous)
            {
                Logger.LogInfo($"Harmony Patching:");

                foreach (MethodBase method in harmony.GetPatchedMethods())
                {
                    Logger.LogInfo($" - {method.DeclaringType.Name}::{method.Name}()");
                }
            }

            // Note: before LoadAllBundlesAsync finishes running, there can be no instantiation of BundleManager under any circumstances
        }
    }
}
