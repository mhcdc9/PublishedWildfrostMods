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
    /*The MatchmakingDashboard is the bulk of the UI for setting up and joining lobbies.
     */
    //Canvas/SafeArea/Menu/Back Button/
    public class MatchmakingDashboard : MonoBehaviour
    {
        public static Lobby? lobby;
        public static Lobby[] lobbyList;
        public static MatchmakingDashboard instance;

        internal GameObject background;
        internal GameObject buttonGroup;
        internal Button createLobbyButton;
        internal Button findLobbyButton;
        internal Button joinLobbyButton;
        internal Button leaveLobbyButton;
        internal Button finalizeButton;
        internal Button unfinalizeButton;

        internal Button backButton;

        internal MemberView memberView;
        internal ModView modView;
        internal LobbyView lobbyView;

        internal Button[] lobbyButtons = new Button[0];

        internal TweenUI exitTween;
        public void CreateObjects()
        {
            instance = this;
            transform.SetAsFirstSibling();
            gameObject.AddComponent<UINavigationLayer>();
            /*
            WorldSpaceCanvasSafeArea can = gameObject.AddComponent<WorldSpaceCanvasSafeArea>();
            can.parent = transform.parent as RectTransform; //NOT A RECT-TRANSFORM <sigh>
            */

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

            //Main buttons
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

            exitTween = buttonGroup.AddComponent<TweenUI>();
            exitTween.target = buttonGroup;
            exitTween.property = TweenUI.Property.Move;
            exitTween.ease = LeanTweenType.easeOutQuart;
            exitTween.duration = 0.5f;
            exitTween.to = new Vector3(0, -8f, 0);

            GameObject backButtonObj = GameObject.Instantiate(GameObject.Find("Canvas/SafeArea/Menu/Back Button"), transform);
            backButton = backButtonObj.GetComponentInChildren<Button>();
            backButtonObj.transform.localPosition = new Vector3(-12.5f, 0, 0);
            backButton.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
            backButton.onClick.AddListener(MultiplayerMain.instance.CloseMatchmaking);

            createLobbyButton = HelperUI.BetterButtonTemplate(buttonGroup.transform,new Vector2(2,0.8f), new Vector3(-1.4f, 1, 0), "Create", HelperUI.restingColor);
            createLobbyButton.EditButtonAnimator(Color.white, Color.black, HelperUI.restingColor, Color.black);
            findLobbyButton = HelperUI.BetterButtonTemplate(buttonGroup.transform, new Vector2(2, 0.8f), new Vector3(-1.4f, 0, 0), "Refresh", HelperUI.restingColor);
            findLobbyButton.EditButtonAnimator(Color.white, Color.black, HelperUI.restingColor, Color.black);
            joinLobbyButton = HelperUI.BetterButtonTemplate(buttonGroup.transform, new Vector2(2, 0.8f), new Vector3(1.4f, 0, 0), "Join", HelperUI.restingColor);
            joinLobbyButton.EditButtonAnimator(Color.white, Color.black, HelperUI.restingColor, Color.black);
            leaveLobbyButton = HelperUI.BetterButtonTemplate(buttonGroup.transform, new Vector2(2, 0.8f), new Vector3(1.4f, 1, 0), "Leave", HelperUI.restingColor);
            leaveLobbyButton.EditButtonAnimator(Color.white, Color.black, HelperUI.restingColor, Color.black);
            finalizeButton = HelperUI.BetterButtonTemplate(buttonGroup.transform, new Vector2(3, 0.8f), new Vector3(0f, -1, 0), "Finalize", HelperUI.restingColor);
            finalizeButton.EditButtonAnimator(Color.white, Color.black, HelperUI.restingColor, Color.black);
            unfinalizeButton = HelperUI.BetterButtonTemplate(buttonGroup.transform, new Vector2(3, 0.8f), new Vector3(0f, -1, 0), "Disband", new Color(1f,0.33f,0.33f));
            unfinalizeButton.EditButtonAnimator(Color.white, Color.red, new Color(1f, 0.33f, 0.33f), Color.black);
            unfinalizeButton.gameObject.SetActive(false);

            ButtonToggle(true, true, false, false, true); //!!!

            createLobbyButton.onClick.AddListener(CreateLobby);
            findLobbyButton.onClick.AddListener(FindLobby);
            joinLobbyButton.onClick.AddListener(JoinLobby);
            leaveLobbyButton.onClick.AddListener(LeaveLobby);
            finalizeButton.onClick.AddListener(FinalizeParty);
            unfinalizeButton.onClick.AddListener(Disband);

            lobbyView = LobbyView.Create(transform);
            memberView = MemberView.Create(transform);
            modView = ModView.Create(transform);
        }

        public void OnEnable()
        {
            if (backButton != null)
            {
                GameObject obj = backButton.transform.parent.parent.gameObject;
                LeanTween.moveLocal(obj, new Vector3(-9.5f, 0, 0), 0.5f).setEaseOutQuart(); ;
            }
        }

        public void DisbandMenu()
        {
            lobbyView.gameObject.SetActive(false);
            ButtonToggle(false, false, false, false, false);
            finalizeButton.gameObject.SetActive(false);
            unfinalizeButton.gameObject.SetActive(true);
        }

        public void CreateLobbyView(Lobby[] lobbies)
        {
            lobbyView.CreateLobbyView(lobbies);
        }

        //Leaves the lobby and you can get on with the game.
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

        public void Disband()
        {
            Dashboard.instance.enabled = false;
            Debug.Log("[Multiplayer] Successfully disbanded!");
            MultiplayerMain.isHost = true;
            finalizeButton.gameObject.SetActive(true);
            unfinalizeButton.gameObject.SetActive(false);
            ButtonToggle(true, true, false, false, true); //!!!
            RemoveSidePanels();
            FindLobby();
        }

        //Closes your current lobby
        public IEnumerator EndLobby()
        {
            yield return new WaitForSeconds(3.0f);
            if (lobby is Lobby lob)
            {
                lob.Leave();
                ButtonToggle(false, false, false, false, false);
                lobby = null;
            }
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
                lobbyView.ExitLobbyView();
                MultiplayerMain.instance.HookToChatRoom();
                ButtonToggle(false, false, false, true, true);
                Debug.Log("[Multiplayer] You have created a lobby.");
            }
        }

        internal async void FindLobby()
        {
            SfxSystem.OneShot("event:/sfx/ui/menu_click");
            Debug.Log("[Multiplayer] Find lobby request");
            Lobby[] lobbies = await SteamMatchmaking.LobbyList.RequestAsync();
            Debug.Log("[Multiplayer] Found lobbies");
            if (lobbies == null)
            {
                lobbies = new Lobby[0];
            }
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
            if (lobbyView.index == -1 || lobby!= null)
            {
                throw new Exception("Pick something first! Or leave your current lobby.");
            }
            RoomEnter enter = await lobbyList[lobbyView.index].Join();
            if (enter == RoomEnter.Success)
            {
                Debug.Log("[Multiplayer] Successfully joined");
                lobby= lobbyList[lobbyView.index];
                MultiplayerMain.isHost = false;
                MultiplayerMain.instance.HookToChatRoom();
                modView.OpenModView((Lobby)lobby, false);
                memberView.OpenMemberView((Lobby)lobby, true, false);
                ButtonToggle(false, false, false, true, false);
                //lobbyView.SelectLobby(-1);
                lobbyView.ExitLobbyView();
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
                ButtonToggle(true, true, false, false, false);
                RemoveSidePanels();
                FindLobby();
                lobby = null;
            }
        }

        public void RemoveSidePanels()
        {
            memberView.CloseMemberView();
            modView.CloseModView();
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

        public void ButtonToggle(bool create, bool find, bool join, bool leave, bool finalize)
        {
            if (create) { ButtonOn(createLobbyButton); } else { ButtonOff(createLobbyButton); }
            if (find) { ButtonOn(findLobbyButton); } else { ButtonOff(findLobbyButton); }
            if (join) { ButtonOn(joinLobbyButton); } else { ButtonOff(joinLobbyButton); }
            if (leave) { ButtonOn(leaveLobbyButton); } else { ButtonOff(leaveLobbyButton); }
            if (finalize) { ButtonOn(finalizeButton); } else { ButtonOff(finalizeButton); }
        }

        public void ButtonOn(Button button)
        {
            button.EnableAllParts();
            button.transform.parent.GetComponent<ButtonAnimator>().UnHighlight();
        }

        public void ButtonOff(Button button)
        {
            button.DisableAllParts();
            button.transform.parent.GetComponent<ButtonAnimator>().Disable();
        }
    }
}
