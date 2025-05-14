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
using MultiplayerBase.UI;

namespace MultiplayerBase.Handlers
{
    internal class HandlerInspect : MonoBehaviour
    {
        Vector3 defaultPosition = new Vector3(-8.6f, 2.35f, 0);
        internal CardControllerSelectCard cc;
        //Friend friend;
        //ulong id;
        public static HandlerInspect instance;


        public Button hideButton;
        public Button clearButton;
        protected bool hidden = false;
        public GameObject verticalGroup;
        public List<OtherCardViewer> lanes = new List<OtherCardViewer>();
        public NoncardViewer charmLane;
        public int laneIndex = 0;
        protected float verticalSpacing = 0.3f;
        public Vector3 offset = new Vector3(0,-3f,0);
        protected Vector3 gap = new Vector3(0.3f, 0, 0);

        protected void Awake()
        {

            instance = this;
            Debug.Log("[Multiplayer] Initializing Inspection Handler...");

            transform.SetParent(GameObject.Find("CameraContainer/CameraMover/MinibossZoomer/CameraPositioner/CameraPointer/Animator/Rumbler/Shaker/InspectSystem").transform);
            transform.SetAsFirstSibling();
            //transform.position = defaultPosition;

            cc = gameObject.AddComponent<CardControllerSelectCard>();
            cc.pressEvent = new UnityEventEntity();
            cc.hoverEvent = new UnityEventEntity();
            cc.unHoverEvent = new UnityEventEntity();
            cc.pressEvent.AddListener(SelectPing);

            RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
            gameObject.AddComponent<WorldSpaceCanvasSafeArea>().parent = transform.parent.GetComponent<RectTransform>();

            verticalGroup = HelperUI.VerticalGroup("Viewers", transform, Vector2.zero, verticalSpacing);
            verticalGroup.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            verticalGroup.transform.localPosition = new Vector3(2.7f, 3.4f, 0f);
            verticalGroup.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 1.75f, 0f);

            charmLane = NoncardViewer.Create(verticalGroup.transform);
            charmLane.gameObject.SetActive(false);

            SetLane(0);

            hideButton = HelperUI.ButtonTemplate(transform, new Vector2(1f, 0.3f), new Vector3(-0.5f, 4.35f, 0), "", Color.white);
            hideButton.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 1.5f, 1);
            hideButton.onClick.AddListener(ToggleHide);
            clearButton = HelperUI.ButtonTemplate(transform, new Vector2(1f, 0.3f), new Vector3(0.5f, 4.35f, 0), "", Color.red);
            clearButton.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 2.5f, 1);
            clearButton.onClick.AddListener(Clear);

            HandlerSystem.HandlerRoutines.Add("INS", HandleMessage);
            Debug.Log("[Multiplayer] Inspection Handler Online!");
        }

        protected void OnEnable()
        {
            hideButton.gameObject.SetActive(true);
            clearButton.gameObject.SetActive(true);
        }

        protected void OnDisable()
        {
            hideButton.gameObject.SetActive(false);
            clearButton.gameObject.SetActive(false);
        }

        private void ToggleHide()
        {
            if (hidden)
            {
                if (charmLane.Count > 0)
                {
                    charmLane.gameObject.SetActive(true);
                }
                foreach(OtherCardViewer ocv in lanes)
                {
                    if (ocv.Count > 0)
                    {
                        ocv.gameObject.SetActive(true);
                    }
                }
                hideButton.GetComponent<Image>().color = Color.white;
                clearButton.GetComponent<Image>().color = Color.red;
                HandlerEvent.instance.ShowIndicators();
                hidden = false;
            }
            else
            {
                charmLane.gameObject.SetActive(false);
                foreach (OtherCardViewer ocv in lanes)
                {
                    ocv.gameObject.SetActive(false);
                }
                hideButton.GetComponent<Image>().color = new Color(0.5f,0.5f,0.5f,0.5f);
                clearButton.GetComponent<Image>().color = new Color(1f, 0f, 0f, 0.5f);
                HandlerEvent.instance.HideIndicators();
                hidden = true;
            }
        }

        private void SetLane(int index)
        {
            for(int i=lanes.Count(); i<=index; i++)
            {
                OtherCardViewer lane = HelperUI.OtherCardViewer($"Lane {lanes.Count()}", verticalGroup.transform, cc);
                lane.gameObject.SetActive(false);
                lane.gap = gap;
                //lane.transform.localPosition = new Vector3(0,2.7f,0) + lanes.Count() * offset;
                //lane.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 2f, 1);
                lane.owner = HandlerSystem.playerDummy;
                cc.hoverEvent.AddListener(lane.Hover);
                cc.unHoverEvent.AddListener(lane.Unhover);
                charmLane.transform.SetAsLastSibling();
                lanes.Add(lane);
            }
            laneIndex = index;
            if (lanes[laneIndex].Count >= 10)
            {
                SetLane(laneIndex+1);
            }
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
                    HandlerSystem.SendMessage("INS", friend, HandlerSystem.ConcatMessage(false,"PING",$"{friend.Id.Value}",$"{id}"));
                    return;
                }
            }
        }

        public static ulong FindTrueID(Entity entity)
        {
            if (entity?.containers == null)
            {
                return 0;
            }
            foreach (CardContainer container in entity.containers)
            {
                if (container is OtherCardViewer ocv)
                {
                    (_, ulong id) = ocv.Find(entity);
                    return id;
                }
            }
            return 0;
        }

        public static void SelectDisp(Entity entity)
        {
            foreach(OtherCardViewer ocv in instance.lanes)
            {
                if(ocv.Contains(entity))
                {
                    return;
                }
            }
            Friend friend = HandlerSystem.self;
            ulong id = entity.data.id;
            string s = HandlerSystem.ConcatMessage(false,"DISP",$"{friend.Id.Value}");
            //s += EncodeEntity(entity, id);
            s += "! " + CardEncoder.Encode(entity, id);
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
            string[] messages = HandlerSystem.DecodeMessages(message);
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
            HandlerEvent.instance.ClearIndicators();
            NoncardReward[] noncardRewards = charmLane.ToArray();
            charmLane.Clear();
            for (int i = noncardRewards.Length - 1; i >= 0; i--)
            {
                noncardRewards[i].gameObject.DestroyImmediate();
            }
            charmLane.gameObject.SetActive(false);
            foreach (OtherCardViewer ocv in lanes)
            {
                //Debug.Log("[Multiplayer] " + lanes[laneIndex].ToArray());
                ocv.ClearAndDestroyAllImmediately();
                ocv.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
                ocv.gameObject.SetActive(false);
            }
            
        }

        public IEnumerator DispCard(Friend friend, string[] messages, int index = 0, bool clear = true)
        {
            SetLane(index);
            lanes[laneIndex].SetSize(1, 0.5f);
            if (clear)
            {
                Clear();
            }
            if (!hidden)
            {
                lanes[laneIndex].gameObject.SetActive(true);
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

            Entity entity = CardEncoder.DecodeEntity1(cc, lanes[laneIndex].owner, messages.Skip(3).ToArray());
            yield return CardEncoder.DecodeEntity2(entity, messages.Skip(3).ToArray());
            lanes[laneIndex].Add(entity,owner, id);
            lanes[laneIndex].SetChildPositions();
            entity.flipper.FlipUp(force: true);
            if (hidden)
            {
                hideButton.GetComponent<Image>().color = new Color(0f,1f,0f,0.75f);
                clearButton.GetComponent<Image>().color = new Color(1f, 0f, 0f, 0.75f);
            }
        }

        //CardData!customData!attackEffects!startWithEffects!traits!injuries!hp!damage!counter!upgrades!forceTitle!
        //Entity!height!damageCurrent!damageMax!hpcurrent!hpMax!counterCurrent!counterMax!usesCurrent!usesMax!
        public static Card CreateDisplayCard(CardController cc, CardContainer container, string[] messages)
        {
            Debug.Log("[Multiplayer] " + messages[0]);
            CardData cardData = CardEncoder.DecodeData(messages);
            Card card = CardManager.Get(cardData, cc, container.owner, inPlay: false, isPlayerCard: true);
            if (References.Battle?.cards != null)
            {
                References.Battle.cards.Remove(card.entity);
            }
            card.entity.flipper.FlipDownInstant();
            return card;
        }

        //DISP2!id!Type!Name
        public IEnumerator DispNoncard(Friend friend, string[] messages, bool clear = true)
        {
            if (clear)
            {
                Clear();
            }
            if (!hidden)
            {
                charmLane.gameObject.SetActive(true);
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
            int id = int.Parse(messages[0]);//0 -> id
            NoncardReward ncr = null;
            if (messages[2] == "MODI")
            {
                ncr = NoncardReward.CreateModifier(transform, new Vector2(0.66f, 1f), messages[3]);
            }
            else if (messages[2] == "UPGR")
            {
                ncr = NoncardReward.CreateUpgrade(transform, new Vector2(1f, 1f), messages[3]);
            }
            else if (messages[2] == "MISC")
            {
                ncr = NoncardReward.CreateMisc(transform, new Vector2(1f, 1f), messages[3], friend.Name);
            }
            if (ncr == null)
            {
                Debug.Log("[Multiplayer] Could not find type " + messages[3]);
                yield break;
            }
            charmLane.Add(ncr, owner, id);
            ncr.gameObject.SetActive(true);
            yield break;
        }

        public IEnumerator PingCard(Friend friend, string[] messages)
        {
            ulong id = ulong.Parse(messages[2]);//2 -> id
            if (ulong.Parse(messages[1]) != HandlerSystem.self.Id.Value)//1 -> Friend
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
