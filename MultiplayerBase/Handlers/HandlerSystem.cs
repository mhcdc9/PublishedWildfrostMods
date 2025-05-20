using Steamworks.Data;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MultiplayerBase.UI;
using UnityEngine.SceneManagement;
using TMPro;
using static UnityEngine.Rendering.DebugUI;

//Battle/Canvas


namespace MultiplayerBase.Handlers
{
    public enum PlayerState
    {
        MainMenu,
        Map,
        Event,
        Battle,
        Other = 0
    }
    public static class HandlerSystem
    {
        public static float refreshRate = 0.1f;
        public static Friend self;
        public static Friend[] friends;
        public static Dictionary<Friend, PlayerState> friendStates = new Dictionary<Friend, PlayerState>();

        public static bool initialized = false;
        public static bool enabled;

        public static Dictionary<string, Action<Friend, string>> HandlerRoutines = new Dictionary<string, Action<Friend, string>>();

        public static Character playerDummy;
        public static Character enemyDummy;

        #region INIT
        public static void Enable()
        {
            if (enabled) return;


            if (!initialized)
            {
                playerDummy = AddressableLoader.Get<ClassData>("ClassData", "Basic").characterPrefab.InstantiateKeepName();
                playerDummy.name = "Fake Player";
                enemyDummy = playerDummy.InstantiateKeepName();
                enemyDummy.name = "Fake Enemy";
                enemyDummy.team = 2;

                SteamNetworking.OnP2PSessionRequest += SessionRequest;
                HandlerRoutines.Add("CHT", CHT_Handler);
                HandlerRoutines.Add("MSC", MSC_Handler);
                GameObject gameObject = new GameObject("Inspect Handler");
                gameObject.AddComponent<HandlerInspect>();
                gameObject = new GameObject("Battle Handler");
                gameObject.AddComponent<HandlerBattle>();
                gameObject = new GameObject("Event Handler");
                gameObject.AddComponent<HandlerEvent>();
                gameObject = new GameObject("Map Handler");
                gameObject.AddComponent<HandlerMap>();
                Events.OnSceneChanged += SceneChanged;
                

                initialized = true;
            }
            else
            {
                SteamNetworking.OnP2PSessionRequest += SessionRequest;
                HandlerInspect.instance.enabled = true;
                HandlerBattle.instance.enabled = true;
                HandlerEvent.instance.enabled = true;
                HandlerMap.instance.enabled = true;
                Events.OnSceneChanged += SceneChanged;
            }

            References.instance.StartCoroutine(ListenLoop());

            friendStates = new Dictionary<Friend, PlayerState>();
            foreach(Friend friend in friends)
            {
                friendStates[friend] = PlayerState.Other;
            }
            SceneChanged(SceneManager.GetActive());

            enabled = true;
        }

        public static void Disable()
        {
            if (!enabled) return;

            SteamNetworking.OnP2PSessionRequest -= SessionRequest;
            Events.OnSceneChanged -= SceneChanged;
            References.instance.StopCoroutine(ListenLoop());
            HandlerInspect.instance.enabled = false;
            HandlerBattle.instance.enabled = false;
            HandlerEvent.instance.enabled = false;
            HandlerMap.instance.enabled = false;
            Debug.Log("[Multiplayer] Handler System is disabled.");
            enabled = false;
        }

        private static IEnumerator ListenLoop()
        {
            while (true)
            {
                TryReadMessage();
                yield return null;//new WaitForSeconds(refreshRate);
            }
        }

        private static void SessionRequest(SteamId id)
        {
            foreach (Friend friend in HandlerSystem.friends)
            {
                if (friend.Id == id)
                {
                    if (SteamNetworking.AcceptP2PSessionWithUser(id))
                    {
                        Debug.Log("[Multiplayer] Accepted Session");
                    }
                }
            }
        }
        #endregion INIT

        #region MESSAGES
        public static void SendMessage(string handler, Friend to, string message, string feedback = null)
        {
            if (!enabled) return;

            string s = $"{handler}|{self.Id.Value}|{message}";
            SteamNetworking.SendP2PPacket(to.Id, Encoding.UTF8.GetBytes(s));
            if (feedback != null)
            {
                MultTextManager.VisFeedback(to, feedback);
            }
        }

        public static void SendMessageToAll(string handler, string message, bool includeSelf = true, string feedback = null)
        {
            if (!enabled) return;

            string s = $"{handler}|{self.Id.Value}|{message}";
            foreach (Friend friend in friends)
            {
                if ((friend.Id.Value != self.Id.Value) || includeSelf)
                {
                    SteamNetworking.SendP2PPacket(friend.Id, Encoding.UTF8.GetBytes(s));
                    if (feedback != null)
                    {
                        MultTextManager.VisFeedback(friend, feedback);
                    }
                }
            }
        }

        public static void SendMessageToAllOthers(string handler, string message, string feedback = null)
        {
            SendMessageToAll(handler, message, false, feedback);
            /*
            if (!enabled) return;

            string s = $"{handler}|{self.Id.Value}|{message}";
            foreach (Friend friend in friends)
            {
                if (friend.Id.Value != self.Id.Value)
                {
                    SteamNetworking.SendP2PPacket(friend.Id, Encoding.UTF8.GetBytes(s));
                }
            }
            */
        }

        public static void SendMessageToRandom(string handler, string message, bool includeSelf = true, string feedback = null)
        {
            if (!enabled) return;

            Friend[] choices = friends.Where(f => (includeSelf || f.Id == self.Id)).ToArray();
            if (choices.Length == 0) return;

            SendMessage(handler, choices.RandomItem(), message, feedback);
        }

        public static string ConcatMessage(bool performReplacement, params string[] messages)
        {
            if (performReplacement)
            {
                messages = messages.Select((s) => s.Replace("!", "!:")).ToArray();
            }
            
            return string.Join("! ", messages);
        }

        public static string AppendTo(string original, string addOn, bool performReplacement = true)
        {
            if (performReplacement)
            {
                addOn = addOn.Replace("!", "!:");
            }
            return original + "! " + addOn;
        }

        public static string[] DecodeMessages(string message)
        {
            string[] messages = message.Split(new string[] { "! " },StringSplitOptions.None);
            /*foreach(string s in messages)
            {
                Debug.Log(s);
            }*/
            return messages.Select((s) => s.Replace("!:", "!")).ToArray();
        }

        public static bool TryReadMessage()
        {
            Steamworks.Data.P2Packet? packet = SteamNetworking.ReadP2PPacket();
            if (packet is P2Packet p)
            {
                string s = Encoding.UTF8.GetString(p.Data);
                string[] strings = s.Split(new char[] { '|' });
                if (strings.Length < 3)
                {
                    Debug.Log($"[Multiplayer] Invalid message: {s}");
                    return true;
                }
                if (HandlerRoutines.ContainsKey(strings[0]))
                {
                    Debug.Log($"[Multiplayer] Running handler [{strings[0]}]");
                    if (FindFriend(strings[1]) is Friend friend)
                    {
                        s = string.Concat(strings.RangeSubset(2, strings.Length - 2));
                        //Debug.Log($"[Multiplayer] Message [{s}]");
                        HandlerRoutines[strings[0]](friend,s);
                    }
                    else
                    {
                        Debug.Log($"[Multiplayer] Unknown friend: {strings[1]}");
                        Debug.Log($"[Multiplayer] Message [{s}]");
                        return true;
                    }
                }
                else
                {
                    Debug.Log($"[Multiplayer] Unknown handler \"{strings[0]}\"");
                    Debug.Log($"[Multiplayer] Message [{s}]");
                    return true;
                }
            }
            return false;
        }
        #endregion MESSAGES

        #region MISC
        public static Friend? FindFriend(string id)
        {
            foreach(Friend friend in friends)
            {
                if (friend.Id.Value.ToString() == id)
                {
                    return friend;
                }
            }
            return null;
        }

        public static void CHT_Handler(Friend friend, string message)
        {
            MultiplayerMain.textElement.text = message;
        }

        public static void MSC_Handler(Friend friend, string message)
        {
            string[] messages = HandlerSystem.DecodeMessages(message); ;
            switch(messages[0])
            {
                case "SCENE":
                    FriendSceneChanged(friend, messages[1], messages[2]);
                    break;
                case "NICKNAME":
                    Dashboard.friendIcons[friend].SetNickname(messages[1]);
                    break;
                default:
                    break;
            }
        }

        private static void FriendSceneChanged(Friend friend, string scene, string extra)
        {
            Debug.Log($"[Multiplayer] {scene}");
            Dashboard.friendIcons[friend].SceneChanged(scene, extra);
            switch (scene)
            {
                case "Battle":
                    friendStates[friend] = PlayerState.Battle;
                    Dashboard.friendIcons[friend].GetComponentInChildren<TextMeshProUGUI>(true).text = "42";
                    if (friend.Id == self.Id)
                    {
                        References.instance.StartCoroutine(HandlerBattle.instance.BattleRoutine());
                    }
                    break;
                case "MapNew":
                    Dashboard.friendIcons[friend].GetComponentInChildren<TextMeshProUGUI>(true).text = "92";
                    friendStates[friend] = PlayerState.Map;
                    break;
                case "Event":
                    Dashboard.friendIcons[friend].GetComponentInChildren<TextMeshProUGUI>(true).text = "25";
                    friendStates[friend] = PlayerState.Event;
                    break;
                case "MainMenu":
                case "Town":
                    Dashboard.friendIcons[friend].GetComponentInChildren<TextMeshProUGUI>(true).text = "00";
                    friendStates[friend] = PlayerState.MainMenu;
                    break;
                default:
                    Dashboard.friendIcons[friend].GetComponentInChildren<TextMeshProUGUI>(true).text = "99";
                    friendStates[friend] = PlayerState.Other;
                    break;
            }
        }

        private static void SceneChanged(Scene scene)
        {
            MultiplayerMain.textElement.text = "";
            SceneChanged(scene.name);
        }

        public static void SceneChanged(string sceneName)
        {
            string s2 = "";
            if (sceneName == "Battle")
            {
                object value;
                if (Campaign.FindCharacterNode(References.Player).data.TryGetValue("battle", out value) && value is string assetName)
                {
                    BattleData data = AddressableLoader.Get<BattleData>("BattleData", assetName);
                    s2 = "???";
                    if (data.nameRef != null)
                    {
                        s2 = data.nameRef.IsEmpty ? "???" : data.nameRef.GetLocalizedString();
                    }
                }
                else
                {
                    s2 = "???";
                }
            }
            //Dashboard.friendIcons[self].SceneChanged(sceneName,s2);
            SendMessageToAllOthers("MSC", HandlerSystem.ConcatMessage(true, "SCENE", sceneName, s2));
            FriendSceneChanged(self, sceneName, s2);
        }
        #endregion MISC
    }
}
