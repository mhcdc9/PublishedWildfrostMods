using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
using Color = UnityEngine.Color;
using Steamworks;
using System.Collections;
using MultiplayerBase.Handlers;
using MultiplayerBase.UI;

namespace MultiplayerBase.Matchmaking
{
    public class MatchmakingDashboard : MonoBehaviour
    {
        public static Lobby? lobby;
        public static Lobby[] lobbyList;

        internal GameObject background;
        internal Button createLobbyButton;
        internal Button findLobbyButton;
        internal Button joinLobbyButton;
        internal Button leaveLobbyButton;
        internal Button finalizeButton;

        internal LobbyView lobbyView;

        internal Button[] lobbyButtons = new Button[0];
        public int index = -1;

        public void CreateObjects()
        {
            transform.SetAsFirstSibling();

            background = HelperUI.Background(transform, new Color(0f, 0f, 0f, .9f));

            createLobbyButton = HelperUI.ButtonTemplate(transform,new Vector2(2,0.8f), new Vector3(-1.5f, -3, 0), "Create", Color.white);
            findLobbyButton = HelperUI.ButtonTemplate(transform, new Vector2(2, 0.8f), new Vector3(-1.5f, -4, 0), "Find", Color.white);
            joinLobbyButton = HelperUI.ButtonTemplate(transform, new Vector2(2, 0.8f), new Vector3(1.5f, -4, 0), "Join", Color.white);
            joinLobbyButton.interactable = false;
            leaveLobbyButton = HelperUI.ButtonTemplate(transform, new Vector2(2, 0.8f), new Vector3(1.5f, -3, 0), "Leave", Color.white);
            leaveLobbyButton.interactable = false;
            finalizeButton = HelperUI.ButtonTemplate(transform, new Vector2(3, 0.8f), new Vector3(0f, -5, 0), "Finalize", Color.white);
            //finalizeButton.interactable = false;

            createLobbyButton.onClick.AddListener(CreateLobby);
            findLobbyButton.onClick.AddListener(FindLobby);
            joinLobbyButton.onClick.AddListener(JoinLobby);
            leaveLobbyButton.onClick.AddListener(LeaveLobby);
            finalizeButton.onClick.AddListener(FinalizeParty);

            lobbyView = LobbyView.Create(transform);
        }

        public void CreateLobbyView(Lobby[] lobbies)
        {
            index = -1;
            joinLobbyButton.interactable = false;
            for (int i = lobbyButtons.Length - 1; i >= 0; i--)
            {
                lobbyButtons[i].gameObject.Destroy();
            }
            lobbyButtons = new Button[lobbies.Length];
            for (int i = 0; i < lobbies.Length; i++)
            {
                lobbyButtons[i] = HelperUI.ButtonTemplate(transform, new Vector2(5, 1.3f), new Vector3(0, 3 - 1.5f*i, 0), $"{lobbies[i].GetData("name")}", Color.white);
                lobbyButtons[i].GetComponentInChildren<TextMeshProUGUI>().fontSize = 0.6f;
                int j = i;
                lobbyButtons[i].onClick.AddListener(() => SelectLobby(j));
            }
        }

        public void FinalizeParty()
        {
            if (lobby is Lobby lob)
            {
                lob.SetPrivate();
                MultiplayerMain.SendMessage("AndSoThePartyIsFinallyFinalized");
            }
            else
            {
                MultiplayerMain.instance.DebugMode();
            }
        }

        public IEnumerator EndLobby()
        {
            yield return new WaitForSeconds(3.0f);
            if (lobby is Lobby lob)
            {
                lob.Leave();
                leaveLobbyButton.interactable = false;
                finalizeButton.interactable = false;
            }
        }

        public void SelectLobby(int newIndex)
        {
            if (index != -1)
            {
                lobbyButtons[index].GetComponent<Image>().color = Color.white;
            }
            if (newIndex == index)
            {
                index = -1;
                joinLobbyButton.interactable = false;
                return;
            }
            index = newIndex;
            lobbyButtons[index].GetComponent<Image>().color = Color.green;
            joinLobbyButton.interactable = true;
        }

        private async void CreateLobby()
        {
            Debug.Log("[Multiplayer] Lobby creation request");
            lobby = await SteamMatchmaking.CreateLobbyAsync(2);
            if (lobby is Lobby lob)
            {
                lob.SetData("name", $"{HandlerSystem.self.Name}");
                lob.SetPublic();
                MultiplayerMain.instance.HookToChatRoom();
                leaveLobbyButton.interactable = true;
                finalizeButton.interactable = true;
                findLobbyButton.interactable = false;
                Debug.Log("[Multiplayer] You have created a lobby.");
            }
        }

        internal async void FindLobby()
        {
            Debug.Log("[Multiplayer] Find lobby request");
            MultiplayerMain.textElement.text = "<color=#FC5>Searching for a lobby...</color>";
            Lobby[] lobbies = await SteamMatchmaking.LobbyList.RequestAsync();
            Debug.Log("[Multiplayer] Found lobbies");
            if (lobbies == null)
            {
                MultiplayerMain.textElement.text = "No lobbies found :(";
                lobbies = new Lobby[0];
                return;
            }
            MultiplayerMain.textElement.text = $"<size=0.55><color=#FC5>{lobbies.Length} lobbies found!</color></size>";
            foreach (Lobby lobby in lobbies)
            {
                Debug.Log(lobby.Owner.Name);
            }
            lobbyList = lobbies;
            CreateLobbyView(lobbyList);
        }

        private async void JoinLobby()
        {
            Debug.Log("[Multiplayer] Joining lobby");
            if (index == -1 || lobby!= null)
            {
                throw new Exception("Pick something first! Or leave your current lobby.");
            }
            RoomEnter enter = await lobbyList[index].Join();
            if (enter == RoomEnter.Success)
            {
                Debug.Log("[Multiplayer] Successfully joined");
                lobby= lobbyList[index];
                MultiplayerMain.isHost = false;
                MultiplayerMain.instance.HookToChatRoom();
                leaveLobbyButton.interactable = true;
                findLobbyButton.interactable = false;
            }
            else
            {
                Debug.Log("[Multiplayer] Join failed");
            }
        }

        private void LeaveLobby()
        {
            Debug.Log("[Multiplayer] Leaving lobby");
            if (lobby is Lobby lob)
            {
                lob.Leave();
                MultiplayerMain.isHost = true;
                MultiplayerMain.instance.UnhookToChatRoom();
                leaveLobbyButton.interactable = false;
                finalizeButton.interactable = false;
                findLobbyButton.interactable = true;
                lobby = null;
            }
        }
    }
}
