using NaughtyAttributes.Test;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization.Pseudo;
using UnityEngine.UI;

namespace MultiplayerBase.Handlers
{
    internal class HandlerInspect : MonoBehaviour
    {
        Vector3 defaultPosition = new Vector3(-10, 3, 2);
        CardControllerSelectCard cc;
        CardLane lane;
        Friend friend;
        ulong id;
        public static HandlerInspect instance;

        protected void Start()
        {
            instance = this;
            Debug.Log("[Multiplayer] Initializing Inspection Handler...");

            transform.SetParent(GameObject.Find("CameraContainer/CameraMover/MinibossZoomer/CameraPositioner/CameraPointer/Animator/Rumbler/Shaker/InspectSystem").transform);
            transform.SetAsFirstSibling();
            transform.position = defaultPosition;

            cc = gameObject.AddComponent<CardControllerSelectCard>();
            cc.pressEvent = new UnityEventEntity();
            cc.hoverEvent = new UnityEventEntity();
            cc.unHoverEvent = new UnityEventEntity();
            cc.pressEvent.AddListener(SelectPing);

            Image image = gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.25f);

            GetComponent<RectTransform>().sizeDelta = new Vector2(1, 1);

            lane = gameObject.AddComponent<CardLane>();
            lane.holder = GetComponent<RectTransform>();
            lane.onAdd = new UnityEventEntity();
            lane.onRemove = new UnityEventEntity();
            lane.gap = new Vector3(0f, 0f, 0f);

            HandlerSystem.HandlerRoutines.Add("INS", HandleMessage);
            Debug.Log("[Multiplayer] Inspection Handler Online!");
        }

        public void SelectPing(Entity entity)
        {
            string s = $"INS|{MultiplayerMain.self.Name}|PING!{friend.Name}!{id}";
            SteamNetworking.SendP2PPacket(friend.Id, Encoding.UTF8.GetBytes(s));
        }

        public void SelectDisp(Entity entity)
        {
            string s = $"INS|{MultiplayerMain.self.Name}|DISP!{entity.data.id}!{entity.data.name}!";
            string upgradeString = "";
            foreach(CardUpgradeData upgrade in entity.data.upgrades)
            {
                upgradeString += upgrade.name + ",";
            }
            upgradeString = upgradeString.IsNullOrEmpty() ? upgradeString : upgradeString.Remove(upgradeString.Length - 1);
            s += upgradeString + "!";
            foreach(Friend friend in MultiplayerMain.friends)
            {
                SteamNetworking.SendP2PPacket(friend.Id, Encoding.UTF8.GetBytes(s));
            }     
        }

        public void HandleMessage(Friend friend, string message)
        {
            string[] messages = message.Split(new char[] { '!' });
            Debug.Log($"[Multiplayer] {message}");

            switch(messages[0])//0 -> Action
            {
                case "DISP":
                    StartCoroutine(DispCard(friend,messages));
                    break;
                case "PING":
                    StartCoroutine(PingCard(friend, messages));
                    break;
            }

        }

        public void Clear()
        {
            Debug.Log("[Multiplayer] " + lane.ToArray());
            lane.ClearAndDestroyAllImmediately();
            GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        }

        public IEnumerator DispCard(Friend friend, string[] messages)
        {
            GetComponent<Image>().color = Color.black;
            lane.SetSize(1, 0.5f);
            Clear();

            id = ulong.Parse(messages[1]); //1 -> id
            this.friend = friend;

            CardData cardData = AddressableLoader.Get<CardData>("CardData", messages[2]).Clone(false); //2 -> Carddata
            if (!messages[3].IsNullOrWhitespace())
            {
                Debug.Log("[Multiplayer] Has Upgrades.");
                string[] upgrades = messages[3].Split(new char[] { ',' }); //3 -> Upgrades
                foreach (string upgrade in upgrades)
                {
                    CardUpgradeData upgradeData = AddressableLoader.Get<CardUpgradeData>("CardUpgradeData", upgrade).Clone();
                    upgradeData.Assign(cardData);
                }
            }
            Card card = CardManager.Get(cardData, cc, null, inPlay: false, isPlayerCard: true);
            card.entity.flipper.FlipDownInstant();
            lane.Add(card.entity);
            lane.SetChildPositions();
            yield return card.UpdateData();
            card.entity.flipper.FlipUp(force: true);
        }

        public IEnumerator PingCard(Friend friend, string[] messages)
        {
            ulong id = ulong.Parse(messages[2]);//2 -> id
            if (messages[1] != MultiplayerMain.self.Name)//1 -> Friend
            {
                yield break;
            }
            foreach (Entity entity in GameObject.FindObjectsOfType<Entity>()) 
            {
                if (entity?.data?.id == id && entity?.curveAnimator != null)
                {
                    entity.curveAnimator.Ping();
                }
            }
            yield break;
        }
    }
}
