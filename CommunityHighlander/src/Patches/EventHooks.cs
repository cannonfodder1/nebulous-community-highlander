using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;

using Bundles;
using Modding;
using UI;
using CommunityHighlander.Framework;
using CommunityHighlander.Helpers;

namespace CommunityHighlander.Patches
{
    [HarmonyPatch(typeof(MainMenu), "OnFinishedLoading")]
    class Patch_OnFinishedLoading
    {
        static bool Prefix(ref BundleManager.ModLoadReport report, ref List<EventListenerManager.ListenerRegisterReport> __state)
        {
            __state = new();
            List<ulong> modIDs = new();

            foreach (ModRecord modRecord in report.Loaded)
            {
                EventListenerManager.ListenerRegisterReport regReport = EventListenerManager.Instance.RegisterEventListener(modRecord, Plugin.logEventHooks);
                
                if (Plugin.logEventHooks) Debug.Log($"Prefix {regReport.modName}: {regReport.minimum}/{regReport.maximum} = {regReport.result}");
               
                __state.Add(regReport);
                modIDs.Add(modRecord.Info.UniqueIdentifier);
            }

            EventListenerManager.Instance.TriggerEventHook("OnModLoadedAtStartup", modIDs, Plugin.logEventHooks);

            return true;
        }

        static void Postfix(ref List<EventListenerManager.ListenerRegisterReport> __state)
        {
            string current = Utilities.GetHighlanderVersion();

            foreach (EventListenerManager.ListenerRegisterReport report in __state)
            {
                if (Plugin.logEventHooks) Debug.Log($"Postfix {report.modName}: {report.minimum}/{report.maximum} = {report.result}");

                if (report.result == EventListenerManager.ListenerRegisterResult.VersionMismatch)
                {
                    ModalConfirm warning = MenuController.Instance.OpenMenu<ModalConfirm>("Confirm");
                    warning.Set(
                        $"{report.modName} requires a highlander version between {report.minimum} and {report.maximum}.\n" +
                        $"Your installed version is {current}.\n\n" +
                        $"This mod may work incorrectly or cause technical issues. Please install a valid highlander version.",
                        "Understood",
                        false, null, null
                    );
                }
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

                    EventListenerManager.Instance.RegisterEventListener(modRecord, Plugin.logEventHooks);

                    modIDs.Add(modID);
                }

                EventListenerManager.Instance.TriggerEventHook("OnModLoadedInLobby", modIDs, Plugin.logEventHooks);
            }

            return true;
        }
    }
}
