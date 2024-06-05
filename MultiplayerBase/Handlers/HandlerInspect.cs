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
        //Friend friend;
        //ulong id;
        public static HandlerInspect instance;

        public List<OtherCardViewer> lanes = new List<OtherCardViewer>();
        public int laneIndex = 0;
        public Vector3 offset = new Vector3(0,-3f,0);
        protected Vector3 gap = new Vector3(0.3f, 0, 0);

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

            SetLane(0);

            HandlerSystem.HandlerRoutines.Add("INS", HandleMessage);
            Debug.Log("[Multiplayer] Inspection Handler Online!");
        }

        private void SetLane(int index)
        {
            for(int i=lanes.Count(); i<=index; i++)
            {
                OtherCardViewer lane = HelperUI.OtherCardViewer($"Lane {lanes.Count()}", transform, cc);
                lane.gap = gap;
                lane.transform.localPosition = lanes.Count() * offset;
                cc.hoverEvent.AddListener(lane.Hover);
                cc.unHoverEvent.AddListener(lane.Unhover);
                lanes.Add(lane);
            }
            laneIndex = index;
        }

        public static void SelectPing(Entity entity)
        {
            if(entity?.containers == null)
            {
                return;
            }
            foreach(CardContainer container in entity.containers)
            {
                if (container is OtherCardViewer ocv)
                {
                    (Friend friend, ulong id) = ocv.Find(entity);
                    HandlerSystem.SendMessage("INS", friend, $"PING!{friend.Name}!{id}!");
                    return;
                }
            }
        }

        public static void SelectDisp(Entity entity)
        {
            Friend friend = HandlerSystem.self;
            ulong id = entity.data.id;
            string s = $"DISP!{friend.Name}!";
            s += EncodeEntity(entity, id);
            HandlerSystem.SendMessageToAll("INS", s);    
        }

        public static string EncodeEntity(Entity entity, ulong id)
        {
            string s = $"{id}!{ entity.data.name}!";
            string upgradeString = "";
            foreach (CardUpgradeData upgrade in entity.data.upgrades)
            {
                upgradeString += upgrade.name + ",";
            }
            upgradeString = upgradeString.IsNullOrEmpty() ? upgradeString : upgradeString.Remove(upgradeString.Length - 1);
            s += upgradeString + "!";
            return s;
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
            foreach (OtherCardViewer ocv in lanes)
            {
                //Debug.Log("[Multiplayer] " + lanes[laneIndex].ToArray());
                ocv.ClearAndDestroyAllImmediately();
                ocv.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            }
            
        }

        /*
         * CardData:
         * id
         * name
         * title
         * hp
         * damage
         * counter
         * random3
         * upgrades
         * customData
         * attackEffects
         * startWithEffects
         * traits
         * injuries
         * 
         * BattleEntity:
         * height
         * damage.current
         * damage.max
         * hp.current
         * hp.max
         * counter.current
         * counter.max
         * uses.current
         * uses.max
         * flipped
         * attackEffects
         */
        public IEnumerator DispCard(Friend friend, string[] messages, int index = 0, bool clear = true)
        {
            GetComponent<Image>().color = Color.black;
            SetLane(index);
            lanes[laneIndex].SetSize(1, 0.5f);
            if (clear)
            {
                Clear();
            }

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
            Card card = CreateDisplayCard(cc, messages.Skip(3).ToArray());
            lanes[laneIndex].Add(card.entity,owner, id);
            lanes[laneIndex].SetChildPositions();
            yield return card.UpdateData();
            card.entity.flipper.FlipUp(force: true);
        }

        public static Card CreateDisplayCard(CardController cc, string[] messages)
        {
            Debug.Log("[Multiplayer] " + messages[0]);
            CardData cardData = AddressableLoader.Get<CardData>("CardData", messages[0]).Clone(false); //3(0) -> Carddata
            if (!messages[1].IsNullOrWhitespace())
            {
                Debug.Log("[Multiplayer] Has Upgrades.");
                string[] upgrades = messages[1].Split(new char[] { ',' }); //4(1) -> Upgrades
                foreach (string upgrade in upgrades)
                {
                    CardUpgradeData upgradeData = AddressableLoader.Get<CardUpgradeData>("CardUpgradeData", upgrade).Clone();
                    upgradeData.Assign(cardData);
                }
            }
            if (cardData.cardType.name == "Leader")
            {
                Debug.Log("[Multiplayer] Leader Detected.");
                cardData.customData = References.PlayerData.inventory.deck.FirstOrDefault((deckcard) => deckcard.cardType.name == "Leader").customData;
            }
            Card card = CardManager.Get(cardData, cc, HandlerSystem.enemyDummy, inPlay: false, isPlayerCard: true);
            card.entity.flipper.FlipDownInstant();
            return card;
        }

        public IEnumerator PingCard(Friend friend, string[] messages)
        {
            ulong id = ulong.Parse(messages[2]);//2 -> id
            if (messages[1] != HandlerSystem.self.Name)//1 -> Friend
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
