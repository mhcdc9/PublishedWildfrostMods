﻿using Steamworks.Data;
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

        public static Dictionary<string, Action<Friend, string>> HandlerRoutines = new Dictionary<string, Action<Friend, string>>();

        public static Character playerDummy;
        public static Character enemyDummy;
        public static void Initialize()
        {
            friendStates = new Dictionary<Friend, PlayerState>();
            foreach(Friend friend in friends)
            {
                friendStates[friend] = PlayerState.Other;
            }
            SceneChanged(SceneManager.GetActive());

            playerDummy = AddressableLoader.Get<ClassData>("ClassData", "Basic").characterPrefab.InstantiateKeepName();
            enemyDummy = playerDummy.InstantiateKeepName();
            enemyDummy.team = 2;

            if (initialized)
            {
                return;
            }

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
            References.instance.StartCoroutine(ListenLoop());

            initialized = true;
        }

        private static IEnumerator ListenLoop()
        {
            while (true)
            {
                TryReadMessage();
                yield return new WaitForSeconds(refreshRate);
            }
        }

        public static void SendMessage(string handler, Friend to, string message)
        {
            if (!initialized) return;

            string s = $"{handler}|{self.Name}|{message}";
            SteamNetworking.SendP2PPacket(to.Id, Encoding.UTF8.GetBytes(s));
        }

        public static void SendMessageToAll(string handler, string message)
        {
            if (!initialized) return;

            string s = $"{handler}|{self.Name}|{message}";
            foreach (Friend friend in friends)
            {
                SteamNetworking.SendP2PPacket(friend.Id, Encoding.UTF8.GetBytes(s));
            }
        }

        public static void SendMessageToAllOthers(string handler, string message)
        {
            if (!initialized) return;

            string s = $"{handler}|{self.Name}|{message}";
            foreach (Friend friend in friends)
            {
                if (friend.Name != self.Name)
                {
                    SteamNetworking.SendP2PPacket(friend.Id, Encoding.UTF8.GetBytes(s));
                }
            }
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
                    Debug.Log($"[Multiplayer] Running handler \"{strings[0]}\"");
                    if (FindFriend(strings[1]) is Friend friend)
                    {
                        s = string.Concat(strings.RangeSubset(2, strings.Length - 2));
                        Debug.Log($"[Multiplayer] Sending message to {friend.Name}: \"{s}\"");
                        HandlerRoutines[strings[0]](friend,s);
                    }
                    else
                    {
                        Debug.Log($"[Multiplayer] Unknown friend: {strings[1]}");
                        return true;
                    }
                }
            }
            return false;
        }

        public static Friend? FindFriend(string id)
        {
            foreach(Friend friend in friends)
            {
                if (friend.Name == id)
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
            string[] messages = message.Split('!');
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
        private static void SceneChanged(Scene scene)
        {
            MultiplayerMain.textElement.text = "";
            SceneChanged(scene.name);
        }

        public static void SceneChanged(string sceneName)
        {
            string s = $"SCENE!{sceneName}!";
            string s2 = "";
            if (sceneName == "Battle")
            {
                s2 = $"{Campaign.FindCharacterNode(References.Player).name}";
            }
            //Dashboard.friendIcons[self].SceneChanged(sceneName,s2);
            SendMessageToAllOthers("MSC", s + s2);
            FriendSceneChanged(self, sceneName, s2);
        }
    }
}
