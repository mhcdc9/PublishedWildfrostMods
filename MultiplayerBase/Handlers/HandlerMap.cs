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
using MultiplayerBase.UI;

namespace MultiplayerBase.Handlers
{
    //Campaign.FindNode
    //CampaignNode
    public class HandlerMap : MonoBehaviour
    {
        public static HandlerMap instance;

        public bool Blocking => background != null && background.activeSelf;
        public Friend? friend;

        GameObject holder;
        GameObject background;
        List<GameObject> levels = new List<GameObject>();
        Vector3 offset = new Vector3(0.2f, 0f, 0f);
        float spacing = 0.2f;
        List<int> ids = new List<int>();

        protected void Start()
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

            background = HelperUI.Background(holder.transform, new Color(0.5f, 0.5f, 0.5f, 0.9f));
            background.GetComponent<RectTransform>().sizeDelta = new Vector2(1f, 0.6f);
        }

        protected void AddLevel()
        {
            int index = levels.Count();
            GameObject level = HelperUI.VerticalGroup($"Level {index}", holder.transform, new Vector2(0.2f, 1f), spacing);
            
            levels.Add(level);
            for(int i=0; i< levels.Count(); i++)
            {
                levels[i].transform.localPosition = (i - levels.Count()/2f) * offset;
            }
            background.GetComponent<RectTransform>().sizeDelta = new Vector2(0.4f + offset.x * levels.Count(), 0.6f);
        }

        public void AskForData(Friend friend)
        {
            HandlerSystem.SendMessage("MAP", friend, "ASK");
        }

        public void SendData(Friend friend)
        {
            if (References.Player == null)
            {
                return;
            }
            CampaignNode startNode = Campaign.FindCharacterNode(References.Player);
            string cleared = startNode.cleared ? "T" : "F";
            HandlerSystem.SendMessage("MAP", friend, $"NODE!0!{startNode.id}!{startNode.type.name}!{cleared}!");
            if(startNode.type is CampaignNodeTypeBattle && !startNode.cleared)
            {
                return;
            }
            SendAdjacentNodeData(friend, startNode, 0);
        }

        private void SendAdjacentNodeData(Friend friend, CampaignNode startNode, int index)
        {
            foreach(CampaignNode.Connection connection in startNode.connections)
            {
                CampaignNode node = Campaign.GetNode(connection.otherId);
                if (node != null)
                {
                    HandlerSystem.SendMessage("MAP", friend, $"NODE!{index + 1}!{node.id}!{node.type.name}!F!");
                    if (!(node.type is CampaignNodeTypeBattle))
                    {
                        SendAdjacentNodeData(friend, node, index + 1);
                    }
                }
            }
        }

        public void HandleMessage(Friend friend, string message)
        {
            string[] messages = message.Split(new char[] { '!' });
            Debug.Log($"[Multiplayer] {message}");

            switch (messages[0])//0 -> Action
            {
                case "ASK":
                    SendData(friend);
                    break;
                case "NODE":
                    StartCoroutine(DisplayNode(friend, messages));
                    break;
            }
        }

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
            Sprite sprite = null;
            if (node.mapNodeSprite != null)
            {
                sprite = node.mapNodeSprite;
            }
            if (messages[4] == "T")
            {
                if (node?.mapNodePrefab?.spriteOptions != null && node.mapNodePrefab.clearedSpriteOptions.Length > 0)
                {
                    sprite = node.mapNodePrefab.clearedSpriteOptions[0];
                }
            }
            else
            {
                if (node?.mapNodePrefab?.spriteOptions != null && node.mapNodePrefab.spriteOptions.Length > 0)
                {
                    sprite = node.mapNodePrefab.spriteOptions[0];
                }
            }
            if (sprite != null)
            {
                GameObject obj = new GameObject(node.zoneName);
                Image image = obj.AddComponent<Image>();
                image.sprite = sprite;
                image.GetComponent<RectTransform>().sizeDelta = new Vector2(0.1f, 0.1f*sprite.rect.height/sprite.rect.width);
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

        public void Clear()
        {
            ids.Clear();
            levels.DestroyAllAndClear();
        }

        public void ToggleViewer(Friend friend)
        {
            if (holder.activeSelf)
            {
                holder.SetActive(false);
                this.friend = friend;
                Clear();
            }
            else
            {
                holder.SetActive(true);
                AskForData(friend);
            }
        }
    }
}
