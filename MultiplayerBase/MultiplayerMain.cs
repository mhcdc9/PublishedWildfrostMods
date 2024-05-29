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

namespace MultiplayerBase
{
    public class MultiplayerMain : WildfrostMod
    {

        public static bool isHost = false;
        public static Friend self;
        public static Friend[] friends;
        public static MultiplayerMain instance;

        internal static Matchmaking matchmaker;
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

        public override string Description => "This mod provides barebones matchmaking and helpful functions.";

        protected override void Load()
        {
            base.Load();
            GameObject gameobject = new GameObject("Matchmaker");
            gameobject.transform.SetParent(GameObject.Find("Canvas/SafeArea").transform);
            Debug.Log("[Multiplayer] 1");
            matchmaker = gameobject.AddComponent<Matchmaking>();
            Debug.Log("[Multiplayer] 2");
            matchmaker.CreateObjects();
            Debug.Log("[Multiplayer] 3");
            Debug.Log("[Multiplayer] 4");
            gameobject.SetActive(false);

            self = new Friend(SteamClient.SteamId);

            gameobject = new GameObject("Start Button");
            UnityEngine.Object.DontDestroyOnLoad(gameobject);
            Image image = gameobject.AddComponent<Image>();
            image.color = new UnityEngine.Color(Dead.PettyRandom.Range(0f, 1f), Dead.PettyRandom.Range(0f, 1f), Dead.PettyRandom.Range(0f, 1f));
            openMatchmaking = gameobject.AddComponent<Button>();
            openMatchmaking.onClick.AddListener(ToggleMatchmaking);
            gameobject.GetComponent<RectTransform>().sizeDelta = new Vector2(1, 1);
            gameobject.transform.SetParent(GameObject.Find("Canvas/SafeArea/Buttons").transform);
            gameobject.transform.localPosition = new Vector3(-11, 5, 0);

            gameobject = new GameObject("Start Button");
            UnityEngine.Object.DontDestroyOnLoad(gameobject);
            textElement = gameobject.AddComponent<TextMeshProUGUI>();
            textElement.horizontalAlignment = HorizontalAlignmentOptions.Center;
            textElement.verticalAlignment = VerticalAlignmentOptions.Middle;
            textElement.fontSize = 0.4f;
            textElement.color = UnityEngine.Color.white;
            gameobject.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 1);
            gameobject.transform.SetParent(GameObject.Find("Canvas/SafeArea").transform);
            gameobject.transform.localPosition = new Vector3(0, 5, 0);
        }

        protected override void Unload()
        {
            base.Unload();
            matchmaker.gameObject.Destroy();
            openMatchmaking.gameObject.Destroy();
            textElement.Destroy();
        }

        internal void HookToChatRoom()
        {
            Events.OnSceneChanged += AnnounceSceneToOthers;
            SteamMatchmaking.OnLobbyCreated += SendMessageCreate;
            SteamMatchmaking.OnLobbyEntered += SendMessageEnter;
            SteamMatchmaking.OnChatMessage += DisplayMessage;
        }

        internal void UnhookToChatRoom()
        {
            Events.OnSceneChanged -= AnnounceSceneToOthers;
            SteamMatchmaking.OnLobbyCreated -= SendMessageCreate;
            SteamMatchmaking.OnLobbyEntered -= SendMessageEnter;
            SteamMatchmaking.OnChatMessage -= DisplayMessage;
        }

        

        private void SendMessageCreate(Result result, Lobby lobby) => SendMessage("Created lobby");
        private void SendMessageEnter(Lobby lobby) => SendMessage("Joined lobby");

        private void DisplayMessage(Lobby lobby, Friend friend, String message)
        {
            if(message == "AndSoThePartyIsFinallyFinalized")
            {
                friends = lobby.Members.ToArray();
                References.instance.StartCoroutine(matchmaker.EndLobby());
                GameObject gameObject = new GameObject("Multiplayer Dashboard");
                dashboard = gameObject.AddComponent<Dashboard>();
                dashboard.Init();
                ToggleMatchmaking();
                Debug.Log("[Multiplayer] Finalized.");
                return;
            }
            textElement.text = $"{friend.Name}: {message}";
        }

        public static void SendMessage(string message)
        {
            if (Matchmaking.lobby is Lobby lob)
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
        }
    }
}
