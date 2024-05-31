using Steamworks.Data;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

//Battle/Canvas


namespace MultiplayerBase.Handlers
{
    internal static class HandlerSystem
    {
        public static float refreshRate = 0.1f;

        public static Dictionary<string, Action<Friend, string>> HandlerRoutines = new Dictionary<string, Action<Friend, string>>();

        public static IEnumerator ListenLoop()
        {
            while (true)
            {
                TryReadMessage();
                yield return new WaitForSeconds(refreshRate);
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
            foreach(Friend friend in MultiplayerMain.friends)
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
    }
}
