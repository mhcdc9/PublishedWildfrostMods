using MultiplayerBase.UI;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Pseudo;
using UnityEngine.SceneManagement;



/* The HandlerSystem class deals with sending/receiving of messages.
 * For modders
 * - Don't forget to add your handler to the handlerRoutines dictionary in your Load() of your main mod class.
 * - Messages will not send/receive until you have finalized the party.
 * - Use SendMessage and variants to send messages
 * 
 * Useful methods
 * - SendMessage
 * - SendMessageToAll
 * - SendMessageToAllOthers
 * - SendMessageToRandom
 * 
 * Notes
 * - SendMessage is done through safe channels. So, a sequence of messages will be received at most once and in the right order.
 */
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
                HandlerRoutines.Add("MSC", MSC_Handler);
                GameObject gameObject = new GameObject("InspectHandler");
                gameObject.AddComponent<HandlerInspect>();
                gameObject = new GameObject("ChatHandler");
                gameObject.AddComponent<HandlerChat>();
                gameObject = new GameObject("BattleHandler");
                gameObject.AddComponent<HandlerBattle>();
                gameObject = new GameObject("EventHandler");
                gameObject.AddComponent<HandlerEvent>();
                gameObject = new GameObject("DeckHandler");
                gameObject.AddComponent<HandlerDeck>(); //The deck handler does not need its own object.
                gameObject = new GameObject("MapHandler");
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
                HandlerChat.instance.enabled = true;
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
            MultEvents.InvokeHandlerSystemEnabled();
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
            HandlerChat.instance.enabled = false;
            Debug.Log("[Multiplayer] Handler System is disabled.");
            enabled = false;
            MultEvents.InvokeHandlerSystemDisabled();
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
                        //Debug.Log($"[Multiplayer] {s}");
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

        /*public static void CHT_Handler(Friend friend, string message)
        {
            MultiplayerMain.textElement.text = message;
        }*/

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

            //Prematurely end battle viewer
            if (scene != "Battle" && HandlerBattle.instance.Blocking && HandlerBattle.friend is Friend f && f.Id == friend.Id)
            {
                if (!HandlerBattle.instance.ignoreFurtherMessages)
                {
                    HandlerBattle.instance.CloseBattleViewer();
                    if (!HandlerBattle.instance.ignoreFurtherMessages)
                    {
                        MultTextManager.AddEntry($"Battle ended. Please leave the viewer.", 0.6f, UnityEngine.Color.yellow, 3f);
                    }
                }
            }
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
