using System.Linq;
using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;
using TMPro;
using Steamworks;
using Steamworks.Data;

using Networking;
using Game;
using UI;
using CommunityHighlander.Helpers;

namespace CommunityHighlander.Patches
{
    [HarmonyPatch(typeof(PortableNetworkManager), "SetLobbyName")]
    class Patch_SetLobbyName
    {
        static void Postfix(ref PortableNetworkManager __instance)
        {
            if (Plugin.logMiscellaneous) Debug.Log("PortableNetworkManager::SetLobbyName CALLED");

            if (__instance == null)
            {
                if (Plugin.logMiscellaneous) Debug.LogWarning("Manager NULL");
                return;
            }

            Lobby? hostedLobby = (Lobby?)Utilities.GetPrivateValue(__instance, "_hostedLobby");
            if (hostedLobby == null)
            {
                if (Plugin.logMiscellaneous) Debug.LogWarning("Lobby NULL");
                return;
            }

            hostedLobby.Value.SetData("NCH_host", Utilities.GetHighlanderVersion());
        }
    }

    [HarmonyPatch(typeof(MPMatchEntry), "Set")]
    class Patch_Set
    {
        static void Postfix(ref MPMatchEntry __instance, ref SteamLobby lobby)
        {
            if (Plugin.logMiscellaneous) Debug.Log("MPMatchEntry::Set CALLED");

            if (__instance == null)
            {
                if (Plugin.logMiscellaneous) Debug.LogWarning("Match NULL");
                return;
            }

            Lobby? lobbyData = (Lobby?)Utilities.GetPrivateValue(lobby, "_lobby");
            if (lobbyData == null)
            {
                if (Plugin.logMiscellaneous) Debug.LogWarning("Lobby NULL");
                return;
            }

            string version = lobbyData.Value.GetData("NCH_version");

            if (Plugin.logMiscellaneous) Debug.Log($"{lobby.Name} version is {((version != null && version.Length > 0) ? version : "NULL")}");

            TextMeshProUGUI matchName = (TextMeshProUGUI)Utilities.GetPrivateValue(__instance, "_matchName");
            matchName.text = "<b>" + Utilities.FormatRemoteVersionTag(matchName.text, version) + "</b>";
        }
    }

    [HarmonyPatch(typeof(SkirmishLobbyMenu), "HandlePlayerAdded")]
    class Patch_HandlePlayerAdded
    {
        static void Postfix(ref SkirmishLobbyMenu __instance, ref IPlayer player)
        {
            if (Plugin.logMiscellaneous) Debug.Log("SkirmishLobbyMenu::HandlePlayerAdded CALLED");

            GameManager gameManager = GameManager.Instance;

            if (player.PlayerName == "Joining Player..." || player.IsBot || gameManager.IsSoloGame) return;

            PortableNetworkManager networkManager = (PortableNetworkManager)Utilities.GetPrivateProperty(gameManager, "_netManager");
            Lobby? lobby = (Lobby?)Utilities.GetPrivateValue(networkManager.LobbyInfo, "_lobby");

            if (lobby.Value.GetData("NCH_host").Length > 0)
            {
                List<SkirmishLobbyPlayerSlot> slots = (List<SkirmishLobbyPlayerSlot>)Utilities.GetPrivateValue(__instance, "_slots");
                SkirmishLobbyPlayerSlot slot = slots.Last();

                string finalText = Utilities.FormatMissingVersionTag(player.PlayerName);
                slot.SetPlayerName(finalText);
            }
        }
    }

    [HarmonyPatch(typeof(LobbyPlayer), "SyncPlayerNameChangedInternal")]
    class Patch_SyncPlayerNameChangedInternal
    {
        static void Postfix(ref LobbyPlayer __instance, ref string oldName, ref string newName)
        {
            if (Plugin.logMiscellaneous) Debug.Log("LobbyPlayer::SyncPlayerNameChangedInternal CALLED");

            GameManager gameManager = GameManager.Instance;

            if (newName == "Joining Player..." || __instance.IsBot || gameManager.IsSoloGame) return;

            PortableNetworkManager networkManager = (PortableNetworkManager)Utilities.GetPrivateProperty(gameManager, "_netManager");
            Lobby? lobby = (Lobby?)Utilities.GetPrivateValue(networkManager.LobbyInfo, "_lobby");

            if (lobby.Value.GetData("NCH_host").Length > 0)
            {
                string finalText = Utilities.FormatMissingVersionTag(__instance.PlayerName);
                Utilities.CallPrivateMethod(__instance, "OnPlayerNameChanged", new object[] { finalText });
            }
        }
    }

    [HarmonyPatch(typeof(SkirmishLobbyPlayer), "OnStartAuthority")]
    class Patch_OnStartAuthority
    {
        static void Postfix(ref SkirmishLobbyPlayer __instance)
        {
            if (Plugin.logMiscellaneous) Debug.Log("LobbyPlayer::OnStartAuthority CALLED");

            GameManager gameManager = GameManager.Instance;
            if (__instance.IsBot || gameManager.IsSoloGame) return;

            PortableNetworkManager networkManager = (PortableNetworkManager)Utilities.GetPrivateProperty(gameManager, "_netManager");
            Lobby? lobby = (Lobby?)Utilities.GetPrivateValue(networkManager.LobbyInfo, "_lobby");
            string lobbyVersion = lobby.Value.GetData("NCH_host");

            if (lobbyVersion.Length > 0)
            {
                string finalText = Utilities.FormatLocalVersionTag(SteamClient.Name, lobbyVersion);
                Utilities.CallPrivateMethod(__instance, "CmdSetPlayerName", new object[] { finalText }, true);
            }
        }
    }

    [HarmonyPatch(typeof(SkirmishPlayer), "SetCommonPlayerData")]
    class Patch_SetCommonPlayerData
    {
        static void Postfix(ref SkirmishPlayer __instance, PlayerHandoffData data)
        {
            if (Plugin.logMiscellaneous) Debug.Log("SkirmishPlayer::SetCommonPlayerData CALLED");

            GameManager gameManager = GameManager.Instance;
            if (__instance.IsBot || gameManager.IsSoloGame) return;

            PortableNetworkManager networkManager = (PortableNetworkManager)Utilities.GetPrivateProperty(gameManager, "_netManager");
            Lobby? lobby = (Lobby?)Utilities.GetPrivateValue(networkManager.LobbyInfo, "_lobby");
            string lobbyVersion = lobby.Value.GetData("NCH_host");

            if (lobbyVersion.Length > 0)
            {
                string finalText = Utilities.UndoLocalVersionTag(data.PlayerName, lobbyVersion);
                __instance.Network_playerName = finalText;
            }
        }
    }
}
