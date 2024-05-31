using NaughtyAttributes.Test;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MultiplayerBase.Handlers
{
    internal class HandlerInspect : MonoBehaviour
    {
        Vector3 defaultPosition = new Vector3(-10, 3, 2);
        private CardControllerSelectCard cc;
        OtherCardViewer lane;
        //Friend friend;
        //ulong id;
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

            lane = gameObject.AddComponent<OtherCardViewer>();
            lane.holder = GetComponent<RectTransform>();
            lane.onAdd = new UnityEventEntity();
            lane.onRemove = new UnityEventEntity();
            lane.gap = new Vector3(0f, 0f, 0f);

            cc.hoverEvent.AddListener(lane.Hover);
            cc.unHoverEvent.AddListener(lane.Unhover);

            HandlerSystem.HandlerRoutines.Add("INS", HandleMessage);
            Debug.Log("[Multiplayer] Inspection Handler Online!");
        }

        public void SelectPing(Entity entity)
        {
            (Friend friend, ulong id) = lane.Find(entity);
            string s = $"INS|{MultiplayerMain.self.Name}|PING!{friend.Name}!{id}";
            SteamNetworking.SendP2PPacket(friend.Id, Encoding.UTF8.GetBytes(s));
        }

        public void SelectDisp(Entity entity)
        {
            Friend friend = MultiplayerMain.self;
            ulong id = entity.data.id;
            if (entity.preContainers != null)
            {
                foreach (CardContainer c in entity.preContainers)
                {
                    if (c is OtherCardViewer ocv)
                    {
                        if (ocv == lane)
                        {
                            return;
                        }
                        (friend, id) = ocv.Find(entity);
                        break;
                    }
                }
            }
            string s = $"INS|{MultiplayerMain.self.Name}|DISP!{friend.Name}!{id}!{entity.data.name}!";
            string upgradeString = "";
            foreach(CardUpgradeData upgrade in entity.data.upgrades)
            {
                upgradeString += upgrade.name + ",";
            }
            upgradeString = upgradeString.IsNullOrEmpty() ? upgradeString : upgradeString.Remove(upgradeString.Length - 1);
            s += upgradeString + "!";
            foreach(Friend f in MultiplayerMain.friends)
            {
                SteamNetworking.SendP2PPacket(f.Id, Encoding.UTF8.GetBytes(s));
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

            Friend owner;
            if (HandlerSystem.FindFriend(messages[1]) is Friend f)//1 -> owner
            {
                owner = f;
            }
            else
            {
                owner = friend;
            }

            ulong id = ulong.Parse(messages[2]); //2 -> id

            CardData cardData = AddressableLoader.Get<CardData>("CardData", messages[3]).Clone(false); //3 -> Carddata
            if (!messages[4].IsNullOrWhitespace())
            {
                Debug.Log("[Multiplayer] Has Upgrades.");
                string[] upgrades = messages[4].Split(new char[] { ',' }); //4 -> Upgrades
                foreach (string upgrade in upgrades)
                {
                    CardUpgradeData upgradeData = AddressableLoader.Get<CardUpgradeData>("CardUpgradeData", upgrade).Clone();
                    upgradeData.Assign(cardData);
                }
            }
            Card card = CardManager.Get(cardData, cc, References.Player, inPlay: false, isPlayerCard: true);
            card.entity.flipper.FlipDownInstant();
            lane.Add(card.entity,owner, id);
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
