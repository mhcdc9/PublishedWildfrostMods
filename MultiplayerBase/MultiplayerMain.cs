﻿using Deadpan.Enums.Engine.Components.Modding;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
using Color = UnityEngine.Color;
using HarmonyLib;
using MultiplayerBase.Handlers;
using MultiplayerBase.UI;
using MultiplayerBase.Matchmaking;
using UnityEngine.Events;
using System.Collections;
using MultiplayerBase.ConsoleCommands;
using WildfrostHopeMod;
using WildfrostHopeMod.Configs;

namespace MultiplayerBase
{
    public class MultiplayerMain : WildfrostMod
    {
        public static UnityAction Finalized;

        public static bool isHost = true;
        public static MultiplayerMain instance;

        internal static MatchmakingDashboard matchmaker;
        internal static Button openMatchmaking;
        internal static TextMeshProUGUI textElement;

        internal static Dashboard dashboard;
        public MultiplayerMain(string modDirectory) : base(modDirectory)
        {
            instance = this;
        }

        public override string GUID => "mhcdc9.wildfrost.multiplayer";

        public override string[] Depends => new string[] { "hope.wildfrost.configs"};

        public override string Title => "Multiplayer Base Mod v0.3.0";

        public override string Description => "[Work In Progress] A foundation for multiplayer mods to build on top of.";

        [ConfigManagerTitle("Friend Icon Sizes")]
        [ConfigManagerDesc("Determines the size of the friend icons")]
        [ConfigOptions("Baby Snowbo", "Snowbo", "Snow Knight", "Winter Wyrm", "Bamboozle")]
        [ConfigItem("Snow Knight", "", "friendIconSize")]
        public string iconSize = "Snow Knight";

        [ConfigManagerTitle("Chat Hotkey")]
        [ConfigManagerDesc("Determines which button opens the chat window")]
        [ConfigInput()]
        [ConfigItem("C", "", "chatHotkey")]
        public string chatKey = "C";

        public string _chatKey = "C";
        public KeyCode _chatKeyCode = KeyCode.C;

        public float _iconSize = 1f;

        public void CreateModAssets()
        {
            AddressableLoader.AddToGroup("KeywordData",
            new KeywordDataBuilder(this).Create("friend")
                .WithTitle("Party Memeber")
                .WithDescription("How did you find this")
                .Build()
                );
        }

        public override void Load()
        {
            CreateModAssets();
            FindAnotherConsoleMod();
            
            base.Load();
            Events.OnModLoaded += CheckAnotherConsoleMod;
            ConfigManager.GetConfigSection(this).OnConfigChanged += ConfigChanged;

            GameObject gameobject = new GameObject("Matchmaker");
            gameobject.transform.SetParent(GameObject.Find("Canvas/SafeArea").transform);
            gameobject.SetActive(false);
            matchmaker = gameobject.AddComponent<MatchmakingDashboard>();
            matchmaker.CreateObjects();

            HandlerSystem.self = new Friend(SteamClient.SteamId);
            Task<Steamworks.Data.Image?> imageTask = HandlerSystem.self.GetSmallAvatarAsync();

            gameobject = HelperUI.BetterButtonTemplate(GameObject.Find("Canvas/SafeArea/Buttons").transform, 0.6f*Vector2.one, Vector3.zero, "", Color.white).gameObject;
            Image image = gameobject.GetComponent<Image>();
            image.sprite = ImagePath("ui_lobby.png").ToSprite();
            openMatchmaking = gameobject.GetComponent<Button>();
            openMatchmaking.onClick.AddListener(ToggleMatchmaking);

            gameobject = new GameObject("Top Text");
            UnityEngine.Object.DontDestroyOnLoad(gameobject);
            textElement = gameobject.AddComponent<TextMeshProUGUI>();
            gameobject.AddComponent<MultTextManager>();

            Get<GameMode>("GameModeNormal").saveFileName = "Multiplayer";

            SetChatKeycode();
            SetIconSize();
        }

        public async void ChangeColor(Image image)
        {
            Steamworks.Data.Image? i = await HandlerSystem.self.GetMediumAvatarAsync();
            if (i is Steamworks.Data.Image steamImage)
            {
                int x = Dead.PettyRandom.Range(0, (int) steamImage.Width-1);
                int y = Dead.PettyRandom.Range(0, (int) steamImage.Height - 1);
                image.color = FriendIcon.GetColor(steamImage.GetPixel(x,y));
            }  
        }

        public override void Unload()
        {
            base.Unload();
            UnhookToChatRoom();
            Events.OnModLoaded -= CheckAnotherConsoleMod;
            ConfigManager.GetConfigSection(this).OnConfigChanged -= ConfigChanged;
            matchmaker.gameObject.Destroy();
            openMatchmaking.transform.parent.gameObject.Destroy();
            textElement.Destroy();
            if (HandlerSystem.enabled)
            {
                HandlerSystem.Disable();
            }

            Get<GameMode>("GameModeNormal").saveFileName = "";
        }

        #region CONFIG
        private void ConfigChanged(ConfigItem item, object value)
        {
            if (item.fieldName == "iconSize")
            {
                SetIconSize();
            }
            else if (item.fieldName == "chatKey")
            {
                SetChatKeycode();
            }
        }

        private void SetIconSize()
        {
            switch(iconSize)
            {
                case "Baby Snowbo":
                    _iconSize = 0.64f;
                    break;
                case "Snowbo":
                    _iconSize = 0.8f;
                    break;
                case "Snow Knight":
                default:
                    _iconSize = 1f;
                    break;
                case "Winter Wyrm":
                    _iconSize = 1.2f;
                    break;
                case "Bamboozle":
                    _iconSize = 1.44f;
                    break;

            }

            Dashboard.instance?.ResizeIcons();
        }

        private void SetChatKeycode()
        {
            if (System.Enum.TryParse(chatKey, true, out KeyCode result))
            {
                _chatKeyCode = result;
                _chatKey = chatKey;
                Debug.Log("[Multiplayer] Chat key succesfully changed");
            }
            else
            {
                chatKey = _chatKey;
                //item.button.transform.FindRecursive("Label")?.GetComponentInChildren<TextMeshProUGUI>()?.SetText(_chatKey);
            }
        }

        #endregion CONFIG

        private bool hookedToChatRooms;
        internal void HookToChatRoom()
        {
            if (hookedToChatRooms) { return; }
            hookedToChatRooms = true;
            //Events.OnSceneChanged += AnnounceSceneToOthers;
            SteamMatchmaking.OnLobbyCreated += SendMessageCreate;
            SteamMatchmaking.OnLobbyEntered += SendMessageEnter;
            SteamMatchmaking.OnChatMessage += DisplayMessage;
            Events.OnModLoaded += ModChanged;
            Events.OnModUnloaded += ModChanged;
        }

        internal void UnhookToChatRoom()
        {
            if (!hookedToChatRooms) { return; }
            hookedToChatRooms = false;
            //Events.OnSceneChanged -= AnnounceSceneToOthers;
            SteamMatchmaking.OnLobbyCreated -= SendMessageCreate;
            SteamMatchmaking.OnLobbyEntered -= SendMessageEnter;
            SteamMatchmaking.OnChatMessage -= DisplayMessage;
            Events.OnModLoaded -= ModChanged;
            Events.OnModUnloaded -= ModChanged;
        }

        private void ModChanged(WildfrostMod mod)
        {
            if (isHost)
            {
                matchmaker.UpdateModList();
            }
        }

        private void CheckAnotherConsoleMod(WildfrostMod mod)
        {
            if (mod.GUID == "hope.wildfrost.console")
            {
                CoroutineManager.Start(Commands.AddCustomCommands(mod));
            }
        }

        private void FindAnotherConsoleMod()
        {
            List<WildfrostMod> mods = Bootstrap.Mods.ToList();
            foreach(WildfrostMod mod in mods)
            {
                if (mod.GUID == "hope.wildfrost.console" && mod.HasLoaded)
                {
                    CoroutineManager.Start(Commands.AddCustomCommands(mod));
                }
            }
        }

        private void SendMessageCreate(Result result, Lobby lobby) => SendMessage("Created lobby");
        private void SendMessageEnter(Lobby lobby) => SendMessage("Joined lobby");

        private void DisplayMessage(Lobby lobby, Friend friend, String message)
        {
            if(message == "Joined lobby")
            {
                if (isHost)
                {
                    lobby.SetData("players", lobby.MemberCount.ToString());
                }
                matchmaker.UpdateMemberView();
            }
            if(message == "AndSoThePartyIsFinallyFinalized")
            {
                FinalizeParty(lobby.Members.ToArray());
                return;
            }
            MultTextManager.AddEntry($"{friend.Name}: {message}", 0.4f, Color.white, 5f);
        }

        public void FinalizeParty(Friend[] friends)
        {
            HandlerSystem.friends = friends;
            References.instance.StartCoroutine(matchmaker.EndLobby());
            if (Dashboard.instance != null)
            {
                Dashboard.instance.enabled = true;
            }
            else
            {
                GameObject gameObject = new GameObject("Multiplayer Dashboard");
                dashboard = gameObject.AddComponent<Dashboard>();
            }
            Finalized?.Invoke();
            CloseMatchmaking();
            UnhookToChatRoom();
            Debug.Log("[Multiplayer] Finalized.");
        }

        public void DebugMode()
        {
            FinalizeParty(new Friend[1] { HandlerSystem.self });
        }

        public static void SendMessage(string message)
        {
            if (MatchmakingDashboard.lobby is Lobby lob)
            {
                lob.SendChatString(message);
                Debug.Log("[Multiplayer] Sent message: " + message);
            }
        }

        private void AnnounceSceneToOthers(Scene scene)
        {
            SendMessage($"{scene.name}");
        }

        private void ToggleMatchmaking()
        {
            if (!matchmaker.gameObject.activeSelf)
            {
                if (HandlerSystem.enabled)
                {
                    MatchmakingDashboard.instance.DisbandMenu();
                }
                else if (MatchmakingDashboard.lobby == null)
                {
                    matchmaker.FindLobby();
                }
                matchmaker.gameObject.SetActive(true);
            }
            else
            {
                CloseMatchmaking();
            }
        }

        internal void CloseMatchmaking()
        {
            matchmaker.exitTween.Fire();
            matchmaker.lobbyView.ExitLobbyView(false);
            matchmaker.memberView.CloseMemberView(false);
            matchmaker.modView.CloseModView(false);
            GameObject obj = matchmaker.backButton.transform.parent.parent.gameObject;
            LeanTween.moveLocal(obj, new Vector3(-12.5f, 0, 0), 0.5f).setEaseOutQuart();
            matchmaker.background.GetComponent<Fader>().Out(0.4f);
            References.instance.StartCoroutine(Close(0.45f));
        }

        private bool closing = false;
        private IEnumerator Close(float dur)
        {
            if (closing)
            {
                yield break;
            }
            closing = true;
            yield return new WaitForSeconds(dur);
            matchmaker.gameObject.SetActive(false);
            closing = false;
        }


    }

    [HarmonyPatch(typeof(ScriptBattleSetUp), "CreatePlayerCards", new Type[3]
    {
        typeof(Character),
        typeof(CardController),
        typeof(IList<Entity>),
    })]
    internal static class PatchBattleScript1
    {
        internal static CardControllerBattle battleController;
        internal static void Prefix(Character player, ref CardController cardController, IList<Entity> entities)
        {
            if (battleController == null)
            {
                foreach (CardControllerBattle cc in GameObject.FindObjectsOfType<CardControllerBattle>())
                {
                    if (cc.name == "CardController")
                    {
                        battleController = cc;
                        break;
                    }
                }
            }
            cardController = battleController;
        }
    }

    [HarmonyPatch(typeof(ScriptBattleSetUp), "CreateEnemyCards", new Type[3]
    {
        typeof(Character),
        typeof(CardController),
        typeof(IList<Entity>),
    })]
    internal static class PatchBattleScript2
    {
        internal static void Prefix(Character enemy, ref CardController cardController, IList<Entity> entities)
        {
            if (PatchBattleScript1.battleController == null)
            {
                foreach(CardControllerBattle cc in GameObject.FindObjectsOfType<CardControllerBattle>())
                {
                    if (cc.name == "CardController")
                    {
                        PatchBattleScript1.battleController = cc;
                        break;
                    }
                }
            }
            cardController = PatchBattleScript1.battleController;
        }
    }

    [HarmonyPatch(typeof(Battle), "IsOnBoard", new Type[]
    {
        typeof(Entity)
    })]
    internal static class PatchEntityOnBoard
    {
        internal static bool Prefix(ref bool __result, Entity entity)
        {
            if (entity.owner == null || !Battle.instance.rows.ContainsKey(entity.owner))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(NavigationStateBattle), "Begin")]
    internal static class PatchBeginNav
    {
        static bool Prefix(NavigationStateBattle __instance)
        {
            foreach(CardContainer lane in References.Battle.rows.Values.SelectMany((List<CardContainer> a) => a).Cast<CardContainer>())
            {
                __instance.Disable(lane.nav);
                if (lane is CardSlotLane allRow)
                {  
                    foreach (CardSlot slot in allRow.slots)
                    {
                        __instance.Disable(slot.nav);
                    }
                }
            }

            if (References.Battle.playerCardController is CardControllerBattle cardControllerBattle)
            {
                __instance.Disable(cardControllerBattle.useOnHandAnchor);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(Events), nameof(Events.InvokeEntityDisplayUpdated), new Type[]
    {
        typeof(Entity)
    })]
    class PatchEntityDispUpdated
    {
        static bool Prefix(Entity entity)
        {
            if (HandlerBattle.instance != null && HandlerBattle.instance.Blocking)
            {
                return false;
            }
            return true;
        }
    }
}
