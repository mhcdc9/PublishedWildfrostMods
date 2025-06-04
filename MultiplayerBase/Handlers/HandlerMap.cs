using MultiplayerBase.UI;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

namespace MultiplayerBase.Handlers
{
    //Campaign.FindNode
    //CampaignNode
    public class HandlerMap : MonoBehaviour
    {
        public static HandlerMap instance;

        public static CampaignNode lastBattleCompleted = null;

        public bool Blocking => holder != null && holder.activeSelf;
        public static Friend? friend;

        GameObject holder;
        GameObject background;
        List<GameObject> levels = new List<GameObject>();
        Vector3 offset = new Vector3(0.2f, 0f, 0f);
        float spacing = 0.2f;
        List<int> ids = new List<int>();

        protected void Awake()
        {
            instance = this;
            transform.SetParent(GameObject.Find("CameraContainer/CameraMover/MinibossZoomer/CameraPositioner/CameraPointer/Animator/Rumbler/Shaker/InspectSystem").transform);
            transform.SetAsFirstSibling();
            transform.position = Vector3.zero;
            HandlerSystem.HandlerRoutines.Add("MAP", HandleMessage);

            holder = HelperUI.Background(transform, new Color(0.5f, 0.5f, 0.5f, 0f));
            holder.GetComponent<Image>().enabled = false;
            holder.GetComponent<RectTransform>().sizeDelta = new Vector2(1f, 1f);
            holder.SetActive(false);

            background = HelperUI.Background(holder.transform, new Color(0.77f, 0.70f, 0.56f, 0.8f));
            background.GetComponent<RectTransform>().sizeDelta = new Vector2(1f, 0.6f);
        }

        protected void OnEnable()
        {
            Events.OnBattleWin += BattleWin;
        }

        protected void OnDisable()
        {
            Events.OnBattleWin -= BattleWin;
        }

        private void BattleWin()
        {
            lastBattleCompleted = Campaign.FindCharacterNode(References.Player);
        }

        protected void AddLevel()
        {
            int index = levels.Count();
            GameObject level = HelperUI.VerticalGroup($"Level {index}", holder.transform, new Vector2(0.2f, 1f), spacing);
            
            levels.Add(level);
            for(int i=0; i< levels.Count(); i++)
            {
                levels[i].transform.localPosition = (i - levels.Count()/2f + 0.5f) * offset;
            }
            background.GetComponent<RectTransform>().sizeDelta = new Vector2(0.2f + offset.x * levels.Count(), 0.6f);
        }

        public void AskForData(Friend friend)
        {
            HandlerSystem.SendMessage("MAP", friend, "ASK");
        }

        //NODE! [Level]! [Id]! [Name]! [Cleared]! [misc]
        public void SendData(Friend friend)
        {
            if (References.Player == null)
            {
                return;
            }
            CampaignNode currentNode = Campaign.FindCharacterNode(References.Player);
            CampaignNode startNode = currentNode;
            if (lastBattleCompleted != null && lastBattleCompleted.id <= currentNode.id && lastBattleCompleted.tier - currentNode.tier == 0)
            {
                startNode = lastBattleCompleted;
            }
            string status = (startNode == currentNode) ? "CNODE" : "NODE";
            string cleared = startNode.cleared ? "T" : "F";
            string misc = "";
            if (startNode.type.isBattle)
            {
                object value;
                if (startNode.data.TryGetValue("battle", out value) && value is string battleName)
                {
                    misc = battleName;
                }
            }
            string s = HandlerSystem.ConcatMessage(true, status, "0", $"{startNode.id}", startNode.type.name, cleared, misc);
            HandlerSystem.SendMessage("MAP", friend, s);
            if(startNode.type is CampaignNodeTypeBattle && !startNode.cleared)
            {
                return;
            }
            SendAdjacentNodeData(friend, startNode, 0);
        }

        //NODE! [Level]! [Id]! [Name]! [Cleared]! [misc]
        private void SendAdjacentNodeData(Friend friend, CampaignNode startNode, int index)
        {
            foreach(CampaignNode.Connection connection in startNode.connections)
            {
                CampaignNode node = Campaign.GetNode(connection.otherId);
                if (node != null)
                {
                    string status = (node == Campaign.FindCharacterNode(References.Player)) ? "CNODE" : "NODE";
                    string cleared = node.cleared ? "T" : "F";
                    string misc = "";
                    if (node.type.isBattle)
                    {
                        object value;
                        if (node.data.TryGetValue("battle", out value) && value is string battleName)
                        {
                            misc = battleName;
                        }
                    }
                    string s = HandlerSystem.ConcatMessage(true, status, $"{index+1}", $"{node.id}", node.type.name, cleared, misc);
                    HandlerSystem.SendMessage("MAP", friend, s);
                    if (!(node.type is CampaignNodeTypeBattle))
                    {
                        SendAdjacentNodeData(friend, node, index + 1);
                    }
                }
            }
        }

        public void HandleMessage(Friend friend, string message)
        {
            string[] messages = HandlerSystem.DecodeMessages(message);
            Debug.Log($"[Multiplayer] {message}");

            switch (messages[0])//0 -> Action
            {
                case "ASK":
                    SendData(friend);
                    break;
                case "CNODE":
                case "NODE":
                    StartCoroutine(DisplayNode(friend, messages));
                    break;
            }
        }

        //NODE! [Level]! [Id]! [Name]! [Cleared]! [misc]
        public IEnumerator DisplayNode(Friend friend, string[] messages)
        {
            if (messages[1] == "0")
            {
                Clear();
            }
            int level = int.Parse(messages[1]);
            if (level >= levels.Count())
            {
                AddLevel();
            }
            int id = int.Parse(messages[2]);
            if (ids.Contains(id))
            {
                yield break;
            }
            ids.Add(id);
            CampaignNodeType node = AddressableLoader.Get<CampaignNodeType>("CampaignNodeType",messages[3]);

            Sprite sprite = DetermineNodeSprite(node, messages);

            if (sprite != null)
            {
                GameObject obj = new GameObject(node.zoneName);
                Image image = obj.AddComponent<Image>();
                image.sprite = sprite;
                image.GetComponent<RectTransform>().sizeDelta = new Vector2(0.1f, 0.1f * sprite.rect.height / sprite.rect.width);
                obj.transform.SetParent(levels[level].transform, false);
            }
            else
            {
                GameObject obj = new GameObject(node.zoneName);
                TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
                text.text = node.zoneName;
                text.fontSize = 0.04f;
                text.outlineColor = Color.black;
                text.outlineWidth = 0.06f;
                text.horizontalAlignment = HorizontalAlignmentOptions.Center;
                text.verticalAlignment = VerticalAlignmentOptions.Middle;
                text.GetComponent<RectTransform>().sizeDelta = new Vector2(0.3f, 0.1f);
                obj.transform.SetParent(levels[level].transform, false);
            }

        }

        public Sprite DetermineNodeSprite(CampaignNodeType node, string[] messages)
        {
            Sprite sprite = null;
            //A map sprite can be made in three different ways: mapNodeSprite (easy), spriteOptions (easy to cheese), sprite setters (impossibly hard). 
            if (node?.mapNodeSprite != null)
            {
                sprite = node.mapNodeSprite;
            }
            if (messages[4] == "T" && node?.mapNodePrefab?.spriteOptions != null && node.mapNodePrefab.clearedSpriteOptions.Length > 0)
            {
                return node.mapNodePrefab.clearedSpriteOptions[0];
            }
            else if (node?.mapNodePrefab?.spriteOptions != null && node.mapNodePrefab.spriteOptions.Length > 0)
            {
                return node.mapNodePrefab.spriteOptions[0];
            }
            else if (node?.isBattle == true)
            {
                BattleData battleData = AddressableLoader.Get<BattleData>("BattleData", messages[5]);
                if (battleData?.sprite != null && battleData.sprite.texture.width > 10)
                {
                    return battleData.sprite;
                }
            }
            else if (node?.mapNodePrefab?.GetComponentInChildren<MapNodeSpriteSetterItem>() != null)
            {
                MapNodeSpriteSetterItem spriteSetterItem = node.mapNodePrefab.GetComponentInChildren<MapNodeSpriteSetterItem>();
                sprite = (messages[4] == "T") ? spriteSetterItem.clearedSprite : spriteSetterItem.normalSprite;
            }
            return sprite;
        }

        public void Clear()
        {
            ids.Clear();
            levels.DestroyAllAndClear();
        }

        public void ToggleViewer(Friend friend)
        {
            if (holder.activeSelf)
            {
                CloseViewer();
            }
            else
            {
                holder.SetActive(true);
                HandlerMap.friend = friend;
                AskForData(friend);
            }
        }

        public void CloseViewer()
        {
            holder.SetActive(false);
            Clear();
        }
    }
}
