using Deadpan.Enums.Engine.Components.Modding;
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
using HarmonyLib;
using MultiplayerBase.Handlers;
using MultiplayerBase.UI;
using MultiplayerBase.Matchmaking;
using UnityEngine.Events;

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

        public override string[] Depends => new string[0];

        public override string Title => "Multiplayer Base Mod";

        public override string Description => "A foundation for multiplayer mods to build on top of.";

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
            base.Load();
            GameObject gameobject = new GameObject("Matchmaker");
            gameobject.transform.SetParent(GameObject.Find("Canvas/SafeArea").transform);
            Debug.Log("[Multiplayer] 1");
            matchmaker = gameobject.AddComponent<MatchmakingDashboard>();
            Debug.Log("[Multiplayer] 2");
            matchmaker.CreateObjects();
            Debug.Log("[Multiplayer] 3");
            Debug.Log("[Multiplayer] 4");
            gameobject.SetActive(false);

            HandlerSystem.self = new Friend(SteamClient.SteamId);
            Task<Steamworks.Data.Image?> imageTask = HandlerSystem.self.GetSmallAvatarAsync();

            gameobject = new GameObject("Start Button");
            UnityEngine.Object.DontDestroyOnLoad(gameobject);
            Image image = gameobject.AddComponent<Image>();
            //image.color = new UnityEngine.Color(Dead.PettyRandom.Range(0f, 1f), Dead.PettyRandom.Range(0f, 1f), Dead.PettyRandom.Range(0f, 1f));
            ChangeColor(image);
            openMatchmaking = gameobject.AddComponent<Button>();
            openMatchmaking.onClick.AddListener(ToggleMatchmaking);
            gameobject.GetComponent<RectTransform>().sizeDelta = new Vector2(1, 1);
            gameobject.transform.SetParent(GameObject.Find("Canvas/SafeArea/Buttons").transform);
            gameobject.transform.localPosition = new Vector3(-11, 5, 0);

            gameobject = new GameObject("Top Text");
            UnityEngine.Object.DontDestroyOnLoad(gameobject);
            textElement = gameobject.AddComponent<TextMeshProUGUI>();
            textElement.horizontalAlignment = HorizontalAlignmentOptions.Center;
            textElement.verticalAlignment = VerticalAlignmentOptions.Middle;
            textElement.fontSize = 0.4f;
            textElement.outlineWidth = 0.1f;
            textElement.color = UnityEngine.Color.white;
            gameobject.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 1);
            gameobject.transform.SetParent(GameObject.Find("Canvas/SafeArea").transform);
            gameobject.transform.localPosition = new Vector3(0, 5, 0);
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
            matchmaker.gameObject.Destroy();
            openMatchmaking.gameObject.Destroy();
            textElement.Destroy();
        }

        internal void HookToChatRoom()
        {
            //Events.OnSceneChanged += AnnounceSceneToOthers;
            SteamMatchmaking.OnLobbyCreated += SendMessageCreate;
            SteamMatchmaking.OnLobbyEntered += SendMessageEnter;
            SteamMatchmaking.OnChatMessage += DisplayMessage;
            Events.OnModLoaded += ModChanged;
            Events.OnModUnloaded += ModChanged;
        }

        internal void UnhookToChatRoom()
        {
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
                HandlerSystem.friends = lobby.Members.ToArray();
                References.instance.StartCoroutine(matchmaker.EndLobby());
                GameObject gameObject = new GameObject("Multiplayer Dashboard");
                dashboard = gameObject.AddComponent<Dashboard>();
                Finalized?.Invoke();
                ToggleMatchmaking();
                Debug.Log("[Multiplayer] Finalized.");
                return;
            }
            textElement.text = $"{friend.Name}: {message}";
        }

        public void DebugMode()
        {
            HandlerSystem.friends = new Friend[1] { HandlerSystem.self };
            GameObject gameObject = new GameObject("Multiplayer Dashboard");
            dashboard = gameObject.AddComponent<Dashboard>();
            Finalized?.Invoke();
            ToggleMatchmaking();
            Debug.Log("[Multiplayer] Finalized.");
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
            matchmaker.gameObject.SetActive(!matchmaker.gameObject.activeSelf);
            if (matchmaker.gameObject.activeSelf && MatchmakingDashboard.lobby == null)
            {
                matchmaker.FindLobby();
            }
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
}
