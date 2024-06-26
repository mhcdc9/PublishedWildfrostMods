﻿using Steamworks.Data;
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
        internal GameObject buttonGroup;
        internal Button createLobbyButton;
        internal Button findLobbyButton;
        internal Button joinLobbyButton;
        internal Button leaveLobbyButton;
        internal Button finalizeButton;

        internal MemberView memberView;
        internal ModView modView;

        internal Button[] lobbyButtons = new Button[0];
        public int index = -1;

        public void CreateObjects()
        {
            transform.SetAsFirstSibling();

            background = HelperUI.Background(transform, new Color(0f, 0f, 0f, .8f));
            Fader fader = background.AddComponent<Fader>();
            fader.onEnable = true;
            fader.gradient = new Gradient();
            GradientColorKey[] colors = new GradientColorKey[]
            {
                new GradientColorKey(Color.black, 0f),
                new GradientColorKey(Color.black, 1f)
            };
            GradientAlphaKey[] alphas = new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.8f, 1f)
            };
            fader.gradient.SetKeys(colors, alphas);

            buttonGroup = new GameObject("Button Group");
            buttonGroup.AddComponent<Image>().color = Color.black;
            buttonGroup.GetComponent<RectTransform>().sizeDelta = new Vector2(5.5f, 3.5f);
            buttonGroup.transform.SetParent(transform, false);
            buttonGroup.transform.localPosition = new Vector3(0, -4f, 0);
            TweenUI tween = buttonGroup.AddComponent<TweenUI>();
            tween.target = buttonGroup;
            tween.property = TweenUI.Property.Move;
            tween.ease = LeanTweenType.easeOutBounce;
            tween.fireOnEnable = true;
            tween.duration = 0.75f;
            tween.to = new Vector3(0, -4f, 0);
            tween.hasFrom = true;
            tween.from = new Vector3(0, -8f, 0);

            createLobbyButton = HelperUI.ButtonTemplate(buttonGroup.transform,new Vector2(2,0.8f), new Vector3(-1.5f, 1, 0), "Create", Color.white);
            findLobbyButton = HelperUI.ButtonTemplate(buttonGroup.transform, new Vector2(2, 0.8f), new Vector3(-1.5f, 0, 0), "Find", Color.white);
            joinLobbyButton = HelperUI.ButtonTemplate(buttonGroup.transform, new Vector2(2, 0.8f), new Vector3(1.5f, 0, 0), "Join", Color.white);
            joinLobbyButton.interactable = false;
            leaveLobbyButton = HelperUI.ButtonTemplate(buttonGroup.transform, new Vector2(2, 0.8f), new Vector3(1.5f, 1, 0), "Leave", Color.white);
            leaveLobbyButton.interactable = false;
            finalizeButton = HelperUI.ButtonTemplate(buttonGroup.transform, new Vector2(3, 0.8f), new Vector3(0f, -1, 0), "Finalize", Color.white);
            //finalizeButton.interactable = false;

            createLobbyButton.onClick.AddListener(CreateLobby);
            findLobbyButton.onClick.AddListener(FindLobby);
            joinLobbyButton.onClick.AddListener(JoinLobby);
            leaveLobbyButton.onClick.AddListener(LeaveLobby);
            finalizeButton.onClick.AddListener(FinalizeParty);

            memberView = MemberView.Create(transform);
            modView = ModView.Create(transform);
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
            SfxSystem.OneShot("event:/sfx/ui/menu_click");
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
            SfxSystem.OneShot("event:/sfx/ui/menu_click_sub");
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
            modView.OpenModView(lobbyList[index], false);
            memberView.OpenMemberView(lobbyList[index], false, false);
            joinLobbyButton.interactable = true;
        }

        private async void CreateLobby()
        {
            SfxSystem.OneShot("event:/sfx/ui/menu_click");
            int num = 4;
            Debug.Log("[Multiplayer] Lobby creation request");
            lobby = await SteamMatchmaking.CreateLobbyAsync(num);
            if (lobby is Lobby lob)
            {
                lob.SetData("name", $"{HandlerSystem.self.Name}");
                lob.SetData("mods", ModView.ActiveModListAsString());
                lob.SetData("id", $"{HandlerSystem.self.Id}");
                lob.SetData("maxplayers", num.ToString());
                lob.SetData("players", "1");
                lob.SetPublic();
                modView.OpenModView(lob, true);
                memberView.OpenMemberView(lob, true, true);
                MultiplayerMain.instance.HookToChatRoom();
                leaveLobbyButton.interactable = true;
                finalizeButton.interactable = true;
                findLobbyButton.interactable = false;
                Debug.Log("[Multiplayer] You have created a lobby.");
            }
        }

        internal async void FindLobby()
        {
            SfxSystem.OneShot("event:/sfx/ui/menu_click");
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
            SfxSystem.OneShot("event:/sfx/ui/menu_click");
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
                modView.OpenModView((Lobby)lobby, false);
                memberView.OpenMemberView((Lobby)lobby, true, false);
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
            SfxSystem.OneShot("event:/sfx/ui/menu_click");
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

        public void UpdateMemberView()
        {
            if (lobby is Lobby lob)
            {
                memberView.OpenMemberView(lob, true, MultiplayerMain.isHost);
            }
            
        }

        public void UpdateModView()
        {
            if (lobby is Lobby lob)
            {
                modView.OpenModView(lob, MultiplayerMain.isHost);
            }
        }

        public void UpdateModList()
        {
            if (lobby is Lobby lob)
            {
                (lob).SetData("mods", ModView.ActiveModListAsString());
            }
            UpdateModView();
        }
    }
}
