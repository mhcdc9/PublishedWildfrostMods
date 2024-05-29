using JetBrains.Annotations;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;

namespace MultiplayerBase
{
    //After the party is finalized, the dashboard will be the main means of communication. It lives in front of the inspection system.
    public class Dashboard : MonoBehaviour
    {
        GameObject background;
        Vector3 defaultPosition = new Vector3(7, 5, 0);
        GameObject buttonGroup;
        Dictionary<Friend, Button> buttons;

        InspectSystem inspectsystem;

        public static float refreshRate = 0.1f;
        public void Init()
        {
            transform.SetParent(GameObject.Find("CameraContainer/CameraMover/MinibossZoomer/CameraPositioner/CameraPointer/Animator/Rumbler/Shaker/InspectSystem").transform);
            transform.position = defaultPosition;
            background = HelperUI.Background(transform, new Color(0f, 0f, 0f, .75f));
            background.SetActive(false);

            inspectsystem = GameObject.FindObjectOfType<InspectSystem>(true);

            float totalSize = MultiplayerMain.friends.Length*(1.2f) - 0.2f;
            buttonGroup = HelperUI.HorizontalGroup("Friend Icons", transform, new Vector2(totalSize,1f));
            buttons = new Dictionary<Friend, Button>();
            foreach (Friend friend in MultiplayerMain.friends)
            {
                buttons.Add(friend, HelperUI.ButtonTemplate(buttonGroup.transform, new Vector2(1, 1), Vector3.zero, "42", friend.Id == MultiplayerMain.self.Id ? Color.white : Color.gray ));
                buttons[friend].onClick.AddListener(() => FriendIconPressed(friend));
            }
            SteamNetworking.OnP2PSessionRequest += SessionRequest;
            StartCoroutine(ListenLoop());
            Debug.Log("[Multiplayer] Dashboard is set up!");
        }

        public void FriendIconPressed(Friend friend)
        {
            Debug.Log($"[Multiplayer] Sending Message to {friend.Name}");
            String s;
            if (inspectsystem.isActiveAndEnabled)
            {
                s = $"{MultiplayerMain.self.Name}: {inspectsystem.inspect.data.title}";
            }
            else
            {
                s = $"{MultiplayerMain.self.Name}: {Dead.PettyRandom.Range(0f,1f)}";
            }
            SteamNetworking.SendP2PPacket(friend.Id, Encoding.UTF8.GetBytes(s));
        }

        public void SessionRequest(SteamId id)
        {
            foreach(Friend friend in MultiplayerMain.friends)
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

        public IEnumerator ListenLoop()
        {
            while (true)
            {
                TryReadMessage();
                yield return new WaitForSeconds(refreshRate);
            }
        }

        public bool TryReadMessage()
        {
            Steamworks.Data.P2Packet? packet = SteamNetworking.ReadP2PPacket();
            if (packet is P2Packet p)
            {
                string s = Encoding.UTF8.GetString(p.Data);
                MultiplayerMain.textElement.text = s;
                return true;
            }
            return false;
        }
    }
}
